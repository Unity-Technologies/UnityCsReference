// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class PreviewThemeState
{
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

    public ThemeStyleSheet SelectedTheme
    {
        get
        {
            var guid = SessionState.GetString(m_SessionKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
                return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
        }
        set
        {
            var guid = value != null
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value))
                : string.Empty;
            SessionState.SetString(m_SessionKey, guid);
        }
    }
}
