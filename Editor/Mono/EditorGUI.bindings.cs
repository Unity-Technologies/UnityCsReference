// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngineInternal;

namespace UnityEditor
{
    public partial class EditorGUI
    {
        internal static int Popup(Rect position, GUIContent label, int selectedIndex, string[] displayedOptions, GUIStyle style)
        { return PopupInternal(position, label, selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style); }
        internal static int Popup(Rect position, GUIContent label, int selectedIndex, string[] displayedOptions)
        { return Popup(position, label, selectedIndex, displayedOptions, EditorStyles.popup); }
    }
}
