// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRRepaintUpdater : BaseVisualTreeUpdater
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

            PanelClearSettings clearSettings = panel.clearSettings;
            if (clearSettings.clearColor || clearSettings.clearDepthStencil)
            {
                // Case 1277149: Clear color must be pre-multiplied like when we render.
                Color clearColor = clearSettings.color;
                clearColor = clearColor.RGBMultiplied(clearColor.a);

                GL.Clear(clearSettings.clearDepthStencil, // Clearing may impact MVP
                    clearSettings.clearColor, clearColor, UIRUtility.k_ClearZ);
            }

            // Apply these debug values every frame because the render chain may have been recreated.
            renderChain.drawStats = drawStats;
            renderChain.device.breakBatches = breakBatches;

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
                    (it.Current.Value.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater)?.DestroyRenderChain();

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
            attachedPanel.standardShaderChanged += OnPanelStandardShaderChanged;
            attachedPanel.standardWorldSpaceShaderChanged += OnPanelStandardWorldSpaceShaderChanged;
            attachedPanel.hierarchyChanged += OnPanelHierarchyChanged;

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
            attachedPanel.standardShaderChanged -= OnPanelStandardShaderChanged;
            attachedPanel.standardWorldSpaceShaderChanged -= OnPanelStandardWorldSpaceShaderChanged;
            attachedPanel.hierarchyChanged -= OnPanelHierarchyChanged;
            attachedPanel = null;
        }

        void InitRenderChain()
        {
            Debug.Assert(attachedPanel != null);

            renderChain = CreateRenderChain();
            renderChain.UIEOnChildAdded(attachedPanel.visualTree);

            OnPanelStandardShaderChanged();
            if (panel.contextType == ContextType.Player)
                OnPanelStandardWorldSpaceShaderChanged();
        }

        internal void DestroyRenderChain()
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

        void OnPanelStandardShaderChanged()
        {
            if (renderChain == null)
                return;

            Shader shader = panel.standardShader;
            if (shader == null)
            {
                shader = Shader.Find(UIRUtility.k_DefaultShaderName);
                Debug.Assert(shader != null, "Failed to load UIElements default shader");
                if (shader != null)
                    shader.hideFlags |= HideFlags.DontSaveInEditor;
            }
            renderChain.defaultShader = shader;
        }

        void OnPanelStandardWorldSpaceShaderChanged()
        {
            if (renderChain == null)
                return;

            Shader shader = panel.standardWorldSpaceShader;
            if (shader == null)
            {
                shader = Shader.Find(UIRUtility.k_DefaultWorldSpaceShaderName);
                Debug.Assert(shader != null, "Failed to load UIElements default world-space shader");
                if (shader != null)
                    shader.hideFlags |= HideFlags.DontSaveInEditor;
            }
            renderChain.defaultWorldSpaceShader = shader;
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
