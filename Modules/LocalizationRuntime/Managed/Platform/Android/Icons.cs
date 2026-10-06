// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization.Android;

/// <summary>
/// Identifies the screen density an Android launcher icon is drawn for.
/// </summary>
/// <remarks>
/// Android keeps one icon per density and picks the closest match for the device. The values are ordered from the
/// lowest density to the highest, and each maps to the mipmap resource folder suffix of the same name. Use them with
/// <see cref="Icons.GetIcon"/> to read or set a single density.
/// </remarks>
/// <example>
/// Assign a localized icon for one density.
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AndroidIconsExample.cs"/>
/// </example>
/// <seealso cref="Icons"/>
/// <seealso cref="AdaptiveIcons"/>
public enum IconDensity
{
    /// <summary>
    /// Low density screens, the mipmap-ldpi qualifier.
    /// </summary>
    Low,

    /// <summary>
    /// Medium density screens, the mipmap-mdpi qualifier.
    /// </summary>
    Medium,

    /// <summary>
    /// High density screens, the mipmap-hdpi qualifier.
    /// </summary>
    High,

    /// <summary>
    /// Extra high density screens, the mipmap-xhdpi qualifier.
    /// </summary>
    ExtraHigh,

    /// <summary>
    /// Twice extra high density screens, the mipmap-xxhdpi qualifier.
    /// </summary>
    ExtraExtraHigh,

    /// <summary>
    /// Three times extra high density screens, the mipmap-xxxhdpi qualifier.
    /// </summary>
    ExtraExtraExtraHigh
}

/// <summary>
/// Holds one localized Android launcher icon per screen density.
/// </summary>
/// <remarks>
/// Android keeps a separate icon per screen density and picks the closest match for the device, so this type holds one
/// texture for each. Assign every density: Android matches the locale before the density, so a locale that supplies
/// only some densities still wins for the rest and the device scales the wrong size instead of taking the icon from
/// the Player Settings. Unity writes a locale only when all of its densities resolve, and reports the ones missing.
/// Leaving the whole set empty is the way to keep the Player Settings icons for a locale.
/// </remarks>
/// <example>
/// Assign a localized icon for one density.
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AndroidIconsExample.cs"/>
/// </example>
/// <seealso cref="AppInfo"/>
/// <seealso cref="IconDensity"/>
/// <seealso cref="Unity.Localization.LocalizedTexture"/>
[Serializable]
public class Icons
{
    [Tooltip("Icon for low density screens (mipmap-ldpi).")]
    [SerializeField] LocalizedTexture m_Low = new();

    [Tooltip("Icon for medium density screens (mipmap-mdpi).")]
    [SerializeField] LocalizedTexture m_Medium = new();

    [Tooltip("Icon for high density screens (mipmap-hdpi).")]
    [SerializeField] LocalizedTexture m_High = new();

    [Tooltip("Icon for extra high density screens (mipmap-xhdpi).")]
    [SerializeField] LocalizedTexture m_ExtraHigh = new();

    [Tooltip("Icon for twice extra high density screens (mipmap-xxhdpi).")]
    [SerializeField] LocalizedTexture m_ExtraExtraHigh = new();

    [Tooltip("Icon for three times extra high density screens (mipmap-xxxhdpi).")]
    [SerializeField] LocalizedTexture m_ExtraExtraExtraHigh = new();

    /// <summary>
    /// Returns the icon assigned for a density.
    /// </summary>
    /// <remarks>
    /// The returned object is the stored one, so assigning to its properties changes what this type holds. Every
    /// density has an icon object from the moment this type is created; an unassigned one reports
    /// <see cref="Unity.Localization.LocalizedReference.IsEmpty"/> as true.
    /// </remarks>
    /// <param name="density">The density to read.</param>
    /// <returns>The icon for that density.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="density"/> is not a declared value.</exception>
    /// <example>
    /// Assign a localized icon for one density.
    /// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AndroidIconsExample.cs"/>
    /// </example>
    /// <seealso cref="IconDensity"/>
    public LocalizedTexture GetIcon(IconDensity density) => density switch
    {
        IconDensity.Low => m_Low ??= new(),
        IconDensity.Medium => m_Medium ??= new(),
        IconDensity.High => m_High ??= new(),
        IconDensity.ExtraHigh => m_ExtraHigh ??= new(),
        IconDensity.ExtraExtraHigh => m_ExtraExtraHigh ??= new(),
        IconDensity.ExtraExtraExtraHigh => m_ExtraExtraExtraHigh ??= new(),
        _ => throw new ArgumentOutOfRangeException(nameof(density), density, null)
    };

