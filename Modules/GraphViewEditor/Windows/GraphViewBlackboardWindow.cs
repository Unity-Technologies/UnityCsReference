// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphView not yet converted
using System;
using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    [EditorWindowTitle(title = k_ToolName)]
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public GraphViewBlackboardWindow() { }
        #pragma warning restore UAL0015

        Blackboard m_Blackboard;

        const string k_ToolName = "Blackboard";

        protected override string ToolName => k_ToolName;

        new void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        void OnDisable()
        {
            if (m_SelectedGraphView != null && m_Blackboard != null)
            {
                m_SelectedGraphView.ReleaseBlackboard(m_Blackboard);
            }
        }

        protected override void OnGraphViewChanging()
        {
            if (m_Blackboard != null)
            {
                if (m_SelectedGraphView != null)
                {
                    m_SelectedGraphView.ReleaseBlackboard(m_Blackboard);
                }
                rootVisualElement.Remove(m_Blackboard);
                m_Blackboard = null;
            }
        }

        protected override void OnGraphViewChanged()
        {
            if (m_SelectedGraphView != null)
            {
                m_Blackboard = m_SelectedGraphView.GetBlackboard();
                m_Blackboard.windowed = true;
                rootVisualElement.Add(m_Blackboard);
            }
            else
            {
                m_Blackboard = null;
            }
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return gv.supportsWindowedBlackboard;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
