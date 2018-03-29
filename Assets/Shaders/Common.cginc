/**
* MIT License
*
* Copyright (c) 2018 Joseph Pasek
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
**/

#ifndef COLOURMATH_COMMON_INCLUDED
#define COLOURMATH_COMMON_INCLUDED

#include "Core.cginc"

struct a2v
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;
	float3 normal : NORMAL;
	#if defined(NORMAL_ON)
		float4 tangent : TANGENT;
	#endif
	float4 color : COLOR; // vertex color
};

struct v2f
{
	float4 vertex : SV_POSITION;
	fixed4 color : COLOR0;
	float4 uv : TEXCOORD0;
	float4 viewDir : TEXCOORD1;
	#if defined(NORMAL_ON)
		half4 lighting[3] : TEXCOORD2; // 2..4
		half4 specColor : COLOR1;
	#else
		float3 normal : TEXCOORD2;
	#endif

	// Shadowmap needs 1/2 of a TEXCOORD, do perspective division per-vertex
	half4 shadowCoords[2] : TEXCOORD5; // 5..6 we can support up-to 4 shadow maps
	half4 shadowDepths : TEXCOORD7; // store depth component for 4 shadows 
};

sampler2D _MainTex;
float4 _MainTex_ST;

half4 _SpecColor;
half4 _Color;

// TODO: Emission
half4 _EmissiveColor;

v2f vert (a2v v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	float4 worldNormal = mul(unity_ObjectToWorld, float4(v.normal,0.0));
	worldNormal.xyz = NORMALIZE(worldNormal.xyz, SQUARED_DIST(worldNormal.xyz));

	//////// Real-Time Shadows ////////
	#if defined(RECEIVE_SHADOWS)
		// Shadow 1
		float4 shadowCoord = mul(shadowMatrices[0], worldPos);
		o.shadowCoords[0].xy = shadowCoord.xy;
		#if defined(SHADOW_PROJECTION_ORTHO)
			o.shadowDepths[0] = (shadowCoord.z / shadowCoord.w  * .5 + .5);
		#else
			o.shadowDepths[0] = shadowCoord.w;
		#endif
		// Shadow 2
		shadowCoord = mul(shadowMatrices[1], worldPos);
		o.shadowCoords[0].zw = shadowCoord.xy;
		#if defined(SHADOW_PROJECTION_ORTHO)
			o.shadowDepths[1] = (shadowCoord.z / shadowCoord.w  * .5 + .5);
		#else
			o.shadowDepths[1] = shadowCoord.w;
		#endif
		// Shadow 3
		shadowCoord = mul(shadowMatrices[2], worldPos);
		o.shadowCoords[1].xy = shadowCoord.xy;
		#if defined(SHADOW_PROJECTION_ORTHO)
			o.shadowDepths[2] = (shadowCoord.z / shadowCoord.w  * .5 + .5);
		#else
			o.shadowDepths[2] = shadowCoord.w;
		#endif
		// Shadow 4
		shadowCoord = mul(shadowMatrices[3], worldPos);
		o.shadowCoords[1].zw = shadowCoord.xy;
		#if defined(SHADOW_PROJECTION_ORTHO)
			o.shadowDepths[3] = (shadowCoord.z / shadowCoord.w  * .5 + .5);
		#else
			o.shadowDepths[3] = shadowCoord.w;
		#endif
	#endif // RECEIVE_SHADOWS

	float3 viewPos = UnityObjectToViewPos(v.vertex).xyz;
	float4 normal = float4(v.normal, 0.0);

	o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	o.uv.zw = LightmapTexcoord(v.uv2);
	o.color = v.color * _Color;

	#if defined(NORMAL_ON)
		float3 viewTangent =	mul(UNITY_MATRIX_IT_MV, v.tangent).xyz;
		float3 viewNormal =		mul(UNITY_MATRIX_IT_MV, normal).xyz;
		float3 viewBitan =		cross(viewNormal, viewTangent) * v.tangent.w;

		float3x3 tbn = float3x3(viewTangent, viewBitan, viewNormal);

		half4 lighting0 = 0;
		half4 lighting1 = 0;
		half4 lighting2 = 0;
		half4 specColor = 0;
		for(int i = 0; i < LIGHT_COUNT; i++)
			ComputeLight(
				i, 
				viewPos, 
				tbn, 
				lighting0.xyz, 
				lighting1.xyz, 
				lighting2.xyz, 
				specColor.xyz,
				_SpecColor.rgb);

		// Ambient lighting shouldn't be accumulated with mixed lighting as it already
		// is included in the light maps
	#if defined(LIGHTING_DYNAMIC)
		half3 ambient = Sample3PointAmbient(viewNormal);
		// Add lightprobes into Ambient Lighting
		ambient += ShadeSH9(worldNormal);
		AmbientContribution(ambient, lighting0.rgb, lighting1.rgb, lighting2.rgb);
	#endif

		float3 viewDir = TransformDirectionTBN(tbn[0], tbn[1], tbn[2], FORWARD.xyz);
		o.viewDir.xyz = NORMALIZE(viewDir, SQUARED_DIST(viewDir));
		// Pack Fog coordinate into our view direction interpolator
		o.viewDir.w = CALCULATE_LINEAR_FOG(-viewPos.z); 

		o.lighting[0] = lighting0;
		o.lighting[1] = lighting1;
		o.lighting[2] = lighting2;
		// TODO: Spec Color value still has a 0..1 value available in alpha component
		o.specColor = specColor;

		#if defined(CUBE_REFLECTIONS)
			half3 viewWorld = worldPos - _WorldSpaceCameraPos;
			half3 reflection = viewWorld - 2.0*dot(worldNormal, viewWorld) * worldNormal;
			
			// Pack reflection vector across our lighting values
			o.lighting[0].w = reflection.x;
			o.lighting[1].w = reflection.y;
			o.lighting[2].w = reflection.z;
		#endif
	#else
		o.normal = mul(UNITY_MATRIX_IT_MV, normal);
	#endif

	return o;
}
			
