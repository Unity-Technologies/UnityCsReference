// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.StyleSheets;
using UnityEngine;

namespace UnityEditor
{
    internal class GameViewSizesMenuModifyItemUI : FlexibleMenuModifyItemUI
    {
        private static class Styles
        {
            public static GUIContent headerAdd = EditorGUIUtility.TrTextContent("Add");
            public static GUIContent headerEdit = EditorGUIUtility.TrTextContent("Edit");
            public static GUIContent typeName = EditorGUIUtility.TrTextContent("Type");
            public static GUIContent widthHeightText = EditorGUIUtility.TrTextContent("Width & Height");
            public static GUIContent optionalText = EditorGUIUtility.TrTextContent("Label");
            public static GUIContent ok = EditorGUIUtility.TrTextContent("OK");
            public static GUIContent cancel = EditorGUIUtility.TrTextContent("Cancel");
            public static GUIContent[] typeNames = new[] {EditorGUIUtility.TrTextContent("Aspect Ratio"), EditorGUIUtility.TrTextContent("Fixed Resolution")};

            public static SVC<float> windowWidth = new SVC<float>("GameView", "--sizes-menu-modify-item-window-width", 230f);
            public static SVC<float> windowHeight = new SVC<float>("GameView", "--sizes-menu-modify-item-window-height", 140f);
            public static SVC<float> windowBottomPadding = new SVC<float>("GameView", "--window-bottom-padding");
            public static SVC<float> spaceBetweenOkCancelButtons = new SVC<float>("GameView", "--space-between-ok-cancel-buttons", 10f);
        }

        private GameViewSize m_GameViewSize;

        public override void OnClose()
        {
            m_GameViewSize = null;
            base.OnClose();
        }

        override public Vector2 GetWindowSize()
        {
            return new Vector2(Styles.windowWidth, Styles.windowHeight);
        }

        override public void OnGUI(Rect rect)
        {
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
            GUILayout.Label(m_MenuType == MenuType.Add ? Styles.headerAdd : Styles.headerEdit,
                EditorStyles.boldLabel);

            Rect seperatorRect = GUILayoutUtility.GetRect(1, 1);
            FlexibleMenu.DrawRect(seperatorRect,
                (EditorGUIUtility.isProSkin)
                ? new Color(0.32f, 0.32f, 0.32f, 1.333f)
                : new Color(0.6f, 0.6f, 0.6f, 1.333f));                      // dark : light
            GUILayout.Space(4);

            EditorGUIUtility.labelWidth = kSpacing + kColumnWidth;

            // Optional text
            m_GameViewSize.baseText = EditorGUILayout.TextField(Styles.optionalText, m_GameViewSize.baseText);

            // Drop list (aspect / fixed res)
            m_GameViewSize.sizeType = (GameViewSizeType)EditorGUILayout.Popup(Styles.typeName, (int)m_GameViewSize.sizeType, Styles.typeNames);

            var wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Styles.widthHeightText);
            var widthAndHeight = new Vector2Int(m_GameViewSize.width, m_GameViewSize.height);
            widthAndHeight = EditorGUILayout.Vector2IntField(GUIContent.none, widthAndHeight, GUILayout.MaxWidth(Styles.windowWidth - EditorGUIUtility.labelWidth - EditorStyles.textField.padding.horizontal));
            m_GameViewSize.width = widthAndHeight.x;
            m_GameViewSize.height = widthAndHeight.y;
            EditorGUIUtility.wideMode = wideMode;
            GUILayout.EndHorizontal();

            GUILayout.Space(Styles.spaceBetweenOkCancelButtons);

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

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(5f);

            // Cancel, Ok
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(Styles.cancel))
            {
                editorWindow.Close();
            }

            using (new EditorGUI.DisabledScope(!validSettings))
            {
                if (GUILayout.Button(Styles.ok))
                {
                    gameViewSizeState.Set(m_GameViewSize);
                    Accepted();
                    editorWindow.Close();
                }
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(Styles.windowBottomPadding);
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
