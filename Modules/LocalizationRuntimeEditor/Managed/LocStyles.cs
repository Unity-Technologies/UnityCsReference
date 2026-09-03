// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class LocStyles
{
    const string k_Theme = "LocalizationRuntime/StyleSheets/LocalizationTheme.uss";

    public static void Apply(VisualElement root, string viewStyleSheet = null)
    {
        if (EditorGUIUtility.LoadRequired(k_Theme) is StyleSheet theme)
            root.styleSheets.Add(theme);
        if (!string.IsNullOrEmpty(viewStyleSheet) && EditorGUIUtility.LoadRequired(viewStyleSheet) is StyleSheet sheet)
            root.styleSheets.Add(sheet);
    }
}
