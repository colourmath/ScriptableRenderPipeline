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
		[NoScaleOffset]
		_NormalTex ("Normal", 2D) = "bump" {}
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		[KeywordEnum(POW_2,POW_4,POW_8,POW_16)]
		SPEC ("Specular Power", Float) = 0

		_ShadowFalloff("Shadow Falloff", Float) = 1
		_ShadowIntensity("Shadow Intensity", Float) = 1

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
			#pragma shader_feature SPEC_POW_2 SPEC_POW_4 SPEC_POW_8 SPEC_POW_16
			#pragma multi_compile __ LIGHTMAP_ON
			#pragma multi_compile __ SHADOW_PROJECTION_ORTHO

			#define NORMAL_ON
			#define SPECULAR_ON

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

			// TODO: Can still support TEXCOORD 5..7 with Normals.
			// Other paths can have different options.
			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 albedo : COLOR0;
				float4 uv : TEXCOORD0;
				float4 viewDir : TEXCOORD1;
				#if defined(NORMAL_ON)
					half4 color[3] : TEXCOORD2; // 2..4
					half4 specColor : COLOR1;
				#else
					float3 normal : TEXCOORD2;
				#endif

				// TODO: Cubemap needs world normal
				// Shadowmap needs 1/2 of a TEXCOORD, do perspective division per-vertex
				half4 shadowCoords[2] : TEXCOORD5; // 5..6 we can support up-to 4 shadow maps
				half4 shadowDepths : TEXCOORD7; // store depth component for 4 shadows 
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			half4 _SpecColor;

			v2f vert (a2v v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

				// TODO: Support all shadows
				//for(int i = 0; i < SHADOW_COUNT; i++)
				//{
					float4 shadowCoord = mul(shadowMatrices[0], worldPos);
					o.shadowCoords[0].xy = shadowCoord.xy;
					#if defined(SHADOW_PROJECTION_ORTHO)
						o.shadowDepths[0] = (shadowCoord.z / shadowCoord.w  * .5 + .5);// * shadowDistances[0];
					#else
						o.shadowDepths[0] = shadowCoord.w;
					#endif

				//}
					o.shadowCoords[0].zw = 0;
					o.shadowCoords[1].xy = 0;
					o.shadowCoords[1].zw = 0;
					o.shadowDepths.yzw = 0;

				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				float3 viewPos = UnityObjectToViewPos(v.vertex).xyz;
				float4 normal = float4(v.normal, 0.0);

				o.uv.zw = LightmapTexcoord(v.uv2);

				o.albedo = v.color;

				#if defined(NORMAL_ON)
					float3 viewTangent =	mul(UNITY_MATRIX_IT_MV, v.tangent).xyz;
					float3 viewNormal =		mul(UNITY_MATRIX_IT_MV, normal).xyz;
					float3 viewBitan =		cross(viewNormal, viewTangent) * v.tangent.w;

					float3x3 tbn = float3x3(viewTangent, viewBitan, viewNormal);

					half4 ambient = Sample3PointAmbient(viewNormal);

					half4 color0 = 0;
					half4 color1 = 0;
					half4 color2 = 0;
					half4 specColor = 0;
					for(int i = 0; i < LIGHT_COUNT; i++)
						ComputeLight(
							i, 
							viewPos, 
							tbn, 
							color0.xyz, 
							color1.xyz, 
							color2.xyz, 
							specColor.xyz,
							_SpecColor.rgb);

					AmbientContribution(ambient.rgb, color0.rgb, color1.rgb, color2.rgb);

					float3 viewDir = TransformDirectionTBN(tbn[0], tbn[1], tbn[2], FORWARD.xyz);
					o.viewDir.xyz = NORMALIZE(viewDir,SQUARED_DIST(viewDir));
					o.viewDir.w = (shadowIndex & shadowMask[0]) == 0 ? 0 : 1;
					o.color[0] = color0;
					o.color[1] = color1;
					o.color[2] = color2;
					o.specColor = specColor;
				#else
					o.normal = mul(UNITY_MATRIX_IT_MV, normal);
				#endif

				return o;
			}
			
			sampler2D _NormalTex;
			fixed _ShadowFalloff;
			fixed _ShadowIntensity;

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * i.albedo;

				// TODO: Support all shadow casters
				// Real-Time Shadows
				const half mask = i.viewDir.w;
				half2 coord = i.shadowCoords[0].xy / i.shadowDepths[0];
				coord = coord * .5 + .5;

				half shadow = ComputeShadow(
					coord, 
					shadowTexture, 
					i.shadowDepths[0], 
					shadowBiases[0], 
					mask,
					_ShadowFalloff);

				#if defined(NORMAL_ON)
					fixed3 n = UnpackNormal(tex2D(_NormalTex, i.uv.xy));
					n = NORMALIZE(n, SQUARED_DIST(n));

					fixed3 diffuse;
					// Sample per-object Lightmaps
					#if defined(LIGHTMAP_ON)
						diffuse = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv.zw)).rgb;
					#else
						// Radiosity Lambert Term
						diffuse = RadiosityNormalMap(
							i.color[0].xyz,
							i.color[1].xyz,
							i.color[2].xyz,
							n);
					#endif

					col.rgb *= diffuse * shadow;
					
					// Radiosity Blinn Specular 
					fixed3 spec = i.specColor.rgb;

					// TODO: This vector could probably suffice for the Cubemap lookup as well
					fixed3 reflDir = i.viewDir.xyz - 2.0*dot(n, i.viewDir.xyz) * n;
					fixed3 sr;
					sr.x = POW(dot(BASIS_0, reflDir));
					sr.y = POW(dot(BASIS_1, reflDir));
					sr.z = POW(dot(BASIS_2, reflDir));
					spec *= sr.x + sr.y + sr.z;
					col.rgb += spec * col.a; // For now force texture alpha to be spec map
				#endif

				return col;
			}
			ENDCG
		}
	}
}
