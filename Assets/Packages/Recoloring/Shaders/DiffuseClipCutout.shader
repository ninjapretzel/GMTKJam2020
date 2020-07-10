Shader "Recolor/Cutout/Diffuse Clip" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		
		
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
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 600

		CGPROGRAM
			#pragma surface surf Lambert fullforwardshadows addshadow alpha:blend alphatest:_Cutoff
			#include "./clip.cginc"
			
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
