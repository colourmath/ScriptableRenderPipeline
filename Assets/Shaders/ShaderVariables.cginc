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

#ifndef COLOURMATH_SHADERVARIABLES_INCLUDED
#define COLOURMATH_SHADERVARIABLES_INCLUDED

// Half-Life 2 Basis: http://www.valvesoftware.com/publications/2004/GDC2004_Half-Life2_Shading.pdf
#define BASIS_0 float3(	-1.0/sqrt(6.0),		-1.0/sqrt(2.0),		1.0/sqrt(3.0))
#define BASIS_1 float3(	-1.0/sqrt(6.0),		1.0/sqrt(2.0),		1.0/sqrt(3.0))
#define BASIS_2 float3(	sqrt(3.0/2.0),		0.0,				1.0/sqrt(3.0))

// Global Lighting Data
CBUFFER_START(GlobalLightData)
	// view-space lighting 
    fixed4 globalLightColors[8];
    float4 globalLightPositions[8];
    float4 globalLightAtten[8];
    int4  globalLightCount;
CBUFFER_END

// Convenience macros
#define LIGHT_POS(id)		globalLightPositions[id]
#define LIGHT_COLOR(id)		globalLightColors[id]
#define LIGHT_ATTEN(id)		globalLightAtten[id]
#define LIGHT_COUNT			globalLightCount.x

CBUFFER_START(AmbientLightData)
	fixed4 ambientLightSky;
	fixed4 ambientLightHorizon;
	fixed4 ambientLightGround;
CBUFFER_END

// Convenience macros
#define AMBIENT_SKY				ambientLightSky
#define AMBIENT_HORIZON			ambientLightHorizon
#define AMBIENT_GROUND			ambientLightGround

CBUFFER_START(ShadowData)
	sampler2D shadowTexture;
	float4x4 shadowMatrices[4];
	int shadowCount;
	float4 shadowDistances;
	float4 shadowBiases;
	half shadowIntensity;
CBUFFER_END

#define SHADOW_COUNT			shadowCount

static const float2 shadowTexOffsets[4] = 
{
	float2(0,0),
	float2(.5,0),
	float2(.5,.5),
	float2(0,.5)
};

int shadowIndex;

static const int shadowMask[4] = 
{
	1 << 0,
	1 << 1,
	1 << 2,
	1 << 3
};

// Convenience macros
#define	SHADOW_TEX				shadowTexture
#define	SHADOW_MATRIX(id)		shadowMatrices[id]
#define SHADOW_TEX_OFFSET(id)	shadowTexOffsets[id]	

#define RIGHT					float4(1,0,0,0)
#define UP						float4(0,1,0,0)
#define FORWARD					float4(0,0,1,0)
#define ORIGIN					float4(0,0,0,1)

#define RED						half4(1,0,0,1)
#define GREEN					half4(0,1,0,1)
#define BLUE					half4(0,0,1,1)
#define WHITE					half4(1,1,1,1)
#define BLACK					half4(0,0,0,1)

#endif // COLOURMATH_SHADERVARIABLES_INCLUDED
