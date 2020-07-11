using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using static GGUI;
using TMPro;

public class PrimaryGUI : GGUIBehaviour {

	ExDaemon exDaemon;
	float timer = 0;


	public override void OnEnable() {
		base.OnEnable();
		//gameObject.AddComponent<ChatPanel>();
		//gameObject.AddComponent<ChatLog>().Title("Chat").Filter("global").Area(unitRect.BottomLeft(.3f, .3f));
		//gameObject.AddComponent<ChatLog>().Title("System").Filter("system").Area(unitRect.BottomRight(.3f, .3f));
	}
	public override void OnDisable() {
		base.OnDisable();

	}
	public void Awake() {
		exDaemon = GetComponent<ExDaemon>();

	}

	public override void Update() {
		base.Update();

		timer += Time.deltaTime;
	}

	public void OnExConnected() {
		Debug.Log("Got OnExConnected event");
		rebuildOnUpdate = true;
	}

	public override void RenderGUI() {
		LoadSkin("Tech");





	}
}
