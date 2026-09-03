// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// An asset provider that loads assets from Resources folders, treating the address as a Resources path.
/// </summary>
/// <remarks>
/// An address that does not resolve to an asset in a <c>Resources</c> folder is a miss and returns
/// <see langword="null"/>, so addresses meant for other providers do not resolve here. Because this provider always
/// loads by <c>Resources</c> path, a table referenced by collection guid cannot address it directly. Register a
/// guid-address to path alias with <see cref="AddAlias(string,string)"/> so a guid reference still resolves.
/// </remarks>
/// <example>
/// <para>Load an asset from a Resources folder by path.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ResourceFolderProviderOverviewExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="AssetProvider"/>
/// <seealso cref="ReferencedAssetProvider"/>
/// <seealso cref="AssetKey"/>
[Serializable]
public sealed class ResourceFolderProvider : IAsyncAssetProvider, ISynchronousAssetProvider
{
    // Authored by the provider editor (guid-address to Resources-path aliases), not hand-edited.
    [SerializeField, HideInInspector] Dictionary<string, string> m_AliasByAddress = new();
    // The aliases the editor registered, so a rebuild can drop them without touching aliases added through AddAlias.
    [SerializeField, HideInInspector] List<string> m_OwnedAddresses = new();


    /// <summary>
    /// Maps an address to a Resources path, replacing any existing alias for that address.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/> or <paramref name="path"/> is ignored. After aliasing, a load whose
    /// <see cref="AssetKey.Address"/> matches <paramref name="address"/> resolves to <paramref name="path"/>.
    /// </remarks>
    /// <param name="address">The address callers request through an <see cref="AssetKey"/>, for example a table guid address.</param>
    /// <param name="path">The <c>Resources</c> path the address resolves to.</param>
    /// <example>
    /// <para>Alias a guid address to a Resources path, then load through the alias.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ResourceFolderProviderAddAliasExample.cs"/>
    /// </example>
    public void AddAlias(string address, string path)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(path))
            return;
        m_AliasByAddress[address] = path;
        // An alias set through this method is no longer editor-owned, so ClearOwned leaves it alone.
        m_OwnedAddresses.Remove(address);
    }

    /// <summary>
    /// Removes any alias registered under the given address.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/> is treated as absent and returns <c>false</c>. After removal, the
    /// address resolves as a plain <c>Resources</c> path again.
    /// </remarks>
    /// <param name="address">The address to clear.</param>
    /// <returns><c>true</c> if an alias was removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Remove an alias that was registered earlier.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ResourceFolderProviderRemoveExample.cs"/>
    /// </example>
    public bool Remove(string address)
    {
        if (string.IsNullOrEmpty(address) || !m_AliasByAddress.Remove(address))
            return false;
        m_OwnedAddresses.Remove(address);
        return true;
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void RemoveAliasesTo(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        List<string> stale = null;
        for (var i = 0; i < m_OwnedAddresses.Count; i++)
        {
            var address = m_OwnedAddresses[i];
            if (m_AliasByAddress.TryGetValue(address, out var value) && value == path)
                (stale ??= new List<string>()).Add(address);
        }
        if (stale == null)
            return;
        foreach (var address in stale)
        {
            m_AliasByAddress.Remove(address);
            m_OwnedAddresses.Remove(address);
        }
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void AddOwnedAlias(string address, string path)
    {
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(path))
            return;
        // AddAlias drops any owned record for the address, so this append cannot duplicate.
        AddAlias(address, path);
        m_OwnedAddresses.Add(address);
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void ClearOwned()
    {
        for (var i = 0; i < m_OwnedAddresses.Count; i++)
            m_AliasByAddress.Remove(m_OwnedAddresses[i]);
        m_OwnedAddresses.Clear();
    }

    /// <summary>
    /// Returns the <c>Resources</c> path <paramref name="address"/> aliases to, or the address itself when not aliased.
    /// </summary>
    /// <param name="address">The requested address.</param>
    internal string ResolveAddress(string address)
        => !string.IsNullOrEmpty(address) && m_AliasByAddress.TryGetValue(address, out var path) ? path : address;

    /// <summary>
    /// Loads the asset for the key from a Resources folder as the requested type.
    /// </summary>
    /// <remarks>
    /// Resolves the address through any registered alias, then loads it from <c>Resources</c> as <typeparamref name="T"/>,
    /// or as <see cref="AssetKey.Type"/> when <typeparamref name="T"/> is the base <see cref="UnityEngine.Object"/>. When
    /// <see cref="AssetKey.SubAssetName"/> is set, the named sub-asset at the path is returned. Completes with
    /// <see langword="null"/> when nothing at the path matches, and throws <see cref="System.OperationCanceledException"/>
    /// when the load is cancelled.
    /// </remarks>
    /// <typeparam name="T">The asset type to load, or <see cref="UnityEngine.Object"/> to use the type on the key.</typeparam>
    /// <param name="key">The asset to load.</param>
    /// <param name="cancellationToken">A token that cancels the load.</param>
    /// <returns>The loaded asset, or <see langword="null"/> when nothing at the path matches.</returns>
    /// <example>
    /// <para>Load a sprite from a Resources folder.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ResourceFolderProviderLoadAssetGenericExample.cs"/>
    /// </example>
    public async Awaitable<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : Object
        => await LoadInternal(key, LoadType<T>(key), cancellationToken) as T;

    // The explicit type argument wins; a base-Object T defers to the type carried on the key (the type-erased path).
    static Type LoadType<T>(AssetKey key) where T : Object => typeof(T) != typeof(Object) ? typeof(T) : key.Type;

    async Awaitable<Object> LoadInternal(AssetKey key, Type type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(key.Address))
            return null;

        var address = ResolveAddress(key.Address);
        if (key.SubAssetName != null)
            return SelectByName(Resources.LoadAll(address, type), key.SubAssetName);

        var request = Resources.LoadAsync(address, type);
        await Awaitable.FromAsyncOperation(request);
        cancellationToken.ThrowIfCancellationRequested();
        return request.asset;
    }

    /// <inheritdoc/>
    public bool TryLoadAsset<T>(AssetKey key, out T asset) where T : Object
    {
        asset = null;
        if (string.IsNullOrEmpty(key.Address))
            return false;
        var type = LoadType<T>(key);
        var address = ResolveAddress(key.Address);
        asset = (key.SubAssetName != null
            ? SelectByName(Resources.LoadAll(address, type), key.SubAssetName)
            : Resources.Load(address, type)) as T;
        return asset != null;
    }

    /// <summary>
    /// Unloads an asset previously loaded from a Resources folder.
    /// </summary>
    /// <remarks>
    /// Releases the Resources reference so the asset can be unloaded when nothing else uses it. Pass an asset returned
    /// by one of the load methods.
    /// </remarks>
    /// <param name="asset">The asset to unload.</param>
    /// <example>
    /// <para>Release a loaded asset once it is no longer needed.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ResourceFolderProviderReleaseExample.cs"/>
    /// </example>
    public void Release(Object asset) => ResourceRelease.Unload(asset);

    static Object SelectByName(Object[] objects, string name)
    {
        if (objects == null)
            return null;
        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].name == name)
                return objects[i];
        }
        return null;
    }
}
