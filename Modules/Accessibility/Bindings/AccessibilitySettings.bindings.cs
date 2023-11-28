// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility;

[NativeHeader("Modules/Accessibility/Native/AccessibilitySettings.h")]
public static partial class AccessibilitySettings
{
    /// <summary>
    /// Gets the font scale set by the user in the system settings.
    /// </summary>
    static extern float GetFontScale();

    /// <summary>
    /// Checks whether or not bold text is enabled in the system settings.
    /// </summary>
    static extern bool IsBoldTextEnabled();

    /// <summary>
    /// Checks whether or not closed captioning is enabled in the system
    /// settings.
    /// </summary>
    static extern bool IsClosedCaptioningEnabled();

    [RequiredByNativeCode]
    static void Internal_OnFontScaleChanged(float newFontScale)
    {
        AccessibilityManager.QueueNotification(new AccessibilityManager.NotificationContext
        {
            notification = AccessibilityNotification.FontScaleChanged,
            fontScale = newFontScale,
        });
    }

    [RequiredByNativeCode]
    static void Internal_OnBoldTextStatusChanged(bool enabled)
    {
        AccessibilityManager.QueueNotification(new AccessibilityManager.NotificationContext
        {
            notification = AccessibilityNotification.BoldTextStatusChanged,
            isBoldTextEnabled = enabled,
        });
    }

    [RequiredByNativeCode]
    static void Internal_OnClosedCaptioningStatusChanged(bool enabled)
    {
        AccessibilityManager.QueueNotification(new AccessibilityManager.NotificationContext
        {
            notification = AccessibilityNotification.ClosedCaptioningStatusChanged,
            isClosedCaptioningEnabled = enabled,
        });
    }
}
