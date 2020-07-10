Shader "Recolor/Cutout/Diffuse Palette 8" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		
		[Header(Tolerance)]
		_Tolerance ("Search Tolerance", Range(.01, .65)) = .1
		_Strength ("Strength", Range(0.00, 4.00)) = 1.00
		
		[Header(Color 1)]
		_ColorSearch1 ("Search Color 1", Color) = (1, 0, 0, 1)
		_ColorTarget1 ("Target Color 1", Color) = (1, 0, 0, 1)
		
		[Header(Color 2)]
		_ColorSearch2 ("Search Color 2", Color) = (0, 1, 0, 1)
		_ColorTarget2 ("Target Color 2", Color) = (0, 1, 0, 1)
		
		[Header(Color 3)]
		_ColorSearch3 ("Search Color 3", Color) = (0, 0, 1, 1)
		_ColorTarget3 ("Target Color 3", Color) = (0, 0, 1, 1)
		
		[Header(Color 4)]
		_ColorSearch4 ("Search Color 4", Color) = (0, 0, 0, 1)
		_ColorTarget4 ("Target Color 4", Color) = (0, 0, 0, 1)
		
		[Header(Color 5)]
		_ColorSearch5 ("Search Color 5", Color) = (1, 1, 0, 1)
		_ColorTarget5 ("Target Color 5", Color) = (1, 1, 0, 1)
		
		[Header(Color 6)]
		_ColorSearch6 ("Search Color 6", Color) = (1, 0, 1, 1)
		_ColorTarget6 ("Target Color 6", Color) = (1, 0, 1, 1)
		
		[Header(Color 7)]
		_ColorSearch7 ("Search Color 7", Color) = (0, 1, 1, 1)
		_ColorTarget7 ("Target Color 7", Color) = (0, 1, 1, 1)
		
		[Header(Color 8)]
		_ColorSearch8 ("Search Color 8", Color) = (1, 1, 1, 1)
		_ColorTarget8 ("Target Color 8", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 600

		CGPROGRAM
			#pragma surface surf Lambert fullforwardshadows addshadow alpha:blend alphatest:_Cutoff
			#include "./palette8.cginc"

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
			sampler2D _MainTex;
			
			struct Input {
				float2 uv_MainTex;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				half4 c = tex2D(_MainTex, IN.uv_MainTex);
				c = apply_palette(c);
				
				o.Albedo = c.rgb;
				o.Alpha = c.a;
			}
		ENDCG
	}

	Fallback Off
}
