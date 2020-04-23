using System;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRRepaintUpdater : BaseVisualTreeUpdater
    {
        internal RenderChain renderChain;

        static ProfilerMarker s_MarkerDrawChain = new ProfilerMarker("DrawChain");

        public UIRRepaintUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        private static readonly string s_Description = "UIRepaint";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public event Action<RenderChain> BeforeDrawChain
        {
            add { if (renderChain != null) renderChain.BeforeDrawChain += value; }
            remove { if (renderChain != null) renderChain.BeforeDrawChain -= value; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (renderChain == null)
                return;

            bool transformChanged = (versionChangeType & VersionChangeType.Transform) != 0;
            bool sizeChanged = (versionChangeType & VersionChangeType.Size) != 0;
            bool overflowChanged = (versionChangeType & VersionChangeType.Overflow) != 0;
            bool borderRadiusChanged = (versionChangeType & VersionChangeType.BorderRadius) != 0;
            bool borderWidthChanged = (versionChangeType & VersionChangeType.BorderWidth) != 0;

            if (transformChanged || sizeChanged || borderWidthChanged)
                renderChain.UIEOnTransformOrSizeChanged(ve, transformChanged, sizeChanged || borderWidthChanged);

            if (overflowChanged || borderRadiusChanged)
                renderChain.UIEOnClippingChanged(ve, false);

            if ((versionChangeType & VersionChangeType.Opacity) != 0)
                renderChain.UIEOnOpacityChanged(ve);

            if ((versionChangeType & VersionChangeType.Repaint) != 0)
                renderChain.UIEOnVisualsChanged(ve, false);
        }

        public override void Update()
        {
            if (renderChain?.device == null)
                return;

            using (s_MarkerDrawChain.Auto())
            {
                renderChain.ProcessChanges();

                PanelClearFlags clearFlags = panel.clearFlags;
                if (clearFlags != PanelClearFlags.None)
                {
                    GL.Clear((clearFlags & PanelClearFlags.Depth) != 0, // Clearing may impact MVP
                        (clearFlags & PanelClearFlags.Color) != 0, Color.clear, UIRUtility.k_ClearZ);
                }

                renderChain.Render();
            }
        }

        internal RenderChain DebugGetRenderChain() { return renderChain; }

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
            {
                var renderChain = (it.Current.Value.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater)?.renderChain;
                if (recreate)
                    renderChain?.AfterRenderDeviceRelease();
                else renderChain?.BeforeRenderDeviceRelease();
            }

            if (!recreate)
                UIRenderDevice.FlushAllPendingDeviceDisposes();
            else UIRenderDevice.WrapUpGfxDeviceRecreate();
        }

        void OnPanelChanged(BaseVisualElementPanel obj)
        {
            DisposeRenderChain();
            if (panel != null)
            {
                renderChain = CreateRenderChain();
                if (panel.visualTree != null)
                {
                    renderChain.UIEOnChildAdded(panel.visualTree.hierarchy.parent, panel.visualTree,
                        panel.visualTree.hierarchy.parent == null ? 0 : panel.visualTree.hierarchy.parent.IndexOf(panel.visualTree));
                    renderChain.UIEOnVisualsChanged(panel.visualTree, true);
                }
                panel.standardShaderChanged += OnPanelStandardShaderChanged;
                panel.standardWorldSpaceShaderChanged += OnPanelStandardWorldSpaceShaderChanged;
                panel.hierarchyChanged += OnPanelHierarchyChanged;
                OnPanelStandardShaderChanged();
                if (panel.contextType == ContextType.Player)
                    OnPanelStandardWorldSpaceShaderChanged();
            }
        }

        void OnPanelHierarchyChanged(VisualElement ve, HierarchyChangeType changeType)
        {
            if (renderChain == null || ve.panel == null)
                return;

            switch (changeType)
            {
                case HierarchyChangeType.Add:
                    renderChain.UIEOnChildAdded(ve.hierarchy.parent, ve, ve.hierarchy.parent != null ? ve.hierarchy.parent.IndexOf(ve) : 0);
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

        void DisposeRenderChain()
        {
            if (renderChain != null)
            {
                var oldPanel = renderChain.panel;
                renderChain.Dispose();
                renderChain = null;
                if (oldPanel != null)
                {
                    panel.hierarchyChanged -= OnPanelHierarchyChanged;
                    panel.standardShaderChanged -= OnPanelStandardShaderChanged;
                    ResetAllElementsDataRecursive(oldPanel.visualTree);
                }
            }
        }

        #region Dispose Pattern
        protected bool disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                DisposeRenderChain();
            else UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
