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

using UnityEngine;
using UnityEditor;
using MaterialProps = ColourMath.Rendering.ShaderLib.Variables.Material;
using Keywords = ColourMath.Rendering.ShaderLib.Keywords;
using Passes = ColourMath.Rendering.ShaderLib.Passes;
using UnityEngine.Rendering;

namespace ColourMath
{
    public class BasicShaderGUI : ShaderGUI
    {
        // TODO: Add More
        static string[] TransparencyModeKeys = new string[] 
        {
            "Opaque",
            "Transparent"
        };
        static RenderQueue[] TransparencyModeVals = new RenderQueue[] 
        {
            RenderQueue.Geometry,
            RenderQueue.Transparent
        };

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            Debug.Log("Assign new Shader");
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            MaterialProperty prop = MaterialEditor.GetMaterialProperty(
                new Object[1] { material },
                MaterialProps.ZWrite);

            if (prop != null)
                material.SetShaderPassEnabled(Passes.ZPRIME, prop.floatValue != 0);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //base.OnGUI(materialEditor, properties);
            Object[] materials = materialEditor.targets;

            // Gather Properties
            MaterialProperty _MainTex = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._MainTex);
            MaterialProperty _Color = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._Color);
            MaterialProperty _NormalTex = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._NormalTex);

            MaterialProperty SPEC = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps.SPEC);
            MaterialProperty _SpecColor = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._SpecColor);

            MaterialProperty _CubeTex = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._CubeTex);

            MaterialProperty OVERRIDE_FOG = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps.OVERRIDE_FOG);
            MaterialProperty _LocalFogColor = MaterialEditor.GetMaterialProperty(
                materials, MaterialProps._LocalFogColor);

            // Draw Properties
            DrawTransparencyMode(materialEditor, materials); // Transparency Mode

            EditorGUILayout.Space();

            materialEditor.ShaderProperty(_MainTex, _MainTex.displayName);
            materialEditor.ShaderProperty(_Color, _Color.displayName);

            materialEditor.ShaderProperty(_NormalTex, _NormalTex.displayName);
            // TODO: This can be turned on once there are separate variants for no normals
            //if (_NormalTex.textureValue != null)
            //    EnableKeyword(materials, Keywords.NORMAL_MAP);
            //else
            //    DisableKeyword(materials, Keywords.NORMAL_MAP);

            DrawSpecularControl(materials, SPEC);
            materialEditor.ShaderProperty(_SpecColor, _SpecColor.displayName);

            DrawReflectionControls(materialEditor, materials, _CubeTex);

            DrawLocalFogControls(materialEditor, materials, OVERRIDE_FOG, _LocalFogColor);
        }

        void DrawLocalFogControls(
            MaterialEditor materialEditor, 
            Object[] materials,
            MaterialProperty OVERRIDE_FOG,
            MaterialProperty _LocalFogColor)
        {
            GUILayout.Label("Fog Settings", EditorStyles.boldLabel);

            int numEnabled = 0;
            foreach (Material m in materials)
            {
                bool e = m.IsKeywordEnabled(Keywords.OVERRIDE_FOG);
                if (e) numEnabled++;
            }

            bool someEnabled = numEnabled > 0;
            bool hasMixed = someEnabled && numEnabled < materials.Length;

            EditorGUI.showMixedValue = hasMixed;

            EditorGUI.BeginChangeCheck();
            {
                materialEditor.ShaderProperty(OVERRIDE_FOG, OVERRIDE_FOG.displayName);
            }
            if (EditorGUI.EndChangeCheck())
            {
                // This should hopefully already be handled.
            }

            if (OVERRIDE_FOG.floatValue == 1 && !OVERRIDE_FOG.hasMixedValue) // enabled, not mixed
            {
                materialEditor.ShaderProperty(_LocalFogColor, _LocalFogColor.displayName);
            }
        }

        void DrawTransparencyMode(MaterialEditor materialEditor, Object[] materials)
        {
            // TODO: This could be way better
            bool hasMixed = false;
            int baseVal = (materials[0] as Material).renderQueue;
            int index = (baseVal < (int) RenderQueue.Transparent) ? 0 : 1;

            if (materials.Length > 1)
            {
                for (int i = 1; i < materials.Length; i++)
                {
                    if ((materials[i] as Material).renderQueue != baseVal)
                    {
                        hasMixed = true;
                        index = -1;
                        break;
                    }
                }
            }

            EditorGUI.showMixedValue = hasMixed;
            EditorGUI.BeginChangeCheck();
            {
                index = EditorGUILayout.Popup("Mode", index, TransparencyModeKeys);
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (index > -1)
                {
                    foreach (Material m in materials)
                    {
                        m.renderQueue = (int) TransparencyModeVals[index];
                    }
                }
            }
            EditorGUI.showMixedValue = false;
            EditorGUILayout.Space();

            if(index > 0) // Transparent?
            {
                MaterialProperty BlendSrc = MaterialEditor.GetMaterialProperty(
                    materials, MaterialProps.BlendSrc);
                MaterialProperty BlendDst = MaterialEditor.GetMaterialProperty(
                    materials, MaterialProps.BlendDst);
                MaterialProperty ZTest = MaterialEditor.GetMaterialProperty(
                    materials, MaterialProps.ZTest);
                MaterialProperty ZWrite = MaterialEditor.GetMaterialProperty(
                    materials, MaterialProps.ZWrite);
                MaterialProperty CullMode = MaterialEditor.GetMaterialProperty(
                    materials, MaterialProps.CullMode);

                GUILayout.Label("Transparent Properties",EditorStyles.boldLabel);
                materialEditor.ShaderProperty(BlendSrc, BlendSrc.displayName);
                materialEditor.ShaderProperty(BlendDst, BlendDst.displayName);
                materialEditor.ShaderProperty(ZTest, ZTest.displayName);
                materialEditor.ShaderProperty(ZWrite, ZWrite.displayName);
                materialEditor.ShaderProperty(CullMode, CullMode.displayName);
            }
        }

        void DrawReflectionControls(MaterialEditor materialEditor, Object[] materials, MaterialProperty _CubeTex)
        {
            int numReflective = 0;
            foreach (Material m in materials)
            {
                bool cr = m.GetShaderPassEnabled(Passes.MIXED_REFLECTIVE) || 
                    m.GetShaderPassEnabled(Passes.DYNAMIC_REFLECTIVE);
                if (cr) numReflective++;
            }

            bool cubeReflections = numReflective > 0;
            bool hasMixed = cubeReflections && numReflective < materials.Length;

            EditorGUI.showMixedValue = hasMixed;
            EditorGUI.BeginChangeCheck();
            {
                cubeReflections = EditorGUILayout.Toggle("Cube Reflections", cubeReflections);
            }
            if(EditorGUI.EndChangeCheck())
            {
                // The reason why we disable both Reflective Passes here is that
                // we can't be sure whether or not a given object will be baked or not.
                // That is actually handled by the Renderer itself, as it will receive the 
                // renderingLayerMask used to enabled mixed vs. dynamic lighting.
                // Considering a Material could be shared across static/non-static geometry
                // this should prove to be more efficient than a variant.
                if (cubeReflections)
                {
                    // enable reflective passes
                    EnablePass(materials, Passes.MIXED_REFLECTIVE);
                    EnablePass(materials, Passes.DYNAMIC_REFLECTIVE);
                }
                else
                {
                    // disable reflective passes
                    DisablePass(materials, Passes.MIXED_REFLECTIVE);
                    DisablePass(materials, Passes.DYNAMIC_REFLECTIVE);
                }
            }
            // If we have cube reflections enabled, draw control for override
            if (cubeReflections || hasMixed)
            {
                EditorGUI.showMixedValue = _CubeTex.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                {
                    materialEditor.ShaderProperty(_CubeTex, "Local Cubemap");
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (_CubeTex.textureValue != null)
                        EnableKeyword(materials, Keywords.OVERRIDE_LOCAL_CUBEMAP);
                    else
                        DisableKeyword(materials, Keywords.OVERRIDE_LOCAL_CUBEMAP);
                }
            }

            EditorGUI.showMixedValue = false;
        }

        void DrawSpecularControl(Object[] materials, MaterialProperty prop)
        {
            int index = prop.hasMixedValue ? -1 : (int)prop.floatValue;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            {
                index = EditorGUILayout.Popup(prop.displayName, index, Keywords.SPEC_LABELS);
            }
            if(EditorGUI.EndChangeCheck())
            {
                prop.floatValue = index;
                // wipe any previous spec keywords
                for (int i = 0; i < Keywords.SPEC_KEYWORDS.Length; i++)
                    DisableKeyword(materials, Keywords.SPEC_KEYWORDS[i]);
                EnableKeyword(materials, Keywords.SPEC_KEYWORDS[index]);
            }
            EditorGUI.showMixedValue = false;
        }

        void EnablePass(Object[] materials, string passName)
        {
            foreach (Material m in materials)
                m.SetShaderPassEnabled(passName, true);
        }

        void DisablePass(Object[] materials, string passName)
        {
            foreach (Material m in materials)
                m.SetShaderPassEnabled(passName, false);
        }

        void EnableKeyword( Object[] materials, string keyword)
        {
            foreach(Material m in materials)
                m.EnableKeyword(keyword);
        }

        void DisableKeyword(Object[] materials, string keyword)
        {
            foreach (Material m in materials)
                m.DisableKeyword(keyword);
        }

    }
}