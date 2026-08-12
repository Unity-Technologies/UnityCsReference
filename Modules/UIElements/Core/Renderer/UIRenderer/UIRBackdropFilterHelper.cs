// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    static class BackdropFilterHelper
    {
        // Not thread-safe; UIR rendering is sequential. Revisit if panel processing becomes parallel.
        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        // The editor window's sampleable back buffer (the GUIView aux RT) and its content row order
        // (isTopOrigin: texel row 0 is the top of the window; platform windowing convention).
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

            // Backdrop-filter preserves the color space across the chain, so every pass writes what it reads.
            FilterHelper.InvokeFilterCallbacks(
                backdropFilters,
                blocks,
                readsGamma: readsGamma,
                writesGamma: readsGamma,
                lastPassWritesGamma: readsGamma,
                ve.scaledPixelsPerPoint);

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
        public static void GenerateBackdropFilterTexture(DrawParams drawParams, VisualElement ve, float pixelsPerPoint, RenderData owner)
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

            RectInt pixelRect;
            RectInt captureRect;
            if (!isNestedRT)
            {
                pixelRect = RenderChainCommand.RectPointsToPixelsAndFlipYAxis(worldBound, drawParams.boundsMin, pixelsPerPoint);
                if (pixelRect.width <= 0 || pixelRect.height <= 0)
                    return;

                // Clamp to the ancestor clip in the capture's own space: clippingRect (panel space, covers overflow:hidden)
                // for a normal element, but the scissor for a filtered element captured in a nested tree (UI-5094).
                Rect clipRect = object.ReferenceEquals(owner, ve.renderData) ? owner.clippingRect : drawParams.scissor.Peek();
                RectInt clipRectInt = RenderChainCommand.RectPointsToPixelsAndFlipYAxis(clipRect, drawParams.boundsMin, pixelsPerPoint);

                // A top-origin aux RT needs the bottom-origin rects reflected about the full source
                // height (not the viewport) into raw-row space; bottom-origin rows already match.
                if (sourceIsAuxBackBuffer && auxIsTopOrigin)
                {
                    pixelRect.y = source.height - pixelRect.y - pixelRect.height;
                    clipRectInt.y = source.height - clipRectInt.y - clipRectInt.height;
                }

                captureRect = pixelRect;

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

                Rect drawBounds = drawParams.drawBounds;
                RectInt activeViewport = Utility.GetActiveViewport();

                // Mid-render of the nested tree: draw bounds and viewport must be valid here, so a bad value is a bug.
                bool validRenderState = drawBounds.width > UIRUtility.k_Epsilon && drawBounds.height > UIRUtility.k_Epsilon
                    && activeViewport.width > 0 && activeViewport.height > 0;
                Debug.Assert(validRenderState, "Backdrop-filter nested-RT remap reached with invalid draw bounds or viewport.");
                if (!validRenderState)
                    return;

                // The nested tree's projection scale differs from the panel's pixelsPerPoint (e.g. UIBuilder canvas
                // zoom), so derive it from the active viewport / draw bounds; the rect mapping itself is shared.
                float scaleX = activeViewport.width / drawBounds.width;
                float scaleY = activeViewport.height / drawBounds.height;
                pixelRect = RenderChainCommand.RectPointsToPixels(treeBound, drawBounds.min, scaleX, scaleY, activeViewport);
                if (pixelRect.width <= 0 || pixelRect.height <= 0)
                    return;

                captureRect = pixelRect;

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

            // Filtered alpha = captured coverage scaled by the filter chain (tint/opacity alpha<1 -> translucent,
            // empty capture -> transparent), so compositing premultiplied-over matches the runtime's backdrop opacity.
            RenderTexture filtered = ApplyBackdropFilters(backdrop, ve, owner, colorSpace);

            // Output is the element's full pixel rect; the capture is blitted into its matching sub-rect.
            RenderTexture outputTexture = RenderTexture.GetTemporary(
                pixelRect.width,
                pixelRect.height,
                0,
                filtered.format, // Match filtered so the CopyTexture in BlitToTarget is format-compatible.
                colorSpace
            );
            outputTexture.filterMode = FilterMode.Bilinear;

            int destX = captureRect.xMin - pixelRect.xMin;
            // Top-origin aux rects place the sub-rect from the top; bottom-origin rects from the bottom.
            int destY = sourceIsAuxBackBuffer && auxIsTopOrigin
                ? pixelRect.yMax - captureRect.yMax
                : captureRect.yMin - pixelRect.yMin;
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

        static void BlitToTarget(RenderTexture source, RenderTexture target, RectInt destRect)
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
            // re-multiply by alpha (double premultiply -> darkened backdrop). CopyTexture overwrites exactly (UI-5094).
            Graphics.CopyTexture(source, 0, 0, 0, 0, destRect.width, destRect.height,
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
