// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Object = UnityEngine.Object;

namespace Unity.Localization.Providers;

/// <summary>
/// Identifies a single asset to load through an asset provider. The key pairs a provider specific address with an
/// expected asset type, and optionally the name of a sub-asset at that address.
/// </summary>
/// <remarks>
/// The <see cref="Address"/> is opaque to the cache and is interpreted by the active provider:
/// <see cref="ReferencedAssetProvider"/> treats it as the key into its address-to-reference map,
/// <see cref="ResourceFolderProvider"/> treats it as a <c>Resources</c> path, and a provider installed from a package
/// (for example an Addressables-backed one) can interpret it as its own address. <see cref="Type"/> is part of the
/// identity because one address can hold objects of several types, so two keys with the same address but a different
/// type are distinct.
/// </remarks>
/// <example>
/// <para>Create a key for a texture and inspect its parts.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyOverviewExample.cs"/>
/// </example>
/// <seealso cref="IAssetProvider"/>
/// <seealso cref="AssetProvider"/>
/// <seealso cref="ReferencedAssetProvider"/>
/// <seealso cref="ResourceFolderProvider"/>
public readonly struct AssetKey : IEquatable<AssetKey>
{
    readonly string m_Address;
    readonly string m_SubAssetName;
    readonly Type m_Type;

    /// <summary>
    /// Creates a key for the given address, interpreted by the active provider.
    /// </summary>
    /// <remarks>
    /// An empty or null <paramref name="address"/> produces a key whose <see cref="IsValid"/> is <c>false</c>, which
    /// providers treat as a miss. When <paramref name="type"/> is <see langword="null"/>, <see cref="Type"/> reports
    /// <see cref="UnityEngine.Object"/>. Pass <paramref name="subAssetName"/> to target a named sub-asset at the
    /// address, such as one sprite in a sheet.
    /// </remarks>
    /// <param name="address">The provider-specific address, for example a <c>Resources</c> path.</param>
    /// <param name="type">The expected asset type. Defaults to <see cref="UnityEngine.Object"/>.</param>
    /// <param name="subAssetName">The optional name of a sub-asset at the address.</param>
    /// <example>
    /// <para>Create a key for a whole asset and a key for a named sub-asset.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyConstructorExample.cs"/>
    /// </example>
    public AssetKey(string address, Type type = null, string subAssetName = null)
    {
        m_Address = string.IsNullOrEmpty(address) ? null : address;
        m_SubAssetName = string.IsNullOrEmpty(subAssetName) ? null : subAssetName;
        m_Type = type;
    }

    /// <summary>
    /// The provider-specific address that identifies the asset.
    /// </summary>
    public string Address => m_Address;

    /// <summary>
    /// The name of the sub-asset at the address, or null when the key targets the whole asset.
    /// </summary>
    public string SubAssetName => m_SubAssetName;

    /// <summary>
    /// The expected asset type.
    /// </summary>
    /// <remarks>
    /// Reports <see cref="UnityEngine.Object"/> when no type was supplied to the constructor.
    /// </remarks>
    public Type Type => m_Type ?? typeof(Object);

    /// <summary>
    /// Whether this key carries an address.
    /// </summary>
    /// <remarks>
    /// A key built from an empty or null address is invalid, and providers resolve it to <see langword="null"/>.
    /// </remarks>
    public bool IsValid => m_Address != null;

    /// <summary>
    /// Checks whether this key identifies the same asset as another.
    /// </summary>
    /// <remarks>
    /// Two keys are equal when their addresses match by ordinal comparison, their sub-asset names match by ordinal
    /// comparison, and their <see cref="Type"/> values are the same. Keys with the same address but a different type
    /// are not equal.
    /// </remarks>
    /// <param name="other">The key to compare with this one.</param>
    /// <returns><c>true</c> if both keys identify the same asset; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two keys built for the same asset.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyEqualsExample.cs"/>
    /// </example>
    public bool Equals(AssetKey other)
    {
        return string.Equals(m_Address, other.m_Address, StringComparison.Ordinal)
            && string.Equals(m_SubAssetName, other.m_SubAssetName, StringComparison.Ordinal)
            && Type == other.Type;
    }

    /// <summary>
    /// Checks whether this key equals another object.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when <paramref name="obj"/> is not an <see cref="AssetKey"/>. Otherwise, defers to
    /// <see cref="Equals(AssetKey)"/> for value comparison.
    /// </remarks>
    /// <param name="obj">The object to compare with this key.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is a key that identifies the same asset; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare against a boxed key.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyEqualsObjectExample.cs"/>
    /// </example>
    public override bool Equals(object obj) => obj is AssetKey other && Equals(other);

    /// <summary>
    /// Computes a hash code for this key.
    /// </summary>
    /// <remarks>
    /// Consistent with <see cref="Equals(AssetKey)"/>: keys that compare equal produce the same hash code. Derived from
    /// the address, sub-asset name, and type, so a key is safe to use as a dictionary key for a load cache.
    /// </remarks>
    /// <returns>An integer hash code derived from the address, sub-asset name, and type.</returns>
    /// <example>
    /// <para>Use a key as a dictionary key for loaded assets.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyHashCodeExample.cs"/>
    /// </example>
    public override int GetHashCode() => HashCode.Combine(m_Address, m_SubAssetName, Type);

    /// <summary>
    /// Compares two keys for equality.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="Equals(AssetKey)"/>: the keys match when their address, sub-asset name, and
    /// <see cref="Type"/> all match.
    /// </remarks>
    /// <param name="a">The first key.</param>
    /// <param name="b">The second key.</param>
    /// <returns><c>true</c> if the keys identify the same asset; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two keys with the equality operator.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyEqualityOperatorExample.cs"/>
    /// </example>
    public static bool operator ==(AssetKey a, AssetKey b) => a.Equals(b);

    /// <summary>
    /// Compares two keys for inequality.
    /// </summary>
    /// <remarks>
    /// The negation of <see cref="Equals(AssetKey)"/>: the keys differ when their address, sub-asset name, or
    /// <see cref="Type"/> differ.
    /// </remarks>
    /// <param name="a">The first key.</param>
    /// <param name="b">The second key.</param>
    /// <returns><c>true</c> if the keys identify different assets; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Distinguish two keys that share an address but differ by type.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyInequalityOperatorExample.cs"/>
    /// </example>
    public static bool operator !=(AssetKey a, AssetKey b) => !a.Equals(b);

    /// <summary>
    /// Returns a readable string describing this key.
    /// </summary>
    /// <remarks>
    /// Formats the address, the sub-asset name in square brackets when present, and the type name in parentheses. An
    /// invalid key uses &lt;invalid&gt; in place of the address. Intended for debugging and logging.
    /// </remarks>
    /// <returns>A human-readable description of the key.</returns>
    /// <example>
    /// <para>Log a sub-asset key for debugging.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/AssetKeyToStringExample.cs"/>
    /// </example>
    public override string ToString()
    {
        var text = IsValid ? m_Address : "<invalid>";
        if (m_SubAssetName != null)
            text += $"[{m_SubAssetName}]";
        return text + $" ({Type.Name})";
    }
}
