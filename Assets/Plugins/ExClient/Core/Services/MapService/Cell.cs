#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;

#if !UNITY
namespace Ex {
	/// <summary> Support class for <see cref="Map"/> to divide a map into cells. </summary>
	public class Cell {
		/// <summary> Entities in the cell </summary>
		public List<Guid> entities { get; private set; }
		/// <summary> Clients connected to the cell </summary>
		public List<Client> clients { get; private set; }
		/// <summary> Cached cell visibility. Won't change during a cell's lifetime. </summary>
		private List<Vector3Int> _visibility = null;
		/// <summary> All visible Cells from this Cell </summary>
		public IEnumerable<Vector3Int> visibility {
			get {
				return (_visibility == null)
				  ? (_visibility = (map.is3d ? Visibility3d(map.cellDist) : Visibility2d(map.cellDist)))
				  : _visibility;
			}
		}
		/// <summary> Cell position in the given map </summary>
		public Vector3Int cellPos { get; private set; }
		/// <summary> Map this cell belongs to </summary>
		public Map map { get; private set; }
		
		public Cell(Map map, Vector3Int cellPos) {
			this.map = map;
			this.cellPos = cellPos;
			entities = new List<Guid>();
			clients = new List<Client>();
		}

		public void AddEntity(Guid id) {
			if (!entities.Contains(id)) {

				entities.Add(id);
				Client c = map.service.server.GetClient(id);
				if (c != null) {
					clients.Add(c);
				}

				foreach (var cellPos in visibility) {
					Cell cell = map.GetCell(cellPos);
					if (cell != null) {
						foreach (var client in cell.clients) {
							if (client.id == id) { continue; }
							map.entityService.Subscribe(client, id);
						}
						if (c != null) {
							foreach (var entityId in cell.entities) {
								if (entityId == id) { continue; }
								map.entityService.Subscribe(c, entityId);
							}
						}
					}
				}

			} else {
				Log.Warning($"Cell.AddEntity: Map {map.identity} cell {cellPos} already contains entity {id}");
			}

		}

		public void RemoveEntity(Guid id) {
			if (entities.Contains(id)) {

				entities.Remove(id);
				Client c = map.service.server.GetClient(id);
				if (c != null) {
					clients.Remove(c);
				}

				foreach (var cellPos in visibility) {
					Cell cell = map.GetCell(cellPos);
					if (cell != null) {

						foreach (var client in cell.clients) {
							if (client.id == id) { continue; }
							map.entityService.Unsubscribe(client, id);
						}
						if (c != null) {
							foreach (var entityId in cell.entities) {
								if (entityId == id) { continue; }
								map.entityService.Unsubscribe(c, entityId);
							}
						}
					}
				}
		

			} else {
				Log.Warning($"Cell.RemoveEntity: Map {map.identity} cell {cellPos} does not contains entity {id}");
			}
		}

		/// <summary> Transfer an entity by <paramref name="id"/> from this cell to <paramref name="other"/>.
		/// Unsubscribes clients that no longer see the entity, and subsribes new clients that see it.
		/// If a client belongs to the entity, that client is included. </summary>
		/// <param name="other"> Cell to transfer to </param>
		/// <param name="id"> Id of entity to transfer </param>
		public void TransferEntity(Cell other, Guid id) {
			if (entities.Contains(id) && !other.entities.Contains(id)) {
				entities.Remove(id);
				other.entities.Add(id);
				Client c = map.service.server.GetClient(id);
				if (c != null) {
					clients.Remove(c);
					other.clients.Add(c);
				}

				foreach (var missingPos in MissingVisibility(other)) {
					var cell = map.GetCell(missingPos);
					if (cell != null) {
						foreach (var client in cell.clients) { if (client == c) { continue; } map.entityService.Unsubscribe(client, id); } 
						if (c != null) {
							foreach (var entity in cell.entities) { if (entity == id) { continue; } map.entityService.Unsubscribe(c, entity); }
						}
					}
				}

				foreach (var newPos in other.MissingVisibility(this)) {
					var cell = map.GetCell(newPos);
					if (cell != null) {
						foreach (var client in cell.clients) { if (client == c) { continue; } map.entityService.Subscribe(client, id); }
						if (c != null) {
							foreach (var entity in cell.entities) { if (entity == id) { continue; } map.entityService.Subscribe(c, entity); }
						}
					}
				}

			} else {
				Log.Warning($"Cell.TransferEntity: Map {map.identity} cell {cellPos} and {other.cellPos} cannot transfer entity {id}");
			}
		}

		/// <summary> Gets the shared visibility between two cells </summary>
		/// <param name="other"> Other cell </param>
		/// <returns> Shared visibilities between two cells </returns>
		public IEnumerable<Vector3Int> CommonVisibility(Cell other) {
			var otherVis = other.visibility;
			return visibility.Where( (it) => { return otherVis.Contains(it); });
		}

		/// <summary> Gets the visibility that this cell has, but not the other. </summary>
		/// <param name="other"> Other Cell </param>
		/// <returns> Visibilities from this cell, but not the other. </returns>
		public IEnumerable<Vector3Int> MissingVisibility(Cell other) {
			var otherVis = other.visibility;
			return visibility.Where( (it) => { return !otherVis.Contains(it); }); 
		}

		/// <summary> Provides a list of 2d cell neighbors for a given <paramref name="position"/> and <paramref name="maxDist"/> </summary>
		/// <param name="position"> Center position to get neighbors for </param>
		/// <param name="maxDist"> Radius of valid neighbors, in cell distance. Defaults to 1, which is enough for a 3x3 grid around the cell. </param>
		/// <returns> Collection of neighbors </returns>
		public List<Vector3Int> Visibility2d(int maxDist = 1) {
			List<Vector3Int> vis = new List<Vector3Int>();
			maxDist = Mathf.Abs(maxDist);
			Vector3Int center = cellPos;

			for (int z = -maxDist; z <= maxDist; z++) {
				for (int x = -maxDist; x <= maxDist; x++) {
					vis.Add(center + new Vector3Int(x, 0, z));
				}
			}

			return vis;
		}

		/// <summary> Provides a list of 3d cell neighbors for a given <paramref name="position"/> and <paramref name="maxDist"/> </summary>
		/// <param name="position"> Center position to get neighbors for </param>
		/// <param name="maxDist"> Radius of valid neighbors, in cell distance. Defaults to 1, which is enough for a 3x3x3 grid around the cell. </param>
		/// <returns> Collection of neighbors </returns>
		public List<Vector3Int> Visibility3d(int maxDist = 1) {
			List<Vector3Int> vis = new List<Vector3Int>();
			maxDist = Mathf.Abs(maxDist);
			Vector3Int center = cellPos;

			for (int z = -maxDist; z < maxDist; z++) {
				for (int y = -maxDist; y < maxDist; y++) {
					for (int x = -maxDist; x < maxDist; x++) {
						vis.Add(center + new Vector3Int(x, y, z));
					}
				}
			}

			return vis;
		}

	}
}

#endif	
