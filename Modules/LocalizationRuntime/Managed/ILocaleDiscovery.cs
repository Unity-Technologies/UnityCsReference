// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using Unity.Localization.Providers;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Optional interface an asset provider implements to discover and register additional locales at runtime.
/// </summary>
/// <remarks>
/// Implement this alongside <see cref="IAssetProvider"/> when a provider can find locales that are not known at build time,
/// for example from data files added to a built player. Each provider in the chain is called during
/// <see cref="LocalizationSettings.InitializeAsync"/>, before the startup locale selectors run, so a discovered locale can be
/// chosen as the startup locale. Register discovered locales through <see cref="LocalizationSettings.AddLocale"/>; providers
/// that only serve the locales shipped with the project do not need this interface.
/// </remarks>
/// <example>
/// <para>Discover locales from installed data files and register each one at startup.</para>
/// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/DiskLocaleProviderExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="Locale"/>
/// <seealso cref="IAssetProvider"/>
public interface ILocaleDiscovery
{
    /// <summary>
    /// Registers any additional locales this provider can serve.
    /// </summary>
    /// <remarks>
    /// Called during <see cref="LocalizationSettings.InitializeAsync"/>. Add each discovered locale with
    /// <see cref="LocalizationSettings.AddLocale"/>, and honor <paramref name="cancellationToken"/> for long-running discovery.
    /// </remarks>
    /// <param name="settings">The settings to register discovered locales on.</param>
    /// <param name="cancellationToken">The token that signals the discovery should be canceled.</param>
    /// <returns>An <see cref="UnityEngine.Awaitable"/> that completes once locale discovery finishes.</returns>
    Awaitable DiscoverLocalesAsync(LocalizationSettings settings, CancellationToken cancellationToken = default);
}
