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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ColourMath.Rendering
{
    public class TestRenderPipelineInstance : RenderPipeline, IRenderPipeline
    {
        public struct LightData
        {
            //public int pixelAdditionalLightsCount;
            //public int totalAdditionalLightsCount;
            //public int mainLightIndex;
            //public LightShadows shadowMapSampleType;
        }

        readonly TestRenderPipeline settings;

        public TestRenderPipelineInstance(TestRenderPipeline asset) : base()
        {
            settings = asset;
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                // Culling
                ScriptableCullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                CullResults cull = CullResults.Cull(ref cullingParams, context);

                List<VisibleLight> visibleLights = cull.visibleLights;
                SetupLightBuffers(context, visibleLights, camera.worldToCameraMatrix);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                // clear depth buffer
                CommandBuffer cmd = new CommandBuffer() { name = "Clear Framebuffer" };
                cmd.ClearRenderTarget(true, false, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();

                // Draw opaque objects using BasicPass shader pass
                DrawRendererSettings settings =
                    new DrawRendererSettings(camera, ShaderLib.Passes.Base);
                settings.sorting.flags = SortFlags.CommonOpaque;

                FilterRenderersSettings filterSettings =
                    new FilterRenderersSettings(true)
                    {
                        renderQueueRange = RenderQueueRange.opaque,
                        layerMask = camera.cullingMask
                    };
                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Draw skybox
                context.DrawSkybox(camera);

                // Draw transparent objects using BasicPass shader pass
                //settings.sorting.flags = SortFlags.CommonTransparent;
                //filterSettings.renderQueueRange = RenderQueueRange.transparent;
                //context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                context.Submit();
            }
        }

        private void SetupLightBuffers(ScriptableRenderContext context, List<VisibleLight> lights, Matrix4x4 viewMatrix)
        {
            int maxLights = settings.maxLights;
            int lightCount = Mathf.Min(lights.Count, maxLights);

            // Prepare light data
            Vector4[] lightColors =         new Vector4[maxLights];
            Vector4[] lightPositions =      new Vector4[maxLights];
            Vector4[] lightAtten =          new Vector4[maxLights];

            for (int i = 0; i < lightCount; i++)
            {
                VisibleLight light = lights[i];
                lightColors[i] = light.finalColor;
                if (light.lightType == LightType.Directional)
                {
                    // light position for directional lights is: (-direction, 0)
                    Vector4 dir = viewMatrix * light.localToWorld.GetColumn(2);
                    lightPositions[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0);
                    lightAtten[i] = new Vector4(
                        -1,
                        1,
                        0,
                        0);
                }
                else if (light.lightType == LightType.Point)
                {
                    Vector4 pos = viewMatrix * light.localToWorld.GetColumn(3);
                    lightPositions[i] = new Vector4(pos.x, pos.y, pos.z, 1);
                    lightAtten[i] = new Vector4(
                        -1, 
                        1, 
                        25f/(light.range*light.range),
                        light.range * light.range);

                }
                else // TODO: Support spot lights at the very least.
                {
                    Debug.LogError(
                        string.Format(
                            "Unsupported LightType '{0}'", 
                            light.lightType.ToString()),
                        light.light);
                }
            }

            // setup global shader variables to contain all the data computed above
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalVectorArray(ShaderLib.Variables.Global.LIGHTS_COLOR, lightColors);
            cmd.SetGlobalVectorArray(ShaderLib.Variables.Global.LIGHTS_POSITION, lightPositions);
            cmd.SetGlobalVectorArray(ShaderLib.Variables.Global.LIGHTS_ATTEN, lightAtten);
            cmd.SetGlobalVector(ShaderLib.Variables.Global.LIGHTS_COUNT, new Vector4(lightCount, 0, 0, 0));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}