using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LevelUpper.Extensions;
using System.Linq;


[Serializable][Flags]
public enum TKeys : long {
	None = 0,
	Fire = 1 << 0,
	AltFire = 1 << 1,
	Throw = 1 << 2,

	Crouch = 1 << 6,
	Jump = 1 << 7,
	Sprint = 1 << 8,

	Use = 1 << 9,
	Zoom = 1 << 10,

	Action1 = 1 << 11,
	Action2 = 1 << 12,
	Action3 = 1 << 13,
	Action4 = 1 << 14,
	Action5 = 1 << 15,
	Action6 = 1 << 16,
	Action7 = 1 << 17,
	Action8 = 1 << 18,
	Action9 = 1 << 19,
	Action0 = 1 << 20,

	SwitchWeapons = 1 << 61,
	ChangeSides = 1 << 62,
}

public class AnimatorData {
	public Dictionary<string, float> floats = new Dictionary<string, float>();
	public Dictionary<string, bool> bools = new Dictionary<string, bool>();
	public void SetTo(Animator animator) {
		foreach (var pair in floats) { animator.SetFloat(pair.Key, pair.Value); }
		foreach (var pair in bools) { animator.SetBool(pair.Key, pair.Value); }
	}
	public void Record(string key, float value) { floats[key] = value; }
	public void Record(string key, bool value) { bools[key] = value; }
	private object o;
	public T Get<T>(string key) {
		if (typeof(T) == typeof(float) && floats.ContainsKey(key)) {
			o = floats[key];
		} else if (typeof(T) == typeof(bool) && bools.ContainsKey(key)) {
			o = bools[key];
		} else {
			o = default(T);
		}
		return (T)o;
	}
}

public static class TKeysHelpers {
	public static bool Has(this TKeys compare, TKeys key) {
		return (key & compare) != TKeys.None;
	}
	public static bool Pressed(this TKeys now, TKeys key, TKeys last) {
		return !last.Has(key) && now.Has(key);
	}
	public static bool Released(this TKeys now, TKeys key, TKeys last) {
		return last.Has(key) && !now.Has(key);
	}
}



public class PlayerControl : MonoBehaviour {

	public bool USE_TEST_CONTROLS = false;
	public bool IS_PLAYER = false;
	public Transform moveRoot;

	// Reactivity.
	public float responseBase = 250f;
	public float mass = 30;
	public float speed = 5;
	public Vector2 mouseSensitivity = new Vector2(11, 6);


	public float baseRotorSpeed = 440;
	public float moveRotorSpeed = 1080;
	public float rotorSpeedResponse = 5f;
	public float hoverResponse = 5f;
	public float hoverDistance = 6.0f;
	public float minHoverDistance = 1.0f;
	public float slopeHeightBias = 5.0f;


	[Tooltip("x - backwards, y - neutral, z- forwards")]
	public Vector3 pitchTargets = new Vector3(-33, 0, 33);
	[Tooltip("x - leftward, y - neutral, z- rightwards")]
	public Vector3 rollTargets = new Vector3(60, 0, -60);

	public float pitchResponse = 5f;
	public float rollResponse = 5f;
	public float rotationResponse = 5f;


	public Transform[] rotors;
	public Transform body;
	public Transform leftWing;
	public Transform rightWing;



	public float gravity = 9;
	public float jumpForce = 7;
	public float terminalVelocity = 20;
	public float snapDistance = .1f;
	public float lookDampening = 5f;

	public float doubleTapTimeThreshold = .05f;
	public float tapThreshold = .5f * .5f;

	public float sprintPower = 2.0f;

	public float sideSpeedRatio = .5f;
	public float animResponse = 5;
	public bool snapToGround = false;

	public Func<Vector3> moveInputFunc;
	public Func<Vector3> aimInputFunc;
	public Func<TKeys> keysInputFunc;
	Action doNextFrame = null;

	public BoneAdjust[] weaponAdjustments;
	public Transform[] weapons;

