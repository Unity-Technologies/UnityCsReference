// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization.Providers;

/// <summary>
/// An ordered chain of asset providers used to load localized assets. Providers are tried in order and the first one
/// to resolve an asset wins.
/// </summary>
/// <remarks>
/// The localization settings hold a single instance whose provider list is configurable in the inspector. Add
/// providers such as <see cref="ReferencedAssetProvider"/> (direct references), <see cref="ResourceFolderProvider"/>,
/// or an Addressables-backed provider from a package. Each provider resolves an <see cref="AssetKey"/> in its own way,
/// so ordering decides which one answers a given address. The <see cref="Selected"/> provider is the default target
/// for new content authored in the editor.
/// </remarks>
/// <example>
/// <para>Build a chain, choose the default provider, then look one up by type.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderOverviewExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="AssetKey"/>
/// <seealso cref="ReferencedAssetProvider"/>
/// <seealso cref="ResourceFolderProvider"/>
[Serializable]
public sealed class AssetProvider : ISerializationCallbackReceiver
{
    [SerializeReference]
    List<IAssetProvider> m_Providers;

    [SerializeReference]
    IAssetProvider m_Selected;

    /// <summary>
    /// Creates an empty chain.
    /// </summary>
    /// <remarks>
    /// The new chain has no providers and no <see cref="Selected"/> provider. Populate it with
    /// <see cref="AddProvider(IAssetProvider)"/> or <see cref="InsertProvider(int,IAssetProvider)"/>.
    /// </remarks>
    /// <example>
    /// <para>Create a chain and add a provider to it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderConstructorExample.cs"/>
    /// </example>
    public AssetProvider()
    {
        m_Providers = new List<IAssetProvider>();
    }

    /// <summary>
    /// The providers, in the order they are tried.
    /// </summary>
    public IReadOnlyList<IAssetProvider> Providers => m_Providers;

    /// <summary>
    /// The selected provider: the entry used as the default for new content, or null when none is chosen.
    /// </summary>
    /// <remarks>
    /// Assigning a provider that is not in <see cref="Providers"/> is ignored and leaves the selection
    /// <see langword="null"/>. When the selected provider is removed from the chain, the selection resets to the first
    /// provider, or <see langword="null"/> when the chain is empty.
    /// </remarks>
    public IAssetProvider Selected
    {
        get => m_Selected;
        set => m_Selected = value != null && m_Providers.Contains(value) ? value : null;
    }

    /// <summary>
    /// Adds a provider to the end of the chain.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> provider is ignored. Because providers are tried in order, an entry added last is
    /// consulted last.
    /// </remarks>
    /// <param name="provider">The provider to add.</param>
    /// <example>
    /// <para>Append a provider to a chain.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderAddProviderExample.cs"/>
    /// </example>
    public void AddProvider(IAssetProvider provider)
    {
        if (provider != null)
            m_Providers.Add(provider);
    }

    /// <summary>
    /// Inserts a provider at the given position in the chain.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> provider is ignored. Inserting at index 0 makes the provider the first one tried, which
    /// lets it take priority over the existing entries.
    /// </remarks>
    /// <param name="index">The position to insert at.</param>
    /// <param name="provider">The provider to insert.</param>
    /// <example>
    /// <para>Insert a provider at the front so it is tried first.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderInsertProviderExample.cs"/>
    /// </example>
    public void InsertProvider(int index, IAssetProvider provider)
    {
        if (provider != null)
            m_Providers.Insert(index, provider);
    }

    /// <summary>
    /// Removes a provider from the chain.
    /// </summary>
    /// <remarks>
    /// When the removed provider was the <see cref="Selected"/> one, the selection moves to the first remaining
    /// provider, or <see langword="null"/> when the chain becomes empty.
    /// </remarks>
    /// <param name="provider">The provider to remove.</param>
    /// <returns><c>true</c> if the provider was present and removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Remove a provider and confirm it is gone.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderRemoveProviderExample.cs"/>
    /// </example>
    public bool RemoveProvider(IAssetProvider provider)
    {
        var removed = m_Providers.Remove(provider);
        if (removed && ReferenceEquals(m_Selected, provider))
            m_Selected = m_Providers.Count > 0 ? m_Providers[0] : null;
        return removed;
    }

    /// <summary>
    /// Returns the first provider of the given type in the chain, or null when there is none.
    /// </summary>
    /// <remarks>
    /// Use this to reach a specific provider, for example to register a direct reference on the chain's
    /// <see cref="ReferencedAssetProvider"/>.
    /// </remarks>
    /// <typeparam name="T">The provider type to find.</typeparam>
    /// <returns>The first matching provider, or <see langword="null"/> when none is present.</returns>
    /// <example>
    /// <para>Find a provider of a specific type in the chain.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetProviderGetProviderExample.cs"/>
    /// </example>
    public T GetProvider<T>() where T : class, IAssetProvider
    {
        for (var i = 0; i < m_Providers.Count; i++)
        {
            if (m_Providers[i] is T typed)
                return typed;
        }
        return null;
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void RevalidateSelection()
    {
        if (m_Selected != null && !m_Providers.Contains(m_Selected))
            m_Selected = m_Providers.Count > 0 ? m_Providers[0] : null;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() { }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // The constructor does not run on deserialize, so guard against a missing field from older data.
        m_Providers ??= new List<IAssetProvider>();
        // SerializeReference entries deserialize to null when their type cannot be resolved, so drop them.
        m_Providers.RemoveAll(p => p is null);
        RevalidateSelection();
    }
}
