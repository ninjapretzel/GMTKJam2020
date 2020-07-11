using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Speen : MonoBehaviour {
	public Vector3 speeds = new Vector3(0, 60, 0);

	void Update() {
		transform.Rotate(speeds * Time.deltaTime);
	}
	
}
