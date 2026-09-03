// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class LocIcons
{
    // USS resource() cannot pick the skin variant, so these are loaded from C# instead.
    public const string Table = "UnityEditor.HierarchyWindow";
    public const string Menu = "_Menu";
    public const string Metadata = "editicon.sml";
    public const string Search = "Search Icon";

    public static Texture2D Tex(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return EditorGUIUtility.IconContent(name)?.image as Texture2D;
    }

    public static void Apply(VisualElement element, string name)
    {
        element.style.backgroundImage = new StyleBackground(Tex(name));
    }
}
