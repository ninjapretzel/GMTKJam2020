////////////////////////////////////
// 
//	Written by NinjaPretzel 
//	2017
//	ninjapretzel@gmail.com
//	For Unity Asset Store
//

Shader "Bar/Fancy - Overlay" {
	Properties {
		// Toggles the bar filling direction
		[Toggle(LEFT_TO_RIGHT)] _LEFT_TO_RIGHT("Left To Right", Float) = 1
		// Changes detail textures from existing in Screen-Space to in UV space
		[Toggle(SCREENSPACE_TEXTURES)] _SCREENSPACE_TEXTURES("Screenspace Textures", Float) = 1
		
		// Used to provide information about the physical size of the bar
		// Mostly to make sure it has the correct aspect ratio for the border.
		// Also used for sizing non-screenspace textures.
		_SizeInfo ("Size information (_SizeInfo), xy = Width/Height", Vector) = (8,2, 0,0)
		
		// Amount of fill.
		// [0, _Fill] is considered 'Full'
		// [_Fill, 1] is considered 'Empty'
		_Fill ("Fill Amount (_Fill)", Range(0, 1)) = .5
		
		// Delta of _Fill
		// If this is zero, there is no third segment.
		// If this is > zero, the third segment is 'filling'
		//		Is tinted with _PosColor, and is the region [_Fill - _Delta, _Fill]
		// If this is < zero, the third segment is 'emptying'
		//		Is tinted with _NegColor, and is the region [_Fill, _Fill - _Delta]
		_Delta ("Delta Fill (_Delta)", Range(-1, 1)) = .2
		
		// Main texture, basically just overlaid across the bar. 
		// Vertical gradients work best
		_MainTex ("Main Texture (Bar Background)", 2D) = "white" {}
		
		// Color tint applied after everything else. 
		_Color ("Primary Color Tint", Color) = (1,1,1,1)
		
		// Color of border region.
		_BorderColor ("Border Color", Color) = (0,0,0,1)
		
		// Thickness of border region. Do you like it Thicc or Slim?
		_Border ("Border Strength", Range(0, .03)) = .01
		
		// Glowyness of the edge between regions.
		_EdgeGlow ("Edge Glow", Range(.0, .2)) = .05
		// Wavyness of the edge between regions. 
		_Wavyness ("Wavyness (X = Amp, Y = Freq, Z = Speed, W = Wobble", Vector) = (.25, 20, 5, .5)
		// How quickly the edge between regions pans over time.
		_WavePan ("Wave Pan", Float) = 11
		
		// Color blended with _MainTex
		_BGColor ("Background Color (RGB * A)", Color) = (1,1,1,.5)
		// Color blended with the 'Filled' region
		_FillColor ("Filled Color Tint (RGB * A)", Color) = (0,.66,0,.5)
		// Color blended with the 'Empty' region
		_EmptyColor ("Empty Color Tint (RGB * A)", Color) = (.11,0,0,.5)
		// Color blended with the 'Filling' region
		_PosColor ("Positive Color Tint (RGB * A)", Color) = (0,.22,.66,.5)
		// Color blended with the 'Emptying' region
		_NegColor ("Negative Color Tint (RGB * A)", Color) = (.66,.22,0,.35)
		
		// How much of the Detail layers is always added to the result.
		// Using this and _TintMultiplier, you can balance the colors
		// Between the detail textures and the Region Tint colors very easily.
		_LayerBlend ("Layer Blend Amount", Range(0, .2)) = .1
		// Multiplier for the 4 above tint colors. Allows for HDR.
		_TintMultiplier ("Tint Multiplier", Range(0, 8)) = 1
		
		// First detail texture
		[NoScaleOffset] _LayerOne ("Detail Layer One", 2D) = "white" {}
		// Color blended with first detail texture
		_LayerOneColor ("Layer One Color (RGB * A)", Color) = (1,1,1,.21)
		// Movement of First Detail Texture
		// x = constant x-panning over time
		// y = amplitude of sin(time) panning on y axis
		// z = frequency of sin(time) panning on y axis
		// w = relative scale of texture
		_LayerOneScale ("Layer One Movement", Vector) = (-.2, .3, .1, .5)
		
		// Second detail texture
		[NoScaleOffset] _LayerTwo ("Detail Layer Two", 2D) = "white" {}
		// Color blended with second detail texture
		_LayerTwoColor ("Layer Two Color (RGB * A)", Color) = (1,1,1,.21)
		// Movement of Second Detail Texture
		// x = constant x-panning over time
		// y = amplitude of sin(time) panning on y axis
		// z = frequency of sin(time) panning on y axis
		// w = relative scale of texture
		_LayerTwoScale ("Layer Two Movement", Vector) = (-.1, .2, .2, .3)
		
		// Everything below is to work with UnityUI system
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
		_ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader {
		Tags { 
			"Queue"="Overlay" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass {
			CGPROGRAM
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0

				#include "UnityCG.cginc"
				#include "UnityUI.cginc"

				#pragma multi_compile __ UNITY_UI_ALPHACLIP
				#pragma multi_compile __ SCREENSPACE_TEXTURES
				#pragma multi_compile __ LEFT_TO_RIGHT
				
				struct appdata_t {
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					fixed4 color    : COLOR;
					half2 texcoord  : TEXCOORD0;
					float4 worldPosition : TEXCOORD1;
					
				};
				
				float4 _SizeInfo;
				float _Fill;
				float _Delta;
				
				sampler2D _MainTex;
				fixed4 _Color;
				fixed4 _BorderColor;
				float _Border;
				float _EdgeGlow;
				float4 _Wavyness;
				float _WavePan;
				
				float4 _BGColor;
				float4 _FillColor;
				float4 _EmptyColor;
				float4 _PosColor;
				float4 _NegColor;
				
				float _LayerBlend;
				float _TintMultiplier;
				
				sampler2D _LayerOne;
				float4 _LayerOneColor;
				float4 _LayerTwoColor;
				
				sampler2D _LayerTwo;
				float4 _LayerOneScale;
				float4 _LayerTwoScale;
				
				fixed4 _TextureSampleAdd;
				float4 _ClipRect;
#if UNITY_VERSION < 530
				bool _UseClipRect;
#endif
				v2f vert(appdata_t IN, out float4 outpos : SV_POSITION) {
					v2f OUT;
					OUT.worldPosition = IN.vertex;
					
					outpos = UnityObjectToClipPos(OUT.worldPosition);
					OUT.texcoord = IN.texcoord;
					
					
					#ifdef UNITY_HALF_TEXEL_OFFSET
					OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
					#endif
					
					
					OUT.color = IN.color;
					return OUT;
				}


				fixed4 frag(v2f IN, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target {
					float aspect = _ScreenParams.x / _ScreenParams.y;
					half4 inputColor = IN.color;
					half4 barColor = tex2D(_MainTex, IN.texcoord);
					float time = _Time.y;
					
					float2 scrollOne = float2(_LayerOneScale.x * time, _LayerOneScale.y * sin(time * _LayerOneScale.z));
					float2 scrollTwo = float2(_LayerTwoScale.x * time, _LayerTwoScale.y * sin(time * _LayerTwoScale.z));
					#ifdef SCREENSPACE_TEXTURES
					float2 screenP = screenPos * .03;
					half4 layerOne = tex2D(_LayerOne, scrollOne + screenP.xy * _LayerOneScale.w);
					half4 layerTwo = tex2D(_LayerTwo, scrollTwo + screenP.xy * _LayerTwoScale.w);
					#else
					float2 uv = _SizeInfo.xy * IN.texcoord;
					half4 layerOne = tex2D(_LayerOne, scrollOne + uv * _LayerOneScale.w);
					half4 layerTwo = tex2D(_LayerTwo, scrollTwo + uv * _LayerTwoScale.w);
					
					#endif
					
					half4 blendColor;
					
					float fn = _Fill;
					float ft = _Fill - _Delta;
					float fmin = min(fn, ft);
					float fmax = max(fn, ft);
					
					float y = IN.texcoord.y;
					#ifdef LEFT_TO_RIGHT
					float p = IN.texcoord.x;
					#else
					float p = 1.0f - IN.texcoord.x;
					#endif
					
					float dborderlerp = 0; 
					if (_Border > 0) { 
						float dborderx = min(p, abs(1.0-p)) * _SizeInfo.x / 4.0;
						float dbordery = min(y, abs(1.0-y)) * _SizeInfo.y / 4.0;
						float dborder = min(dborderx, dbordery);
						if (dborder < .000001) { return _BorderColor; }
						//if (dborder < .0001) { return _BorderColor; }
					
						dborderlerp = _Border / dborder; 
						if (dborderlerp > 1) { return _BorderColor; }
					}
					
					
					float w_freq = 3.0 * _Wavyness.y;
					float w_amp = 0.01 * _Wavyness.x;
					float w_speed = .5 * _Wavyness.z;
					float w_wobble = .5 * _Wavyness.w;
					p += sin(y * w_freq + _WavePan * time + sin(time * w_speed) * w_wobble) * w_amp;
					
					float dmin = abs(p-fmin);
					float dmax = abs(p-fmax);
					
					
					if (p < fmin) { blendColor = _FillColor; }
					else if (p < fmax) { blendColor = (_Delta > 0) ? _PosColor : _NegColor; }
					else { blendColor = _EmptyColor; }
					
					half4 color = barColor * inputColor;
					color.rgb *= _BGColor.rgb * _BGColor.a * 4.0;
					half3 lone = _LayerOneColor.rgb; 
					half3 ltwo = _LayerTwoColor.rgb;
					
					color.rgb += layerOne * lone * _LayerOneColor.a * 4.0;
					color.rgb += layerTwo * ltwo * _LayerTwoColor.a * 4.0;
					
					color.rgb *= half3(_LayerBlend, _LayerBlend, _LayerBlend) + blendColor.rgb * (blendColor.a * _TintMultiplier);
					
					color *= _Color;
					
					if (_EdgeGlow > 0) {
						
						float3 fillC = _FillColor.rgb; fillC = fillC * fillC;
						float3 emptyC = _EmptyColor.rgb; emptyC = emptyC * emptyC;
						
						color.rgb += (_EdgeGlow) / dmin * fillC * _FillColor.a;
						color.rgb += (_EdgeGlow) / dmax * emptyC * _EmptyColor.a;
					}
					color = lerp(color, _BorderColor, dborderlerp);
				#if UNITY_VERSION < 530
					if (_UseClipRect) {
						color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
					}
				#else
					color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif
					
					#ifdef UNITY_UI_ALPHACLIP
					clip (color.a - 0.001);
					#endif

					return clamp(color, half4(0,0,0,0), half4(1,1,1,1));
				}
			ENDCG
		}
	}
}









