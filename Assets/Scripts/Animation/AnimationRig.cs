using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AnimationRig : MonoBehaviour {

	public AnimatorData data = new AnimatorData();

	public void Apply(AnimatorData data) {
		this.data = data;
	}

	public virtual void Reinitialize() { }
}
