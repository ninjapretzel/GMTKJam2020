Shader "Recolor/Ramp Clip" {
	Properties {
		//Toggle for lighting
		[Toggle(RC_UNLIT)] _Unlit("Unlit?", Float) = 0
		//Main texture sampler
		_MainTex ("Base (RGB)", 2D) = "white" {}
		//Lighting ramp
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		//Color clip splatmap
		_Clip ("Clip", 2D) = "black" {} 
		_Color ("Base Color (Black Clip)", Color) = (1,1,1,1)
		_Color1 ("Color 1 (Red Clip)", Color) = (1,1,1,1)
		_Color2 ("Color 2 (Green Clip)", Color) = (1,1,1,1)
		_Color3 ("Color 3 (Blue Clip)", Color) = (1,1,1,1)
		
		//Color of rim lighting
		_RimColor ("Rim Color", Color) = (0.16,0.19,0.26,0.0)
		//Amount of rim lighting, Lower number = more effect
		_RimPower ("Rim Power", Range(0.1,8.0)) = 3.0 
		//Amount of specular effect
		_Shininess ("Shininess", Range(0.001,5.0)) = .015
		//Color of specular effect
		_SpecColor("Spec Color", Color) = (0.5,0.5,0.5,1)
	}
	
	SubShader {
		
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
			#include "UnityCG.cginc"
			#include "./clip.cginc"
			#pragma surface surf ToonBlinnPhong
			#pragma multi_compile __ RC_UNLIT
			sampler2D _MainTex;
			float4 _RimColor;
			float _RimPower;
			half  _Shininess;
			
			
			sampler2D _Ramp;

			// custom lighting function that uses a texture ramp based
			// on angle between light direction and normal
			#pragma lighting ToonBlinnPhong exclude_path:prepass
			inline half4 LightingToonBlinnPhong(SurfaceOutput s, fixed3 lightDir, half3 viewDir, fixed atten) {
				half3 h = normalize(lightDir + viewDir);
				#ifndef USING_DIRECTIONAL_LIGHT
				lightDir = normalize(lightDir);
				#endif
				
				//Sample Ramp for Diffuse power
				half d = dot(s.Normal, lightDir)*0.5 + 0.5;
				half3 diff = tex2D(_Ramp, float2(d,d)).rgb;
				
				//BlinnPhong specular calculation
				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0) * s.Gloss;
				
				fixed4 c;
				c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * (atten * 2);
				c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;
				
				return c;
			}
			
			struct Input {
				float2 uv_MainTex : TEXCOORD0;
				float3 viewDir;
			};
			
			void surf (Input IN, inout SurfaceOutput o) {
				half4 color = tex2D(_MainTex, IN.uv_MainTex);
				color = apply_clip(color, IN.uv_MainTex);
				
				half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
				float3 emis = _RimColor.rgb * pow(rim, _RimPower);
				
				#ifdef RC_UNLIT
					o.Emission = color.rgb + emis;
				#else
					o.Albedo = color.rgb;
					o.Emission = emis;
				#endif
				
				o.Alpha = color.a * _Color.a;
				o.Gloss = color.a;
				o.Specular = _Shininess;
			}
			
		ENDCG

	}

	Fallback "Recolor/Diffuse Clip"
}
