// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
    }

    // This is intended for data that used infrequently, to such an extent, that it's not worth being directly in RenderChainVEData.
    // This data is accessed through a dictionary lookup, so it's not as fast as direct access.
    class ExtraRenderData : LinkedPoolItem<ExtraRenderData>
    {
        public BasicNode<MeshHandle> extraMesh;
    }

    struct TextureEntry
    {
        public Texture source;
        public TextureId actual;
        public bool replaced;
    }

    // IMPORTANT: Initialize all fields in this struct in RenderTreeManager.InitRenderData()
    class RenderData
    {
        public VisualElement owner;
        public RenderTree renderTree;
        public RenderData parent, prevSibling, nextSibling;
        public RenderData firstChild, lastChild;
        public RenderData groupTransformAncestor, boneTransformAncestor;
        public RenderData prevDirty, nextDirty; // Embedded doubly-linked list for dirty updates
        public RenderDataFlags flags;
        public int depthInRenderTree;
        public RenderDataDirtyTypes dirtiedValues;
        public uint dirtyID;
        public RenderChainCommand firstHeadCommand, lastHeadCommand; // Sequential for the same owner
        public RenderChainCommand firstTailCommand, lastTailCommand; // Sequential for the same owner
        public bool localFlipsWinding;
        public bool worldFlipsWinding;
        public bool worldTransformScaleZero;

        public ClipMethod clipMethod; // Self
        public int childrenStencilRef;
        public int childrenMaskDepth;

        public MeshHandle headMesh, tailMesh;
        public Matrix4x4 verticesSpace; // Transform describing the space which the vertices in 'data' are relative to
        public BMPAlloc transformID, clipRectID, opacityID, textCoreSettingsID;
        public BMPAlloc colorID, backgroundColorID, borderLeftColorID, borderTopColorID, borderRightColorID, borderBottomColorID, tintColorID;
        public float compositeOpacity;
        public float backgroundAlpha;

        public BasicNode<TextureEntry> textures;

        public RenderChainCommand lastTailOrHeadCommand { get { return lastTailCommand ?? lastHeadCommand; } }
        public static bool AllocatesID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Owned) && alloc.IsValid(); }
        public static bool InheritsID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Inherited) && alloc.IsValid(); }

        // This is set whenever there is repaint requested when HierarchyDisplayed == false and is used to trigger the repaint when it finally get displayed
        public bool pendingRepaint;
        // This is set whenever a hierarchical repaint was needed when HierarchyDisplayed == false.
        public bool pendingHierarchicalRepaint;

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
            prevDirty = null;
            nextDirty = null;
            flags = RenderDataFlags.IsClippingRectDirty;
            depthInRenderTree = 0;
            dirtiedValues = RenderDataDirtyTypes.None;
            dirtyID = 0;
            firstHeadCommand = null;
            lastHeadCommand = null;
            firstTailCommand = null;
            lastTailCommand = null;
            localFlipsWinding = false;
            worldFlipsWinding = false;
            worldTransformScaleZero = false;
            clipMethod = ClipMethod.Undetermined;
            childrenStencilRef = 0;
            childrenMaskDepth = 0;
            headMesh = null;
            tailMesh = null;
            verticesSpace = Matrix4x4.identity;
            transformID = UIRVEShaderInfoAllocator.identityTransform;
            clipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            opacityID = UIRVEShaderInfoAllocator.fullOpacity;
            colorID = BMPAlloc.Invalid;
            backgroundColorID = BMPAlloc.Invalid;
            borderLeftColorID = BMPAlloc.Invalid;
            borderTopColorID = BMPAlloc.Invalid;
            borderRightColorID = BMPAlloc.Invalid;
            borderBottomColorID = BMPAlloc.Invalid;
            tintColorID = BMPAlloc.Invalid;
            textCoreSettingsID = UIRVEShaderInfoAllocator.defaultTextCoreSettings;
            compositeOpacity = float.MaxValue; // Any unreasonable value will do to trip the opacity composer to work
            backgroundAlpha = 0.0f;
            textures = null;
            pendingRepaint = false;
            pendingHierarchicalRepaint = false;
            clippingRect = Rect.zero;
            clippingRectMinusGroup = Rect.zero;
            clippingRectIsInfinite = false;
        }

        public bool isGroupTransform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsGroupTransform) == RenderDataFlags.IsGroupTransform;
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

        public bool isClippingRectDirty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsClippingRectDirty) == RenderDataFlags.IsClippingRectDirty;
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

            Rect inheritedClipping;
            Rect inheritedClippingMinusGroup;
            bool parentClipIsInfinite = (parent == null) || parent.clippingRectIsInfinite;

            if (parent != null)
            {
                inheritedClipping = parent.clippingRect;
                if (parent.isGroupTransform)
                {
                    inheritedClippingMinusGroup = DrawParams.k_UnlimitedRect;
                    parentClipIsInfinite = true;
                }
                else
                    inheritedClippingMinusGroup = parent.clippingRectMinusGroup;
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
                        var clipMinusGroup = clip;

                        // TODO: Skip for identity
                        VisualElement.TransformAlignedRect(ref owner.worldTransformRef, ref clipMinusGroup);

                        if (groupTransformAncestor != null)
                            VisualElement.TransformAlignedRect(ref groupTransformAncestor.owner.worldTransformInverse, ref clipMinusGroup);
                        else
                            VisualElement.TransformAlignedRect(ref renderTree.rootRenderData.owner.worldTransformInverse, ref clipMinusGroup);

                        m_ClippingRectMinusGroup = parentClipIsInfinite ? clipMinusGroup : IntersectClipRects(clipMinusGroup, inheritedClippingMinusGroup);
                    }
                }

                // Bring clip in render-tree space
                VisualElement.TransformAlignedRect(ref owner.worldTransformRef, ref clip);

                var tree = renderTree;
                var treeRoot = tree.rootRenderData;
                if (!tree.isRootRenderTree)
                    VisualElement.TransformAlignedRect(ref treeRoot.owner.worldTransformInverse, ref clip);

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

        private static Rect IntersectClipRects(Rect rect, Rect parentRect)
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
