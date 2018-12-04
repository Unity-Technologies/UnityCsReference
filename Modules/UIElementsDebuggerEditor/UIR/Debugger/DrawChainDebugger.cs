// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements.Debugger
{
    internal class DrawChainDebugger : UIRDebugger
    {
        [SerializeField]
        private bool m_DoInspect;
        private bool m_ShowRawView;

        private TreeViewState m_TreeViewState;
        private DrawChainTreeView m_TreeView;
        private IChainItem m_SelectedItem;

        private SplitterState m_SplitterState;

        private Vector2 m_DetailScroll = Vector2.zero;
        private const float kToolbarButtonWidth = 120;

        [MenuItem("Window/Analysis/UIR Draw Chain Debugger", false, 201, true)]
        public static void Open()
        {
            GetWindow<DrawChainDebugger>().Show();
        }

        public new void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Draw Chain Debugger");
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new DrawChainTreeView(m_TreeViewState);

            if (m_SplitterState == null)
                m_SplitterState = new SplitterState(1, 2);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            OnGUIPanelSelectDropDown();

            bool expand = false;
            expand = GUILayout.Toggle(expand, GUIContent.Temp("Expand"), EditorStyles.toolbarButton, GUILayout.Width(kToolbarButtonWidth));
            bool collapse = false;
            collapse = GUILayout.Toggle(collapse, GUIContent.Temp("Collapse"), EditorStyles.toolbarButton, GUILayout.Width(kToolbarButtonWidth));
            m_DoInspect = GUILayout.Toggle(m_DoInspect, GUIContent.Temp("Inspect Chain"), EditorStyles.toolbarButton, GUILayout.Width(kToolbarButtonWidth));
            m_ShowRawView = GUILayout.Toggle(m_ShowRawView, GUIContent.Temp("Raw View"), EditorStyles.toolbarButton, GUILayout.Width(kToolbarButtonWidth));
            EditorGUILayout.EndHorizontal();

            if (m_TreeView.rendererChain == null)
                return;

            SplitterGUILayout.BeginHorizontalSplit(m_SplitterState, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            SplitterGUILayout.EndHorizontalSplit();

            float column1Width = m_SplitterState.realSizes.Length > 0 ? m_SplitterState.realSizes[0] : 150;
            float column2Width = position.width - column1Width;
            Rect column1Rect = new Rect(0, EditorGUI.kWindowToolbarHeight, column1Width, position.height - EditorGUI.kWindowToolbarHeight);
            Rect column2Rect = new Rect(column1Width, EditorGUI.kWindowToolbarHeight, column2Width, column1Rect.height);

            m_TreeView.ShowInspect(m_DoInspect);
            m_TreeView.SetRawView(m_ShowRawView);
            m_TreeView.OnGUI(column1Rect);

            if (expand)
                m_TreeView.ExpandAll();
            if (collapse)
                m_TreeView.Collapse();

            DrawSelection(column2Rect);
        }

        private void DrawSelection(Rect rect)
        {
            Event evt = Event.current;
            if (evt.type == EventType.Layout)
                CacheData();

            if (m_SelectedItem == null)
                return;

            GUILayout.BeginArea(rect);

            m_SelectedItem.DrawToolbar();

            m_DetailScroll = EditorGUILayout.BeginScrollView(m_DetailScroll);
            EditorGUILayout.LabelField(m_SelectedItem.name, Styles.KInspectorTitle);

            using (new EditorGUI.DisabledScope(Event.current.type != EventType.Repaint))
            {
                m_SelectedItem.DrawProperties();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void CacheData()
        {
            if (!m_TreeView.HasSelection())
            {
                m_SelectedItem = null;
                return;
            }

            int selectedId = m_TreeView.GetSelection().First();
            m_SelectedItem = m_TreeView.GetSelectedItem(selectedId);
        }

        // Called when renderer chain change
        public override void Refresh()
        {
            // Try to restore the selected item
            int selectedId = 0;
            bool hasSelection = m_TreeView.HasSelection();
            if (hasSelection)
                selectedId = m_TreeView.GetSelection().First();

            m_TreeView.rendererChain = UIRDebugUtility.GetUIRendererChain(m_SelectedVisualTree.panel);
            m_TreeView.uirDataChain = UIRDebugUtility.GetRootUirData(m_SelectedVisualTree.panel);
            if (m_TreeView.rendererChain != null)
            {
                var topRect = m_SelectedVisualTree.panel.visualTree.layout;
                MeshNodePreview.s_PreviewProjection = Matrix4x4.Ortho(topRect.xMin, topRect.xMax, topRect.yMax, topRect.yMin, -1, 1);
                m_TreeView.Reload();
                m_TreeView.ExpandAll();

                if (hasSelection)
                    m_SelectedItem = m_TreeView.GetSelectedItem(selectedId);
            }
        }

        protected override void OnSelectVisualTree(VisualTreeDebug vtDebug)
        {
            if (vtDebug != null)
            {
                m_TreeView.rendererChain = vtDebug.rendererChain;
                m_TreeView.uirDataChain = vtDebug.uirDataChain;
                if (m_TreeView.rendererChain != null)
                {
                    m_TreeView.Reload();
                    m_TreeView.ExpandAll();
                }
            }
            else
            {
                // No tree selected
                m_SelectedVisualTree = null;
                m_TreeView.rendererChain = null;
                m_TreeView.uirDataChain = null;
                m_TreeView.Reload();
            }
        }
    }

    internal static class Styles
    {
        public static readonly GUIStyle KSizeLabel = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        public static readonly GUIStyle KInspectorTitle = new GUIStyle(EditorStyles.whiteLargeLabel) { alignment = TextAnchor.MiddleCenter };
        public static readonly GUIContent rendererStatePropertiesContent = new GUIContent("State");
    }
}
