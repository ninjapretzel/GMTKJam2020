// SDF Functions from https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
// SDF = Signed Distance Field 
// SDFs are a useful way of modeling using simple shapes for graphics cards.
// Most sampler functions (sdX- Signed Distance to X):
// 		- Take the first parameter of a point in 3d space
// 		- Then take other parameters that define the shape.
// 		- Return the distance from the given point, to the requested shape placed at the origin.
// There are also operations which work on two distances to union, subtract, or intersect
//		Shapes defined by their distances.


float sdSphere(float3 p, float r) {
	return length(p)-r;
}
float sdBox(float3 p, float3 b) {
	float3 q = abs(p) - b;
	return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}
float sdBoxRound(float3 p, float3 b, float r) {
	float3 q = abs(p) - b;
	return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
}
float sdPlane(float3 p, float4 n) {
	// n must be normalized.
	return dot(p, n.xyz) + n.w;
}


// Union, exact
float opUnion(float d1, float d2) { return min(d1, d2); }
// Subtraction, bound
float opSub(float d1, float d2) { return max(-d1, d2); }
// Intersection, bound
float opIntersect(float d1, float d2) { return max(d1, d2); }

// Union, exact
float opSmoothUnion(float d1, float d2, float k) {
	float h = saturate(.5 + .5 * (d2 - d1)/k);
	return lerp(d2, d1, h) - k*h*(1.0-h);
}

// Subtraction, bound
float opSmoothSub(float d1, float d2, float k) {
	float h = saturate(.5 - .5 * (d2 + d1)/k);
	return lerp(d2, -d1, h) + k*h*(1.0-h);
}

// Intersection, bound
float opSmoothIntersect(float d1, float d2, float k) {
	float h = saturate(.5 - .5 * (d2 - d1) / k);
	return lerp(d2, d1, h) + k*h*(1.0-h);
}