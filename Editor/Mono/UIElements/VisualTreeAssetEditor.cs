// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomEditor(typeof(VisualTreeAsset))]
    internal class VisualTreeAssetEditor : ScriptableObjectAssetEditor
    {
        private Panel m_Panel;
        private VisualElement m_Tree;
        private int m_LastTreeHash = -1;
        private Texture2D m_FileTypeIcon;

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture(typeof(VisualTreeAsset));
        }

        protected void OnDisable()
        {
            if (m_Panel != null)
            {
                m_Panel.Dispose();
                m_Panel = null;
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

            if (m_LastTreeHash != vta.contentHash)
            {
                m_LastTreeHash = vta.contentHash;
                m_Tree = (vta as UnityEngine.UIElements.VisualTreeAsset).CloneTree();
                m_Tree.StretchToParentSize();
                dirty = true;
            }

            if (m_Panel == null)
            {
                m_Panel = UIElementsUtility.FindOrCreatePanel(vta, ContextType.Editor);
                var visualTree = m_Panel.visualTree;
                visualTree.pseudoStates |= PseudoStates.Root;
                UIElementsEditorUtility.AddDefaultEditorStyleSheets(visualTree);
                m_Panel.allowPixelCaching = false;
                dirty = true;
            }

            if (dirty)
            {
                m_Panel.visualTree.Clear();
                m_Panel.visualTree.Add(m_Tree);
            }

            EditorGUI.DrawRect(r, EditorGUIUtility.kViewBackgroundColor);

            Rect layoutRect = GUIClip.UnclipToWindow(r);
            m_Panel.visualTree.layout = new Rect(0, 0, layoutRect.width, layoutRect.height); // We will draw relative to a viewport covering the preview area, so draw at 0,0
            m_Panel.visualTree.IncrementVersion(VersionChangeType.Repaint);

            var oldState = SavedGUIState.Create();
            int clips = GUIClip.Internal_GetCount();
            while (clips > 0)
            {
                GUIClip.Pop();
                clips--;
            }

            // Establish preview area viewport
            var pixelsPerPoint = GUIUtility.pixelsPerPoint;
            var viewportRect = new Rect(
                layoutRect.x * pixelsPerPoint, (GUIClip.visibleRect.height - layoutRect.yMax) * pixelsPerPoint,
                layoutRect.width * pixelsPerPoint, layoutRect.height * pixelsPerPoint);
            GL.Viewport(viewportRect);

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
