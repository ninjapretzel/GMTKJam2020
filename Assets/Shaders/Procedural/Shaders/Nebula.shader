// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Procedural/Nebula" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,.341)
		
		[Header(Volumetric Settings)]
		//Volumetric rendering steps. Each 'step' renders more objects at all distances.
		//This has a higher performance hit than iterations.
		_Volsteps ("Volumetric Steps", Range(1,128)) = 32
		_Dust ("Cosmic Dust", Range(.1, 5)) = 1.0
		_SurfaceLevel ("Cloud Surface Level", Range(.001, 500)) = 338
		_Fullness ("Cloud Fullness", Range(0, 3)) = 1
		_Opacity ("Cloud Opacity", Range(0, 3)) = .238
		_EmissiveBlending ("Cloud Emissive Blending", Range(0, .2)) = .069
		
		_CloudNoiseScale ("Cloud Noise Scale", Range(.01, 1)) = .3
		_CloudNoiseFactor ("Cloud Noise Factor", Range(0.0, 1.0)) = .3
		
		_StepStart ("Volumetric Starting Depth", Float) = 1
		//How much farther each volumestep goes
		_StepSize ("Step Size", Float) = 11
		_StepScale ("Step Scale with density (10^)", Range(-3, 3)) = 0.0
		_MinStep ("Minimum Step Distance (10^)", Range(-6, 0)) = -3
		_MaxDepth ("Max Depth", Float) = 0
		
		[Header(Global Noise Settings)]
		_Seed ("Seed", Float) = 31383.73
		_Octaves ("Octaves", Range(1, 32)) = 6
		_Persistence ("Persistence", Range(0, 1)) = .381
		_Scale ("Base Scale", Float) = 1
		
		[Header(Color Noise Settings)]
		//_ColorNoise ("Color Noise", Vector) = (13337.1337, 1.0, 0.381, 6)
		_ColorScale ("Color swizzle spread", Vector) = (1, 1, -1, .25)
		_ColorOffset ("Color Noise Offset", Vector) = (-2, 3, 1, .25)
	}
	SubShader {
		Tags { 
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"DisableBatching"="True"
			"RenderType"="Transparent" 
		}
		
		LOD 100
		
		CGPROGRAM
		//#pragma vertex vert
		//#pragma fragment frag
		#pragma surface surf Lambert alpha:blend
		
		
		#include "UnityCG.cginc"
		#include "inc/sdf.cginc"
		#include "inc/hsv.cginc"
		#include "inc/noiseprims.cginc"
		#include "inc/fbm.cginc"
			
		int _Volsteps;
		float _StepStart;
		float _SurfaceLevel;
		float _StepSize;
		float _MaxDepth;
		float _MinStep;
		float _StepScale;
		float _EmissiveBlending;
		float _Fullness;
		float _Opacity;
		float _Dust;
		float _CloudNoiseScale;
		float _CloudNoiseFactor;
		float4 _Color;
		
		//float4 _ColorNoise;
		float4 _ColorScale;
		float4 _ColorOffset;
		
		
		struct Input {
			float3 worldPos;
			float3 viewDir;
			float3 worldNormal;
		};
		
		
		float4 noised(float3 pos) {
			const float3 p = floor(pos);
			const float3 w = frac(pos);
			
			const float3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
			const float3 du = 30.0*w*w*(w*(w-2.0)+1.0);
			
			const float a = noise( p+float3(0,0,0) );
			const float b = noise( p+float3(1,0,0) );
			const float c = noise( p+float3(0,1,0) );
			const float d = noise( p+float3(1,1,0) );
			const float e = noise( p+float3(0,0,1) );
			const float f = noise( p+float3(1,0,1) );
			const float g = noise( p+float3(0,1,1) );
			const float h = noise( p+float3(1,1,1) );
			
			const float k0 =   a;
			const float k1 =   b - a;
			const float k2 =   c - a;
			const float k3 =   e - a;
			const float k4 =   a - b - c + d;
			const float k5 =   a - c - e + g;
			const float k6 =   a - b - e + f;
			const float k7 = - a + b + c - d + e - f - g + h;

			return float4( -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z),
                 2.0* du * float3( k1 + k4*u.y + k6*u.z + k7*u.y*u.z,
                                 k2 + k5*u.z + k4*u.x + k7*u.z*u.x,
                                 k3 + k6*u.x + k5*u.y + k7*u.x*u.y ) );
		}
		float4 nnoised(float3 pos) {
			float frequency = scale;
			float amplitude = 1.0;
			float val = 0.0;
			float3 d = float3(0,0,0);
									
			for (int i = 0; i < octaves; i++) {
				float4 n = noised(pos * frequency);
				val += amplitude * n.x; // Values
				d += amplitude * n.yzw; // Derivatives
				amplitude *= persistence;
				frequency *= 2.0;
				
				pos = pos.yzx;
			}
			
			
			return float4(val, d);
		}
		
		
		float spiralNoise(float3 pos, float factor) {
			float total = 0.0;
			float frequency = scale;
			float amplitude = 1.0;
			float maxAmplitude = 0.0;
			for (int i = 0; i < octaves; i++) {
				total += abs(sin(pos.y*frequency) + cos(pos.x*frequency)) * amplitude;
				
				maxAmplitude += amplitude * 2.0;
				amplitude *= persistence;
				frequency *= 2.0;
				
				pos = pos.yzx;
			}
			
			
			//*
			const float avg = maxAmplitude * .5;
			if (factor != 0) {
				const float range = avg * clamp(factor, 0, 1);
				const float mmin = avg - range;
				const float mmax = avg + range;
				
				const float val = clamp(total, mmin, mmax);
				return (val - mmin) / (mmax - mmin);
			} 
			
			if (total > avg) { return 1; }
			return 0;
			//*/
		}
		float spiralNoise(float3 pos) { return spiralNoise(pos, 0.5); }
		
		float3 nebColor(float3 pos, float density) {
			//setNoise(_ColorNoise);
			const float csx = _ColorScale.x * _ColorScale.w;
			const float csy = _ColorScale.y * _ColorScale.w;
			const float csz = _ColorScale.z * _ColorScale.w;
			const float3 cpos = _ColorOffset.xyz * _ColorOffset.w;
			
			const float3 colA = float3(noise(pos*csx+cpos.xzy*1.5), noise(pos.yzx*csy+cpos.yxz*3.0), noise(pos.zxy*csz+cpos.zyx*2.5));
			const float3 colB = float3(noise(pos*csz+cpos.xyz*-1.5), noise(pos.yzx*csx+cpos.yzx*-3.0), noise(pos.zxy*csy+cpos.zxy*-2.5));
			// resetNoise();
			//float3 colA = toRGB(float3(noise(pos*.25), .84, .60));
			//float3 colB = toRGB(float3(noise(pos*.03), .54, .40));
			return lerp(colB, colA, clamp(density,0,1));
			//return colA;
			//return colB;
		}
		
		float3 nebDensity(float3 pos) {
			 return nnoise(pos*_CloudNoiseScale, _CloudNoiseFactor);
			// return spiralNoise(pos.zxy * .052, .3);
			//return (spiralNoise(pos.zxy*.052, .3) + nnoise(pos*.3, .3)) / 2.0;
		}

		void surf(Input IN, inout SurfaceOutput o) {
			resetNoise();
			const float3 ro = IN.worldPos;
			const float3 rd = normalize(IN.viewDir);
			float4 sum = 0.0;
			
			const float time = _Time.x;
			
			//Un-scale parameters (source parameters for these are mostly in 0...1 range)
			//Scaling them up makes it much easier to fine-tune shader in the inspector.
			const float stepStart= -_StepStart / 10;
			const float stepSize = -_StepSize / 10;
			const float cloudSurfaceLevel = _SurfaceLevel / 1000;
			
			// maybe rotate field?
			
			float t = stepStart, fade = 1.0;
			float3 v = float3(0, 0, 0);
			float ld=0., td=0., w=0.;
			const float maxDist = 20.0;

			// sum = _Color;
			
			const float STEPSCALE = pow(10, _StepScale);
			const float MINSTEP = pow(10, _MinStep);
			
			for (int r = 0; r < _Volsteps; r++) {
				float3 pos = ro + rd * t;
				if (td > 1.0 || (sum.a > 0.99) || (t > _MaxDepth)) { break; }
				
				const float nextD = max(nebDensity(pos), 0.0) ;
									//* ((float(r)+1) / float(_Volsteps));
				
				if (nextD < cloudSurfaceLevel) {
					ld = cloudSurfaceLevel - nextD;
					w = (1. - td) * ld;
					td += w * _Fullness;
					
					float4 col = float4(nebColor(pos, td), td);
					
					sum += sum.a * float4(sum.rgb, 0.0) * _EmissiveBlending;
					
					col.a *= 64.0 * _Opacity / float(_Volsteps);
					col.a = clamp(col.a, 0.0, 1.0);
					col.rgb *= col.a;
					
					sum = sum + col * (1.0 - sum.a);
				}
				
				// dust
				td += 1.0 / (float(_Volsteps) / _Dust);
				
				
				// distance fading
				// fade *= distFade;
				t += stepSize * max(nextD * STEPSCALE, MINSTEP);
			}
			
    
			// simple scattering
			// sum *= 1. / exp( ld * 0.2 ) * 0.6;
			// sum = clamp( sum, 0.0, 1.0 );
			// sum.xyz = sum.xyz*sum.xyz*(3.0-2.0*sum.xyz);
				
		   
			// col = spiralNoise(pos);
			// col.rgb += a * _Color;
			// col *= .01;
			//sum.rgb = nebDensity(ro);
			// sum.rgb = nebColor(ro, nebDensity(ro));
			// col = abs(col);
			// col += abs(normalize(dir));
			// col.rgb = pos;
			o.Emission = sum.xyz;
			o.Alpha = sum.a * _Color.a;
			//o.Alpha = _Color.a;
		}
		
		
		
		
		ENDCG
	}
	
}