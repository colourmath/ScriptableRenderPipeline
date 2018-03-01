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
        static Camera shadowCamera;

        public static List<ShadowCaster> casters 
            = new List<ShadowCaster>(TestRenderPipeline.MAX_SHADOWMAPS);

        [System.NonSerialized]
        new Renderer renderer;
        [System.NonSerialized]
        int index = -1;

        static void SetupShadowCamera(Light l, Renderer r)
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

            Vector3 boundsMin = shadowCamera.transform.InverseTransformPoint(r.transform.TransformPoint(r.bounds.min));
            Vector3 boundsMax = shadowCamera.transform.InverseTransformPoint(r.transform.TransformPoint(r.bounds.max));

            Vector3 fbl, fbr, ftl, ftr, bbl, bbr, btl, btr;
            fbl = boundsMin;
            fbr = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
            ftl = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
            ftr = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
            bbl = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
            bbr = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
            btl = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
            btr = boundsMax;

            float minX = Mathf.Min(fbl.x, fbr.x, ftl.x, ftr.x, bbl.x, bbr.x, btl.x, btr.x);
            float maxX = Mathf.Max(fbl.x, fbr.x, ftl.x, ftr.x, bbl.x, bbr.x, btl.x, btr.x);
            float minY = Mathf.Min(fbl.y, fbr.y, ftl.y, ftr.y, bbl.y, bbr.y, btl.y, btr.y);
            float maxY = Mathf.Max(fbl.y, fbr.y, ftl.y, ftr.y, bbl.y, bbr.y, btl.y, btr.y);
            float minZ = Mathf.Min(fbl.z, fbr.z, ftl.z, ftr.z, bbl.z, bbr.z, btl.z, btr.z);
            float maxZ = Mathf.Max(fbl.z, fbr.z, ftl.z, ftr.z, bbl.z, bbr.z, btl.z, btr.z);

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            shadowCamera.farClipPlane = Mathf.Abs(maxZ - minZ) * 2;

            if (shadowCamera.orthographic)
                shadowCamera.orthographicSize = .5f * (maxY - minY);
            else
                shadowCamera.fieldOfView = Vector3.Angle(min, max);
        }

        private void OnEnable()
        {
            renderer = GetComponent<Renderer>();

            // We only allow for a finite number of casters, denoted by a constant value
            if (casters.Count < TestRenderPipeline.MAX_SHADOWMAPS)
            {
                index = casters.Count;
                casters.Add(this);
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
        }

        //private void OnWillRenderObject()
        //{
        //
        //}

        public void SetupShadowMatrices(Light shadowLight, out Matrix4x4 view, out Matrix4x4 proj)
        {
            SetupShadowCamera(shadowLight, renderer);
            view = shadowCamera.worldToCameraMatrix;
            proj = shadowCamera.projectionMatrix;
        }
    }
}
