#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
// using UnityEngine; // This file basically becomes a no-op if inside of unity, as it defines equivelant vector structs.
#else

#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#if !UNITY
using static Ex.Utils.Mathf;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Ex.Utils {
	#region Mathf
	/// <summary> Like UnityEngine.Mathf, Wrap <see cref="System.Math"/> functions to deal with float/int, and some custom functions. </summary>
	public struct Mathf {
		public const float PI = 3.14159274f;
		public const float EPSILON = 1E-05f;
		public const float SQR_EPSILON = 1E-15f;
		public const float COMPARE_EPSILON = 9.99999944E-11f;
		public const float Infinity = float.PositiveInfinity;
		public const float NegativeInfinity = float.NegativeInfinity;
		public const float Deg2Rad = (2f * PI) / 360f;
		public const float Rad2Deg = 360f / (PI * 2f);
		public static float Sin(float f) { return (float)Math.Sin(f); }
		public static float Cos(float f) { return (float)Math.Cos(f); }
		public static float Tan(float f) { return (float)Math.Tan(f); }
		public static float Asin(float f) { return (float)Math.Asin(f); }
		public static float Acos(float f) { return (float)Math.Acos(f); }
		public static float Atan(float f) { return (float)Math.Atan(f); }
		public static float Atan2(float y, float x) { return (float) Math.Atan2(y, x); }
		public static float Sqrt(float f) { return (float) Math.Sqrt(f); }
		public static float Abs(float f) { return Math.Abs(f); }
		public static int Abs(int f) { return Math.Abs(f); }

		public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
		public static float Exp(float power) { return (float)Math.Exp(power); }
		public static float Log(float f, float b) { return (float)Math.Log(f, b); }
		public static float Log(float f) { return (float)Math.Log(f); }
		public static float Log10(float f) { return (float)Math.Log10(f); }

		public static float Ceil(float f) { return (float)Math.Ceiling(f); }
		public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
		public static float Floor(float f) { return (float)Math.Floor(f); }
		public static int FloorToInt(float f) { return (int)Math.Floor(f); }
		public static float Round(float f) { return (float)Math.Round(f); }
		public static int RoundToInt(float f) { return (int)Math.Round(f); }

		public static float Min(float a, float b) { return a < b ? a : b; }
		public static float Min(float a, float b, float c) { return a < b ? (a < c ? a : c) : (b < c ? b : c); }
		public static float Max(float a, float b) { return a > b ? a : b; }
		public static float Max(float a, float b, float c) { return a > b ? (a > c ? a : c) : (b > c ? b : c); }
		public static int Min(int a, int b) { return a < b ? a : b; }
		public static int Min(int a, int b, int c) { return a < b ? (a < c ? a : c) : (b < c ? b : c); }
		public static int Max(int a, int b) { return a > b ? a : b; }
		public static int Max(int a, int b, int c) { return a > b ? (a > c ? a : c) : (b > c ? b : c); }

		public static float Repeat(float f, float length) { return Clamp(f - Floor(f / length) * length, 0, length); }
		public static float PingPong(float f, float length) { f = Repeat(f, length*2f); return length - Abs(f - length); }
		
		public static float Sign(float f) { return (f < 0) ? -1f : 1f; }
		public static float Clamp01(float f) { return f < 0 ? 0 : f > 1 ? 1 : f; }
		public static float Clamp(float f, float min, float max) { return f < min ? min : f > max ? max : f; }
		public static int Clamp(int f, int min, int max) { return f < min ? min : f > max ? max : f; }
		public static float DeltaAngle(float current, float target) {
			float angle = Repeat(target - current, 360f);
			if (angle > 180f) { angle -= 360f; }
			return angle;
		}
		// @TODO: Look into the specific value of UnityEngine.Mathf.Epsilon (for COMPARE_EPSILON)
		public static bool Approximately(float a, float b) {
			return Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), COMPARE_EPSILON * 8f);
		}
		public static float Map(float a, float b, float val, float x, float y) { return Lerp(x, y, InverseLerp(a, b, val)); }
		public static float Lerp(float a, float b, float f) { return a + (b-a) * Clamp01(f); }
		public static float InverseLerp(float a, float b, float value) { return (a != b) ? Clamp01((value-a) / (b-a)) : 0f; }
		public static float LerpUnclamped(float a, float b, float f) { return a + (b-a) * f; }
		public static float SmoothStep(float a, float b, float f) {
			f = Clamp01(f);
			f = -2f * f * f * f + 3f * f * f;
			return a * f + b * (1f - f);
		}
		public static float LerpAngle(float a, float b, float f) {
			float angle = Repeat(b - a, 360f);
			if (angle > 180f) { angle -= 360f; }
			return a + angle * Clamp01(f);
		}
		public static float MoveTowards(float current, float target, float maxDelta) {
			return (Abs(target - current) <= maxDelta) ? target : (current + Sign(target-current) * maxDelta);
		}
		public static float MoveTowardsAngle(float current, float target, float maxDelta) {
			float delta = DeltaAngle(current, target);
			return (-maxDelta < delta && delta < maxDelta) ? target : MoveTowards(current, current+delta, maxDelta);
		}
		public static float Gamma(float value, float absmax, float gamma) {
			bool negative = value < 0f;
			float abs = Abs(value);
			if (abs > absmax) { return negative ? -abs : abs; }
			float pow =  Pow(abs / absmax, gamma) * absmax;
			return negative ? -pow : pow;
		}
		
		public static float Damp(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = Infinity) {
			smoothTime = Max(.0001f, smoothTime);
			float step = 2f / smoothTime;
			float d = step*deltaTime;
			float smoothed = 1f / (1f + d + 0.48f * d * d + 0.235f * d * d * d);

			float desired = target;
			float maxDelta = maxSpeed * smoothTime;
			float diff = Clamp(current - target, -maxDelta, maxDelta);
			target = current - diff;
			
			float velocityStep = (currentVelocity + step * diff) * deltaTime;
			currentVelocity = (currentVelocity - step * velocityStep) * smoothed;
			float result = target + (diff + velocityStep) * smoothed;
			if (desired - current > 0f == result > desired) {
				result = desired;
				currentVelocity = (result - desired) / deltaTime;
			}
			return result;
		}
		public static float DampAngle(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = Infinity) {
			target = current + DeltaAngle(current, target);
			return Damp(current, target, ref currentVelocity, smoothTime, deltaTime, maxSpeed);
		}

		public static float Spring(float value, float target, ref float velocity, float deltaTime, float strength = 100, float dampening = 1) {
			velocity += (target - value) * strength * deltaTime;
			velocity *= Pow(dampening * .0001f, deltaTime);
			value += velocity * deltaTime;
			return value;
		}
	}
	#endregion Mathf
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region extensions
	public static class MathExtensions {
		public static Vector3Int ToInts(this Vector3 v) {
			int x = FloorToInt(v.x);
			int y = FloorToInt(v.y);
			int z = FloorToInt(v.z);
			return new Vector3Int(x,y,z);
		}
	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector2
	/// <summary> Surrogate class, similar to UnityEngine.Vector2 </summary>
	[System.Serializable]
	public struct Vector2 {
		public static Vector2 zero { get { return new Vector2(0, 0); } }
		public static Vector2 one { get { return new Vector2(1, 1); } }
		public static Vector2 up{ get { return new Vector2(0, 1); } }
		public static Vector2 down { get { return new Vector2(0, -1); } }
		public static Vector2 left { get { return new Vector2(-1, 0); } }
		public static Vector2 right { get { return new Vector2(1, 0); } }
		public static Vector2 negativeInfinity { get { return new Vector2(float.NegativeInfinity, float.NegativeInfinity); } }
		public static Vector2 positiveInfinity { get { return new Vector2(float.PositiveInfinity, float.PositiveInfinity); } }
		
		public float x, y;
		public Vector2(float x, float y) { this.x = x; this.y = y; }

		public float magnitude { get { return Mathf.Sqrt(x*x + y*y); } }
		public float sqrMagnitude { get { return (x*x) + (y*y); } }
		public Vector2 normalized { get { float m = magnitude; if (m > EPSILON) { return this / m; } return zero; } }
		public float this[int i] { 
			get { if (i == 0) { return x; } if (i == 1) { return y; } throw new IndexOutOfRangeException($"Vector2 has length=2, {i} is out of range."); } 
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } throw new IndexOutOfRangeException($"Vector2 has length=2, {i} is out of range."); }
		}
		
		public override bool Equals(object other) { return other is Vector2 && Equals((Vector2)other); }
		public bool Equals(Vector2 other) { return x.Equals(other.x) && y.Equals(other.y); }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public override string ToString() { return $"({x:F2}, {y:F2})"; }

		public void Normalize() { float m = magnitude; if (m > EPSILON) { this /= m; } else { this = zero; } }
		public void Set(float x, float y) { this.x = x; this.y = y; }
		public void Scale(float a, float b) { x *= a; y *= b; }
		public void Scale(Vector2 s) { x *= s.x; y *= s.y; }
		public void Clamp(Vector2 min, Vector2 max) {
			x = Mathf.Clamp(x, min.x, max.x);
			y = Mathf.Clamp(y, min.y, max.y);
		}
		
		public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
		public static Vector2 Min(Vector2 a, Vector2 b) { return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
		public static Vector2 Max(Vector2 a, Vector2 b) { return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }

		public static Vector2 Lerp(Vector2 a, Vector2 b, float f) { f = Clamp01(f); return new Vector2(a.x + (b.x-a.x) * f, a.y + (b.y-a.y) * f); }
		public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float f) { return new Vector2(a.x + (b.x-a.x) *f, a.y + (b.y-a.y) * f); }
		public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta) {
			Vector2 a = target - current;
			float m = a.magnitude;
			return (m < maxDistanceDelta || m == 0f) ? target : (current + a / m * maxDistanceDelta);
		}
		public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 ClampMagnitude(Vector2 vector, float maxLength) {
			return (vector.sqrMagnitude > maxLength * maxLength) ? vector.normalized * maxLength : vector;
		}
		public static Vector2 Reflect(Vector2 dir, Vector2 normal) { return -2f * Dot(normal, dir) * normal + dir; }
		public static Vector2 Project(Vector2 dir, Vector2 normal) {
			float len = Dot(normal, normal);
			return (len < SQR_EPSILON) ? zero : normal * Dot(dir, normal) / len;
		}
		public static Vector2 Perpendicular(Vector2 dir) { return new Vector2(-dir.y, dir.x); }

		public static float Distance(Vector2 a, Vector2 b) { return (a-b).magnitude; }
		public static float Angle(Vector2 from, Vector2 to) { 
			float e = Sqrt(from.sqrMagnitude * to.sqrMagnitude);
			if (e < SQR_EPSILON) { return 0; }
			float f = Mathf.Clamp(Dot(from, to) / e, -1f, 1f);
			return Acos(f) * Rad2Deg;
		}
		public static float SignedAngle(Vector2 from, Vector2 to) {
			float angle = Angle(from, to);
			float sign = Sign(from.x * to.y - from.y * to.x);
			return sign * angle;
		}

		public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
		public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
		public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
		public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
		public static Vector2 operator *(Vector2 a, float f) { return new Vector2(a.x * f, a.y * f); }
		public static Vector2 operator *(float f, Vector2 a) { return new Vector2(a.x * f, a.y * f); }
		public static Vector2 operator /(Vector2 a, float f) { return new Vector2(a.x / f, a.y / f); }
		public static Vector2 operator /(float f, Vector2 a) { return new Vector2(f / a.x, f / a.y); }
		public static bool operator ==(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < COMPARE_EPSILON; }
		public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
		public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
		public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector2Int
	/// <summary> Surrogate class, similar to UnityEngine.Vector2Int </summary>
	[System.Serializable]
	public struct Vector2Int : IEquatable<Vector2Int> {
		public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
		public static Vector2Int one { get { return new Vector2Int(1, 1); } }
		public static Vector2Int up { get { return new Vector2Int(0, 1); } }
		public static Vector2Int down { get { return new Vector2Int(0, -1); } }
		public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
		public static Vector2Int right { get { return new Vector2Int(1, 0); } }

		public int x, y;
		public Vector2Int(int x, int y) { this.x = x; this.y = y; }

		public int this[int i] { 
			get { if (i == 0) { return x; } if (i == 1) { return y; } throw new IndexOutOfRangeException($"Vector2Int has length=2, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } throw new IndexOutOfRangeException($"Vector2Int has length=2, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector2Int && Equals((Vector2Int)other); }
		public bool Equals(Vector2Int other) { return x.Equals(other.x) && y.Equals(other.y); }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public override string ToString() { return $"({x}, {y})"; }
		
		public float magnitude { get { return Sqrt(x * x + y * y); } }
		public int sqrMagnitude { get { return x * x + y * y; } }

		public void Set(int a, int b) { x = a; y = b; }
		public void Scale(Vector2Int scale) { x *= scale.x; y *= scale.y; }
		public void Clamp(Vector2 min, Vector2 max) {
			x = (int) Mathf.Clamp(x, min.x, max.x);
			y = (int) Mathf.Clamp(y, min.y, max.y);
		}

		public static Vector2Int Min(Vector2Int a, Vector2Int b) { return new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
		public static Vector2Int Max(Vector2Int a, Vector2Int b) { return new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }
		public static Vector2Int Scale(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }
		public static float Distance(Vector2Int a, Vector2Int b) { return (b-a).magnitude; }

		public static Vector2Int FloorToInt(Vector2 v) { return new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y)); }
		public static Vector2Int CeilToInt(Vector2 v) { return new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y)); }
		public static Vector2Int RoundToInt(Vector2 v) { return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y)); }
		
		public static Vector2Int operator -(Vector2Int a) { return new Vector2Int(-a.x, -a.y); }
		public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
		public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
		public static Vector2Int operator *(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }
		public static Vector2Int operator /(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x / b.x, a.y / b.y); }
		public static Vector2Int operator *(Vector2Int a, int i) { return new Vector2Int(a.x * i, a.y * i); }
		public static Vector2Int operator *(int i, Vector2Int a) { return new Vector2Int(a.x * i, a.y * i); }
		public static Vector2Int operator /(Vector2Int a, int i) { return new Vector2Int(a.x / i, a.y / i); }
		public static Vector2Int operator /(int i, Vector2Int a) { return new Vector2Int(i / a.x, i / a.y); }
		public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
		public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }
		
		public static implicit operator Vector2(Vector2Int v) { return new Vector2(v.x, v.y); }
		public static explicit operator Vector3Int(Vector2Int v) { return new Vector3Int(v.x, v.y, 0); }
		
	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector3
	/// <summary> Surrogate class, similar to UnityEngine.Vector3 </summary>
	[System.Serializable]
	public struct Vector3 {
		public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
		public static Vector3 one { get { return new Vector3(1, 1, 1); } }
		public static Vector3 right { get { return new Vector3(1, 0, 0); } }
		public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
		public static Vector3 up { get { return new Vector3(0, 1, 0); } }
		public static Vector3 down { get { return new Vector3(0, -1, 0); } }
		public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
		public static Vector3 back { get { return new Vector3(0, 0, -1); } }
		public static Vector3 positiveInfinity { get { return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity); } }
		public static Vector3 negativeInfinity { get { return new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); } }

		public float x,y,z;
		public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
		public float this[int i] {
			get { if (i == 0) { return x; } if (i == 1) { return y; } if (i == 2) { return z; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } if (i == 2) { z = value; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector3 && Equals((Vector3)other); }
		public bool Equals(Vector3 other) { return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z); }
		public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
		public override string ToString() { return $"({x:F2}, {y:F2}, {z:F2})"; }
		
		public Vector3 normalized { get { float m = magnitude; if (m > EPSILON) { return this / m; } return zero; } }
		public float magnitude { get { return Sqrt(x * x + y * y + z * z); } }
		public float sqrMagnitude { get { return x * x + y * y + z * z; } }

		public void Set(float a, float b, float c) { x = a; y = b; z = c; }
		public void Normalize() { float m = magnitude; if (m > EPSILON) { this /= m; } else { this = zero; } }
		public void Scale(Vector3 s) { x *= s.x; y *= s.y; z *= s.z; }
		public void Clamp(Vector3 min, Vector3 max) {
			x = Mathf.Clamp(x, min.x, max.x);
			y = Mathf.Clamp(y, min.y, max.y);
			z = Mathf.Clamp(z, min.z, max.z);
		}

		public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
		public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
		
		public static Vector3 Cross(Vector3 a, Vector3 b) {
			return new Vector3(a.y * b.z - a.z * b.y, 
								a.z * b.x - a.x * b.y,
								a.x * b.y * a.y * b.x);
		}
		public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
		public static Vector3 Reflect(Vector3 dir, Vector3 normal) { return -2f * Dot(normal, dir) * normal + dir; }
		public static Vector3 Project(Vector3 dir, Vector3 normal) {
			float len = Dot(normal, normal);
			return (len < SQR_EPSILON) ? zero : normal * Dot(dir, normal) / len;
		}
		public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) { return v - Project(v, normal); }
		public static float Angle(Vector3 from, Vector3 to) {
			float e = Sqrt(from.sqrMagnitude * to.sqrMagnitude);
			if (e < SQR_EPSILON) { return 0; }
			float f = Mathf.Clamp(Dot(from, to) / e, -1f, 1f);
			return Acos(f) * Rad2Deg;
		}
		public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) {
			float angle = Angle(from, to);
			float sign = Sign(Dot(axis, Cross(from, to)));
			return sign * angle;
		}
		public static float Distance(Vector3 a, Vector3 b) {
			Vector3 v = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
			return Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
		}
		public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) {
			return (vector.sqrMagnitude > maxLength * maxLength) ? vector.normalized * maxLength : vector; 
		}
		public static Vector3 Lerp(Vector3 a, Vector3 b, float f) { f = Clamp01(f); return new Vector3(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f); }
		public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float f) { return new Vector3(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f); }
		public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) {
			Vector3 a = target - current;
			float m = a.magnitude;
			return (m < maxDistanceDelta || m == 0f) ? target : (current + a / m * maxDistanceDelta);
		}

		public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
		public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
		public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
		public static Vector3 operator *(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static Vector3 operator /(Vector3 a, Vector3 b) { return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z); }
		public static Vector3 operator *(Vector3 a, float f) { return new Vector3(a.x * f, a.y * f, a.z * f); }
		public static Vector3 operator *(float f, Vector3 a) { return new Vector3(a.x * f, a.y * f, a.z * f); }
		public static Vector3 operator /(Vector3 a, float f) { return new Vector3(a.x / f, a.y / f, a.z / f); }
		public static Vector3 operator /(float f, Vector3 a) { return new Vector3(f / a.x, f / a.y, f / a.z); }
		public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < COMPARE_EPSILON; }
		public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }

	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector3Int
	/// <summary> Surrogate class, similar to UnityEngine.Vector3Int </summary>
	[System.Serializable]
	public struct Vector3Int : IEquatable<Vector3Int> {
		public static Vector3Int zero { get { return new Vector3Int(0, 0, 0); } }
		public static Vector3Int one { get { return new Vector3Int(0, 0, 0); } }
		public static Vector3Int right { get { return new Vector3Int(1, 0, 0); } }
		public static Vector3Int left { get { return new Vector3Int(-1, 0, 0); } }
		public static Vector3Int up { get { return new Vector3Int(0, 1, 0); } }
		public static Vector3Int down { get { return new Vector3Int(0, -1, 0); } }
		public static Vector3Int forward { get { return new Vector3Int(0, 0, 1); } }
		public static Vector3Int back { get { return new Vector3Int(0, 0, -1); } }

		public int x,y,z;
		public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
		public int this[int i] {
			get { if (i == 0) { return x; } if (i == 1) { return y; } if (i == 2) { return z; } throw new IndexOutOfRangeException($"Vector3Int has length=3, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } if (i == 2) { z = value; } throw new IndexOutOfRangeException($"Vector3Int has length=3, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector3Int && Equals((Vector3Int)other); }
		public bool Equals(Vector3Int other) { return this == other; }
		public override int GetHashCode() { 
			int yy = y.GetHashCode(); int zz = z.GetHashCode(); int xx = x.GetHashCode();
			return xx ^ (yy << 4) ^ (yy >> 28) ^ (zz >> 4) ^ (zz << 28);
		}
		public override string ToString() { return $"({x}, {y}, {z})"; }

		public float magnitude { get { return Sqrt(x * x + y * y + z * z); } }
		public int sqrMagnitude { get { return x * x + y * y + z * z; } }

		public void Set(int a, int b, int c) { x = a; y = b; z = c; }
		public void Scale(Vector3Int scale) { x *= scale.x; y *= scale.y; z *= scale.z; }
		public void Clamp(Vector3 min, Vector3 max) {
			x = (int) Mathf.Clamp(x, min.x, max.x);
			y = (int) Mathf.Clamp(y, min.y, max.y);
			z = (int) Mathf.Clamp(z, min.z, max.z);
		}

		public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
		public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
		public static Vector3Int Scale(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static float Distance(Vector3Int a, Vector3Int b) { return (a - b).magnitude; }

		public static Vector3Int FloorToInt(Vector3 v) { return new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z)); }
		public static Vector3Int CeilToInt(Vector3 v) { return new Vector3Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z)); }
		public static Vector3Int RoundToInt(Vector3 v) { return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)); }

		public static Vector3Int operator -(Vector3Int a) { return new Vector3Int(-a.x, -a.y, -a.z); }
		public static Vector3Int operator +(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z); }
		public static Vector3Int operator -(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z); }
		public static Vector3Int operator *(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static Vector3Int operator /(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x / b.x, a.y / b.y, a.z / b.z); }
		public static Vector3Int operator *(Vector3Int a, int i) { return new Vector3Int(a.x * i, a.y * i, a.z * i); }
		public static Vector3Int operator *(int i, Vector3Int a) { return new Vector3Int(a.x * i, a.y * i, a.z * i); }
		public static Vector3Int operator /(Vector3Int a, int i) { return new Vector3Int(a.x / i, a.y / i, a.z / i); }
		public static Vector3Int operator /(int i, Vector3Int a) { return new Vector3Int(i / a.x, i / a.y, i / a.z); }
		public static bool operator ==(Vector3Int a, Vector3Int b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
		public static bool operator !=(Vector3Int a, Vector3Int b) { return !(a == b); }

		public static implicit operator Vector3(Vector3Int v) { return new Vector3(v.x, v.y, v.z); }
		public static explicit operator Vector2Int(Vector3Int v) { return new Vector2Int(v.x, v.y); }
	}
	#endregion
	#region Vector4 
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	[System.Serializable]
	public struct Vector4 {
		public static Vector4 zero { get { return new Vector4(0,0,0,0); } }
		public static Vector4 one { get { return new Vector4(1,1,1,1); } }
		public static Vector4 positiveInfinity { get { return new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity); } }
		public static Vector4 negativeInfinity { get { return new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); } }
		
		public float x, y, z, w;
		public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
		public float this[int i] {
			get {
				if (i == 0) { return x; } if (i == 1) { return y; } if (i == 2) { return z; } if (i == 3) { return w; }
				throw new IndexOutOfRangeException($"Vector4 has length=4, {i} is out of range.");
			}
			set {
				if (i == 0) { x = value; } if (i == 1) { y = value; } if (i == 2) { z = value; } if (i == 3) { w = value; }
				throw new IndexOutOfRangeException($"Vector4 has length=4, {i} is out of range.");
			}
		}

		public override bool Equals(object other) { return other is Vector4 && Equals((Vector4)other); }
		public bool Equals(Vector4 other) { return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w); }
		public override int GetHashCode() {
			int yy = y.GetHashCode(); int zz = z.GetHashCode(); int xx = x.GetHashCode(); int ww = w.GetHashCode();
			return xx ^ (yy << 2) ^ (zz >> 2) ^ (ww >> 1);
		}
		public override string ToString() { return $"({x}, {y}, {z}, {w})"; }

		public Vector4 normalized { get { float m = magnitude; if (m > EPSILON) { return this / m; } return zero; } }
		public float magnitude { get { return Sqrt(x * x + y * y + z * z + w * w); } }
		public float sqrMagnitude { get { return x * x + y * y + z * z + w * w; } }

		public void Set(float a, float b, float c, float d) { x = a; y = b; z = c; w = d; }
		public void Normalize() { float m = magnitude; if (m > EPSILON) { this /= m; } else { this = zero; } }
		public void Scale(Vector4 s) { x *= s.x; y *= s.y; z *= s.z; w *= s.w; }
		public void Clamp(Vector4 min, Vector4 max) {
			x = Mathf.Clamp(x, min.x, max.x);
			y = Mathf.Clamp(y, min.y, max.y);
			z = Mathf.Clamp(z, min.z, max.z);
			w = Mathf.Clamp(w, min.w, max.w);
		}

		public static Vector4 Min(Vector4 a, Vector4 b) { return new Vector4(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z), Mathf.Min(a.w, b.w)); }
		public static Vector4 Max(Vector4 a, Vector4 b) { return new Vector4(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z), Mathf.Max(a.w, b.w)); }

		public static float Dot(Vector4 a, Vector4 b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w; }
		public static Vector4 Reflect(Vector4 dir, Vector4 normal) { return -2f * Dot(normal, dir) * normal + dir; }
		public static Vector4 Project(Vector4 dir, Vector4 normal) {
			float len = Dot(normal, normal);
			return (len < SQR_EPSILON) ? zero : normal * Dot(dir, normal) / len;
		}

		public static float Distance(Vector4 a, Vector4 b) {
			Vector4 v = new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
			return Sqrt(v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w);
		}
		public static Vector4 ClampMagnitude(Vector4 vector, float maxLength) {
			return (vector.sqrMagnitude > maxLength * maxLength) ? vector.normalized * maxLength : vector;
		}

		public static Vector4 Lerp(Vector4 a, Vector4 b, float f) {
			f = Clamp01(f);
			return new Vector4(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f, a.w + (b.w - a.w) * f);
		}
		public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float f) {
			return new Vector4(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f, a.z + (b.z - a.z) * f, a.w + (b.w - a.w) * f);
		}
		public static Vector4 MoveTowards(Vector4 current, Vector4 target, float maxDistanceDelta) {
			Vector4 a = target - current;
			float m = a.magnitude;
			return (m < maxDistanceDelta || m == 0f) ? target : (current + a / m * maxDistanceDelta);
		}

		public static Vector4 operator -(Vector4 a) { return new Vector4(-a.x, -a.y, -a.z, -a.w); }
		public static Vector4 operator +(Vector4 a, Vector4 b) { return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); } 
		public static Vector4 operator -(Vector4 a, Vector4 b) { return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); } 
		public static Vector4 operator *(Vector4 a, Vector4 b) { return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w); } 
		public static Vector4 operator /(Vector4 a, Vector4 b) { return new Vector4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w); } 
		public static Vector4 operator *(Vector4 a, float f) { return new Vector4(a.x * f, a.y * f, a.z * f, a.w * f); } 
		public static Vector4 operator *(float f, Vector4 a) { return new Vector4(a.x * f, a.y * f, a.z * f, a.w * f); } 
		public static Vector4 operator /(Vector4 a, float f) { return new Vector4(a.x / f, a.y / f, a.z / f, a.w / f); } 
		public static Vector4 operator /(float f, Vector4 a) { return new Vector4(f / a.x, f / a.y, f / a.z, f / a.w); } 
		
		public static bool operator ==(Vector4 a, Vector4 b) { return (a-b).sqrMagnitude <= COMPARE_EPSILON; }
		public static bool operator !=(Vector4 a, Vector4 b) { return !(a == b); }

		public static implicit operator Vector4(Vector3 v) { return new Vector4(v.x, v.y, v.z, 0f); }
		public static implicit operator Vector3(Vector4 v) { return new Vector3(v.x, v.y, v.z); }
		public static implicit operator Vector4(Vector2 v) { return new Vector4(v.x, v.y, 0f, 0f); }
		public static implicit operator Vector2(Vector4 v) { return new Vector2(v.x, v.y); }
		
	}
	#endregion
	#region Rect
	[System.Serializable]
	public struct Rect : IEquatable<Rect> {
		public static Rect zero { get { return new Rect(0, 0, 0, 0); } }
		public static Rect unit{ get { return new Rect(0, 0, 1f, 1f); } }

		public float x,y,width,height;
		public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
		public Rect(Vector2 pos, Vector2 size) { x = pos.x; y = pos.y; width = size.x; height = size.y; }
		public Rect(Rect source) { x = source.x; y = source.y; width = source.width; height = source.height; }

		public Vector2 position { 
			get { return new Vector2(x, y); } 
			set { x = value.x; y = value.y; }
		}
		public Vector2 center { 
			get { return new Vector2(x + width/2f, y + height/2f); } 
			set { x = value.x - width/2f; y = value.y - height / 2f; }
		}
		public Vector2 min { 
			get { return new Vector2(x, y); } 
			set { x = value.x; y = value.y; }
		}
		public Vector2 max {
			get { return new Vector2(x + width, y + height); } 
			set { x = value.x - width; y = value.y - height; }
		}
		public Vector2 size {
			get { return new Vector2(width, height); }
			set { width = value.x; height = value.y; }
		}
		public float xMin { get { return x; } set { float xm = xMax; x = value; width = xm - x; } }
		public float yMin { get { return y; } set { float ym = yMax; y = value; height = ym - y; } }
		public float xMax { get { return x + width; } set { width = value - x; } }
		public float yMax { get { return y + height; } set { height = value - y; } }

		public float left { get { return x; } }
		public float right { get { return x + width; } }
		public float top { get { return y; } }
		public float bottom { get { return y + height; } }

		public override bool Equals(object other) { return other is Rect && this.Equals((Rect)other); }
		public bool Equals(Rect other) { return x.Equals(other.x) && y.Equals(other.y) && width.Equals(other.width) && height.Equals(other.height); }
		public override string ToString() { return $"(x:{x:F2}, y:{y:F2}, width:{width:F2}, height:{height:F2})"; }
		public override int GetHashCode() { return x.GetHashCode() ^ (width.GetHashCode() << 2) ^ (y.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1); }

		public void Set(float x, float y, float width, float height) {
			this.x = x; this.y = y; this.width = width; this.height = height;
		}
		public bool Contains(Vector2 point) {
			return point.x >= xMin && point.x <= xMax && point.y >= yMin && point.y <= yMax;
		}
		public bool Contains(Vector3 point) {
			return point.x >= xMin && point.x <= xMax && point.y >= yMin && point.y <= yMax;
		}

		public bool Overlaps(Rect other) {
			return other.xMax > xMin && other.xMin < xMax && other.yMax > yMin && other.yMin < yMax;
		}
		public bool Touches(Rect other) {
			return other.xMax >= xMin && other.xMin <= xMax && other.yMax >= yMin && other.yMin <= yMax;
		}

		public Vector2 NormalizedToPoint(Vector2 coords) {
			return new Vector2(Lerp(x, xMax, coords.x), Lerp(y, yMax, coords.y));
		}
		public Vector2 PointToNormalized(Vector2 point) {
			return new Vector2(InverseLerp(x, xMax, point.x), InverseLerp(y, yMax, point.y));
		}

		public static bool operator !=(Rect a, Rect b) { return !(a == b); }
		public static bool operator ==(Rect a, Rect b) { return a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height; }
	}
	#endregion
	#region RectInt
	[System.Serializable]
	public struct RectInt : IEquatable<RectInt> {
		public int x,y,width,height;
		public RectInt(int x, int y, int width, int height) { this.x = x; this.y = y; this.width = width; this.height = height; }
		public RectInt(Vector2Int pos, Vector2Int size) { x = pos.x; y = pos.y; width = size.x; height = size.y; }
		public RectInt(RectInt source) { x = source.x; y = source.y; width = source.width; height = source.height; }

		public Vector2Int position {
			get { return new Vector2Int(x, y); }
			set { x = value.x; y = value.y; }
		}
		public Vector2 center {
			get { return new Vector2(x + width / 2f, y + height / 2f); }
		}
		public Vector2Int min {
			get { return new Vector2Int(x, y); }
			set { x = value.x; y = value.y; }
		}
		public Vector2Int max {
			get { return new Vector2Int(x + width, y + height); }
			set { x = value.x - width; y = value.y - height; }
		}
		public Vector2Int size {
			get { return new Vector2Int(width, height); }
			set { width = value.x; height = value.y; }
		}

		public int xMin { get { return x; } set { int xm = xMax; x = value; width = xm - x; } }
		public int yMin { get { return y; } set { int ym = yMax; y = value; height = ym - y; } }
		public int xMax { get { return x + width; } set { width = value - x; } }
		public int yMax { get { return y + height; } set { height = value - y; } }

		public int left { get { return x; } }
		public int right { get { return x + width; } }
		public int top { get { return y; } }
		public int bottom { get { return y + height; } }

		public override bool Equals(object other) { return other is RectInt && Equals((RectInt)other); }
		public bool Equals(RectInt other) { return x == other.x && y == other.y && width == other.width && height == other.height; }
		public override string ToString() { return $"(x:{x}, y:{y}, width:{width}, height:{height})"; }
		public override int GetHashCode() { return x.GetHashCode() ^ (width.GetHashCode() << 2) ^ (y.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1); }
	}
	#endregion
	#region Plane
	[System.Serializable]
	public struct Plane { 

		private Vector3 _normal;
		public float distance;
		public Vector3 normal { get { return _normal; } set { _normal = value.normalized; } }
		
		public Plane(Vector3 normal, Vector3 point) {
			this._normal = normal.normalized;
			distance = -Vector3.Dot(normal, point);
		}
		public Plane(Vector3 normal, float distance) {
			this._normal = normal.normalized;
			this.distance = distance;
		}
		public Plane(Vector3 a, Vector3 b, Vector3 c) {
			_normal = Vector3.Cross(b-a, c-a).normalized;
			distance = -Vector3.Dot(_normal, a);
		}
		public Plane flipped { get { return new Plane(-_normal, -distance); } }

		public override string ToString() { return $"(Normal:{normal}, distance:{distance})"; }

		public void SetNormalAndPosition(Vector3 normal, Vector3 point) {
			this._normal = normal.normalized;
			distance = -Vector3.Dot(normal, point);
		}
		public void Set3Points(Vector3 a, Vector3 b, Vector3 c) {
			_normal = Vector3.Cross(b - a, c - a).normalized;
			distance = -Vector3.Dot(_normal, a);
		}
		public void Flip() { _normal = -_normal; distance = -distance; }
		public void Translate(Vector3 translation) { distance += Vector3.Dot(_normal, translation); }

		public static Plane Translate(Plane p, Vector3 translation) { return new Plane(p._normal, p.distance + Vector3.Dot(p._normal, translation)); }

		public Vector3 ClosestPointOnPlane(Vector3 point) {
			float d = Vector3.Dot(_normal, point) + distance;
			return point - _normal * d;
		}
		public float GetDistanceToPoint(Vector3 point) { return Vector3.Dot(_normal, point) + distance; }
		public bool GetSide(Vector3 point) { return Vector3.Dot(_normal, point) + distance > 0f; }
		public bool SameSide(Vector3 a, Vector3 b) {
			float da = GetDistanceToPoint(a);
			float db = GetDistanceToPoint(b);
			return (da > 0f && db > 0f) || (da <= 0f && db <= 0f);
		}

		public bool Raycast(Ray ray, out float enter) {
			float angle = Vector3.Dot(ray.direction, _normal);
			if (Approximately(angle, 0f)) {
				enter = 0f;
				return false;
			}
			float distance = -Vector3.Dot(ray.origin, _normal) - this.distance;
			enter = distance/angle;
			return (enter > 0f);
		}
	}
	#endregion
	#region Ray
	[System.Serializable]
	public struct Ray {
		public Vector3 origin, dir;
		public Ray(Vector3 origin, Vector3 dir) { this.origin = origin; this.dir = dir.normalized; }
		public Vector3 direction { get { return dir; } set { dir = value.normalized; } }

		public override string ToString() { return $"(Origin: {origin} Direction: {dir})"; }
		public Vector3 GetPoint(float distance) { return origin + dir * distance; }
	}
	#endregion
	#region Ray2D
	[System.Serializable]
	public struct Ray2D {
		public Vector2 origin, dir;
		public Ray2D(Vector2 origin, Vector2 dir) { this.origin = origin; this.dir = dir.normalized; }
		public Vector2 direction { get { return dir; } set { dir = value.normalized; } }

		public override string ToString() { return $"(Origin: {origin} Direction: {dir})"; }
		public Vector2 GetPoint(float distance) { return origin + dir * distance; }
	}
	#endregion
	#region Bounds aka AABB
	[System.Serializable]
	public struct Bounds : IEquatable<Bounds> {
		public Vector3 center, extents;

		public Bounds(Vector3 center, Vector3 size) { this.center = center; extents = size / 2f; }
		public Vector3 size { get { return extents * 2f; } set { extents = value / 2f; } }
		public Vector3 min { get { return center - extents; } set { SetMinMax(value, max); } }
		public Vector3 max { get { return center + extents; } set { SetMinMax(min, value); } }
		
		public override bool Equals(object other) { return other is Bounds && Equals((Bounds) other); }
		public bool Equals(Bounds other) { return center.Equals(other.center) && extents.Equals(other.extents); }
		public override int GetHashCode() { return center.GetHashCode() ^ extents.GetHashCode() << 2; }
		public override string ToString() { return $"(Center: {center}, Extents: {extents})"; }

		public void SetMinMax(Vector3 min, Vector3 max) { extents = (max - min) * 0.5f; center = min + extents; }
		public void Encapsulate(Vector3 point) { SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point)); }
		public void Encapsulate(Bounds bounds) { Encapsulate(bounds.center - bounds.extents); Encapsulate(bounds.center + bounds.extents); }
		public void Expand(float amount) { var a = amount * .5f; extents += new Vector3(a,a,a); }
		public void Expand(Vector3 amount) { extents += amount * .5f; }

		public bool Intersects(Bounds bounds) {
			Vector3 amin = min; Vector3 amax = max;
			Vector3 bmin = bounds.min; Vector3 bmax = bounds.max;
			return amin.x <= bmax.x && amax.x >= bmin.x
				&& amin.y <= bmax.y && amax.y >= bmin.y
				&& amin.z <= bmax.z && amax.z >= bmin.z;
		}
		// Unfortunately some of the more useful stuff is hiddin in native code. R I P guess I'll have to code them myself.
		public bool Contains(Vector3 point) {
			Vector3 min = this.min; Vector3 max = this.max;
			return point.x <= max.x && point.x >= min.x
				&& point.y <= max.y && point.y >= min.y
				&& point.z <= max.z && point.z >= min.z;
		}
		public bool Intersects(Ray r) {
			Vector3 min = this.min; Vector3 max = this.max;
			Vector3 inv = 1f / r.dir;
			float tmin = -Infinity; 
			float tmax = Infinity;
			
			float x1 = (min.x - r.origin.x) * inv.x;
			float x2 = (max.x - r.origin.x) * inv.x;
			tmin = Max(tmin, Min(x1, x2));
			tmax = Min(tmax, Max(x1, x2));
			float y1 = (min.y - r.origin.y) * inv.y;
			float y2 = (max.y - r.origin.y) * inv.y;
			tmin = Max(tmin, Min(y1, y2));
			tmax = Min(tmax, Max(y1, y2));
			float z1 = (min.z - r.origin.z) * inv.z;
			float z2 = (max.z - r.origin.z) * inv.z;
			tmin = Max(tmin, Min(z1, z2));
			tmax = Min(tmax, Max(z1, z2));
			
			return tmax >= tmin;
		}
	}
	#endregion

	#region Serializers and Deserializers
	/// <summary> Class to easily read/write small vectors of numbers for BSON serialization. Does not write begin/end constructs for tighter packing. </summary>
	/// <remarks> This may make things more brittle, but should this should not matter, since each of these fundamental types shouldn't be used haphazardly. </remarks>
	internal static class SerHelper {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReadInt(this BsonDeserializationContext ctx) {
			return (int)ctx.Reader.ReadDouble();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReadLong(this BsonDeserializationContext ctx) {
			return (long)ctx.Reader.ReadDouble();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ReadFloat(this BsonDeserializationContext ctx) {
			return (float)ctx.Reader.ReadDouble();
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ReadDouble(this BsonDeserializationContext ctx) {
			return ctx.Reader.ReadDouble();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 ReadV4(this BsonDeserializationContext ctx) {
			float x = (float)ctx.Reader.ReadDouble();
			float y = (float)ctx.Reader.ReadDouble();
			float z = (float)ctx.Reader.ReadDouble();
			float w = (float)ctx.Reader.ReadDouble();
			return new Vector4(x, y, z, w);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ReadV3(this BsonDeserializationContext ctx) {
			float x = (float) ctx.Reader.ReadDouble();
			float y = (float) ctx.Reader.ReadDouble();
			float z = (float) ctx.Reader.ReadDouble();
			return new Vector3(x,y,z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ReadV2(this BsonDeserializationContext ctx) {
			float x = (float)ctx.Reader.ReadDouble();
			float y = (float)ctx.Reader.ReadDouble();
			return new Vector2(x, y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ReadV3I(this BsonDeserializationContext ctx) {
			int x = (int)ctx.Reader.ReadDouble();
			int y = (int)ctx.Reader.ReadDouble();
			int z = (int)ctx.Reader.ReadDouble();
			return new Vector3Int(x, y, z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int ReadV2I(this BsonDeserializationContext ctx) {
			int x = (int)ctx.Reader.ReadDouble();
			int y = (int)ctx.Reader.ReadDouble();
			return new Vector2Int(x, y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect ReadRect(this BsonDeserializationContext ctx) {
			float x = (float)ctx.Reader.ReadDouble();
			float y = (float)ctx.Reader.ReadDouble();
			float width = (float)ctx.Reader.ReadDouble();
			float height = (float)ctx.Reader.ReadDouble();
			return new Rect(x, y, width, height);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectInt ReadRectInt(this BsonDeserializationContext ctx) {
			int x = (int)ctx.Reader.ReadDouble();
			int y = (int)ctx.Reader.ReadDouble();
			int width = (int)ctx.Reader.ReadDouble();
			int height = (int)ctx.Reader.ReadDouble();
			return new RectInt(x, y, width, height);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartArray(this BsonDeserializationContext ctx) { ctx.Reader.ReadStartArray(); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndArray(this BsonDeserializationContext ctx) { ctx.Reader.ReadEndArray(); }
		////////////////////////////////////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////////////////////////////
		//////////////////////////////////////////////////////////////////////////////////////////////////
		// Serialization
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteInt(this BsonSerializationContext ctx, int v) {
			ctx.Writer.WriteDouble(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteLong(this BsonSerializationContext ctx, long v) {
			ctx.Writer.WriteDouble(v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteFloat(this BsonSerializationContext ctx, float v) {
			ctx.Writer.WriteDouble(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteDouble(this BsonSerializationContext ctx, float v) {
			ctx.Writer.WriteDouble(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteV4(this BsonSerializationContext ctx, Vector4 v) {
			ctx.Writer.WriteDouble(v.x);
			ctx.Writer.WriteDouble(v.y);
			ctx.Writer.WriteDouble(v.z);
			ctx.Writer.WriteDouble(v.w);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteV3(this BsonSerializationContext ctx, Vector3 v) {
			ctx.Writer.WriteDouble(v.x);
			ctx.Writer.WriteDouble(v.y);
			ctx.Writer.WriteDouble(v.z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteV2(this BsonSerializationContext ctx, Vector2 v) {
			ctx.Writer.WriteDouble(v.x);
			ctx.Writer.WriteDouble(v.y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteRect(this BsonSerializationContext ctx, Rect r) {
			ctx.Writer.WriteDouble(r.x);
			ctx.Writer.WriteDouble(r.y);
			ctx.Writer.WriteDouble(r.width);
			ctx.Writer.WriteDouble(r.height);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteV2I(this BsonSerializationContext ctx, Vector2Int v) {
			ctx.Writer.WriteDouble(v.x);
			ctx.Writer.WriteDouble(v.y);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteV3I(this BsonSerializationContext ctx, Vector3Int v) {
			ctx.Writer.WriteDouble(v.x);
			ctx.Writer.WriteDouble(v.y);
			ctx.Writer.WriteDouble(v.z);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteRectInt(this BsonSerializationContext ctx, RectInt r) {
			ctx.Writer.WriteDouble(r.x);
			ctx.Writer.WriteDouble(r.y);
			ctx.Writer.WriteDouble(r.width);
			ctx.Writer.WriteDouble(r.height);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartArray(this BsonSerializationContext ctx) { ctx.Writer.WriteStartArray(); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndArray(this BsonSerializationContext ctx) { ctx.Writer.WriteEndArray(); }
	}
	public class BoundsSerializer : SerializerBase<Bounds> {
		public override Bounds Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector3 center = context.ReadV3();
			Vector3 size = context.ReadV3();
			context.EndArray();
			return new Bounds(center, size);
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Bounds value) {
			Vector3 center = value.center;
			Vector3 size = value.size;
			context.StartArray();
			context.WriteV3(center);
			context.WriteV3(size);
			context.EndArray();
		}
	}
	public class PlaneSerializer : SerializerBase<Plane> {
		public override Plane Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector3 normal = context.ReadV3();
			float distance = context.ReadFloat();
			context.EndArray();
			return new Plane(normal, distance);
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Plane value) {
			context.StartArray();
			context.WriteV3(value.normal);
			context.WriteFloat(value.distance);
			context.EndArray();
		}
	}
	public class RaySerializer : SerializerBase<Ray> {
		public override Ray Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector3 origin = context.ReadV3();
			Vector3 direction = context.ReadV3();
			context.EndArray();
			return new Ray(origin, direction);
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Ray value) {
			context.StartArray();
			context.WriteV3(value.origin);
			context.WriteV3(value.direction);
			context.EndArray();
		}
	}
	public class Ray2DSerializer : SerializerBase<Ray2D> {
		public override Ray2D Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector2 origin = context.ReadV2();
			Vector2 dir = context.ReadV2();
			context.EndArray();
			return new Ray2D(origin, dir);
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Ray2D value) {
			context.StartArray();
			context.WriteV2(value.origin);
			context.WriteV2(value.direction);
			context.EndArray();
		}
	}
	public class RectSerializer : SerializerBase<Rect> {
		public override Rect Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Rect r = context.ReadRect();
			context.EndArray();
			return r;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rect value) {
			context.StartArray();
			context.WriteRect(value);
			context.EndArray();
		}
	}
	public class RectIntSerializer : SerializerBase<RectInt> {
		public override RectInt Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			RectInt r = context.ReadRectInt();
			context.EndArray();
			return r;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RectInt value) {
			context.StartArray();
			context.WriteRectInt(value);
			context.EndArray();
		}
	}
	public class Vector4Serializer : SerializerBase<Vector4> {
		public override Vector4 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector4 v = context.ReadV4();
			context.EndArray();
			return v;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector4 value) {
			context.StartArray();
			context.WriteV4(value);
			context.EndArray();
		}
	}
	public class Vector3Serializer : SerializerBase<Vector3> {
		public override Vector3 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector3 v = context.ReadV3();
			context.EndArray();
			return v;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector3 value) {
			context.StartArray();
			context.WriteV3(value);
			context.EndArray();
		}
	}
	public class Vector2Serializer : SerializerBase<Vector2> {
		public override Vector2 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector2 v = context.ReadV2();
			context.EndArray();
			return v;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector2 value) {
			context.StartArray();
			context.WriteV2(value);
			context.EndArray();
		}
	}
	public class Vector3IntSerializer : SerializerBase<Vector3Int> {
		public override Vector3Int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector3Int v = context.ReadV3I();
			context.EndArray();
			return v;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector3Int value) {
			context.StartArray();
			context.WriteV3I(value);
			context.EndArray();
		}
	}
	public class Vector2IntSerializer : SerializerBase<Vector2Int> {
		public override Vector2Int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
			context.StartArray();
			Vector2Int v = context.ReadV2I();
			context.EndArray();
			return v;
		}
		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector2Int value) {
			context.StartArray();
			context.WriteV2I(value);
			context.EndArray();
		}
	}
	#endregion

}

#endif
