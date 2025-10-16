// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class PreviewGraphViewImp : GraphView
    {
        GraphElement m_PreviewedElement;

        public PreviewGraphViewImp(EditorWindow window, GraphTool graphTool, string graphViewName, GraphRootViewModel graphViewModel, ViewSelection viewSelection, GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive, TypeHandleInfos typeHandleInfos = null)
            : base(window, graphTool, graphViewName, graphViewModel, viewSelection, displayMode, typeHandleInfos) { RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel); }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_PreviewedElement != null)
            {
                if (m_PreviewedElement.Model is IUserNodeModelImp userNodeModelImp)
                {
                    userNodeModelImp.CallOnDisable();
                }
                m_PreviewedElement = null;
            }
        }

        public override void RemoveElement(GraphElement graphElement)
        {
            if (graphElement?.Model is IUserNodeModelImp userNodeModelImp)
            {
                userNodeModelImp.CallOnDisable();
            }
            base.RemoveElement(graphElement);
            m_PreviewedElement = null;
        }

        public override void AddElement(GraphElement graphElement)
        {
            m_PreviewedElement = graphElement;
            base.AddElement(graphElement);
        }
    }
}
