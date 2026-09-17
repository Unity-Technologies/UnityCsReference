// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Buffers;
using System.Collections.Generic;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// A placeholder is the part of a format string between the {braces}.
/// </summary>
/// <remarks>
/// For example, in "{Items.Length,-10:choose(1|2|3):one|two|three}",
/// the <see cref="Alignment" />s is "-10",
/// the <see cref="Selector" />s are "Items" and "Length", separated by the dot "Operator".
/// the <see cref="FormatterName" /> is "choose",
/// the <see cref="FormatterOptionsRaw" /> is "1|2|3",
/// and the <see cref="Format" /> is "one|two|three".
/// </remarks>
public class Placeholder : FormatItem
{
    string m_FormatterNameCache;
    string m_FormatterOptionsCache;
    string m_FormatterOptionsRawCache;
    string m_ToStringCache;
    readonly List<Selector> m_Selectors = new();

    /// <summary>
    /// Initializes the instance of <see cref="Placeholder"/>.
    /// </summary>
    /// <param name="parent">Parent <see cref="Format"/> of the placeholder.</param>
    /// <param name="startIndex">Index inside the input string where the placeholder starts.</param>
    /// <param name="nestedDepth">Nesting level of this placeholder.</param>
    /// <returns>This <see cref="Placeholder"/> instance.</returns>
    public Placeholder Initialize(Format parent, int startIndex, int nestedDepth)
    {
        base.Initialize(parent.SmartSettings, parent, parent.BaseString, startIndex, parent.EndIndex);

        // inherit alignment
        if (parent.ParentPlaceholder != null) Alignment = parent.ParentPlaceholder.Alignment;
        NestedDepth = nestedDepth;
        FormatterNameStartIndex = startIndex;
        FormatterNameLength = 0;
        FormatterOptionsStartIndex = startIndex;
        FormatterOptionsLength = 0;

        return this;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        base.Clear();

        m_FormatterNameCache = null;
        m_FormatterOptionsCache = null;
        m_FormatterOptionsRawCache = null;
        m_ToStringCache = null;

        NestedDepth = 0;
        Alignment = 0;
        FormatterNameStartIndex = 0;
        FormatterNameLength = 0;
        FormatterOptionsStartIndex = 0;
        FormatterOptionsLength = 0;
    }

    /// <summary>
    /// Return items we own to the object pools.
    /// This method gets called by <see cref="LiteralTextPool"/> when it releases an instance.
    /// </summary>
    public void ReturnToPool()
    {
        Clear();

        if (Format != null)
        {
            FormatPool.Pool.Release(Format);
            Format = null;
        }

        foreach (var selector in Selectors)
        {
            selector.Clear();
            SelectorPool.Pool.Release(selector);
        }
        Selectors.Clear();
    }

    /// <summary>
    /// Gets the parent <see cref="Parsing.Format"/>.
    /// </summary>
    public Format Parent => (Format)ParentFormatItem;

    /// <summary>
    /// Gets or sets the nesting level of the <see cref="Placeholder"/>.
    /// </summary>
    public int NestedDepth { get; set; }

    /// <summary>
    /// Gets a list of all <see cref="Selector"/> within the <see cref="Placeholder"/>.
    /// </summary>
    internal List<Selector> Selectors => m_Selectors;

    /// <summary>
    /// Gets an <see cref="IReadOnlyList{T}"/> of all <see cref="Selector"/>s within the <see cref="Placeholder"/>.
    /// </summary>
    /// <returns>A read-only list of the <see cref="Selector"/>s within the <see cref="Placeholder"/>.</returns>
    public IReadOnlyList<Selector> GetSelectors()
    {
        return m_Selectors.AsReadOnly();
    }

    /// <summary>
    /// Add a new <see cref="Selector"/> to the list <see cref="Selectors"/>.
    /// If the <see cref="Selector"/> has an alignment operator, the <see cref="Alignment"/> will be set.
    /// </summary>
    /// <param name="selector">The <see cref="Selector"/> to add.</param>
    internal void AddSelector(Selector selector)
    {
        // 1. The operator character must have a value, usually ','
        // 2. The alignment is an integer value
        if (selector.OperatorLength > 0
            && selector.Operator[0] == SmartSettings.Parser.AlignmentOperator
            && int.TryParse(selector.RawText, out var alignment))
        {
            Alignment = alignment;
        }

        m_Selectors.Add(selector);
    }

