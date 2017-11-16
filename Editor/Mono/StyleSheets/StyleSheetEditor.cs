// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    [CustomEditor(typeof(StyleSheet))]
    internal class StyleSheetEditor : Editor
    {
        private Texture2D m_FileTypeIcon;

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture("UssScript Icon");
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
