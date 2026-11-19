// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Formats <see langword="null"/> values.
/// </summary>
/// <remarks>
/// <code>
/// Smart.Format("{0:isnull:It's null}", arg)
/// Smart.Format("{0:isnull:It's null|Not null}", arg)
/// Smart.Format("{0:isnull:It's null|{}}", arg)
/// </code>
/// </remarks>
[Serializable]
public class NullFormatter : FormatterBase, IFormatterLiteralExtractor
{
    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = '|';

    /// <inheritdoc/>
    public override string DefaultName => "isnull";

    /// <summary>
    /// The character used to split the option text literals.
    /// Valid characters are: | (pipe) , (comma)  ~ (tilde)
    /// </summary>
    public char SplitChar
    {
        get => m_SplitChar;
        set => m_SplitChar = Utilities.Validation.GetValidSplitCharOrThrow(value);
    }

    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var currentValue = formattingInfo.CurrentValue;
        var chooseOptions = formattingInfo.FormatterOptions.AsSpan().Trim();
        var formats = formattingInfo.Format?.Split(SplitChar);

        // Check whether arguments can be handled by this formatter
        if (chooseOptions.Length != 0)
        {
            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException($"Formatter named '{formattingInfo.Placeholder?.FormatterName}' does not allow choose options");
        }

        if (formats is null || formats.Count is < 1 or > 2)
        {
            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException(
                $"Formatter named '{formattingInfo.Placeholder.FormatterName}' must have 1 or 2 format options");
        }

        // Use the format for null
        // UNITY - Special handling for Unity Objects which need to use the == operator
        if (currentValue is null || currentValue is UnityEngine.Object unityObject && unityObject == null)
        {
            formattingInfo.FormatAsChild(formats[0], currentValue);
            return true;
        }

        // Use the format for a value other than null
        if (formats.Count > 1)
        {
            formattingInfo.FormatAsChild(formats[1], currentValue);
            return true;
        }

        // There is no format for a value other than null
        formattingInfo.Write(ReadOnlySpan<char>.Empty);

        return true;
    }

    void IFormatterLiteralExtractor.WriteAllLiterals(IFormattingInfo formattingInfo)
    {
        var chooseOptions = formattingInfo.FormatterOptions.AsSpan().Trim();
        var formats = formattingInfo.Format?.Split(SplitChar);

        if (chooseOptions.Length != 0 || formats is null || formats.Count is < 1 or > 2)
            return;

        // Null value
        formattingInfo.FormatAsChild(formats[0], null);

        // Other than null value
        if (formats.Count > 1)
        {
            formattingInfo.FormatAsChild(formats[1], null);
            return;
        }
    }
}
