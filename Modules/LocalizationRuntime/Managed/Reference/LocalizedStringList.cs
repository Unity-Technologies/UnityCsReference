// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Receives the localized values whenever a localized string list resolves or changes.
/// </summary>
/// <remarks>
/// Subscribe a handler through <see cref="ILocalizedStringList.ListChanged"/>. The list instance passed to the handler
/// is a copy, so the handler can keep or edit it freely.
/// </remarks>
/// <param name="values">The localized values, in list order.</param>
public delegate void ListChangeHandler(List<string> values);

/// <summary>
/// Interface for resolving a localized list of strings.
/// </summary>
/// <remarks>
/// Implementations resolve one or more table entries into an ordered list, for example the options of a dropdown.
/// Subscribe to <see cref="ListChanged"/> to receive the values and every later change, including locale switches, or
/// resolve on demand with <see cref="GetLocalizedList"/> and <see cref="GetLocalizedListAsync"/>. The built-in
/// implementations are <see cref="LocalizedStringList"/>, which splits one entry on a separator, and
/// <see cref="LocalizedStringGroup"/>, which combines several entries.
/// </remarks>
/// <example>
/// Resolve a list of options and fill a dropdown.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringListExample.cs"/>
/// </example>
/// <seealso cref="LocalizedStringList"/>
/// <seealso cref="LocalizedStringGroup"/>
/// <seealso cref="Unity.Localization.Components.LocalizeStringListEvent"/>
public interface ILocalizedStringList
{
    /// <summary>
    /// Raised with the localized values when the list first resolves and again whenever a value changes.
    /// </summary>
    /// <remarks>
    /// Subscribing begins resolution, so the first invocation arrives once the values have loaded. The event is raised
    /// again when the selected locale changes.
    /// </remarks>
    /// <example>
    /// Resolve a list of options and fill a dropdown.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringListExample.cs"/>
    /// </example>
    /// <seealso cref="ListChangeHandler"/>
    event ListChangeHandler ListChanged;

    /// <summary>
    /// Resolves the localized values without waiting.
    /// </summary>
    /// <remarks>
    /// The list holds an empty string for any value that cannot resolve synchronously; use
    /// <see cref="GetLocalizedListAsync"/> to wait for those values instead.
    /// </remarks>
    /// <example>
    /// Resolve a list of options and fill a dropdown.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringListExample.cs"/>
    /// </example>
    /// <returns>The localized values, in list order.</returns>
    /// <seealso cref="GetLocalizedListAsync"/>
    List<string> GetLocalizedList();

    /// <summary>
    /// Resolves the localized values, loading tables as needed.
    /// </summary>
    /// <remarks>
    /// Waits for initialization and any table loads before returning, so it works with sources the synchronous
    /// accessors cannot reach.
    /// </remarks>
    /// <example>
    /// Resolve a list of options and fill a dropdown.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringListExample.cs"/>
    /// </example>
    /// <returns>The localized values, in list order.</returns>
    /// <seealso cref="GetLocalizedList"/>
    Awaitable<List<string>> GetLocalizedListAsync();
}

/// <summary>
/// A localized string list stored as one table entry whose value is split on a separator.
/// </summary>
/// <remarks>
/// The referenced entry holds every item in one value, for example <c>Low,Medium,High</c>, and the list splits it on
/// <see cref="Separator"/>. One entry keeps the items together for translators, who can reorder them per language.
/// Splitting does not trim the value, so spaces around the separator stay part of the items.
/// </remarks>
/// <example>
/// Resolve a list of options and fill a dropdown.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringListExample.cs"/>
/// </example>
/// <seealso cref="ILocalizedStringList"/>
/// <seealso cref="LocalizedStringGroup"/>
[Serializable]
public class LocalizedStringList : LocalizedString, ILocalizedStringList
{
    [SerializeField] string m_Separator = ",";

    ListChangeHandler m_ListChanged;

    /// <summary>
    /// The text that separates the items inside the entry's value.
    /// </summary>
    public string Separator
    {
        get => m_Separator;
        set => m_Separator = value;
    }

    /// <inheritdoc/>
    public event ListChangeHandler ListChanged
    {
        add
        {
            if (value == null)
                return;
            var first = m_ListChanged == null;
            m_ListChanged += value;
            if (first)
                StringChanged += OnStringChanged;
        }
        remove
        {
            m_ListChanged -= value;
            if (m_ListChanged == null)
                StringChanged -= OnStringChanged;
        }
    }