    /// <summary>
    /// Reports whether no density has an icon assigned.
    /// </summary>
    /// <remarks>
    /// Use it to tell an unconfigured icon set from a partly configured one, for example to decide whether there is
    /// anything to localize.
    /// </remarks>
    /// <returns>True when every density is empty, otherwise false.</returns>
    public bool IsEmpty()
    {
        foreach (var density in Densities)
        {
            if (!GetIcon(density).IsEmpty)
                return false;
        }
        return true;
    }

    internal static readonly IconDensity[] Densities =
    {
        IconDensity.Low,
        IconDensity.Medium,
        IconDensity.High,
        IconDensity.ExtraHigh,
        IconDensity.ExtraExtraHigh,
        IconDensity.ExtraExtraExtraHigh
    };
}

/// <summary>
/// Holds one localized Android adaptive launcher icon per screen density.
/// </summary>
/// <remarks>
/// An adaptive icon is a background and a foreground layer the operating system composes and masks to whatever shape
/// the launcher uses, so a density needs both layers to be usable. Assign both layers at every density: Unity writes
/// a locale only when the whole set resolves, because mixing a layer from one density with a layer scaled from another
/// distorts the icon, and a partial set would still outrank the Player Settings icons for that locale. Android 8.0
/// introduced adaptive icons, so populate <see cref="AppInfo.LegacyIcons"/> as well to cover older devices.
/// </remarks>
/// <example>
/// Assign a localized adaptive icon for one density.
/// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Platform/AndroidAdaptiveIconsExample.cs"/>
/// </example>
/// <seealso cref="AppInfo"/>
/// <seealso cref="Icons"/>
/// <seealso cref="IconDensity"/>
[Serializable]
public class AdaptiveIcons
{
    [Tooltip("The layer drawn behind the foreground, which the launcher may move to animate the icon.\n" +
        "The ic_launcher_background mipmap resource.")]
    [SerializeField] Icons m_Background = new();

    [Tooltip("The layer drawn on top, holding the part of the icon that should stay readable.\n" +
        "The ic_launcher_foreground mipmap resource.")]
    [SerializeField] Icons m_Foreground = new();

    /// <summary>
    /// The layer drawn behind the foreground, which the launcher may move to animate the icon.
    /// </summary>
    /// <remarks>
    /// Holds the textures for the <c>ic_launcher_background</c> mipmap resource, one per density.
    /// </remarks>
    public Icons Background
    {
        get => m_Background ??= new Icons();
        set => m_Background = value ?? new Icons();
    }

    /// <summary>
    /// The layer drawn on top of the background, holding the part of the icon that should stay readable.
    /// </summary>
    /// <remarks>
    /// Holds the textures for the <c>ic_launcher_foreground</c> mipmap resource, one per density.
    /// </remarks>
    public Icons Foreground
    {
        get => m_Foreground ??= new Icons();
        set => m_Foreground = value ?? new Icons();
    }

    /// <summary>
    /// Reports whether no density has both layers assigned.
    /// </summary>
    /// <remarks>
    /// A density is only usable when both its background and its foreground are assigned, so this reports true
    /// whenever no density has the pair. Assigning null to either layer replaces it with an empty set rather than
    /// leaving it null, so this is always safe to call.
    /// </remarks>
    /// <returns>True when no density has both layers, otherwise false.</returns>
    public bool IsEmpty()
    {
        foreach (var density in Icons.Densities)
        {
            if (!Background.GetIcon(density).IsEmpty && !Foreground.GetIcon(density).IsEmpty)
                return false;
        }
        return true;
    }
}
