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

Shader "Hidden/Dynamic Shadow"
{
	Properties
	{
	}
	SubShader
	{
		// Depth Pass
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#pragma multi_compile __ SHADOW_PROJECTION_ORTHO

			#include "UnityCG.cginc"

			struct a2v_shadow
			{
				float4 vertex : POSITION;
			};
			struct v2f_shadow
			{
				float4 pos : SV_POSITION;
				float2 depth : TEXCOORD0;
			};

			v2f_shadow vert(a2v_shadow v)
			{
				v2f_shadow o;
				o.pos = UnityObjectToClipPos(v.vertex);
				#if defined(SHADOW_PROJECTION_ORTHO)
					o.depth = o.pos.z / o.pos.w  * .5 + .5;
				#else
					o.depth = o.pos.w;
				#endif
				return o;
			}

			half4 frag(v2f_shadow i) : SV_Target
			{
				const float depthScale = 1.0 / 32.0;

				half d = i.depth * depthScale;
				return half4(d,1.0,1.0,1.0);
			}

			ENDCG
		}
	} 
}
