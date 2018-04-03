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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ColourMath.Rendering
{
    public static class ShaderLib
    {
        public static class Shaders
        {
            const string DYNAMIC_SHADOW = "Hidden/Dynamic Shadow";
            public static Shader DynamicShadow { get { return SafeFind(DYNAMIC_SHADOW); } }
            public static Shader SafeFind(string shaderName)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                    Debug.LogWarningFormat("Couldn't locate Shader. '{0}'.");
                return shader;
            }
        }

        public static class RenderLayers
        {
            public const uint Nothing = 0;
            public const uint ReceivesShadows = 1;
            public const uint CastsShadows = 2;
            public const uint BakedLightmaps = 4;
            public const uint Everything = uint.MaxValue;
        }

        public static class Variables
        {
            public static class Global
            {
                // Light CBuffer
                public const string LIGHTS_COLOR =      "globalLightColors";
                public const string LIGHTS_POSITION =   "globalLightPositions";
                public const string LIGHTS_ATTEN =      "globalLightAtten";
                public const string LIGHTS_COUNT =      "globalLightCount";
                public const string LIGHTS_SPOT_DIRS =  "globalSpotDirections";

                // Enviroment CBuffer
                public const string AMBIENT_SKY =       "ambientLightSky";
                public const string AMBIENT_HORIZON =   "ambientLightHorizon";
                public const string AMBIENT_GROUND =    "ambientLightGround";

                public const string FOG_PARAMS =        "fogParams";
                public const string FOG_COLOR =         "fogColor";

                // Shadow CBuffer
                public const string SHADOW_TEX =        "shadowTexture";
                public const string SHADOW_MATRICES =   "shadowMatrices";
                public const string SHADOW_COUNT =      "shadowCount";
                public const string SHADOW_DISTANCES =  "shadowDistances";
                public const string SHADOW_BIASES =     "shadowBiases";
                public const string SHADOW_INTENSITY =  "shadowIntensity";

                public const string SHADOW_PROJ =       "shadowMatrix";

                public static int id_ShadowTex;

                public const string TEMP_TEX =          "_TempTex";
                public static int id_TempTex;

                public const string FRAMEBUFFER =       "__TempFrameBuffer";
                public static int id_TempFrameBuffer;
            }
            
            public static class Renderer
            {
                public const string SHADOW_INDEX =      "shadowIndex";
            }

            public static class Material
            {
                public const string _MainTex =          "_MainTex";
                public const string _Color =            "_Color";
                public const string _NormalTex =        "_NormalTex";
                public const string _SpecColor =        "_SpecColor";
                public const string _CubeTex =          "_CubeTex";
                public const string SPEC =              "SPEC";

                public const string OVERRIDE_FOG =      "OVERRIDE_FOG";
                public const string _LocalFogColor =    "_LocalFogColor";

                public const string BlendSrc =          "__BlendSrc";
                public const string BlendDst =          "__BlendDst";
                public const string ZTest =             "__ZTest";
                public const string ZWrite =            "__ZWrite";
                public const string CullMode =          "__CullMode";
            }
        }

        public static class Passes
        {
            // Curently unused
            public const string BASE_PASS = "BasePass";
            public static ShaderPassName Base { get { return new ShaderPassName(BASE_PASS); } }

            public const string MIXED = "Mixed";
            public static ShaderPassName Mixed { get { return new ShaderPassName(MIXED); } }

            public const string MIXED_REFLECTIVE = "MixedReflective";
            public static ShaderPassName MixedReflective { get { return new ShaderPassName(MIXED_REFLECTIVE); } }

            public const string DYNAMIC = "Dynamic";
            public static ShaderPassName Dynamic { get { return new ShaderPassName(DYNAMIC); } }

            public const string DYNAMIC_REFLECTIVE = "DynamicReflective";
            public static ShaderPassName DynamicReflective { get { return new ShaderPassName(DYNAMIC_REFLECTIVE); } }

            public const string TRANSPARENT = "Transparent";
            public static ShaderPassName Transparent { get { return new ShaderPassName(TRANSPARENT); } }

            public const string ZPRIME = "ZPrime";
            public static ShaderPassName ZPrime { get { return new ShaderPassName(ZPRIME); } }

            /// <summary>
            /// The Pass Index of ZPrime
            /// </summary>
            public const int ZPRIME_PASS_INDEX =        0;
            /// <summary>
            /// The Pass Index of Transparent
            /// </summary>
            public const int TRANSPARENT_PASS_INDEX =   1;

            /// <summary>
            /// The Pass that gets invoked by the Shadow CommandBuffer
            /// </summary>
            public const int SHADOW_PASS_ID = 0;
        }

        public static class Keywords
        {
            public const string SHADOW_PROJECTION_ORTHO =   "SHADOW_PROJECTION_ORTHO";

            // TODO: Support Optional NormalMap for performance
            public const string NORMAL_MAP =                "NORMAL_MAP";
            public const string OVERRIDE_LOCAL_CUBEMAP =    "OVERRIDE_LOCAL_CUBEMAP";
            public const string CUBE_REFLECTIONS =          "CUBE_REFLECTIONS";

            public const string OVERRIDE_FOG =              "OVERRIDE_FOG_ON";

            // TODO: Support turning specular off entirely
            public const string SPEC_OFF =                  "SPEC_OFF";
            public const string SPEC_POW2 =                 "SPEC_POW_2";
            public const string SPEC_POW4 =                 "SPEC_POW_4";
            public const string SPEC_POW8 =                 "SPEC_POW_8";
            public const string SPEC_POW16 =                "SPEC_POW_16";

            public static string[] SPEC_KEYWORDS = new string[]
            {
                SPEC_POW2, SPEC_POW4, SPEC_POW8, SPEC_POW16
            };
            public static string[] SPEC_LABELS = new string[]
            {
                "2", "4", "8", "16"
            };
        }
    }
}