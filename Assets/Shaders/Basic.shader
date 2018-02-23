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
		_NormalTex ("Normal", 2D) = "bump" {}
	}
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
			#include "Core.cginc"

			struct a2v
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				#if defined(NORMAL_ON)
					float4 tangent : TANGENT;
				#endif
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewPos : TEXCOORD1;
				#if defined(NORMAL_ON)
					half4 color[3] : TEXCOORD2; // 2..4
				#else
					float3 normal : TEXCOORD2;
				#endif
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (a2v v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 viewPos = UnityObjectToViewPos(v.vertex).xyz;
				o.viewPos = viewPos;
				float4 normal = float4(v.normal, 0.0);

				#if defined(NORMAL_ON)
					float3 viewTangent = mul(UNITY_MATRIX_IT_MV, v.tangent).xyz;
					float3 viewNormal = mul(UNITY_MATRIX_IT_MV, normal).xyz;
					float3 viewBitan = cross(viewNormal, viewTangent) * v.tangent.w;

					float3x3 tbn = float3x3(viewTangent, viewBitan, viewNormal);

					half4 color0 = 0;
					half4 color1 = 0;
					half4 color2 = 0;

					for(int i = 0; i < LIGHT_COUNT; i++)
					{
						ComputeLight(i, viewPos, tbn, color0.xyz, color1.xyz, color2.xyz);
					}
					
					o.color[0] = color0;
					o.color[1] = color1;
					o.color[2] = color2;

				#else
					o.normal = mul(UNITY_MATRIX_IT_MV, normal);
				#endif

				return o;
			}
			
			sampler2D _NormalTex;

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				#if defined(NORMAL_ON)
					fixed3 n = UnpackNormal(tex2D(_NormalTex, i.uv));
					n = NORMALIZE(n, SQUARED_DIST(n));

					fixed3 diffuse = RadiosityNormalMap(
						i.color[0].xyz,
						i.color[1].xyz,
						i.color[2].xyz,
						n);
					col.rgb *= diffuse;
				#endif

				return col;
			}
			ENDCG
		}
	}
}
