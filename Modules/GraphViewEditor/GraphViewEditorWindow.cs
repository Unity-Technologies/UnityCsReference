// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal abstract class GraphViewEditorWindow : EditorWindow
    {
        public GraphView graphView { get; private set; }
        public GraphViewPresenter presenter { get; private set; }

        public T GetPresenter<T>() where T : GraphViewPresenter
        {
            return presenter as T;
        }

        // we watch the data source for destruction and re-create it
        IDataWatchHandle handle;

        protected void OnEnable()
        {
            presenter = BuildPresenters();
            graphView = BuildView();
            graphView.name = "theView";
            graphView.presenter = presenter;
            graphView.StretchToParentSize();
            graphView.onEnter += OnEnterPanel;
            graphView.onLeave += OnLeavePanel;

            this.GetRootVisualContainer().AddChild(graphView);
        }

        protected void OnDisable()
        {
            this.GetRootVisualContainer().RemoveChild(graphView);
        }

        // Override these methods to properly support domain reload & enter/exit playmode
        protected abstract GraphView BuildView();
        protected abstract GraphViewPresenter BuildPresenters();

        void OnEnterPanel()
        {
            if (presenter == null)
            {
                presenter = BuildPresenters();
                graphView.presenter = presenter;
            }
            handle = graphView.panel.dataWatch.AddWatch(graphView, presenter, OnChanged);
        }

        void OnLeavePanel()
        {
            if (handle != null)
            {
                handle.Dispose();
                handle = null;
            }
            else
            {
                Debug.LogError("No active handle to remove");
            }
        }

        void OnChanged()
        {
            // If data was destroyed, remove the watch and try to re-create it
            if (presenter == null && graphView.panel != null)
            {
                if (handle != null)
                {
                    handle.Dispose();
                }

                presenter = BuildPresenters();
                graphView.presenter = presenter;
                handle = graphView.panel.dataWatch.AddWatch(graphView, presenter, OnChanged);
            }
        }
    }
}
