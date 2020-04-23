using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements.Debugger
{
    internal class VisualTreeDebug
    {
        public Panel panel;
        public VisualElement visualTree { get { return panel.visualTree; } }

        public string name { get { return panel.name; } }
    }

    internal abstract class UIRDebugger : EditorWindow, IPanelDebugger
    {
        [SerializeField]
        private string m_LastVisualTreeName;

        protected VisualTreeDebug m_SelectedVisualTree;
        protected List<VisualTreeDebug> m_VisualTrees;
        private GUIContent[] m_Labels;

        private int m_SelectedPanelDropDownIndex;
        private bool m_TryAutoSelect;

        private static int s_PopupHash = "EditorPopup".GetHashCode();
        private const float kSingleLineHeight = 16;
        private const float kDropDownWidth = 300;

        public IPanelDebug panelDebug { get; set; }
        public virtual bool showOverlay { get { return false; } }

        protected abstract void OnSelectVisualTree(VisualTreeDebug vtDebug);
        public abstract void Refresh();

        public void OnEnable()
        {
            m_VisualTrees = new List<VisualTreeDebug>();
            m_Labels = new GUIContent[0];

            m_TryAutoSelect = true;
        }

        public void OnDestroy()
        {
            if (m_SelectedVisualTree != null)
                m_SelectedVisualTree.panel.panelDebug?.DetachDebugger(this);
        }

        public void Disconnect()
        {
            SelectVisualTree(null);
        }

        public void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag)
        {
        }

        public bool InterceptEvent(EventBase ev)
        {
            return false;
        }

        public void PostProcessEvent(EventBase ev)
        {
        }

        // Show a dropdown of all Panels with UIR enabled
        protected void OnGUIPanelSelectDropDown()
        {
            var label = m_SelectedPanelDropDownIndex >= 0 && m_SelectedPanelDropDownIndex < m_Labels.Length ? m_Labels[m_SelectedPanelDropDownIndex] : GUIContent.Temp("Select a panel");
            if (GUILayout.Button(label, EditorStyles.popup, GUILayout.Width(kDropDownWidth)))
            {
                RefreshPanelDropdown();
                Rect rect = EditorGUILayout.GetControlRect(false, kSingleLineHeight, EditorStyles.popup);
                var controlID = GUIUtility.GetControlID(s_PopupHash, FocusType.Keyboard, rect);
                var position = EditorGUI.IndentedRect(rect);
                position.y += kSingleLineHeight;
                EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(controlID);
                EditorUtility.DisplayCustomMenu(position, m_Labels, m_SelectedPanelDropDownIndex, (data, options, selected) =>
                {
                    m_SelectedPanelDropDownIndex = selected;
                    if (selected > 0)
                    {
                        SelectVisualTree(m_VisualTrees[selected - 1]);
                    }
                    else
                    {
                        SelectVisualTree(null);
                    }
                }, null);
                GUIUtility.keyboardControl = controlID;
            }

            bool autoSelect = false;
            autoSelect = GUILayout.Toggle(autoSelect, GUIContent.Temp("Auto Select"), EditorStyles.toolbarButton, GUILayout.Width(100));
            bool refresh = false;
            refresh = GUILayout.Toggle(refresh, GUIContent.Temp("Refresh"), EditorStyles.toolbarButton, GUILayout.Width(100));

            if (refresh && m_SelectedVisualTree != null)
                Refresh();

            if (m_TryAutoSelect || autoSelect)
                AutoSelectTree();
        }

        private void RefreshPanelDropdown()
        {
            RefreshVisualTrees();

            m_Labels = new GUIContent[m_VisualTrees.Count + 1];
            m_Labels[0] = new GUIContent("Select a panel");
            for (int i = 0; i < m_VisualTrees.Count; i++)
                m_Labels[i + 1] = new GUIContent(m_VisualTrees[i].name);
        }

        private void RefreshVisualTrees()
        {
            m_VisualTrees.Clear();

            List<GUIView> guiViews = new List<GUIView>();
            GUIViewDebuggerHelper.GetViews(guiViews);
            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                HostView view = guiViews.FirstOrDefault(v => v.GetInstanceID() == it.Current.Key) as HostView;
                if (view == null)
                    continue;

                // Skip this window
                if (view.actualView == this)
                    continue;

                var panel = it.Current.Value;
                var panelMode = UIRDebugUtility.GetPanelRepaintMode(panel);
                if (panelMode != RepaintMode.Standard)
                {
                    m_VisualTrees.Add(new VisualTreeDebug() { panel = panel });
                }
            }
        }

        private void AutoSelectTree()
        {
            RefreshPanelDropdown();
            if (m_VisualTrees.Count > 0)
            {
                if (!string.IsNullOrEmpty(m_LastVisualTreeName))
                {
                    // Try to retrieve last selected VisualTree
                    for (int i = 0; i < m_VisualTrees.Count; i++)
                    {
                        var vt = m_VisualTrees[i];
                        if (vt.name == m_LastVisualTreeName)
                        {
                            SelectVisualTree(vt);
                            break;
                        }
                    }
                }
                if (m_SelectedVisualTree == null || !m_VisualTrees.Contains(m_SelectedVisualTree))
                {
                    SelectVisualTree(m_VisualTrees[0]);
                }

                m_TryAutoSelect = false;
            }
            else if (m_SelectedVisualTree != null)
            {
                SelectVisualTree(null);
            }
        }

        private void SelectVisualTree(VisualTreeDebug vt)
        {
            // Detach debugger from current panel
            if (m_SelectedVisualTree != null)
                m_SelectedVisualTree.panel.panelDebug.DetachDebugger(this);

            if (vt != null)
            {
                for (int i = 0; i < m_VisualTrees.Count; i++)
                {
                    if (vt == m_VisualTrees[i])
                    {
                        vt.panel.panelDebug.AttachDebugger(this);

                        m_SelectedPanelDropDownIndex = i + 1;
                        m_SelectedVisualTree = vt;
                        m_LastVisualTreeName = vt.name;

                        OnSelectVisualTree(vt);
                        return;
                    }
                }
            }

            // No tree selected
            m_SelectedPanelDropDownIndex = 0;
            m_SelectedVisualTree = null;
            m_LastVisualTreeName = null;
            OnSelectVisualTree(null);
        }

        public virtual bool InterceptEvents(Event ev)
        {
            return false;
        }
    }
}
