﻿Shader "Recolor/Cutout/Standard Clip" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		
		[Header(Surface)]
		_Spec ("Metallic", Range(0,1)) = 0.5
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		
		[NoScaleOffset] _Occlusion ("Occlusion", 2D) = "white" {}
		
		[NoScaleOffset] _Height ("Height", 2D) = "gray" {}
		_Parallax ("Parallax ammount", Range(0, .1)) = .02
		_Bias ("Parallax bias", Range(-.1, .1)) = 0
		
		_BumpMap ("Bump", 2D) = "bump" {}
		
		[Header(Emission)]
		_EmissionAmt ("Diffuse->Emissive %", Range(-1,2)) = 0
		[NoScaleOffset] _EmissionTex ("Emission", 2D) = "black" {}
		_Emission ("Emissive Color", Color) = (0, 1, 0, 1)
		
		[Header(Color Clip Regions)]
		[NoScaleOffset] _Clip ("Clip", 2D) = "black" {}
		_Color ("Base Color (Black Clip)", Color) = (1,1,1,1)
		_Color1 ("Color 1 (Red Clip)", Color) = (1,1,1,1)
		_Color2 ("Color 2 (Green Clip)", Color) = (1,1,1,1)
		_Color3 ("Color 3 (Blue Clip)", Color) = (1,1,1,1)
		
	}
		
	SubShader {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		LOD 600
		
		CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf StandardSpecular fullforwardshadows addshadow vertex:vert alphatest:_Cutoff
			
			//#pragma multi_compile_fwdbase
			//#pragma multi_compile_fwdadd_fullshadows

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
			#include "./clip.cginc"
			#include "AutoLight.cginc"
			//#include "UnityStandardShadow.cginc"
			
			struct Input {
				float2 uv_MainTex;
				float2 uv_BumpMap;
				float3 viewDir;
				float3 worldPos;
				float3 worldRefl;
				float3 worldNormal;
				INTERNAL_DATA
			};

			fixed4 _Emission;
			
			float _Spec;
			float _EmissionAmt;
			float _Parallax;
			float _Bias;
			
			sampler2D _MainTex;
			sampler2D _BumpMap;
			sampler2D _Height;
			sampler2D _Occlusion;
			sampler2D _EmissionTex;
			
			half _Glossiness;
			half _Metallic;

			float3 eyeVec;
			
			void vert(inout appdata_full v) {
				float3 binormal = cross(v.normal, v.tangent.xyz) * v.tangent.w;
				float3x3 tbnMatrix = float3x3(v.tangent.xyz, binormal, v.normal);
				
				eyeVec = _WorldSpaceCameraPos - v.vertex.xyz;
				eyeVec = mul(tbnMatrix, eyeVec);
			}
			
			void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
				float h = tex2D(_Height, IN.uv_MainTex);
				float v = h * _Parallax - _Bias;
				float3 eye = normalize(IN.viewDir);
				float2 offset = eye.xy * v;
				float2 newTexCo = IN.uv_MainTex + offset;
				float2 newNrmCo = IN.uv_BumpMap + offset;
				o.Normal = UnpackNormal(tex2D(_BumpMap, newNrmCo));
				
				fixed4 c = tex2D(_MainTex, newTexCo);
				c = apply_clip(c, newTexCo);
				
				fixed4 e = c * _EmissionAmt +  tex2D(_EmissionTex, newTexCo) * _Emission;
				
				o.Albedo = c;
				o.Emission = e.rgb;
				
				o.Specular = fixed3(_Spec, _Spec, _Spec);
				o.Smoothness = _Glossiness;
				o.Occlusion = tex2D(_Occlusion, newTexCo);
				
				o.Alpha = c.a;
			}
		ENDCG
	} 
	FallBack "Recolor/Cutout/Diffuse Clip"
}
