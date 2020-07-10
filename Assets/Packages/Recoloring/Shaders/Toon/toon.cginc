#ifndef TOONINC
#define TOONINC

/* Properties Block should contain

		// Toggle for lighting
		[Toggle(RC_UNLIT)] _Unlit("Actual Unlit (on) / Lit-Unlit (off)", Float) = 0
		// Used for Lit-Unlit lighting
		[Header(Outline)]
		_OutlineVal ("Base Thickness", Range(.001, 4.)) = .7
		_OutlineScreen ("Screen Height -> Thickness", Range(0, 2)) = .7
		_OutlineDist ("Distance -> Thickness", Range(0, 2)) = .7
		_OutlineCol ("Outline Color", Color) = (0,0,0,1)
		
		[Header(Lighting for Lit Unlit)]
		_WrapMin ("Min Light Wrap", Range(0,1)) = .3
		_WrapScale ("Light Wrap Power", Range(0,2)) = 1.5
		_AttenBoostMin ("Attenuation Boost", Range(0,1)) = .3
		_AttenScale ("Attenuation Power", Range(0,1)) = .6
		
		_RampBlend ("Light Ramp Blend", Range(0,1)) = .7
		_LitUnlitBlend ("Lit-Unlit blend", Range(0,1)) = .38
		_EnvironmentLight ("Environment Light", Range(0,1)) = .5
		
		
		// Lighting ramp
		[NoScaleOffset] _Ramp ("Lighting Ramp (RGB)", 2D) = "gray" {} 
		
		[Header(Rim)]
		// Color of rim lighting
		_RimColor ("Rim Color", Color) = (0.16,0.19,0.26,0.0)
		// Amount of rim lighting, Lower number = more effect
		_RimPower ("Rim Power", Range(0.1,8.0)) = 3.0
		
//*/

// Variables
float4 _RimColor;
float _RimPower;

float _EnvironmentLight;

float _AttenBoostMin;
float _AttenScale;
float _WrapMin;
float _WrapScale;
float _LitUnlitBlend;
float _RampBlend;

sampler2D _Ramp;

// Input struct for Surface function
struct Input {
	float2 uv_MainTex : TEXCOORD0;
	float3 wpos;
	float3 viewDir;
	float3 envLight;
};

// struct for surface/lighting functions
struct SurfaceOutputToon {
	fixed3 Albedo;
	fixed3 Normal;
	fixed3 Emission;
	half Specular;
	fixed Gloss;
	fixed Alpha;
	
	fixed Intensity;
};

void toonVert(inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input, o);
	
	o.wpos = mul(unity_ObjectToWorld, v.vertex);
	
	// The whole point of doing this is to cheat and get access
	// To the environment lighting inside the lighting function,
	// So it can be nullified during the base pass.
	#if defined(UNITY_PASS_FORWARDBASE)
		o.envLight = 0.0;
		float3 worldNormal = UnityObjectToWorldDir(v.normal);
		
		#ifndef LIGHTMAP_ON
		#if UNITY_SHOULD_SAMPLE_SH
			float3 sh = ShadeSH9(float4(worldNormal, 1.0));
			o.envLight = sh;
		#endif 
		
		#ifdef VERTEXLIGHT_ON
			o.envLight += Shade4PointLights (
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, o.wpos, worldNormal );
		#endif
		
		#endif
	#endif
	
}

inline half4 LightingToon(inout SurfaceOutputToon s, half3 lightDir, half3 viewDir, half atten) {
	#if RC_UNLIT
		return half4(0, 0, 0, 1);
	#else
		fixed rawAtten = atten;
		
		half NdL = dot(s.Normal, lightDir);
		
		#if SPOT
			// Don't wrap light with spotlights
			half diffWrap = NdL;
		#else
			half diffWrap = NdL * (2.0 - _WrapScale) + _WrapMin;
		#endif
			
		// Spotlights project a lot of light at 0 attenuation
		// Gotta check for and basically ignore such pixels.
		// This makes spotlights a little more expensive, 
		// Most compilers should be able to properly optimize this...
		#if SPOT
			if (atten < .01) { atten = 0; }
			else { atten = _AttenBoostMin + atten * (1.0-_AttenScale); }
		#else
			atten = _AttenBoostMin + atten * (1.0-_AttenScale);
		#endif
		
		//atten = clamp(atten, 0, 1);
		//half diffWrapClamp = clamp(diffWrap, 0, 1);
		half3 diff = s.Albedo * _LightColor0.rgb * diffWrap;
		half3 ramp = s.Albedo * tex2D(_Ramp, float2(diffWrap, 0.5)) * _LightColor0.rgb;
		half3 light = lerp(diff, ramp, _RampBlend) * atten;
		
		half4 c;
		
		#if defined(UNITY_PASS_FORWARDBASE)
			c.rgb = s.Albedo * _LitUnlitBlend;
			//c.rgb = lerp(light, unlit, _LitUnlitBlend);
		#else
			c.rgb = half3(0, 0, 0);
			//c.rgb = light * (1.0 - _LitUnlitBlend);
		#endif
		
		float3 lightApply = light * (1.0 - _LitUnlitBlend);
		#if SPOT
			//lightApply *= clamp(rawAtten, 0, 1) * 10.0;
			lightApply *= rawAtten;
		#endif
		
		#if POINT
			c.rgb += lightApply * clamp(s.Intensity, 0.0, 1.0);
		#else
			c.rgb += lightApply;
		#endif 
		
		c.a = s.Alpha;
		return c;
	#endif
}

// Applies part of the toon lighting effect during the surface shader.
inline void surf_toonPre(Input IN, inout SurfaceOutputToon o, half4 color) {
			
	half face = dot(normalize(IN.viewDir), o.Normal);
	half rim = 1.0 - saturate(abs(face));
	float3 rimC = _RimColor.rgb * pow(rim, _RimPower);
	
	#ifdef RC_UNLIT
		o.Albedo = 0.0;
		o.Emission = color.rgb + rimC;
	#else
		o.Albedo = color.rgb;
		o.Emission = rimC;
		
		#if defined(UNITY_PASS_FORWARDBASE)
			o.Emission -= IN.envLight * (1.0 - _EnvironmentLight);
		#endif
		
		o.Intensity = 1.0;
		#if POINT || SPOT
			// o.Intensity = 1.0 - length(_LightPositionRange.xyz - IN.wpos) * _LightPositionRange.w;
			o.Intensity = 1.0 - length(_LightPositionRange.xyz - IN.wpos) * _LightPositionRange.w;
			o.Emission = float4(0,0,0,0);
			o.Specular = float4(0,0,0,0);
		#endif
		
	#endif
}


void toonAdjust(Input IN, SurfaceOutputToon o, inout fixed4 color) {
	
}

#endif