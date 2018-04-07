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
                if(x.light.bakingOutput.isBaked && !y.light.bakingOutput.isBaked)
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

        readonly TestRenderPipeline pipelineSettings;
        readonly LightComparer lightcomparer;

        RenderTextureDescriptor framebufferDescriptor;
        RenderTargetIdentifier framebufferID;

        RenderTextureDescriptor shadowMapDescriptor;
        
        Material shadowMaterial;
        RenderTexture shadowRT;
        RenderTargetIdentifier shadowRTID;
        RenderTargetIdentifier tempRTID;

        public TestRenderPipelineInstance(TestRenderPipeline asset) : base()
        {
            lightcomparer = new LightComparer();
            pipelineSettings = asset;

            shadowMapDescriptor = new RenderTextureDescriptor(
                pipelineSettings.shadowMapSize,
                pipelineSettings.shadowMapSize,
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

            shadowMaterial = new Material(ShaderLib.Shaders.DynamicShadow);

            framebufferDescriptor = new RenderTextureDescriptor(
                (int) (Screen.width * asset.renderScale),
                (int) (Screen.height * asset.renderScale), 
                RenderTextureFormat.Default, 
                24);

            ShaderLib.Variables.Global.id_TempFrameBuffer = 
                Shader.PropertyToID(ShaderLib.Variables.Global.FRAMEBUFFER);
            framebufferID = 
                new RenderTargetIdentifier(ShaderLib.Variables.Global.id_TempFrameBuffer);
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
            cmd.SetGlobalVector(
                ShaderLib.Variables.Global.FOG_PARAMS,
                new Vector4(
                    RenderSettings.fogStartDistance, 
                    RenderSettings.fogEndDistance));
            cmd.SetGlobalColor(
                ShaderLib.Variables.Global.FOG_COLOR,
                RenderSettings.fogColor);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ScriptableCullingParameters cullingParams;
            FilterRenderersSettings filterSettings;
            DrawRendererSettings settings;

            framebufferDescriptor.width = (int)(Screen.width * pipelineSettings.renderScale);
            framebufferDescriptor.height = (int)(Screen.height * pipelineSettings.renderScale);

            foreach (Camera camera in cameras)
            {
                lightcomparer.cameraPosition = camera.transform.position;

                // Culling
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

                shadowMapDescriptor.width = pipelineSettings.shadowMapSize;
                shadowMapDescriptor.height = pipelineSettings.shadowMapSize;

                // Shadow Pass
                if(shadowLight != null)
                    ShadowPass(context, shadowLight);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                // clear frame buffer
                cmd = CommandBufferPool.Get();
                    cmd.name = "Clear Framebuffer";
                    cmd.GetTemporaryRT(
                        ShaderLib.Variables.Global.id_TempFrameBuffer, 
                        framebufferDescriptor);
                    cmd.SetRenderTarget(framebufferID);
                    cmd.ClearRenderTarget(true, true, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                // Render objects with Lightmaps first. 
                // Mixed Lights should contribute specular but not diffuse

                settings = new DrawRendererSettings(camera, ShaderLib.Passes.Mixed);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.flags = DrawRendererFlags.EnableDynamicBatching;
                settings.rendererConfiguration = 
                    RendererConfiguration.PerObjectLightmaps | 
                    RendererConfiguration.PerObjectLightProbe;
                
                filterSettings =
                    new FilterRenderersSettings(true)
                    {
                        renderQueueRange = RenderQueueRange.opaque,
                        layerMask = camera.cullingMask,
                        renderingLayerMask = ShaderLib.RenderLayers.BakedLightmaps
                    };

                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Mixed Lights with Reflectives
                settings = new DrawRendererSettings(camera, ShaderLib.Passes.MixedReflective);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.flags = DrawRendererFlags.EnableDynamicBatching;
                settings.rendererConfiguration =
                    RendererConfiguration.PerObjectLightmaps |
                    RendererConfiguration.PerObjectLightProbe | 
                    RendererConfiguration.PerObjectReflectionProbes;

                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Render Dynamic Objects
                settings = new DrawRendererSettings(camera, ShaderLib.Passes.Dynamic);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.flags = DrawRendererFlags.EnableDynamicBatching;
                settings.rendererConfiguration =
                    RendererConfiguration.PerObjectLightmaps |
                    RendererConfiguration.PerObjectLightProbe;

                filterSettings =
                    new FilterRenderersSettings(true)
                    {
                        renderQueueRange = RenderQueueRange.opaque,
                        layerMask = camera.cullingMask,
                        renderingLayerMask = ShaderLib.RenderLayers.Everything & ~ShaderLib.RenderLayers.BakedLightmaps
                    };

                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Render Dynamic Objects
                settings = new DrawRendererSettings(camera, ShaderLib.Passes.DynamicReflective);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.flags = DrawRendererFlags.EnableDynamicBatching;
                settings.rendererConfiguration =
                    RendererConfiguration.PerObjectLightmaps |
                    RendererConfiguration.PerObjectLightProbe |
                    RendererConfiguration.PerObjectReflectionProbes;

                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Draw skybox
                context.DrawSkybox(camera);

                // Transparency actually has 2 passes here
                // 1) ZPrime is a special Pass that can be disabled, it only writes Depth
                // 2) Transparent Pass for writing transparent colour
                // TODO: Support more special types of transparency for things like glass

                // Note:    Some additional uses for multiple Passes could be rendering 
                //          backfaces and then front faces for a better 2-sided sorting.
                settings = new DrawRendererSettings(camera, ShaderLib.Passes.ZPrime);
                settings.SetShaderPassName(
                    ShaderLib.Passes.TRANSPARENT_PASS_INDEX, 
                    ShaderLib.Passes.Transparent);
                settings.sorting.flags = SortFlags.CommonTransparent;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                filterSettings.renderingLayerMask = ShaderLib.RenderLayers.Everything;
                context.DrawRenderers(cull.visibleRenderers, ref settings, filterSettings);

                // Final Blit
                cmd = CommandBufferPool.Get();
                cmd.name = "Blit Framebuffer";
                cmd.Blit(framebufferID, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

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

            bool isOrtho = 
                shadowLight.type == LightType.Directional || 
                shadowLight.type == LightType.Area;

            cmd.SetRenderTarget(shadowRTID);
            cmd.SetViewport(
                new Rect(0, 0, shadowMapDescriptor.width, shadowMapDescriptor.height));
            cmd.ClearRenderTarget(true, true, Color.clear, 1);

            if (isOrtho)
                cmd.EnableShaderKeyword(ShaderLib.Keywords.SHADOW_PROJECTION_ORTHO);
            else
                cmd.DisableShaderKeyword(ShaderLib.Keywords.SHADOW_PROJECTION_ORTHO);

            float[] shadowDistances =   new float[TestRenderPipeline.MAX_SHADOWMAPS];
            float[] shadowBiases =      new float[TestRenderPipeline.MAX_SHADOWMAPS];

            Vector2 shadowQuadrant = new Vector2(
                shadowMapDescriptor.width / 2f,
                shadowMapDescriptor.height / 2f);

            // Set pixel rects for each quadrant of the shadowmap texture
            Rect[] pixelRects = new Rect[TestRenderPipeline.MAX_SHADOWMAPS]
            {
                new Rect(Vector2.zero,shadowQuadrant),
                new Rect(new Vector2(0,shadowQuadrant.y),shadowQuadrant),
                new Rect(new Vector2(shadowQuadrant.x,0),shadowQuadrant),
                new Rect(shadowQuadrant,shadowQuadrant)
            };

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
                cmd.SetViewport(pixelRects[i]);
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

            int maxLights = pipelineSettings.maxLights;
            int lightCount = 0;

            // Prepare light data
            Vector4[] lightColors =         new Vector4[maxLights];
            Vector4[] lightPositions =      new Vector4[maxLights];
            Vector4[] lightAtten =          new Vector4[maxLights];
            Vector4[] lightSpotDirections = new Vector4[maxLights];

            // TODO: A non-GC sort will be clutch here.
            lights.Sort(lightcomparer);

            for (int i = 0; i < lights.Count; i++)
            {
                if(lightCount == maxLights)
                    break;

                VisibleLight vl = lights[i];

                // baked lights should not make it into our run-time buffer
                if (vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
                    continue;

                Color lightColor = vl.finalColor;
                // we will be able to multiply out any light data that isn't a mixed light
                // this will help better with blending on lightmapped objects
                lightColor.a = vl.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed ? 0f : 1f;
                //if(vl.lightType == LightType.Spot) lightColor *= 2;
                lightColors[lightCount] = lightColor;

                float sqrRange = vl.range * vl.range;
                float quadAtten = 25.0f / sqrRange;

                if (vl.lightType == LightType.Directional)
                {
                    // light position for directional lights is: (-direction, 0)
                    Vector4 dir = viewMatrix * vl.localToWorld.GetColumn(2);
                    lightPositions[lightCount] = new Vector4(-dir.x, -dir.y, -dir.z, 0);
                    lightAtten[lightCount] = new Vector4(
                        -1,
                        1,
                        0,
                        0);
                }
                else if (vl.lightType == LightType.Point)
                {
                    Vector4 pos = viewMatrix * vl.localToWorld.GetColumn(3);
                    lightPositions[lightCount] = new Vector4(pos.x, pos.y, pos.z, 1);
                    lightAtten[lightCount] = new Vector4(
                        -1, 
                        1, 
                        quadAtten,
                        sqrRange);

                }
                else if (vl.lightType == LightType.Spot)
                {
                    Vector4 pos = viewMatrix * vl.localToWorld.GetColumn(3);
                    lightPositions[lightCount] = new Vector4(pos.x, pos.y, pos.z, 1);

                    Vector4 dir = viewMatrix * vl.localToWorld.GetColumn(2);
                    lightSpotDirections[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                    float radAngle, cosTheta, cosPhi;
                    radAngle = Mathf.Deg2Rad * vl.light.spotAngle;
                    cosTheta = Mathf.Cos(radAngle * 0.25f);
                    cosPhi = Mathf.Cos(radAngle * 0.5f);

                    lightAtten[i] = new Vector4(
                        cosPhi,
                        1.0f / (cosTheta - cosPhi),
                        quadAtten,
                        sqrRange);
                }
                else // TODO: Support area
                {
                    Debug.LogError(
                        string.Format(
                            "Unsupported LightType '{0}'", 
                            vl.lightType.ToString()),
                        vl.light);
                }

                if (vl.light.shadows != LightShadows.None && shadowLightID < 0)
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
                    cmd.SetGlobalVectorArray(
                    ShaderLib.Variables.Global.LIGHTS_SPOT_DIRS,
                    lightSpotDirections);
                cmd.SetGlobalVector(
                    ShaderLib.Variables.Global.LIGHTS_COUNT,
                    new Vector4(lightCount, 0, 0, 0));
                context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}