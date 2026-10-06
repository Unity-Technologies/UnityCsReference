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
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Utilities;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Formats values according to culture-specific pluralization rules.
/// </summary>
[Serializable]
public class PluralLocalizationFormatter : FormatterBase, IFormatterLiteralExtractor
{
    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = '|';

    /// <inheritdoc/>
    public override string DefaultName => "plural";

    /// <summary>
    /// The character used to split the option text literals.
    /// Valid characters are: | (pipe) , (comma)  ~ (tilde)
    /// </summary>
    public char SplitChar
    {
        get => m_SplitChar;
        set => m_SplitChar = Validation.GetValidSplitCharOrThrow(value);
    }

    /// <summary>
    /// Creates a new instance of the formatter.
    /// </summary>
    public PluralLocalizationFormatter()
    {
        CanAutoDetect = true;
    }

    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;
        var current = formattingInfo.CurrentValue;

        if (format == null) return false;

        // Extract the plural words from the format string:
        var pluralWords = format.Split(SplitChar);

        var useAutoDetection = string.IsNullOrEmpty(formattingInfo.Placeholder?.FormatterName);

        // Auto-detection needs at least two plural words; a named call may pass one for singular languages
        if (useAutoDetection && pluralWords.Count <= 1) return false;

        decimal value;

        // We can format numbers, and IEnumerables. For IEnumerables we look at the number of items
        // in the collection: this means the user can e.g. use the same parameter for both plural and list, for example
        // 'Smart.Format("The following {0:plural:person is|people are} impressed: {0:list:{}|, |, and}", new[] { "bob", "alice" });'
        switch (current)
        {
            case IConvertible convertible when convertible is not (bool or string) && TryGetDecimalValue(convertible, out value):
                break;
            case IEnumerable<object> objects:
                if (objects is ICollection<object> collection)
                    value = collection.Count;
                else
                {
                    var count = 0;
                    foreach (var _ in objects)
                        count++;
                    value = count;
                }
                break;
            default:
                // Auto detection calls just return a failure to evaluate
                if (useAutoDetection) return false;

                // throw, if the formatter has been called explicitly
                throw new FormattingException(format,
                    $"Formatter named '{formattingInfo.Placeholder?.FormatterName}' can format numbers and IEnumerables, but the argument was of type '{current?.GetType().ToString() ?? "null"}'", 0);
        }

        // Get the specific plural rule, or the default rule:
        var pluralRule = GetPluralRule(formattingInfo);

        var pluralCount = pluralWords.Count;
        var pluralIndex = pluralRule(value, pluralCount);

        if (pluralIndex < 0 || pluralWords.Count <= pluralIndex)
            throw new FormattingException(format, $"Invalid number of plural parameters in {nameof(PluralLocalizationFormatter)}",
                pluralWords[pluralWords.Count - 1].EndIndex);

        // Output the selected word (allowing for nested formats):
        var pluralForm = pluralWords[pluralIndex];
        formattingInfo.FormatAsChild(pluralForm, current);
        return true;
    }

    static bool TryGetDecimalValue(IConvertible convertible, out decimal value)
    {
        // ToDecimal always throws for these, so skip the exception
        if (convertible.GetTypeCode() is TypeCode.Char or TypeCode.DateTime or TypeCode.DBNull)
        {
            value = default;
            return false;
        }

        try
        {
            value = convertible.ToDecimal(null);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    static PluralRules.PluralRuleDelegate GetPluralRule(IFormattingInfo formattingInfo)
    {
        // Determine the culture
        var culture = GetCultureInfo(formattingInfo);
        var pluralOptions = formattingInfo.FormatterOptions.Trim();
        if (pluralOptions.Length != 0) return PluralRules.GetPluralRule(culture.TwoLetterISOLanguageName);

        // See if a CustomPluralRuleProvider is available from the FormatProvider:
        var provider = formattingInfo.FormatDetails.Provider;
        var pluralRuleProvider =
            (CustomPluralRuleProvider)provider?.GetFormat(typeof(CustomPluralRuleProvider));
        if (pluralRuleProvider != null) return pluralRuleProvider.GetPluralRule();

        // No CustomPluralRuleProvider, so use the CultureInfo

        return PluralRules.GetPluralRule(culture.TwoLetterISOLanguageName);
    }

    static CultureInfo GetCultureInfo(IFormattingInfo formattingInfo)
    {
        var culture = formattingInfo.FormatterOptions.Trim();
        CultureInfo cultureInfo;
        if (culture == string.Empty)
        {
            if (formattingInfo.FormatDetails.Provider is CultureInfo ci)
                cultureInfo = ci;
            else
            {
                cultureInfo = CultureInfo.CurrentUICulture;
            }

            // There is no pluralization rule for invariant culture (TwoLetterISOLanguageName == "iv"),
            // so we take English as default
            if (cultureInfo.Equals(CultureInfo.InvariantCulture))
                cultureInfo = CultureInfo.GetCultureInfo("en");
        }
        else
        {
            try
            {
                cultureInfo = CultureInfo.GetCultureInfo(culture);
            }
            catch (Exception e)
            {
                throw new FormattingException(formattingInfo.Format, e, 0);
            }
        }

        return cultureInfo;
    }

    void IFormatterLiteralExtractor.WriteAllLiterals(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;
        if (format == null) return;

        // Extract the plural words from the format string:
        var pluralWords = format.Split(SplitChar);

        // This extension requires at least two plural words for auto-detection
        if (pluralWords.Count <= 1 && string.IsNullOrEmpty(formattingInfo.Placeholder?.FormatterName))
            return;

        for (int i = 0; i < pluralWords.Count; ++i)
        {
            formattingInfo.FormatAsChild(pluralWords[i], null);
        }
    }
}

/// <summary>
/// Provides custom plural rules to Smart.Format.
/// </summary>
public class CustomPluralRuleProvider : IFormatProvider
{
    readonly PluralRules.PluralRuleDelegate m_PluralRule;

    /// <summary>
    /// Creates a new custom plural rule provider.
    /// </summary>
    /// <param name="pluralRule">Delegate that provides the plural rule.</param>
    public CustomPluralRuleProvider(PluralRules.PluralRuleDelegate pluralRule)
    {
        m_PluralRule = pluralRule;
    }

    /// <summary>
    /// Gets the format <see cref="object"/> for a <see cref="CustomPluralRuleProvider"/>.
    /// </summary>
    /// <param name="formatType">Type of formatting object to return.</param>
    /// <returns>The format <see cref="object"/> for a <see cref="CustomPluralRuleProvider"/> or <see langword="null"/>.</returns>
    public object GetFormat(Type formatType)
    {
        return formatType == typeof(CustomPluralRuleProvider) ? this : default;
    }

    /// <summary>
    /// Gets the <see cref="PluralRules.PluralRuleDelegate"/> of the current <see cref="CustomPluralRuleProvider"/> instance.
    /// </summary>
    /// <returns><see cref="PluralRules.PluralRuleDelegate"/> that this provider was created with.</returns>
    public PluralRules.PluralRuleDelegate GetPluralRule()
    {
        return m_PluralRule;
    }
}
