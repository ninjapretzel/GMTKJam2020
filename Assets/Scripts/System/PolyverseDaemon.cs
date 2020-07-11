using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PolyverseDaemon : MonoBehaviour {

	public static PolyverseDaemon main { get; private set; }

	public FollowCam cam;
	public FollowCam focus;
	public PlayerControl player;

	void OnLink(ExPlayerLink playerLink) {
		cam.target = focus.transform;
		focus.target = playerLink.transform;

		player = playerLink.gameObject.AddComponent<PlayerControl>();
		player.gameObject.AddComponent<CharacterController>();
		player.moveRoot = focus.transform;
		player.IS_PLAYER = true;
		player.USE_TEST_CONTROLS = true;
		

		cam.enabled = focus.enabled = player.enabled = false;
	}

	void OnLogin() {
		cam.enabled = focus.enabled = player.enabled = true;
	}


	void Awake() {
		if (main != null) { Destroy(gameObject); return; }
		
		main = this;
		DontDestroyOnLoad(gameObject);
		ExEntityLink.OnPlayerLinked += OnLink;
		ExEntityLink.OnPlayerLoggedIn += OnLogin;
	}
	
	void Start() {
		
	}
	
	void Update() {
		
	}
	
}
