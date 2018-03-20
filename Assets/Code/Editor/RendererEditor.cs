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

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ColourMath.Rendering;

namespace ColourMath
{
    // TODO: May need to do specific Renderer stuff

    [CanEditMultipleObjects, CustomEditorForRenderPipeline(typeof(MeshRenderer), typeof(TestRenderPipeline), true)]
    public class MeshRendererEditor : RendererEditor
    {
        // Just extends Renderer Editor
    }

    [CanEditMultipleObjects, CustomEditorForRenderPipeline(typeof(SkinnedMeshRenderer), typeof(TestRenderPipeline), true)]
    public class SkinnedMeshRendererEditor : RendererEditor
    {
        // Just extends Renderer Editor
    }

    public class RendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            const string renderingLayerMask = "m_RenderingLayerMask";

            Renderer renderer = (Renderer) target;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            {
                DrawPropertiesExcluding(serializedObject, renderingLayerMask);
            }
            if(EditorGUI.EndChangeCheck())
            {
                SerializedProperty prop_renderingLayerMask = 
                    serializedObject.FindProperty(renderingLayerMask);

                if (renderer.shadowCastingMode == ShadowCastingMode.On || 
                    renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                    renderer.shadowCastingMode == ShadowCastingMode.TwoSided)
                    prop_renderingLayerMask.intValue = 
                        (int) (prop_renderingLayerMask.intValue |= (int) ShaderLib.RenderLayers.CastsShadows);
                if (renderer.receiveShadows)
                    prop_renderingLayerMask.intValue = 
                        (int) (prop_renderingLayerMask.intValue |= (int) ShaderLib.RenderLayers.ReceivesShadows);
                if (renderer.lightmapIndex != -1)
                    prop_renderingLayerMask.intValue = 
                        (int) (prop_renderingLayerMask.intValue |= (int) ShaderLib.RenderLayers.BakedLightmaps);

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}