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
        public static class Variables
        {
            public static class Global
            {
                // Light CBuffer
                public const string LIGHTS_COLOR =      "globalLightColors";
                public const string LIGHTS_POSITION =   "globalLightPositions";
                public const string LIGHTS_ATTEN =      "globalLightAtten";
                public const string LIGHTS_COUNT =      "globalLightCount";

                // Ambient CBuffer
                public const string AMBIENT_SKY =       "ambientLightSky";
                public const string AMBIENT_HORIZON =   "ambientLightHorizon";
                public const string AMBIENT_GROUND =    "ambientLightGround";

                // Shadow CBuffer
                public const string SHADOW_TEX =        "shadowTexture";
                public const string SHADOW_MATRICES =   "shadowMatrices";
                public const string SHADOW_COUNT =      "shadowCount";
                public const string SHADOW_DISTANCES =  "shadowDistances";
                public const string SHADOW_BIASES =     "shadowBiases";
                public const string SHADOW_INTENSITY =  "shadowIntensity";


                public const string SHADOW_PROJ =       "shadowMatrix";

                public static int id_ShadowTex;

                public const string TEMP_TEX =        "_TempTex";
                public static int id_TempTex;

            }

            public static class Renderer
            {
                public const string SHADOW_INDEX = "shadowIndex";
            }
        }

        public static class Passes
        {
            public const string BASE_PASS = "BasePass";
            public static ShaderPassName Base { get { return new ShaderPassName(BASE_PASS); } }

            public const int SHADOW_PASS_ID = 0;
        }

        public static class Keywords
        {
            public const string SHADOW_PROJECTION_ORTHO = "SHADOW_PROJECTION_ORTHO";
        }
    }
}