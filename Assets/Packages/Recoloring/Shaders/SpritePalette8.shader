// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/Palette 8 Color" {
	Properties {
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		
		_Color ("Final Tint", Color) = (1,1,1,1)
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
		Tags {
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile _ PIXELSNAP_ON
				#pragma shader_feature ETC1_EXTERNAL_ALPHA
				#include "UnityCG.cginc"
				#include "./palette8.cginc"
				
				struct appdata_t {
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex   : SV_POSITION;
					fixed4 color    : COLOR;
					half2 texcoord  : TEXCOORD0;
				};
				
				fixed4 _Color;

				v2f vert(appdata_t IN) {
					v2f OUT;
					OUT.vertex = UnityObjectToClipPos(IN.vertex);
					OUT.texcoord = IN.texcoord;
					OUT.color = IN.color;
					#ifdef PIXELSNAP_ON
					OUT.vertex = UnityPixelSnap(OUT.vertex);
					#endif

					return OUT;
				}

				sampler2D _MainTex;
				sampler2D _AlphaTex;

				fixed4 frag(v2f IN) : SV_Target {
					float4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
					if (c.a <= 0) { c.rgb = 0; return c; }
					
					c = apply_palette(c);
					c.rgb *= c.a;
					
					return c * _Color;
				}
			
			ENDCG
		
		}
		
	}
	
}











