// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    static class BackdropFilterHelper
    {
        // Not thread-safe; UIR rendering is sequential. Revisit if panel processing becomes parallel.
        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        [NoAutoStaticsCleanup]
        static bool s_CustomFilterWarningLogged;

        static bool GetForceGammaRendering(VisualElement ve)
        {
            return ve.panel is BaseVisualElementPanel basePanel && basePanel.panelRenderer != null
                ? basePanel.panelRenderer.forceGammaRendering
                : false;
        }

        static RenderTextureReadWrite GetColorSpace(bool forceGamma)
        {
            var colorspace = QualitySettings.activeColorSpace;
            if (forceGamma)
                colorspace = ColorSpace.Gamma;

            return (colorspace == ColorSpace.Linear)
                ? RenderTextureReadWrite.Linear  // Linear space → SRGB format
                : RenderTextureReadWrite.Default; // Gamma space → UNorm format
        }

        // Reserves the TextureId. The actual GPU texture is bound to it later, during command
        // execution by GenerateBackdropFilterTexture.
        public static void AllocBackdropFilterTextureId(RenderTreeManager renderTreeManager, RenderData owner)
        {
            if (owner.backdropFilterTextureId.IsValid())
                return;

            owner.backdropFilterTextureId = renderTreeManager.textureRegistry.AllocAndAcquireDynamic();
        }

        // Releases the TextureId and any pooled temporary RT. Safe to call when no resources are held.
        public static void ReleaseBackdropFilterResources(RenderTreeManager renderTreeManager, RenderData owner)
        {
            if (owner.backdropFilterTextureId.IsValid())
            {
                renderTreeManager.textureRegistry.Release(owner.backdropFilterTextureId);
                owner.backdropFilterTextureId = TextureId.invalid;
            }

            if (owner.backdropFilterTemporaryTexture != null)
            {
                RenderTexture.ReleaseTemporary(owner.backdropFilterTemporaryTexture);
                owner.backdropFilterTemporaryTexture = null;
            }
        }

        // Recomputed every mesh-record pass: the UV corners depend on the world transform.
        public static void UpdateBackdropFilterUVCorners(VisualElement ve, RenderData owner)
        {
            Rect worldBound = ve.worldBound;
            if (worldBound.width <= UIRUtility.k_Epsilon || worldBound.height <= UIRUtility.k_Epsilon)
                return;

            ComputeBackdropFilterUVCorners(ve, worldBound, owner);
        }

        // Maps each local corner to world space, then to a UV within the captured worldBound (handles rotation).
        static void ComputeBackdropFilterUVCorners(VisualElement ve, Rect worldBound, RenderData owner)
        {
            var veSize = ve.layoutSize;
            Matrix4x4 worldTransform = ve.worldTransform;

            // Local corners: BL(0,h), TL(0,0), TR(w,0), BR(w,h)
            Vector3 localBL = new Vector3(0, veSize.y, 0);
            Vector3 localTL = new Vector3(0, 0, 0);
            Vector3 localTR = new Vector3(veSize.x, 0, 0);
            Vector3 localBR = new Vector3(veSize.x, veSize.y, 0);

            Vector3 worldBL = worldTransform.MultiplyPoint3x4(localBL);
            Vector3 worldTL = worldTransform.MultiplyPoint3x4(localTL);
            Vector3 worldTR = worldTransform.MultiplyPoint3x4(localTR);
            Vector3 worldBR = worldTransform.MultiplyPoint3x4(localBR);

            // UV = (worldPos - worldBound.min) / size. V is flipped: screen Y is down, texture V=0 is bottom.
            float invWidth = worldBound.width > UIRUtility.k_Epsilon ? 1f / worldBound.width : 0f;
            float invHeight = worldBound.height > UIRUtility.k_Epsilon ? 1f / worldBound.height : 0f;

            owner.backdropFilterUVBottomLeft = new Vector2(
                (worldBL.x - worldBound.x) * invWidth,
                1f - (worldBL.y - worldBound.y) * invHeight
            );
            owner.backdropFilterUVTopLeft = new Vector2(
                (worldTL.x - worldBound.x) * invWidth,
                1f - (worldTL.y - worldBound.y) * invHeight
            );
            owner.backdropFilterUVTopRight = new Vector2(
                (worldTR.x - worldBound.x) * invWidth,
                1f - (worldTR.y - worldBound.y) * invHeight
            );
            owner.backdropFilterUVBottomRight = new Vector2(
                (worldBR.x - worldBound.x) * invWidth,
                1f - (worldBR.y - worldBound.y) * invHeight
            );
        }

        // Captures the backdrop region, applies filters, and binds the result to the (pre-allocated) TextureId.
        // The output RenderTexture is stored in RenderData and released next frame.
        public static void GenerateBackdropFilterTexture(DrawParams drawParams, VisualElement ve, float pixelsPerPoint, RenderData owner)
        {
            var textureRegistry = owner.renderTree.renderTreeManager.textureRegistry;

            // The TextureId should already be allocated during mesh generation
            if (!owner.backdropFilterTextureId.IsValid())
                return;

            Rect worldBound = ve.worldBound;
            if (worldBound.width <= UIRUtility.k_Epsilon || worldBound.height <= UIRUtility.k_Epsilon)
                return;

            // Convert to pixel coordinates and calculate capture region
            RectInt pixelRect = RenderChainCommand.RectPointsToPixelsAndFlipYAxis(worldBound, drawParams.boundsMin, pixelsPerPoint);
            if (pixelRect.width <= 0 || pixelRect.height <= 0)
                return;

            RectInt captureRect = pixelRect;

            // Clamp to current scissor rect to avoid capturing outside valid region
            Rect scissorRect = drawParams.scissor.Peek();
            RectInt scissorRectInt = RenderChainCommand.RectPointsToPixelsAndFlipYAxis(scissorRect, drawParams.boundsMin, pixelsPerPoint);

            captureRect.xMin = Mathf.Max(captureRect.xMin, scissorRectInt.xMin);
            captureRect.yMin = Mathf.Max(captureRect.yMin, scissorRectInt.yMin);
            captureRect.xMax = Mathf.Min(captureRect.xMax, scissorRectInt.xMax);
            captureRect.yMax = Mathf.Min(captureRect.yMax, scissorRectInt.yMax);

            if (captureRect.width <= 0 || captureRect.height <= 0)
                return;

            // The active color target (e.g. URP's overlay-composite buffer). Null when the pipeline draws
            // straight to the backbuffer or inside a native pass — backdrop-filter is unsupported there, skip.
            RenderTexture source = RenderTexture.active;
            if (source == null)
                return;

            // Clamp to source bounds; worldBound can extend past the RT (custom Camera.rect, split-screen),
            // which would make the CopyTexture below throw on out-of-range coords.
            captureRect.xMin = Mathf.Max(captureRect.xMin, 0);
            captureRect.yMin = Mathf.Max(captureRect.yMin, 0);
            captureRect.xMax = Mathf.Min(captureRect.xMax, source.width);
            captureRect.yMax = Mathf.Min(captureRect.yMax, source.height);

            if (captureRect.width <= 0 || captureRect.height <= 0)
                return;

            bool forceGamma = GetForceGammaRendering(ve);
            RenderTextureReadWrite colorSpace = GetColorSpace(forceGamma);
            bool readsGamma = forceGamma || QualitySettings.activeColorSpace == ColorSpace.Gamma;

            // Release only after UpdateDynamic rebinds the TextureId below; releasing now could let the
            // GetTemporary calls recycle this RT while it's still bound.
            RenderTexture previousFrameRT = owner.backdropFilterTemporaryTexture;

            RenderTexture backdrop = CaptureBackdrop(source, captureRect, colorSpace);
            if (backdrop == null)
                return;

            RenderTexture filtered = ApplyBackdropFilters(backdrop, ve, pixelsPerPoint, colorSpace, readsGamma);

            // Output is the element's full pixel rect; the capture is blitted into its matching sub-rect.
            RenderTexture outputTexture = RenderTexture.GetTemporary(
                pixelRect.width,
                pixelRect.height,
                0,
                RenderTextureFormat.ARGB32,
                colorSpace
            );
            outputTexture.filterMode = FilterMode.Bilinear;

            int destX = captureRect.xMin - pixelRect.xMin;
            int destY = pixelRect.yMax - captureRect.yMax; // top-left origin: flip Y
            BlitToTarget(filtered, outputTexture, new RectInt(destX, destY, captureRect.width, captureRect.height));

            textureRegistry.UpdateDynamic(owner.backdropFilterTextureId, outputTexture);
            owner.backdropFilterTemporaryTexture = outputTexture;

            // Now safe to release the previous frame's RT: the TextureId no longer references it.
            if (previousFrameRT != null)
                RenderTexture.ReleaseTemporary(previousFrameRT);

            if (filtered != backdrop)
                RenderTexture.ReleaseTemporary(filtered);
            RenderTexture.ReleaseTemporary(backdrop);
        }

        static void BlitToTarget(RenderTexture source, RenderTexture target, RectInt destRect)
        {
            RenderTexture oldRT = RenderTexture.active;

            RenderTexture.active = target;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, target.width, target.height, 0);

            // No GL.Clear needed: `filtered` (alpha = 1) overwrites destRect, and texels outside it are never
            // sampled — the quad is scissor-clipped to the captured region when finally drawn.
            Graphics.DrawTexture(
                new Rect(destRect.x, destRect.y, destRect.width, destRect.height),
                source,
                new Rect(0, 0, 1, 1),
                0, 0, 0, 0
            );

            GL.PopMatrix();

            RenderTexture.active = oldRT;
        }

        static RenderTexture CaptureBackdrop(Texture source, RectInt region, RenderTextureReadWrite colorSpace)
        {
            if (region.width <= 0 || region.height <= 0)
                return null;

            // Match the source RT's format so the CopyTexture below is format-compatible.
            RenderTextureFormat format = source is RenderTexture sourceRT ? sourceRT.format : RenderTextureFormat.ARGB32;
            RenderTexture backdrop = RenderTexture.GetTemporary(
                region.width,
                region.height,
                0,  // No depth buffer needed
                format,
                colorSpace
            );

            backdrop.filterMode = FilterMode.Bilinear;

            // Byte-for-byte GPU copy; avoids DrawTexture's alpha-blend and sub-pixel drift. Bottom-left origin pixels.
            Graphics.CopyTexture(source, 0, 0, region.xMin, region.yMin, region.width, region.height,
                                 backdrop, 0, 0, 0, 0);

            return backdrop;
        }

        static RenderTexture ApplyBackdropFilters(RenderTexture source, VisualElement ve, float pixelsPerPoint, RenderTextureReadWrite colorSpace, bool readsGamma)
        {
            var backdropFilters = ve.computedStyle.backdropFilter;

            // Custom filters are unsupported for backdrop-filter. One-shot warning: this runs every frame.
            if (!s_CustomFilterWarningLogged && HasCustomFilter(backdropFilters))
            {
                s_CustomFilterWarningLogged = true;
                Debug.LogWarning($"Custom filters are not supported for backdrop-filter on element '{ve.name}'. Custom filters will be ignored.");
            }

            // No pre-clear needed: ApplyFilterChain clears the block before each pass.
            return FilterHelper.ApplyFilterChain(
                source,
                backdropFilters,
                pixelsPerPoint,
                colorSpace,
                readsGamma,
                writesGamma: readsGamma,  // Backdrop-filter: same color space in and out
                s_PropertyBlock,
                usePixelMatrix: true,
                skipCustomFilters: true  // Custom filters not supported for backdrop-filter
            );
        }

        static bool HasCustomFilter(System.ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                if (filterFunc.type == FilterFunctionType.Custom)
                    return true;
            }
            return false;
        }
    }
}