    /// <inheritdoc/>
    public List<string> GetLocalizedList() => Split(GetLocalizedString());

    /// <inheritdoc/>
    public async Awaitable<List<string>> GetLocalizedListAsync() => Split(await GetLocalizedStringAsync());

    void OnStringChanged(string value) => m_ListChanged?.Invoke(Split(value));

    List<string> Split(string value)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(value))
            return result;
        if (string.IsNullOrEmpty(m_Separator))
        {
            result.Add(value);
            return result;
        }
        result.AddRange(value.Split(m_Separator));
        return result;
    }
}

/// <summary>
/// A localized string list that combines several localized string references.
/// </summary>
/// <remarks>
/// Each item is its own <see cref="LocalizedString"/>, so the items can come from different entries or collections.
/// <see cref="ListChanged"/> is raised once every item has resolved, and again when any item's value changes. The
/// subscription does not observe edits to <see cref="Strings"/>; unsubscribe and resubscribe after changing the list.
/// </remarks>
/// <example>
/// Aggregate several entries into one list.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringGroupExample.cs"/>
/// </example>
/// <seealso cref="ILocalizedStringList"/>
/// <seealso cref="LocalizedStringList"/>
[Serializable]
public class LocalizedStringGroup : ILocalizedStringList
{
    struct ChildSlot
    {
        public LocalizedString Reference;
        public LocalizedString.ChangeHandler Handler;
        public string Value;
        public bool HasValue;
    }

    [SerializeField] List<LocalizedString> m_Strings = new();

    ListChangeHandler m_ListChanged;
    ChildSlot[] m_Children;
    int m_Resolved;

    /// <summary>
    /// The localized strings that make up the list, in list order.
    /// </summary>
    public List<LocalizedString> Strings => m_Strings;

    /// <inheritdoc/>
    public event ListChangeHandler ListChanged
    {
        add
        {
            if (value == null)
                return;
            var first = m_ListChanged == null;
            m_ListChanged += value;
            if (first)
                Subscribe();
        }
        remove
        {
            m_ListChanged -= value;
            if (m_ListChanged == null)
                Unsubscribe();
        }
    }

    /// <inheritdoc/>
    public List<string> GetLocalizedList()
    {
        var result = new List<string>(m_Strings.Count);
        foreach (var reference in m_Strings)
            result.Add(reference != null ? reference.GetLocalizedString() : string.Empty);
        return result;
    }

    /// <inheritdoc/>
    public async Awaitable<List<string>> GetLocalizedListAsync()
    {
        var result = new List<string>(m_Strings.Count);
        foreach (var reference in m_Strings)
            result.Add(reference != null ? await reference.GetLocalizedStringAsync() : string.Empty);
        return result;
    }

    void Subscribe()
    {
        var count = m_Strings.Count;
        m_Children = new ChildSlot[count];
        m_Resolved = 0;
        for (var i = 0; i < count; i++)
        {
            var reference = m_Strings[i];
            if (reference == null)
            {
                m_Children[i].Value = string.Empty;
                m_Children[i].HasValue = true;
                m_Resolved++;
                continue;
            }
            var index = i;
            m_Children[i].Reference = reference;
            m_Children[i].Handler = value => OnChildChanged(index, value);
            reference.StringChanged += m_Children[i].Handler;
        }
        if (count == 0)
            m_ListChanged?.Invoke(new List<string>());
    }

    void Unsubscribe()
    {
        if (m_Children == null)
            return;
        // Detach from the references captured at subscribe time; Strings may have been edited since.
        foreach (var child in m_Children)
        {
            if (child.Handler != null)
                child.Reference.StringChanged -= child.Handler;
        }
        m_Children = null;
        m_Resolved = 0;
    }

    void OnChildChanged(int index, string value)
    {
        if (m_Children == null || index >= m_Children.Length)
            return;
        m_Children[index].Value = value;
        if (!m_Children[index].HasValue)
        {
            m_Children[index].HasValue = true;
            m_Resolved++;
        }
        if (m_Resolved < m_Children.Length)
            return;
        var values = new List<string>(m_Children.Length);
        foreach (var child in m_Children)
            values.Add(child.Value);
        m_ListChanged?.Invoke(values);
    }
}
