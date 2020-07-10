#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;

namespace Ex {


#if !UNITY
	/// <summary> Shape for the bounds of a map </summary>
	public enum BoundsShape { 
		/// <summary> Axis Aligned Bounding Box, or hard bounding on all XYZ. </summary>
		Box, 
		/// <summary> Y-Axis Aligned Bounding Cylinder, or distance bounding on XZ (Radius of X), hard bounding on Y. </summary>
		Cylinder, 
		/// <summary> Bounding Sphere (Radius of X), or distance bounding on XYZ. </summary>
		Sphere, 
	}
	/// <summary> DB info for data about a map, used to create and make decisions about a map instance </summary>
	[BsonIgnoreExtraElements] 
	public class MapInfo : DBEntry {

		/// <summary> Display name </summary>
		public string name { get; set; }

		/// <summary> Is this map instanced? </summary>
		[BsonDefaultValue(false)]
		public bool instanced { get; set; }
		/// <summary> Default number of instances to create, if this map is instanced. </summary>
		[BsonDefaultValue(1)]
		public int numInstances { get; set; }
		
		/// <summary> Is this map 3d? </summary>
		[BsonDefaultValue(false)]
		public bool is3d { get; set; }
		/// <summary> Does this map show other players's entities? </summary>
		[BsonDefaultValue(false)]
		public bool solo { get; set; }
		/// <summary> Does this map destroy cells (and entities) when not viewed by a client? </summary>
		[BsonDefaultValue(true)]
		public bool sparse { get; set; }
		
		/// <summary> Axes of map to use if 2d. Used on client to hint at orientation if not provided, treated as "xz" </summary>
		[BsonIgnoreIfNull]
		public string axes { get; set; }
		/// <summary> Size of cells in arbitrary units. </summary>
		[BsonDefaultValue(10)]
		public float cellSize { get; set; }
		/// <summary> Radius of visible surrounding cells in arbitrary units. </summary>
		[BsonDefaultValue(2)]
		public int cellDist { get; set; }
		
		/// <summary> Bounds of map space in arbitrary units. If bounds is zero'd, map is infinite. </summary>
		[BsonIgnoreIfNull]
		public Bounds bounds { get; set; }
		[BsonIgnoreIfNull]
		public BoundsShape boundsShape { get; set; }

		

		/// <summary> Entities in the map </summary>
		[BsonIgnoreIfNull]
		public EntityInstanceInfo[] entities { get; set; }


	}
	
	/// <summary> Entity information. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInfo : DBEntry {
		/// <summary> Kind of entity </summary>
		public string type { get; set; }
		/// <summary> Source filename. </summary>
		[BsonIgnoreIfNull]
		public string filename{ get; set; } = "";
		/// <summary> Is this a global (map-wide) entity? </summary>
		[BsonIgnoreIfNull] 
		public bool global { get; set; } = false;
		/// <summary> Information about <see cref="Comp"/>s attached to entity </summary>
		public ComponentInfo[] components;
	}

	/// <summary> Information about <see cref="Comp"/>s attached to an entity </summary>
	[BsonIgnoreExtraElements]
	public class ComponentInfo {
		/// <summary> name of type of component </summary>
		public string type { get; set; }
		/// <summary> Arbitrary Data to merge into component </summary>
		public BsonDocument data { get; set; }
	}

	/// <summary> <see cref="MapInfo"/> embedded entity information. 
	/// These are spawned into entities when the map starts. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInstanceInfo {
		/// <summary> Location of Entity </summary>
		public Vector3 position { get; set; }
		/// <summary> Rotation of Entity </summary>
		public Vector3 rotation { get; set; }
		/// <summary> Scale of Entity </summary>
		public Vector3 scale { get; set; }
		/// <summary> ID of entity <see cref="EntityInfo"/> object in database </summary>
		public string type { get; set; }
	}
	
	
#endif


	/// <summary> Request to move an entity to a position </summary>
	/// <remarks> 
	/// Server uses these internally, but also recieves them from clients.
	/// <see cref="serverMove"/> is set false on any that come in from a client, 
	/// so even if a client tries to pretend they are the server, it should not work.
	/// </remarks>
	public struct EntityMoveRequest {
		/// <summary> ID of entity to move </summary>
		public Guid id;
		/// <summary> Assumed old position of the entity </summary>
		public Vector3 oldPos;
		/// <summary> new position of the entity </summary>
		public Vector3 newPos;
		/// <summary> Assumed old rotation of the entity </summary>
		public Vector3 oldRot;
		/// <summary> new rotation of the entity </summary>
		public Vector3 newRot;
		/// <summary> true if the server is instigating this movement, false if it is a client </summary>
		public bool serverMove;
	}
	


}


