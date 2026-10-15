// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Formatting;

/// <summary>
/// The class contains the fields and methods which are necessary for formatting.
/// </summary>
public class FormattingInfo : IFormattingInfo, ISelectorInfo, IFormattingExtensionsToggle
{
    /// <summary>
    /// Initializes this formatting info with the details required for formatting.
    /// </summary>
    /// <param name="formatDetails">Shared details for the current formatting operation.</param>
    /// <param name="format">Parsed format to evaluate.</param>
    /// <param name="currentValue">Value to format.</param>
    /// <returns>This same instance, initialized and ready for formatting.</returns>
    public FormattingInfo Initialize(FormatDetails formatDetails, Format format, object currentValue)
    {
        return Initialize(null, formatDetails, format, currentValue);
    }

    /// <summary>
    /// Initializes this formatting info as a child of the specified parent.
    /// </summary>
    /// <param name="parent">Parent formatting info to inherit alignment from.</param>
    /// <param name="formatDetails">Shared details for the current formatting operation.</param>
    /// <param name="format">Parsed format to evaluate.</param>
    /// <param name="currentValue">Value to format.</param>
    /// <returns>This same instance, initialized and ready for formatting.</returns>
    public FormattingInfo Initialize(FormattingInfo parent, FormatDetails formatDetails, Format format, object currentValue)
    {
        Parent = parent;
        CurrentValue = currentValue;
        FormatDetails = formatDetails;
        Format = format;
        DisableFormattingExtensions = false;
        // inherit alignment
        if (parent != null) Alignment = parent.Alignment;
        else if (format.ParentPlaceholder != null) Alignment = format.ParentPlaceholder.Alignment;

        return this;
    }

    /// <summary>
    /// Initializes this formatting info from the specified placeholder.
    /// </summary>
    /// <param name="parent">Parent formatting info to inherit alignment from.</param>
    /// <param name="formatDetails">Shared details for the current formatting operation.</param>
    /// <param name="placeholder">Placeholder that supplies the format and alignment.</param>
    /// <param name="currentValue">Value to format.</param>
    /// <returns>This same instance, initialized and ready for formatting.</returns>
    public FormattingInfo Initialize(FormattingInfo parent, FormatDetails formatDetails, Placeholder placeholder,
        object currentValue)
    {
        Parent = parent;
        FormatDetails = formatDetails;
        Placeholder = placeholder;
        Format = placeholder.Format;
        DisableFormattingExtensions = false;
        CurrentValue = currentValue;
        // inherit alignment
        Alignment = placeholder.Alignment;

        return this;
    }

    /// <summary>
    /// Returns this instance and its <see cref="FormattingInfo"/> children to the object pool.
    /// This method gets called by <see cref="FormattingInfoPool"/> when it releases an instance.
    /// </summary>
    public void ReturnToPool()
    {
        Parent = null;
        // Assign new value, but leave existing references untouched
        FormatDetails = null;
        Placeholder = null;
        Selector = null;
        Alignment = 0;

        Format = null;
        DisableFormattingExtensions = false;
        CurrentValue = null;

        // Children can safely be returned
        foreach (var c in Children)
        {
            FormattingInfoPool.Pool.Release(c);
        }

        Children.Clear();
    }

    /// <summary>
    /// The parent <see cref="FormattingInfo"/> that created this instance.
    /// </summary>
    public FormattingInfo Parent { get; private set; }

    /// <summary>
    /// The <see cref="Parsing.Selector"/> currently being evaluated.
    /// </summary>
    public Selector Selector { get; internal set; }

    /// <summary>
    /// Extra details shared across the current formatting operation.
    /// </summary>
    public FormatDetails FormatDetails { get; private set; }

    /// <summary>
    /// The current value being formatted.
    /// </summary>
    public object CurrentValue { get; set; }

    /// <summary>
    /// The <see cref="Parsing.Placeholder"/> currently being formatted.
    /// </summary>
    public Placeholder Placeholder { get; internal set; }

    /// <summary>
    /// The alignment of the current <see cref="Parsing.Placeholder"/>, or, if that is
    /// <see langword="null"/>, the alignment inherited from the nearest parent
    /// <see cref="IFormattingInfo"/> whose alignment is not zero.
    /// </summary>
    public int Alignment { get; set; }

    /// <summary>
    /// The formatter options of the current <see cref="Parsing.Placeholder"/>.
    /// </summary>
    public string FormatterOptions => Placeholder?.FormatterOptions;

    /// <summary>
    /// The parsed format that specifies how to output the current value.
    /// </summary>
    public Format Format { get; private set; }

