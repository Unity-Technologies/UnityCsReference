// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    [EditorWindowTitle(title = "Blackboard")]
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        Blackboard m_Blackboard;

        new void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        protected override void OnGraphViewChanging()
        {
            if (m_Blackboard != null)
            {
                rootVisualElement.Remove(m_Blackboard);
                m_Blackboard = null;
            }
        }

        protected override void OnGraphViewChanged()
        {
            string windowTitle = "Blackboard";
            if (m_SelectedGraphView != null)
            {
                windowTitle += " - " + m_SelectedGraphView.name;
                m_Blackboard = m_SelectedGraphView.CreateBlackboard();
                m_Blackboard.windowed = true;
                rootVisualElement.Add(m_Blackboard);
            }

            titleContent.text = windowTitle;
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return gv.supportsWindowedBlackboard;
        }
    }
}
