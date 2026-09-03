// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// An optional capability for an asset provider that loads assets asynchronously.
/// </summary>
/// <remarks>
/// Implement this alongside <see cref="IAssetProvider"/> when a provider loads through an asynchronous operation, such as
/// a Resources request or Addressables. The localization system calls this on the asynchronous resolve paths. A provider
/// that can also resolve immediately implements <see cref="ISynchronousAssetProvider"/> as well; one that can only resolve
/// immediately implements just the synchronous capability, and the asynchronous paths wrap its result in a completed
/// <see cref="UnityEngine.Awaitable"/>.
/// </remarks>
/// <example>
/// <para>A provider that resolves its assets through an asynchronous operation.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/IAsyncAssetProviderExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="ISynchronousAssetProvider"/>
public interface IAsyncAssetProvider : IAssetProvider
{
    /// <summary>
    /// Loads the asset identified by the key as the requested type.
    /// </summary>
    /// <remarks>
    /// The asset type is <typeparamref name="T"/>, or <see cref="AssetKey.Type"/> when <typeparamref name="T"/> is the
    /// base <see cref="UnityEngine.Object"/>, so a type-erased caller can pass <c>&lt;Object&gt;</c> and carry the type on
    /// the key. Completes with <see langword="null"/> when the key cannot be resolved, or when the resolved asset is not a
    /// <typeparamref name="T"/>. A cancelled load throws <see cref="System.OperationCanceledException"/>.
    /// </remarks>
    /// <typeparam name="T">The asset type to load, or <see cref="UnityEngine.Object"/> to use the type on the key.</typeparam>
    /// <param name="key">The asset to load.</param>
    /// <param name="cancellationToken">A token that cancels the load.</param>
    /// <returns>The loaded asset, or <see langword="null"/> when it cannot be resolved.</returns>
    /// <example>
    /// <para>Load a texture through a provider.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/IAsyncAssetProviderLoadAssetGenericExample.cs"/>
    /// </example>
    Awaitable<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : Object;
}
