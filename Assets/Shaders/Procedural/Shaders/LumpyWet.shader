/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// Lumpy Wet

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

Shader "Procedural/LumpyWet" {
	Properties {
		
		[Header(Variant Toggles)]
		[Toggle(WORLEY)] _WORLEY("Use Worley Noise", Float) = 0
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 1
		[Toggle(FANCY_PARALLAX)] _FANCY_PARALLAX("Fancy Parallax", Float) = 0
		
		[Header(Colors)]
		_Color1 ("Primary Color", Color) = (1.,.8588,.8588,1)
		_Color2 ("Secondary Color", Color) = (.1255,.098,.0314,1)
		_WaterColor ("Water Color", Color) = (.085, .16, .25, 1)
		
		[Header(Surface Property)]
		_Polish ("Polish", Range(0,4)) = 2
		_Glossiness ("Smoothness", Range(0,1)) = 0.211
		_Metallic ("Metallic", Range(0,1)) = 0.0
		
		[Header(Lumpyness)]
		_VoroComp ("Voroni Composition", Vector) = (-1, 1, .3, 1)
		_VoroBlend ("Voroni Layer Blending", Vector) = (-.1, .3, -.2, .6)
		_VoroScale ("Voroni Scale", Float) = 7.01
		_Pan ("Wetness Panning (x,y,z) * w * time", Vector) = (0, 3, 0, .1)
		
		[Header(Bump Texture)]
		_BumpOctaves ("BumpOctaves", Range(1, 8)) = 5.0
		_BumpPersistence ("Bump Persistence", Range(0, 1)) = .666
		_BumpScale ("Bumpiness Spread", Range(1.337, 33.37)) = 14
		_BumpAmt ("Bumpiness Amount", Range(.01, 4)) = 2.46
		
		[Header(Noise Settings)]
		_Seed ("Seed", Float) = 16567
		_Octaves ("Octaves", Range(1, 32)) = 3
		_Persistence ("Persistence", Range(0, 1)) = .482
		_Scale ("Scale", Float) = 15
		
		[Header(Parallax Settings)]
		_Parallax ("Parallax Amount", Float) = 111
		_WetHeight ("Water Height", Float) = 1
		// Depth Layers for Fancy parallax
		_DepthLayers ("Depth Layers", Range(1, 32)) = 8
		// How much cam->pixel distance affects fancy parallax layers.
		_ParallaxDistanceEffect ("Parallax Distance Effect", Range(0, 1)) = 0
		// Radius used to filter/scale the parallax effect
		_CamDistScale ("Camera Distance Scale", Float) = 55
		
		[Header(Texture Offsets)]
		_Offset ("NoiseOffset (x,y,z) * w", Vector) = (0, 0, 0, 1)
	}
	SubShader {
		Tags { 
			"RenderType"="Opaque" 
			"DisableBatching"="True"
		}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard vertex:vert_add_wNormal fullforwardshadows 
		#pragma target 3.0
		#pragma multi_compile __ WORLEY
		#pragma multi_compile __ WORLDSPACE
		#pragma multi_compile __ FANCY_PARALLAX
		
		#include "inc/procheight.cginc"
		#include "inc/noiseprims.cginc"
		#include "inc/fbm.cginc"
		#include "inc/voroni.cginc"
		#include "inc/fbmNormal.cginc"
		
		
		
		float _VoroScale;		
		float _DetScale;
		float _Polish;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _WaterColor;
		
		float _Factor;
		float _WetHeight;
		float4 _VoroComp;
		float4 _VoroBlend;
		float4 _Offset;
		float4 _Pan;
		
		inline float voro(float3 pos) {
			return 
				voroni(pos, float3(1,1,1), _VoroComp,
			#ifdef WORLEY
				VORONI_NORMAL
			#else	
				VORONI_MANHATTAN
			#endif 
				);
					
		}
		
		float3 pan;
		inline half Depth3D(float3 pos) {
			
			const float3 panPos1 = pos * 2.0 	+ pan;
			const float3 panPos2 = pos * 1.0 	+ pan * .5;
			const float3 panPos3 = pos * 0.5	+ pan * .25;
			const float3 panPos4 = pos * 0.25	+ pan * .125;
			const float4 vh = float4( 	voro(panPos1 * _VoroScale),
										voro(panPos2 * _VoroScale),
										voro(panPos3 * _VoroScale),
										voro(panPos4 * _VoroScale)) * _VoroBlend;
			
			const float baseDepth = 1.0 - voro(pos * _VoroScale);
			const float wetHeight = (vh.x + vh.y + vh.z + vh.w);
			
			return baseDepth - (1.0 - wetHeight) * _WetHeight;
		}
		
		void surf(Input IN, inout SurfaceOutputStandard o) {
			resetNoise();
			pan = _Pan.xyz * _Pan.w * _Time.z;
			const float4 wpos = float4(IN.worldPos, 1);
			#ifdef WORLDSPACE
				float3 pos = wpos;
			#else 
				float3 pos = mul(unity_WorldToObject, wpos).xyz;
			#endif
			
			pos += + _Offset.xyz * _Offset.w;
			
			
			#ifdef FANCY_PARALLAX
				pos = Parallax3D_Occ(IN, pos, wpos);
			#else
				float h = Depth3D(pos);
								
				float3 offset = parallax3d(IN, h);
				
				pos += offset;
			#endif
			const float3 panPos1 = pos * 2.0 	+ pan;
			const float3 panPos2 = pos * 1.0 	+ pan * .5;
			const float3 panPos3 = pos * 0.5	+ pan * .25;
			const float3 panPos4 = pos * 0.25	+ pan * .125;
			
			
			const float v0 = voro(pos * _VoroScale);
			const float v1 = nnoise(pos);
			const float4 vs = float4( 	voro(panPos1 * _VoroScale),
										voro(panPos2 * _VoroScale),
										voro(panPos3 * _VoroScale),
										voro(panPos4 * _VoroScale));
			
			const float4 vb = _VoroBlend * vs;
			const float val = (vb.x + vb.y + vb.z + vb.w);
			
			
			o.Albedo = v0 * _Color1 + v1 * _Color2 + val * _WaterColor;
			
			o.Normal = fbmNormal(pos);
			
			//o.Emission = clamp(2 * abs(IN.wNormal), 0., 1.);
			const float gloss = val * _Polish;
			const float antiGloss = (1.0-val) * saturate(1.0-_Polish);
			
			o.Metallic = _Metallic * gloss + _Metallic * antiGloss;
			o.Smoothness = _Glossiness * gloss + _Glossiness * antiGloss;
			o.Alpha = 1;
		}
		ENDCG
	}
	
	FallBack "Diffuse"
}
