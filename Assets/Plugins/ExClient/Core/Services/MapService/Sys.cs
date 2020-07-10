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
	/// <summary> Base class for systems. </summary>
	public abstract class Sys {
		/// <summary> Connected EntityService </summary>
		public EntityService service { get; private set; }

		/// <summary> Binds this system to an EntityService </summary>
		/// <param name="service"> Service to bind to </param>
		/// <param
		public void Bind(EntityService service, Type[] types, Delegate callback) {
			if (service != null) {
				throw new InvalidOperationException($"System of {GetType()} has already been bound.");
			}
			this.service = service;
		}

	}
}
