Shader "Skybox/TechLoader" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		
		_GodRay ("Godrays Color", Color) = (1,1,1,1)
		_GodRayParam ("GodRay Param", Vector) = (4, .5, 1, 1)
		_GodRayPow("GodRay Power", Float) = 5
		_GodRayStr("GodRay Strength", Float) = .2
		_GodRayFade("GodRay Fade", Float) = 2
		
		_Horizon1("Horizon Color 1", Color) = (1,1,1,1)
		_HorizonPow1("Horizon Power 1", Float) = .01

		_Horizon2("Horizon Color 2", Color) = (1,1,1,1)
		_HorizonPow2("Horizon Power 2", Float) = .01
	
		_StreamParam1 ("Stream Parameters 1", Vector) = (1, 1, 1, 1)
		_StreamParam2 ("Stream Parameters 2", Vector) = (2, 30, 10, 0)
		_WobbleScale ("Stream Wobble Scale", Vector) = (1, 1, 1, 1)
		_WobblePower ("Stream Wobble Power", Vector) = (3, 3, 3, 1)
		_StreamCount ("Stream Count", Float) = 20
		_Octaves ("NoiseOctaves", Range(1, 32)) = 5.0
		_Seed ("Seed", Float) = 31337.1337
		_Persistence ("NoisePersistence", Range(0, 1)) = .95
		_Scale ("NoiseScale", Float) = 20.0
		_OctaveScale ("OctaveScale", Float) = 2.0
	}

	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ CLAMPOUT
			#include "UnityCG.cginc"
			
// Subset of #include "noiseprims.cginc"
//Standard random generation variables
//values are from hooks into material system
						//Recommended hooks are
int _Octaves; 			//_Octaves ("NoiseOctaves", Range(1, 32)) = 5.0
float _Seed; 			//_Seed ("Seed", Float) = 31337.1337
float _Persistence;		//_Persistence ("NoisePersistence", Range(0, 1)) = .95
float _Scale;			//_Scale ("NoiseScale", Float) = 20.0
float _OctaveScale;		//_OctaveScale ("OctaveScale", Float) = 2.0

int octaves;			//Number of 'octaves' for perlin noise
float seed;				//'seed' value for generation of randomness.
float persistence;		//
float scale;			//
float octaveScale;			//
///Resets the octaves, seed, persistence and scale to the 'hooked' values
///(_Octaves, _Seed, _Persistence, _Scale)
///Typically, this function should be called once at the begining of procedural texture code.
inline void resetNoise() { 
	octaves = _Octaves;
	seed = _Seed;
	persistence = _Persistence;
	scale = _Scale;
	octaveScale = _OctaveScale;
}
///Quick hash method
inline float hash(float n) { return frac(sin(n)*seed); }
///Smooth, standard 3d FBM like noise.
float noise(float3 x) {
	const float3 p = floor(x);
	const float3 f = frac(x);
	const float3 f2= f*f*(3.0-2.0*f);
	const float n = p.x + p.y*157.0 + 113.0*p.z;

	return lerp(lerp(	lerp( hash(n+0.0), hash(n+1.0),f2.x),
						lerp( hash(n+157.0), hash(n+158.0),f2.x),f2.y),
				lerp(	lerp( hash(n+113.0), hash(n+114.0),f2.x),
						lerp( hash(n+270.0), hash(n+271.0),f2.x),f2.y),f2.z);
}

///Octave-3d noise  function
///Output is in range [0...1], but, clusters mostly between .4 and .6
///Use nnoise for values that take up the whole range.
float onoise(float3 pos) {
	float total = 0.0
		, frequency = scale
		, amplitude = 1.0
		, maxAmplitude = 0.0;
	
	for (int i = 0; i < octaves; i++) {
		total += noise(pos * frequency) * amplitude;
		frequency *= octaveScale, maxAmplitude += amplitude;
		amplitude *= persistence;
		// No reason to not ever do this
		// Swizle to spread degredation across all axis.
		pos = pos.yzx; 
	}
	
	
	return total / maxAmplitude;
}



