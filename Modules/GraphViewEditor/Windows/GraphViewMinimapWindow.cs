// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphView not yet converted
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    [EditorWindowTitle(title = k_ToolName)]
    public class GraphViewMinimapWindow : GraphViewToolWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public GraphViewMinimapWindow() { }
        #pragma warning restore UAL0015

        MiniMap m_MiniMap;
        Label m_ZoomLabel;

        const string k_ToolName = "MiniMap";

        protected override string ToolName => k_ToolName;

        new void OnEnable()
        {
            base.OnEnable();
            var root = rootVisualElement;
            m_MiniMap = new MiniMap();
            m_MiniMap.windowed = true;
            m_MiniMap.zoomFactorTextChanged += ZoomFactorTextChanged;
            root.Add(m_MiniMap);

            OnGraphViewChanged();
            m_ZoomLabel = new Label();
            m_ZoomLabel.style.width = 35;
            m_ZoomLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            m_ToolbarContainer.Add(m_ZoomLabel);
        }

        void OnDestroy()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn -= GraphViewRedrawn;
        }

        protected override void OnGraphViewChanging()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn -= GraphViewRedrawn;
        }

        protected override void OnGraphViewChanged()
        {
            if (m_SelectedGraphView != null)
                m_SelectedGraphView.redrawn += GraphViewRedrawn;
            else
                ZoomFactorTextChanged("");

            if (m_MiniMap == null) // Probably called from base.OnEnable(). We're not ready just yet.
                return;

            m_MiniMap.graphView = m_SelectedGraphView;
            m_MiniMap.MarkDirtyRepaint();
        }

        void ZoomFactorTextChanged(string text)
        {
            if (m_ZoomLabel != null)
                m_ZoomLabel.text = text;
        }

        void GraphViewRedrawn()
        {
            m_MiniMap.MarkDirtyRepaint();
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return true;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
