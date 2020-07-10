#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace Ex.Utils {

	public class Random {
	
		public static SRNG rng = new SRNG();

		public static float Range(float min, float max) { return rng.NextFloat(min, max); }

		public static int Range(int min, int max) { return rng.NextInt(min, max); }

		public static float value { get { return rng.NextFloat(); } }
		
		public static Vector3 onUnitSphere {
			get { return new Vector3(.5f-value, .5f-value, .5f-value).normalized;  }
		}
		public static Vector2 insideUnitCircle {
			get { return new Vector2(.5f-value, .5f-value).normalized * value; }
		}
		public static Vector2 insideUnitSphere {
			get { return new Vector3(.5f-value, .5f-value, .5f-value).normalized * value; }
		}

	}

	 
}
