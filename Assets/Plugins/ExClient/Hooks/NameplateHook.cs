using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Ex;
using LevelUpper.Extensions;
using TMPro;

namespace ExClient {
	public class NameplateHook : MonoBehaviour {

		public string nname;
		public TextMeshPro name3d;

		[ExEntityLink.AutoRegisterChange]
		public static void OnNameplateChanged(Nameplate nameplate, ExEntityLink link) {
			NameplateHook hook = link.Require<NameplateHook>();

			if (hook.name3d == null) {
				var prefab = Resources.Load<TextMeshPro>("Nameplate");
				hook.name3d = Instantiate(prefab, hook.transform.position, hook.transform.rotation);
				hook.name3d.transform.SetParent(hook.transform);

			}

			if (hook.nname != nameplate.name) {
				hook.nname = nameplate.name;
				hook.name3d.text = hook.nname;

			}

		}

		[ExEntityLink.AutoRegisterRemove]
		public static void OnNameplateRemoved(Nameplate nameplate, ExEntityLink link) {
			NameplateHook hook = link.GetComponent<NameplateHook>();
			if (hook != null) {
				NameplateHook.Destroy(hook);
			}
		}

	}


}
