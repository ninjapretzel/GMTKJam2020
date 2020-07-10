/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// Marble

Distributed under MIT license
--------------------------------------------------------------------------------
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
--------------------------------------------------------------------------------
*/
Shader "Procedural/Marble" {
	Properties {
		[Header(Variant Toggles)]
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 1
		[Toggle(FANCY_PARALLAX)] _FANCY_PARALLAX("Fancy Parallax", Float) = 1
		
		[Header(Colors)]
		_Color ("Primary Color", Color) = (.8,.8,.8,1)
		_Color2 ("Second Color", Color) = (.5,.5,.9,1)
		
		[Header(Surface Property)]
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_OcclusionScale ("Occlusion Scale", Float) = .7
		
		[Header(Texture Settings)]
		_Marbling ("Marbling (x+y+z)*w", Vector) = (1, 0, 0, 50)
		
		[Header(Bump Texture)]
		_BumpOctaves ("Bump Octaves", Range(1, 8)) = 6.0
		_BumpPersistence ("Bump Persistence", Range(0, 1)) = .904
		_BumpScale ("Bump Spread", Float) = 20.46
		_BumpAmt ("Bump Amount", Range(.01, 2)) = .6
		
		[Header(Noise Settings)]
		_Seed ("Seed", Float) = 31359.88
		_Octaves ("Octaves", Range(1, 32)) = 8.0
		_Persistence ("Persistence", Range(0, 1)) = .7
		_Scale ("Scale", Float) = 1
		_Factor("Smoothing", Range(0, 1)) = .794
		
		[Header(Parallax Settings)]
		_Parallax ("Parallax Amount", Float) = 77
		_HeightScale ("Height Texture Scale", Float) = 4
		_HeightFactor ("Height Texture Smoothing", Range(0, 1)) = .335
		_DepthBias ("Depth Bias", Float) = 0
		// Depth Layers for Fancy parallax
		_DepthLayers ("Depth Layers", Range(1, 32)) = 8
		// How much cam->pixel distance affects fancy parallax layers.
		_ParallaxDistanceEffect ("Parallax Distance Effect", Range(0, 1)) = 0
		// Radius used to filter/scale the parallax effect
		_CamDistScale ("Camera Distance Scale", Float) = 55
		
		_Offset ("NoiseOffset (x,y,z) * w", Vector) = (0, 0, 0, .1)
	}
	SubShader {
		Tags { 
			"RenderType"="Opaque"
			"DisableBatching" = "True"
		}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard vertex:vert_add_wNormal fullforwardshadows
		#pragma target 3.0
		#pragma multi_compile __ WORLDSPACE
		#pragma multi_compile __ FANCY_PARALLAX
		
		#include "inc/noiseprims.cginc"
		#include "inc/fbm.cginc"
		#include "inc/fbmnormal.cginc"
		#include "inc/procheight.cginc"
		
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		fixed4 _Color2;
		float4 _Offset;
		float _Factor;
		float4 _Marbling;
		
		float _OcclusionScale;
		float _HeightFactor;
		float _HeightScale;
		float _DepthBias;
		
		inline half Depth3D(float3 pos) {
			float h = nnoise(pos * _HeightScale, _HeightFactor);
			return 1.0 - (_DepthBias + h);
		}
		
		void surf(Input IN, inout SurfaceOutputStandard o) {
			resetNoise();
			float4 wpos = float4(IN.worldPos, 1);
			#ifdef WORLDSPACE
				float3 pos = wpos;
			#else 
				float3 pos = mul(unity_WorldToObject, wpos);
			#endif
			pos += _Offset.xyz * _Offset.w;
			float3 start = pos;
			float firstDepth = Depth3D(pos);
			
			#ifdef FANCY_PARALLAX
				pos = Parallax3D_Occ(IN, pos, wpos);
			#else
				float h = Depth3D(pos);
				pos += parallax3d(IN, h);
			#endif
			
			float3 marble = pos.xyz * _Marbling.xyz;
			float p = marble.x + marble.y + marble.z;
			float v = ( 1 + sin( ( p + nnoise(pos, _Factor) ) * _Marbling.w ) ) / 2;
			o.Albedo = lerp(_Color, _Color2, v);
			
			o.Normal = fbmNormal(pos);
			
			float occ = (firstDepth + length(pos - start)) * _OcclusionScale;
			o.Occlusion = 1.0 - occ;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
