// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// Serves as the base class for asset entries that own a stored reference and load and release the asset themselves.
/// </summary>
/// <remarks>
/// The type parameter is the stored reference the entry keeps in <see cref="Default"/>, and each concrete kind decides
/// how that reference resolves to an asset: <see cref="AssetEntry"/> stores a direct object, and
/// <see cref="ResourceAssetEntry"/> stores a <c>Resources</c> path. The built-in kinds are synchronous: they resolve
/// through <see cref="ISynchronousAssetEntry.TryLoadAsset(out UnityEngine.Object)"/>, and public callers use the
/// <see cref="IAssetEntry"/> members <see cref="HasAsset()"/> and <see cref="ReleaseAsset(UnityEngine.Object)"/>. To add
/// a custom kind, override the protected <c>HasValue</c>, <c>TryLoadSync</c>, and <c>Release</c> members to back a
/// different storage, such as an Addressables handle provided by a package.
/// </remarks>
/// <typeparam name="TStore">The stored reference kind, for example a direct object or a <c>Resources</c> path. A package can add other kinds, such as an Addressables handle.</typeparam>
/// <example>
/// <para>Read the base members through a concrete asset entry.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/AssetEntryBaseOverviewExample.cs"/>
/// </example>
/// <seealso cref="AssetEntry"/>
/// <seealso cref="ResourceAssetEntry"/>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="ResourceTable"/>
[Serializable]
public abstract class AssetEntryBase<TStore> : ResourceEntryBase, IAssetEntry, ISynchronousAssetEntry
{
    [SerializeField] TStore m_Default;

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    protected AssetEntryBase() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    protected AssetEntryBase(long keyId) : base(keyId) { }

    /// <summary>
    /// The stored reference this entry loads its asset from.
    /// </summary>
    /// <remarks>
    /// The meaning depends on the kind: a direct object for <see cref="AssetEntry"/>, or a <c>Resources</c> path for
    /// <see cref="ResourceAssetEntry"/>.
    /// </remarks>
    public TStore Default
    {
        get => m_Default;
        set => m_Default = value;
    }

    /// <inheritdoc/>
    public bool HasAsset() => HasValue(m_Default);

    /// <inheritdoc/>
    public void ReleaseAsset(Object asset) => Release(m_Default, asset);

    /// <inheritdoc/>
    public bool TryLoadAsset(out Object asset) => TryLoadSync(m_Default, out asset);

    /// <summary>
    /// Returns whether the given stored reference holds a value.
    /// </summary>
    /// <param name="store">The stored reference to test.</param>
    /// <returns><c>true</c> when the reference holds a value; otherwise, <c>false</c>.</returns>
    protected abstract bool HasValue(TStore store);

    /// <summary>
    /// Resolves the asset from the given stored reference synchronously, when the kind supports it.
    /// </summary>
    /// <param name="store">The resolved store.</param>
    /// <param name="asset">The resolved asset, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the asset resolved synchronously; otherwise <see langword="false"/>.</returns>
    protected abstract bool TryLoadSync(TStore store, out Object asset);

    /// <summary>
    /// Releases an asset that was loaded from a stored reference.
    /// </summary>
    /// <remarks>
    /// The stored reference is passed so a store-aware kind can release by its reference rather than by the loaded
    /// object. The built-in direct and Resources kinds ignore it, but a package-provided Addressables-backed kind whose
    /// store is an address or handle uses it to release the right handle.
    /// </remarks>
    /// <param name="store">The resolved stored reference the asset was loaded from.</param>
    /// <param name="asset">The asset to release.</param>
    protected abstract void Release(TStore store, Object asset);
}

/// <summary>
/// Represents a localized asset stored as a direct object reference that ships and loads with the table.
/// </summary>
/// <remarks>
/// The asset is referenced directly through <see cref="DirectAsset"/>, so it is included in the build and resolves
/// without an asynchronous load. Use this kind for assets you want packed with the table, and use
/// <see cref="ResourceAssetEntry"/> when the asset ships through a <c>Resources</c> folder instead. Add one to a table
/// with <see cref="ResourceTable.AddEntry(IResourceEntry)"/>.
/// </remarks>
/// <example>
/// <para>Add a direct asset entry to a table and check its state.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/AssetEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="ResourceAssetEntry"/>
/// <seealso cref="AssetEntryBase{TStore}"/>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="ResourceTable"/>
[Serializable]
public class AssetEntry : AssetEntryBase<Object>
{
    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public AssetEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    public AssetEntry(long keyId) : base(keyId) { }

    /// <summary>
    /// The directly referenced asset for this entry.
    /// </summary>
    /// <remarks>
    /// This is the same value as <see cref="AssetEntryBase{TStore}.Default"/>, typed as <see cref="UnityEngine.Object"/>
    /// for convenience.
    /// </remarks>
    public Object DirectAsset
    {
        get => Default;
        set => Default = value;
    }

