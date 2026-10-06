// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization.Apple;

/// <summary>
/// Stores the localized values that describe an Apple application in its property list.
/// </summary>
/// <remarks>
/// These are the strings the operating system shows while the application is not running, such as the name under the
/// icon and the permission prompts. Unity reads this object when it builds for an Apple platform, resolves each
/// value in every enabled locale, and hands the results to the installed Apple platform support, which writes them
/// into the generated Xcode project. Without that platform support the values cannot be written and the build says
/// so. Each property names the property list key it holds the value for. A value is resolved once per locale, so it
/// cannot use Smart Strings; the operating system reads the built files without Unity running. Configure it before
/// building, and reach it through <see cref="Unity.Localization.LocalizationSettings.AppleAppInfo"/>.
/// No property ever reads as null: assigning null stores an empty value instead, so a property can always be read
/// and configured without a null check.
/// </remarks>
/// <example>
/// Configure the Apple application name and permission prompts before a build.
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AppleAppInfoExample.cs"/>
/// </example>
/// <seealso cref="Unity.Localization.LocalizationSettings"/>
/// <seealso cref="Android.AppInfo"/>
/// <seealso cref="Unity.Localization.LocalizedString"/>
[Serializable]
public class AppInfo
{
    [Tooltip("The short name shown under the icon and in menus, up to 15 characters.\n" +
        "CFBundleName in the property list.")]
    [SerializeField] LocalizedString m_ShortName = new();

    [Tooltip("The full name, for when it is longer than the 15 characters Short Name allows.\n" +
        "CFBundleDisplayName in the property list.")]
    [SerializeField] LocalizedString m_DisplayName = new();

    [Tooltip("Why the application needs the camera, shown in the permission prompt.\n" +
        "NSCameraUsageDescription in the property list.")]
    [SerializeField] LocalizedString m_CameraUsageDescription = new();

    [Tooltip("Why the application needs the microphone, shown in the permission prompt.\n" +
        "NSMicrophoneUsageDescription in the property list.")]
    [SerializeField] LocalizedString m_MicrophoneUsageDescription = new();

    [Tooltip("Why the application needs the device location while it is in the foreground.\n" +
        "NSLocationWhenInUseUsageDescription in the property list.")]
    [SerializeField] LocalizedString m_LocationUsageDescription = new();

    [Tooltip("Why the application needs to track the user or device, shown in the tracking prompt.\n" +
        "NSUserTrackingUsageDescription in the property list.")]
    [SerializeField] LocalizedString m_UserTrackingUsageDescription = new();

    /// <summary>
    /// The short user-visible name of the application.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>CFBundleName</c> property list key. Apple limits this name to 15 characters, so
    /// prefer <see cref="DisplayName"/> for anything longer.
    /// </remarks>
    public LocalizedString ShortName
    {
        get => m_ShortName ??= new LocalizedString();
        set => m_ShortName = value ?? new LocalizedString();
    }

    /// <summary>
    /// The full user-visible name of the application.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>CFBundleDisplayName</c> property list key. Use it when the name is longer than the
    /// 15 characters <see cref="ShortName"/> allows.
    /// </remarks>
    public LocalizedString DisplayName
    {
        get => m_DisplayName ??= new LocalizedString();
        set => m_DisplayName = value ?? new LocalizedString();
    }

    /// <summary>
    /// The reason the application asks for access to the camera.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>NSCameraUsageDescription</c> property list key, which the operating system shows in
    /// the permission prompt.
    /// </remarks>
    public LocalizedString CameraUsageDescription
    {
        get => m_CameraUsageDescription ??= new LocalizedString();
        set => m_CameraUsageDescription = value ?? new LocalizedString();
    }

    /// <summary>
    /// The reason the application asks for access to the microphone.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>NSMicrophoneUsageDescription</c> property list key, which the operating system shows
    /// in the permission prompt.
    /// </remarks>
    public LocalizedString MicrophoneUsageDescription
    {
        get => m_MicrophoneUsageDescription ??= new LocalizedString();
        set => m_MicrophoneUsageDescription = value ?? new LocalizedString();
    }

    /// <summary>
    /// The reason the application asks for the device location while it is in the foreground.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>NSLocationWhenInUseUsageDescription</c> property list key. macOS has no Player
    /// Settings field for this key, unlike the camera and microphone descriptions.
    /// </remarks>
    public LocalizedString LocationUsageDescription
    {
        get => m_LocationUsageDescription ??= new LocalizedString();
        set => m_LocationUsageDescription = value ?? new LocalizedString();
    }

    /// <summary>
    /// The reason the application asks to track the user or the device.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>NSUserTrackingUsageDescription</c> property list key, which the operating system
    /// shows in the App Tracking Transparency prompt.
    /// </remarks>
    public LocalizedString UserTrackingUsageDescription
    {
        get => m_UserTrackingUsageDescription ??= new LocalizedString();
        set => m_UserTrackingUsageDescription = value ?? new LocalizedString();
    }
}
