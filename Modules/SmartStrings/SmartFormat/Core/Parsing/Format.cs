// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Pooling.SmartPools;
namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Represents a parsed format string.
/// Contains a list of <see cref="FormatItem" />s,
/// including <see cref="LiteralText" />s and <see cref="Placeholder" />s.
/// Note: <see cref="Format"/> is <see cref="IDisposable"/>.
/// </summary>
public sealed class Format : FormatItem, IDisposable
{
    string m_ToStringCache;
    string m_LiteralTextCache;
    bool m_ReturnedToPool;

    /// <summary>
    /// Initializes the <see cref="Format"/> instance.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="baseString">Base format string.</param>
    /// <returns>This <see cref="Format"/> instance.</returns>
    public Format Initialize(SmartSettings smartSettings, string baseString)
    {
        base.Initialize(smartSettings, null, baseString, 0, baseString.Length);
        ParentPlaceholder = null;
        return this;
    }

    /// <summary>
    /// Initializes the instance of <see cref="Format"/>.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="parent">Parent <see cref="Placeholder"/>.</param>
    /// <param name="startIndex">Start index within the format base string.</param>
    /// <returns>This <see cref="Format"/> instance.</returns>
    public Format Initialize(SmartSettings smartSettings, Placeholder parent, int startIndex)
    {
        base.Initialize(smartSettings, parent, parent.BaseString, startIndex, parent.EndIndex);
        ParentPlaceholder = parent;
        return this;
    }

    /// <summary>
    /// Initializes the instance of <see cref="Format"/>.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="baseString">Base format string.</param>
    /// <param name="startIndex">Start index within the format base string.</param>
    /// <param name="endIndex">End index within the format base string.</param>
    /// <returns>This <see cref="Format"/> instance.</returns>
    public Format Initialize(SmartSettings smartSettings, string baseString, int startIndex, int endIndex)
    {
        base.Initialize(smartSettings, null, baseString, startIndex, endIndex);
        ParentPlaceholder = null;
        return this;
    }

    /// <summary>
    /// Initializes the instance of <see cref="Format"/>.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="baseString">Base format string.</param>
    /// <param name="startIndex">Start index within the format base string.</param>
    /// <param name="endIndex">End index within the format base string.</param>
    /// <param name="hasNested"><see langword="true"/> if the nested formats exist.</param>
    /// <returns>This <see cref="Format"/> instance.</returns>
    public Format Initialize(SmartSettings smartSettings, string baseString, int startIndex, int endIndex, bool hasNested)
    {
        base.Initialize(smartSettings, null, baseString, startIndex, endIndex);
        ParentPlaceholder = null;
        HasNested = hasNested;

        return this;
    }

    /// <summary>
    /// Return items we own to the object pools.
    /// This method gets called by <see cref="FormatPool"/> when it releases an instance.
    /// </summary>
    public void ReturnToPool()
    {
        // Clear the format
        Clear();

        if (AdditionalData != null)
        {
            UnityEngine.Pool.GenericPool<AdditionalFormatData>.Release(AdditionalData);
            AdditionalData = null;
        }

        ParentPlaceholder = null;
        HasNested = false;

        // Return and clear FormatItems we own
        foreach (var item in Items)
        {
            if (ReferenceEquals(this, item.ParentFormatItem))
                ReturnFormatItemToPool(item);
        }
        Items.Clear();

        // Return and clear the list of SplitLists
        foreach (var splitList in m_ListOfSplitLists)
        {
            SplitListPool.Pool.Release(splitList);
        }
        m_ListOfSplitLists.Clear();
        // Items of _splitCache are returned via _listOfSplitLists
        m_SplitCache = null;

        m_ToStringCache = null;
        m_LiteralTextCache = null;
    }

    /// <summary>
    /// Gets the parent <see cref="Placeholder"/>.
    /// </summary>
    public Placeholder ParentPlaceholder { get; internal set; }

    /// <summary>
    /// Gets the <see cref="List{T}"/> of <see cref="FormatItem"/>s.
    /// </summary>
    public List<FormatItem> Items { get; } = new();

