// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    static partial class BackdropFilterHelper
    {
        // Not thread-safe; UIR rendering is sequential. Revisit if panel processing becomes parallel.
        [NoAutoStaticsCleanup] // Reused property block; ApplyFilterChain clears it before each pass
        static readonly MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        // The editor window's sampleable back buffer (the GUIView aux RT) and its content row order
        // (isTopOrigin: texel row 0 is the top of the window; platform windowing convention).
        [AutoStaticsCleanupOnCodeReload] // delegate into editor code; RetainedMode's cctor re-registers it after reload
        // Installed by RetainedMode.Initialize(), which runs on every code load, so the slot cleared by
        // cleanup is wired again before any editor UI uses it.
        [IgnoreForUAL0015("Editor IoC slot reinstalled on every code load by RetainedMode.Initialize()")]
        public static System.Func<(RenderTexture texture, bool isTopOrigin)> editorWindowBackdropSource { private get; set; }

        static readonly int s_ColorMatrixId = Shader.PropertyToID("_ColorMatrix");
        static readonly int s_ColorOffsetId = Shader.PropertyToID("_ColorOffset");
        static readonly int s_ColorInvertId = Shader.PropertyToID("_ColorInvert");

        [NoAutoStaticsCleanup] // persist the cached material; nulling it would orphan the native HideAndDontSave object
        static Material s_NormalizeMaterial;
        static Material normalizeMaterial
        {
            get
            {
                if (s_NormalizeMaterial == null)
                {
                    s_NormalizeMaterial = new Material(Shader.Find(Shaders.k_RuntimeColorEffect));
                    s_NormalizeMaterial.hideFlags = HideFlags.HideAndDontSave;
                    s_NormalizeMaterial.SetMatrix(s_ColorMatrixId, Matrix4x4.identity);
                    s_NormalizeMaterial.SetFloat(s_ColorOffsetId, 0f);
                    s_NormalizeMaterial.SetFloat(s_ColorInvertId, 0f);
                }
                return s_NormalizeMaterial;
            }
        }

        // Shaders sharing UnityUIEFilter.cginc read unity_uie_UVRect via GetFilterUVRect. The
        // compositor sets it per-pass for the regular `filter` style; backdrop-filter always
        // samples the full backdrop texture, so InvokeBackdropFilterCallbacks primes it to
        // (0,0,1,1).
        static readonly Vector4[] s_FullUVRectArray = new Vector4[] { new Vector4(0f, 0f, 1f, 1f) };

        static RenderTextureReadWrite GetColorSpace()
        {
            // Linear project -> sRGB temps, Gamma -> raw: the sRGB backdrop source decodes on sample and
            // the sRGB target encodes once on store. Raw temps here double-encoded on the sRGB game-view
            // target (the backdrop-filter wash). Matches the filter atlas RTs and the readsGamma below.
            return RenderTextureReadWrite.Default; // Linear -> sRGB, Gamma -> raw
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

            // Return the per-pass MPBs to the manager pool. On the element-removal path,
            // FreeExtraData has already done this (the extra data is gone by the time this runs).
            if (owner.hasExtraData)
                renderTreeManager.ReleaseFilterCallbackBlocks(renderTreeManager.GetExtraData(owner).backdropFilterCallbackPropertyBlocks);
        }

        // Scale of the element relative to its render tree root. The backdrop is captured
        // post-transform, so point-based filter parameters (blur sigma, shadow offsets, capture
        // margins) must scale by this on top of the DPI factor. Not the full world scale: a nested
        // tree's compositing transform re-applies the outer scale to the filtered output.
        static Vector2 ComputeContentScale(RenderData owner)
        {
            UIRUtility.ComputeMatrixRelativeToRenderTree(owner, out Matrix4x4 m);
            return GetScale(in m);
        }

        static Vector2 GetScale(in Matrix4x4 m)
        {
            return new Vector2(
                new Vector3(m.m00, m.m10, m.m20).magnitude,
                new Vector3(m.m01, m.m11, m.m21).magnitude);
        }

        // Smallest per-pass sigma worth downsampling for; below it the kernel is already cheap.
        const float k_MinChainSigma = 2f;

        // Largest built-in sigma in the chain, in points. Custom filters contribute nothing: their
        // cost is unknown, so they never trigger downscaling and always run at full resolution.
        static float ComputeMaxChainSigma(System.ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            float maxSigma = 0f;
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                if (filterFunc.type == FilterFunctionType.Blur && filterFunc.parameterCount > 0)
                    maxSigma = Mathf.Max(maxSigma, filterFunc.parameters[0].floatValue);
                else if (filterFunc.type == FilterFunctionType.DropShadow && filterFunc.parameterCount > 2)
                    maxSigma = Mathf.Max(maxSigma, filterFunc.parameters[2].floatValue);
            }
            return maxSigma;
        }

        // Number of 2x downscale steps the filter chain runs at, keeping the kernel cost bounded when
        // the content scale grows (blur(img downscaled by k, sigma/k) upscaled ~= blur(img, sigma)).
        // Power of two so the update and render phases compute the exact same factor. Bounded by the
        // content scale (0 at scale <= 1, so unscaled rendering stays bit-identical; never below
        // authored resolution) and by the chain's largest sigma (a chain without a built-in blur,
        // e.g. tint-only, must stay crisp).
        public static int ComputeDownscaleShift(System.ReadOnlySpan<UnmanagedFilterFunction> filters, Vector2 contentScale, float scaledPixelsPerPoint)
        {
            float minScale = Mathf.Min(contentScale.x, contentScale.y);
            if (minScale <= 1f)
                return 0;

            float sigmaDevice = ComputeMaxChainSigma(filters) * scaledPixelsPerPoint * Mathf.Sqrt(contentScale.x * contentScale.y);
            float budget = Mathf.Min(minScale, sigmaDevice / k_MinChainSigma);
            if (budget <= 1f)
                return 0;

            return Mathf.FloorToInt(Mathf.Log(budget, 2f));
        }

        // Update-phase entry point: populates the per-pass blocks while no render target is bound.
        public static void InvokeBackdropFilterCallbacks(RenderTreeManager renderTreeManager, RenderData owner)
        {
            VisualElement ve = owner.owner;
            if (ve == null)
                return;

            var backdropFilters = ve.computedStyle.backdropFilter;
            if (backdropFilters.Length == 0)
                return;

            int passCount = FilterHelper.CountFilterChainPasses(backdropFilters);
            if (passCount == 0)
            {
                // Zero-pass chains still capture/blit the backdrop, so release only the blocks, not the texture.
                if (owner.hasExtraData)
                    renderTreeManager.ReleaseFilterCallbackBlocks(renderTreeManager.GetExtraData(owner).backdropFilterCallbackPropertyBlocks);
                return;
            }

            // A callback removing its own element frees the extra data mid-walk; keep a local reference.
            var extraData = renderTreeManager.GetOrAddExtraData(owner);
            var blocks = extraData.backdropFilterCallbackPropertyBlocks ??= new List<MaterialPropertyBlock>(passCount);
            renderTreeManager.SizeFilterCallbackBlocks(blocks, passCount);

            // GetColorSpace() returns Default (sRGB temps), so the shader reads linear even under force-gamma; params track the active color space only.
            bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;

            // Per-axis so a non-uniform content scale keeps drop-shadow offsets and per-pass blur
            // sigmas on the right axis; the downscale shift divides out what the chain will not
            // render at (see ComputeDownscaleShift).
            Vector2 contentScale = ComputeContentScale(owner);
            int downscaleShift = ComputeDownscaleShift(backdropFilters, contentScale, ve.scaledPixelsPerPoint);
            Vector2 pixelsPerPoint = contentScale * (ve.scaledPixelsPerPoint / (1 << downscaleShift));

            // Backdrop-filter preserves the color space across the chain, so every pass writes what it reads.
            FilterHelper.InvokeFilterCallbacks(
                backdropFilters,
                blocks,
                readsGamma: readsGamma,
                writesGamma: readsGamma,
                lastPassWritesGamma: readsGamma,
                pixelsPerPoint);

            // Renderer-owned (like _MainTex), so set after the callbacks. The Count re-check covers
            // a callback removing the element mid-walk.
            for (int i = 0; i < passCount && i < blocks.Count; i++)
                blocks[i].SetVectorArray(FilterHelper.s_UVRectId, s_FullUVRectArray);
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

            // UV = (worldPos - worldBound.min) / size. V is flipped: screen Y is down, texture V=0 is bottom.
            float invWidth = worldBound.width > UIRUtility.k_Epsilon ? 1f / worldBound.width : 0f;
            float invHeight = worldBound.height > UIRUtility.k_Epsilon ? 1f / worldBound.height : 0f;

            // Maps a local corner to world space, then to its UV within worldBound (handles rotation). Local
            // function called directly, so the capture of worldTransform/worldBound/inv* allocates nothing.
            Vector2 CornerUV(float localX, float localY)
            {
                Vector3 world = worldTransform.MultiplyPoint3x4(new Vector3(localX, localY, 0));
                return new Vector2(
                    (world.x - worldBound.x) * invWidth,
                    1f - (world.y - worldBound.y) * invHeight);
            }

            // Local corners: BL(0,h), TL(0,0), TR(w,0), BR(w,h)
            owner.backdropFilterUVBottomLeft = CornerUV(0, veSize.y);
            owner.backdropFilterUVTopLeft = CornerUV(0, 0);
            owner.backdropFilterUVTopRight = CornerUV(veSize.x, 0);
            owner.backdropFilterUVBottomRight = CornerUV(veSize.x, veSize.y);
        }

        // Captures the backdrop region, applies filters, and binds the result to the (pre-allocated) TextureId.
        // The output RenderTexture is stored in RenderData and released next frame.
        public static void GenerateBackdropFilterTexture(DrawParams drawParams, VisualElement ve, RenderData owner)
        {
            var textureRegistry = owner.renderTree.renderTreeManager.textureRegistry;

            // The TextureId should already be allocated during mesh generation
            if (!owner.backdropFilterTextureId.IsValid())
                return;

            Rect worldBound = ve.worldBound;
            if (worldBound.width <= UIRUtility.k_Epsilon || worldBound.height <= UIRUtility.k_Epsilon)
                return;

            // A render tree backed by a nested RT projects in the tree root's space, not panel space,
            // so it needs a different rect-to-pixel mapping (below).
            bool isNestedRT = owner.renderTree.rootRenderData.isNestedRenderTreeRoot;

            // In editor windows the "back buffer" is GUIView's sampleable aux RT (RenderTexture.active is null there).
            RenderTexture source = RenderTexture.active;
            bool sourceIsAuxBackBuffer = false;
            bool auxIsTopOrigin = false;
            if (source == null && ve.panel.contextType == ContextType.Editor)
            {
                (source, auxIsTopOrigin) = editorWindowBackdropSource?.Invoke() ?? default;
                sourceIsAuxBackBuffer = source != null;
            }
            if (source == null)
                return;

            Debug.Assert(!(isNestedRT && sourceIsAuxBackBuffer), "A nested render tree keeps RenderTexture.active non-null, so the aux back buffer is never its source.");

            // Single source of truth for the capture's row order: every rect below (inflation sides,
            // crop placement) must agree on it.
            bool topOriginRows = sourceIsAuxBackBuffer && auxIsTopOrigin;

            // Read margins (points, CSS sides): the capture is inflated only for passes that need real
            // neighborhood content (drop-shadow), so offset shadows stay exact. Blur kernels instead read
            // edge-clamped samples, matching how browsers clamp the backdrop at the element bounds.
            // InflateCapture maps the CSS sides onto the capture's row order.
            var chainMargins = FilterHelper.ComputeBackdropCaptureReadMargins(ve.computedStyle.backdropFilter);

            Rect drawBounds = drawParams.drawBounds;
            RectInt activeViewport = Utility.GetActiveViewport();
            float scaleX = drawParams.pixelScale.x;
            float scaleY = drawParams.pixelScale.y;

            RectInt pixelRect;
            RectInt captureRect;
            Vector2 contentScale;
            if (!isNestedRT)
            {
                pixelRect = RenderChainCommand.RectPointsToPixels(worldBound, drawBounds.min, scaleX, scaleY, activeViewport);
                if (pixelRect.width <= 0 || pixelRect.height <= 0)
                    return;

                // Clamp to the ancestor clip in the capture's own space: clippingRect (panel space, covers overflow:hidden)
                // for a normal element, but the scissor for a filtered element captured in a nested tree (UI-5094).
                Rect clipRect = object.ReferenceEquals(owner, ve.renderData) ? owner.clippingRect : drawParams.scissor.Peek();
                RectInt clipRectInt = RenderChainCommand.RectPointsToPixels(clipRect, drawBounds.min, scaleX, scaleY, activeViewport);

                // A top-origin aux RT needs the bottom-origin rects reflected about the full source
                // height (not the viewport) into raw-row space; bottom-origin rows already match.
                if (topOriginRows)
                {
                    pixelRect.y = source.height - pixelRect.y - pixelRect.height;
                    clipRectInt.y = source.height - clipRectInt.y - clipRectInt.height;
                }

                captureRect = pixelRect;
                contentScale = ComputeContentScale(owner);
                InflateCapture(ref captureRect, chainMargins, topOriginRows,
                    scaleX * contentScale.x, scaleY * contentScale.y);

                if (!ClampCapture(ref captureRect, clipRectInt))
                    return;
            }
            else
            {
                // Nested render texture: remap the element's local rect into the nested tree root's
                // space and through the active viewport. The ancestor scissor is clamped below in that same space.
                UIRUtility.ComputeMatrixRelativeToRenderTree(owner, out Matrix4x4 elementToTreeRoot);
                Rect localRect = new Rect(0, 0, ve.layoutSize.x, ve.layoutSize.y);
                Rect treeBound = VisualElement.CalculateConservativeRect(ref elementToTreeRoot, localRect);
                if (treeBound.width <= UIRUtility.k_Epsilon || treeBound.height <= UIRUtility.k_Epsilon)
                    return;

                pixelRect = RenderChainCommand.RectPointsToPixels(treeBound, drawBounds.min, scaleX, scaleY, activeViewport);
                if (pixelRect.width <= 0 || pixelRect.height <= 0)
                    return;

                captureRect = pixelRect;
                contentScale = GetScale(in elementToTreeRoot);
                InflateCapture(ref captureRect, chainMargins, topOriginRows,
                    scaleX * contentScale.x, scaleY * contentScale.y);

                // Clamp to the ancestor scissor, mapped through the same nested projection as pixelRect. The scissor
                // stack holds the correct tree-root-space clip here; owner.clippingRect would be the panel rect (UI-5094).
                Rect scissorRect = drawParams.scissor.Peek();
                RectInt scissorRectInt = RenderChainCommand.RectPointsToPixels(scissorRect, drawBounds.min, scaleX, scaleY, activeViewport);

                if (!ClampCapture(ref captureRect, scissorRectInt))
                    return;
            }

            // Clamp to source bounds; worldBound can extend past the RT (custom Camera.rect, split-screen),
            // which would make the CopyTexture below throw on out-of-range coords.
            if (!ClampCapture(ref captureRect, new RectInt(0, 0, source.width, source.height)))
                return;

            // Effective force-gamma state (the raw panel flag is false for editor panels); matches the filter compositor.
            bool forceGamma = owner.renderTree.renderTreeManager.forceGammaRendering;
            RenderTextureReadWrite colorSpace = GetColorSpace();

            // Release only after UpdateDynamic rebinds the TextureId below; releasing now could let the
            // GetTemporary calls recycle this RT while it's still bound.
            RenderTexture previousFrameRT = owner.backdropFilterTemporaryTexture;

            RenderTexture backdrop = CaptureBackdrop(source, captureRect, colorSpace);
            if (backdrop == null)
                return;

            // Flip a top-origin aux capture to bottom-origin; under force-gamma also decode its raw gamma values to linear.
            if (sourceIsAuxBackBuffer)
            {
                RenderTexture normalized = RenderTexture.GetTemporary(backdrop.width, backdrop.height, 0, backdrop.format, colorSpace);
                normalized.filterMode = FilterMode.Bilinear;

                // Not Graphics.Blit: it clobbers the projection matrix mid-EvaluateChain.
                var normalizePass = new PostProcessingPass { material = normalizeMaterial };
                s_PropertyBlock.Clear();
                FilterHelper.ApplyFilterPass(backdrop, normalized, normalizePass, s_PropertyBlock,
                    outputLinear: forceGamma,
                    sourceUVRect: auxIsTopOrigin ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1));

                RenderTexture.ReleaseTemporary(backdrop);
                backdrop = normalized;
            }

            // Run the chain on a downscaled capture when the content scale allows it; the update phase
            // divided the callbacks' sigma/offsets by the same power of two, so the visual result only
            // changes by the resampling. Stepped by 2x so each bilinear tap is a proper prefilter.
            int downscaleShift = ComputeDownscaleShift(ve.computedStyle.backdropFilter, contentScale, ve.scaledPixelsPerPoint);
            if (downscaleShift > 0)
            {
                var resamplePass = new PostProcessingPass { material = normalizeMaterial };
                for (int i = 0; i < downscaleShift; i++)
                {
                    RenderTexture half = RenderTexture.GetTemporary(
                        Mathf.Max(1, (backdrop.width + 1) >> 1),
                        Mathf.Max(1, (backdrop.height + 1) >> 1),
                        0, backdrop.format, colorSpace);
                    half.filterMode = FilterMode.Bilinear;
                    s_PropertyBlock.Clear();
                    FilterHelper.ApplyFilterPass(backdrop, half, resamplePass, s_PropertyBlock, outputLinear: false);
                    RenderTexture.ReleaseTemporary(backdrop);
                    backdrop = half;
                }
            }

            // Filtered alpha = captured coverage scaled by the filter chain (tint/opacity alpha<1 -> translucent,
            // empty capture -> transparent), so compositing premultiplied-over matches the runtime's backdrop opacity.
            RenderTexture filtered = ApplyBackdropFilters(backdrop, ve, owner, colorSpace);

            // Stretch the chain output back to capture resolution so the crop/copy below stays in
            // source pixels; bilinear upscaling of blurred content is visually lossless.
            if (downscaleShift > 0)
            {
                RenderTexture upscaled = RenderTexture.GetTemporary(captureRect.width, captureRect.height, 0, filtered.format, colorSpace);
                upscaled.filterMode = FilterMode.Bilinear;
                var resamplePass = new PostProcessingPass { material = normalizeMaterial };
                s_PropertyBlock.Clear();
                FilterHelper.ApplyFilterPass(filtered, upscaled, resamplePass, s_PropertyBlock, outputLinear: false);
                if (filtered != backdrop)
                    RenderTexture.ReleaseTemporary(filtered);
                RenderTexture.ReleaseTemporary(backdrop);
                backdrop = upscaled;
                filtered = upscaled;
            }

            void ReleaseCaptures()
            {
                if (filtered != backdrop)
                    RenderTexture.ReleaseTemporary(filtered);
                RenderTexture.ReleaseTemporary(backdrop);
            }

            // Crop the (margin-inflated) filtered capture back to the element rect.
            RectInt innerRect = captureRect;
            if (!ClampCapture(ref innerRect, pixelRect))
            {
                ReleaseCaptures();
                return;
            }

            // Output is the element's full pixel rect; the capture is blitted into its matching sub-rect.
            RenderTexture outputTexture = RenderTexture.GetTemporary(
                pixelRect.width,
                pixelRect.height,
                0,
                filtered.format, // Match filtered so the CopyTexture in BlitToTarget is format-compatible.
                colorSpace
            );
            outputTexture.filterMode = FilterMode.Bilinear;

            var srcOffset = new Vector2Int(innerRect.xMin - captureRect.xMin, innerRect.yMin - captureRect.yMin);
            int destX = innerRect.xMin - pixelRect.xMin;
            // Top-origin rects place the sub-rect from the top; bottom-origin rects from the bottom.
            int destY = topOriginRows
                ? pixelRect.yMax - innerRect.yMax
                : innerRect.yMin - pixelRect.yMin;
            BlitToTarget(filtered, outputTexture, new RectInt(destX, destY, innerRect.width, innerRect.height), srcOffset);

            textureRegistry.UpdateDynamic(owner.backdropFilterTextureId, outputTexture);
            owner.backdropFilterTemporaryTexture = outputTexture;

            // Now safe to release the previous frame's RT: the TextureId no longer references it.
            if (previousFrameRT != null)
                RenderTexture.ReleaseTemporary(previousFrameRT);

            ReleaseCaptures();
        }

        // Intersects captureRect with bounds (true rectangular intersection); returns false when the result is
        // empty. Note: RectInt.ClampToBounds is NOT equivalent -- it repositions the rect into bounds instead of
        // intersecting, so a fully-outside rect would yield a bogus in-bounds rect rather than an empty one.
        static bool ClampCapture(ref RectInt captureRect, RectInt bounds)
        {
            captureRect.xMin = Mathf.Max(captureRect.xMin, bounds.xMin);
            captureRect.yMin = Mathf.Max(captureRect.yMin, bounds.yMin);
            captureRect.xMax = Mathf.Min(captureRect.xMax, bounds.xMax);
            captureRect.yMax = Mathf.Min(captureRect.yMax, bounds.yMax);
            return captureRect.width > 0 && captureRect.height > 0;
        }

        // Margins are in CSS sides (points): CSS top is the yMax side of a bottom-origin rect;
        // top-origin rows swap the vertical mapping.
        static void InflateCapture(ref RectInt captureRect, in PostProcessingMargins margins, bool topOriginRows, float scaleX, float scaleY)
        {
            float yMinSide = topOriginRows ? margins.top : margins.bottom;
            float yMaxSide = topOriginRows ? margins.bottom : margins.top;
            captureRect.xMin -= Mathf.CeilToInt(margins.left * scaleX);
            captureRect.xMax += Mathf.CeilToInt(margins.right * scaleX);
            captureRect.yMin -= Mathf.CeilToInt(yMinSide * scaleY);
            captureRect.yMax += Mathf.CeilToInt(yMaxSide * scaleY);
        }

        static void BlitToTarget(RenderTexture source, RenderTexture target, RectInt destRect, Vector2Int srcOffset)
        {
            // The GetTemporary above can hand back a buffer holding a previous frame's backdrop, and the
            // CopyTexture below only overwrites destRect (a clipped capture leaves a margin), so wipe stale
            // pooled content first -- unless the copy fully covers the target, where the clear is redundant.
            bool fullyCovered = destRect.x == 0 && destRect.y == 0
                && destRect.width == target.width && destRect.height == target.height;
            if (!fullyCovered)
            {
                RenderTexture oldRT = RenderTexture.active;
                RenderTexture.active = target;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = oldRT;
            }

            // Verbatim copy, NOT an alpha-blend: `filtered` is already premultiplied, so alpha-blending it would
            // re-multiply by alpha (double premultiply -> darkened backdrop). CopyTexture overwrites exactly.
            // srcOffset selects the element sub-rect out of the margin-inflated capture.
            Graphics.CopyTexture(source, 0, 0, srcOffset.x, srcOffset.y, destRect.width, destRect.height,
                                 target, 0, 0, destRect.x, destRect.y);
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

        static RenderTexture ApplyBackdropFilters(RenderTexture source, VisualElement ve, RenderData owner, RenderTextureReadWrite colorSpace)
        {
            var backdropFilters = ve.computedStyle.backdropFilter;

            int passCount = FilterHelper.CountFilterChainPasses(backdropFilters);
            if (passCount == 0)
                return source;

            // Re-prime with defaults when the pass count changed after the update-phase walk (chain mutated
            // mid-frame): new blocks are empty and survivors hold stale uniforms, incl. a degenerate
            // unity_uie_UVRect. Only count changes are detected; user callbacks cannot run at render time.
            var renderTreeManager = owner.renderTree.renderTreeManager;
            var extraData = renderTreeManager.GetOrAddExtraData(owner);
            var blocks = extraData.backdropFilterCallbackPropertyBlocks ??= new List<MaterialPropertyBlock>(passCount);
            if (renderTreeManager.SizeFilterCallbackBlocks(blocks, passCount))
            {
                bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;
                PrimeDefaultBlocks(blocks, backdropFilters, readsGamma);
            }

            return FilterHelper.ApplyFilterChain(
                source,
                backdropFilters,
                colorSpace,
                blocks,
                usePixelMatrix: true);
        }

        // Default bindings + full UV rect for every rendered slot; user callbacks must not run at render time.
        static void PrimeDefaultBlocks(List<MaterialPropertyBlock> blocks, System.ReadOnlySpan<UnmanagedFilterFunction> filters, bool readsGamma)
        {
            int flatBlockIndex = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var filterDef = filterFunc.GetDefinition();
                if (filterDef == null || filterDef.passes == null)
                    continue;

                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    var pass = filterDef.passes[j];
                    if (pass.material != null)
                    {
                        var block = blocks[flatBlockIndex];
                        block.Clear();
                        FilterHelper.ApplyDefaultParameterBindings(block, pass, filterFunc, readsGamma);
                        block.SetVectorArray(FilterHelper.s_UVRectId, s_FullUVRectArray);
                    }
                    flatBlockIndex++;
                }
            }
        }
    }
}
