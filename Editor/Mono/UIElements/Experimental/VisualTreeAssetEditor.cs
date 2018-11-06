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
    internal class VisualTreeAssetEditor : ScriptableObjectAssetEditor
    {
        private Panel m_Panel;
        private VisualElement m_Tree;
        private VisualTreeAsset m_LastTree;
        private Texture2D m_FileTypeIcon;

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture(typeof(VisualTreeAsset));
        }

        protected void OnDestroy()
        {
            m_Panel = null;
            if (m_LastTree != null)
            {
                UIElementsUtility.RemoveCachedPanel(m_LastTree.GetInstanceID());
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        private void RenderIcon(Rect iconRect)
        {
            Debug.Assert(m_FileTypeIcon != null);
            GUI.DrawTexture(iconRect, m_FileTypeIcon, ScaleMode.ScaleToFit);
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
                    UIElementsEditorUtility.AddDefaultEditorStyleSheets(m_Panel.visualTree);
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

            EditorGUI.DrawRect(r, EditorGUIUtility.kViewBackgroundColor);

            m_Panel.visualTree.layout = GUIClip.UnclipToWindow(r);
            m_Panel.visualTree.IncrementVersion(VersionChangeType.Repaint);

            var oldState = SavedGUIState.Create();
            int clips = GUIClip.Internal_GetCount();
            while (clips > 0)
            {
                GUIClip.Pop();
                clips--;
            }

            m_Panel.Repaint(Event.current);

            oldState.ApplyAndForget();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            const int k_IconSize = 64;

            base.OnPreviewGUI(r, background);
            if (r.width > k_IconSize || r.height > k_IconSize)
                Render(target as VisualTreeAsset, r, background);
            else
                RenderIcon(r);
        }
    }
}
