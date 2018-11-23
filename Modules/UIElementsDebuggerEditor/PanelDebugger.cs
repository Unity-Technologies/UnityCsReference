// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal abstract class PanelDebugger : EditorWindow, IPanelDebugger
    {
        protected class PanelChoice
        {
            public Panel panel;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        [SerializeField]
        private string m_LastVisualTreeName;

        private EditorWindow m_WindowToDebug;

        private PanelChoice m_SelectedPanel;
        protected Toolbar m_Toolbar;
        private ToolbarMenu m_PanelSelect;
        private List<PanelChoice> m_PanelChoices;
        private IVisualElementScheduledItem m_ConnectWindowScheduledItem;
        private IVisualElementScheduledItem m_RestoreSelectionScheduledItem;

        public IPanelDebug panelDebug { get; set; }

        protected IPanel panel
        {
            get { return panelDebug?.panel; }
        }

        protected VisualElement visualTree
        {
            get { return panelDebug?.visualTree; }
        }

        public bool showOverlay => false;

        public void OnEnable()
        {
            m_Toolbar = new Toolbar();

            // Register panel choice refresh on the toolbar so the event
            // is received before the ToolbarPopup clickable handle it.
            m_Toolbar.RegisterCallback<MouseDownEvent>((e) =>
            {
                if (e.target == m_PanelSelect)
                    RefreshPanelChoices();
            }, TrickleDown.TrickleDown);

            m_PanelChoices = new List<PanelChoice>();
            m_PanelSelect = new ToolbarMenu() { name = "panelSelectPopup", variant = ToolbarMenu.Variant.Popup};
            m_PanelSelect.text = "Select a panel";

            m_Toolbar.Add(m_PanelSelect);

            if (!string.IsNullOrEmpty(m_LastVisualTreeName))
                m_RestoreSelectionScheduledItem = m_Toolbar.schedule.Execute(RestorePanelSelection).Every(500);
        }

        public void OnDisable()
        {
            var lastTreeName = m_LastVisualTreeName;
            panelDebug?.DetachDebugger(this);
            SelectPanelToDebug((PanelChoice)null);

            m_LastVisualTreeName = lastTreeName;
        }

        public void Disconnect()
        {
            var lastTreeName = m_LastVisualTreeName;
            m_SelectedPanel = null;
            SelectPanelToDebug((PanelChoice)null);

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
            var root = m_WindowToDebug.rootVisualElement;
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

        public abstract void Refresh();

        protected virtual bool ValidateDebuggerConnection(IPanel panelConnection)
        {
            return true;
        }

        protected virtual void OnSelectPanelDebug(IPanelDebug pdbg) {}
        protected virtual void OnRestorePanelSelection() {}

        private void RefreshPanelChoices()
        {
            m_PanelChoices.Clear();

            List<GUIView> guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                GUIView view = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key);
                if (view == null)
                    continue;

                // Skip this window
                HostView hostView = view as HostView;
                if (hostView != null && hostView.actualView == this)
                    continue;

                var p = it.Current.Value;
                m_PanelChoices.Add(new PanelChoice { panel = p, name = p.name });
            }

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

            SelectPanelToDebug(action.userData as PanelChoice);
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
                        if (vt.name == m_LastVisualTreeName)
                        {
                            SelectPanelToDebug((PanelChoice)vt);
                            break;
                        }
                    }
                }

                if (m_SelectedPanel != null)
                    OnRestorePanelSelection();
                else
                    SelectPanelToDebug((PanelChoice)null);

                m_RestoreSelectionScheduledItem.Pause();
            }
        }

        private void SelectPanelToDebug(PanelChoice pc)
        {
            // Detach debugger from current panel
            if (m_SelectedPanel != null)
                m_SelectedPanel.panel.panelDebug.DetachDebugger(this);

            string menuText = "";

            if (pc != null && ValidateDebuggerConnection(pc.panel))
            {
                pc.panel.panelDebug.AttachDebugger(this);

                m_SelectedPanel = pc;
                m_LastVisualTreeName = pc.name;

                OnSelectPanelDebug(panelDebug);
                menuText = pc.name;
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
                SelectPanelToDebug((PanelChoice)null);
                RefreshPanelChoices();
                for (int i = 0; i < m_PanelChoices.Count; i++)
                {
                    var vt = m_PanelChoices[i];
                    if (vt.panel == panel)
                    {
                        SelectPanelToDebug((PanelChoice)vt);
                        break;
                    }
                }
            }
        }

        public abstract void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag);

        public abstract bool InterceptEvent(EventBase ev);
        public abstract void PostProcessEvent(EventBase ev);
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
