// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// An optional capability for an asset provider that can resolve an asset immediately, without an asynchronous load.
/// </summary>
/// <remarks>
/// Implement this alongside <see cref="IAssetProvider"/> when a provider already has the asset in hand, such as a
/// direct reference or a synchronous <c>Resources</c> load, so the localization system can resolve it on the
/// synchronous paths (the <c>GetTable</c>/<c>GetLocalizedString</c>/<c>GetLocalizedAsset</c> accessors, and
/// references resolved under the <see cref="LoadingPreference.Synchronous"/> loading preference). A provider that can only load
/// asynchronously does not implement this interface; the synchronous paths skip it and fall back to whatever is
/// already cached. A Unity <see cref="UnityEngine.Awaitable"/> cannot be forced to complete on the main thread, so
/// this interface is how a provider opts in to synchronous resolution rather than blocking one.
/// </remarks>
/// <example>
/// <para>A provider that keeps assets in memory and resolves them synchronously.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/SyncAssetProviderExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="ResourceDatabase"/>
public interface ISynchronousAssetProvider
{
    /// <summary>
    /// Tries to resolve the asset for a key without loading asynchronously.
    /// </summary>
    /// <remarks>
    /// Return <see langword="true"/> and set <paramref name="asset"/> when the asset resolves immediately. Return
    /// <see langword="false"/> on a miss so the provider chain continues to the next provider, and leave
    /// <paramref name="asset"/> as <see langword="null"/>. The asset type is <typeparamref name="T"/>, or
    /// <see cref="AssetKey.Type"/> when <typeparamref name="T"/> is the base <see cref="UnityEngine.Object"/>, mirroring
    /// <see cref="IAsyncAssetProvider.LoadAssetAsync{T}(AssetKey, System.Threading.CancellationToken)"/>.
    /// </remarks>
    /// <typeparam name="T">The asset type to resolve, or <see cref="UnityEngine.Object"/> to use the type on the key.</typeparam>
    /// <param name="key">The asset to resolve.</param>
    /// <param name="asset">The resolved asset, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the asset resolved synchronously; otherwise <see langword="false"/>.</returns>
    bool TryLoadAsset<T>(AssetKey key, out T asset) where T : Object;
}
