#ifndef PALETTE_8_INCLUDED
#define PALETTE_8_INCLUDED
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
		
		[Header(Color 5)]
		_ColorSearch5 ("Search Color 5", Color) = (1, 1, 0, 1)
		_ColorTarget5 ("Target Color 5", Color) = (1, 1, 0, 1)
		
		[Header(Color 6)]
		_ColorSearch6 ("Search Color 6", Color) = (1, 0, 1, 1)
		_ColorTarget6 ("Target Color 6", Color) = (1, 0, 1, 1)
		
		[Header(Color 7)]
		_ColorSearch7 ("Search Color 7", Color) = (0, 1, 1, 1)
		_ColorTarget7 ("Target Color 7", Color) = (0, 1, 1, 1)
		
		[Header(Color 8)]
		_ColorSearch8 ("Search Color 8", Color) = (1, 1, 1, 1)
		_ColorTarget8 ("Target Color 8", Color) = (1, 1, 1, 1)
*/

fixed4 _ColorSearch1;
fixed4 _ColorSearch2;
fixed4 _ColorSearch3;
fixed4 _ColorSearch4;
fixed4 _ColorSearch5;
fixed4 _ColorSearch6;
fixed4 _ColorSearch7;
fixed4 _ColorSearch8;

fixed4 _ColorTarget1;
fixed4 _ColorTarget2;
fixed4 _ColorTarget3;
fixed4 _ColorTarget4;
fixed4 _ColorTarget5;
fixed4 _ColorTarget6;
fixed4 _ColorTarget7;
fixed4 _ColorTarget8;

float _Tolerance;
float _Strength;


inline float4 apply_palette(float4 c) {
	float d1 = distance(c, _ColorSearch1);
	float d2 = distance(c, _ColorSearch2);
	float d3 = distance(c, _ColorSearch3);
	float d4 = distance(c, _ColorSearch4);
	float d5 = distance(c, _ColorSearch5);
	float d6 = distance(c, _ColorSearch6);
	float d7 = distance(c, _ColorSearch7);
	float d8 = distance(c, _ColorSearch8);
	
	if (d1 < _Tolerance) { c = lerp(c, _ColorTarget1, (1 - d1 / _Tolerance) * _Strength); }
	if (d2 < _Tolerance) { c = lerp(c, _ColorTarget2, (1 - d2 / _Tolerance) * _Strength); }
	if (d3 < _Tolerance) { c = lerp(c, _ColorTarget3, (1 - d3 / _Tolerance) * _Strength); }
	if (d4 < _Tolerance) { c = lerp(c, _ColorTarget4, (1 - d4 / _Tolerance) * _Strength); }
	if (d5 < _Tolerance) { c = lerp(c, _ColorTarget5, (1 - d5 / _Tolerance) * _Strength); }
	if (d6 < _Tolerance) { c = lerp(c, _ColorTarget6, (1 - d6 / _Tolerance) * _Strength); }
	if (d7 < _Tolerance) { c = lerp(c, _ColorTarget7, (1 - d7 / _Tolerance) * _Strength); }
	if (d8 < _Tolerance) { c = lerp(c, _ColorTarget8, (1 - d8 / _Tolerance) * _Strength); }
	
	return c;
}


#endif