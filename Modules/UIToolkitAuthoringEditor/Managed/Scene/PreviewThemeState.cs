// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class PreviewThemeState
{
    // Built-in editor themes have no usable asset GUID; they are stored under these keys instead.
    internal const string EditorThemeKeyPrefix = "editor-theme:";

    readonly string m_SessionKey;

    public PreviewThemeState(string sessionKey)
    {
        m_SessionKey = sessionKey;
    }

    internal static PreviewThemeState ForDocument(VisualTreeAsset document)
    {
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(document));
        return new PreviewThemeState($"UIToolkitAuthoring.PreviewTheme.{guid}");
    }

    internal void Clear() => SessionState.EraseString(m_SessionKey);

    public StyleSheet SelectedTheme
    {
        get => FromKey(SessionState.GetString(m_SessionKey, string.Empty));
        set => SessionState.SetString(m_SessionKey, ToKey(value));
    }

    /// <summary>
    /// The theme chosen for a document previewed in <paramref name="contextType"/>. A theme chosen for the other
    /// context type, before the document switched, counts as none.
    /// </summary>
    public StyleSheet GetSelectedTheme(ContextType contextType)
    {
        var theme = SelectedTheme;
        return theme != null && ThemeUtility.GetContextTypeForTheme(theme) == contextType ? theme : null;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static string ToKey(StyleSheet theme)
    {
        if (theme == null)
            return string.Empty;
        if (ThemeUtility.TryGetEditorCanvasTheme(theme, out var editorTheme))
            return EditorThemeKeyPrefix + editorTheme;
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(theme));
    }

    internal static StyleSheet FromKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (key.StartsWith(EditorThemeKeyPrefix, StringComparison.Ordinal))
        {
            return Enum.TryParse<CanvasTheme>(key.Substring(EditorThemeKeyPrefix.Length), out var editorTheme) &&
                ThemeUtility.IsEditorCanvasTheme(editorTheme)
                ? ThemeUtility.GetStyleSheetForTheme(editorTheme, null)
                : null;
        }

        var path = AssetDatabase.GUIDToAssetPath(key);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
    }
}
