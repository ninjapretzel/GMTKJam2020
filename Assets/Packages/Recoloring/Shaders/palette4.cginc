#ifndef PALETTE_4_INCLUDED
#define PALETTE_4_INCLUDED
//Properties Block should contain
/*
		[Header(Tolerance)]
		_Tolerance ("Search Tolerance", Range(.01, .65)) = .1
		_Strength ("Strength", Range(0.00, 4.00)) = 1.00
		
		[Header(Color 1)]
		_ColorSearch1 ("Search Color 1", Color) = (1, 0, 0, 1)
		_ColorTarget1 ("Target Color 1", Color) = (1, 0, 0, 1)
		
		[Header(Color 2)]
		_ColorSearch2 ("Search Color 2", Color) = (0, 1, 0, 1)
		_ColorTarget2 ("Target Color 2", Color) = (0, 1, 0, 1)
		
		[Header(Color 3)]
		_ColorSearch3 ("Search Color 3", Color) = (0, 0, 1, 1)
		_ColorTarget3 ("Target Color 3", Color) = (0, 0, 1, 1)
		
		[Header(Color 4)]
		_ColorSearch4 ("Search Color 4", Color) = (0, 0, 0, 1)
		_ColorTarget4 ("Target Color 4", Color) = (0, 0, 0, 1)
*/
fixed4 _ColorSearch1;
fixed4 _ColorSearch2;
fixed4 _ColorSearch3;
fixed4 _ColorSearch4;

fixed4 _ColorTarget1;
fixed4 _ColorTarget2;
fixed4 _ColorTarget3;
fixed4 _ColorTarget4;

float _Tolerance;
float _Strength;


inline float4 apply_palette(half4 c) {
	float d1 = distance(c, _ColorSearch1);
	float d2 = distance(c, _ColorSearch2);
	float d3 = distance(c, _ColorSearch3);
	float d4 = distance(c, _ColorSearch4);
	
	if (d1 < _Tolerance) { c = lerp(c, _ColorTarget1, (1 - d1 / _Tolerance) * _Strength); }
	if (d2 < _Tolerance) { c = lerp(c, _ColorTarget2, (1 - d2 / _Tolerance) * _Strength); }
	if (d3 < _Tolerance) { c = lerp(c, _ColorTarget3, (1 - d3 / _Tolerance) * _Strength); }
	if (d4 < _Tolerance) { c = lerp(c, _ColorTarget4, (1 - d4 / _Tolerance) * _Strength); }
	
	return c;
}


#endif