Shader "Diffuse Splat" {
	Properties {
		_Color("Base Blend Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color1("Layer 1 Blend Color", Color) = (1,1,1,1)
		_Layer1("Layer 1", 2D) = "black" {}
		_Color2("Layer 2 Blend Color", Color) = (1,1,1,1)
		_Layer2("Layer 2", 2D) = "black" {}
		_Color3("Layer 3 Blend Color", Color) = (1,1,1,1)
		_Layer3("Layer 3", 2D) = "black" {}
		_Splat("Splatmap ", 2D) = "red" {}
		_UVScaling ("Layer Scales", Vector) = (1,1,1,1)
		_CliffColor("Cliff Blend Color", Color) = (.2,.2,.2,1)
		_CliffTex  ("Cliff (RGB)", 2D) = "gray" {}
		_CliffScaling ("Cliff Scaling", Vector) = (.0625, .25, 1, 4)
		
		// Tri-planar scaling 
		_Scale ("Texture Scale", Float) = 1
		_Offset ("Texture Offset (xyz * w)", Vector) = (0, 0, 0, 1)
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Lambert vertex:vert fullforwardshadows

		half4 _Color;
		half4 _Color1;
		half4 _Color2;
		half4 _Color3;
		sampler2D _MainTex;
		sampler2D _Layer1;
		sampler2D _Layer2;
		sampler2D _Layer3;
		sampler2D _Splat;
		sampler2D _CliffTex;
		float4 _UVScaling;
		float4 _CliffScaling;
		half4 _CliffColor;
		
		float _Scale;
		float4 _Offset;
		
		struct Input {
			float2 uv_MainTex;
			float2 uv_Splat;
			
			float3 worldPos;
			float3 viewDir;
			float3 wNormal;
			// float3 wTangent;
			// float3 wBinormal;
		};
		
		void vert(inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			data.worldPos = v.vertex;
			data.viewDir = WorldSpaceViewDir(v.vertex);
			
			float3x3 o2w = (float3x3)unity_ObjectToWorld;
			data.wNormal = normalize(mul(o2w, v.normal));
			// data.wTangent = normalize(mul(o2w, v.tangent));
			// data.wBinormal = cross(data.wNormal, data.wTangent);
		}
		
		void surf(Input IN, inout SurfaceOutput o) {
			
			const float4 wPos = float4(IN.worldPos, 1);
			#ifdef WORLDSPACE
				const float4 pos = wPos;
			#else
				const float4 pos = mul(unity_WorldToObject, wPos);
			#endif
			
			const float3 offset = _Offset.xyz * _Offset.w;
			half3 blend = pow(abs(IN.wNormal), _CliffScaling.z);
			const half sum = (blend.x + blend.y + blend.z); 
			if (sum != 0) { blend /= sum; }
			
			const float3 coords = pos * _Scale + offset;
			
			
			half4 c0 = tex2D(_MainTex, IN.uv_MainTex * _UVScaling.x);
			half4 c1 = tex2D(_Layer1, IN.uv_MainTex * _UVScaling.y);
			half4 c2 = tex2D(_Layer2, IN.uv_MainTex * _UVScaling.z);
			half4 c3 = tex2D(_Layer3, IN.uv_MainTex * _UVScaling.w);
			half4 splat = tex2D(_Splat, IN.uv_Splat);
			
			half4 cx = tex2D(_CliffTex, coords.zy * _CliffScaling.x) * _CliffColor;
			half4 cz = tex2D(_CliffTex, coords.xy * _CliffScaling.x) * _CliffColor;
			
			half4 cy = 
				c0 * splat.r * _Color
				+ c1 * splat.g * _Color1
				+ c2 * splat.b * _Color2
				+ c3 * splat.a * _Color3
			;
			
			half4 c = cx * blend.x + cy * blend.y + cz * blend.z;


			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		
		ENDCG
	}
	
	
	FallBack "Diffuse"
}