	public Transform[] guides;
	public Transform alternateGuideSource;
	public Transform muzzleflash;
	public Transform marker;



	Vector3 velocity;
	float pitch;
	float yaw;
	float roll;

	float rotorSpeed;
	float lastMoveTime = 100;

	Vector3 moveDir;
	Vector3 lastMoveDir;
	Vector3 lastInput;
	Vector3 animMoveXYZ;
	Animator animator;
	AnimatorData aniData;
	CharacterController controller;
	Transform head;

	/// <summary> Preallocated raycast hits for ground check </summary>
	private RaycastHit[] hits = new RaycastHit[16];

	long _lastKeys = (long)TKeys.None;
	TKeys lastKeys { get { return (TKeys)_lastKeys; } set { _lastKeys = (long)value; } }
	int currentWeaponKind = 0;

	void Awake() {
		snapToGround = gameObject.activeSelf;
		GrabWeaponGuides();
		
		

	}

	void Start() {

		aniData = new AnimatorData();
		animator = GetComponentInChildren<Animator>();
		controller = GetComponentInChildren<CharacterController>();

		if (!IS_PLAYER && !USE_TEST_CONTROLS && marker != null) {
			marker.gameObject.SetActive(false);
		}

		if (USE_TEST_CONTROLS) {
			moveInputFunc = () => {
				Vector3 input = new Vector3();
				if (Input.GetKey(KeyCode.A)) { input.x = -1; }
				if (Input.GetKey(KeyCode.D)) { input.x = 1; }

				if (Input.GetKey(KeyCode.W)) { input.z = 1; }
				if (Input.GetKey(KeyCode.S)) { input.z = -1; }

				if (Input.GetKey(KeyCode.Space)) { input.y = 1f; }
				return input;
			};
			aimInputFunc = () => {
				Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit rayhit;
				if (Physics.Raycast(mouseRay, out rayhit)) {
					Vector3 p = rayhit.point;
					if (marker != null) {
						marker.position = p;
						//marker.LookAt(transform);
					}
					p.y = transform.position.y;

					Vector3 diff = (p - transform.position) / 20f;
					if (diff.sqrMagnitude > 1.0f) {
						diff.Normalize();
					}
					return diff;
				}

				return Vector3.zero;
			};
			keysInputFunc = () => {
				TKeys v = TKeys.None;
				v = Input.GetKey(KeyCode.C) ? (v | TKeys.Crouch) : v;
				v = Input.GetKey(KeyCode.R) ? (v | TKeys.Use) : v;

				v = Input.GetKey(KeyCode.F) ? (v | TKeys.AltFire) : v;
				v = Input.GetKey(KeyCode.G) ? (v | TKeys.Throw) : v;
				v = Input.GetKey(KeyCode.LeftShift) ? (v | TKeys.Sprint) : v;
				v = Input.GetMouseButton(0) ? (v | TKeys.Fire) : v;

				v = Input.GetKey(KeyCode.Alpha1) ? (v | TKeys.Action1) : v;
				v = Input.GetKey(KeyCode.Alpha2) ? (v | TKeys.Action2) : v;
				v = Input.GetKey(KeyCode.Alpha3) ? (v | TKeys.Action3) : v;
				v = Input.GetKey(KeyCode.Alpha4) ? (v | TKeys.Action4) : v;
				v = Input.GetKey(KeyCode.Alpha5) ? (v | TKeys.Action5) : v;
				v = Input.GetKey(KeyCode.Alpha6) ? (v | TKeys.Action6) : v;
				v = Input.GetKey(KeyCode.Alpha7) ? (v | TKeys.Action7) : v;
				v = Input.GetKey(KeyCode.Alpha8) ? (v | TKeys.Action8) : v;
				v = Input.GetKey(KeyCode.Alpha9) ? (v | TKeys.Action9) : v;
				v = Input.GetKey(KeyCode.Alpha0) ? (v | TKeys.Action0) : v;

				v = Input.GetKey(KeyCode.Tab) ? (v | TKeys.SwitchWeapons) : v;

				return v;
			};
		}
	}

