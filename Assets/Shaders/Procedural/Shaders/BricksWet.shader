/*
// Procedural Surface Shaders
// Jonathan Cohen / ninjapretzel
// 2016-2017
// BricksWet

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

Shader "Procedural/BricksWet" {
	Properties {
		
		[Header(Variant Toggles)]
		[Toggle(WORLDSPACE)] _WORLDSPACE("Use Worldspace Position", Float) = 1
		[Toggle(FANCY_PARALLAX)] _FANCY_PARALLAX("Fancy Parallax", Float) = 0
		
		[Header(Colors)]
		_TileColor1 ("Brick Color 1", Color) = (.8,.3,.2,1)
		_TileColor3 ("Brick Color 2", Color) = (.8,.5,.3,1)
		_TileColor4 ("Brick Color 3", Color) = (.8,.4,.5,1)
		_TileColor2 ("Brick Marbling Color", Color) = (.3,.2,.15,1)
		
		_WaterColor ("Water Color", Color) = (.085,.16,.25,1)
		
		_GroutColor1 ("Mortar Color 1", Color) = (.2,.2,.2,1)
		_GroutColor2 ("Mortar Color 2", Color) = (.1,.1,.1,1)
		
		[Header(Surface Property)]
		_Polish ("Polish", Range(0,4)) = 1
		_Glossiness ("Smoothness", Range(0,1)) = 1
		_Metallic ("Metallic", Range(0,1)) = 0.0
		
		[Header(Brick Pattern)]
		_TileSize ("Brick Size", Vector) = (2, 1, 2, 0)
		_TileOffsets ("Brick Offsets", Vector) = (1.5, 0, 1.5, 0)
		_TexScale ("Overall Scale", Float) = 2.0
		_Borderx ("Inner Border", Range(0, .2)) = 0
		_Bordery ("Outer Border", Range(0, .2)) = .15
		
		[Header(Brick Texture Scaling)]
		_MortarScale ("Mortar Texture Scale", Float) = 2.
		_TileScale ("Brick Texture Scale", Float) = 10.
		_TileSampleScale ("Marbling Scales", Vector) = (.2, .2, .2, .5)
		_TileSampleScale2 ("Marbling Params", Vector) = (2., 42., 3.1, 0.)
		_Factor1("Noise Smoothing 1", Range(0, 1)) = .9
		_Factor2("Noise Smoothing 2", Range(0, 1)) = .4
		_ColorSpread ("Color Spread", Vector) = (21.3, 20.8, 3.22, 1)
		_TileBlend ("Color Blending", Vector) = (.5, .5, .5, .9)
		
		[Header(Water Texture Settings)]
		_VoroScale ("Voroni Scale", Float) = 2
		_VoroComp ("Voroni Composition", Vector) = (-1, 1, .3, 1)
		_VoroBlend ("Voroni Layer Blending", Vector) = (-.1, .3, -.2, .6)
		_Pan ("Panning (x,y,z) * w * time", Vector) = (0, 1.5, 0, 1)
		
		[Header(Bump Texture)]
		_BumpOctaves ("Bump Octaves", Range(1, 8)) = 5.0
		_BumpPersistence ("Bump Persistence", Range(0, 1)) = .666
		_NormalScale ("Bump Spread",Float) = 66.6
		_BumpAmt ("Bump Amount", Range(.01, 2)) = .3
		_BumpFactor("Bump Noise Smoothing", Range(0, 1)) = .8
		
		[Header(Noise Settings)]
		_Seed ("Seed", Float) = 31337.1337
		_Octaves ("Octaves", Range(1, 32)) = 6
		_Persistence ("Persistence", Range(0, 1)) = .55
		_Scale ("Scale", Float) = .15
		
		[Header(Parallax Settings)]
		_Parallax ("Parallax Amount", Float) = 333
		_HeightScale ("Height Texture Scale", Float) = 22
		_HeightPower ("Height Texture Power", Range(0, 1)) = 1
		_WetHeight ("Water Height", Float) = .83
		// Depth Layers for Fancy parallax
		_DepthLayers ("Depth Layers", Range(1, 32)) = 5
		// How much cam->pixel distance affects fancy parallax layers.
		_ParallaxDistanceEffect ("Parallax Distance Effect", Range(0, 1)) = 0
		// Radius used to filter/scale the parallax effect
		_CamDistScale ("Camera Distance Scale", Float) = 55
		
		[Header(Texture Offsets)]
		_Offset ("NoiseOffset (x,y,z) * w", Vector) = (0, .1, 0, 1)
		_TileOffset ("Brick Texture Offset", Vector) = (10., 1., 4., 1.)
		
		
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
		#include "inc/voroni.cginc"
		#include "inc/fbm.cginc"
		#include "inc/procheight.cginc"

		half _Glossiness;
		half _Metallic;
		
		fixed4 _TileColor1;
		fixed4 _TileColor2;
		fixed4 _TileColor3;
		fixed4 _TileColor4;
		fixed4 _GroutColor1;
		fixed4 _GroutColor2;
		fixed4 _WaterColor;
		float _Polish;
		
		float4 _ColorSpread;
		
		float _Factor1;
		float _Factor2;
		int _BumpOctaves;
		float _BumpPersistence;
		float _BumpFactor;
		float _BumpAmt;
		float _HeightScale;
		float _HeightPower;
		float _NormalScale;
		float _MortarScale;
		float4 _Offset;
		float4 _Pan;
		float4 _TileSize;
		float _TileScale;
		float4 _TileOffset;
		float4 _TileOffsets;
		float4 _TileBlend;
		float4 _TileSampleScale;
		float4 _TileSampleScale2;
		float _TexScale;
		
		float _Borderx;
		float _Bordery;
		
		float _WetHeight;
		
		float4 _VoroComp;
		float4 _VoroBlend;
		float _VoroScale;	
		
		float _ParallaxFactor;
		
		
		inline float map(float v, float a, float b, float x, float y) {
			const float p = (v-a) / (b-a);
			return x + (y-x) * clamp(p, 0., 1.);
		}
		
		
		inline float voro(float3 pos) {
			return voroni(pos, float3(1,1,1), _VoroComp, VORONI_NORMAL);
					
		}
		
		float3 pan;
		inline half Depth3D(float3 pos) {
			////////////////////////////////////////////////////////////////////
			///////////////////////////////////////////////////////////////////
			//////////////////////////////////////////////////////////////////
			// Tile Position calculation
			//Y-Borders
			
			const float bvm = floor(pos.y / _TileSize.y) * _TileSize.y;
			const float bvp = bvm + _TileSize.y;
			
			
			const float zoff = fmod(abs(bvm), _TileSize.z) * _TileOffsets.z;
			//zoff = bvm * _TileOffsets.z;
			pos.z += zoff;
			float zpos = floor(pos.z / _TileSize.z) * _TileSize.z;
			
			//Z-Borders
			const float bdm = floor((pos.z) / _TileSize.z) * _TileSize.z;
			const float bdp = bdm + _TileSize.z;
			
			//Offset X-Position
			const float xoff = fmod(abs(bvm), _TileSize.x) * _TileOffsets.x
				+ fmod(zpos, _TileSize.x) * _TileOffsets.y;
			//xoff = bvm * _TileOffsets.x;
			pos.x += xoff;
			
			//X-Borders
			const float bhm = floor((pos.x) / _TileSize.x) * _TileSize.x;
			const float bhp = bhm + _TileSize.x;
			
			//Distances
			const float dh = min(abs(pos.x - bhm), abs(pos.x - bhp)) * _TileSize.x;
			const float dv = min(abs(pos.y - bvm), abs(pos.y - bvp)) * _TileSize.y;
			const float dd = min(abs(pos.z - bdm), abs(pos.z - bdp)) * _TileSize.z;
			
			//Restore X/Z-Position
			pos.x -= xoff;
			pos.z -= zoff;
			// End tile position calculation
			////////////////////////////////////////////////////////////////////
			///////////////////////////////////////////////////////////////////
			//////////////////////////////////////////////////////////////////
			
			
			//Blend between tile/mortar
			//Make make transition occur over a small range.
			//For fast transition, rather than slow fade.
			const float v = map(dv * dh * dd, _Borderx, _Bordery, 0., 1.);
			
			const float3 panPos1 = pos * 2.0 	+ pan;
			const float3 panPos2 = pos * 1.0 	+ pan * .5;
			const float3 panPos3 = pos * 0.5	+ pan * .25;
			const float3 panPos4 = pos * 0.25	+ pan * .125;
			const float4 vh = float4( 	voro(panPos1 * _VoroScale),
										voro(panPos2 * _VoroScale),
										voro(panPos3 * _VoroScale),
										voro(panPos4 * _VoroScale)) * _VoroBlend;
										
			const float wetHeight = (1.0 - (vh.x + vh.y + vh.z + vh.w)) * _WetHeight;
			const float hnoise = nnoise(pos * _HeightScale) * _HeightPower;
			const float baseDepth = 1.0 - v + v * v * hnoise;
			
			//float h = clamp(v, 0, 1.) * nnoise(pos * _HeightScale, _ParallaxFactor);
			//if (v > 0) { h = nnoise(pos * _HeightScale, _ParallaxFactor); }
			
			return baseDepth - wetHeight;
		}
		

		void surf (Input IN, inout SurfaceOutputStandard o) {
			pan = _Pan.xyz * _Pan.w * _Time.z;
			resetNoise();
			
			float4 wpos = float4(IN.worldPos, 1);
			#ifdef WORLDSPACE
				float3 pos = wpos;
			#else 
				float3 pos = mul(unity_WorldToObject, wpos);
			#endif
			
			pos += _Offset.xyz * _Offset.w;
			pos *= _TexScale;
			
			
			#ifdef FANCY_PARALLAX
				pos = Parallax3D_Occ(IN, pos, wpos);
			#else
				float h = Depth3D(pos);
				float3 offset = parallax3d(IN, h);
				pos += offset;
			#endif
			
			
			// Sample water panning again after parallax.
			const float3 panPos1 = pos * 2.0 	+ pan;
			const float3 panPos2 = pos * 1.0 	+ pan * .5;
			const float3 panPos3 = pos * 0.5	+ pan * .25;
			const float3 panPos4 = pos * 0.25	+ pan * .125;
			////////////////////////////////////////////////////////////////////
			///////////////////////////////////////////////////////////////////
			//////////////////////////////////////////////////////////////////
			// Tile Position calculation
			//Y-Borders
			
			const float bvm = floor(pos.y / _TileSize.y) * _TileSize.y;
			const float bvp = bvm + _TileSize.y;
			
			
			const float zoff = fmod(abs(bvm), _TileSize.z) * _TileOffsets.z;
			//zoff = bvm * _TileOffsets.z;
			pos.z += zoff;
			float zpos = floor(pos.z / _TileSize.z) * _TileSize.z;
			
			//Z-Borders
			const float bdm = floor((pos.z) / _TileSize.z) * _TileSize.z;
			const float bdp = bdm + _TileSize.z;
			
			//Offset X-Position
			const float xoff = fmod(abs(bvm), _TileSize.x) * _TileOffsets.x
				+ fmod(zpos, _TileSize.x) * _TileOffsets.y;
			//xoff = bvm * _TileOffsets.x;
			pos.x += xoff;
			
			//X-Borders
			const float bhm = floor((pos.x) / _TileSize.x) * _TileSize.x;
			const float bhp = bhm + _TileSize.x;
			
			//Distances
			const float dh = min(abs(pos.x - bhm), abs(pos.x - bhp)) * _TileSize.x;
			const float dv = min(abs(pos.y - bvm), abs(pos.y - bvp)) * _TileSize.y;
			const float dd = min(abs(pos.z - bdm), abs(pos.z - bdp)) * _TileSize.z;
			
			//Restore X/Z-Position
			pos.x -= xoff;
			pos.z -= zoff;
			// End tile position calculation
			////////////////////////////////////////////////////////////////////
			///////////////////////////////////////////////////////////////////
			//////////////////////////////////////////////////////////////////
			
			//Blend between tile/mortar
			//Make make transition occur over a small range.
			//For fast transition, rather than slow fade.
			const float v = map(dv * dh * dd, _Borderx, _Bordery, 0., 1.);
			const float4 vorov = float4( voro(panPos1 * _VoroScale),
								voro(panPos2 * _VoroScale),
								voro(panPos3 * _VoroScale),
								voro(panPos4 * _VoroScale));
			const float4 vbl = _VoroBlend * vorov;
			const float	vval = (vbl.x + vbl.y + vbl.z + vbl.w);
			
			//Tile Position
			float3 pt = _TileOffset.xyz * _TileOffset.w 
					+ float3(pos.x, 0., 0.) 
					+ _TileScale * (pos);
			
			//Tile Sample (blend between tile colors)
			float t = pos.x * _TileSampleScale.x 
					+ pos.y * _TileSampleScale.y
					+ pos.z * _TileSampleScale.z
					+ nnoise(pt * _TileSampleScale2.x, _Factor1) * _TileSampleScale.w;
			t = sin(t * _TileSampleScale2.y) * _TileBlend.z;
			t += _TileBlend.w * nnoise(_TileSampleScale.xyz * _TileSampleScale2.w + pt * _TileSampleScale2.z, _Factor2);
			//Tile color
			float4 tc1 = _TileColor1;
			float4 tc2 = _TileColor2;
			float3 cmixCoord = float3(bhm, bvm, bdm) * _ColorSpread.xyz * _ColorSpread.w;
			float cmix = noise(cmixCoord);
			
			float cmix1 = map(cmix, _TileBlend.x, 0., 0., 1.);
			float cmix2 = map(cmix, _TileBlend.y, 1., 0., 1.);
			if (cmix1 > 0) {
				tc1 = lerp(tc1, _TileColor3, cmix1);
			} else if (cmix2 > 0) {
				tc1 = lerp(tc1, _TileColor4, cmix2);
			}
				
			float4 tc = lerp(tc1, tc2, t);
			//tc = nnoise(pos);
			
			//Mortar Position
			float3 pm = _MortarScale * (pos.xzy);
			float m = nnoise(pm, .1);
			float4 mc = lerp(_GroutColor1, _GroutColor2, m) * .8;
			
			//Tile color
			o.Albedo =
				saturate(_WaterColor * vval) +
				lerp(mc, tc, v).xyz;
			
			//Surface normal
			float normalScale = _NormalScale * (1 + v);
			float bumpAmt = _BumpAmt * (.5 + v * .5);
			
			octaves = _BumpOctaves;
			persistence = _BumpPersistence;
			
			float a = -1 + 2 * nnoise((pos) * _NormalScale, _BumpFactor);
			float b = -1 + 2 * nnoise((pos) * 1.5 * _NormalScale, _BumpFactor);
			fixed3 n = fixed3(a * bumpAmt,b * bumpAmt,1);
			o.Normal = normalize(n);
			
			const float gloss = (v + vval) * _Polish;
			const float antiGloss = (1.0 - (v+vval)) * saturate(1.0 - _Polish);
			
			o.Metallic = _Metallic * gloss + _Metallic * antiGloss;
			o.Smoothness = _Glossiness * gloss + _Glossiness * antiGloss;
			o.Alpha = 1.;
			
			
			
			
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
