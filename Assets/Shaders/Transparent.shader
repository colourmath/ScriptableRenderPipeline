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

Shader "ColourMath/Transparent"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Albedo Tint", Color) = (1,1,1,1)

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

		[Header(Fog Settings)]
		[Toggle]
		OVERRIDE_FOG("Override Fog", Float) = 0
		_LocalFogColor("Local Fog Color", Color) = (0,0,0,0)
	}

	CGINCLUDE
		#pragma target 2.0
		#pragma shader_feature __ OVERRIDE_FOG_ON
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Transparent" }
		LOD 100

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

			#include "Core.cginc"

			struct a2v
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half3 uv : TEXCOORD0;
				half4 color : COLOR0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			half4 _Color;

			v2f vert(a2v v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				float3 viewPos = UnityObjectToViewPos(v.vertex).xyz;
				o.uv.z = CALCULATE_LINEAR_FOG(-viewPos.z); 
				o.color = v.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv.xy) * i.color;
				c.rgb = lerp(c.rgb, FOG_COLOR, i.uv.z);
				return c;
			}

			ENDCG
		}
	}
}
