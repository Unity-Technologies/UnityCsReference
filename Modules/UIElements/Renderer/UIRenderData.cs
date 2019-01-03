// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This class contains data related to the rendering of a single VisualElement.
    /// </summary>
    internal class UIRenderData
    {
        private VisualElement m_VisualElement;
        private uint m_Version = 0;
        private uint m_LastRepaintVersion = uint.MaxValue;

        // Connections.
        public UIRenderData previousData { get; private set; }
        public UIRenderData nextData { get; private set; }
        public UIRenderData nextNestedData { get; private set; }

        // Renderers.
        public RendererBase innerBegin { get; private set; }
        public RendererBase innerEnd { get; private set; }
        public RendererBase innerNestedEnd { get; private set; }
        public RendererBase innerLastQueued { get; private set; }

        // Cached renderers that can be found within the draw chain of this VE.
        public ZoomPanRenderer cachedViewRenderer { get; private set; }

        public UIRenderData inheritedMaskRendererData { get; private set; }
        public MaskRenderer cachedMaskRenderer { get; private set; }
        public ImmediateRenderer cachedImmediateRenderer { get; private set; }

        // View data.
        public UIRenderData inheritedViewTransformData { get; private set; }
        public bool overridesViewTransform;

        // Skinning data.
        public UIRenderData inheritedSkinningTransformData { get; private set; }
        public bool overridesSkinningTransform;
        public Alloc skinningAlloc;

        // Clipping Rect data.
        public UIRenderData inheritedClippingRectData { get; private set; }
        public Rect worldClippingRect = UIRUtility.s_InfiniteRect;
        public Rect viewClippingRect = UIRUtility.s_InfiniteRect;
        public Rect skinningClippingRect = UIRUtility.s_InfiniteRect;
        public bool overridesClippingRect;
        public Alloc clippingRectAlloc;

        // Text data.
        public MeshHandle textMeshHandle;
        public TextStylePainterParameters textParams;
        public RendererBase cachedTextRenderer;

        // Misc data.
        public bool usesTextures;
        public readonly EmptyRenderer emptyRenderer = new EmptyRenderer();
        public RenderHint effectiveRenderHint;

        public void ResetInnerChain(IUIRenderDevice renderDevice)
        {
            if (innerBegin != null)
            {
                if (innerBegin != emptyRenderer)
                    // We MUST be disconnected before this method is called.
                    UIRUtility.FreeDrawChain(renderDevice, innerBegin);
                innerBegin = null;
                innerEnd = null;
                innerNestedEnd = null;
                innerLastQueued = null;
                cachedViewRenderer = null;
                cachedMaskRenderer = null;
                cachedImmediateRenderer = null;
                overridesViewTransform = false;
                textMeshHandle = null;
                cachedTextRenderer = null;
            }
        }

        // Does NOT reconnect the children or the nextData or the nextNestedData with the previous!
        public void Disconnect()
        {
            // There is some redundancy which could be avoided.
            Disconnect(previousData, this);
            Disconnect(this, nextData);
            Disconnect(this, nextNestedData);
        }

        static void Disconnect(UIRenderData previousData, UIRenderData nextData)
        {
            if (previousData == null || nextData == null)
                return;

            // Disconnect the renderers.
            var previousRenderer = previousData.innerEnd;
            if (previousRenderer != null)
            {
                if (previousRenderer.next == nextData.innerBegin)
                    previousRenderer.next = null;
            }

            var previousNestedRenderer = previousData.innerNestedEnd;
            if (previousNestedRenderer != null)
            {
                if (previousNestedRenderer.next == nextData.innerBegin)
                    previousNestedRenderer.next = null;

                if (previousNestedRenderer.contents == nextData.innerBegin)
                    previousNestedRenderer.contents = null;
            }

            // Disconnect the data.
            if (previousData.nextData == nextData)
                previousData.nextData = null;

            if (previousData.nextNestedData == nextData)
                previousData.nextNestedData = null;

            if (nextData.previousData == previousData)
                nextData.previousData = null;
        }

        public void SetNextData(UIRenderData newNext)
        {
            Debug.Assert(nextData == null);
            Debug.Assert(newNext.previousData == null);

            // Connect Data.
            nextData = newNext;
            newNext.previousData = this;

            // Connect Renderers.
            innerEnd.next = newNext.innerBegin;
        }

        // Sets as the next nested only if we have a nested renderer...
        public void SetNextNestedData(UIRenderData newNext)
        {
            Debug.Assert(nextNestedData == null);
            Debug.Assert(newNext.previousData == null);

            // Connect Data.
            nextNestedData = newNext;
            newNext.previousData = this;

            // Connect Renderers.
            if (innerNestedEnd != null)
            {
                if ((innerNestedEnd.type & RendererTypes.ContentRenderer) != 0)
                    innerNestedEnd.contents = newNext.innerBegin;
                else
                    innerNestedEnd.next = newNext.innerBegin;
            }
            else
                innerEnd.next = newNext.innerBegin;
        }

        public void SetParent(UIRenderData parentData)
        {
            if (parentData == null)
                return;

            bool changed = false;

            if (inheritedViewTransformData != parentData.effectiveViewTransformData)
            {
                inheritedViewTransformData = parentData.effectiveViewTransformData;
                changed = true;
            }

            if (inheritedSkinningTransformData != parentData.effectiveSkinningTransformData)
            {
                inheritedSkinningTransformData = parentData.effectiveSkinningTransformData;
                changed = true;
            }

            if (inheritedClippingRectData != parentData.effectiveClippingRectData)
            {
                inheritedClippingRectData = parentData.effectiveClippingRectData;
                changed = true;
            }

            if (inheritedMaskRendererData != parentData.effectiveMaskRendererData)
            {
                inheritedMaskRendererData = parentData.effectiveMaskRendererData;
                changed = true;
            }

            if (changed)
                visualElement.IncrementVersion(VersionChangeType.Repaint);
        }

        public UIRenderData effectiveViewTransformData
        {
            get
            {
                if (overridesViewTransform)
                    return this;
                return inheritedViewTransformData;
            }
        }

        public UIRenderData effectiveSkinningTransformData
        {
            get
            {
                if (overridesSkinningTransform)
                    return this;
                return inheritedSkinningTransformData;
            }
        }

        public uint effectiveSkinningTransformId
        {
            get
            {
                UIRenderData skinningTransformData = effectiveSkinningTransformData;
                if (skinningTransformData != null)
                    return skinningTransformData.skinningAlloc.start;
                return 0;
            }
        }

        public UIRenderData effectiveClippingRectData
        {
            get
            {
                if (overridesClippingRect)
                    return this;
                return inheritedClippingRectData;
            }
        }

        public UIRenderData effectiveMaskRendererData
        {
            get
            {
                if (cachedMaskRenderer != null)
                    return this;
                return inheritedMaskRendererData;
            }
        }

        public uint effectiveClippingRectId
        {
            get
            {
                UIRenderData clippingRectData = effectiveClippingRectData;
                if (clippingRectData != null)
                    return clippingRectData.clippingRectAlloc.start;
                return 0;
            }
        }

        public void QueueRenderer(RendererBase renderer)
        {
            // For now, we only support modifying the chain when the data is disconnected.
            Debug.Assert(previousData == null && nextData == null && nextNestedData == null);
            Debug.Assert(innerBegin == null || !(innerBegin is EmptyRenderer)); // We must free the chain before queuing.
            Debug.Assert(renderer.next == null && renderer.contents == null, "Renderers being queued must be disconnected.");

            switch (renderer.type)
            {
                case RendererTypes.ZoomPanRenderer:
                    Debug.Assert(cachedViewRenderer == null, "A view renderer has already been added.");
                    cachedViewRenderer = (ZoomPanRenderer)renderer;
                    overridesViewTransform = true;
                    break;
                case RendererTypes.MaskRenderer:
                    Debug.Assert(cachedMaskRenderer == null, "A mask renderer has already been added.");
                    cachedMaskRenderer = (MaskRenderer)renderer;
                    break;
                case RendererTypes.ImmediateRenderer:
                    Debug.Assert(cachedImmediateRenderer == null, "An immediate renderer has already been added.");
                    cachedImmediateRenderer = (ImmediateRenderer)renderer;
                    break;
            }

            bool isContentRenderer = (renderer.type & RendererTypes.ContentRenderer) != 0;

            if (innerBegin == null)
            {
                // First renderer being queued.
                innerBegin = renderer;
                innerEnd = renderer;
                if (isContentRenderer)
                    innerNestedEnd = renderer;
            }
            else
            {
                // At least one renderer has been queued so far.
                if (innerNestedEnd == null)
                {
                    // No queued renderer is a nested renderer.
                    innerEnd.next = renderer;
                    innerEnd = renderer;
                    if (isContentRenderer)
                        innerNestedEnd = renderer;
                }
                else
                {
                    // At least one queued renderer is a nested renderer.
                    if ((innerNestedEnd.type & RendererTypes.ContentRenderer) != 0)
                        innerNestedEnd.contents = renderer;
                    else
                        innerNestedEnd.next = renderer;
                    innerNestedEnd = renderer;
                }
            }

            innerLastQueued = renderer;
        }

        public VisualElement visualElement
        {
            get { return m_VisualElement; }
            set
            {
                m_VisualElement = value;
                m_Version = 0;
                m_LastRepaintVersion = uint.MaxValue;
            }
        }

        public bool isDirty { get { return m_Version != m_LastRepaintVersion; } }

        public float scale { get; set; } = 1.0f;

        public void IncrementVersion()
        {
            ++m_Version;
        }

        public void OnRepaint()
        {
            m_LastRepaintVersion = m_Version;
        }
    }
}
