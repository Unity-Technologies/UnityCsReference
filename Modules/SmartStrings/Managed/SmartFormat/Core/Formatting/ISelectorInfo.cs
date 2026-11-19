// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System.ComponentModel;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Contains all the necessary information for evaluating a selector.
/// </summary>
/// <remarks>
/// When evaluating "{Items.Length}",
/// the CurrentValue might be Items, and the Selector would be "Length".
/// The job of an ISource is to set CurrentValue to Items.Length.
/// </remarks>
public interface ISelectorInfo
{
    /// <summary>
    /// The current value to evaluate.
    /// </summary>
    object CurrentValue { get; }

    /// <summary>
    /// The selector to evaluate
    /// </summary>
    string SelectorText { get; }

    /// <summary>
    /// The index of the selector in a multi-part selector.
    /// Example: {Person.Birthday.Year} has 3 selectors,
    /// and Year has a SelectorIndex of 2.
    /// </summary>
    int SelectorIndex { get; }

    /// <summary>
    /// The operator that came before the selector; typically "."
    /// </summary>
    string SelectorOperator { get; }

    /// <summary>
    /// The result of evaluating the selector.
    /// </summary>
    object Result { get; set; }

    /// <summary>
    /// Contains all the details about the current placeholder.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    Placeholder Placeholder { get; }

    /// <summary>
    /// Infrequently used details, often used for debugging
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    FormatDetails FormatDetails { get; }
}
