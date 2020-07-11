#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

using Ex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

#if !UNITY

namespace Ex {
	
	/// <summary> A single instance of a map. </summary>
	public class Map {

		/// <summary> Field to check against for infinite map bounds. </summary>
		static readonly Bounds EMPTY_BOUNDS = new Bounds();
		/// <summary> Check if a point is inside of a bounds, with the given shape applied. </summary>
		/// <param name="bounds"> Bounds parameters to use </param>
		/// <param name="shape"> Shape to use </param>
		/// <param name="point"> Point to check </param>
		/// <returns> True if the point is contained within bounds, false otherwise. </returns>
		public static bool Contains(Bounds bounds, BoundsShape shape, Vector3 point) {
			if (bounds.Equals(EMPTY_BOUNDS)) { return true; }
			Vector3 center = bounds.center;
			float sqRadius = bounds.extents.x * bounds.extents.x;
			Vector3 diff = center-point;
			
			switch (shape) {
				case BoundsShape.Sphere:
					return diff.sqrMagnitude < sqRadius; 
				case BoundsShape.Cylinder:
					float y = diff.y; diff.y = 0;
					return diff.sqrMagnitude < sqRadius && Mathf.Abs(y) < bounds.extents.y;
				case BoundsShape.Box:
				default:
					return bounds.Contains(point);
			}
		}

		/// <summary> Service the map is bound to. Used to facilitate interactions with the <see cref="EntityService"/> and other functionality </summary>
		public MapService service { get; private set; }
		/// <summary> Shared information about this map. </summary>
		public MapInfo info { get; private set; }
		/// <summary> ID assigned to map </summary>
		public Guid id { get; private set; }
		/// <summary> Clients in the map </summary>
		public List<Client> clients { get; private set; }
		/// <summary> Cells in the map </summary>
		public Dictionary<Vector3Int, Cell> cells { get; private set; }
		/// <summary> Entities in the map, other than globals. </summary>
		public Dictionary<Guid, Entity> entities { get; private set; }

		// <summary> All ids in the map, other than globals. </summary>
		//public List<Guid> idsInMap { get; private set; }

		/// <summary>  Global entity list. 
		/// These are visible for all connected clients, regardless of position. 
		/// Used for some background objects, terrain, and 'bosses'. </summary>
		public List<Guid> globalEntities { get; private set; }
		
		public EntityService entityService { get { return service.GetService<EntityService>(); } }

		public bool is3d { get { return info.is3d; } }
		public string name { get { return info.name; } }
		/// <summary> Instance index of this map. </summary>
		public int instanceIndex { get; private set; }
		public float cellSize { get { return info.cellSize; } }
		public int cellDist { get { return info.cellDist; } }

		public float speedCap = 3332.0f;

		/// <summary> Get a string uniquely identifying this map. </summary>
		public string identity { get { return $"{name}#{instanceIndex}:{id}"; } } 

		private ConcurrentQueue<Guid> toSpawn;
		private ConcurrentQueue<Guid> toDespawn;
		private ConcurrentQueue<EntityMoveRequest> toMove;

		private DateTime lastUpdate;
		private int tickTime = 10;

		const int TREND_SIZE = 128;

		/// <summary> Time Taken during spawning part of update tick </summary>
		public Trender spawnTrend = new Trender(TREND_SIZE);
		/// <summary> Time Taken during despawning part of update tick </summary>
		public Trender despawnTrend = new Trender(TREND_SIZE);
		/// <summary> Time Taken during entity update part of update tick </summary>
		public Trender updateTrend = new Trender(TREND_SIZE);
		/// <summary> Time Taken during collision part of update tick </summary>
		public Trender collideTrend = new Trender(TREND_SIZE);

		public Map(MapService service, MapInfo info, int? instanceIndex = null) {
			if (!service.isMaster) {
				throw new Exception($"Only Master Server may create map instances.");
			}
			this.service = service;
			this.info = info;
			id = Guid.NewGuid();
			this.instanceIndex = instanceIndex ?? 0;
			
			clients = new List<Client>();
			cells = new Dictionary<Vector3Int, Cell>();
			entities = new Dictionary<Guid, Entity>();
			//idsInMap = new List<Guid>();
			globalEntities = new List<Guid>();
			toSpawn = new ConcurrentQueue<Guid>();
			toDespawn = new ConcurrentQueue<Guid>();
			toMove = new ConcurrentQueue<EntityMoveRequest>();
			
			lastUpdate = DateTime.UtcNow;
			lastTell = DateTime.UtcNow;
		}

