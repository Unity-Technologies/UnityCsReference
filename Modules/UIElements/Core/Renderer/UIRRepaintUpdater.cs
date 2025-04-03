// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    interface IPanelRenderer
    {
        /// <summary>
        /// Requests the fragment shader to output sRGB-encoded values. The project must be in linear color space and
        /// drawInCameras must be false for this flag to have an effect. Otherwise, it is ignored.
        /// </summary>
        bool forceGammaRendering { get; set; }
        uint vertexBudget { get; set; }
        void Reset();
        void Render();
    }

    class UIRRepaintUpdater : BaseVisualTreeUpdater, IPanelRenderer
    {
        BaseVisualElementPanel attachedPanel;
        internal RenderTreeManager renderTreeManager; // May be recreated any time.

        public UIRRepaintUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        private static readonly string s_Description = "UIElements.UpdateRenderData";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        bool m_ForceGammaRendering;

        public bool forceGammaRendering
        {
            get => m_ForceGammaRendering;
            set
            {
                if (m_ForceGammaRendering == value)
                    return;

                m_ForceGammaRendering = value;
                DestroyRenderChain();
            }
        }

        uint m_VertexBudget;

        public uint vertexBudget
        {
            get => m_VertexBudget;
            set
            {
                if (m_VertexBudget == value)
                    return;

                m_VertexBudget = value;
                DestroyRenderChain();
            }
        }

        public bool drawStats { get; set; }
        public bool breakBatches { get; set; }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (renderTreeManager == null)
                return;

            bool transformChanged = (versionChangeType & VersionChangeType.Transform) != 0;
            bool sizeChanged = (versionChangeType & VersionChangeType.Size) != 0;
            bool overflowChanged = (versionChangeType & VersionChangeType.Overflow) != 0;
            bool borderRadiusChanged = (versionChangeType & VersionChangeType.BorderRadius) != 0;
            bool borderWidthChanged = (versionChangeType & VersionChangeType.BorderWidth) != 0;
            bool renderHintsChanged = (versionChangeType & VersionChangeType.RenderHints) != 0;
            bool disableRenderingChanged = (versionChangeType & VersionChangeType.DisableRendering) != 0;
            bool repaintChanged = (versionChangeType & VersionChangeType.Repaint) != 0;

            // Check if we now need to render to a texture or not. If this change, this is equivalent to
            // changing the DynamicPostprocessing render hint.
            bool stackingContextChanged = false;
            if (ve.renderData != null)
            {
                stackingContextChanged =
                    (ve.useRenderTexture && ((ve.renderData.flags & RenderDataFlags.IsSubTreeQuad) == 0)) ||
                    (!ve.useRenderTexture && (ve.renderData.flags & RenderDataFlags.IsSubTreeQuad) != 0);
            }

            if (renderHintsChanged || stackingContextChanged)
                renderTreeManager.UIEOnRenderHintsChanged(ve);

            if (transformChanged || sizeChanged || borderWidthChanged)
                renderTreeManager.UIEOnTransformOrSizeChanged(ve, transformChanged, sizeChanged || borderWidthChanged);

            if (overflowChanged || borderRadiusChanged)
                renderTreeManager.UIEOnClippingChanged(ve, false);

            if ((versionChangeType & VersionChangeType.Opacity) != 0)
                renderTreeManager.UIEOnOpacityChanged(ve);

            if ((versionChangeType & VersionChangeType.Color) != 0)
                renderTreeManager.UIEOnColorChanged(ve);

            if (repaintChanged)
                renderTreeManager.UIEOnVisualsChanged(ve, false);

            if (disableRenderingChanged && !repaintChanged) // The disable rendering will be taken care of by the repaint (it clear all commands)
                renderTreeManager.UIEOnDisableRenderingChanged(ve);
        }

        public override void Update()
        {
            if (renderTreeManager == null)
                InitRenderChain();

            if (renderTreeManager == null || renderTreeManager.device == null)
                return;

            renderTreeManager.ProcessChanges();

            // Apply these debug values every frame because the render chain may have been recreated.
            renderTreeManager.drawStats = drawStats;
            renderTreeManager.device.breakBatches = breakBatches;
        }

        public void Render()
        {
            // Since the calls to Update and Render can be disjoint, this check seems reasonable.
            if (renderTreeManager == null)
                return;

            Debug.Assert(!renderTreeManager.drawInCameras);
            renderTreeManager.RenderRootTree();
        }

        // Overriden in tests
        protected virtual RenderTreeManager CreateRenderChain() { return new RenderTreeManager(panel); }

        static UIRRepaintUpdater()
        {
            UIR.Utility.GraphicsResourcesRecreate += OnGraphicsResourcesRecreate;
        }

        static void OnGraphicsResourcesRecreate(bool recreate)
        {
            if (!recreate)
                UIRenderDevice.PrepareForGfxDeviceRecreate();

            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
                if (recreate)
                    it.Current.Value.atlas?.Reset();
                else
                    it.Current.Value.panelRenderer.Reset();

            if (!recreate)
                UIRenderDevice.FlushAllPendingDeviceDisposes();
            else UIRenderDevice.WrapUpGfxDeviceRecreate();
        }

        void OnPanelChanged(BaseVisualElementPanel obj)
        {
            DetachFromPanel();
            AttachToPanel();
        }

        void AttachToPanel()
        {
            Debug.Assert(attachedPanel == null);

            if (panel == null)
                return;

            attachedPanel = panel;
            attachedPanel.isFlatChanged += OnPanelIsFlatChanged;
            attachedPanel.atlasChanged += OnPanelAtlasChanged;
            attachedPanel.hierarchyChanged += OnPanelHierarchyChanged;
            Debug.Assert(attachedPanel.panelRenderer == null);
            attachedPanel.panelRenderer = this;

            if (panel is BaseRuntimePanel runtimePanel)
                runtimePanel.drawsInCamerasChanged += OnPanelDrawsInCamerasChanged;
        }

        void DetachFromPanel()
        {
            if (attachedPanel == null)
                return;

            DestroyRenderChain();

            if (panel is BaseRuntimePanel runtimePanel)
                runtimePanel.drawsInCamerasChanged -= OnPanelDrawsInCamerasChanged;

            attachedPanel.isFlatChanged -= OnPanelIsFlatChanged;
            attachedPanel.atlasChanged -= OnPanelAtlasChanged;
            attachedPanel.hierarchyChanged -= OnPanelHierarchyChanged;
            Debug.Assert(attachedPanel.panelRenderer == this);
            attachedPanel.panelRenderer = null;
            attachedPanel = null;
        }

        void InitRenderChain()
        {
            Debug.Assert(attachedPanel != null);

            renderTreeManager = CreateRenderChain();
            renderTreeManager.UIEOnChildAdded(attachedPanel.visualTree);
        }

        public void Reset() => DestroyRenderChain();

        void DestroyRenderChain()
        {
            if (renderTreeManager == null)
                return;

            renderTreeManager.Dispose();
            renderTreeManager = null;

            ResetAllElementsDataRecursive(attachedPanel.visualTree);
        }

        void OnPanelIsFlatChanged()
        {
            DestroyRenderChain();
        }

        void OnPanelAtlasChanged()
        {
            DestroyRenderChain();
        }

        void OnPanelDrawsInCamerasChanged()
        {
            DestroyRenderChain();
        }

        void OnPanelHierarchyChanged(VisualElement ve, HierarchyChangeType changeType, IReadOnlyList<VisualElement> additionalContext = null)
        {
            if (renderTreeManager == null)
                return;

            switch (changeType)
            {
                case HierarchyChangeType.AddedToParent:
                    renderTreeManager.UIEOnChildAdded(ve);
                    break;

                case HierarchyChangeType.RemovedFromParent:
                    renderTreeManager.UIEOnChildRemoving(ve);
                    break;

                case HierarchyChangeType.ChildrenReordered:
                    renderTreeManager.UIEOnChildrenReordered(ve);
                    break;
            }
        }

        void ResetAllElementsDataRecursive(VisualElement ve)
        {
            // NOTE: There is no need to return the renderChainData to the pool,
            // this is called while destroying the renderChain, which will dispose of the pool anyway.
            ve.renderData = null;
            ve.nestedRenderData = null;

            // Recurse on children
            int childrenCount = ve.hierarchy.childCount - 1;
            while (childrenCount >= 0)
                ResetAllElementsDataRecursive(ve.hierarchy[childrenCount--]);
        }

        #region Dispose Pattern
        protected bool disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                DetachFromPanel();
            else UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
