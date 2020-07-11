using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Ex;
using Ex.Utils;

public class ExPlayerLink : MonoBehaviour {

	public ExEntityLink.ExEntityLinkService service;
	private MapService mservice { get { return service.GetService<MapService>(); }}

	public float distanceThreshold = .2f;
	public float angleThreshold = 3f;

	float timer = 0;

	Vector3 lastPos;
	Quaternion lastRot;

	void Update() {
		if (service == null) { return; }

		ExEntityLink link = GetComponent<ExEntityLink>();
		timer += Time.unscaledDeltaTime;
		if (timer >= service.server.tickRate / 1000.0f) {
			timer -= service.server.tickRate / 1000.0f;
			
			Vector3 pos = transform.position;
			Quaternion rot = transform.rotation;
			
			
			if (link != null && ((pos - lastPos).magnitude > distanceThreshold) || (Quaternion.Angle(lastRot, rot) > angleThreshold)) {
				service.server.localClient.Call(mservice.RequestMove, Pack.Base64(link.id), Pack.Base64(pos), Pack.Base64(rot.eulerAngles));
				lastRot = transform.rotation;
				lastPos = pos;
			}

		}
	}
	
}
