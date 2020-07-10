// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Recolor/Toon Clip HSV" {
	Properties {
		// Toggle for lighting
		[Toggle(RC_UNLIT)] _Unlit("Actual Unlit (on) / Lit-Unlit (off)", Float) = 0
		// Used for Lit-Unlit lighting
		[Header(Outline)]
		_OutlineVal ("Base Thickness", Range(.001, 4.)) = .7
		_OutlineScreen ("Screen Height -> Thickness", Range(0, 2)) = .7
		_OutlineDist ("Distance -> Thickness", Range(0, 2)) = .7
		_OutlineCol ("Outline Color", Color) = (0,0,0,1)
		
		[Header(Lighting for Lit Unlit)]
		_WrapMin ("Min Light Wrap", Range(0,1)) = .45
		_WrapScale ("Light Wrap Power", Range(0,2)) = 1.5
		_AttenBoostMin ("Attenuation Boost", Range(0,1)) = .4
		_AttenScale ("Attenuation Power", Range(0,1)) = .6
		
		_RampBlend ("Light Ramp Blend", Range(0,1)) = .6
		_LitUnlitBlend ("Lit-Unlit blend", Range(0,1)) = .38
		_EnvironmentLight ("Environment Light", Range(0,1)) = .5
		
		[Header(Textures)]
		// Main texture sampler
		_MainTex ("Base (RGB)", 2D) = "white" {}
		// Color clip splatmap
		[NoScaleOffset] _Clip ("Clip", 2D) = "black" {} 
		// Lighting ramp
		[NoScaleOffset] _Ramp ("Lighting Ramp (RGB)", 2D) = "gray" {} 
		
		[Header(Rim)]
		// Color of rim lighting
		_RimColor ("Rim Color", Color) = (0.16,0.19,0.26,0.0)
		// Amount of rim lighting, Lower number = more effect
		_RimPower ("Rim Power", Range(0.1,8.0)) = 3.0
		
		// Base Settings (Black Clip)
		[Header(Base Black Clip)]
		_Color ("Main Color (Color 0) (Black) ", Color) = (0.5,0.5,0.5,1)
		_Saturation ("Saturation (Black)", Range(0, 5)) = 1.0 
		_HueShift ("Hue Shift (Black)", Range(0, 1)) = 0.0
		
		// Group 1 (Red Clip)
		[Header(Red Clip)]
		_Color1 ("Color 1 (Red)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation1 ("Saturation 1 (Red)", Range(0, 5)) = 1.0 
		_HueShift1 ("Hue Shift (Red)", Range(0, 1)) = 0.0
		
		// Group 2 (Green)
		[Header(Green Clip)]
		_Color2 ("Color 2 (Green)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation2 ("Saturation (Green)", Range(0, 5)) = 1.0 
		_HueShift2 ("Hue Shift (Green)", Range(0, 1)) = 0.0
		
		// Group 3 (Blue)
		[Header(Blue Clip)]
		_Color3 ("Color 3 (Blue)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation3 ("Saturation (Blue)", Range(0, 5)) = 1.0 
		_HueShift3 ("Hue Shift (Blue)", Range(0, 1)) = 0.0
		
	}
	
	SubShader {
		
		Tags { 
			"RenderType"="Opaque" 
		}
		LOD 200
		
		Cull Off
		CGPROGRAM
			#include "UnityCG.cginc"
			// Colorspace conversions from RGB->HSV and HSV->RGB 
			#include "./../hsv.cginc"
			
			// Clip texture application
			#include "./../clip.cginc"
			
			// Toon lighting/shading
			#include "./toon.cginc"
			
			#pragma target 3.0
			// Somehow using a finalcolor adjustment function fixes an issue with allocating interpolators...
			#pragma surface surf Toon vertex:toonVert finalcolor:toonAdjust
			#pragma multi_compile __ RC_UNLIT
			sampler2D _MainTex;
			
			void surf(Input IN, inout SurfaceOutputToon o) {
				half4 color = tex2D(_MainTex, IN.uv_MainTex);
				color = apply_clip(color, IN.uv_MainTex);
				surf_toonPre(IN, o, color);
				
				o.Alpha = color.a * _Color.a;
				o.Gloss = color.a;
			}
		ENDCG
			
		UsePass "Hidden/Outline/OUTLINE"
	}

	Fallback "Recolor/Ramp Clip"
}
