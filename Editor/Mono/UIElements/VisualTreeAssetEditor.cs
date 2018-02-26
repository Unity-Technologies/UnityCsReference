// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    [CustomEditor(typeof(VisualTreeAsset))]
    internal class VisualTreeAssetEditor : Editor
    {
        private Panel m_Panel;
        private VisualElement m_Tree;
        private VisualTreeAsset m_LastTree;

        protected void OnDestroy()
        {
            m_Panel = null;
            UIElementsUtility.RemoveCachedPanel(m_LastTree.GetInstanceID());
        }

        // hack to avoid null references when a scriptedImporter runs and replaces the current selection
        internal override string targetTitle
        {
            get
            {
                if (!target)
                {
                    serializedObject.Update();
                    InternalSetTargets(serializedObject.targetObjects);
                }
                return base.targetTitle;
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return GUIContent.Temp(targetTitle);
        }

        public void Render(VisualTreeAsset vta, Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint || r.width < 100 && r.height < 100)
                return;

            bool dirty = false;
            if (vta != m_LastTree || !m_LastTree)
            {
                m_LastTree = vta;
                m_Tree = vta.CloneTree(null);
                m_Tree.StretchToParentSize();
                dirty = true;
            }

            if (m_Panel == null)
            {
                m_Panel = UIElementsUtility.FindOrCreatePanel(m_LastTree, ContextType.Editor, new DataWatchService());
                if (m_Panel.visualTree.styleSheets == null)
                {
                    GUIView.AddDefaultEditorStyleSheets(m_Panel.visualTree);
                    m_Panel.visualTree.LoadStyleSheetsFromPaths();
                }
                m_Panel.allowPixelCaching = false;
                dirty = true;
            }

            if (dirty)
            {
                m_Panel.visualTree.Clear();
                m_Panel.visualTree.Add(m_Tree);
            }

            m_Panel.visualTree.layout = r;

            m_Panel.visualTree.Dirty(ChangeType.Layout);
            m_Panel.visualTree.Dirty(ChangeType.Repaint);

            var oldClipMatrix = GUIClip.GetMatrix();
            var oldClipRect = GUIClip.GetTopRect();
            EditorGUI.DrawRect(r, EditorGUIUtility.isProSkin ? EditorGUIUtility.kDarkViewBackground : HostView.kViewColor);
            m_Panel.Repaint(Event.current);
            GUIClip.SetTransform(oldClipMatrix, oldClipRect);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
            Render(target as VisualTreeAsset, r, background);
        }
    }
}