    /// <summary>
    /// Returns <see langword="true"/>, if the <see cref="Format"/> is nested.
    /// </summary>
    public bool HasNested { get; internal set; }

    /// <summary>
    /// Used to pass and return additional information during formatting.
    /// </summary>
    public AdditionalFormatData AdditionalData { get; set; }

    /// <summary>
    /// Gets a substring of the current <see cref="Format"/>.
    /// </summary>
    /// <param name="start">Start index of the substring.</param>
    /// <returns>The substring of the current <see cref="Format"/>.</returns>
    public Format Substring(int start)
    {
        return Substring(start, Length - start);
    }

    /// <summary>
    /// Gets a substring of the current <see cref="Format"/>.
    /// </summary>
    /// <param name="start">Start index of the substring.</param>
    /// <param name="length">Number of characters in the substring.</param>
    /// <returns>The substring of the current <see cref="Format"/>.</returns>
    public Format Substring(int start, int length)
    {
        start = StartIndex + start;
        var end = start + length;
        ValidateArguments(start, length);

        // If startIndex and endIndex already match this item, we're done:
        if (start == StartIndex && end == EndIndex) return this;

        var substring = FormatPool.Pool.Get().Initialize(SmartSettings, BaseString, start, end);
        foreach (var item in Items)
        {
            if (item.EndIndex <= start)
                continue; // Skip first items
            if (end <= item.StartIndex)
                break; // Done

            var newItem = item;
            if (item is LiteralText)
            {
                // See if we need to slice the LiteralText
                if (start > item.StartIndex || item.EndIndex > end)
                    newItem = LiteralTextPool.Pool.Get().Initialize(substring.SmartSettings, substring,
                        substring.BaseString, Math.Max(start, item.StartIndex), Math.Min(end, item.EndIndex));
            }
            else
            {
                // item is a placeholder -- we can't split a placeholder though.
                substring.HasNested = true;
            }

            substring.Items.Add(newItem);
        }

        return substring;
    }

    void ValidateArguments(int start, int length)
    {
        var end = start + length;
        if (start < StartIndex || start > EndIndex)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (end > EndIndex)
            throw new ArgumentOutOfRangeException(nameof(length));
    }

    /// <summary>
    /// Searches the literal text for the search char.
    /// Does not search in nested placeholders.
    /// </summary>
    /// <param name="search">Character to locate.</param>
    /// <returns>The zero-based index of the first occurrence of the character in the literal text, or -1 if the character is not found.</returns>
    public int IndexOf(char search)
    {
        return IndexOf(search, 0);
    }

    /// <summary>
    /// Searches the literal text for the search char.
    /// Does not search in nested placeholders.
    /// </summary>
    /// <param name="search">Character to locate.</param>
    /// <param name="start">Index to start the search from.</param>
    /// <returns>The zero-based index of the first occurrence of the character in the literal text, or -1 if the character is not found.</returns>
    public int IndexOf(char search, int start)
    {
        start = StartIndex + start;
        foreach (var item in Items)
        {
            if (item.EndIndex < start || item is not LiteralText literalItem) continue;

            if (start < literalItem.StartIndex) start = literalItem.StartIndex;
            var literalIndex =
                literalItem.BaseString.IndexOf(search, start, literalItem.EndIndex - start);
            if (literalIndex != -1) return literalIndex - StartIndex;
        }

        return -1;
    }

    List<int> FindAll(char search, int maxCount)
    {
        var results = UnityEngine.Pool.ListPool<int>.Get();
        var index = 0;
        while (maxCount != 0)
        {
            index = IndexOf(search, index);
            if (index == -1) break;
            results.Add(index);
            index++;
            maxCount--;
        }

        return results;
    }

    // set the default
    char k_SplitCacheChar = '\0';
    // Items of the _splitCache are returned to the pool using _listOfSplitLists
    IList<Format> m_SplitCache;
    readonly List<SplitList> m_ListOfSplitLists = new();

