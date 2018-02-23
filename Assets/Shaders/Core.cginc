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

inline fixed4 Sample3PointAmbient(float3 worldNormal)
{
	fixed4 o;
	fixed nl = dot(worldNormal, float3(0.0, 1.0, 0.0)); // -1..1
	o.rgb = lerp(unity_AmbientGround.rgb, unity_AmbientEquator.rgb, min(1.0, 1.0 + nl));
	o.rgb = lerp(o.rgb, unity_AmbientSky.rgb, max(0.0, nl)).rgb;
	o.a = 1.0;
	return saturate(o);
}

inline float LightModel(float nl)
{
	return nl;
}

inline void ComputeLight(int index, float3 viewPos, float3x3 tbn, inout float3 color0, inout float3 color1, inout float3 color2)
{
	float4 lightPos = LIGHT_POS(index);
	float3 lightDir = lightPos.xyz - viewPos*lightPos.w;

	float sqrDist = SQUARED_DIST(lightDir);
	lightDir = NORMALIZE(lightDir,sqrDist);
	lightDir = TRANSFORM_TBN(tbn,lightDir);

	float atten = LINEAR_ATTEN(LIGHT_ATTEN(index).z, sqrDist);

	// TODO: Inject a lighting model that isn't just Lambert
	color0 += dot(lightDir, BASIS_0) * LIGHT_COLOR(index) * atten;
	color1 += dot(lightDir, BASIS_1) * LIGHT_COLOR(index) * atten;
	color2 += dot(lightDir, BASIS_2) * LIGHT_COLOR(index) * atten;
}



#endif // COLOURMATH_CORE_INCLUDED
