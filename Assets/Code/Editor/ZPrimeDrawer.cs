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
using ColourMath.Rendering;

namespace ColourMath
{
    public class ZPrimeDrawer : MaterialPropertyDrawer
    {
        public ZPrimeDrawer() : base() { }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float)
            {
                EditorGUI.HelpBox(
                    position,
                    "ZWritePropertyDrawer must be used on a Float Property.",
                    MessageType.Warning);
                return;
            }

            bool b = prop.floatValue == 1;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            {
                b = EditorGUI.Toggle(position, label, b);
            }
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = b ? 1 : 0;
                foreach (Material m in editor.targets)
                    m.SetShaderPassEnabled(ShaderLib.Passes.ZPRIME, b);
            }
        }

        public override float GetPropertyHeight(
            MaterialProperty prop, 
            string label, 
            MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float)
                return EditorGUIUtility.singleLineHeight * 2.5f;

            return base.GetPropertyHeight(prop, label, editor);
        }
    }
}
