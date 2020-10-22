using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Serialization;

using Toolbar = UnityEditor.UIElements.Toolbar;

namespace UnityEditor.UIElements.Debugger
{
    internal interface IPanelChoice
    {
        Panel panel { get; }
    }

    internal class PanelChoice : IPanelChoice
    {
        public Panel panel { get; }

        public PanelChoice(Panel p)
        {
            panel = p;
        }

        public override string ToString()
        {
            return panel.name ?? panel.visualTree.name;
        }
    }

    [Serializable]
    internal class PanelDebugger : IPanelDebugger
    {
        [SerializeField]
        private string m_LastVisualTreeName;

        protected EditorWindow m_DebuggerWindow;
        private EditorWindow m_WindowToDebug;

        private IPanelChoice m_SelectedPanel;
        protected VisualElement m_Toolbar;
        protected ToolbarMenu m_PanelSelect;
        private List<IPanelChoice> m_PanelChoices;
        private IVisualElementScheduledItem m_ConnectWindowScheduledItem;
        private IVisualElementScheduledItem m_RestoreSelectionScheduledItem;

        protected void TryFocusCorrespondingWindow(ScriptableObject panelOwner)
        {
            var hostView = panelOwner as HostView;
            if (hostView != null && hostView.actualView != null)
                hostView.actualView.Focus();
        }

        public IPanelDebug panelDebug { get; set; }

        protected IPanel panel
        {
            get { return panelDebug?.panel; }
        }

        protected VisualElement visualTree
        {
            get { return panelDebug?.visualTree; }
        }

        public void Initialize(EditorWindow debuggerWindow)
        {
            m_DebuggerWindow = debuggerWindow;

            if (m_Toolbar == null)
                m_Toolbar = new Toolbar();

            // Register panel choice refresh on the toolbar so the event
            // is received before the ToolbarPopup clickable handle it.
            m_Toolbar.RegisterCallback<MouseDownEvent>((e) =>
            {
                if (e.target == m_PanelSelect)
                    RefreshPanelChoices();
            }, TrickleDown.TrickleDown);

            m_PanelChoices = new List<IPanelChoice>();
            m_PanelSelect = new ToolbarMenu() { name = "panelSelectPopup", variant = ToolbarMenu.Variant.Popup};
            m_PanelSelect.text = "Select a panel";

            m_Toolbar.Insert(0, m_PanelSelect);

            if (!string.IsNullOrEmpty(m_LastVisualTreeName))
                m_RestoreSelectionScheduledItem = m_Toolbar.schedule.Execute(RestorePanelSelection).Every(500);
        }

        public void OnDisable()
        {
            var lastTreeName = m_LastVisualTreeName;
            panelDebug?.DetachDebugger(this);
            SelectPanelToDebug((IPanelChoice)null);

            m_LastVisualTreeName = lastTreeName;
        }

        public void Disconnect()
        {
            var lastTreeName = m_LastVisualTreeName;
            m_SelectedPanel = null;
            SelectPanelToDebug((IPanelChoice)null);

            m_LastVisualTreeName = lastTreeName;
        }

        public void ScheduleWindowToDebug(EditorWindow window)
        {
            if (window != null)
            {
                Disconnect();
                m_WindowToDebug = window;
                m_ConnectWindowScheduledItem = m_Toolbar.schedule.Execute(TrySelectWindow).Every(500);
            }
        }

        private void TrySelectWindow()
        {
            VisualElement root = null;

            if (m_WindowToDebug is GameView)
            {
                var runtimePanels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
                if (runtimePanels != null && runtimePanels.Count > 0)
                {
                    root = runtimePanels[0].visualTree;
                }
            }

            if (root == null)
            {
                root = m_WindowToDebug.rootVisualElement;
            }


            if (root == null)
                return;

            IPanel searchedPanel = root.panel;
            SelectPanelToDebug(searchedPanel);

            if (m_SelectedPanel != null)
            {
                m_WindowToDebug = null;
                m_ConnectWindowScheduledItem.Pause();
            }
        }

        public virtual void Refresh()
        {}

        protected virtual bool ValidateDebuggerConnection(IPanel panelConnection)
        {
            return true;
        }

        protected virtual void OnSelectPanelDebug(IPanelDebug pdbg) {}
        protected virtual void OnRestorePanelSelection() {}

        protected virtual void PopulatePanelChoices(List<IPanelChoice> panelChoices)
        {
            List<GUIView> guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                // Skip this debugger window
                GUIView view = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key);
                HostView hostView = view as HostView;
                if (hostView != null && hostView.actualView == m_DebuggerWindow)
                    continue;