	void Update() {
		if (controller == null) {
			controller = GetComponentInChildren<CharacterController>();
		}
		doNextFrame?.Invoke();
		doNextFrame = null;
		if (animator == null) { animator = GetComponentInChildren<Animator>(); }

		//transform.rotation = Quaternion.Euler(0, yaw, 0);
		//head.localRotation = Quaternion.Euler(pitch, 0, 0);

		if (velocity.y > 0 && (controller.collisionFlags & CollisionFlags.Above) != 0) {
			velocity.y = 0;
		}


		CheckForSnapToGround();
		UpdateDroneStuff();
		
		Vector3 moveInput = moveInputFunc?.Invoke() ?? Vector3.zero;
		Vector3 aimInput = aimInputFunc?.Invoke() ?? Vector3.zero;
		TKeys keyInput = keysInputFunc?.Invoke() ?? TKeys.None;

		if (moveInput.sqrMagnitude > 1) { moveInput = moveInput.normalized; }


		Vector3 newMoveDir = moveInput;

		if (moveRoot != null && moveInput.sqrMagnitude > 0.0f) {
			Quaternion q = moveRoot.rotation;
			Vector3 qe = q.eulerAngles;
			q = Quaternion.Euler(0, qe.y, 0);

			Vector3 forward = moveRoot.forward; forward.y = 0; forward.Normalize();
			Vector3 right = moveRoot.right; right.y = 0; right.Normalize();

			newMoveDir = forward * moveInput.z + right * moveInput.x;

			//forward = transform.forward;		forward.y = 0;	forward.Normalize();
			//right = transform.right;			right.y = 0;	right.Normalize();

			//forward = Vector3.Project(forward, moveDir);
			//right = Vector3.Project(right, moveDir);

			//moveDir = forward + right * sideSpeedRatio;

		}

		float moveRate = 1.0f;
		if (moveInput.sqrMagnitude > tapThreshold) {
			lastMoveDir = moveInput;
			lastMoveTime = 0;
		} else {
			lastMoveTime += Time.deltaTime;
		}
		if (keyInput.Has(TKeys.Sprint)) {
			moveRate *= sprintPower;
		}
		moveDir = Vector3.Lerp(moveDir, newMoveDir, Time.deltaTime * (moveRate * responseBase / mass));

		if (Mathf.Abs(newMoveDir.x) > 0 || Mathf.Abs(newMoveDir.z) > 0) {
			Vector3 target = new Vector3(newMoveDir.x, 0, newMoveDir.z);
			//float target = Mathf.Sign(moveInput.x);
			Vector3 fwd = transform.forward;
			fwd = Vector3.Slerp(fwd, target, Time.deltaTime * lookDampening);

			Debug.DrawLine(transform.position, transform.position + fwd * 4);
		}

		RagCam rcam = moveRoot.GetComponent<RagCam>();
		if (rcam) {
			rcam.camHardPush = rcam.mousePushCamera ? Vector3.zero : aimInput;
		}
		if (aimInput.sqrMagnitude > 0) {
			Vector3 targetPos = transform.position + aimInput;
			targetPos.y = transform.position.y;
			Quaternion prevRotation = transform.rotation;
			transform.LookAt(targetPos, Vector3.up);
			transform.rotation = Quaternion.Lerp(prevRotation, transform.rotation, Time.deltaTime * animResponse);
		}

		Vector3 targetMoveXYZ = new Vector3(Vector3.Dot(moveDir, transform.right), 0, Vector3.Dot(moveDir, transform.forward));
		animMoveXYZ = Vector3.Lerp(animMoveXYZ, targetMoveXYZ, Time.deltaTime * animResponse);

		aniData.Record("MoveX", animMoveXYZ.x);
		aniData.Record("MoveZ", animMoveXYZ.z);
		aniData.Record("MoveAnimSpeed", 2.2f - .4f * moveDir.magnitude);

		bool fire = keyInput.Has(TKeys.Fire);
		aniData.Record("Fire", fire);
		if (muzzleflash != null) {
			if (guides.Length > 0) {
				muzzleflash.transform.position = guides[0].position;
				muzzleflash.transform.LookAt(guides[0].position + guides[0].forward, Vector3.up);
			}
			if (fire) {
				muzzleflash.gameObject.BroadcastMessage("Play", SendMessageOptions.DontRequireReceiver);
			}
		}

		aniData.Record("Crouch", keyInput.Has(TKeys.Crouch));
		aniData.Record("Reload", false);
		aniData.Record("Throw", keyInput.Has(TKeys.Throw));
		if (keyInput.Pressed(TKeys.Use, lastKeys)) {
			if (false) {
				//Use(trackedUsable);

			} else {
				aniData.Record("Reload", keyInput.Has(TKeys.Use));
			}
		}

		if (keyInput.Pressed(TKeys.SwitchWeapons, lastKeys)) {
			ChangeWeapon(currentWeaponKind == 0 ? 1 : 0);
		} else {
			aniData.Record("SwapWeapon", false);
		}


		Vector3 movement = moveDir * speed;

		movement += velocity;
		controller.CheckMoveBack(movement * Time.deltaTime);
		lastInput = moveInput;
		lastKeys = keyInput;

		if (animator != null && animator.enabled) {
			aniData.SetTo(animator);
		}
	}

