#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;
using System.Runtime.CompilerServices;

namespace Ex {
	/// <summary> Service which creates and manages Map instances </summary>
	public class MapService : Service {
		
		/// <summary> Struct for sending a message to rubberband a client entity. </summary>
		public struct RubberBand_Client {
			/// <summary> ID of entity to move </summary>
			public Guid id;
			/// <summary> Position to move to </summary>
			public Vector3 pos;
			/// <summary> Rotation to move to </summary>
			public Vector3 rot;
		}

		/// <summary> Struct for sending a message to change the map a client thinks it is on.  </summary>
		public struct MapChange_Client {
			/// <summary> Name of map </summary>
			public string mapName;
			/// <summary> Instance of map </summary>
			public int? mapInstanceIndex;
			/// <summary> New position on map </summary>
			public Vector3 pos;
			/// <summary> New rotation on map </summary>
			public Vector3 rot;

		}

		#if !UNITY
		
		/// <summary> Connected DBService </summary>
		public DBService db;
		/// <summary> Connected EntityService </summary>
		public EntityService entityService;

		/// <summary> Cached MapInfo from database </summary>
		public ConcurrentDictionary<string, MapInfo> mapInfoByName;

		/// <summary> All map instances </summary>
		public ConcurrentDictionary<string, List<Guid>> instances;

		/// <summary> Live map instances </summary>
		public ConcurrentDictionary<Guid, Map> maps;

		/// <summary> Work pool for map instances. </summary>
		public WorkPool<Map> mapWorkPool;

		public override void OnStart() {
			db = GetService<DBService>();
			entityService = GetService<EntityService>();
		}
		#endif

		public override void OnEnable() {
			#if !UNITY
			if (isMaster) {
				mapInfoByName = new ConcurrentDictionary<string, MapInfo>();
				instances = new ConcurrentDictionary<string, List<Guid>>();
				maps = new ConcurrentDictionary<Guid, Map>();
				mapWorkPool = new WorkPool<Map>(UpdateMap
				//	, null, 5, 100
				);
			}
			#endif

		}

		public override void OnDisable() {
			#if !UNITY
			if (isMaster) {
				mapWorkPool.Finish();
			}
			#endif

		}

		/// <summary> RPC, Client -> Server. Requests the client's entity get moved </summary>
		/// <param name="msg"> RPC Message info </param>
		public void RequestMove(RPCMessage msg) {
#if !UNITY
			if (isMaster) {
				// Id, position, rotation 
				if (msg.numArgs != 3) { return; }
				Guid id = Unpack.Base64<Guid>(msg[0]);
				Vector3 pos = Unpack.Base64<Vector3>(msg[1]);
				Vector3 rot = Unpack.Base64<Vector3>(msg[2]);
				Log.Debug($"Getting move request for {id} / {pos} / {rot}");

				// Check that requester owns the entity they are requesting to move
				// right now, just a direct id check 
				if (id != msg.sender.id) { return; }

				Entity entity = entityService[id];
				if (entity != null) {
					OnMap onMap = entity.GetComponent<OnMap>();
					if (onMap != null) {
						Map map = GetMap(onMap.mapId, onMap.mapInstanceIndex);

						Log.Debug($"Forwarding move request to map {onMap.mapId} for {id} / {pos} / {rot}");
						map?.Move(id, pos, rot, false);

					}
				}
			}
#endif
		}

		/// <summary> RPC, Server -> Client. Asks the client to reposition. </summary>
		/// <param name="msg"> RPC Message info </param>
		public void Rubberband(RPCMessage msg) {
			if (isSlave) {
				if (msg.numArgs != 3) { return; }
				RubberBand_Client rubberBand;
				rubberBand.id = Unpack.Base64<Guid>(msg[0]);
				rubberBand.pos = Unpack.Base64<Vector3>(msg[1]);
				rubberBand.rot = Unpack.Base64<Vector3>(msg[2]);

				server.On(rubberBand);
			}
		}

