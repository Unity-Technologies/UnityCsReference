// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class ColliderEditorUtility
    {
        private static GUIStyle s_EditColliderButtonStyle;
        private const float k_EditColliderbuttonWidth = 22;
        private const float k_EditColliderbuttonHeight = 22;
        private const float k_SpaceBetweenLabelAndButton = 5;

        public static bool InspectorEditButtonGUI(bool editing)
        {
            if (s_EditColliderButtonStyle == null)
            {
                s_EditColliderButtonStyle = new GUIStyle("Button");
                s_EditColliderButtonStyle.padding = new RectOffset(0, 0, 0, 0);
                s_EditColliderButtonStyle.margin = new RectOffset(0, 0, 0, 0);
            }

            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.GetControlRect(true, k_EditColliderbuttonHeight);
            Rect buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, k_EditColliderbuttonWidth, k_EditColliderbuttonHeight);

            GUIContent labelContent = new GUIContent("Edit Collider");
            Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);

            Rect labelRect = new Rect(
                    buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                    rect.yMin + (rect.height - labelSize.y) * .5f,
                    labelSize.x,
                    rect.height);

            GUILayout.Space(2f);
            bool newValue = GUI.Toggle(buttonRect, editing, EditorGUIUtility.IconContent("EditCollider"), s_EditColliderButtonStyle);
            GUI.Label(labelRect, "Edit Collider");
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            return newValue;
        }
    }
}
