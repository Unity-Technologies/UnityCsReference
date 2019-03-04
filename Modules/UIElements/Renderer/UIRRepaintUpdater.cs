// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRRepaintUpdater : BaseVisualTreeUpdater
    {
        internal RenderChain renderChain;

        public UIRRepaintUpdater()
        {
            panelChanged += OnPanelChanged;
        }

        public override string description
        {
            get { return "UIRepaint"; }
        }

        public event Action<UIRenderDevice> BeforeDrawChain
        {
            add { if (renderChain != null) renderChain.BeforeDrawChain += value; }
            remove { if (renderChain != null) renderChain.BeforeDrawChain -= value; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (renderChain == null)
                return;

            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Clip)) != 0)
                renderChain.UIEOnTransformChanged(ve);

            if ((versionChangeType & VersionChangeType.Repaint) != 0)
                renderChain.UIEOnVisualsChanged(ve, false);
        }

        public override void Update()
        {
            if (renderChain?.device == null)
                return;

            var topRect = visualTree.layout;
            var projection = Matrix4x4.Ortho(topRect.xMin, topRect.xMax, topRect.yMax, topRect.yMin, -1, 1);
            DrawChain(topRect, projection);
        }

        internal RenderChain DebugGetRenderChain() { return renderChain; }

        // Overriden in tests
        protected virtual RenderChain CreateRenderChain() { return new RenderChain(panel, panel.standardShader); }
        protected virtual void DrawChain(Rect topRect, Matrix4x4 projection)
        {
            Profiler.BeginSample("DrawChain");
            try
            {
                renderChain.Render(topRect, projection);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

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
                panel.hierarchyChanged += OnPanelHierarchyChanged;
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
            if (renderChain != null)
                renderChain.UIEOnStandardShaderChanged(panel.standardShader);
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
