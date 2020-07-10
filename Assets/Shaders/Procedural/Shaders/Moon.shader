/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// Moon

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
Shader "Procedural/Moon" {
	Properties {
		[Header(Variant Toggles)]
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 0
		[Toggle(WORLEY)] _WORLEY("Use Worley Noise", Float) = 1
		[Toggle(FANCY_PARALLAX)] _FANCY_PARALLAX("Fancy Parallax", Float) = 1
		
		
		[Header(Colors)]
		_Color1 ("Primary Color", Color) = (.376,.208,.059,1)
		_Color2 ("Secondary Color", Color) = (.161,.102,.000,1)
		_Color3 ("Crater Color", Color) = (.1,.052,.030,1)
		_Color4 ("Crater Detail Color", Color) = (.15,.072,.060,1)
		
		[Header(Surface Property)]
		_Polish ("Polish", Range(0,4)) = .82
		_Glossiness ("Smoothness", Range(0,1)) = 0.04
		_Metallic ("Metallic", Range(0,1)) = 0.14
		
		[Header(Main Texture Settings)]
		_CraterOctaves ("Crater Octaves", Range(2, 9)) = 3
		_CraterFreq ("Crater Octave Scale", Float) = 1.8
		_VoroScale ("Voroni Scale", Float) = 2.9
		_CraterShift ("Crater Shift", Vector) = (1,1,1,1)
		_CraterComp ("Crater Composition", Vector) = (1, 0, 0, 1)
		_CraterMax ("Crater Max", Range(0, 1)) = .411
		_CraterMin ("Crater Min", Range(0, 1)) = .14
		_CraterLip ("Crater Lip", Range(0, 1)) = .559
		_Base ("Crater/Surface Texture Nudging", Range(-1, 1)) = 0
		_DetScale ("Detail Scale", Float) = 111.0
		
		[Header(Bump Texture)]
		_BumpOctaves ("Bump Octaves", Range(1, 8)) = 6.0
		_BumpPersistence ("Bump Persistence", Range(0, 1)) = .931
		_BumpScale ("Bump Spread", Float) = 9.3
		_BumpAmt ("Bump Amount", Range(.01, 4)) = 1.89
		
		[Header(Noise Settings)]
		_Seed ("Seed", Float) = 31337.1337
		_Octaves ("Octaves", Range(1, 32)) = 6.0
		_Persistence ("Persistence", Range(0, 1)) = .858
		_Scale ("Scale", Float) = 1
		
		// Basic Settings 
		[Header(Parallax Settings)]
		_Parallax ("Parallax amount", Float) = 44
		// Depth Layers for Fancy parallax
		_DepthLayers ("Depth Layers", Range(1, 32)) = 8
		// How much cam->pixel distance affects fancy parallax layers.
		_ParallaxDistanceEffect ("Parallax Distance Effect", Range(0, 1)) = 0
		// Radius used to filter/scale the parallax effect
		_CamDistScale ("Camera Distance Scale", Float) = 33
		
		_HeightScale ("Height Texture Scale", Float) = 25
		_HeightPower ("Height Texture Power", Range(0, 1)) = .2
		
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
		#pragma multi_compile __ WORLDSPACE
		#pragma multi_compile __ WORLEY
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
		float _CraterMin;
		float _CraterMax;
		float _CraterPersistence;
		float _CraterLip;
		float _CraterFreq;
		int _CraterOctaves;
		
		float _HeightScale;
		float _HeightPower;
		
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _Color3;
		fixed4 _Color4;
		
		float _Base;
		float _Factor;
		float4 _Offset;
		
		float4 _CraterShift;
		float4 _CraterComp;
		
		
		inline float map(float v, float a, float b, float x, float y) {
			const float p = (v-a) / (b-a);
			return x + (y-x) * clamp(p, 0., 1.);
		}
		
		float lipCubed;
		inline float craterCurve(float v) {
			if (v < _CraterLip) { return (v * v * v) / (lipCubed); }
			return map(v, _CraterLip, 1., 1., _CraterLip);
		}
		
		inline float craters(float3 v) { return voroni(v, _CraterShift.w * _CraterShift.xyz, _CraterComp, 
		#ifdef WORLEY
			VORONI_NORMAL
		#else
			VORONI_MANHATTAN
		#endif
		); }
		
		inline float ocraters(float3 pos) {
			float v = 1;
			float frequency = 1.0;
			float amplitude = 1.0;
			//float maxAmplitude = 0;
			
			for (int i = 0; i < _CraterOctaves; i++) {
				float c = craters(pos * frequency);
				if (c < v) { v = c; }
				//maxAmplitude += amplitude;
				//amplitude *= _CraterPersistence;
				frequency *= _CraterFreq;
			}
			
			return v;
		}
		
		inline float craterSample(float3 pos) {
			return craterCurve(map(ocraters(pos * _VoroScale), _CraterMin, _CraterMax, 0., 1.));
		}
		
		inline half Depth3D(float3 pos) {
			const float hnoise = nnoise(pos * _HeightScale) * _HeightPower;
			const float v = craterSample(pos);
			return 1.0 - v + (v * v * hnoise);
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			lipCubed = _CraterLip * _CraterLip * _CraterLip;
			resetNoise();
			float4 wpos = float4(IN.worldPos, 1);
			//float3 pos = mul(unity_WorldToObject, wpos) + _Offset.xyz * _Offset.w;
			#ifdef WORLDSPACE
				float3 pos = wpos + _Offset.xyz * _Offset.w;
			#else 
				float3 pos = mul(unity_WorldToObject, wpos) + _Offset.xyz * _Offset.w;
			#endif
			//scale = _VoroScale;/
			#ifdef FANCY_PARALLAX
				pos = Parallax3D_Occ(IN, pos, wpos);
			#else 
				const float h = Depth3D(pos);
				
				pos += parallax3d(IN, h);
			#endif 
			const float v0 = craterSample(pos);
			const float v1 = nnoise(pos * _DetScale);
			
			const float rate = saturate(_Base + map(v0, 0, _CraterLip, 0, 1));
			const float4 crater = lerp(_Color3, _Color4, v1);
			const float4 upper = v0 * _Color1 + v1 * _Color2;
			
			/////
			
			
			o.Albedo = lerp(crater, upper, rate);
			
			
			o.Normal = lerp(float3(0, 0, 1), fbmNormal(pos), v0);
			
			//o.Emission = clamp(2 * abs(IN.wNormal), 0., 1.);
			const float gloss = v0 * _Polish;
			
			o.Metallic = _Metallic * gloss;
			o.Smoothness = _Glossiness * gloss;
			o.Alpha = 1;
		}
		ENDCG
	}
	
	FallBack "Diffuse"
}
