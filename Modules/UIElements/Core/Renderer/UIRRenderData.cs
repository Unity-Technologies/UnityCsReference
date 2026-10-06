// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    [Flags]
    enum RenderDataDirtyTypes
    {
        None = 0,
        Transform = 1 << 0,
        ClipRectSize = 1 << 1,
        Clipping = 1 << 2,           // The clipping state of the VE needs to be reevaluated.
        ClippingHierarchy = 1 << 3,  // Same as above, but applies to all descendants too.
        Visuals = 1 << 4,            // The visuals of the VE need to be repainted.
        VisualsHierarchy = 1 << 5,   // Same as above, but applies to all descendants too.
        VisualsOpacityId = 1 << 6,   // The vertices only need their opacityId to be updated.
        Opacity = 1 << 7,            // The opacity of the VE needs to be updated.
        OpacityHierarchy = 1 << 8,   // Same as above, but applies to all descendants too.
        Color = 1 << 9,              // The background color of the VE needs to be updated.

        AllVisuals = Visuals | VisualsHierarchy | VisualsOpacityId
    }

    enum RenderDataDirtyTypeClasses
    {
        Clipping,
        Opacity,
        Color,
        TransformSize,
        Visuals,

        Count
    }

    [Flags]
    enum RenderDataFlags
    {
        IsGroupTransform = 1 << 0,
        IsIgnoringDynamicColorHint = 1 << 1,
        HasExtraData = 1 << 2,
        HasExtraMeshes = 1 << 3,
        IsSubTreeQuad = 1 << 4,
        IsNestedRenderTreeRoot = 1 << 5,
        IsClippingRectDirty = 1 << 6,
        IsStickyBone = 1 << 7,
        IsElementInfoDirty = 1 << 8,
        RegisteredForFilterCallbacks = 1 << 9,
        RegisteredForBackdropFilterCallbacks = 1 << 10,
        // Curved UI: the engine-owned curvature mesh modifier is registered on the owner.
        HasCurvatureModifier = 1 << 11,
        // Value of the owner's isWorldSpaceRootPanelComponent at insertion time, which decides both the
        // render-chain cut and whether z-indexed descendants may be promoted past this element.
        CutsRenderChain = 1 << 12,
        HasBackdropFilter = 1 << 13,

        // Set when a descendant is dirty for the matching class, so a dirty-class pass can descend
        // straight to the dirty elements instead of walking the whole tree. One bit per class rather
        // than one shared bit, because the five passes are separate: a shared bit would make each of
        // them descend the union of all the classes' closures.
        //
        // These must stay contiguous and in RenderDataDirtyTypeClasses order -- the bit for a class is
        // obtained by shifting SubtreeDirtyClipping left by the class index.
        SubtreeDirtyClipping = 1 << 14,
        SubtreeDirtyOpacity = 1 << 15,
        SubtreeDirtyColor = 1 << 16,
        SubtreeDirtyTransformSize = 1 << 17,
        SubtreeDirtyVisuals = 1 << 18,

        SubtreeDirtyAll = SubtreeDirtyClipping | SubtreeDirtyOpacity | SubtreeDirtyColor | SubtreeDirtyTransformSize | SubtreeDirtyVisuals,

        // Set on a parent that holds a ChildrenDirtyTracker, so the walk skips the ExtraRenderData lookup
        // for the parents that never needed one.
        HasChildrenTracker = 1 << 19,

        // Set while this element sits in its parent's ChildrenDirtyTracker.
        TrackedByParent = 1 << 20,

        // Set on a parent that has given up on tracking which of its children are dirty, because too many
        // of them are. Its walk goes back to the child list until the subtree comes clean. A new tracker
        // starts in this state, since the children dirtied before it are not tracked.
        ChildrenTrackerSaturated = 1 << 21,

        // Set on a parent whose children's sibling keys no longer order them: it just started tracking, or
        // an insertion found no gap. The keys are rebuilt the next time its tracker is iterated.
        ChildrenKeysStale = 1 << 22,
    }

    // This is intended for data that used infrequently, to such an extent, that it's not worth being directly in RenderChainVEData.
    // This data is accessed through a dictionary lookup, so it's not as fast as direct access.
    class ExtraRenderData : LinkedPoolItem<ExtraRenderData>
    {
        public BasicNode<MeshHandle> extraMesh;

        // Hash-deduped per-glyph TCS allocs written during PostProcessTextVertices.
        // Null until first SetTints; freed in FreeExtraData.
        public Dictionary<TextCoreSettings, BMPAlloc> textCoreSettingsAllocs;

        // One pooled block per pass in flat chain order; populated in the update phase so user
        // callbacks never run while a render target is bound. The lists persist (empty) across pooled reuse.
        public List<MaterialPropertyBlock> filterCallbackPropertyBlocks;
        public List<MaterialPropertyBlock> backdropFilterCallbackPropertyBlocks;

        // Backdrop-filter render state; live only while RenderData.hasBackdropFilterAllocated.
        // The TextureId is a persistent handle, the RT is created during render and released next frame.
        public TextureId backdropFilterTextureId;
        public RenderTexture backdropFilterTemporaryTexture;

        // Backdrop texture mapping, accounting for rotation.
        public Vector2 backdropFilterUVBottomLeft;
        public Vector2 backdropFilterUVTopLeft;
        public Vector2 backdropFilterUVTopRight;
        public Vector2 backdropFilterUVBottomRight;

        // The element's world rect clipped to its ancestors, fixed at mesh-record time. The UV corners above
        // are normalized within it, so the render phase sizes the texture from this and not from ve.worldBound.
        public Rect backdropFilterRecordedRect;

        // Null until this element first has ChildrenDirtyTracker.k_MinTrackedChildCount children.
        public ChildrenDirtyTracker childrenTracker;
    }

    struct GraphicEntry
    {
        public Texture source;
        public TextureId actual;
        public bool replaced;
        public VectorImage vectorImage;
    }

    // IMPORTANT: Initialize all fields in this struct in RenderTreeManager.InitRenderData()
    class RenderData
    {
        public VisualElement owner;
        public RenderTree renderTree;
        public RenderData parent, prevSibling, nextSibling;
        public RenderData firstChild, lastChild;
        public RenderData groupTransformAncestor, boneTransformAncestor;
        public RenderDataFlags flags;
        public int depthInRenderTree;
        public int childCount;
        public int siblingKey; // Gapped label ordering this element among its siblings; assigned by SpliceAfter.
        public RenderDataDirtyTypes dirtiedValues;
        public uint dirtyID;
        public RenderChainCommand firstHeadCommand, lastHeadCommand; // Sequential for the same owner
        public RenderChainCommand firstTailCommand, lastTailCommand; // Sequential for the same owner
        public bool localFlipsWinding;
        public bool worldFlipsWinding;

        public ClipMethod clipMethod; // Self
        public int childrenStencilRef;
        public int childrenMaskDepth;

        public MeshHandle headMesh, tailMesh;
        public ushort elementId; // 0 <=> none
        public BMPAlloc transformID, clipRectID, opacityID, textCoreSettingsID;
        public BMPAlloc colorID, backgroundColorID, borderLeftColorID, borderTopColorID, borderRightColorID, borderBottomColorID, tintColorID;
        public float compositeOpacity;
        public float backgroundAlpha;

        public BasicNode<GraphicEntry> graphicEntries;

        // True while this render data's ExtraRenderData holds live backdrop-filter state. A flag rather than a
        // derived check, so a pooled ExtraRenderData's stale fields can never read as allocated.
        public bool hasBackdropFilterAllocated => (flags & RenderDataFlags.HasBackdropFilter) != 0;

        // Curved UI: true while the engine-owned curvature mesh modifier is registered on the
        // owner. Synchronized against owner.hasCurvature by UIRCurvatureMeshModifier.SyncState. Packed
        // into flags (reset by Init), so pooled RenderData never carries stale state.
        public bool hasCurvatureModifier
        {
            get => (flags & RenderDataFlags.HasCurvatureModifier) != 0;
            set => flags = value ? (flags | RenderDataFlags.HasCurvatureModifier) : (flags & ~RenderDataFlags.HasCurvatureModifier);
        }

        public RenderChainCommand lastTailOrHeadCommand { get { return lastTailCommand ?? lastHeadCommand; } }
        public static bool AllocatesID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Owned) && alloc.IsValid(); }
        public static bool InheritsID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Inherited) && alloc.IsValid(); }

        // This is set whenever there is repaint requested when HierarchyDisplayed == false and is used to trigger the repaint when it finally get displayed
        public bool pendingRepaint;
        // This is set whenever a hierarchical repaint was needed when HierarchyDisplayed == false.
        public bool pendingHierarchicalRepaint;

        // Tracks the z-index value at the time of last insertion, used to detect int-to-int z-index changes.
        public int zIndex;

        public List<MeshModifierRegistration> m_EffectiveModifiers;

        public void Init()
        {
            // IMPORTANT NOTE: Is is important to initialize every RenderData field here
            // as they are reused from previously pooled elements.

            owner = null;
            renderTree = null;
            parent = null;
            nextSibling = null;
            prevSibling = null;
            firstChild = null;
            lastChild = null;
            groupTransformAncestor = null;
            boneTransformAncestor = null;
            flags = RenderDataFlags.IsClippingRectDirty;
            depthInRenderTree = 0;
            childCount = 0;
            siblingKey = 0;
            dirtiedValues = RenderDataDirtyTypes.None;
            dirtyID = 0;
            firstHeadCommand = null;
            lastHeadCommand = null;
            firstTailCommand = null;
            lastTailCommand = null;
            localFlipsWinding = false;
            worldFlipsWinding = false;
            clipMethod = ClipMethod.Undetermined;
            childrenStencilRef = 0;
            childrenMaskDepth = 0;
            headMesh = null;
            tailMesh = null;
            elementId = 0;
            transformID = ShaderInfoAllocator.identityTransform;
            clipRectID = ShaderInfoAllocator.infiniteClipRect;
            opacityID = ShaderInfoAllocator.fullOpacity;
            colorID = BMPAlloc.Invalid;
            backgroundColorID = BMPAlloc.Invalid;
            borderLeftColorID = BMPAlloc.Invalid;
            borderTopColorID = BMPAlloc.Invalid;
            borderRightColorID = BMPAlloc.Invalid;
            borderBottomColorID = BMPAlloc.Invalid;
            tintColorID = BMPAlloc.Invalid;
            textCoreSettingsID = ShaderInfoAllocator.defaultTextCoreSettings;
            compositeOpacity = float.MaxValue; // Any unreasonable value will do to trip the opacity composer to work
            backgroundAlpha = 0.0f;
            graphicEntries = null;
            pendingRepaint = false;
            pendingHierarchicalRepaint = false;
            m_EffectiveModifiers = null;
            zIndex = int.MinValue;
            clippingRect = Rect.zero;
            clippingRectMinusGroup = Rect.zero;
            clippingRectIsInfinite = false;
        }

        public void Reset()
        {
            owner = null;
            renderTree = null;
            parent = null;
            nextSibling = null;
            prevSibling = null;
            firstChild = null;
            lastChild = null;
            groupTransformAncestor = null;
            boneTransformAncestor = null;
            firstHeadCommand = null;
            lastHeadCommand = null;
            firstTailCommand = null;
            lastTailCommand = null;
            headMesh = null;
            tailMesh = null;
            graphicEntries = null;
            m_EffectiveModifiers = null;
        }

        public bool isGroupTransform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsGroupTransform) == RenderDataFlags.IsGroupTransform;
        }

        public bool cutsRenderChain
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.CutsRenderChain) == RenderDataFlags.CutsRenderChain;
        }

        public bool hasChildrenTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.HasChildrenTracker) == RenderDataFlags.HasChildrenTracker;
        }

        // Dirty itself, or on the path to a dirty descendant, for any dirty class.
        public bool isOnDirtyPath
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => dirtiedValues != RenderDataDirtyTypes.None || (flags & RenderDataFlags.SubtreeDirtyAll) != 0;
        }

        // Explicit z-index (auto is encoded as int.MinValue; 0 keeps document order, so both are excluded).
        public bool hasZIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HasZIndex(zIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasZIndex(int zIndex) => zIndex != int.MinValue && zIndex != 0;

        // Reparented in the render tree to the stacking context root, but inherited clip/opacity still come from the visual parent.
        public RenderData GetInheritanceParent(RenderData renderTreeParent)
        {
            if (hasZIndex && renderTreeParent != null)
            {
                var visualParent = owner.hierarchy.parent;
                var visualParentRD = visualParent?.nestedRenderData ?? visualParent?.renderData;
                if (visualParentRD != null && visualParentRD != renderTreeParent)
                    return visualParentRD;
            }
            return renderTreeParent;
        }

        public bool isIgnoringDynamicColorHint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsIgnoringDynamicColorHint) == RenderDataFlags.IsIgnoringDynamicColorHint;
        }

        public bool hasExtraData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.HasExtraData) == RenderDataFlags.HasExtraData;
        }

        public bool hasExtraMeshes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.HasExtraMeshes) == RenderDataFlags.HasExtraMeshes;
        }

        public bool isSubTreeQuad
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsSubTreeQuad) == RenderDataFlags.IsSubTreeQuad;
        }

        // This is only set on the root render data of a nested render tree.
        // Children of the root (in the same render tree) will not have this flag set.
        public bool isNestedRenderTreeRoot
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsNestedRenderTreeRoot) == RenderDataFlags.IsNestedRenderTreeRoot;
        }

        // Defines its own transform space (a bone, a group, or a nested render-tree root): bone inheritance and re-pointing stop here.
        public bool isBoneBarrier
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isGroupTransform || isNestedRenderTreeRoot || AllocatesID(transformID);
        }

        public bool isClippingRectDirty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsClippingRectDirty) == RenderDataFlags.IsClippingRectDirty;
        }

        public bool isStickyBone
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsStickyBone) == RenderDataFlags.IsStickyBone;
        }

        public bool isElementInfoDirty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsElementInfoDirty) == RenderDataFlags.IsElementInfoDirty;
        }

        // Tracked separately from chain emptiness so the sync paths register/unregister once per transition.
        public bool isRegisteredForFilterCallbacks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.RegisteredForFilterCallbacks) == RenderDataFlags.RegisteredForFilterCallbacks;
        }

        public bool isRegisteredForBackdropFilterCallbacks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.RegisteredForBackdropFilterCallbacks) == RenderDataFlags.RegisteredForBackdropFilterCallbacks;
        }

        private Rect m_ClippingRect;

        // The clipping rect coordinates are relative to the render tree, which corresponds to the
        // intersection of the clipping rect of the RenderData and its ancestors, up until the absolute root element.
        public Rect clippingRect
        {
            get
            {
                if (isClippingRectDirty)
                {
                    UpdateClippingRect();
                    flags &= ~RenderDataFlags.IsClippingRectDirty;
                }
                return m_ClippingRect;
            }
            set
            {
                m_ClippingRect = value;
            }
        }

        private Rect m_ClippingRectMinusGroup;

        // The clipping rect coordinates are relative to the group, and the rect is the
        // intersection result of our clipping rect with those of our ancestors, up to the nearest group.
        // The group itself is excluded from the intersection.
        public Rect clippingRectMinusGroup
        {
            get
            {
                if (isClippingRectDirty)
                {
                    UpdateClippingRect();
                    flags &= ~RenderDataFlags.IsClippingRectDirty;
                }
                return m_ClippingRectMinusGroup;
            }
            set
            {
                m_ClippingRectMinusGroup = value;
            }

        }

        private bool m_ClippingRectIsInfinite;

        internal bool clippingRectIsInfinite
        {
            get
            {
                if (isClippingRectDirty)
                {
                    UpdateClippingRect();
                    flags &= ~RenderDataFlags.IsClippingRectDirty;
                }
                return m_ClippingRectIsInfinite;
            }
            set
            {
                m_ClippingRectIsInfinite = value;
            }
        }

        internal void UpdateClippingRect()
        {
            // TODO: Optimize to avoid full matrix multiplications, instead apply scale+offset

            RenderData clipParent = GetInheritanceParent(parent);

            Rect inheritedClipping;
            Rect inheritedClippingMinusGroup;
            bool parentClipIsInfinite = (clipParent == null) || clipParent.clippingRectIsInfinite;

            if (clipParent != null)
            {
                inheritedClipping = clipParent.clippingRect;
                if (clipParent.isGroupTransform)
                {
                    inheritedClippingMinusGroup = DrawParams.k_UnlimitedRect;
                    parentClipIsInfinite = true;
                }
                else
                    inheritedClippingMinusGroup = clipParent.clippingRectMinusGroup;
            }
            else
            {
                var baseClippingRect = (owner?.panel != null) ? owner.panel.visualTree.rect : DrawParams.k_UnlimitedRect;
                if (renderTree.renderTreeManager.drawInCameras)
                    baseClippingRect = DrawParams.k_UnlimitedRect;
                inheritedClippingMinusGroup = baseClippingRect;
                inheritedClipping = baseClippingRect;
            }

            if (owner.ShouldClip())
            {
                GetLocalClippingRect(owner, out var clip);

                // Evaluate the clipping-rect-minus-group
                if (isGroupTransform)
                    // Not applicable to the group itself.
                    // By definition, the field must not include the group in the intersection.
                    // Reminder: groups clip their children with scissor rects or stencil mask.
                    m_ClippingRectMinusGroup = Rect.zero;
                else
                {
                    if (isNestedRenderTreeRoot)
                        m_ClippingRectMinusGroup = clip;
                    else
                    {
                        // Relative to the boundary directly: its inverse does not exist while it is collapsed.
                        Matrix4x4 toBoundary;
                        if (groupTransformAncestor != null)
                            UIRUtility.ComputeMatrixRelativeToAncestor(this, groupTransformAncestor, out toBoundary);
                        else
                            UIRUtility.ComputeMatrixRelativeToRenderTree(this, out toBoundary);

                        var clipMinusGroup = clip;
                        VisualElement.TransformAlignedRect(ref toBoundary, ref clipMinusGroup);

                        m_ClippingRectMinusGroup = parentClipIsInfinite ? clipMinusGroup : IntersectClipRects(clipMinusGroup, inheritedClippingMinusGroup);
                    }
                }

                // Bring clip in render-tree space
                UIRUtility.ComputeMatrixRelativeToRenderTree(this, out var toTree);
                VisualElement.TransformAlignedRect(ref toTree, ref clip);

                // Intersect with inherited clipping
                m_ClippingRect = IntersectClipRects(clip, inheritedClipping);
            }
            else
            {
                m_ClippingRect = inheritedClipping;
                m_ClippingRectMinusGroup = inheritedClippingMinusGroup;
                m_ClippingRectIsInfinite = parentClipIsInfinite;
            }
        }

        internal static Rect IntersectClipRects(Rect rect, Rect parentRect)
        {
            float x1 = Mathf.Max(rect.xMin, parentRect.xMin);
            float x2 = Mathf.Min(rect.xMax, parentRect.xMax);
            float y1 = Mathf.Max(rect.yMin, parentRect.yMin);
            float y2 = Mathf.Min(rect.yMax, parentRect.yMax);
            float width = Mathf.Max(x2 - x1, 0);
            float height = Mathf.Max(y2 - y1, 0);
            return new Rect(x1, y1, width, height);
        }

        private static void GetLocalClippingRect(VisualElement owner, out Rect localRect)
        {
            var resolvedStyle = owner.resolvedStyle;

            localRect = owner.rect;
            localRect.x += resolvedStyle.borderLeftWidth;
            localRect.y += resolvedStyle.borderTopWidth;
            localRect.width -= (resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth);
            localRect.height -= (resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth);

            if (owner.computedStyle.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                localRect.x += resolvedStyle.paddingLeft;
                localRect.y += resolvedStyle.paddingTop;
                localRect.width -= (resolvedStyle.paddingLeft + resolvedStyle.paddingRight);
                localRect.height -= (resolvedStyle.paddingTop + resolvedStyle.paddingBottom);
            }
        }

    }
}
