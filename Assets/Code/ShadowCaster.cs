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
using System.Collections.Generic;

namespace ColourMath.Rendering
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class ShadowCaster : MonoBehaviour
    {
        const int NO_SHADOW_INDEX = 0;
        const int SHADOW_INDEX_0 = 1 << 0;
        const int SHADOW_INDEX_1 = 1 << 1;
        const int SHADOW_INDEX_2 = 1 << 2;
        const int SHADOW_INDEX_3 = 1 << 3;

        static Camera shadowCamera;

        public static List<ShadowCaster> casters 
            = new List<ShadowCaster>(TestRenderPipeline.MAX_SHADOWMAPS);

        [System.NonSerialized]
        new public Renderer renderer; // GIVE THESE BACK, UNITY!!
        [System.NonSerialized]
        int index = -1;

        int casterID;

        static Rect[] PixelRects = new Rect[4]
        {
            new Rect(0f,0f,.5f,.5f),
            new Rect(.5f,0f,.5f,.5f),
            new Rect(0f,.5f,.5f,.5f),
            new Rect(.5f,.5f,.5f,.5f)
        };

        MaterialPropertyBlock mpb;

        static void SetupShadowCamera(int index, Light l, Renderer r)
        {
            if(shadowCamera == null)
            {
                shadowCamera = new GameObject().AddComponent<Camera>();
                //shadowCamera.gameObject.hideFlags = HideFlags.HideInHierarchy;
                shadowCamera.enabled = false;
            }

            shadowCamera.aspect = 1f;
            shadowCamera.nearClipPlane = 0.01f;

            // The light type should dictate the type of projection we're building
            shadowCamera.orthographic =
                l.type == LightType.Directional || l.type == LightType.Area;

            // TODO: Calculate this manually, but for now just use a Unity Camera to do the heavy lifting.
            if (shadowCamera.orthographic)
            {
                shadowCamera.transform.rotation = l.transform.rotation;
                shadowCamera.transform.position = l.type == LightType.Area ?
                    l.transform.position :
                    r.transform.position - (shadowCamera.transform.forward * r.bounds.extents.magnitude);
            }
            else
            {
                shadowCamera.transform.position = l.transform.position;
                shadowCamera.transform.LookAt(r.transform, Vector3.up);
            }

            // We need to get the extremes of the bounds relative to the shadow Camera
            // then build a tight-fitting frustum to get the absolute best texel-density.

            Vector3 boundsMin = r.bounds.min;
            Vector3 boundsMax = r.bounds.max;

            Vector3 fbl, fbr, ftl, ftr, bbl, bbr, btl, btr;
            fbl = boundsMin;
            fbr = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
            ftl = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
            ftr = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
            bbl = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
            bbr = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
            btl = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
            btr = boundsMax;

            fbl = shadowCamera.transform.InverseTransformPoint(fbl);
            fbr = shadowCamera.transform.InverseTransformPoint(fbr);
            ftl = shadowCamera.transform.InverseTransformPoint(ftl);
            ftr = shadowCamera.transform.InverseTransformPoint(ftr);
            bbl = shadowCamera.transform.InverseTransformPoint(bbl);
            bbr = shadowCamera.transform.InverseTransformPoint(bbr);
            btl = shadowCamera.transform.InverseTransformPoint(btl);
            btr = shadowCamera.transform.InverseTransformPoint(btr);

            // TODO: Don't GCAlloc
            float minX = Mathf.Min(fbl.x, fbr.x, ftl.x, ftr.x, bbl.x, bbr.x, btl.x, btr.x);
            float maxX = Mathf.Max(fbl.x, fbr.x, ftl.x, ftr.x, bbl.x, bbr.x, btl.x, btr.x);
            float minY = Mathf.Min(fbl.y, fbr.y, ftl.y, ftr.y, bbl.y, bbr.y, btl.y, btr.y);
            float maxY = Mathf.Max(fbl.y, fbr.y, ftl.y, ftr.y, bbl.y, bbr.y, btl.y, btr.y);
            float minZ = Mathf.Min(fbl.z, fbr.z, ftl.z, ftr.z, bbl.z, bbr.z, btl.z, btr.z);
            float maxZ = Mathf.Max(fbl.z, fbr.z, ftl.z, ftr.z, bbl.z, bbr.z, btl.z, btr.z);

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            shadowCamera.farClipPlane = l.type == LightType.Directional ? maxZ : l.range;

            //DebugShadowFrustum(minX, minY, minZ, maxX, maxY, maxZ);

            if (shadowCamera.orthographic)
                shadowCamera.orthographicSize = .5f * (maxY - minY);
            else
                shadowCamera.fieldOfView = Vector3.Angle(max,min);

            shadowCamera.rect = PixelRects[index];
        }

        static void DebugShadowFrustum(
            float minX, float minY, float minZ, 
            float maxX, float maxY, float maxZ)
        {
            /*
              //top
            Debug.DrawLine( // bottom
                fbl,//shadowCamera.transform.TransformPoint(fbl),
                fbr,//shadowCamera.transform.TransformPoint(fbr),
                Color.blue);
            Debug.DrawLine( // left
                fbl,//shadowCamera.transform.TransformPoint(fbl),
                ftl,//shadowCamera.transform.TransformPoint(ftl),
                Color.blue);
            Debug.DrawLine( // top
                ftl,//shadowCamera.transform.TransformPoint(ftl),
                ftr,//shadowCamera.transform.TransformPoint(ftr),
                Color.blue);
            Debug.DrawLine( // right
                fbr,//shadowCamera.transform.TransformPoint(fbr),
                ftr,//shadowCamera.transform.TransformPoint(ftr),
                Color.blue);
            //back
            Debug.DrawLine( // bottom
                bbl,//shadowCamera.transform.TransformPoint(bbl),
                bbr,//shadowCamera.transform.TransformPoint(bbr),
                Color.green);
            Debug.DrawLine( // left
                bbl,//shadowCamera.transform.TransformPoint(bbl),
                btl,// shadowCamera.transform.TransformPoint(btl),
                Color.green);
            Debug.DrawLine( // top
                btl,//shadowCamera.transform.TransformPoint(btl),
                btr,//shadowCamera.transform.TransformPoint(btr),
                Color.green);
            Debug.DrawLine( // right
                bbr,//shadowCamera.transform.TransformPoint(bbr),
                btr,//shadowCamera.transform.TransformPoint(btr),
                Color.green);
             */

            Debug.DrawLine( // bottom
                shadowCamera.transform.TransformPoint(new Vector3(minX, minY, minZ)),
                shadowCamera.transform.TransformPoint(new Vector3(maxX, minY, minZ)),
                Color.yellow);
            Debug.DrawLine( // left
                shadowCamera.transform.TransformPoint(new Vector3(minX, minY, minZ)),
                shadowCamera.transform.TransformPoint(new Vector3(minX, maxY, minZ)),
                Color.yellow);
            Debug.DrawLine( // top
                shadowCamera.transform.TransformPoint(new Vector3(minX, maxY, minZ)),
                shadowCamera.transform.TransformPoint(new Vector3(maxX, maxY, minZ)),
                Color.yellow);
            Debug.DrawLine( // right
                shadowCamera.transform.TransformPoint(new Vector3(maxX, minY, minZ)),
                shadowCamera.transform.TransformPoint(new Vector3(maxX, maxY, minZ)),
                Color.yellow);
        }

        private void OnEnable()
        {
            renderer = GetComponent<Renderer>();

            mpb = new MaterialPropertyBlock();
            // We only allow for a finite number of casters, denoted by a constant value
            if (casters.Count < TestRenderPipeline.MAX_SHADOWMAPS)
            {
                index = casters.Count;
                casterID = 1 << index;
                casters.Add(this);
                renderer.GetPropertyBlock(mpb);
                mpb.SetFloat(
                    ShaderLib.Variables.Renderer.SHADOW_INDEX, 
                    casterID);
                renderer.SetPropertyBlock(mpb);
            }
            else
                Debug.LogWarning(string.Format(
                    "There are already the maximum allowed active ShadowCasters in the Scene ({0}). " +
                    "Remove or Disable one of the other ShadowCasters first before adding another.",
                    TestRenderPipeline.MAX_SHADOWMAPS));
        }

        private void OnDisable()
        {
            if (casters.Contains(this))
                casters.Remove(this);
            index = -1;
            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat(
                ShaderLib.Variables.Renderer.SHADOW_INDEX, 
                NO_SHADOW_INDEX);
            renderer.SetPropertyBlock(mpb);
        }

        public void SetupShadowMatrices(
            int index, 
            Light shadowLight, 
            out Matrix4x4 view, 
            out Matrix4x4 proj,
            out float d)
        {
            SetupShadowCamera(index, shadowLight, renderer);
            view = shadowCamera.worldToCameraMatrix;
            proj = shadowCamera.projectionMatrix;
            d = 1f/shadowCamera.farClipPlane;

        }
    }
}
