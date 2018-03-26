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

        public static List<ShadowCaster> casters 
            = new List<ShadowCaster>(TestRenderPipeline.MAX_SHADOWMAPS);

        // TODO: Make this less arbitrary by translating to texels
        // TODO: Make this applicable to perspective as well
        [Tooltip("Padding for edge of shadow map (world units for now, will eventually move to texels)")]
        public float padding = 1f;

        [System.NonSerialized]
        new public Renderer renderer; // GIVE THESE BACK, UNITY!!
        [System.NonSerialized]
        int index = -1;

        static Rect[] PixelRects = new Rect[4]
        {
            new Rect(0f,0f,.5f,.5f),
            new Rect(.5f,0f,.5f,.5f),
            new Rect(0f,.5f,.5f,.5f),
            new Rect(.5f,.5f,.5f,.5f)
        };

        MaterialPropertyBlock mpb;

        /// <summary>
        /// Given a Renderer and a Light, calculates the Shadow View and Projection Matrices,
        /// as well as a distance value describing the far clip plane of the projection.
        /// </summary>
        static void CalculateShadowMatrices(
            int index, 
            Light l, 
            Renderer r,
            out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix,
            out float distance)
        {
            float padding = casters[index].padding;

            float aspect = 1f;
            float nearClip = 0.01f;
            float farClip;
            float fov = 60;

            Vector3 position = Vector3.zero;
            Vector3 forward = Vector3.forward;
            Quaternion rotation = Quaternion.identity;

            Matrix4x4 projectionMatrix = Matrix4x4.identity;

            // The light type should dictate the type of projection we're building
            bool ortho = 
                l.type == LightType.Directional || l.type == LightType.Area;

            // Calculate a Shadow 'Camera' manually
            if (ortho)
            {
                forward = l.transform.forward;
                rotation = l.transform.rotation;
                rotation = Quaternion.LookRotation(forward, Vector3.up);

                position = l.type == LightType.Area ?
                    l.transform.position :
                    r.transform.position - (forward * r.bounds.extents.magnitude);
            }
            else
            {
                position = l.transform.position;
                forward = r.transform.position - position;
                rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            // Our world-to-shadow matrix is just the inverse of our TRS
            Matrix4x4 worldToShadow = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
            
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

            fbl = worldToShadow.MultiplyPoint3x4(fbl);
            fbr = worldToShadow.MultiplyPoint3x4(fbr);
            ftl = worldToShadow.MultiplyPoint3x4(ftl);
            ftr = worldToShadow.MultiplyPoint3x4(ftr);
            bbl = worldToShadow.MultiplyPoint3x4(bbl);
            bbr = worldToShadow.MultiplyPoint3x4(bbr);
            btl = worldToShadow.MultiplyPoint3x4(btl);
            btr = worldToShadow.MultiplyPoint3x4(btr);

            float minX = Mathf.Min(fbl.x, Mathf.Min(fbr.x, Mathf.Min(ftl.x, Mathf.Min(ftr.x, Mathf.Min(bbl.x, Mathf.Min(bbr.x, Mathf.Min(btl.x, btr.x)))))));
            float maxX = Mathf.Max(fbl.x, Mathf.Max(fbr.x, Mathf.Max(ftl.x, Mathf.Max(ftr.x, Mathf.Max(bbl.x, Mathf.Max(bbr.x, Mathf.Max(btl.x, btr.x)))))));
            float minY = Mathf.Min(fbl.y, Mathf.Min(fbr.y, Mathf.Min(ftl.y, Mathf.Min(ftr.y, Mathf.Min(bbl.y, Mathf.Min(bbr.y, Mathf.Min(btl.y, btr.y)))))));
            float maxY = Mathf.Max(fbl.y, Mathf.Max(fbr.y, Mathf.Max(ftl.y, Mathf.Max(ftr.y, Mathf.Max(bbl.y, Mathf.Max(bbr.y, Mathf.Max(btl.y, btr.y)))))));
            float minZ = Mathf.Min(fbl.z, Mathf.Min(fbr.z, Mathf.Min(ftl.z, Mathf.Min(ftr.z, Mathf.Min(bbl.z, Mathf.Min(bbr.z, Mathf.Min(btl.z, btr.z)))))));
            float maxZ = Mathf.Max(fbl.z, Mathf.Max(fbr.z, Mathf.Max(ftl.z, Mathf.Max(ftr.z, Mathf.Max(bbl.z, Mathf.Max(bbr.z, Mathf.Max(btl.z, btr.z)))))));

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            farClip = l.type == LightType.Directional ? maxZ : l.range;

            //DebugShadowFrustum(
            //    minX, minY, minZ,
            //    maxX, maxY, maxZ,
            //    fbl,fbr,ftl,ftr,
            //    bbl,bbr,btl, btr,
            //    toShadowCaster.inverse);

            if (ortho)
            {
                float size = .5f * (maxY - minY) + padding;

                projectionMatrix = Matrix4x4.Ortho(
                    -aspect * size, aspect * size,
                    -size, size,
                    nearClip, farClip);
            }
            else
            {
                fov = Vector3.Angle(max, min);
                projectionMatrix = Matrix4x4.Perspective(fov, aspect, nearClip, farClip);
            }

            // Third row (Z) needs to be inverted when sending off to the Graphics API,
            // which is facing opposite Z
            worldToShadow.SetRow(2, -worldToShadow.GetRow(2));

            viewMatrix = worldToShadow;
            projMatrix = projectionMatrix;
            distance = 1f / farClip;
        }

        /// <summary>
        /// Draws debug visuals for world-space and shadow-space AABB extents
        /// </summary>
        static void DebugShadowFrustum(
            float minX, float minY, float minZ, 
            float maxX, float maxY, float maxZ,
            Vector3 fbl, Vector3 fbr, Vector3 ftl, Vector3 ftr,
            Vector3 bbl, Vector3 bbr, Vector3 btl, Vector3 btr,
            Matrix4x4 localToWorld)
        {
            
            //top
            Debug.DrawLine( // bottom
                localToWorld.MultiplyPoint3x4(fbl),
                localToWorld.MultiplyPoint3x4(fbr),
                Color.blue);
            Debug.DrawLine( // left
                localToWorld.MultiplyPoint3x4(fbl),
                localToWorld.MultiplyPoint3x4(ftl),
                Color.blue);
            Debug.DrawLine( // top
                localToWorld.MultiplyPoint3x4(ftl),
                localToWorld.MultiplyPoint3x4(ftr),
                Color.blue);
            Debug.DrawLine( // right
                localToWorld.MultiplyPoint3x4(fbr),
                localToWorld.MultiplyPoint3x4(ftr),
                Color.blue);
            //back
            Debug.DrawLine( // bottom
                localToWorld.MultiplyPoint3x4(bbl),
                localToWorld.MultiplyPoint3x4(bbr),
                Color.green);
            Debug.DrawLine( // left
                localToWorld.MultiplyPoint3x4(bbl),
                localToWorld.MultiplyPoint3x4(btl),
                Color.green);
            Debug.DrawLine( // top
                localToWorld.MultiplyPoint3x4(btl),
                localToWorld.MultiplyPoint3x4(btr),
                Color.green);
            Debug.DrawLine( // right
                localToWorld.MultiplyPoint3x4(bbr),
                localToWorld.MultiplyPoint3x4(btr),
                Color.green);

            Debug.DrawLine( // bottom
                localToWorld.MultiplyPoint3x4(new Vector3(minX, minY, minZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, minY, minZ)),
                Color.yellow);
            Debug.DrawLine( // left
                localToWorld.MultiplyPoint3x4(new Vector3(minX, minY, minZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(minX, maxY, minZ)),
                Color.yellow);
            Debug.DrawLine( // top
                localToWorld.MultiplyPoint3x4(new Vector3(minX, maxY, minZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, maxY, minZ)),
                Color.yellow);
            Debug.DrawLine( // right
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, minY, minZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, maxY, minZ)),
                Color.yellow);

            Debug.DrawLine( // bottom
                localToWorld.MultiplyPoint3x4(new Vector3(minX, minY, maxZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, minY, maxZ)),
                Color.red);
            Debug.DrawLine( // left
                localToWorld.MultiplyPoint3x4(new Vector3(minX, minY, maxZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(minX, maxY, maxZ)),
                Color.red);
            Debug.DrawLine( // top
                localToWorld.MultiplyPoint3x4(new Vector3(minX, maxY, maxZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, maxY, maxZ)),
                Color.red);
            Debug.DrawLine( // right
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, minY, maxZ)),
                localToWorld.MultiplyPoint3x4(new Vector3(maxX, maxY, maxZ)),
                Color.red);
        }

        static int AddCaster(ShadowCaster c)
        {
            casters.Add(c);
            return casters.Count - 1;
        }

        static void RemoveCaster(ShadowCaster c)
        {
            casters.Remove(c);
            for (int i = 0; i < casters.Count; i++)
            {
                casters[i].index = 1 << i;
                casters[i].ApplyPropertyBlock();
            }
        }

        void ApplyPropertyBlock()
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat(
                ShaderLib.Variables.Renderer.SHADOW_INDEX,
                index);
            renderer.SetPropertyBlock(mpb);
        }

        private void OnEnable()
        {
            renderer = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            
            // We only allow for a finite number of casters, denoted by a constant value
            if (casters.Count < TestRenderPipeline.MAX_SHADOWMAPS)
            {
                int i = AddCaster(this);
                index = 1 << i;
                ApplyPropertyBlock();
            }
            else
                Debug.LogWarning(string.Format(
                    "There are already the maximum allowed active ShadowCasters in the Scene ({0}). " +
                    "Remove or Disable one of the other ShadowCasters first before adding another.",
                    TestRenderPipeline.MAX_SHADOWMAPS));
        }

        private void OnDisable()
        {
            RemoveCaster(this); // Remove this caster and update all others
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
            CalculateShadowMatrices(index, shadowLight, renderer, out view, out proj, out d);
        }
    }
}
