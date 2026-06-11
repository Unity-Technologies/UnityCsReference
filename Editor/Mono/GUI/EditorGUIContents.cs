// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        // Common GUIContents used for EditorGUI controls.
        internal sealed class GUIContents
        {
            // The settings dropdown icon top right in a component
            static GUIContent s_TitleSettingsIcon;
            internal static GUIContent titleSettingsIcon => s_TitleSettingsIcon ??= EditorGUIUtility.IconContent("_Popup");

            // The help icon in a component
            static GUIContent s_HelpIcon;
            internal static GUIContent helpIcon => s_HelpIcon ??= EditorGUIUtility.IconContent("_Help");
        }
    }
}
