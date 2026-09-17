// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Formats values by matching them against a regular expression, and can output matching group values.
/// </summary>
/// <remarks>
/// <code>
/// Syntax:
///   {value:ismatch(regex): format | default}
///
/// Or in context of a list:
///   {myList:list:{:ismatch(^regex$):{:format}|'no match'}|, | and }
///
/// Or with output of the first matching group value:
///   {value:ismatch(regex):First match in '{}'\\: {m[1]}|No match}
/// </code>
/// </remarks>
[Serializable]
public class IsMatchFormatter : FormatterBase, IInitializer, IFormatterLiteralExtractor
{
    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = '|';
    [SerializeField] RegexOptions m_RegexOptions;

    [Tooltip("The name of the placeholder used to output RegEx matching group values")]
    [SerializeField] string m_PlaceholderNameForMatches = "m";

    readonly ConcurrentDictionary<string, Regex> m_RegexCache = new();

    /// <inheritdoc/>
    public override string DefaultName => "ismatch";

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
        // Cannot deal with null
        if (formattingInfo.CurrentValue is null)
            return false;

        var expression = formattingInfo.FormatterOptions;
        var formats = formattingInfo.Format?.Split(SplitChar);

        // Check whether arguments can be handled by this formatter
        if (formats is null || formats.Count != 2)
        {
            if (formats?.Count == 0)
                return true;

            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder?.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException(
                $"Formatter named '{formattingInfo.Placeholder?.FormatterName}' requires at least 2 format options.");
        }

        var regEx = m_RegexCache.GetOrAdd(expression, static (expr, options) => new Regex(expr, options), m_RegexOptions);
        var match = regEx.Match(formattingInfo.CurrentValue.ToString());

        if (!match.Success)
        {
            // Output the "no match" part of the format
            if (formats.Count == 2)
                formattingInfo.FormatAsChild(formats[1], formattingInfo.CurrentValue);

            return true;
        }

        // Match successful

        // If we have child Placeholders in the format, we want to use the matches for the output
        var matchingGroupValues = new List<string>(match.Groups.Count);
        foreach (Group grp in match.Groups)
            matchingGroupValues.Add(grp.Value);

        // Output the successful match part of the format
        foreach (var formatItem in formats[0].Items)
        {
            if (formatItem is Placeholder ph)
            {
                var variable = new KeyValuePair<string, object>(PlaceholderNameForMatches, matchingGroupValues);
                Format(formattingInfo, ph, variable);
                continue;
            }

            // so it must be a literal
            var literalText = (LiteralText)formatItem;
            // On Dispose() the Format goes back to the object pool
            using var childFormat = formattingInfo.Format?.Substring(literalText.StartIndex - formattingInfo.Format.StartIndex, literalText.Length);
            if (childFormat is null) continue;
            formattingInfo.FormatAsChild(childFormat, formattingInfo.CurrentValue);
        }

        return true;
    }

    void Format(IFormattingInfo formattingInfo, Placeholder placeholder, object matchingGroupValues)
    {
        // On Dispose() the Format goes back to the object pool
        using var childFormat =
                formattingInfo.Format?.Substring(placeholder.StartIndex - formattingInfo.Format.StartIndex,
                    placeholder.Length);
        if (childFormat is null) return;

        // Is the placeholder a "magic IsMatchFormatter" one?
        if (placeholder.Selectors.Count > 0 && placeholder.Selectors[0]?.RawText == PlaceholderNameForMatches)
        {
            // The nested placeholder will output the matching group values
            formattingInfo.FormatAsChild(childFormat, matchingGroupValues);
        }
        else
        {
            formattingInfo.FormatAsChild(childFormat, formattingInfo.CurrentValue);
        }
    }

    /// <summary>
    /// The <see cref="RegexOptions"/> for the <see cref="Regex"/> expression.
    /// </summary>
    public RegexOptions RegexOptions
    {
        get => m_RegexOptions;
        set
        {
            m_RegexOptions = value;
            m_RegexCache.Clear();
        }
    }

    /// <summary>
    /// The name of the placeholder used to output RegEx matching group values.
    /// 
    /// Example:<br/>
    /// {value:ismatch(regex):First match in '{}'\\: {m[1]}|No match}<br/>
    /// "m" is the PlaceholderNameForMatches
    /// 
    /// </summary>
    public string PlaceholderNameForMatches { get => m_PlaceholderNameForMatches; set => m_PlaceholderNameForMatches = value; }

    ///<inheritdoc/>
    public void Initialize(SmartFormatter smartFormatter)
    {
        // The extension is needed to output the values of RegEx matching groups
        if (smartFormatter.GetSourceExtension<KeyValuePairSource>() is null)
            smartFormatter.AddExtensions(new KeyValuePairSource());
    }

    void IFormatterLiteralExtractor.WriteAllLiterals(IFormattingInfo formattingInfo)
    {
        var formats = formattingInfo.Format?.Split(SplitChar);

        if (formats == null || formats.Count != 2)
            return;

        formattingInfo.FormatAsChild(formats[0], formattingInfo.CurrentValue);
        formattingInfo.FormatAsChild(formats[1], formattingInfo.CurrentValue);
    }
}
