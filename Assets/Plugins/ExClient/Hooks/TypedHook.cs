using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LevelUpper.Extensions;

namespace ExClient {

	public class TypedHook : MonoBehaviour {
		public string type;

		[ExEntityLink.AutoRegisterChange]
		public static void OnDisplayChanged(Ex.Typed comp, ExEntityLink link) {
			TypedHook hook = link.Require<TypedHook>();

			if (hook.type == null || hook.type != comp.type) {
				link.gameObject.name = $"{comp.type}:{link.id}";
				hook.type = comp.type;
			}

		}

		[ExEntityLink.AutoRegisterRemove]
		public static void OnDisplayRemoved(Ex.Typed display, ExEntityLink link) {
			TypedHook hook = link.GetComponent<TypedHook>();
			if (hook != null) {
				if (hook.type != null) {
					link.gameObject.name = "" + link.id;
				}
				DisplayHook.Destroy(hook);
			}
		}

	}
}