    /// <inheritdoc/>
    protected override bool HasValue(Object store) => store != null;

    /// <inheritdoc/>
    protected override bool TryLoadSync(Object store, out Object asset)
    {
        asset = store;
        return asset != null;
    }

    /// <inheritdoc/>
    protected override void Release(Object store, Object asset) { }
}

/// <summary>
/// Represents a localized asset stored as a path into a Resources folder.
/// </summary>
/// <remarks>
/// The entry keeps only the <c>Resources</c> path in <see cref="AssetEntryBase{TStore}.Default"/>, and the asset ships
/// through its Resources folder rather than as a direct reference, so nothing extra is packed with the table. The asset
/// is resolved on demand with <c>Resources.Load</c> and unloaded by
/// <see cref="AssetEntryBase{TStore}.ReleaseAsset(UnityEngine.Object)"/>. Use <see cref="AssetEntry"/> instead when you want a direct
/// reference packed with the table.
/// </remarks>
/// <example>
/// <para>Add a Resources-backed asset entry and confirm it has a path.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceAssetEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="AssetEntry"/>
/// <seealso cref="AssetEntryBase{TStore}"/>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="ResourceTable"/>
[Serializable]
public class ResourceAssetEntry : AssetEntryBase<string>, IFileDataEntry
{
    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public ResourceAssetEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    public ResourceAssetEntry(long keyId) : base(keyId) { }

    /// <inheritdoc/>
    protected override bool HasValue(string store) => !string.IsNullOrEmpty(store);

    /// <inheritdoc/>
    protected override bool TryLoadSync(string store, out Object asset)
    {
        asset = string.IsNullOrEmpty(store) ? null : Resources.Load<Object>(store);
        return asset != null;
    }

    /// <inheritdoc/>
    protected override void Release(string store, Object asset) => ResourceRelease.Unload(asset);
}

/// <summary>
/// An <see cref="AssetEntry"/> with per-variant direct-reference values, used when the key is variant-driven.
/// </summary>
[Serializable]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal sealed class VariantAssetEntry : AssetEntry, IVariantAssetEntry, ISynchronousVariantAssetEntry
{
    [SerializeField] List<Variant<Object>> m_Variants = new();

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public VariantAssetEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    public VariantAssetEntry(long keyId) : base(keyId) { }

    /// <summary>
    /// The per-variant values (the active key set is on the shared entry).
    /// </summary>
    public List<Variant<Object>> Variants => m_Variants;

    /// <inheritdoc/>
    public void RemoveVariant(string key)
    {
        for (var i = m_Variants.Count - 1; i >= 0; i--)
        {
            if (m_Variants[i].Key == key)
                m_Variants.RemoveAt(i);
        }
    }

    /// <inheritdoc/>
    public bool HasAsset(string variantKey) => HasValue(ResolveStore(variantKey));

    /// <inheritdoc/>
    public void ReleaseAsset(string variantKey, Object asset) => Release(ResolveStore(variantKey), asset);

    /// <inheritdoc/>
    public bool TryLoadAsset(string variantKey, out Object asset) => TryLoadSync(ResolveStore(variantKey), out asset);

    Object ResolveStore(string variantKey) => VariantResolver.Resolve(Default, variantKey, m_Variants);
}

/// <summary>
/// A <see cref="ResourceAssetEntry"/> with per-variant Resources-path values, used when the key is variant-driven.
/// </summary>
[Serializable]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal sealed class VariantResourceAssetEntry : ResourceAssetEntry, IVariantAssetEntry, ISynchronousVariantAssetEntry
{
    [SerializeField] List<Variant<string>> m_Variants = new();

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public VariantResourceAssetEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    public VariantResourceAssetEntry(long keyId) : base(keyId) { }

    /// <summary>
    /// The per-variant values (the active key set is on the shared entry).
    /// </summary>
    public List<Variant<string>> Variants => m_Variants;

    /// <inheritdoc/>
    public void RemoveVariant(string key)
    {
        for (var i = m_Variants.Count - 1; i >= 0; i--)
        {
            if (m_Variants[i].Key == key)
                m_Variants.RemoveAt(i);
        }
    }

    /// <inheritdoc/>
    public bool HasAsset(string variantKey) => HasValue(ResolveStore(variantKey));

    /// <inheritdoc/>
    public void ReleaseAsset(string variantKey, Object asset) => Release(ResolveStore(variantKey), asset);

    /// <inheritdoc/>
    public bool TryLoadAsset(string variantKey, out Object asset) => TryLoadSync(ResolveStore(variantKey), out asset);

    string ResolveStore(string variantKey) => VariantResolver.Resolve(Default, variantKey, m_Variants);
}