    /// <summary>
    /// Gets or sets the <see cref="Alignment"/> of the result string,
    /// used like with string.Format("{0,-10}"), where -10 is the alignment.
    /// </summary>
    public int Alignment { get; internal set; }

    /// <summary>
    /// Gets or sets the start index of the <see cref="FormatterName"/> within the <see cref="FormatItem.BaseString"/>
    /// </summary>
    internal int FormatterNameStartIndex { get; set; }

    /// <summary>
    /// Gets or sets the length of the <see cref="FormatterName"/> within the <see cref="FormatItem.BaseString"/>
    /// </summary>
    internal int FormatterNameLength { get; set; }

    /// <summary>
    /// Gets or sets the start index of the <see cref="FormatterOptions"/> within the <see cref="FormatItem.BaseString"/>
    /// </summary>
    internal int FormatterOptionsStartIndex { get; set; }

    /// <summary>
    /// Gets or sets the length of the <see cref="FormatterOptions"/> within the <see cref="FormatItem.BaseString"/>
    /// </summary>
    internal int FormatterOptionsLength { get; set; }

    /// <summary>
    /// Gets the name of the formatter.
    /// </summary>
    public string FormatterName => m_FormatterNameCache ??= BaseString.Substring(FormatterNameStartIndex, FormatterNameLength);

    /// <summary>
    /// Gets the formatter option string unescaped.
    /// To get the raw formatter option string, <see cref="FormatterOptionsRaw"/>.
    /// </summary>
    public string FormatterOptions
    {
        get
        {
            if (m_FormatterOptionsCache != null) return m_FormatterOptionsCache;
            if (Length == 0) m_FormatterOptionsCache = string.Empty;

            // It's enough to have a buffer with the same size as input length.
            // The default max array length of ArrayPool<char>.Shared is 1,048,576.
            var pool = ArrayPool<char>.Create(Length > 1024? Length : 1024, 1024);
            var resultBuffer = pool.Rent(Length);
            System.Diagnostics.Debug.Assert(resultBuffer.Length >= Length, "ArrayPool buffer size is smaller than it should be");

            try
            {
                m_FormatterOptionsCache = EscapedLiteral
                    .UnEscapeCharLiterals(SmartSettings.Parser.CharLiteralEscapeChar, BaseString.AsSpan(FormatterOptionsStartIndex, FormatterOptionsLength), true, resultBuffer).ToString();
            }
            finally
            {
                pool.Return(resultBuffer);
            }

            return m_FormatterOptionsCache;
        }
    }

    /// <summary>
    /// Gets the raw formatter option string as in the input format string (unescaped).
    /// </summary>
    public string FormatterOptionsRaw => m_FormatterOptionsRawCache ??= BaseString.Substring(FormatterOptionsStartIndex, FormatterOptionsLength);

    /// <summary>
    /// Gets or sets the <see cref="Format"/> of the <see cref="Placeholder"/>.
    /// </summary>
    public Format Format { get; set; }

    /// <summary>
    /// Gets the string representation of the <see cref="Placeholder"/> with all parsed components.
    /// </summary>
    /// <returns>The string representation of the <see cref="Placeholder"/> with all parsed components.</returns>
    public override string ToString()
    {
        if (m_ToStringCache != null) return m_ToStringCache;

        var sb = new System.Text.StringBuilder(Length);
        sb.Append(SmartSettings.Parser.PlaceholderBeginChar);
        foreach (var s in Selectors)
        {
            // alignment operators will be appended later
            if (s.Operator.Length > 0 && s.Operator[0] == SmartSettings.Parser.AlignmentOperator) continue;

            sb.Append(s.BaseString.AsSpan(s.OperatorStartIndex, s.EndIndex - s.OperatorStartIndex));
        }
        if (Alignment != 0)
        {
            sb.Append(SmartSettings.Parser.AlignmentOperator);
            sb.Append(Alignment);
        }

        if (FormatterName != string.Empty)
        {
            sb.Append(SmartSettings.Parser.FormatterNameSeparator);
            sb.Append(FormatterName);
            if (FormatterOptions != string.Empty)
            {
                sb.Append(SmartSettings.Parser.FormatterOptionsBeginChar);
                sb.Append(FormatterOptions);
                sb.Append(SmartSettings.Parser.FormatterOptionsEndChar);
            }
        }

        if (Format != null)
        {
            sb.Append(SmartSettings.Parser.FormatterNameSeparator);
            sb.Append(Format);
        }

        sb.Append(SmartSettings.Parser.PlaceholderEndChar);

        m_ToStringCache = sb.ToString();
        return m_ToStringCache;
    }
}
