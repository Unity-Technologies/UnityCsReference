// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Assertions;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Outputs one of several literal options, selected according to the input value.
/// </summary>
[Serializable]
public class ChooseFormatter : FormatterBase, IFormatterLiteralExtractor
{
    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = '|';
    [SerializeField] CaseSensitivityType m_CaseSensitivity = CaseSensitivityType.CaseSensitive;

    CultureInfo m_CultureInfo;

    /// <inheritdoc/>
    public override string DefaultName => "choose";

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
    /// The <see cref="CaseSensitivityType"/> for option strings.
    /// Defaults to <see cref="CaseSensitivityType.CaseSensitive"/>.
    /// Comparison of option strings is culture-aware.
    /// </summary>
    public CaseSensitivityType CaseSensitivity { get => m_CaseSensitivity; set => m_CaseSensitivity = value; }

    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var chooseOptions = formattingInfo.FormatterOptions.Split(SplitChar);
        var formats = formattingInfo.Format?.Split(SplitChar);

        // Check whether arguments can be handled by this formatter
        if (formats is null || formats.Count < 2 || chooseOptions is null)
        {
            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException($"Formatter named '{formattingInfo.Placeholder.FormatterName}' requires at least 2 format options.");
        }

        m_CultureInfo = formattingInfo.FormatDetails.Provider as CultureInfo ?? CultureInfo.CurrentUICulture;

        var chosenFormat = DetermineChosenFormat(formattingInfo, formats, chooseOptions);

        formattingInfo.FormatAsChild(chosenFormat, formattingInfo.CurrentValue);

        return true;
    }

    Format DetermineChosenFormat(IFormattingInfo formattingInfo, IList<Format> choiceFormats,
        string[] chooseOptions)
    {
        var chosenIndex = GetChosenIndex(formattingInfo, chooseOptions, out var currentValueString);

        // Validate the number of formats:
        if (choiceFormats.Count < chooseOptions.Length)
            throw formattingInfo.FormattingException("You must specify at least " + chooseOptions.Length +
                " choices");
        if (choiceFormats.Count > chooseOptions.Length + 1)
            throw formattingInfo.FormattingException("You cannot specify more than " + (chooseOptions.Length + 1) +
                " choices");
        if (chosenIndex == -1 && choiceFormats.Count == chooseOptions.Length)
            throw formattingInfo.FormattingException("\"" + currentValueString +
                "\" is not a valid choice, and a \"default\" choice was not supplied");

        if (chosenIndex == -1) chosenIndex = choiceFormats.Count - 1;

        var chosenFormat = choiceFormats[chosenIndex];
        return chosenFormat;
    }

    int GetChosenIndex(IFormattingInfo formattingInfo, string[] chooseOptions, out string currentValueString)
    {
        string valAsString;

        // null and bool types are always case-insensitive
        switch (formattingInfo.CurrentValue)
        {
            case null:
                valAsString = currentValueString = "null";
                return Array.FindIndex(chooseOptions,
                    t => t.Equals(valAsString, StringComparison.OrdinalIgnoreCase));
            case bool boolVal:
                valAsString = currentValueString = boolVal.ToString();
                return Array.FindIndex(chooseOptions,
                    t => t.Equals(valAsString, StringComparison.OrdinalIgnoreCase));
        }

        valAsString = currentValueString = formattingInfo.CurrentValue.ToString();

        return Array.FindIndex(chooseOptions,
            t => AreEqual(t, valAsString));
    }

    bool AreEqual(string s1, string s2)
    {
        Assert.IsNotNull(m_CultureInfo);
        var culture = m_CultureInfo;

        return CaseSensitivity == CaseSensitivityType.CaseSensitive
            ? culture.CompareInfo.Compare(s1, s2, CompareOptions.None) == 0
            : culture.CompareInfo.Compare(s1, s2, CompareOptions.IgnoreCase) == 0;
    }

    void IFormatterLiteralExtractor.WriteAllLiterals(IFormattingInfo formattingInfo)
    {
        var chooseOptions = formattingInfo.FormatterOptions.Split(SplitChar);
        var formats = formattingInfo.Format?.Split(SplitChar);

        if (formats is null || formats.Count < 2 || chooseOptions is null)
            return;

        for (int i = 0; i < formats.Count; ++i)
        {
            formattingInfo.FormatAsChild(formats[i], formattingInfo.CurrentValue);
        }
    }
}