    /// <summary>
    /// Gets the list of child <see cref="FormattingInfo"/>s created by this instance.
    /// </summary>
    internal List<FormattingInfo> Children { get; } = new();

    /// <summary>
    /// Writes the <see cref="string"/> parameter to the <see cref="Output.IOutput"/>
    /// and takes care of alignment.
    /// </summary>
    /// <param name="text">The string to write to the <see cref="Output.IOutput"/></param>
    public void Write(string text)
    {
        if (Alignment > 0) PreAlign(text.Length);
        FormatDetails.Output.Write(text, this);
        if (Alignment < 0) PostAlign(text.Length);
    }

    /// <summary>
    /// Writes the <see cref="ReadOnlySpan{T}"/> text parameter to the <see cref="Output.IOutput"/>
    /// and takes care of alignment.
    /// </summary>
    /// <param name="text">The string to write to the <see cref="Output.IOutput"/></param>

    public void Write(ReadOnlySpan<char> text)
    {
        if (Alignment > 0) PreAlign(text.Length);
        FormatDetails.Output.Write(text, this);
        if (Alignment < 0) PostAlign(text.Length);
    }

    /// <summary>
    /// Creates a child <see cref="IFormattingInfo"/> from the current <see cref="IFormattingInfo"/> instance
    /// and invokes formatting with <see cref="SmartFormatter"/> and with the child as parameter.
    /// </summary>
    /// <param name="format">The <see cref="Format"/> to use.</param>
    /// <param name="value">The value for the item in the format.</param>
    public void FormatAsChild(Format format, object value)
    {
        var nestedFormatInfo = CreateChild(format, value);
        // recursive method call
        FormatDetails.Formatter.Format(nestedFormatInfo);
    }

    /// <summary>
    /// Creates a new <see cref="FormattingException"/>.
    /// </summary>
    /// <param name="issue">The text which goes to the <see cref="Exception.Message"/>.</param>
    /// <param name="problemItem">The <see cref="FormatItem"/> which caused the problem.</param>
    /// <param name="startIndex">The start index in the input format string.</param>
    /// <returns>A new <see cref="FormattingException"/> that describes the formatting issue.</returns>
    public FormattingException FormattingException(string issue, FormatItem problemItem = null, int startIndex = -1)
    {
        problemItem ??= Format;
        if (startIndex == -1) startIndex = problemItem.StartIndex;
        return new FormattingException(problemItem, issue, startIndex);
    }

    /// <summary>
    /// The raw text of the current <see cref="Parsing.Selector"/>.
    /// </summary>
    public string SelectorText => Selector.RawText;

    /// <summary>
    /// The index of the current <see cref="Parsing.Selector"/> in the selector list.
    /// </summary>
    public int SelectorIndex => Selector.SelectorIndex;

    /// <summary>
    /// The operator string of the current <see cref="Parsing.Selector"/>, for example a comma or dot.
    /// </summary>
    public string SelectorOperator => Selector.Operator;

    /// <summary>
    /// The result after an <see cref="ISource"/> has assigned a value.
    /// </summary>
    public object Result { get; set; }

    /// <inheritdoc />
    public bool DisableFormattingExtensions { get; set; }

    FormattingInfo CreateChild(Format format, object currentValue)
    {
        var fi = FormattingInfoPool.Pool.Get().Initialize(this, FormatDetails, format, currentValue);
        Children.Add(fi);
        return fi;
    }

    /// <summary>
    /// Creates a child <see cref="IFormattingInfo"/> from the current <see cref="IFormattingInfo"/> instance for a <see cref="Placeholder"/>.
    /// </summary>
    /// <param name="placeholder">The <see cref="Placeholder"/> used for creating a child <see cref="IFormattingInfo"/>.</param>
    /// <returns>The child <see cref="IFormattingInfo"/>.</returns>
    public FormattingInfo CreateChild(Placeholder placeholder)
    {
        var fi = FormattingInfoPool.Pool.Get().Initialize(this, FormatDetails, placeholder, CurrentValue);
        Children.Add(fi);
        return fi;
    }

    void PreAlign(int textLength)
    {
        var filler = Alignment - textLength;
        if (filler > 0) FormatDetails.Output.Write(FormatDetails.Settings.Formatter.AlignmentFillCharacter, filler, this);
    }

    void PostAlign(int textLength)
    {
        var filler = -Alignment - textLength;
        if (filler > 0) FormatDetails.Output.Write(FormatDetails.Settings.Formatter.AlignmentFillCharacter, filler, this);
    }
}
