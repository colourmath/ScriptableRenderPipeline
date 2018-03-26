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

		[Enum(UnityEngine.Rendering.BlendMode)]
		__BlendSrc("Blend Src", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)]
		__BlendDst("Blend Dst", Float) = 10

		[Enum(UnityEngine.Rendering.CompareFunction)]
		__ZTest("Z Test", Float) = 0
		[ZPrime]
		__ZWrite("Z Prime", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)]
		__CullMode("Cull Mode",Float) = 2

		[Toggle]
		OVERRIDE_FOG("Override Fog", Float) = 0
		_LocalFogColor("Local Fog Color", Color) = (0,0,0,0)
	}

	CGINCLUDE
		#pragma target 2.0
		#pragma shader_feature SPEC_POW_2 SPEC_POW_4 SPEC_POW_8 SPEC_POW_16
		#pragma multi_compile __ LIGHTMAP_ON
		#pragma multi_compile __ SHADOW_PROJECTION_ORTHO
		#pragma shader_feature __ OVERRIDE_LOCAL_CUBEMAP
		#pragma shader_feature __ OVERRIDE_FOG_ON
		//#pragma debug // Uncomment to get better debug info in compiled shaders

		// The following defines are applied globally.
		#define SHADOW_MASK_BITWISE // Use '&' bitwise AND operator to do shadowmask
		#define RECEIVE_SHADOWS
	ENDCG

	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }
		LOD 100

		// TODO: This is not used, either remove it or find a purpose for itm maybe Baked objects?
		Pass
		{
			Tags { "LightMode" = "BasePass" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define NORMAL_ON
			#define SPECULAR_ON

			#include "Common.cginc"
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Dynamic" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Note: Hardcoded defines allow us to reuse large chunks of code without
			// paying for a variant. Since this is all handled on a per-Pass-basis anyway.
			#define LIGHTING_DYNAMIC
			#define NORMAL_ON
			#define SPECULAR_ON

			#include "Common.cginc"
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "DynamicReflective" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define LIGHTING_DYNAMIC
			#define NORMAL_ON
			#define SPECULAR_ON
			#define CUBE_REFLECTIONS

			#include "Common.cginc"
			
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

			#include "Common.cginc"
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "MixedReflective" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define LIGHTING_MIXED
			#define NORMAL_ON
			#define SPECULAR_ON
			#define CUBE_REFLECTIONS

			#include "Common.cginc"
			
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ZPrime" }
			ZWrite On
			ZTest [__ZTest]
			Cull [__CullMode]
			ColorMask 0
		}

		Pass
		{
			Tags { "LightMode" = "Transparent" }

			Blend [__BlendSrc] [__BlendDst]
			ZTest [__ZTest]
			ZWrite Off
			Cull [__CullMode]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#define LIGHTING_DYNAMIC
			#define NORMAL_ON
			#define SPECULAR_ON
			#define CUBE_REFLECTIONS

			#include "Common.cginc"
			
			ENDCG
		}
	}
	CustomEditor "ColourMath.BasicShaderGUI"
}
