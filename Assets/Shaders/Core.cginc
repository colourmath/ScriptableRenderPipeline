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

#ifndef COLOURMATH_CORE_INCLUDED
#define COLOURMATH_CORE_INCLUDED

#include "UnityCG.cginc"
#include "ShaderVariables.cginc"

//#pragma multi_compile __ SHADE_GOURAUD // Pixel or Vertex Shading?

#define SQUARED_DIST(v) dot(v,v)
#define NORMALIZE(v,sqrDist) v * rsqrt(sqrDist)  

#define V2F_TBN half3x3(NORMALIZE(i.tbn[0].xyz, i.tbn[0].w),NORMALIZE(i.tbn[1].xyz, i.tbn[1].w),NORMALIZE(i.tbn[2].xyz, i.tbn[2].w))

#define LINEAR_ATTEN(atten, sqrDist) max(0.0, 1.0 / (1.0 + sqrDist*atten))

// Scalar Powers
inline half Pow2(half n)
{
	return n*n;
}
inline half Pow4(half n)
{
	n = n*n;	// ^2
	return n*n; // ^4
}
inline half Pow8(half n)
{
	n = n*n;	// ^2
	n = n*n;	// ^4
	return n*n; // ^8
}
inline half Pow16(half n)
{
	n = n*n;	// ^2
	n = n*n;	// ^4
	n = n*n;	// ^8
	return n*n; // ^16
}

// selected by shader feature
#if defined(SPEC_POW_2)
	#define POW(n) Pow2(n)
#elif defined(SPEC_POW_4)
	#define POW(n) Pow4(n)
#elif defined(SPEC_POW_8)
	#define POW(n) Pow8(n)
#elif defined(SPEC_POW_16)
	#define POW(n) Pow16(n)
#endif

inline half3 RadiosityNormalMap(half3 color0, half3 color1, half3 color2, half3 normal)
{
	half3 color =
		color0 * dot( BASIS_0, normal ) +
		color1 * dot( BASIS_1, normal ) +
		color2 * dot( BASIS_2, normal );
	return color;
}

inline float3 TransformDirectionTBN(float3 tangent, float3 bitangent, float3 normal, float3 dir)
{
	fixed4 o;
	o.x = dot(dir, tangent);
	o.y = dot(dir, bitangent);
	o.z = dot(dir, normal);
	return o;
}

#define TRANSFORM_TBN(tbn,v) TransformDirectionTBN(tbn[0],tbn[1],tbn[2],v)

inline fixed4 Sample3PointAmbient(float3 normal)
{
	float3 viewWorldUp = float3(UNITY_MATRIX_V[0].y,UNITY_MATRIX_V[1].y,UNITY_MATRIX_V[2].y);
	
	fixed4 o;
	fixed nl = dot(normal, viewWorldUp); // -1..1
	o.rgb = lerp(AMBIENT_GROUND.rgb, AMBIENT_HORIZON.rgb, min(1.0, 1.0 + nl));
	o.rgb = lerp(o.rgb, AMBIENT_SKY.rgb, max(0.0, nl)).rgb;
	o.a = 1.0;
	return saturate(o);
}

inline float LightModel(float nl)
{
	return nl;
}

half ComputeShadow(
	half2 shadowUV, 
	sampler2D shadowSampler, 
	half vertDepth, 
	half bias, 
	half mask,
	half falloff)
{
	const half depthScale = 32.0;

	half2 depthTex;
	half depth;

	depthTex = tex2D(shadowSampler, shadowUV).rg;
	depth = depthTex.r * depthScale + bias;
					 
	half depthDelta = depth - vertDepth;
	half fade = saturate(1.0 + depthDelta * falloff) * shadowIntensity;
	half depthDeltaScaled = saturate(16.0 * depthDelta);

	half atten = max(0.0,vertDepth * shadowDistances[0]);
				
	half shadow = 1.0 - depthTex.g + depthDeltaScaled * depthTex.g;
				
	shadow = saturate(shadow+mask+atten); // prevent self-shadowing artifacts
	return shadow * fade + 1.0 - fade;

}

inline void ComputeLight(
	int index, 
	float3 viewPos, 
	float3x3 tbn, 
	inout float3 color0, 
	inout float3 color1, 
	inout float3 color2, 
	inout float3 specColor,
	float3 specTint)
{
	float4 lightPos = LIGHT_POS(index);
	float3 lightDir = lightPos.xyz - viewPos*lightPos.w;

	// View direction is always forward since we're in View-Space, calculate our 'h' vector
	float3 h = lightDir + FORWARD.xyz;
	h = NORMALIZE(h, SQUARED_DIST(h));

	float sqrDist = SQUARED_DIST(lightDir);
	lightDir = NORMALIZE(lightDir,sqrDist);
	lightDir = TRANSFORM_TBN(tbn,lightDir);

	float atten = LINEAR_ATTEN(LIGHT_ATTEN(index).z, sqrDist);

	// TODO: Inject a lighting model that isn't just Lambert
	color0 += max(0.0, dot(lightDir, BASIS_0) * LIGHT_COLOR(index) * atten);
	color1 += max(0.0, dot(lightDir, BASIS_1) * LIGHT_COLOR(index) * atten);
	color2 += max(0.0, dot(lightDir, BASIS_2) * LIGHT_COLOR(index) * atten);

	// Gouraud Blinn Specular approximation
	specColor += Pow16(max(0.0,dot(h, tbn[2]))) * LIGHT_COLOR(index) * specTint * atten;
}

inline void AmbientContribution(
	half3 ambient, 
	inout half3 color0, 
	inout half3 color1, 
	inout half3 color2)
{
	color0 += ambient;
	color1 += ambient;
	color2 += ambient;
}

float2 LightmapTexcoord(float2 texcoord)
{
	return texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
}

#endif // COLOURMATH_CORE_INCLUDED
