// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/Hue Shift" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		
		_Color ("Final Tint", Color) = (1,1,1,1)
		[Header(Recoloring)]
		_HueShift ("Hue Shift", Range(0, 1)) = 0
		_Saturation ("Saturation", Range(0, 5)) = 1
		
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
				#include "./hsv.cginc"
				
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
				float _HueShift;
				float _Saturation;

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
					float3 hsv_c = toHSV(c.rgb);
					hsv_c.r += _HueShift;
					if (hsv_c.r > 1) { hsv_c.r -= 1; }
					hsv_c.g *= _Saturation;
					c.rgb = toRGB(hsv_c) * c.a;
					
					return c * _Color;
				}
			
			ENDCG
		
		}
		
	}
	
}











