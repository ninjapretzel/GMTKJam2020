Shader "Recolor/Diffuse Palette 4" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		
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
	}
	SubShader {
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
			#pragma surface surf Lambert fullforwardshadows addshadow
			#include "./palette4.cginc"

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