                var p = it.Current.Value;
                panelChoices.Add(new PanelChoice(p));
            }
        }

        private void RefreshPanelChoices()
        {
            m_PanelChoices.Clear();
            PopulatePanelChoices(m_PanelChoices);

            var menu = m_PanelSelect.menu;
            var menuItemsCount = menu.MenuItems().Count;

            // Clear previous items
            for (int i = 0; i < menuItemsCount; i++)
            {
                menu.RemoveItemAt(0);
            }

            foreach (var panelChoice in m_PanelChoices)
            {
                menu.AppendAction(panelChoice.ToString(), OnSelectPanel, DropdownMenuAction.AlwaysEnabled, panelChoice);
            }
        }

        private void OnSelectPanel(DropdownMenuAction action)
        {
            if (m_RestoreSelectionScheduledItem != null && m_RestoreSelectionScheduledItem.isActive)
                m_RestoreSelectionScheduledItem.Pause();

            SelectPanelToDebug(action.userData as IPanelChoice);
        }

        private void RestorePanelSelection()
        {
            RefreshPanelChoices();
            if (m_PanelChoices.Count > 0)
            {
                if (!string.IsNullOrEmpty(m_LastVisualTreeName))
                {
                    // Try to retrieve last selected VisualTree
                    for (int i = 0; i < m_PanelChoices.Count; i++)
                    {
                        var vt = m_PanelChoices[i];
                        if (vt.ToString() == m_LastVisualTreeName)
                        {
                            SelectPanelToDebug((IPanelChoice)vt);
                            break;
                        }
                    }
                }

                if (m_SelectedPanel != null)
                    OnRestorePanelSelection();
                else
                    SelectPanelToDebug((IPanelChoice)null);

                m_RestoreSelectionScheduledItem.Pause();
            }
        }

        protected virtual void SelectPanelToDebug(IPanelChoice pc)
        {
            // Detach debugger from current panel
            if (m_SelectedPanel != null)
                m_SelectedPanel.panel.panelDebug.DetachDebugger(this);

            string menuText = "";

            if (pc != null && ValidateDebuggerConnection(pc.panel))
            {
                pc.panel.panelDebug.AttachDebugger(this);

                m_SelectedPanel = pc;
                m_LastVisualTreeName = pc.ToString();

                OnSelectPanelDebug(panelDebug);
                menuText = pc.ToString();
            }
            else
            {
                // No tree selected
                m_SelectedPanel = null;
                m_LastVisualTreeName = null;

                OnSelectPanelDebug(null);
                menuText = "Select a panel";
            }

            m_PanelSelect.text = menuText;
        }

        protected void SelectPanelToDebug(IPanel panel)
        {
            // Select new tree
            if (m_SelectedPanel?.panel != panel)
            {
                SelectPanelToDebug((IPanelChoice)null);
                RefreshPanelChoices();
                for (int i = 0; i < m_PanelChoices.Count; i++)
                {
                    var pc = m_PanelChoices[i];
                    if (pc.panel == panel)
                    {
                        SelectPanelToDebug((IPanelChoice)pc);
                        break;
                    }
                }
            }
        }

        public virtual void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag)
        {}

        public virtual bool InterceptEvent(EventBase ev)
        {
            return false;
        }

        public virtual void PostProcessEvent(EventBase ev)
        {}
    }

    internal static class UIElementsDebuggerExtension
    {
        public static VisualElement GetRootVisualElement(this IPanel panel)
        {
            if (panel == null)
                return null;

            var visualTree = panel.visualTree;

            // GUIView only has an IMGUIContainer and HostView the IMGUIContainer + root element
            if (visualTree.childCount == 1)
                return null;

            return visualTree[1];
        }

        public static int FindVisualElementIndex(this IPanel panel, VisualElement ve)
        {
            if (panel == null)
                return -1;

            int index = 0;
            var visualTree = panel.visualTree;
            RecurseVisualElementIndex(visualTree, ve, ref index);

            return index;
        }

        public static VisualElement FindVisualElementByIndex(this IPanel panel, int index)
        {
            if (panel == null || index < 0)
                return null;

            int startIndex = 0;
            var visualTree = panel.visualTree;
            return RecurseVisualElementByIndex(visualTree, index, ref startIndex);
        }

        private static bool RecurseVisualElementIndex(VisualElement root, VisualElement ve, ref int index)
        {
            if (root == ve)
                return true;

            int count = root.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = root.hierarchy[i];
                ++index;
                bool found = RecurseVisualElementIndex(child, ve, ref index);
                if (found)
                    return true;
            }

            return false;
        }

        private static VisualElement RecurseVisualElementByIndex(VisualElement ve, int index, ref int currentIndex)
        {
            if (index == currentIndex)
                return ve;

            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                ++currentIndex;
                var found = RecurseVisualElementByIndex(child, index, ref currentIndex);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
