// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class GameViewSizesMenuModifyItemUI : FlexibleMenuModifyItemUI
    {
        private class Styles
        {
            public GUIContent headerAdd = new GUIContent("Add");
            public GUIContent headerEdit = new GUIContent("Edit");
            public GUIContent typeName = new GUIContent("Type");
            public GUIContent widthHeightText = new GUIContent("Width & Height");
            public GUIContent optionalText = new GUIContent("Label");
            public GUIContent ok = new GUIContent("OK");
            public GUIContent cancel = new GUIContent("Cancel");
            public GUIContent[] typeNames = new[] {new GUIContent("Aspect Ratio"), new GUIContent("Fixed Resolution")};
        }

        private static Styles s_Styles;
        private GameViewSize m_GameViewSize;

        public override void OnClose()
        {
            m_GameViewSize = null;
            base.OnClose();
        }

        override public Vector2 GetWindowSize()
        {
            return new Vector2(230, 140);
        }

        override public void OnGUI(Rect rect)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GameViewSize gameViewSizeState = m_Object as GameViewSize;
            if (gameViewSizeState == null)
            {
                Debug.LogError("Invalid object");
                return;
            }

            // We use a local gameviewsize to ensure we do not edit the original state (if user presses cancel state is not changed)
            if (m_GameViewSize == null)
                m_GameViewSize = new GameViewSize(gameViewSizeState);

            bool validSettings = m_GameViewSize.width > 0 && m_GameViewSize.height > 0;
            const float kColumnWidth = 90f;
            const float kSpacing = 10f;

            GUILayout.Space(3);
            GUILayout.Label(m_MenuType == MenuType.Add ? s_Styles.headerAdd : s_Styles.headerEdit,
                EditorStyles.boldLabel);

            Rect seperatorRect = GUILayoutUtility.GetRect(1, 1);
            FlexibleMenu.DrawRect(seperatorRect,
                (EditorGUIUtility.isProSkin)
                ? new Color(0.32f, 0.32f, 0.32f, 1.333f)
                : new Color(0.6f, 0.6f, 0.6f, 1.333f));                      // dark : light
            GUILayout.Space(4);

            // Optional text
            GUILayout.BeginHorizontal();
            GUILayout.Label(s_Styles.optionalText, GUILayout.Width(kColumnWidth));
            GUILayout.Space(kSpacing);
            m_GameViewSize.baseText = EditorGUILayout.TextField(m_GameViewSize.baseText);
            GUILayout.EndHorizontal();

            // Drop list (aspect / fixed res)
            GUILayout.BeginHorizontal();
            GUILayout.Label(s_Styles.typeName, GUILayout.Width(kColumnWidth));
            GUILayout.Space(kSpacing);
            m_GameViewSize.sizeType = (GameViewSizeType)EditorGUILayout.Popup((int)m_GameViewSize.sizeType, s_Styles.typeNames);
            GUILayout.EndHorizontal();

            // Width Height
            GUILayout.BeginHorizontal();
            GUILayout.Label(s_Styles.widthHeightText, GUILayout.Width(kColumnWidth));
            GUILayout.Space(kSpacing);
            m_GameViewSize.width = EditorGUILayout.IntField(m_GameViewSize.width);
            GUILayout.Space(5);
            m_GameViewSize.height = EditorGUILayout.IntField(m_GameViewSize.height);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // Displayed text
            float margin = 10f;
            float cropWidth = rect.width - 2 * margin;
            GUILayout.BeginHorizontal();
            GUILayout.Space(margin);
            GUILayout.FlexibleSpace();
            string displayText = m_GameViewSize.displayText;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(displayText)))
            {
                if (string.IsNullOrEmpty(displayText))
                    displayText = "Result";
                else
                    displayText = GetCroppedText(displayText, cropWidth, EditorStyles.label);
                GUILayout.Label(GUIContent.Temp(displayText), EditorStyles.label);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(margin);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // Cancel, Ok
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(s_Styles.cancel))
            {
                editorWindow.Close();
            }

            using (new EditorGUI.DisabledScope(!validSettings))
            {
                if (GUILayout.Button(s_Styles.ok))
                {
                    gameViewSizeState.Set(m_GameViewSize);
                    Accepted();
                    editorWindow.Close();
                }
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        string GetCroppedText(string fullText, float cropWidth, GUIStyle style)
        {
            // Check if we need to crop
            int characterCountVisible = style.GetNumCharactersThatFitWithinWidth(fullText, cropWidth);
            if (characterCountVisible == -1)
            {
                return fullText;
            }

            if (characterCountVisible > 1 && characterCountVisible != fullText.Length)
                return fullText.Substring(0, characterCountVisible - 1) + ("\u2026"); // 'horizontal ellipsis' (U+2026) is: ...
            else
                return fullText;
        }
    }
}

// namespace