		public void Initialize() {
			Log.Info($"Map is a {info.boundsShape} shape");
			foreach (EntityInstanceInfo spawnInfo in info.entities) {

				EntityInfo entityInfo = entityService.GetEntityInfo(spawnInfo.type);
				if (entityInfo == null) {
					Log.Warning($"Map.Initialize: Initializing map {identity}, no entity of type {{{spawnInfo.type}}} found!");
					continue;
				}

				Log.Verbose($"Map.Initialize: {identity} creating entity for {spawnInfo.type} at T{ spawnInfo.position } R{spawnInfo.rotation} S {spawnInfo.scale}");
				Entity entity = entityService.CreateEntity();
				Guid id = entity.guid;
				
				TRS trs = entity.AddComponent<TRS>();
				trs.position = spawnInfo.position;
				trs.rotation = spawnInfo.rotation;
				trs.scale = spawnInfo.scale;

				OnMap onMap = entity.AddComponent<OnMap>();
				onMap.mapId = name;
				onMap.mapInstanceIndex = instanceIndex;

				foreach (var comp in entityInfo.components) {
					var type = entityService.GetCompType(comp.type);	

					if (type != null) {
						Comp c = entityService.AddComponent(id, type);
						Comp.LoadFromDB(c, comp.data);
					}
				}

				if (entityInfo.global) {
					globalEntities.Add(id);
				} else {
					RequireCell(CellPositionFor(spawnInfo.position)).AddEntity(id);
				}

			}
		}


		private Stopwatch sw = new Stopwatch();
		private Trender deltaTrend = new Trender(TREND_SIZE); 
		const int TELLTIME = 10000;
		int updateCnt = 0;
		private DateTime lastTell;
		/// <summary> Update function, called in server update thread. </summary>
		/// <returns></returns>
		public bool Update() {
			{
				DateTime now = DateTime.UtcNow;
				var diff = now - lastUpdate;
				if (diff.TotalMilliseconds > tickTime) {
					lastUpdate = now;
				} else {
					return false;
				}
				updateCnt++;
				deltaTrend.Record(diff.TotalMilliseconds);

				if ((now - lastTell).TotalMilliseconds > TELLTIME) {
					lastTell = now;
					// Log.Debug($"Map {identity} {updateCnt}x update. Avg delta is {deltaTrend.average} ms");
					updateCnt = 0;
				}
			}

			sw.Start();
			UpdateEntities();
			sw.Stop();
			updateTrend.Record(sw.ElapsedMilliseconds);
			sw.Reset();


			long collideTime = 0;
			foreach (var pair in cells) {
				Cell cell = pair.Value;
				sw.Start();
				CollideCell(cell);
				sw.Stop();
				collideTime += sw.ElapsedMilliseconds;
				sw.Reset();
			}
			collideTrend.Record(collideTime);

			if (!toDespawn.IsEmpty) {
				sw.Start();
				Guid id;
				while (toDespawn.TryDequeue(out id)) {
					OnDespawn(id);
				}
				sw.Stop();
				despawnTrend.Record(sw.ElapsedMilliseconds);
				sw.Reset();
			}

			if (!toSpawn.IsEmpty) {
				sw.Start();
				Guid id;
				while (toSpawn.TryDequeue(out id)) {
					OnSpawn(id);
						
				}
				sw.Stop();
				spawnTrend.Record(sw.ElapsedMilliseconds);
				sw.Reset();
			}
				
			// Just in case
			sw.Reset();

			return true;
		}

