// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

static partial class ColorPreferences
{
    const string k_Category = "UI Viewport";
    const string SelectionOutlineColorName ="Selected Outline";
    public const string SelectionOutlineColor = k_Category + "/" + SelectionOutlineColorName;
    static readonly UIPrefColor k_SelectionOutline = new (k_Category, SelectionOutlineColorName, new Color(255.0f / 255.0f, 102.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f));
    public static Color SelectionOutline => new Color(k_SelectionOutline.Color.r, k_SelectionOutline.Color.g, k_SelectionOutline.Color.b, 1.0f);

    const string PreviewBackgroundColorName ="Preview Background";
    public const string PreviewBackgroundColor = k_Category + "/" + PreviewBackgroundColorName;
    static readonly UIPrefColor k_PreviewBackground = new (k_Category, PreviewBackgroundColorName, new Color(138.0f / 255.0f, 217.0f / 255.0f, 255.0f / 255.0f, 80.0f / 255.0f));
    public static Color PreviewBackground => k_PreviewBackground.Color;

    [OnCodeInitializing]
    static void Init()
    {
        // Intentionally left empty to trigger the `PrefColor` registration.
    }
}
