///clip.cginc
///cginclude file for 'clip' recolor shaders
///Jonathan Cohen
///As-Is, Royalty free.

#ifndef COLOR_CLIP_INCLUDED
#define COLOR_CLIP_INCLUDED
//Properties Block should contain
/*
		[Header(Color Clip Regions)]
		[NoScaleOffset] _Clip ("Clip", 2D) = "black" {}
		_Color ("Base Color (Black Clip)", Color) = (1,1,1,1)
		_Color1 ("Color 1 (Red Clip)", Color) = (1,1,1,1)
		_Color2 ("Color 2 (Green Clip)", Color) = (1,1,1,1)
		_Color3 ("Color 3 (Blue Clip)", Color) = (1,1,1,1)
*/

fixed4 _Color;
fixed4 _Color1;
fixed4 _Color2;
fixed4 _Color3;
sampler2D _Clip;
#ifdef HSV_INCLUDED
//Properties block should contain all of:
/*
		[Header(Color Clip Regions)]
		[NoScaleOffset] _Clip ("Clip", 2D) = "black" {}
		
		// Base Settings (Black Clip)
		[Header(Base)]
		_Color ("Main Color (Color 0) (Black) ", Color) = (0.5,0.5,0.5,1)
		_Saturation ("Saturation (Black)", Range(0, 5)) = 1.0 
		_HueShift ("Hue Shift (Black)", Range(0, 1)) = 0.0
		
		// Group 1 (Red)
		[Header(Red)]
		_Color1 ("Color 1 (Red)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation1 ("Saturation 1 (Red)", Range(0, 5)) = 1.0 
		_HueShift1 ("Hue Shift (Red)", Range(0, 1)) = 0.0
		
		// Group 2 (Green)
		[Header(Green)]
		_Color2 ("Color 2 (Green)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation2 ("Saturation (Green)", Range(0, 5)) = 1.0 
		_HueShift2 ("Hue Shift (Green)", Range(0, 1)) = 0.0
		
		// Group 3 (Blue)
		[Header(Blue)]
		_Color3 ("Color 3 (Blue)", Color) = (0.5, 0.5, 0.5, 1)
		_Saturation3 ("Saturation (Blue)", Range(0, 5)) = 1.0 
		_HueShift3 ("Hue Shift (Blue)", Range(0, 1)) = 0.0
*/
float _Saturation;
float _HueShift;

float _Saturation1;
float _HueShift1;

float _Saturation2;
float _HueShift2;

float _Saturation3;
float _HueShift3;

//Hue/saturation shift version
inline float4 apply_clip(float4 c, float2 uv) {
	half4 clip = tex2D(_Clip, uv);
	half clipTot = clip.r + clip.g + clip.b;
	if (clipTot > 1) {
		clip.r /= clipTot;
		clip.g /= clipTot;
		clip.b /= clipTot;
	}
	
	float4 blendColor = _Color;
	blendColor = lerp(blendColor, _Color1, clip.r);
	blendColor = lerp(blendColor, _Color2, clip.g);
	blendColor = lerp(blendColor, _Color3, clip.b);
	
	c.rgba *= blendColor.rgba;
	float3 hsv = toHSV(c.rgb);
	
	if (hsv.g > 0) {
		float hueShift = _HueShift;
		hueShift = lerp(hueShift, _HueShift1, clip.r);
		hueShift = lerp(hueShift, _HueShift2, clip.g);
		hueShift = lerp(hueShift, _HueShift3, clip.b);
		
		float saturation = _Saturation;
		saturation = lerp(saturation, _Saturation1, clip.r);
		saturation = lerp(saturation, _Saturation2, clip.g);
		saturation = lerp(saturation, _Saturation3, clip.b);
		
		hsv.r = frac(hsv.r + hueShift);
		hsv.g *= saturation;
		c.rgb = saturate(toRGB(hsv));
	}
	return c;
}
#else

//Normal version
inline float4 apply_clip(float4 c, float2 uv) {
	half4 clip = tex2D(_Clip, uv);
	half clipTot = clip.r + clip.g + clip.b;
	if (clipTot > 1) {
		clip.r /= clipTot;
		clip.g /= clipTot;
		clip.b /= clipTot;
	}
	
	float4 blendColor = _Color;
	blendColor = lerp(blendColor, _Color1, clip.r);
	blendColor = lerp(blendColor, _Color2, clip.g);
	blendColor = lerp(blendColor, _Color3, clip.b);
	
	c.rgba *= blendColor.rgba;
	return c;
}


#endif

#endif