	private void UpdateDroneStuff() {

		float targetRotorSpeed = baseRotorSpeed;
		var data = aniData;
		Vector3 movement = new Vector3(data.Get<float>("MoveX"), 0, data.Get<float>("MoveZ"));
		if (movement.magnitude > 0) {
			targetRotorSpeed = Mathf.Lerp(baseRotorSpeed, moveRotorSpeed, movement.magnitude);
		}
		rotorSpeed = Mathf.Lerp(rotorSpeed, targetRotorSpeed, Time.deltaTime * rotorSpeedResponse);

		if (rotors != null) {
			foreach (var rotor in rotors) {
				if (rotor == null) { continue; }
				rotor.Rotate(0, rotorSpeed * Time.deltaTime, 0);
			}
		}

		float targetPitch = pitchTargets.y;
		float targetRoll = rollTargets.y;
		float pitchLerp = Mathf.Clamp(movement.z, -1, 1);
		float rollLerp = Mathf.Clamp(movement.x, -1, 1);
		if (pitchLerp > 0.0) { targetPitch = Mathf.Lerp(targetPitch, pitchTargets.z, pitchLerp); }
		if (pitchLerp < 0.0) { targetPitch = Mathf.Lerp(targetPitch, pitchTargets.x, -pitchLerp); }
		if (rollLerp > 0.0) { targetRoll = Mathf.Lerp(targetRoll, rollTargets.z, rollLerp); }
		if (rollLerp < 0.0) { targetRoll = Mathf.Lerp(targetRoll, rollTargets.x, -rollLerp); }
		pitch = Mathf.Lerp(pitch, targetPitch, pitchResponse);
		roll = Mathf.Lerp(roll, targetRoll, rollResponse);

		if (body != null) {
			body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(pitch, 0, roll), Time.deltaTime * rotationResponse);
		}
		if (leftWing != null) {
			leftWing.localRotation = Quaternion.Euler(-pitch, 0, 0);
		}
		if (rightWing != null) {
			rightWing.localRotation = Quaternion.Euler(-pitch, 0, 0);
		}

		Vector3 pos = transform.position;
		Vector3 dir = Vector3.down;
		Vector3 targetPos = pos;

