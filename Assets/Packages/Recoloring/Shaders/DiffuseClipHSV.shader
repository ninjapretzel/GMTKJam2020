Shader "Recolor/Diffuse Clip HSV" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		
		[Header(Emission)]
		_EmissionAmt ("Diffuse->Emissive %", Range(-1,2)) = 0
		[NoScaleOffset] _EmissionTex ("Emission", 2D) = "black" {}
		_Emission ("Emissive Color", Color) = (0, 1, 0, 1)
		
		[Header(Color Clip Regions)]
		[NoScaleOffset] _Clip ("Clip", 2D) = "black" {}
		//Base Settings (Black)
		[Header(Base)]
		_Color ("Main Color (Color 0) (Black) ", Color) = (0.5,0.5,0.5,1)
		_Saturation ("Saturation (Black)", Range(0, 5)) = 1.0 
		_HueShift ("Hue Shift (Black)", Range(0, 1)) = 0.0
		
		//Group 1 (Red)
		[Header(Red)]
		_Color1 ("Color 1 (Red)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation1 ("Saturation (Red)", Range(0, 5)) = 1.0 
		_HueShift1 ("Hue Shift (Red)", Range(0, 1)) = 0.0
		
		//Group 2 (Green)
		[Header(Green)]
		_Color2 ("Color 2 (Green)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation2 ("Saturation (Green)", Range(0, 5)) = 1.0 
		_HueShift2 ("Hue Shift (Green)", Range(0, 1)) = 0.0
		
		//Group 3 (Blue)
		[Header(Blue)]
		_Color3 ("Color 3 (Blue)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation3 ("Saturation (Blue)", Range(0, 5)) = 1.0 
		_HueShift3 ("Hue Shift (Blue)", Range(0, 1)) = 0.0
		
	}
	SubShader {
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
			#pragma surface surf Lambert fullforwardshadows addshadow
			#include "./hsv.cginc"
			#include "./clip.cginc"

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
			sampler2D _MainTex;
			sampler2D _EmissionTex;
			fixed4 _Emission;
			float _EmissionAmt;
			
			struct Input {
				float2 uv_MainTex;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				half4 c = tex2D(_MainTex, IN.uv_MainTex);
				c = apply_clip(c, IN.uv_MainTex);
				
				fixed4 e = c * _EmissionAmt * .6 + tex2D(_EmissionTex, IN.uv_MainTex) * _Emission;
					
				o.Albedo = c.rgb;
				o.Emission = e;
				o.Alpha = c.a;
			}
		ENDCG
	}

	Fallback Off
}
