// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using UnityEngine.Serialization;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Outputs part of an input string.
/// </summary>
[Serializable]
public class SubStringFormatter : FormatterBase
{
    [FormerlySerializedAs("m_ParameterDelimiter ")]
    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = ',';

    [Tooltip("The string to display for NULL values")]
    [SerializeField] string m_NullDisplayString = "(null)";

    [Tooltip("The behavior when start index and/or length are too big")]
    [SerializeField] SubStringOutOfRangeBehavior m_OutOfRangeBehavior = SubStringOutOfRangeBehavior.ReturnEmptyString;

    /// <inheritdoc/>
    public override string DefaultName => "substr";

    /// <summary>
    /// The character used to split the option text literals.
    /// Valid characters are: | (pipe) , (comma)  ~ (tilde)
    /// </summary>
    public char SplitChar
    {
        get => m_SplitChar;
        set => m_SplitChar = Utilities.Validation.GetValidSplitCharOrThrow(value);
    }

    /// <summary>
    /// The string to display for NULL values, defaults to <c>(null)</c>.
    /// 
    /// It will <b>not</b> be used, if a format option is provided to the formatter.
    /// In this case, the child formatter must handle the NULL result.
    /// 
    /// </summary>
    public string NullDisplayString { get => m_NullDisplayString; set => m_NullDisplayString = value; }

    /// <summary>
    /// The behavior when start index and/or length are too big, defaults to <see cref="SubStringOutOfRangeBehavior.ReturnEmptyString"/>.
    /// </summary>
    public SubStringOutOfRangeBehavior OutOfRangeBehavior { get => m_OutOfRangeBehavior; set => m_OutOfRangeBehavior = value; }

    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var parameters = formattingInfo?.FormatterOptions?.Split(SplitChar);
        if (formattingInfo.CurrentValue is not(string or null) || parameters.Length == 1 && parameters[0].Length == 0)
        {
            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder?.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException($"Formatter named '{formattingInfo.Placeholder.FormatterName}' requires at least 1 formatter option and a string? argument.");
        }

        var currentValue = formattingInfo.CurrentValue?.ToString();

        var substring = currentValue == null ? ReadOnlySpan<char>.Empty : GetSubstring(currentValue.AsSpan(), parameters);

        var format = formattingInfo.Format;
        // A format was supplied, so use it if valid
        if (format is not null && format.Length > 0)
        {
            if (!format.HasNested)
                throw new FormattingException(formattingInfo.Format, "The format requires a nested placeholder",
                    format.StartIndex);

            formattingInfo.FormatAsChild(format, currentValue == null ? null : substring.ToString());
            return true;
        }

        // Just output the substring directly
        if (currentValue == null)
        {
            formattingInfo.Write(NullDisplayString);
            return true;
        }

        formattingInfo.Write(substring);

        return true;
    }

    ReadOnlySpan<char> GetSubstring(ReadOnlySpan<char> currentValue, string[] parameters)
    {
        var(startPos, length) = GetStartAndLength(currentValue, parameters);

        switch (OutOfRangeBehavior)
        {
            case SubStringOutOfRangeBehavior.ReturnEmptyString:
                if (startPos + length > currentValue.Length)
                    length = 0;
                break;
            case SubStringOutOfRangeBehavior.ReturnStartIndexToEndOfString:
                if (startPos + length > currentValue.Length)
                    length = currentValue.Length - startPos;
                break;
        }

        // SubStringOutOfRangeBehavior.ThrowException:
        // Without prior adjustments, this may throw
        return parameters.Length > 1
            ? currentValue.Slice(startPos, length)
            : currentValue.Slice(startPos);
    }

    static (int startPos, int length) GetStartAndLength(ReadOnlySpan<char> currentValue, string[] parameters)
    {
        var startPos = int.Parse(parameters[0]);
        var length = parameters.Length > 1 ? int.Parse(parameters[1]) : 0;
        if (startPos < 0)
            startPos = currentValue.Length + startPos;
        if (startPos > currentValue.Length)
            startPos = currentValue.Length;
        if (length < 0)
            length = currentValue.Length - startPos + length;

        return (startPos, length);
    }

    /// <summary>
    /// Specifies behavior when start index and/or length is out of range.
    /// </summary>
    public enum SubStringOutOfRangeBehavior
    {
        /// <summary>
        /// Returns an empty string.
        /// </summary>
        ReturnEmptyString,
        /// <summary>
        /// Returns the remainder of the string, starting at StartIndex
        /// </summary>
        ReturnStartIndexToEndOfString,
        /// <summary>
        /// Throws <see cref="FormattingException"/>
        /// </summary>
        ThrowException
    }
}
