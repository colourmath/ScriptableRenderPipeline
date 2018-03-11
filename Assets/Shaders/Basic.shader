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

Shader "ColourMath/Basic"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Albedo Tint", Color) = (1,1,1,1)
		[NoScaleOffset]
		_NormalTex ("Normal", 2D) = "bump" {}
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		[KeywordEnum(POW_2,POW_4,POW_8,POW_16)]
		SPEC ("Specular Power", Float) = 0

		_CubeTex("Reflection", CUBE) = "black" {}
		_ShadowFalloff("Shadow Falloff", Float) = 1
		_ShadowIntensity("Shadow Intensity", Float) = 1
	}

	CGINCLUDE
		#pragma target 2.0
		#pragma shader_feature SPEC_POW_2 SPEC_POW_4 SPEC_POW_8 SPEC_POW_16
		#pragma multi_compile __ LIGHTMAP_ON
		#pragma multi_compile __ SHADOW_PROJECTION_ORTHO
		#pragma shader_feature __ OVERRIDE_CUBE_REFLECTION_ON
		#pragma debug
	ENDCG


	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "BasePass" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define NORMAL_ON
			#define SPECULAR_ON

			#include "Shared.cginc"
			
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Mixed" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define LIGHTING_MIXED
			#define NORMAL_ON
			#define SPECULAR_ON

			#include "Shared.cginc"
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Dynamic" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define LIGHTING_DYNAMIC
			#define NORMAL_ON
			#define SPECULAR_ON

			#include "Shared.cginc"
			
			ENDCG
		}
	}
}
