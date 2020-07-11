using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LevelUpper.Extensions;

using static ExClient.Util.Res;

namespace ExClient {

	public class DisplayHook : MonoBehaviour {
		public string prefab;
		public Transform child;

		[ExEntityLink.AutoRegisterChange]
		public static void OnDisplayChanged(Ex.Display display, ExEntityLink link) {
			DisplayHook hook = link.Require<DisplayHook>();

			if (hook.child == null || hook.prefab != display.prefab) {
				if (hook.child != null) {
					GameObject.Destroy(hook.child.gameObject);
				}

				hook.prefab = display.prefab;
				Transform prefab = SafeLoad<Transform>(display.prefab, "Models/Error");
				Transform copy = GameObject.Instantiate(prefab, link.transform);
				hook.child = copy;
			}

			hook.child.localPosition = display.position;
			hook.child.localRotation = Quaternion.Euler(display.rotation);
		}

		[ExEntityLink.AutoRegisterRemove]
		public static void OnDisplayRemoved(Ex.Display display, ExEntityLink link) {
			DisplayHook hook = link.GetComponent<DisplayHook>();
			if (hook != null) {
				if (hook.child != null) {
					GameObject.Destroy(hook.child.gameObject);
				}
				DisplayHook.Destroy(hook);
			}
		}

	}
}