		int numHits = Physics.RaycastNonAlloc(pos + Vector3.up * 1000, dir, hits);
		for (int i = 0; i < numHits; i++) {
			RaycastHit hit = hits[i];
			if (hit.collider.isTrigger) { continue; }

			float angle = Mathf.Clamp(Vector3.Angle(Vector3.up, hit.normal), 0, 90);
			targetPos = hit.point + Vector3.up * (hoverDistance + (slopeHeightBias * angle / 90));

			if (pos.y < hit.point.y + minHoverDistance) {
				pos.y = hit.point.y + minHoverDistance;
				transform.position = pos;
			}


			break;
		}
		targetPos.x = pos.x;
		targetPos.z = pos.z;
		transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * hoverResponse);
	}

	private void CheckForSnapToGround() {
		if (snapToGround) {
			RaycastHit hit;
			if (Physics.Raycast(transform.position, Vector3.down, out hit)) {
				snapToGround = false;
				transform.position = hit.point + Vector3.up * hoverDistance;
			}
		}
	}

	void ChangeWeapon(int weaponKind) {
		animator.SetBool("SwapWeapon", true);
		animator.SetInteger("WeaponKind", weaponKind);
		foreach (var adj in weaponAdjustments) {
			adj.AdjustBones(weaponKind == 0 ? "Longgun" : "HandGun");
		}
		foreach (var w in weapons) {
			w.SetObjectActive(false);
		}
		weapons[weaponKind].SetObjectActive(true);

		currentWeaponKind = weaponKind;

		GrabWeaponGuides();
	}

	void GrabWeaponGuides() {
		if (weapons == null || weapons.Length == 0) {
			weapons = transform.GetComponentsInChildren<Bone>()
				.Where(it => it.kind == "WeaponGuide")
				.Select(it => it.transform)
				.ToArray();
		}
		if (weapons.Length == 0) {
			guides = new Transform[] { transform };
			return;
		}

		string name = weapons[currentWeaponKind].gameObject.name;
		guides = GetComponentsInChildren<Bone>()
			.Where(it => it.kind == "WeaponGuide" && it.name.Contains(name))
			.Select(it => it.transform)
			.ToArray();


		if (guides.Length == 0) {
			guides = weapons[currentWeaponKind].GetComponentsInChildren<Bone>()
			.Where(it => it.kind == "WeaponGuide" && it.name.Contains(name))
			.Select(it => it.transform)
			.ToArray();
		}

		if (guides.Length == 0) {
			guides = transform.GetComponentsInChildren<Bone>()
			.Where(it => it.kind == "WeaponGuide")
			.Select(it => it.transform)
			.ToArray();
		}

		if (guides.Length == 0) {
			guides = alternateGuideSource.GetComponentsInChildren<Bone>()
			.Where(it => it.kind == "WeaponGuide")
			.Select(it => it.transform)
			.ToArray();
		}
		Debug.Log($"Found {guides.Length} weapon guides");

		Array.Sort(guides, (a, b) => a.name.CompareTo(b.name));

	}

}


public static class MovementExt {

	public static bool CheckMoveBack(this CharacterController controller, Vector3 movement, bool doDebug = false) {
		Transform transform = controller.transform;
		Vector3 prev = transform.position;
		controller.Move(movement);

		RaycastHit groundCheck;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out groundCheck)) {
			float angle = Vector3.Angle(Vector3.up, groundCheck.normal);
			Color color = Color.green;
			float thresh = controller.slopeLimit;
			if (groundCheck.point.y > prev.y) {
				thresh += 10;
			}

			if (angle >= thresh) {
				transform.position = prev;
				color = Color.red;
				return true;
			}

			if (doDebug) {
				Debug.DrawLine(groundCheck.point, groundCheck.point + groundCheck.normal, color, 1);
			}


		} else {
			transform.position = prev;

			if (doDebug) {
				Vector3 v1 = prev;
				Vector3 v2 = prev;
				v1.y += 10;
				v2.y -= 10;
				Debug.DrawLine(v1, v2, new Color(1f, .7f, .1f), 1);
			}

			return true;
		}

		return false;
	}

}
