/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// Camo

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
Shader "Procedural/Camo" {
	Properties {
		
		[Header(Variant Toggles)]
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 1
		
		[Header(Colors)]
		_Color1 ("Color 1", Color) = (.490,.431,.294,1)
		_Color2 ("Color 2", Color) = (.274,.196,.059,1)
		_Color3 ("Color 3", Color) = (.196,.235,.098,1)
		_Color4 ("Camo Base Color", Color) = (.098, .078, .094, 1)
		
		[Header(Texture Settings)]
        _Clips ("Clips", Vector) = (1.4, .17, .29, .26)
		_Stretch ("Stretch", Vector) = (1, 1, 1, 1)
        _DiffNoiseJump ("Difference Noise Jump", Float) = 2.5
        _DiffLayers ("Difference Noise Layers", Range(1, 8)) = 3
		
		[Header(Surface Property)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.333
        _Metallic ("Metallic", Range(0,1)) = 0.395
        
		[Header(Noise Settings)]
        _Seed ("Seed", Float) = 13337.13
        _Octaves ("NoiseOctaves", Range(1, 12)) = 4
        _Persistence ("NoisePersistence", Range(0, 1)) = .596
        _Scale ("NoiseScale", Float) = 2.15
        
        
		[Header(Bump Texture)]
        _BumpOctaves ("Bump Octaves", Range(1, 8)) = 5.0
        _BumpScale ("Bump Spread", Float) = 4.5
        _BumpPersistence ("Bump Persistence", Range(0, 1)) = .579
        _BumpAmt ("Bump Amount", Range(.01, 2)) = .779
        
		[Header(Texture Offsets)]
        _Offset ("NoiseOffset (x,y,z) * w", Vector) = (0, 0, 0, 1)
	}
	SubShader {
		Tags { 
			"RenderType"="Opaque" 
			"DisableBatching" = "True" 
		}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
		#pragma multi_compile __ WORLDSPACE
		
		#include "inc/noiseprims.cginc"
		#include "inc/fbm.cginc"
		#include "inc/fbmnormal.cginc"
		
		struct Input {
			float3 worldPos;
			float3 viewDir;
		};
		
		half _Glossiness;
		half _Metallic;
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _Color3;
		fixed4 _Color4;
		int _DiffLayers;
		float _DiffNoiseJump;
		float4 _Offset;
		float4 _Clips;
		float4 _Stretch;
		
		float diffNoise(float3 pos) {
			float v = nnoise(pos);
			for (int i = 0; i < _DiffLayers; i++) {
				pos.z += _DiffNoiseJump;
				v = abs(v-nnoise(pos));
			}
			return v;
		}
		void surf (Input IN, inout SurfaceOutputStandard o) {
			resetNoise();
			
			float4 wpos = float4(IN.worldPos, 1);
			#ifdef WORLDSPACE
				float3 pos = wpos;
			#else 
				float3 pos = mul(unity_WorldToObject, wpos);
			#endif
			float3 clipPos = pos * _Stretch.xyz * _Stretch.w + _Offset.xyz * _Offset.w;
			pos += _Offset.xyz * _Offset.w;
			
			float4 c;
			float clip4 = diffNoise(clipPos);
			if (clip4 < _Clips.w) {
				clipPos.z -= 3.0;
				float clip3 = diffNoise(clipPos);
				if (clip3 < _Clips.z) {
					clipPos.z -= 5.0;
					float clip2 = diffNoise(clipPos);
					if (clip2 > _Clips.y) { c = _Color1; }
					else { c = _Color2; }
				} else { c = _Color3; }
			} else { c = _Color4; }
			
			o.Albedo = c.rgb;
			o.Normal = fbmNormal(pos);
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
