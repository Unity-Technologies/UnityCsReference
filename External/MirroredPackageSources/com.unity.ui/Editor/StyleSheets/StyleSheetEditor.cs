using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.StyleSheets
{
    [CustomEditor(typeof(StyleSheet))]
    internal class StyleSheetEditor : ScriptableObjectAssetEditor
    {
        private Texture2D m_FileTypeIcon;

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture(typeof(StyleSheet));
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

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            const int k_IconSize = 64;

            base.OnPreviewGUI(r, background);
            if (r.width > k_IconSize || r.height > k_IconSize)
                base.OnPreviewGUI(r, background);
            else
                RenderIcon(r);
        }
    }
}
