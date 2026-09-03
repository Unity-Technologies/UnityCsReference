// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

/// <summary>
/// Picks which variant key applies right now (for example the current platform, day of week, or gender).
/// Custom selectors are a single class implementing this interface; they appear automatically in the
/// <c>[SerializeReference]</c> picker on a variant entry.
/// </summary>
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal interface IVariantSelector
{
    /// <summary>
    /// Stable identifier for this selector kind, used in serialized data and search tokens.
    /// </summary>
    string SelectorId { get; }

    /// <summary>
    /// The variant key that applies to the current runtime context, for example <c>"iOS"</c> or <c>"Monday"</c>.
    /// </summary>
    string CurrentKey { get; }

    /// <summary>
    /// Every key this selector knows about, used by the editor to populate "add variant" menus.
    /// </summary>
    IEnumerable<string> AvailableKeys { get; }
}

/// <summary>
/// One variant value: a key matching the selector plus a typed payload.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
[Serializable]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal struct Variant<T>
{
    [SerializeField] string m_Key;
    [SerializeField] T m_Value;

    /// <summary>
    /// Creates a variant.
    /// </summary>
    /// <param name="key">The selector key this value applies to.</param>
    /// <param name="value">The payload.</param>
    public Variant(string key, T value)
    {
        m_Key = key;
        m_Value = value;
    }

    /// <summary>
    /// The selector key this value applies to.
    /// </summary>
    public string Key => m_Key;

    /// <summary>
    /// The payload.
    /// </summary>
    public T Value => m_Value;
}

/// <summary>
/// Makes a key variant-driven by attaching the <see cref="IVariantSelector"/> that chooses its current variant.
/// Attach it to a shared key for a per-key selector, or to the <see cref="SharedTableData"/> for a default that
/// every key in the collection inherits. The selector lives on the shared data (not the per-locale entry) so it
/// stays the same across locales.
/// </summary>
[Serializable]
[Metadata(AllowedTypes = MetadataType.SharedTableEntry | MetadataType.SharedTableData, AllowMultiple = false, MenuItem = "Variant Selector")]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal class VariantSelectorMetadata : IMetadata
{
    [SerializeReference] IVariantSelector m_Selector;
    [SerializeField] List<string> m_Keys = new();

    /// <summary>
    /// Creates empty variant metadata.
    /// </summary>
    public VariantSelectorMetadata() {}

    /// <summary>
    /// Creates variant metadata for a selector.
    /// </summary>
    /// <param name="selector">The selector that chooses the current variant key.</param>
    public VariantSelectorMetadata(IVariantSelector selector) => m_Selector = selector;

    /// <summary>
    /// The selector that chooses the current variant key.
    /// </summary>
    public IVariantSelector Selector
    {
        get => m_Selector;
        set => m_Selector = value;
    }

    /// <summary>
    /// The variant keys authored for this scope, used by the editor to lay out variant rows.
    /// </summary>
    public IReadOnlyList<string> Keys => m_Keys;

    /// <summary>
    /// Adds <paramref name="key"/> to the authored variant keys if absent.
    /// </summary>
    /// <param name="key">The selector key.</param>
    public void AddKey(string key)
    {
        if (!string.IsNullOrEmpty(key) && !m_Keys.Contains(key))
            m_Keys.Add(key);
    }

    /// <summary>
    /// Removes <paramref name="key"/> from the authored variant keys.
    /// </summary>
    /// <param name="key">The selector key.</param>
    /// <returns>Whether the key was present.</returns>
    public bool RemoveKey(string key) => m_Keys.Remove(key);
}

/// <summary>
/// Resolves a variant payload for a given selector key, shared by all variant entry kinds.
/// </summary>
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal static partial class VariantResolver
{
    /// <summary>
    /// The application-wide fallback selector, used when neither the shared key nor the collection defines one.
    /// </summary>
    [AutoStaticsCleanup] // holds a caller supplied selector; mirrors ResetStatics()
    public static IVariantSelector GlobalSelector { get; set; }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static void ResetStatics() => GlobalSelector = null;

    /// <summary>
    /// Returns the variant matching <paramref name="currentKey"/>, or <paramref name="defaultValue"/>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="defaultValue">The value when no variant matches.</param>
    /// <param name="currentKey">The selector key to resolve (from the shared entry's selector); <see langword="null"/> resolves to the default.</param>
    /// <param name="variants">The defined variant values.</param>
    public static T Resolve<T>(T defaultValue, string currentKey, List<Variant<T>> variants)
    {
        if (string.IsNullOrEmpty(currentKey) || variants == null || variants.Count == 0)
            return defaultValue;
        for (var i = 0; i < variants.Count; i++)
        {
            if (variants[i].Key == currentKey)
                return variants[i].Value;
        }
        return defaultValue;
    }
}