		private void UpdateEntities() {
			
			EntityMoveRequest move;
			List<EntityMoveRequest> retry = null;
			while (toMove.TryDequeue(out move)) {
				if (entities.ContainsKey(move.id)) {
					var e = entities[move.id];
					if (e == null) {
						Log.Warning($"No entity {move.id} exists!");
						continue;
					}
					
					var trs = e.RequireComponent<TRS>();
					var client = service.server.GetClient(move.id);

					// move.oldPos is populated with the TRS position at the time of creating the move request.
					// move.newPos is the desired destination
					// Delta only matters if the client is sending the move.
					Vector3 oldPos = trs.position;
					Vector3 oldRot = trs.rotation;

					var delta = oldPos - move.newPos;
					bool posInMap = Contains(info.bounds, info.boundsShape, move.newPos);

					if (posInMap && (move.serverMove || delta.magnitude < speedCap)) {
						// Log.Debug($"Moving {move.id}\n\tposition {move.oldPos} => {move.newPos}\n\trotation {move.oldRot} => {move.newRot}");
						
						trs.position = move.newPos;
						trs.rotation = move.newRot;
						trs.Send();

						var oldCellPos = CellPositionFor(oldPos);
						var newCellPos = CellPositionFor(move.newPos);

						if (oldCellPos != newCellPos) {
							Cell oldCell = GetCell(oldCellPos);
							Cell newCell = RequireCell(newCellPos);
							
							Log.Info($"Entity {move.id} moving from {oldCellPos} => {newCellPos}.");
							oldCell.TransferEntity(newCell, move.id);

						}
						
					} else {
						if (posInMap) {
							Log.Warning($"Entity {move.id} Tried to move too far! {delta.magnitude} vs {speedCap}");
						} else {
							
							Log.Warning($"Entity {move.id} Tried to move outside the map! {move.newPos} vs {info.bounds} / {info.boundsShape}");

						}
						
						if (client != null) {
							client.Call(service.Rubberband, Pack.Base64(move.id), Pack.Base64(oldPos), Pack.Base64(oldRot));
						}
						
					}

				}  else {
					if (toSpawn.Contains(move.id)) {
						Log.Debug($"Map {identity} trying to move entity {move.id}, but it has not been spawned yet. Adding back to queue.");
						
						(retry == null ? (retry = new List<EntityMoveRequest>()) : retry).Add(move);
					} else {
						Log.Warning($"Map {identity} trying to move entity {move.id}, but it does not exist on this map.");
					}
				}
			}

			if (retry != null) {
				foreach (var m in retry) {
					toMove.Enqueue(m);
				}
			}


		}

		private void CollideCell(Cell cell) {
			// TODO: Collision between Spheres and Boxes
		}

		/// <summary> Enqueues an EntityMoveRequest for the given entity to the new position. </summary>
		/// <param name="entityId"> ID of entity to move </param>
		/// <param name="position"> Position to move entity to (null if unchanged) </param>
		/// <param name="rotation"> Rotation to give entity (null if unchanged) </param>
		/// <param name="serverMove"> Did the server initiate the move? True for things such as warping via portal or being pushed by an attack. </param>
		public void Move(Guid entityId, Vector3? position, Vector4? rotation, bool serverMove = false) {
			EntityMoveRequest move = new EntityMoveRequest();
			move.id = entityId;
			TRS trs = entityService.GetComponent<TRS>(entityId);
			if (trs == null) {
				entityService.AddComponent<TRS>(entityId);
				serverMove = true;
			}
			
			move.newPos = position ?? trs.position;
			move.newRot = rotation ?? trs.rotation;
			move.serverMove = serverMove;

			toMove.Enqueue(move);
		}

		/// <summary> Called when a client is added to a map to place them in a cell and subscribe them to information. </summary>
		/// <param name="c"> Client object </param>
		public void EnterMap(Client c) {
			
			if (service.isMaster) {

				Entity entity = entityService[c.id];

				var onMap = entity.GetComponent<OnMap>();
				if (onMap != null && onMap.mapId != null) {
					var oldMap = service.GetMap(onMap.mapId, onMap.mapInstanceIndex);
					if (oldMap != this) {
						Log.Debug($"Exiting {c.identity} from map {oldMap.identity}");
						oldMap.ExitMap(c);
					} else {
						Log.Warning($"Double Map entry of {identity} / {c.identity}");
						return;
					}

				} else if (onMap == null) {
					onMap = entity.AddComponent<OnMap>();
				}


				Log.Debug($"Map {identity} \\jentry of client {c.identity}");
				onMap.mapId = name;
				onMap.mapInstanceIndex = instanceIndex;
				onMap.Send();

				clients.Add(c);

				// entities[c.id] = entity;
				toSpawn.Enqueue(c.id);
			
			}
		}

		/// <summary> Processes a client out of a map, eg unsubscribes a client from all entities in the map </summary>
		/// <param name="c"> client to unsubscribe </param>
		public void ExitMap(Client c) {
			if (service.isMaster) {
				
				foreach (var id in entities.Keys) {
					if (c.id != id) { 
						entityService.Unsubscribe(c, id); 
					}
				}
				Log.Debug($"Map {identity} \\jexit of client {c.identity}");
				
				// Client is finally removed from map during despawn. 
				toDespawn.Enqueue(c.id);
			}
		}
		
