// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal static class UIRUtility
    {
        public static readonly string k_DefaultShaderName = "Hidden/Internal-UIRDefault";

        public static void FreeDrawChain(IUIRenderDevice renderDevice, RendererBase begin)
        {
            var renderer = begin;
            while (renderer != null)
            {
                FreeRenderer(renderDevice, renderer);

                if ((renderer.rendererType & RendererTypes.ContentRenderer) != 0)
                {
                    FreeDrawChain(renderDevice, renderer.contents);
                }

                renderer = renderer.next;
            }
        }

        private static void FreeRenderer(IUIRenderDevice renderDevice, RendererBase renderer)
        {
            if (renderer.rendererType == RendererTypes.MeshRenderer)
            {
                var meshRenderer = renderer as UIR.MeshRenderer;

                var meshNode = meshRenderer.meshChain;
                while (meshNode != null)
                {
                    renderDevice.Free(meshNode.mesh);
                    meshNode = meshNode.next;
                }
            }
            else if (renderer.rendererType == RendererTypes.MaskRenderer)
            {
                var maskRenderer = (MaskRenderer)renderer;
                renderDevice.Free(maskRenderer.maskRegister.mesh);
                renderDevice.Free(maskRenderer.maskUnregister.mesh);
            }
        }

        public static void UpdateSkinningTransform(IUIRenderDevice renderDevice, UIRenderData transformData)
        {
            if (transformData.skinningAlloc.size == 0)
                return;
            var transform = transformData.visualElement.worldTransform;
            var viewTransformElement = transformData.effectiveViewTransformData?.visualElement;
            if (viewTransformElement != null)
            {
                Matrix4x4 inverseViewTransform = viewTransformElement.worldTransform.inverse;
                transform = inverseViewTransform * transform;
            }
            renderDevice.UpdateTransform(transformData.skinningAlloc, transform);
        }

        public static void UpdateClippingRect(IUIRenderDevice renderDevice, UIRenderData renderData)
        {
            VisualElement currentElement = renderData.visualElement;

            // Reset inherited clipping data.
            UIRenderData inheritedClippingData = renderData.inheritedClippingRectData;
            Rect worldClippingRect, viewClippingRect, skinningClippingRect;

            if (inheritedClippingData != null)
            {
                worldClippingRect = inheritedClippingData.worldClippingRect;
                viewClippingRect = inheritedClippingData.viewClippingRect;
                skinningClippingRect = inheritedClippingData.skinningClippingRect;
            }
            else
            {
                worldClippingRect = s_InfiniteRect;
                viewClippingRect = s_InfiniteRect;
                skinningClippingRect = s_InfiniteRect;
            }

            // Combine with the current data.
            Rect worldRect = currentElement.LocalToWorld(currentElement.rect);
            if (renderData.effectiveSkinningTransformData != null)
            {
                Rect convertedRect = renderData.effectiveSkinningTransformData.visualElement.WorldToLocal(worldRect);

                if (inheritedClippingData != null)
                    skinningClippingRect = IntersectRects(convertedRect, skinningClippingRect);
                else
                    skinningClippingRect = convertedRect;
            }
            else if (renderData.effectiveViewTransformData != null)
            {
                Rect convertedRect = renderData.effectiveViewTransformData.visualElement.WorldToLocal(worldRect);

                if (inheritedClippingData != null)
                    viewClippingRect = IntersectRects(convertedRect, viewClippingRect);
                else
                    viewClippingRect = convertedRect;
            }
            else
            {
                if (inheritedClippingData != null)
                    worldClippingRect = IntersectRects(worldRect, worldClippingRect);
                else
                    worldClippingRect = worldRect;
            }

            renderDevice.UpdateClipping(
                renderData.clippingRectAlloc,
                worldClippingRect,
                viewClippingRect,
                skinningClippingRect);

            renderData.worldClippingRect = worldClippingRect;
            renderData.viewClippingRect = viewClippingRect;
            renderData.skinningClippingRect = skinningClippingRect;
        }

        public static void RemoveClippingRect(IUIRenderDevice renderDevice, UIRenderData renderData)
        {
            if (renderData.overridesClippingRect)
            {
                renderDevice.FreeClipping(renderData.clippingRectAlloc);
                renderData.overridesClippingRect = false;
            }
        }

        public static bool IsRoundRect(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            return !(style.borderTopLeftRadius < Mathf.Epsilon &&
                style.borderTopRightRadius < Mathf.Epsilon &&
                style.borderBottomLeftRadius < Mathf.Epsilon &&
                style.borderBottomRightRadius < Mathf.Epsilon);
        }

        public static Rect IntersectRects(Rect r0, Rect r1)
        {
            var r = new Rect(0, 0, 0, 0);
            r.x = Math.Max(r0.x, r1.x);
            r.y = Math.Max(r0.y, r1.y);
            r.xMax = Math.Max(r.x, Math.Min(r0.xMax, r1.xMax));
            r.yMax = Math.Max(r.y, Math.Min(r0.yMax, r1.yMax));
            return r;
        }

        public static void Destroy(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static Vector2 GetRenderTargetSize()
        {
            Vector2 targetSize;

            RenderTexture rt = RenderTexture.active;
            if (rt != null)
            {
                targetSize.x = rt.width;
                targetSize.y = rt.height;
            }
            else
            {
                targetSize.x = Screen.width;
                targetSize.y = Screen.height;
            }

            return targetSize;
        }

        public static bool IsSkinnedTransformWithoutNesting(VisualElement ve)
        {
            // We should not rely on the RenderHint.SkinningTransform flag here, since element
            // transforms may not be skinned even with this flag in the case where compute buffers
            // aren't available and the constant buffer is full.  Looking for a valid skinning allocation
            // is more accurate.
            if (ve == null || ve.uiRenderData == null || ve.uiRenderData.skinningAlloc.size == 0)
                return false;

            ve = ve.hierarchy.parent;
            while (ve != null)
            {
                if (ve.uiRenderData != null && ve.uiRenderData.skinningAlloc.size > 0)
                    return false;
                ve = ve.hierarchy.parent;
            }

            return true;
        }

        public static bool IsViewTransformWithoutNesting(VisualElement ve)
        {
            if (ve == null || (ve.renderHint & RenderHint.ViewTransform) == 0)
                return false;

            ve = ve.hierarchy.parent;
            while (ve != null)
            {
                if ((ve.renderHint & RenderHint.ViewTransform) != 0)
                    return false;
                ve = ve.hierarchy.parent;
            }

            return true;
        }

        public static readonly Rect s_InfiniteRect = new Rect(-1000000, -1000000, 2000000, 2000000);

        public static bool GetOpenGLCoreVersion(out int major, out int minor)
        {
            var version = SystemInfo.graphicsDeviceVersion;
            var rx = new Regex(@"OpenGL( *)[0-9].[0-9]");
            var matches = rx.Matches(version);
            if (matches.Count == 0)
            {
                major = minor = -1;
                return false;
            }
            var match = matches[0].Value;
            major = match[match.Length - 3] - '0';
            minor = match[match.Length - 1] - '0';
            return true;
        }
    }
}
