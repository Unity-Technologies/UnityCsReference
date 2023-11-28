// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Accessibility;

/// <summary>
/// Provides access to the accessibility settings for the current platform.
/// </summary>
/// <remarks>
/// The currently supported platforms are:
///
///- <see cref="RuntimePlatform.Android"/>
///- <see cref="RuntimePlatform.IPhonePlayer"/>
///
/// This class also provides events that are invoked when the user changes accessibility settings.
/// </remarks>
public static partial class AccessibilitySettings
{
    /// <summary>
    /// Gets the font scale set by the user in the system settings.
    /// </summary>
    /// <remarks>
    /// For all the supported platforms, refer to
    /// <see cref="AccessibilitySettings"/>.
    /// </remarks>
    public static float fontScale => GetFontScale();

    /// <summary>
    /// Checks whether or not bold text is enabled in the system settings.
    /// </summary>
    /// <remarks>
    /// For all the supported platforms, refer to
    /// <see cref="AccessibilitySettings"/>.
    ///
    /// This requires at least Android 12 (API level 31). However, it might not
    /// be supported on non-stock Android 12. Here, non-stock refers to versions
    /// of Android that have been modified by the device manufacturer.
    /// </remarks>
    public static bool isBoldTextEnabled => IsBoldTextEnabled();

    /// <summary>
    /// Checks whether or not closed captioning is enabled in the system
    /// settings.
    /// </summary>
    /// <remarks>
    /// For all the supported platforms, refer to
    /// <see cref="AccessibilitySettings"/>.
    /// </remarks>
    public static bool isClosedCaptioningEnabled => IsClosedCaptioningEnabled();

    /// <summary>
    /// Event that is invoked on the main thread when the user changes the
    /// font scale in the system settings.
    /// </summary>
    /// <remarks>
    /// For all the supported platforms, refer to
    /// <see cref="AccessibilitySettings"/>.
    /// </remarks>
    public static event Action<float> fontScaleChanged;

    internal static void InvokeFontScaleChanged(float newFontScale)
    {
        fontScaleChanged?.Invoke(newFontScale);
    }

    /// <summary>
    /// Event that is invoked on the main thread when the user changes the
    /// bold text setting in the system settings.
    /// </summary>
    /// <remarks>
    /// This is only supported on iOS. On Android, the app restarts when the
    /// user changes the bold text setting in the system settings, so this event
    /// is not necessary.
    /// </remarks>
    public static event Action<bool> boldTextStatusChanged;

    internal static void InvokeBoldTextStatusChanged(bool enabled)
    {
        boldTextStatusChanged?.Invoke(enabled);
    }

    /// <summary>
    /// Event that is invoked on the main thread when the user changes the
    /// closed captioning setting in the system settings.
    /// </summary>
    /// <remarks>
    /// For all the supported platforms, refer to
    /// <see cref="AccessibilitySettings"/>.
    /// </remarks>
    public static event Action<bool> closedCaptioningStatusChanged;

    internal static void InvokeClosedCaptionStatusChanged(bool enabled)
    {
        closedCaptioningStatusChanged?.Invoke(enabled);
    }
}
