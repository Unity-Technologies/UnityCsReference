// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    abstract class GraphViewEditorWindow : EditorWindow
    {
        public GraphView graphView { get; private set; }

        private GraphViewPresenter m_Presenter;
        public GraphViewPresenter presenter
        {
            get { return m_Presenter; }
            private set
            {
                if (dataWatchHandle != null && graphView != null)
                {
                    graphView.dataWatch.UnregisterWatch(dataWatchHandle);
                }
                m_Presenter = value;

                if (graphView != null)
                    dataWatchHandle = graphView.dataWatch.RegisterWatch(m_Presenter, OnChanged);
            }
        }

        public T GetPresenter<T>() where T : GraphViewPresenter
        {
            return presenter as T;
        }

        // we watch the data source for destruction and re-create it
        IUIElementDataWatchRequest dataWatchHandle;

        protected void OnEnable()
        {
            presenter = BuildPresenters();
            graphView = BuildView();

            graphView.name = "theView";
            graphView.persistenceKey = "theView";
            graphView.presenter = presenter;
            graphView.StretchToParentSize();
            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);

            if (dataWatchHandle == null)
            {
                dataWatchHandle = graphView.dataWatch.RegisterWatch(m_Presenter, OnChanged);
            }

            this.GetRootVisualContainer().Add(graphView);
        }

        protected void OnDisable()
        {
            this.GetRootVisualContainer().Remove(graphView);
        }

        // Override these methods to properly support domain reload & enter/exit playmode
        protected abstract GraphView BuildView();
        protected abstract GraphViewPresenter BuildPresenters();

        void OnEnterPanel(AttachToPanelEvent e)
        {
            if (presenter == null)
            {
                presenter = BuildPresenters();
                graphView.presenter = presenter;
            }
        }

        void OnChanged(Object changedObject)
        {
            // If data was destroyed, remove the watch and try to re-create it
            if (presenter == null && graphView.panel != null)
            {
                presenter = BuildPresenters();
                graphView.presenter = presenter;
            }
        }
    }
}
