// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Represents a single selector in a <see cref="Placeholder" />.
/// E.g.: {selector0.selector1?.selector2}, while "." and "?." and "?[]" are operators.
/// </summary>
public class Selector : FormatItem
{
    string m_OperatorCache;

    /// <summary>
    /// The start index of the <see cref="Operator"/> inside of a <see cref="Selector"/>.
    /// </summary>
    internal int OperatorStartIndex { get; private set; }

    /// <summary>
    /// Gets the length of the operator.
    /// </summary>
    internal int OperatorLength => StartIndex - OperatorStartIndex;

    /// <summary>
    /// Initializes this <see cref="Selector"/> instance.
    /// </summary>
    /// <param name="settings">Formatter and parser settings.</param>
    /// <param name="parent">Parent <see cref="FormatItem"/>.</param>
    /// <param name="baseString">Input format string.</param>
    /// <param name="startIndex">Start index of the selector inside the <see cref="FormatItem.BaseString"/>.</param>
    /// <param name="endIndex">End index of the selector inside the <see cref="FormatItem.BaseString"/>.</param>
    /// <param name="operatorStartIndex">Start index of the operator that precedes the selector.</param>
    /// <param name="selectorIndex">Index of the selector within its <see cref="Placeholder"/>.</param>
    /// <returns>This <see cref="Selector"/> instance.</returns>
    public Selector Initialize(SmartSettings settings, FormatItem parent, string baseString, int startIndex, int endIndex, int operatorStartIndex,
        int selectorIndex)
    {
        base.Initialize(settings, parent, baseString, startIndex, endIndex);
        SelectorIndex = selectorIndex;
        OperatorStartIndex = operatorStartIndex;
        return this;
    }

    /// <summary>
    /// Clears the <see cref="Selector"/>.
    /// This method gets called by <see cref="SelectorPool"/> when it releases an instance.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        SelectorIndex = 0;
        OperatorStartIndex = 0;
        m_OperatorCache = null;
    }

    /// <summary>
    /// The index of the selector in a multi-part selector.
    /// Example: {Person.Birthday.Year} has 3 selectors,
    /// and Year has a SelectorIndex of 2.
    /// </summary>
    public int SelectorIndex { get; private set; }

    /// <summary>
    /// Gets the operator characters.
    /// </summary>
    /// <remarks>
    /// The operator that came between selectors is typically ("." or "?.")
    /// </remarks>
    public string Operator => m_OperatorCache ??= BaseString.Substring(OperatorStartIndex, OperatorLength);
}
