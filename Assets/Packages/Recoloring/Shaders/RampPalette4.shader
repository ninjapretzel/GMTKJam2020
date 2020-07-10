Shader "Recolor/Ramp Palette 4" {
	Properties {
		//Toggle for lighting
		[Toggle(RC_UNLIT)] _Unlit("Unlit?", Float) = 0
		//Main texture sampler
		_MainTex ("Base (RGB)", 2D) = "white" {}
		//Lighting ramp
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
		
		//Color of rim lighting
		_RimColor ("Rim Color", Color) = (0.16,0.19,0.26,0.0)
		//Amount of rim lighting, Lower number = more effect
		_RimPower ("Rim Power", Range(0.1,8.0)) = 3.0 
		//Amount of specular effect
		_Shininess ("Shininess", Range(0.001,5.0)) = .015
		//Color of specular effect
		_SpecColor("Spec Color", Color) = (0.5,0.5,0.5,1)
		
		//Final tint color
		_Color ("Final Tint", Color) = (1,1,1,1)
		//How close will colors need to be to be replaced?
		_Tolerance ("Search Tolerance", Range(.01, .65)) = .1
		//How much does the color change apply?
		_Strength ("Strength", Range(0.00, 4.00)) = 1.00
		
		_ColorSearch1 ("Search Color 1", Color) = (1, 0, 0, 1)
		_ColorTarget1 ("Target Color 1", Color) = (1, 0, 0, 1)
		
		_ColorSearch2 ("Search Color 2", Color) = (0, 1, 0, 1)
		_ColorTarget2 ("Target Color 2", Color) = (0, 1, 0, 1)
		
		_ColorSearch3 ("Search Color 3", Color) = (0, 0, 1, 1)
		_ColorTarget3 ("Target Color 3", Color) = (0, 0, 1, 1)
		
		_ColorSearch4 ("Search Color 4", Color) = (0, 0, 0, 1)
		_ColorTarget4 ("Target Color 4", Color) = (0, 0, 0, 1)
		
	}
	
	SubShader {
		
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
			#include "UnityCG.cginc"
			//Colorspace conversions from RGB->HSV and HSV->RGB 
			#include "./hsv.cginc"
			#include "./palette4.cginc"
			#pragma multi_compile __ RC_UNLIT
			#pragma surface surf ToonBlinnPhong
			
			sampler2D _MainTex;
			float4 _RimColor;
			float _RimPower;
			half  _Shininess;
			
			fixed4 _Color;

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
				float4 c = tex2D(_MainTex, IN.uv_MainTex);
				c = apply_palette(c);
				c.rgb *= c.a;
				
				half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
				float3 emis = _RimColor.rgb * pow(rim, _RimPower);
				
				#ifdef RC_UNLIT
					o.Emission = c.rgb + emis;
				#else
					o.Albedo = c.rgb;
					o.Emission = emis;
				#endif
				
				o.Alpha = c.a * c.a;
				o.Gloss = c.a;
				o.Specular = _Shininess;
			}
			
		ENDCG

	}

	Fallback "Recolor/Diffuse Palette 4"
}
