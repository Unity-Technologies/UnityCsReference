// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// The base capability shared by asset providers in the localization system.
/// </summary>
/// <remarks>
/// A provider decides how an address resolves to an asset, for example from built content, a Resources folder, or
/// Addressables. It opts into loading by implementing <see cref="IAsyncAssetProvider"/>, <see cref="ISynchronousAssetProvider"/>,
/// or both; the localization system prefers the asynchronous version and falls back to the synchronous one, and on the
/// synchronous paths it uses only synchronous providers. Add an implementation to an <see cref="AssetProvider"/> chain to
/// make it available. The built-in implementations are <see cref="ReferencedAssetProvider"/> (synchronous) and
/// <see cref="ResourceFolderProvider"/> (both).
/// </remarks>
/// <example>
/// <para>A synchronous provider that resolves assets it already holds in memory.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/SyncAssetProviderExample.cs"/>
/// </example>
/// <seealso cref="IAsyncAssetProvider"/>
/// <seealso cref="ISynchronousAssetProvider"/>
/// <seealso cref="AssetProvider"/>
/// <seealso cref="AssetKey"/>
public interface IAssetProvider
{
    /// <summary>
    /// The identifier a table collection uses to name the provider that serves it.
    /// </summary>
    /// <remarks>
    /// Defaults to the provider's type name. A chain holds at most one provider of each type, so the type identifies
    /// the instance. Override only to keep an identifier stable across a rename.
    /// </remarks>
    string Id => GetType().FullName;

    /// <summary>
    /// Releases an asset previously returned by this provider.
    /// </summary>
    /// <remarks>
    /// Call this when the asset is no longer needed so the provider can unload it. Providers that hand back assets they
    /// do not own, such as <see cref="ReferencedAssetProvider"/>, implement this as a no-op.
    /// </remarks>
    /// <param name="asset">The asset to release.</param>
    /// <example>
    /// <para>Release an asset once it is no longer needed.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/IAssetProviderReleaseExample.cs"/>
    /// </example>
    void Release(Object asset);
}
