// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization.Android;

/// <summary>
/// Stores the localized values that describe an Android application in its resources.
/// </summary>
/// <remarks>
/// These are the values the operating system shows while the application is not running, such as the name and icon
/// under the launcher entry. Unity reads this object after it generates the Gradle project, resolves each value in
/// every enabled locale, and writes the results as localized Android resources. Each property names the resource it
/// holds the value for. A value is resolved once per locale, so it cannot use Smart Strings; the operating system
/// reads the built resources without Unity running. Configure it before building, and reach it through
/// <see cref="Unity.Localization.LocalizationSettings.AndroidAppInfo"/>.
/// No property ever reads as null: assigning null stores an empty value instead, so a property can always be read
/// and configured without a null check.
/// </remarks>
/// <example>
/// Configure the Android application name before a build.
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AndroidAppInfoExample.cs"/>
/// </example>
/// <seealso cref="Unity.Localization.LocalizationSettings"/>
/// <seealso cref="Apple.AppInfo"/>
/// <seealso cref="Icons"/>
[Serializable]
public class AppInfo
{
    [Tooltip("The name shown under the launcher icon.\n" +
        "The app_name string resource, which the manifest's android:label references.")]
    [SerializeField] LocalizedString m_DisplayName = new();

    [Tooltip("Square launcher icons for devices from before adaptive icons.\n" +
        "The app_icon mipmap resource, which the manifest's android:icon references.")]
    [SerializeField] Icons m_LegacyIcons = new();

    [Tooltip("Circular launcher icons, for launchers that ask for a round icon.\n" +
        "The app_icon_round mipmap resource, which the manifest's android:roundIcon references.")]
    [SerializeField] Icons m_RoundIcons = new();

    [Tooltip("Layered launcher icons used from Android 8.0 onwards. A density needs both layers.")]
    [SerializeField] AdaptiveIcons m_AdaptiveIcons = new();

    /// <summary>
    /// The user-visible name of the application.
    /// </summary>
    /// <remarks>
    /// Holds the value for the <c>app_name</c> string resource, which the manifest's <c>android:label</c> attribute
    /// references.
    /// </remarks>
    public LocalizedString DisplayName
    {
        get => m_DisplayName ??= new LocalizedString();
        set => m_DisplayName = value ?? new LocalizedString();
    }

    /// <summary>
    /// The square launcher icons used by devices before adaptive icons.
    /// </summary>
    /// <remarks>
    /// Holds the icons for the <c>app_icon</c> mipmap resource, which the manifest's <c>android:icon</c> attribute
    /// references. Leaving every density empty means there is nothing to localize and the Player Settings icons stand.
    /// </remarks>
    public Icons LegacyIcons
    {
        get => m_LegacyIcons ??= new Icons();
        set => m_LegacyIcons = value ?? new Icons();
    }

    /// <summary>
    /// The circular launcher icons used by launchers that ask for a round icon.
    /// </summary>
    /// <remarks>
    /// Holds the icons for the <c>app_icon_round</c> mipmap resource, which the manifest's <c>android:roundIcon</c>
    /// attribute references. Leaving every density empty means there is nothing to localize and the Player Settings
    /// icons stand.
    /// </remarks>
    public Icons RoundIcons
    {
        get => m_RoundIcons ??= new Icons();
        set => m_RoundIcons = value ?? new Icons();
    }

    /// <summary>
    /// The layered launcher icons used from Android 8.0 onwards.
    /// </summary>
    /// <remarks>
    /// Holds a background and foreground pair per density, the two layers the operating system composes through an
    /// adaptive icon descriptor. Leaving every density empty means there is nothing to localize and the Player
    /// Settings icons stand.
    /// </remarks>
    public AdaptiveIcons AdaptiveIcons
    {
        get => m_AdaptiveIcons ??= new AdaptiveIcons();
        set => m_AdaptiveIcons = value ?? new AdaptiveIcons();
    }
}