samplerCUBE _CubeTex;

sampler2D _NormalTex;
fixed _ShadowFalloff;
fixed _ShadowIntensity;

fixed4 frag (v2f i) : SV_Target
{
	// sample the texture
	fixed4 col = tex2D(_MainTex, i.uv) * i.color;
	// Real-Time Shadows
	#if defined(RECEIVE_SHADOWS)
		half shadow = 1;
		half2 coord;

		// Shadow 1
		coord = i.shadowCoords[0].xy / i.shadowDepths[0];
		coord = coord * .5 + .5;
		coord = coord * .5 + shadowTexOffsets[0];
		coord = clamp(coord, shadowClamps[0].xy,shadowClamps[0].zw);

		shadow *= ComputeShadow(
			coord, 
			shadowTexture, 
			i.shadowDepths[0], 
			shadowBiases[0], 
			EVAL_SHADOWMASK(0),
			shadowDistances[0],
			_ShadowFalloff);

		// Shadow 2
		coord = i.shadowCoords[0].zw / i.shadowDepths[1];
		coord = coord * .5 + .5;
		coord = coord * .5 + shadowTexOffsets[1];
		coord = clamp(coord, shadowClamps[1].xy,shadowClamps[1].zw);

		shadow *= ComputeShadow(
			coord, 
			shadowTexture, 
			i.shadowDepths[1], 
			shadowBiases[1], 
			EVAL_SHADOWMASK(1),
			shadowDistances[1],
			_ShadowFalloff);

		// Shadow 3
		coord = i.shadowCoords[1].xy / i.shadowDepths[2];
		coord = coord * .5 + .5;
		coord = coord * .5 + shadowTexOffsets[2];
		coord = clamp(coord, shadowClamps[2].xy,shadowClamps[2].zw);

		shadow *= ComputeShadow(
			coord, 
			shadowTexture, 
			i.shadowDepths[2], 
			shadowBiases[2], 
			EVAL_SHADOWMASK(2),
			shadowDistances[2],
			_ShadowFalloff);

		// Shadow 4
		coord = i.shadowCoords[1].zw / i.shadowDepths[3];
		coord = coord * .5 + .5;
		coord = coord * .5 + shadowTexOffsets[3];
		coord = clamp(coord, shadowClamps[3].xy, shadowClamps[3].zw);
	
		shadow *= ComputeShadow(
			coord, 
			shadowTexture, 
			i.shadowDepths[3], 
			shadowBiases[3],
			EVAL_SHADOWMASK(3),
			shadowDistances[3],
			_ShadowFalloff);
	#endif // RECEIVE_SHADOWS
	
	#if defined(NORMAL_ON)
		fixed3 n = UnpackNormal(tex2D(_NormalTex, i.uv.xy));
		n = NORMALIZE(n, SQUARED_DIST(n));

		fixed3 diffuse = RadiosityNormalMap(
			i.lighting[0].xyz,
			i.lighting[1].xyz,
			i.lighting[2].xyz,
			n);

		// Mixed Lighting should have a baked Lightmap
		#if defined(LIGHTING_MIXED)
			// TODO: This may not be necessary to gate behind a variant
			#if defined(LIGHTMAP_ON)
				diffuse += DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv.zw)).rgb;
			#endif
		#endif

		#if defined(RECEIVE_SHADOWS)
			diffuse *= shadow;
		#endif
		
		col.rgb *= diffuse;
					
		// Radiosity Blinn Specular 
		fixed3 spec = i.specColor.rgb;

		fixed3 reflDir = i.viewDir.xyz - 2.0 * dot(n, i.viewDir.xyz) * n;

		fixed3 sr;
		sr.x = POW(dot(BASIS_0, reflDir));
		sr.y = POW(dot(BASIS_1, reflDir));
		sr.z = POW(dot(BASIS_2, reflDir));
		spec *= sr.x + sr.y + sr.z; 

		#if defined(CUBE_REFLECTIONS)
			const half3 cubeDir = half3(i.lighting[0].w,i.lighting[1].w,i.lighting[2].w);
			fixed4 cube;
			#if defined(OVERRIDE_LOCAL_CUBEMAP)
				cube = texCUBE(_CubeTex, cubeDir+sr);
			#else
				cube = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, cubeDir+sr);
				cube.rgb = DecodeHDR(cube, unity_SpecCube0_HDR) * 0.5;
			#endif
			spec += cube.rgb;
		#endif
		
		col.rgb += spec * col.a; // For now force texture alpha to be spec map
	#endif

	col.rgb = lerp(col.rgb, FOG_COLOR, i.viewDir.w);
	return col;
}

#endif // COLOURMATH_COMMON_INCLUDED