		/// <summary> Informational RPC, Server -> Client. Gives the user data of what map/instance they are in. </summary>
		/// <param name="msg"> RPC Message info </param>
		public void SetMap(RPCMessage msg) {
			if (isSlave) {
				if (msg.numArgs < 3) { return; }
				MapChange_Client mapChange;
				mapChange.mapName = msg[0];
				mapChange.mapInstanceIndex = null;
				mapChange.pos = Unpack.Base64<Vector3>(msg[1]);
				mapChange.rot = Unpack.Base64<Vector3>(msg[2]);
				
				if (msg.numArgs >= 4) {
					int v;
					if (int.TryParse(msg[3], out v)) {
						mapChange.mapInstanceIndex = v;
					}
				}

				server.On(mapChange);
			}
		}

#if !UNITY
		public void UpdateMap(Map map) {
			if (map.clients.Count > 0) {
				map.Update();
			}
		}
		public Map GetMap(string map, int? mapInstanceIndex = null) {

			//Log.Debug($"Loading map {map}");
			// Load the map from db, or the limbo map if it doesn't exist.
			if (!mapInfoByName.ContainsKey(map)) {
				var loadedMap = db.Get<MapInfo>("Content", "name", map) ?? db.Get<MapInfo>("Content", "name", "Limbo");
				mapInfoByName[map] = loadedMap;
				string s = loadedMap?.ToString() ?? "NULL";
				Log.Debug($"Loaded MapInfo for {{{map}}}");
			}

			// A requested map may have been routed to limbo if it does not exist.
			MapInfo info = mapInfoByName[map];
			string mapName = info.name;

			// First time? initialize instances collection of map 
			if (!instances.ContainsKey(mapName)) {
				SpinUp(info);
			}
			List<Guid> instanceIds = instances[mapName];
			
			if (mapInstanceIndex == null) {
				return maps[instanceIds[0]];
			}

			int ind = mapInstanceIndex.Value % instanceIds.Count;
			if (ind < 0) { ind *= -1; }
			var id = instanceIds[ind];
			
			return maps[id];
			
		}

		/// <summary> Initialize default set of Map instances for a MapInfo </summary>
		/// <param name="info"> Info to initialize </param>
		private void SpinUp(MapInfo info) {
			Log.Info($"Initializing MapInfo for {info.name}. {(info.instanced ? info.numInstances : 1)} instances");
			if (info.instanced) {
				for (int i = 0; i < info.numInstances; i++) {
					Initialize(info, i);
				}

			} else {
				Initialize(info);
			}
		}
		
		/// <summary> Initializes a single instance from a <see cref="MapInfo"/>. </summary>
		/// <param name="info"> Data to use to initialize map instance </param>
		/// <returns> Newly created map instance</returns>
		private Map Initialize(MapInfo info, int? instanceIndex = null) {
			Map map = new Map(this, info, instanceIndex);
			map.Initialize();

			maps[map.id] = map;
			List<Guid> instanceIds = instances.ContainsKey(info.name) 
				? instances[info.name] 
				: (instances[info.name] = new List<Guid>());

			instanceIds.Add(map.id);
			mapWorkPool.Add(map);

			return map;
		}
		#endif

		// Server-side logic
#if !UNITY
		/// <summary> Server Command, used when transferring a client into a map. </summary>
		/// <param name="client"> Client connection object </param>
		/// <param name="mapId"> ID to put client into </param>
		public void EnterMap(Client client, string mapId, Vector3 position, Vector3 rotation, int? mapInstanceIndex = null) {
			
			Log.Info($"\\jClient {client.identity} entering map {mapId} ");
			var map = GetMap(mapId, mapInstanceIndex);

			Log.Info($"\\jGot map { map.identity }");
			map.EnterMap(client);
			
			map.Move(client.id, position, rotation, true);
			
			// Call RPC for initializing clients
			if (mapInstanceIndex != null) {
				client.Call(SetMap, mapId, Pack.Base64(position), Pack.Base64(rotation), mapInstanceIndex.Value);
			} else {
				client.Call(SetMap, mapId, Pack.Base64(position), Pack.Base64(rotation));
			}
			
			
		}
			


		/// <summary> Server command, used when removing a client from any maps </summary>
		/// <param name="client"> Client connection object to remove to remove </param>
		public void ExitMap(Client client, string mapId, int mapInstanceIndex) {
			Map map = GetMap(mapId, mapInstanceIndex);
			map.ExitMap(client);
		}

#endif

	}

	
}
