// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphView not yet converted
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    public abstract class GraphViewEditorWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        protected GraphViewEditorWindow() { }
        #pragma warning restore UAL0015

        public virtual IEnumerable<GraphView> graphViews { get; }

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            #pragma warning disable UAC2001 // Avoid Linq
            return Assembly
#pragma warning restore UAC2001
                .GetAssembly(typeof(GraphViewToolWindow))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GraphViewToolWindow)));
        }

        public static List<EditorWindow> ShowGraphViewWindowWithTools<T>() where T : GraphViewEditorWindow
        {
            const float width = 1200;
            const float height = 800;

            const float toolsWidth = 200;

            var mainSplitView = CreateInstance<SplitView>();

            var sideSplitView = CreateInstance<SplitView>();
            sideSplitView.vertical = true;
            sideSplitView.position = new Rect(0, 0, toolsWidth, height);
            var dockArea = CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, height - toolsWidth);
            var blackboardWindow = CreateInstance<GraphViewBlackboardWindow>();
            dockArea.AddTab(blackboardWindow);
            sideSplitView.AddChild(dockArea);

            dockArea = CreateInstance<DockArea>();
            dockArea.position = new Rect(0, 0, toolsWidth, toolsWidth);
            var minimapWindow = CreateInstance<GraphViewMinimapWindow>();
            dockArea.AddTab(minimapWindow);
            sideSplitView.AddChild(dockArea);

            mainSplitView.AddChild(sideSplitView);
            dockArea = CreateInstance<DockArea>();
            var graphViewWindow = CreateInstance<T>();
            dockArea.AddTab(graphViewWindow);
            dockArea.position = new Rect(0, 0, width - toolsWidth, height);
            mainSplitView.AddChild(dockArea);

            #pragma warning disable UAC2011 // Avoid Linq
            var graphView = graphViewWindow.graphViews.FirstOrDefault();
#pragma warning restore UAC2011
            if (graphView != null)
            {
                blackboardWindow.SelectGraphViewFromWindow(graphViewWindow, graphView);
                minimapWindow.SelectGraphViewFromWindow(graphViewWindow, graphView);
            }

            var containerWindow = CreateInstance<ContainerWindow>();
            containerWindow.m_DontSaveToLayout = false;
            containerWindow.position = new Rect(100, 100, width, height);
            containerWindow.rootView = mainSplitView;
            containerWindow.rootView.position = new Rect(0, 0, mainSplitView.position.width, mainSplitView.position.height);

            containerWindow.Show(ShowMode.NormalWindow, false, true, setFocus: true);

            return new List<EditorWindow> { graphViewWindow, blackboardWindow, minimapWindow };
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
