// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;

// NOTE:
// This file should only contain internal functions of the EditorGUILayout class

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static bool IconButton(int id, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            s_LastRect = GUILayoutUtility.GetRect(content, style, options);
            return EditorGUI.IconButton(id, s_LastRect, content, style);
        }

        internal static void GameViewSizePopup(GameViewSizeGroupType groupType, int selectedIndex, IGameViewSizeMenuUser gameView, GUIStyle style, params GUILayoutOption[] options)
        {
            s_LastRect =  GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            EditorGUI.GameViewSizePopup(s_LastRect, groupType, selectedIndex, gameView, style);
        }

        internal static void SortingLayerField(GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            s_LastRect = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, style);
            EditorGUI.SortingLayerField(s_LastRect, label, layerID, style, labelStyle);
        }
    }
}
