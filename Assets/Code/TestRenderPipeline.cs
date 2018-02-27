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

namespace ColourMath.Rendering
{
    [CreateAssetMenu(fileName ="New RenderPipeline", menuName = "ColourMath/Rendering/Render Pipeline")]
    public class TestRenderPipeline : RenderPipelineAsset
    {
        public const int MAX_SHADOWMAPS = 4;

        [Tooltip("The scale of the Frame Buffer. 1 is native scale.")]
        [Range(.1f,1f)]
        public float renderScale = 1f;

        [Tooltip("Maximum number of real-time lights.")]
        [Range(1,8)]
        public int maxLights = 8;

        public int shadowMapSize = 2048;

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new TestRenderPipelineInstance(this);
        }
    }
}