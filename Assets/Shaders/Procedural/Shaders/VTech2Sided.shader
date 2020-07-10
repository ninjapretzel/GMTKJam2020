/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// VTech
// Heavily modified from "Digital Brain" by struss

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

Shader "Procedural/VTech2Sided" {
	Properties {
		
		[Header(Variant Toggles)]
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 1
		[Toggle(WORLEY)] _WORLEY("Use Worley Noise", Float) = 0
		[Toggle(MOVEMENT)] _MOVEMENT("Apply animated texture movement", Float) = 0
		[Toggle(USE_CLAMP)] _USE_CLAMP("Apply Color Clamp", Float) = 0
		
		[Header(Colors)]
		_Color ("Master Color", Color) = (.4,1,0,1)
		_Albedo ("Albedo Color", Color) = (1,1,1,1)
		_AlbedoBright ("Albedo Brightness", Float) = .5
		_Emissive ("Emissive Color", Color) = (1,1,1,1)
		_EmissiveBright ("Emissive Brightness", Float) = .5
		_ClampColor ("Clamp Color", Color) = (1,1,1,1)
		
		[Header(Noise Settings)]
		_Seed ("Seed", Float) = 11957.3
		_Octaves ("Octaves", Range(1, 8)) = 4.0
		_Persistence ("Persistence", Range(0, 1)) = .637
		_Scale ("Scale", Float) = 2.0
		_Freq ("Frequency Per Octave", Float) = 2.0
		_VoroComp ("Voroni Composition (x+y+z)*w", Vector) = (-1, 1, 0, 1)
		
		_Seed2 ("Trace/Electron Seed", Float) = 11957.3
		_TraceBoost ("Trace Strength", Float) = 2
		_ElecBoost ("Electron Strength", Float) = 1
		
		_TraceComp ("Trace Composition", Vector) = (0.0, 0.05, 1.5, .1)
		_MinElectronLayer ("Electron Start Layer", Range(1, 8)) = 2
		_ElecComp ("Electron Composition", Vector) = (0.10, 0.08, 0, 4)
		_Pan ("Electron panning (x+y+z)*w", Vector) = (1, 1, 1, 222)
		
		
		
		[Header(Texture Offsets and Movement)]
		_Offset ("NoiseOffset (x,y,z) * w", Vector) = (0, 0, 0, 1)
		_Movement ("Noise movement amount (x,y,z) * w", Vector) = (0, 0, 0, 1)
		_MovementSpeed ("Noise movement speed (x,y,z) * w", Vector) = (0, 0, 0, 1)
	}
	SubShader {
		Tags {
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent"
			"DisableBatching"="True"
			
		}
		LOD 200
		Cull Off
		CGPROGRAM
			#pragma surface surf StandardSpecular fullforwardshadows alpha:fade
			#pragma target 3.0
			#pragma multi_compile __ WORLDSPACE
			#pragma multi_compile __ WORLEY
			#pragma multi_compile __ USE_CLAMP
			#pragma multi_compile __ MOVEMENT
			
			#include "UnityCG.cginc"
			#include "inc/noiseprims.cginc"
			#include "inc/voroni.cginc"

			struct Input {
				float2 uv_MainTex;
				float3 worldPos;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			float4 _ClampColor;
			fixed4 _Albedo;
			fixed4 _Emissive;
			float _AlbedoBright;
			float _EmissiveBright;
			float _Factor;
			float _Freq;
			
			float _TraceBoost;
			float _ElecBoost;
			int _MinElectronLayer;
			float _Seed2;
			float _Div;
			
			float4 _TraceComp;
			float _Depth;
			float4 _Offset;
			float4 _VoroComp;
			float4 _ElecComp;
			float4 _Pan;
			float4 _Movement;
			float4 _MovementSpeed;
			
			inline float vtech3(float3 p) {
				float total = 0.0
					, frequency = _Scale
					, amplitude = 1.0
					, maxAmplitude = 0.0;
				const float flicker = 0.4 + .8 * qnoise1(sin(_Time.x));
				const float3 elec = .001 * _Time.z * _Pan.xyz * _Pan.w;

				for (int i = 0; i < _Octaves; i++) {
					seed = _Seed;
					float v1 = voroni(p * frequency + 5.0, float3(1,1,1), _VoroComp,
					#ifdef WORLEY
						VORONI_NORMAL						
					#else
						VORONI_MANHATTAN
					#endif
					);
					float v2 = 0.0;
					
					//Electrons
					if (i >= (_MinElectronLayer-1)) {
						seed = _Seed2;
						v2 = voroni(elec + p * frequency * .5 + 50, float3(1,1,1), _VoroComp,
						#ifdef WORLEY
							VORONI_NORMAL
						#else
							VORONI_MANHATTAN
						#endif
						);
						
						const float va = 1.0 - smoothstep(0.0, _ElecComp.x, v1);
						const float vb = 1.0 - smoothstep(0.0, _ElecComp.y, v2);
						const float vpow = _ElecComp.z + (va * (_ElecComp.w * vb));
						
						total += amplitude * vpow * _ElecBoost;
					}
					
					v1 = 1.0 - smoothstep(_TraceComp.x, _TraceComp.y, v1);
					v2 = amplitude * (qnoise1(v1 * _TraceComp.z + _TraceComp.w));
					/*
					if (i == 0) {
						total += v2 * flicker * flicker;
					} else {
					}*/
					total += v2 * _TraceBoost;
					
					frequency *= _Freq;
					maxAmplitude += amplitude;
					amplitude *= _Persistence;
					
				}
				
				return total / maxAmplitude;
			}
			
			void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
				resetNoise();
				
				const float4 wpos = float4(IN.worldPos, 1);
				
				const float3 pos = 
				#ifdef WORLDSPACE
					wpos
				#else 
					mul(unity_WorldToObject, wpos)
				#endif
					+ _Offset.xyz * _Offset.w;
				
				//float v = ( 1 + sin( ( pos.x + nnoise(pos, _Factor) ) * 50 ) ) / 2;
				#ifdef MOVEMENT				
					const float3 ms = _MovementSpeed.xyz * _MovementSpeed.w;
					const float3 ma = _Movement.xyz * _Movement.w;
					const float time = _Time.z;
					const float3 mp = 
						float3(sin(time * ms.x), sin(time * ms.y), sin(time * ms.z))
						* (ma);
					const float v = vtech3(pos + mp);
				#else
					const float v = vtech3(pos);
				#endif
				
				const float4 baseColor = 
				#ifdef USE_CLAMP
					clamp(_Color * v, 0, _ClampColor);
				#else 
					_Color * v;
				#endif
				//o.Glossiness = _Glossiness;
				//o.Specular = _Metallic;
				
				
				
				o.Albedo = baseColor.rgb * _Albedo.xyz * _AlbedoBright;
				o.Emission = baseColor.rgb * _Emissive.xyz * _EmissiveBright;
				o.Alpha = saturate(v * v + v ) * _Color.a;
			}
		ENDCG
	} 
	Fallback "Legacy Shaders/Transparent/VertexLit"
}
