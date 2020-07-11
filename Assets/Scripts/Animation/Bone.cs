using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Bone : MonoBehaviour {

	public string kind;
	public Vector3 pitch;
	public Vector3 yaw;
	public Vector3 roll;

	public LookTarget lookTarget {
		get {
			LookTarget lt;
			lt.target = transform;
			lt.pitch = pitch;
			lt.yaw = yaw;
			lt.roll = roll;
			return lt;
		}
	}
	
}

[System.Serializable]
public struct LookTarget {
	public Transform target;
	public Vector3 pitch;
	public Vector3 yaw;
	public Vector3 roll;
	public Quaternion Rotate(Vector3 euler) {
		float p = euler.x;
		float y = euler.y;
		float r = euler.z;
		Vector3 actual = new Vector3(
			pitch.x * p + yaw.x * y + roll.x * r,
			pitch.y * p + yaw.y * y + roll.y * r,
			pitch.z * p + yaw.z * y + roll.z * r
		);

		return Quaternion.Euler(actual);
	}
}
