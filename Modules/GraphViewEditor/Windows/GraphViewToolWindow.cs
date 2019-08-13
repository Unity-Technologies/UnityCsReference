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

namespace UnityEditor.Experimental.GraphView
{
    public abstract class GraphViewToolWindow : EditorWindow
    {
        struct GraphViewChoice
        {
            public EditorWindow window;
            public GraphView graphView;
            public int idx;
            public bool canUse;
        }

        const string k_DefaultSelectorName = "Select a panel";

        UnityEditor.UIElements.Toolbar m_Toolbar;
        protected VisualElement m_ToolbarContainer;
        ToolbarMenu m_SelectorMenu;

        [SerializeField]
        EditorWindow m_SelectedWindow;

        [SerializeField]
        int m_SelectedGraphViewIdx;

        protected GraphView m_SelectedGraphView;
        List<GraphViewChoice> m_GraphViewChoices;

        bool m_FirstUpdate;

        protected abstract string ToolName { get; }

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
            m_Toolbar.style.flexGrow = 1;
            m_Toolbar.style.overflow = Overflow.Hidden;
            m_ToolbarContainer = new VisualElement();
            m_ToolbarContainer.style.flexDirection = FlexDirection.Row;
            m_ToolbarContainer.Add(m_Toolbar);

            root.Add(m_ToolbarContainer);

            m_FirstUpdate = true;

            titleContent.text = ToolName;
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

            UpdateGraphViewName();
        }

        void RefreshPanelChoices()
        {
            m_GraphViewChoices.Clear();

            var guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);

            var usedGraphViews = new HashSet<GraphView>();
            // Get all GraphViews used by existing tool windows of our type
            using (var it = UIElementsUtility.GetPanelsIterator())
            {
                var currentWindowType = GetType();
                while (it.MoveNext())
                {
                    var dockArea = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as DockArea;
                    if (dockArea == null)
                        continue;

                    foreach (var graphViewTool in dockArea.m_Panes.Where(p => p.GetType() == currentWindowType).Cast<GraphViewToolWindow>())
                    {
                        if (graphViewTool.m_SelectedGraphView != null)
                        {
                            usedGraphViews.Add(graphViewTool.m_SelectedGraphView);
                        }
                    }
                }
            }


            // Get all the existing GraphViewWindows we could use...
            using (var it = UIElementsUtility.GetPanelsIterator())
            {
                while (it.MoveNext())
                {
                    var dockArea = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as DockArea;
                    if (dockArea == null)
                        continue;

                    foreach (var graphViewWindow in dockArea.m_Panes.OfType<GraphViewEditorWindow>())
                    {
                        int idx = 0;
                        foreach (var graphView in graphViewWindow.graphViews.Where(IsGraphViewSupported))
                        {
                            m_GraphViewChoices.Add(new GraphViewChoice {window = graphViewWindow, idx = idx++, graphView = graphView, canUse = !usedGraphViews.Contains(graphView)});
                        }
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
                    a =>
                    {
                        var gvc = (GraphViewChoice)a.userData;
                        return (gvc.graphView == m_SelectedGraphView
                            ? DropdownMenuAction.Status.Checked
                            : (gvc.canUse
                                ? DropdownMenuAction.Status.Normal
                                : DropdownMenuAction.Status.Disabled));
                    },
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
            m_SelectedWindow = choice?.window;
            m_SelectedGraphViewIdx = choice?.idx ?? -1;
            OnGraphViewChanged();
            UpdateGraphViewName();
        }

        // Called just before the change.
        protected abstract void OnGraphViewChanging();

        // Called just after the change.
        protected abstract void OnGraphViewChanged();

        protected virtual bool IsGraphViewSupported(GraphView gv)
        {
            return false;
        }

        void UpdateGraphViewName()
        {
            string updatedName = k_DefaultSelectorName;
            if (m_SelectedGraphView != null)
                updatedName = m_SelectedGraphView.name;

            if (m_SelectorMenu.text != updatedName)
                m_SelectorMenu.text = updatedName;
        }
    }
}