    /// <summary>
    /// Splits the <see cref="Format"/> items by the given search character.
    /// </summary>
    /// <param name="search">Character used to split.</param>
    /// <returns>The list of <see cref="Format"/> segments produced by splitting on the character.</returns>
    public IList<Format> Split(char search)
    {
        if (m_SplitCache == null || k_SplitCacheChar != search)
        {
            k_SplitCacheChar = search;
            m_SplitCache = Split(search, -1);
        }

        return m_SplitCache;
    }

    /// <summary>
    /// Splits the <see cref="Format"/> items by the given search character.
    /// </summary>
    /// <param name="search">Character used to split.</param>
    /// <param name="maxCount">Maximum number of segments to return.</param>
    /// <returns>The list of <see cref="Format"/> segments produced by splitting on the character.</returns>
    public IList<Format> Split(char search, int maxCount)
    {
        var splits = FindAll(search, maxCount);
        var splitList = SplitListPool.Pool.Get().Initialize(this, splits);

        // Keep track of the split lists we create,
        // so that they can be returned to the object pool for later reuse.
        m_ListOfSplitLists.Add(splitList);
        return splitList;
    }

    /// <summary>
    /// Retrieves the literal text contained in this format.
    /// Excludes escaped chars, and does not include the text
    /// of placeholders.
    /// </summary>
    /// <returns>The literal text of this format, excluding escaped characters and placeholder text.</returns>
    public string GetLiteralText()
    {
        if (m_LiteralTextCache != null) return m_LiteralTextCache;

        var sb = new System.Text.StringBuilder(Length + Items.Count * 8);
        foreach (var item in Items)
            if (item is LiteralText literalItem) sb.Append(literalItem.AsSpan());

        m_LiteralTextCache = sb.ToString();
        return m_LiteralTextCache;
    }

    /// <summary>
    /// Reconstructs the format string, but doesn't include escaped chars
    /// and tries to reconstruct placeholders.
    /// </summary>
    public override string ToString()
    {
        if (m_ToStringCache != null) return m_ToStringCache;

        var sb = new System.Text.StringBuilder(Length + Items.Count * 8);
        foreach (var item in Items) sb.Append(item.AsSpan());
        m_ToStringCache = sb.ToString();
        return m_ToStringCache;
    }

    static void ReturnFormatItemToPool(FormatItem formatItem)
    {
        switch (formatItem)
        {
            case LiteralText literal:
                LiteralTextPool.Pool.Release(literal);
                break;

            case Format format:
                FormatPool.Pool.Release(format);
                break;

            case Placeholder placeholder:
                PlaceholderPool.Pool.Release(placeholder);
                break;

            case Selector selector:
                SelectorPool.Pool.Release(selector);
                break;

            default:
                throw new ArgumentException($"Unhandled type '{formatItem.GetType()}'", nameof(formatItem));
        }
    }

    /// <summary>
    /// Returns this instance to the object pool.
    /// Do not use this instance after calling.
    /// </summary>
    /// <param name="disposing"></param>
    void Dispose(bool disposing)
    {
        if (disposing && !m_ReturnedToPool)
        {
            m_ReturnedToPool = true;
            // Clearing this instance is done when returning it to the pool
            FormatPool.Pool.Release(this);
        }
    }

    // Marks this instance live again when the pool hands it out, so Dispose returns it at most once.
    internal void OnTakenFromPool() => m_ReturnedToPool = false;

    /// <summary>
    /// Returns this instance to the object pool, which also clears all objects it owns.
    /// Do not use this instance after calling <see cref="Dispose()"/>
    /// </summary>
    /// <code>
    /// // Example:
    /// var settings = new SmartSettings();
    /// using var formatParsed = new Parser(settings).ParseFormat("inputFormat");
    /// var formatter = new SmartFormatter(settings);
    /// for (var i = 0; i &lt; 10; i++)
    /// {
    ///    var result = formatter.Format(formatParsed, i);
    /// }
    /// </code>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
    ~Format()
    {
        Dispose(false);
    }
#pragma warning restore UA5000
}
