// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    [EditorWindowTitle(title = "MiniMap")]
    public class GraphViewMinimapWindow : GraphViewToolWindow
    {
        MiniMap m_MiniMap;
        Label m_ZoomLabel;

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
            m_ZoomLabel.style.position = Position.Absolute;
            m_ZoomLabel.style.right = 4;
            m_ZoomLabel.style.top = 1;
            m_Toolbar.Add(m_ZoomLabel);
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
            string windowTitle = "Minimap";
            if (m_SelectedGraphView != null)
            {
                windowTitle += " - " + m_SelectedGraphView.name;

                m_SelectedGraphView.redrawn += GraphViewRedrawn;
            }
            else
                ZoomFactorTextChanged("");

            titleContent.text = windowTitle;

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
