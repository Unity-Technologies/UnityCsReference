// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Accessibility;

/// <summary>
/// Access point to system accessibility settings on the user's device and to events that trigger when the user changes
/// these settings.
/// </summary>
/// <remarks>
/// <para>
/// These APIs provide information about the user's accessibility preferences and can be used to adapt an application's
/// UI according to the accessibility settings on the user's device. Their values are read-only and are updated when the
/// user changes them in the system settings.
/// </para>
/// <para>
/// These APIs are currently supported on the following platforms:
///
///- <see cref="RuntimePlatform.Android"/>
///- <see cref="RuntimePlatform.IPhonePlayer"/>
/// </para>
/// <para>
/// SA:
///
///- [[wiki:accessibility|Accessibility for mobile applications]]
///- [Sample project using the accessibility APIs](https://github.com/Unity-Technologies/a11y-public-sample)
/// </para>
/// </remarks>
public static partial class AccessibilitySettings
{
    /// <summary>
    /// Event invoked on the main thread when the user changes the font scale in the system settings.
    /// </summary>
    public static event Action<float> fontScaleChanged;

    /// <summary>
    /// Event invoked on the main thread when the user changes the bold text setting in the system settings.
    /// </summary>
    /// <remarks>
    /// **Platform support**: This event is only triggered on iOS. On Android, this event is not necessary because the
    /// application restarts when the user changes the bold text setting.
    /// </remarks>
    public static event Action<bool> boldTextStatusChanged;

    /// <summary>
    /// Event invoked on the main thread when the user changes the closed captioning setting in the system settings.
    /// </summary>
    public static event Action<bool> closedCaptioningStatusChanged;

    /// <summary>
    /// The font scale set by the user in the system settings.
    /// </summary>
    public static float fontScale => GetFontScale();

    /// <summary>
    /// Whether bold text is enabled in the system settings.
    /// </summary>
    /// <remarks>
    /// **Platform support**: On Android, this property is only set starting with Android 12 (API level 31). It might
    /// not be supported on non-stock Android. Here, non-stock refers to versions of Android that have been modified by
    /// the device manufacturer.
    /// </remarks>
    public static bool isBoldTextEnabled => IsBoldTextEnabled();

    /// <summary>
    /// Whether closed captioning is enabled in the system settings.
    /// </summary>
    public static bool isClosedCaptioningEnabled => IsClosedCaptioningEnabled();

    internal static void InvokeFontScaleChanged(float newFontScale)
    {
        fontScaleChanged?.Invoke(newFontScale);
    }

    internal static void InvokeBoldTextStatusChanged(bool enabled)
    {
        boldTextStatusChanged?.Invoke(enabled);
    }

    internal static void InvokeClosedCaptionStatusChanged(bool enabled)
    {
        closedCaptioningStatusChanged?.Invoke(enabled);
    }
}
