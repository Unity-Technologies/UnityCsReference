// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class LocIcons
{
    // USS resource() cannot pick the skin variant, so these are loaded from C# instead.
    public const string Table = "LocalizationRuntime/Icons/LocalizationTablesWindow.png";
    public const string Menu = "_Menu";
    public const string Metadata = "LocalizationRuntime/Icons/Metadata.png";
    // White artwork, so the accent tint the active state applies comes out as the accent colour itself.
    public const string MetadataOn = "LocalizationRuntime/Icons/Metadata On.png";
    public const string Search = "Search Icon";

    public static Texture2D Tex(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return EditorGUIUtility.IconContent(name)?.image as Texture2D;
    }

    // IconContent only ever looks for an @2x file, so an icon drawn larger than 16pt asks for the bigger one itself.
    public static Texture2D Glyph(string name)
    {
        var retina = EditorGUIUtility.pixelsPerPoint > 1f ? Variant(name, "@4x") : null;
        return retina ?? Variant(name, "@2x") ?? Tex(name);
    }

    static Texture2D Variant(string name, string suffix)
    {
        if (string.IsNullOrEmpty(name) || !name.EndsWith(".png", StringComparison.Ordinal))
            return null;
        var folder = name.Substring(0, name.LastIndexOf('/') + 1);
        var skin = EditorGUIUtility.isProSkin ? "d_" : string.Empty;
        var file = Path.GetFileNameWithoutExtension(name);
        return EditorGUIUtility.Load($"{folder}{skin}{file}{suffix}.png") as Texture2D;
    }

    public static Texture2D TypeIcon(Type type)
    {
        var path = type?.GetCustomAttribute<IconAttribute>(true)?.path;
        return string.IsNullOrEmpty(path) ? null : Tex(path);
    }

    public static void Apply(VisualElement element, string name)
    {
        element.style.backgroundImage = new StyleBackground(Tex(name));
    }

    public static void ApplyGlyph(VisualElement element, string name)
    {
        element.style.backgroundImage = new StyleBackground(Glyph(name));
    }
}
