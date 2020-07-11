using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BoneAdjust : MonoBehaviour {

	public Transform bone;
	public Transform[] guides;
	
	public void AdjustBones(string name) {
		foreach (Transform guide in guides) {
			if (guide.name == "Guide_" + name) {

				bone.localPosition = guide.localPosition;
				bone.localRotation = guide.localRotation;
				bone.localScale = guide.localScale;
				return;
			}
		}
	}

	void Awake() {
		
	}
	
	void Start() {
		
	}
	
	void Update() {
		
	}
	
}
