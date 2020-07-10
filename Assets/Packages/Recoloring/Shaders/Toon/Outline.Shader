Shader "Hidden/Outline" {
	SubShader {
		Pass {
			Name "OUTLINE"
			Cull Front
			ZTest Less
			Tags { "RenderType"="Transparent" }
			LOD 100
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
				#pragma vertex outlineVert
				#pragma fragment outlineFrag
				#pragma multi_compile __ UT_TEXOUTLINE
				
				
				#include "UnityCG.cginc"

				#ifdef UT_TEXOUTLINE
					struct v2f {
						float4 pos : SV_POSITION;
						float2 uv : TEXCOORD0;
					};
					
					float4 _MainTex_ST;					
					sampler2D _MainTex;
				#else
					struct v2f {
						float4 pos : SV_POSITION;
					};
				#endif
				
				
				float _OutlineVal;
				float _OutlineDist;
				float _OutlineScreen;
				fixed4 _OutlineCol;
				
				
				v2f outlineVert(appdata_base v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					
					// o.pos.xyz += normalize(v.normal) * _OutlineVal;
					float3 normal = normalize(mul((float3x3) UNITY_MATRIX_IT_MV, v.normal));
					normal.x *= UNITY_MATRIX_P[0][0];
					normal.y *= UNITY_MATRIX_P[1][1];
					
					#ifdef UT_TEXOUTLINE
					o.uv = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
					#endif
					
					const float2 dist = normal.xy * length(WorldSpaceViewDir(v.vertex)) * _OutlineDist * .001;
					const float2 size = normal.xy * _ScreenParams.y / 720.0 * _OutlineScreen * .001;
					const float2 base = normal.xy * .001 * _OutlineVal;
					
					o.pos.xy += base + size + dist;
					//o.pos.xy += _OutlineVal * normal.xy * dist;// * size;
					return o;
				}


				fixed4 outlineFrag(v2f i) : SV_Target {
					#ifdef UT_TEXOUTLINE
						float4 tex = tex2D(_MainTex, i.uv);
						float val = max(max(tex.r, tex.g), tex.b) - (1.0/255.0);
						float4 col = tex;
						
						float3 mix = saturate( (col.rgb - val) ) * 255.0;
						float3 mixCol = lerp(.6 * col.rgb, col.rgb, mix);
						
						return float4(.8 * col.rgb * tex.rgb, tex.a) * (float4(.5, .5, .5, 0) + _OutlineCol);
						
					#else
						return _OutlineCol;
					#endif
					
				}

			ENDCG
		}
	}
}