// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

static partial class ColorPreferences
{
    public const string SelectionOutlineColor ="UI Viewport/Selected Outline";
    static readonly PrefColor k_SelectionOutline = new PrefColor(SelectionOutlineColor, 255.0f / 255.0f, 102.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
    public static Color SelectionOutline => new Color(k_SelectionOutline.Color.r, k_SelectionOutline.Color.g, k_SelectionOutline.Color.b, 1.0f);

    public const string PreviewBackgroundColor ="UI Viewport/Preview Background";
    static readonly PrefColor k_PreviewBackground = new PrefColor(PreviewBackgroundColor, 138.0f / 255.0f, 217.0f / 255.0f, 255.0f / 255.0f, 80.0f / 255.0f);
    public static Color PreviewBackground => k_PreviewBackground.Color;

    [OnCodeInitializing]
    static void Init()
    {
        // Intentionally left empty to trigger the `PrefColor` registration.
    }
}
