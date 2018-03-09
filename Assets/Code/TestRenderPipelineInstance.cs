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
    public class TestRenderPipelineInstance : RenderPipeline, IRenderPipeline
    {
        /// <summary>
        /// Sorts the Lights by type, then squared distance to Camera. 
        /// Directional Lights will always be first.
        /// </summary>
        class LightComparer : IComparer<VisibleLight>
        {
            public Vector3 cameraPosition;

            public int Compare(VisibleLight x, VisibleLight y)
            {
                // baked lights should be at the back as we will filter them out
                if (x.light.lightmapBakeType == LightmapBakeType.Baked &&
                    y.light.lightmapBakeType != LightmapBakeType.Baked)
                    return 1; 

                // directional lights have infinite distance, so move these to the front
                if (x.lightType == LightType.Directional && y.lightType != LightType.Directional)
                    return -1;
                else if (x.lightType != LightType.Directional && y.lightType == LightType.Directional)
                    return 1;

                return Mathf.Abs((x.light.transform.position - cameraPosition).sqrMagnitude).CompareTo(
                    Mathf.Abs((y.light.transform.position - cameraPosition).sqrMagnitude));
            }
        }

        readonly TestRenderPipeline settings;
        readonly LightComparer lightcomparer;

        RenderTextureDescriptor shadowMapDescriptor;

        Material shadowMaterial;
        RenderTexture shadowRT;
        RenderTargetIdentifier shadowRTID;
        RenderTargetIdentifier tempRTID;

        public TestRenderPipelineInstance(TestRenderPipeline asset) : base()
        {
            lightcomparer = new LightComparer();
            settings = asset;

            shadowMapDescriptor = new RenderTextureDescriptor(
                settings.shadowMapSize,
                settings.shadowMapSize,
                RenderTextureFormat.RGHalf,
                24)
            {
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 1
            };

            shadowRT = new RenderTexture(shadowMapDescriptor) { name = "Shadow Depth Tex" };

            ShaderLib.Variables.Global.id_ShadowTex = 
                Shader.PropertyToID(ShaderLib.Variables.Global.SHADOW_TEX);
            ShaderLib.Variables.Global.id_TempTex =
                Shader.PropertyToID(ShaderLib.Variables.Global.TEMP_TEX);

            shadowRTID = new RenderTargetIdentifier(ShaderLib.Variables.Global.id_ShadowTex);
            tempRTID = new RenderTargetIdentifier(ShaderLib.Variables.Global.id_TempTex);

            shadowMaterial = new Material(Shader.Find("Hidden/Dynamic Shadow"));
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // Ambient lighting
            CommandBuffer cmd = CommandBufferPool.Get();
                cmd.name = "Build Environment CBuffer";
                cmd.SetGlobalVector(
                    ShaderLib.Variables.Global.AMBIENT_SKY,     
                    RenderSettings.ambientSkyColor);
                cmd.SetGlobalVector(
                    ShaderLib.Variables.Global.AMBIENT_HORIZON, 
                    RenderSettings.ambientEquatorColor);
                cmd.SetGlobalVector(
                    ShaderLib.Variables.Global.AMBIENT_GROUND,  
                    RenderSettings.ambientGroundColor);
                context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            foreach (Camera camera in cameras)
            {
                lightcomparer.cameraPosition = camera.transform.position;

                // Culling
                ScriptableCullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                CullResults cull = CullResults.Cull(ref cullingParams, context);

                Light shadowLight;

                List<VisibleLight> visibleLights = cull.visibleLights;
                SetupLightBuffers(
                    context, 
                    visibleLights, 
                    camera.worldToCameraMatrix,
                    out shadowLight);

                shadowMapDescriptor.width = this.settings.shadowMapSize;
                shadowMapDescriptor.height = this.settings.shadowMapSize;

                // Shadow Pass
                if(shadowLight != null)
                    ShadowPass(context, shadowLight);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                // clear depth buffer
                cmd = CommandBufferPool.Get();
                    cmd.name = "Clear Framebuffer";
                    cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                    cmd.ClearRenderTarget(true, false, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);               

                // Draw opaque objects using BasicPass shader pass
                DrawRendererSettings settings =
                    new DrawRendererSettings(camera, ShaderLib.Passes.Base);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.flags = DrawRendererFlags.EnableDynamicBatching;
                settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps;
                // TODO: Circle back when it's time to take on probes

                // It would be nice to filter out things based on a scriptable heuristic.
                FilterRenderersSettings filterSettings =
                    new FilterRenderersSettings(true)
                    {
                        renderQueueRange = RenderQueueRange.opaque,
                        layerMask = camera.cullingMask
                        //renderingLayerMask = ShaderLib.RenderLayers.Everything | ShaderLib.RenderLayers.ReceivesShadows
                    };
                
                // TODO: Render baked objects with Mixed RT Lighting as a different Pass.
                // Mixed-Lights should contribute specular but not diffuse


                // It would be nice if we could do something like flip a multi-compile flag
                // for a renderer based on a setting, like receiveShadows.
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

        private void ShadowPass(
            ScriptableRenderContext context, 
            Light shadowLight)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.name = "Collect Shadows";
            // Set the Shadow RenderTarget and clear it
            cmd.GetTemporaryRT(
                ShaderLib.Variables.Global.id_ShadowTex,
                shadowMapDescriptor, 
                FilterMode.Bilinear);

            //cmd.GetTemporaryRT(
            //    ShaderLib.Variables.Global.id_TempTex,
            //    settings.shadowMapSize,
            //    settings.shadowMapSize,
            //    24,
            //    FilterMode.Bilinear,
            //    RenderTextureFormat.Default);
            bool isOrtho = 
                shadowLight.type == LightType.Directional || 
                shadowLight.type == LightType.Area;

            cmd.SetRenderTarget(shadowRTID);
            cmd.ClearRenderTarget(true, false, Color.clear, 1);

            if (isOrtho)
                cmd.EnableShaderKeyword(ShaderLib.Keywords.SHADOW_PROJECTION_ORTHO);
            else
                cmd.DisableShaderKeyword(ShaderLib.Keywords.SHADOW_PROJECTION_ORTHO);

            float[] shadowDistances =   new float[TestRenderPipeline.MAX_SHADOWMAPS];
            float[] shadowBiases =      new float[TestRenderPipeline.MAX_SHADOWMAPS];

            Matrix4x4[] shadowMatrices = new Matrix4x4[TestRenderPipeline.MAX_SHADOWMAPS];
            // For each ShadowCaster, calculate the local shadow matrix.
            for (int i = 0; i < ShadowCaster.casters.Count; i++)
            {
                Matrix4x4 viewMatrix, projectionMatrix;
                float distance;
                ShadowCaster.casters[i].SetupShadowMatrices(
                    i,
                    shadowLight, 
                    out viewMatrix, 
                    out projectionMatrix,
                    out distance);
                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                cmd.DrawRenderer(
                    ShadowCaster.casters[i].renderer, 
                    shadowMaterial, 
                    0, 
                    ShaderLib.Passes.SHADOW_PASS_ID);
                shadowMatrices[i] = projectionMatrix * viewMatrix;
                shadowDistances[i] = isOrtho ? 0 : distance;
                shadowBiases[i] = shadowLight.shadowBias;
            }
            cmd.SetGlobalFloat(
                ShaderLib.Variables.Global.SHADOW_INTENSITY,
                shadowLight.shadowStrength);
            cmd.SetGlobalVector(
                ShaderLib.Variables.Global.SHADOW_BIASES,
                new Vector4(
                    shadowBiases[0],
                    shadowBiases[1],
                    shadowBiases[2],
                    shadowBiases[3]));
            cmd.SetGlobalVector(
                ShaderLib.Variables.Global.SHADOW_DISTANCES,
                new Vector4(
                    shadowDistances[0], 
                    shadowDistances[1], 
                    shadowDistances[2], 
                    shadowDistances[3]));
            cmd.SetGlobalFloat(
                ShaderLib.Variables.Global.SHADOW_COUNT, 
                ShadowCaster.casters.Count);
            cmd.SetGlobalMatrixArray(
                ShaderLib.Variables.Global.SHADOW_MATRICES,
                shadowMatrices);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupLightBuffers(
            ScriptableRenderContext context, 
            List<VisibleLight> lights, 
            Matrix4x4 viewMatrix,
            out Light shadowLight)
        {
            shadowLight = null;
            int shadowLightID = -1;

            int maxLights = settings.maxLights;
            int lightCount = 0;

            // Prepare light data
            Vector4[] lightColors = new Vector4[maxLights];
            Vector4[] lightPositions = new Vector4[maxLights];
            Vector4[] lightAtten = new Vector4[maxLights];

            lights.Sort(lightcomparer);

            for (int i = 0; i < lights.Count; i++)
            {
                if(lightCount == maxLights)
                    break;

                VisibleLight light = lights[i];

                // baked lights should not make it into our run-time buffer
                if (light.light.lightmapBakeType == LightmapBakeType.Baked)
                    continue;

                Color lightColor = light.finalColor;
                // we will be able to multiply out any light data that isn't a mixed light
                // this will help better with blending on lightmapped objects
                lightColor.a = light.light.lightmapBakeType == LightmapBakeType.Mixed ? 1f : 0f;
                lightColors[lightCount] = lightColor;
                
                if (light.lightType == LightType.Directional)
                {
                    // light position for directional lights is: (-direction, 0)
                    Vector4 dir = viewMatrix * light.localToWorld.GetColumn(2);
                    lightPositions[lightCount] = new Vector4(-dir.x, -dir.y, -dir.z, 0);
                    lightAtten[lightCount] = new Vector4(
                        -1,
                        1,
                        0,
                        0);
                }
                else if (light.lightType == LightType.Point)
                {
                    Vector4 pos = viewMatrix * light.localToWorld.GetColumn(3);
                    lightPositions[lightCount] = new Vector4(pos.x, pos.y, pos.z, 1);
                    lightAtten[lightCount] = new Vector4(
                        -1, 
                        1, 
                        25f/(light.range*light.range),
                        light.range * light.range);

                }
                else // TODO: Support spot and area
                {
                    Debug.LogError(
                        string.Format(
                            "Unsupported LightType '{0}'", 
                            light.lightType.ToString()),
                        light.light);
                }

                if (light.light.shadows != LightShadows.None && shadowLightID < 0)
                    shadowLightID = i;

                lightCount++;
            }

            if (shadowLightID >= 0)
                shadowLight = lights[shadowLightID].light;

            // setup global shader variables to contain all the data computed above
            CommandBuffer cmd = CommandBufferPool.Get();
                cmd.SetGlobalVectorArray(
                    ShaderLib.Variables.Global.LIGHTS_COLOR,       
                    lightColors);
                cmd.SetGlobalVectorArray(
                    ShaderLib.Variables.Global.LIGHTS_POSITION,    
                    lightPositions);
                cmd.SetGlobalVectorArray(
                    ShaderLib.Variables.Global.LIGHTS_ATTEN,       
                    lightAtten);
                cmd.SetGlobalVector(
                    ShaderLib.Variables.Global.LIGHTS_COUNT,
                    new Vector4(lightCount, 0, 0, 0));
                context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}