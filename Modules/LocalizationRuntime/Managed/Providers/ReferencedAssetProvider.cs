// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// An asset provider that resolves an address to a directly referenced asset. Use it when assets are assigned by
/// reference rather than loaded from built content, a Resources folder, or Addressables.
/// </summary>
/// <remarks>
/// The address-to-reference map is populated through <see cref="Add(string,UnityEngine.Object)"/>, so
/// <see cref="AssetKey.Address"/> is the key into that map. Loads complete immediately because the assets are already
/// in memory. <see cref="Release(UnityEngine.Object)"/> is a no-op: the references are owned by whatever holds this
/// provider, so there is nothing to unload. The map is authored programmatically by the table editor, so it is hidden
/// from the inspector.
/// </remarks>
/// <example>
/// <para>Register a texture by address and load it back through the provider.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderOverviewExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="AssetProvider"/>
/// <seealso cref="ResourceFolderProvider"/>
/// <seealso cref="AssetKey"/>
[Serializable]
public sealed class ReferencedAssetProvider : IAssetProvider, ISynchronousAssetProvider
{
    // Authored by the editor, not hand-edited, so hidden from the inspector.
    [SerializeField, HideInInspector] Dictionary<string, Object> m_ByAddress = new();
    // The addresses the editor registered, so a rebuild can drop them without touching mappings added through Add.
    [SerializeField, HideInInspector] List<string> m_OwnedAddresses = new();


    /// <summary>
    /// Maps an address to a directly referenced asset, replacing any existing mapping for that address.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/>, or a <see langword="null"/> <paramref name="asset"/>, is ignored.
    /// Callers request the asset later through an <see cref="AssetKey"/> that carries the same address.
    /// </remarks>
    /// <param name="address">The address callers request through an <see cref="AssetKey"/>.</param>
    /// <param name="asset">The asset to return for that address.</param>
    /// <example>
    /// <para>Register an asset under an address.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderAddExample.cs"/>
    /// </example>
    public void Add(string address, Object asset)
    {
        if (string.IsNullOrEmpty(address) || asset == null)
            return;
        m_ByAddress[address] = asset;
        // A mapping set through this method is no longer editor-owned, so ClearOwned leaves it alone.
        m_OwnedAddresses.Remove(address);
    }

    /// <summary>
    /// Removes any entry registered under the given address.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/> is treated as absent and returns <c>false</c>.
    /// </remarks>
    /// <param name="address">The address to clear.</param>
    /// <returns><c>true</c> if an entry was removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Remove a registered asset and confirm it is gone.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderRemoveExample.cs"/>
    /// </example>
    public bool Remove(string address)
    {
        if (string.IsNullOrEmpty(address) || !m_ByAddress.Remove(address))
            return false;
        m_OwnedAddresses.Remove(address);
        return true;
    }

    // Drops owned addresses for the asset, so a stale one (after a rename or locale change) cannot keep resolving to it.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void RemoveAsset(Object asset)
    {
        if (asset == null)
            return;
        List<string> stale = null;
        for (var i = 0; i < m_OwnedAddresses.Count; i++)
        {
            var address = m_OwnedAddresses[i];
            if (m_ByAddress.TryGetValue(address, out var value) && value == asset)
                (stale ??= new List<string>()).Add(address);
        }
        if (stale == null)
            return;
        foreach (var address in stale)
        {
            m_ByAddress.Remove(address);
            m_OwnedAddresses.Remove(address);
        }
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void AddOwned(string address, Object asset)
    {
        if (string.IsNullOrEmpty(address) || asset == null)
            return;
        // Add drops any owned record for the address, so this append cannot duplicate.
        Add(address, asset);
        m_OwnedAddresses.Add(address);
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void ClearOwned()
    {
        for (var i = 0; i < m_OwnedAddresses.Count; i++)
            m_ByAddress.Remove(m_OwnedAddresses[i]);
        m_OwnedAddresses.Clear();
    }

    /// <summary>
    /// Checks whether an asset is registered under the given address.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/> returns <c>false</c>.
    /// </remarks>
    /// <param name="address">The address to test.</param>
    /// <returns><c>true</c> if an asset is registered under the address; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Test whether an address is registered before and after adding it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderContainsExample.cs"/>
    /// </example>
    public bool Contains(string address) => !string.IsNullOrEmpty(address) && m_ByAddress.ContainsKey(address);

    /// <summary>
    /// Resolves the referenced asset for the key without an asynchronous load.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> and the asset when the address is registered and the asset is a
    /// <typeparamref name="T"/> (or matches <see cref="AssetKey.Type"/> when <typeparamref name="T"/> is the base
    /// <see cref="UnityEngine.Object"/>); otherwise returns <see langword="false"/> with a <see langword="null"/> asset.
    /// Because the assets are already in memory, this provider only implements the synchronous capability.
    /// </remarks>
    /// <typeparam name="T">The asset type to resolve, or <see cref="UnityEngine.Object"/> to use the type on the key.</typeparam>
    /// <param name="key">The asset to resolve.</param>
    /// <param name="asset">The resolved asset, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the asset resolved; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <para>Register a texture by address and resolve it back through the provider.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderLoadAssetGenericExample.cs"/>
    /// </example>
    public bool TryLoadAsset<T>(AssetKey key, out T asset) where T : Object
    {
        asset = Resolve(key) as T;
        return asset != null;
    }

    /// <summary>
    /// Does nothing, because this provider does not own the assets it returns.
    /// </summary>
    /// <remarks>
    /// The references are owned by whatever holds this provider, so there is nothing to unload. This method exists to
    /// satisfy <see cref="IAssetProvider"/>.
    /// </remarks>
    /// <param name="asset">The asset to release. Ignored.</param>
    /// <example>
    /// <para>Call release on a referenced asset. Nothing is unloaded.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/ReferencedAssetProviderReleaseExample.cs"/>
    /// </example>
    public void Release(Object asset) { }

    Object Resolve(AssetKey key)
    {
        if (string.IsNullOrEmpty(key.Address))
            return null;
        return m_ByAddress.TryGetValue(key.Address, out var asset) && asset != null && key.Type.IsInstanceOfType(asset)
            ? asset
            : null;
    }
}
