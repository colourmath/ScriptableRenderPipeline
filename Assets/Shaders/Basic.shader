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
				half4 tbn[3] : TEXCOORD2; // 2..4
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
				o.viewPos = UnityObjectToViewPos(v.vertex);

				float4 normal = float4(v.normal, 0.0);

				#if defined(NORMAL_ON)
					float3 viewTangent = mul(UNITY_MATRIX_IT_MV, v.tangent).xyz;
					float3 viewNormal = mul(UNITY_MATRIX_IT_MV, normal).xyz;
					float3 viewBitan = cross(viewNormal, viewTangent) * v.tangent.w;

					float3x3 tbn = float3x3(viewTangent, viewBitan, viewNormal);
					o.tbn[0].xyz = tbn[0];
					o.tbn[0].w = SQUARED_DIST(viewTangent);
					o.tbn[1].xyz = tbn[1];
					o.tbn[1].w = SQUARED_DIST(viewBitan);
					o.tbn[2].xyz = tbn[2];
					o.tbn[2].w = SQUARED_DIST(viewNormal);
					
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
				fixed3 tbn[3];
				tbn[0] = NORMALIZE(i.tbn[0].xyz, i.tbn[0].w);
				tbn[1] = NORMALIZE(i.tbn[1].xyz, i.tbn[1].w);
				tbn[2] = NORMALIZE(i.tbn[2].xyz, i.tbn[2].w);

				fixed3 n = UnpackNormal(tex2D(_NormalTex, i.uv));
				fixed3 lDir = LIGHT_POS(0).xyz - i.viewPos.xyz;
				fixed lDst2 = SQUARED_DIST(lDir);
				lDir = NORMALIZE(lDir, lDst2);
				fixed3 l = TransformDirectionTBN(tbn[0], tbn[1], tbn[2], lDir);
				col *= dot(n, l);
#endif

				return col;
			}
			ENDCG
		}
	}
}
