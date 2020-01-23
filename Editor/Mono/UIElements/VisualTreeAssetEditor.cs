// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [CustomEditor(typeof(VisualTreeAsset))]
    internal class VisualTreeAssetEditor : ScriptableObjectAssetEditor
    {
        private Panel m_Panel;
        private VisualElement m_Tree;
        private VisualTreeAsset m_LastTree;
        protected Texture2D m_FileTypeIcon;

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

        protected void OnDestroy()
        {
            if (m_LastTree != null)
            {
                UIElementsUtility.RemoveCachedPanel(m_LastTree.GetInstanceID());
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        protected void RenderIcon(Rect iconRect)
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
                m_Tree = (vta as UnityEngine.UIElements.VisualTreeAsset).Instantiate();
                m_Tree.StretchToParentSize();
                dirty = true;
            }

            if (m_Panel == null)
            {
                m_Panel = UIElementsUtility.FindOrCreateEditorPanel(m_LastTree);
                var visualTree = m_Panel.visualTree;
                visualTree.pseudoStates |= PseudoStates.Root;
                UIElementsEditorUtility.AddDefaultEditorStyleSheets(visualTree);
                dirty = true;
            }

            if (dirty)
            {
                m_Panel.visualTree.Clear();
                m_Panel.visualTree.Add(m_Tree);
            }

            EditorGUI.DrawRect(r, EditorGUIUtility.kViewBackgroundColor);

            var dpiScale = EditorGUIUtility.pixelsPerPoint;
            var viewportRect = GUIClip.UnclipToWindow(r); // Still in points, not pixels
            var viewportRectPx = new Rect(viewportRect.min * dpiScale, viewportRect.size * dpiScale);
            m_Panel.visualTree.SetSize(viewportRect.size); // We will draw relative to a viewport covering the preview area, so draw at 0,0
            m_Panel.visualTree.IncrementVersion(VersionChangeType.Repaint);

            var oldState = SavedGUIState.Create();
            int clips = GUIClip.Internal_GetCount();
            while (clips > 0)
            {
                GUIClip.Pop();
                clips--;
            }

            var oldClearFlags = m_Panel.clearFlags;
            m_Panel.clearFlags = PanelClearFlags.None;
            RectInt oldViewport = UnityEngine.UIElements.UIR.Utility.GetActiveViewport();
            GL.Viewport(new Rect(new Vector2(viewportRectPx.xMin, Screen.height - viewportRectPx.yMin - viewportRectPx.height), viewportRectPx.size));

            m_Panel.Repaint(Event.current);

            GL.Viewport(new Rect(oldViewport.xMin, oldViewport.yMin, oldViewport.width, oldViewport.height));
            oldState.ApplyAndForget();
            m_Panel.clearFlags = oldClearFlags;
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
