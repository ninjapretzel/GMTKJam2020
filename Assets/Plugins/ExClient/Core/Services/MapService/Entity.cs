#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif

using System;

namespace Ex {

	/// <summary> An Entity is just a name/id, used to look up Components that are attached </summary>
	public class Entity {
		/// <summary> ID of this entity </summary>
		public Guid guid { get; private set; }
		/// <summary> EntityService this entity belongs to </summary>
		public EntityService service { get; private set; }
		/// <summary> Constructor for creating a new Entity identity </summary>
		/// <remarks> Internal, not intended to be used outside of EntityService. </remarks>
		internal Entity(EntityService service) {
			this.service = service;
			guid = Guid.NewGuid();
		}

		/// <summary> Constructor for wrapping an existing ID with an Entity </summary>
		/// <remarks> Internal, not intended to be used outside of EntityService. </remarks>
		internal Entity(EntityService service, Guid id) {
			this.service = service;
			guid = id;
		}

		/// <summary> Adds a component to this entity. </summary>
		/// <typeparam name="T"> Generic type of component to add </typeparam>
		/// <returns> Component of type T that was added </returns>
		public T AddComponent<T>() where T : Comp { return service.AddComponent<T>(guid); }
		/// <summary> Gets another component associated with this entity </summary>
		/// <typeparam name="T"> Generic type of component to get </typeparam>
		/// <returns> Component of type T that is on this entity, or null if none exists </returns>
		public T GetComponent<T>() where T : Comp { return service.GetComponent<T>(guid); }
		/// <summary> Checks for a component associated with this entity, returns it or creates a new one if it does not exist </summary>
		/// <typeparam name="T"> Generic type of component to get </typeparam>
		/// <returns> Component of type T that is on this entity, or was just added </returns>
		public T RequireComponent<T>() where T : Comp { return service.RequireComponent<T>(guid); }
		/// <summary> Removes a component associated with this entity. </summary>
		/// <typeparam name="T"> Generic type of component to remove </typeparam>
		/// <returns> True if a component was removed, otherwise false. </returns>
		public bool RemoveComponent<T>() where T : Comp { return service.RemoveComponent<T>(guid); }

		/// <summary> Coercion from Entity to Guid, since they are the same information. </summary>
		public static implicit operator Guid(Entity e) { 
			if (e == null) { return Guid.Empty; }
			return e.guid; 
		}
	}
}
