// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        internal RenderChain renderChain; // May be recreated any time.

        public UIRRepaintUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        private static readonly string s_Description = "Update Rendering";
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
            if (renderChain == null)
                return;

            bool transformChanged = (versionChangeType & VersionChangeType.Transform) != 0;
            bool sizeChanged = (versionChangeType & VersionChangeType.Size) != 0;
            bool overflowChanged = (versionChangeType & VersionChangeType.Overflow) != 0;
            bool borderRadiusChanged = (versionChangeType & VersionChangeType.BorderRadius) != 0;
            bool borderWidthChanged = (versionChangeType & VersionChangeType.BorderWidth) != 0;
            bool renderHintsChanged = (versionChangeType & VersionChangeType.RenderHints) != 0;
            bool disableRenderingChanged = (versionChangeType & VersionChangeType.DisableRendering) != 0;
            bool repaintChanged = (versionChangeType & VersionChangeType.Repaint) != 0;

            if (renderHintsChanged)
                renderChain.UIEOnRenderHintsChanged(ve);

            if (transformChanged || sizeChanged || borderWidthChanged)
                renderChain.UIEOnTransformOrSizeChanged(ve, transformChanged, sizeChanged || borderWidthChanged);

            if (overflowChanged || borderRadiusChanged)
                renderChain.UIEOnClippingChanged(ve, false);

            if ((versionChangeType & VersionChangeType.Opacity) != 0)
                renderChain.UIEOnOpacityChanged(ve);

            if ((versionChangeType & VersionChangeType.Color) != 0)
                renderChain.UIEOnColorChanged(ve);

            if (repaintChanged)
                renderChain.UIEOnVisualsChanged(ve, false);

            if (disableRenderingChanged && !repaintChanged) // The disable rendering will be taken care of by the repaint (it clear all commands)
                renderChain.UIEOnDisableRenderingChanged(ve);
        }

        public override void Update()
        {
            if (renderChain == null)
                InitRenderChain();

            if (renderChain == null || renderChain.device == null)
                return;

            renderChain.ProcessChanges();

            // Apply these debug values every frame because the render chain may have been recreated.
            renderChain.drawStats = drawStats;
            renderChain.device.breakBatches = breakBatches;
        }

        public void Render()
        {
            // Since the calls to Update and Render can be disjoint, this check seems reasonable.
            if (renderChain == null)
                return;

            Debug.Assert(!renderChain.drawInCameras);
            renderChain.Render();
        }

        // Overriden in tests
        protected virtual RenderChain CreateRenderChain() { return new RenderChain(panel); }

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

            renderChain = CreateRenderChain();
            renderChain.UIEOnChildAdded(attachedPanel.visualTree);
        }

        public void Reset() => DestroyRenderChain();

        void DestroyRenderChain()
        {
            if (renderChain == null)
                return;

            renderChain.Dispose();
            renderChain = null;
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

        void OnPanelHierarchyChanged(VisualElement ve, HierarchyChangeType changeType)
        {
            if (renderChain == null)
                return;

            switch (changeType)
            {
                case HierarchyChangeType.Add:
                    renderChain.UIEOnChildAdded(ve);
                    break;

                case HierarchyChangeType.Remove:
                    renderChain.UIEOnChildRemoving(ve);
                    break;

                case HierarchyChangeType.Move:
                    renderChain.UIEOnChildrenReordered(ve);
                    break;
            }
        }

        void ResetAllElementsDataRecursive(VisualElement ve)
        {
            ve.renderChainData = new RenderChainVEData(); // Fast reset, no need to go through device freeing

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