		/// <summary> Called when entity is spawned. </summary>
		private void OnSpawn(Guid id) {
			if (service.isMaster) {
				if (entities.ContainsKey(id)) {
					Log.Warning($"Map.OnSpawn: Tried to spawn entity {id} on map {identity}, but the entity is already in that map!");
					return;
				}
				Entity entity = entityService[id];
				entities[id] = entity;
				
				TRS trs = entity.GetComponent<TRS>();
				Client client = service.server.GetClient(id);
				Log.Debug($"Map {identity} \\jspawn of entity {id}");

				if (client != null) {
					foreach (var e in globalEntities) {
						entityService.Subscribe(client, e);
					}
				}

				if (trs != null) {
					Vector3Int cellPos = CellPositionFor(trs.position);
					Cell cell = RequireCell(cellPos);
					cell.AddEntity(id);
				}

			}
		}

		/// <summary> Called when entity is despawned. </summary>
		private void OnDespawn(Guid id) {
			if (service.isMaster) {
				if (!entities.ContainsKey(id)) {
					Log.Warning($"Map.OnDespawn: Tried to remove entity {id} from map {identity}, but the entity is not in that map!");
					return;
				}
				entities.Remove(id);

				TRS trs = entityService.GetComponent<TRS>(id);
				Client client = service.server.GetClient(id);
				Log.Warning($"Map.OnDespawn: Map {identity} \\odespawn of entity {id}");

				if (client != null) {
					foreach (var e in globalEntities) {
						entityService.Unsubscribe(client, e);
					}
				}

				if (trs != null) {
					Vector3Int cellPos = CellPositionFor(trs.position);
					Cell cell = GetCell(cellPos);
					if (cell != null) {
						cell.RemoveEntity(id);
					} else {
						Log.Warning($"Map.OnDespawn: Tried to remove entity {id} from map {identity} cell {cellPos}, but the cell did not exist!");
					}
				} else {
					Log.Warning($"Map.OnDespawn: Probably expected a TRS on entity {id} on map {identity}...");
				}

				// Finally, remove entity id for client if the client is gone.
				if (client != null && client.closed) {
					entityService.Revoke(id);
				}
			}


		}

		/// <summary> Get a snapshot of all entities that have a given component </summary>
		/// <typeparam name="T"> Type of component to search for </typeparam>
		/// <returns> Snapshot IEnumerable of components of the given type for all entities that exist </returns>
		public IEnumerable<T> All<T>() where T : Comp {
			return entityService.GetEntities<T>() as IEnumerable<T>;
		}
		
		/// <summary> Get a <see cref="Cell"/> for the given position. Returns null if it does not exist. </summary>
		/// <param name="cellPosition"> Position of cell to get </param>
		/// <returns> Cell at given position or null </returns>
		public Cell GetCell(Vector3Int cellPosition) {
			if (cells.ContainsKey(cellPosition)) {
				return cells[cellPosition];
			}
			return null;
		}

		/// <summary> Require a <see cref="Cell"/> exists (Create if it doesn't) and get the <see cref="Cell"/> object </summary>
		/// <param name="cellPosition"> Position of cell to get </param>
		/// <returns> Cell at given position </returns>
		public Cell RequireCell(Vector3Int cellPosition) {
			if (cells.ContainsKey(cellPosition)) {
				return cells[cellPosition];
			}
			Cell cell = new Cell(this, cellPosition);
			cells[cellPosition] = cell;
			return cell;
		}
		
		/// <summary> Gets the coordinate of the cell that <paramref name="position"/> belongs to. </summary>
		/// <param name="position"> Position in worldspace </param>
		/// <returns> Coordinate of <paramref name="position"/> in cell space </returns>
		public Vector3Int CellPositionFor(Vector3 position) { return CellPositionFor(position, cellSize, is3d); }
		/// <summary> Gets the coordinate of the cell that <paramref name="position"/> belongs to, given a specific cellSize </summary>
		/// <param name="position"> Position in worldspace </param>
		/// <param name="cellSize"> Size of cells </param>
		/// <returns> Position of <paramref name="position"/> in cell space </returns>
		public static Vector3Int CellPositionFor(Vector3 position, float cellSize, bool is3d = false) {
			float halfSize = cellSize / 2f;
			Vector3 cell = position;
			if (!is3d) { cell.y = 0; }
			cell += Vector3.one * halfSize;
			cell /= cellSize;
			return Vector3Int.FloorToInt(cell);
		}

	}


}
#endif
