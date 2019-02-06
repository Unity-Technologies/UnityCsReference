// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor.TextCore
{
    internal static class TextCoreEditorStyles
    {
        public static GUIStyle sectionHeader;

        public static GUIStyle label;
        public static GUIStyle leftLabel;
        public static GUIStyle centeredLabel;
        public static GUIStyle rightLabel;

        static TextCoreEditorStyles()
        {
            if (EditorGUIUtility.isProSkin)
            {
                // Section Header GUI Style
                sectionHeader = new GUIStyle(EditorStyles.textArea) { fixedHeight = 22, richText = true, overflow = new RectOffset(9, 0, 0, 0), padding = new RectOffset(0, 0, 4, 0) };
                Texture2D sectionHeaderTexture = new Texture2D(1, 1);
                sectionHeaderTexture.SetPixel(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.5f));
                sectionHeaderTexture.Apply();
                sectionHeader.normal.background = sectionHeaderTexture;
            }
            else
            {
                // Section Header GUI Style
                sectionHeader = new GUIStyle(EditorStyles.textArea) { fixedHeight = 22, richText = true, overflow = new RectOffset(9, 0, 0, 0), padding = new RectOffset(0, 0, 4, 0) };
                Texture2D sectionHeaderTexture = new Texture2D(1, 1);
                sectionHeaderTexture.SetPixel(1, 1, new Color(0.6f, 0.6f, 0.6f, 0.5f));
                sectionHeaderTexture.Apply();
                sectionHeader.normal.background = sectionHeaderTexture;
            }

            // Labels
            label = leftLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, richText = true };
            centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, richText = true };
            rightLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, richText = true };
        }
    }
}
