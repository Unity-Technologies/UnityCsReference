// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
//using Toolbar = UnityEditor.UIElements.Toolbar;

namespace UnityEditor.Experimental.GraphView
{
    public abstract class GraphViewToolWindow : EditorWindow
    {
        struct GraphViewChoice
        {
            public EditorWindow window;
            public GraphView graphView;
            public int idx;
        }

        protected UnityEditor.UIElements.Toolbar m_Toolbar;
        ToolbarMenu m_SelectorMenu;

        [SerializeField]
        EditorWindow m_SelectedWindow;

        [SerializeField]
        int m_SelectedGraphViewIdx;

        protected GraphView m_SelectedGraphView;
        List<GraphViewChoice> m_GraphViewChoices;

        bool m_FirstUpdate;

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            return Assembly
                .GetAssembly(typeof(GraphViewToolWindow))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GraphViewToolWindow)));
        }

        public void SelectGraphViewFromWindow(GraphViewEditorWindow window, GraphView graphView, int graphViewIndexInWindow = 0)
        {
            var gvChoice = new GraphViewChoice { window = window, graphView = graphView, idx = graphViewIndexInWindow };
            SelectGraphView(gvChoice);
        }

        protected void OnEnable()
        {
            var root = rootVisualElement;

            this.SetAntiAliasing(4);

            m_Toolbar = new UnityEditor.UIElements.Toolbar();

            // Register panel choice refresh on the toolbar so the event
            // is received before the ToolbarPopup clickable handle it.
            m_Toolbar.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.target == m_SelectorMenu)
                    RefreshPanelChoices();
            }, TrickleDown.TrickleDown);
            m_GraphViewChoices = new List<GraphViewChoice>();
            m_SelectorMenu = new ToolbarMenu { name = "panelSelectPopup", text = "Select a panel" };

            var menu = m_SelectorMenu.menu;
            menu.AppendAction("None", OnSelectGraphView,
                a => m_SelectedGraphView == null ?
                DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_Toolbar.Add(m_SelectorMenu);

            root.Add(m_Toolbar);

            m_FirstUpdate = true;
        }

        void Update()
        {
            // We need to wait until all the windows are created to re-assign a potential graphView.
            if (m_FirstUpdate)
            {
                m_FirstUpdate = false;
                if (m_SelectedWindow != null)
                {
                    var graphViewEditor = m_SelectedWindow as GraphViewEditorWindow;
                    if (graphViewEditor != null && m_SelectedGraphViewIdx >= 0 && m_SelectedGraphView == null)
                    {
                        m_SelectedGraphView = graphViewEditor.graphViews.ElementAt(m_SelectedGraphViewIdx);
                        m_SelectorMenu.text = m_SelectedGraphView.name;
                        OnGraphViewChanged();
                    }
                }
            }
            else
            {
                if (!m_SelectedWindow && m_SelectedGraphView != null)
                    SelectGraphView(null);
            }
        }

        void RefreshPanelChoices()
        {
            m_GraphViewChoices.Clear();

            List<GUIView> guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                GUIView view = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key);
                if (view == null)
                    continue;

                DockArea dockArea = view as DockArea;
                if (dockArea == null)
                    continue;

                foreach (var graphViewEditor in dockArea.m_Panes.OfType<GraphViewEditorWindow>())
                {
                    int idx = 0;
                    foreach (var graphView in graphViewEditor.graphViews.Where(IsGraphViewSupported))
                    {
                        m_GraphViewChoices.Add(new GraphViewChoice {window = graphViewEditor, idx = idx++, graphView = graphView});
                    }
                }
            }

            var menu = m_SelectorMenu.menu;
            var menuItemsCount = menu.MenuItems().Count;

            // Clear previous items (but not the "none" one at the top of the list)
            for (int i = menuItemsCount - 1; i > 0; i--)
                menu.RemoveItemAt(i);

            foreach (var graphView in m_GraphViewChoices)
            {
                menu.AppendAction(graphView.graphView.name, OnSelectGraphView,
                    a => ((GraphViewChoice)a.userData).graphView == m_SelectedGraphView ?
                    DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                    graphView);
            }
        }

        void OnSelectGraphView(DropdownMenuAction action)
        {
            var choice = (GraphViewChoice?)action.userData;
            var newlySelectedGraphView = choice?.graphView;
            if (newlySelectedGraphView == m_SelectedGraphView)
                return;

            SelectGraphView(choice);
        }

        void SelectGraphView(GraphViewChoice? choice)
        {
            OnGraphViewChanging();
            m_SelectedGraphView = choice?.graphView;
            if (m_SelectedGraphView != null)
                m_SelectorMenu.text = m_SelectedGraphView.name;
            else
                m_SelectorMenu.text = "Select a panel";

            m_SelectedWindow = choice?.window;
            m_SelectedGraphViewIdx = choice?.idx ?? -1;

            OnGraphViewChanged();
        }

        // Called just before the change.
        protected abstract void OnGraphViewChanging();

        // Called just after the change.
        protected abstract void OnGraphViewChanged();

        protected virtual bool IsGraphViewSupported(GraphView gv)
        {
            return false;
        }
    }
}
