// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class GraphViewEditorWindowImp : GraphViewEditorWindow, IGraphWindow
    {
        class MainToolbarDefinition : ToolbarDefinition
        {
            readonly GraphViewEditorWindowImp m_Window;

            public MainToolbarDefinition(GraphViewEditorWindowImp window)
            {
                m_Window = window;
            }

            public override IEnumerable<string> ElementIds
                => new[] { SaveButton.id, ShowInProjectWindowButton.id };

            public override IReadOnlyDictionary<string, ToolbarElementDefinition> CustomElementMap
            {
                get
                {
                    var map = new Dictionary<string, ToolbarElementDefinition>();
                    if (m_Window.Graph == null)
                        return map;

                    var graphType = m_Window.Graph.GetType();
                    foreach (var type in TypeCache.GetTypesWithAttribute<GraphToolbarElementAttribute>())
                    {
                        var attrs = type.GetCustomAttributes<GraphToolbarElementAttribute>();
                        foreach (var attr in attrs)
                        {
                            if (!attr.GraphType.IsAssignableFrom(graphType))
                                continue;

                            if (map.ContainsKey(attr.Id))
                            {
                                Debug.LogWarning($"Duplicate GraphToolbarElement id '{attr.Id}' on type {type.Name}. Skipping.");
                                break;
                            }

                            map[attr.Id] = new ToolbarElementDefinition(attr.Order, type);
                            break;
                        }
                    }

                    return map;
                }
            }
        }


        public Graph Graph =>
                (GraphTool?.ToolState?.GraphModel as Implementation.GraphModelImp)?.Graph;

        public static GraphViewEditorWindowImp GetOpenedWindow(GraphObjectImp graphObject)
        {
            for (int i = OpenedWindows.Count - 1; i >= 0; --i)
            {
                var window = OpenedWindows[i];
                if (window.GraphTool.ToolState.GraphObject == graphObject)
                {
                    return window as GraphViewEditorWindowImp;
                }
            }

            return null;
        }

        public static void ShowGraph(GraphObjectImp graphObject)
        {
            foreach (var window in OpenedWindows)
            {
                if (window?.GraphTool?.ToolState?.GraphObject == graphObject)
                {
                    window.Focus();
                    return;
                }
            }

            var graphWindow = EditorWindow.CreateWindow<GraphViewEditorWindowImp>(typeof(GraphViewEditorWindowImp), typeof(GraphViewEditorWindow), typeof(SceneView));
            graphWindow.GraphTool.Dispatch(new LoadGraphCommand(graphObject.GraphModel));
        }

        protected override void OnEnable()
        {
            PublicGraphFactory.EnsureStaticConstructorIsCalled();
            base.OnEnable();
        }

        protected override GraphTool CreateGraphTool()
        {
            return GraphTool.Create<GraphToolImp>(WindowID);
        }

        public override ItemLibraryHelper CreateItemLibraryHelper(GraphModel graphModel)
        {
            return new PublicLibraryHelper(graphModel);
        }

        protected override GraphView CreateGraphView(GraphRootViewModel viewModel, ViewSelection viewSelection)
        {
            return new GraphViewImp(this, GraphTool, GraphViewName, viewModel, viewSelection);
        }

        protected override BlackboardContentModel CreateBlackboardContentModel()
        {
            return new BlackboardContentModelImp(GraphTool);
        }

        protected override ToolbarDefinition CreateToolbarDefinition(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainToolbar.toolbarId:
                    return new MainToolbarDefinition(this);
                default:
                    return base.CreateToolbarDefinition(toolbarId);
            }
        }
    }
}