// endsubset
			
			static const float kInnerRadius = 1.0;
			static const float kCameraHeight = 0.0001;

			fixed3 _Color;
			fixed3 _GodRay;
			fixed3 _Horizon1;
			fixed3 _Horizon2;
			float4 _GodRayParam;
			float4 _StreamParam1;
			float4 _StreamParam2;
			float4 _WobbleScale;
			float4 _WobblePower;
			float _HorizonPow1;
			float _HorizonPow2;
			float _GodRayPow;
			float _GodRayStr;
			float _GodRayFade;

			float _StreamCount;

			struct appdata_t {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				half3 rayDir : TEXCOORD0;	// Vector for incoming ray, normalized ( == -eyeRay )
			}; 

			float box(float2 p, float2 b, float r) {
				return length(max(abs(p) - b, 0.0)) - r;
			}

			float3 intersect(float3 o, float3 d, float3 c, float3 u, float3 v) {
				float3 q = o - c;
				return float3(
					dot(cross(u, v), q),
					dot(cross(q, u), d),
					dot(cross(v, q), d)) / dot(cross(v, u), d);
			}
			
			float rand11(float p) {
				return frac(sin(p * 591.32) * 3758.5357);
			}
			float rand12(float2 p) {
				return frac(sin(dot(p.xy, float2(12.9898, 78.233))) * 43758.5357);
			}
			float2 rand21(float p) {
				return frac(float2(sin(p * 591.32), cos(p * 391.32) * 2321.123));
			}
			float2 rand22(float2 p) {
				return frac(float2(sin(p.x * 591.32 + p.y * 154.077), cos(p.x * 391.32 + p.y * 49.077)));
			}


			float shade1(float d) {
				float v = 1.0 - smoothstep(0.0, lerp(0.012, 0.2, 0.0), d);
				float g = exp(d * -20.0);
				return v + g * 0.5;
			}
			
			float3 voro(float2 x) {
				float2 n = floor(x); // grid cell id
				float2 f = frac(x); // grid internal position
				float2 mg; // shortest distance...
				float2 mr; // ..and second shortest distance
				float md = 8.0, md2 = 8.0;
				for (int j = -1; j <= 1; j++) {
					for (int i = -1; i <= 1; i++) {
						float2 g = float2(float(i), float(j)); // cell id
						float2 o = rand22(n + g); // offset to edge point
						float2 r = g + o - f;
						

						float d = max(abs(r.x), abs(r.y)); // distance to the edge

						if (d < md) {
							md2 = md; md = d; mr = r; mg = g;
						} else if (d < md2) {
							md2 = d;
						}
					}
				}
				return float3(n + mg, md2 - md);
			}
			float vorofloor(float3 ro, float3 rd) {
				float sum = 0;
				const float time = _Time.x * 10.0;
				
				const float warp = noise(rd * 444);
				
				for (int i = 0; i < 4; i++) {
					float layer = float(i);
					float base = (-1.0 + 2.0 * onoise(float3(time, i, i) * _WobbleScale.w)) * _WobblePower.w;
					float3 its = intersect(ro, rd, float3(0, -5.0 - layer * 5.0 + base, 0.0), float3(1,0,0), float3(0,0,1));

					if (its.x > 0.0) {
						const float explode = abs(-1.0 + 2.0 * noise(float3(0, time, 0)));
						float nn1 = noise(float3(time, time, 0));
						float nn2 = noise(float3(time, i, 0));
						float nn3 = noise(float3(i, time, 0));
						
						float3 vo = voro((its.yz) * 0.05 + 33.0 * rand21(layer));
						vo -= clamp(4.0 * warp * pow(explode, 30), 0, 1);
						
						float v = exp((-4.0 + nn3 * -200) * (vo.z - 0.02));
						float fx = 0.0;
						
						if (i == 3) {
							float crd = 0.0;
							float xoff = noise(float3(time * 1.1, i, 0));
							
							float fxi = cos(1.0 * xoff + vo.x * .2 + time * 1.5);
							
							fx = clamp(smoothstep(nn2 * .9, 1.0, fxi), 0.0, 0.9) * 1.0 * rand12(vo.xy);
							fx *= exp((-3 + nn1 * -2) * vo.z) * (.1 + nn3 * 1.5);
						}
						sum += v * 0.1 + fx;
					}
				}
				return sum;
			}

			float sky(float3 p) {
				const float a = atan2(p.x, p.z);
				const float gr = _GodRayParam.x;
				const float gp = _GodRayParam.y;
				const float t = _Time.x * _GodRayParam.z * (_GodRayParam.x / 8);
				const float v = rand11(floor(a * gr + t)) * gp
					+ rand11(floor(a * gr * 2.0 - t)) * gp * .5
					+ rand11(floor(a * gr * 4.0 + t)) * gp * .25;

				// return v;
				p = normalize(p);
				return pow(v, max(.0001, p.y * _GodRayFade * .01));
			}

			float horizon(float3 p) {
				return .001 * _HorizonPow1 /  normalize(p).y;
			}
			float mod(float x, float y) { return x - y * floor(x / y); }



			float stream(float3 ro, float3 rd, float4 param1, float4 param2) {
				float sum = 0;
				const float spreadY = param1.x;
				const float spreadXZ = param1.y;
				const float repDist = param1.z;
				const float time = 100.0 + param1.w * 10.0 * _Time.x;
				const float sizeX = param2.x / 100.0;
				const float sizeZ = param2.y / 100.0;
				const float radius = param2.z / 100.0;
				const float glow = param2.w;
				const int reps = (int)_StreamCount;
				float nn1 = onoise(float3(0,0,time));
				
				const float warp = noise(rd * 222);
				
				for (int i = 0; i < reps; i++) {
					const float id = round(floor(float(i))); // float(i);
					const float3 noisePosY = float3(i, 0, i) + float3(time, 0, time);
					const float planeWobble = (-1.0 + 2.0 * onoise(noisePosY*_WobbleScale.y)) * _WobblePower.y;
					const float3 bp = float3(0.0, rand11(id) * spreadY * 2 - spreadY + planeWobble, 0.0);
						
					const float3 its = intersect(ro, rd, bp, float3(1.0,0.0,0.0), float3(0.0,0.0,1.0));
					
					
					if (its.x > 0) {
						float2 pp = its.yz;
						const float3 noisePosX = float3(0, i, i) + float3(0, time, time);
						const float3 noisePosZ = float3(i, i, 0) + float3(time, time, 0);
						
						pp.x += (-1.0 + 2.0 * onoise(noisePosX*_WobbleScale.x)) * _WobblePower.x;
						pp.y += (-1.0 + 2.0 * onoise(noisePosZ*_WobbleScale.z)) * _WobblePower.z;
						
						const float spd = (1.0 + rand11(id) * 6.0) * 1.5;
						pp.y += spd * time;
						pp += (rand21(id) * spreadXZ * 2.0 - spreadXZ) * float2(1.0, 3.3);
						
						const float rep = (rand11(id) + 3) * repDist;
						pp.y = fmod(pp.y, rep * 2.0) - rep;
						
						// Map (0, 1) to (.5, 1)
						float radNoise = .5 + .5 * noise(noisePosX * 3.3);
						float sizeXNoise = .5 + .5 * noise(noisePosY * 2.3);
						float sizeZNoise = .5 + .5 * noise(noisePosZ * 1.3);
						//
						float glowNoise = .1 + .9 * noise(noisePosX * 1.3);
						// .02, .3, .1
						
						const float explode = abs(-1.0 + 2.0 * noise(float3(i, time, 0)));
						const float d = 
							(.40 * box(pp, float2(sizeX * sizeXNoise, sizeZ * sizeZNoise), radius * radNoise))
							- warp * pow(explode, 100);
						const float v = 1.0 - smoothstep(0.0, .03, abs(d) -  .001);
						const float g = min(exp(d * -10.0), 2.0);
						// float g = .001;
						
						sum += (v + g * 0.7 * glow * glowNoise) * 0.5;
					}

				}
				return sum;
			}

			v2f vert(appdata_t v) {
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				OUT.rayDir = half3(eyeRay);
				return OUT;
			}
			
			half4 frag(v2f IN) : SV_Target {
				resetNoise();
				float4 pos = float4(_WorldSpaceCameraPos.xyz, 0);
				
				half3 dir = normalize(IN.rayDir);
				float3 hor = clamp(horizon(dir), 0, 1) * _Horizon1;
				
				float inten = stream(pos, dir, _StreamParam1, _StreamParam2);
				inten += vorofloor(pos, dir);
				inten *= .4 + (sin(_Time.x) * .5 + .5) * .8;
				
				float3 colorMap = float3(	lerp(1.0, .01, _Color.r),
											lerp(1.0, .01, _Color.g),
											lerp(1.0, .01, _Color.b));;
				float3 str = pow(inten, colorMap);
				
				//float3 str = 0;
				
				float sd = dot(dir, float3(0, 1, 0));
				float3 bg = pow(1.0 - abs(sd), 1000.0 / _HorizonPow2) * _Horizon2
					+ (pow(sky(dir), _GodRayPow * .1) * step(0.0, dir.y) * _GodRayStr * .01 * _GodRay);
				
				return half4(str + bg + hor, 1.0);
			}
			
			
			
			
			ENDCG
		}
		
		
	}

	Fallback Off
}
