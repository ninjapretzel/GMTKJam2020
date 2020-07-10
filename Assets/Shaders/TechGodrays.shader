Shader "Skybox/TechGodrays" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		
		_GodRay ("Godrays Color", Color) = (.4485,.0431,1,1)
		_GodRayParam ("GodRay Param", Vector) = (22, .53, 1, 1)
		_GodRayPow("GodRay Power", Float) = 25.7
		_GodRayStr("GodRay Strength", Float) = 154.57
		_GodRayFade("GodRay Fade", Float) = 171.2
		
		_Horizon1("Horizon Color 1", Color) = (.7764,.3647,1,1)
		_HorizonPow1("Horizon Power 1", Float) = 7.49

		_Horizon2("Horizon Color 2", Color) = (0,.8825,1,1)
		_HorizonPow2("Horizon Power 2", Float) = 10.6
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

			float sky(float3 p) {
				const float skew = p.y > 0 ? 1.0 : .1;
				
				const float a = atan2(p.x, p.z);
				const float gr = _GodRayParam.x;
				const float gp = _GodRayParam.y;
				const float t = _Time.x * _GodRayParam.z * (_GodRayParam.x / 8);
				const float v = rand11(floor(a * gr + t)) * gp  * skew
					+ rand11(floor(a * gr * 2.0 - t)) * gp * .5
					+ rand11(floor(a * gr * 4.0 + t)) * gp * .25;

				// return v;
				p = normalize(p);
				return pow(v, max(.0001, abs(p.y) * _GodRayFade * .01));
			}

			float horizon(float3 p) {
				return .001 * _HorizonPow1 /  normalize(p).y;
			}
			float mod(float x, float y) { return x - y * floor(x / y); }

			v2f vert(appdata_t v) {
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				OUT.rayDir = half3(eyeRay);
				return OUT;
			}
			
			half4 frag(v2f IN) : SV_Target {
				float4 pos = float4(_WorldSpaceCameraPos.xyz, 0);
				
				half3 dir = normalize(IN.rayDir);
				float3 hor = clamp(horizon(dir), 0, 1) * _Horizon1;
				/*
				float3 colorMap = float3(	lerp(1.0, .01, _Color.r),
											lerp(1.0, .01, _Color.g),
											lerp(1.0, .01, _Color.b));;
				float3 str = pow(inten, colorMap);
				*/
				//float3 str = 0;
				
				float sd = dot(dir, float3(0, 1, 0));
				float3 bg = pow(1.0 - abs(sd), 1000.0 / _HorizonPow2) * _Horizon2
					+ (pow(sky(dir), _GodRayPow * .1) * step(0.0, abs(dir.y)) * _GodRayStr * .01 * _GodRay);
				
				return half4(bg + hor, 1.0);
			}
			
			
			
			
			ENDCG
		}
		
		
	}

	Fallback Off
}
