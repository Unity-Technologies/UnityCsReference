// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Parses a format string.
/// </summary>
[Serializable]
public class Parser
{
    const int k_PositionUndefined = -1;
    readonly ParsingErrorText m_ParsingErrorText = new();

    IndexContainer m_Index;
    string m_InputFormat;
    Format m_ResultFormat;

    /// <summary>
    /// Gets the <see cref="SmartSettings" /> for Smart.Format.
    /// </summary>
    public SmartSettings Settings { get; }

    // Cache method results from settings
    readonly List<char> m_OperatorChars;
    readonly List<char> m_CustomOperatorChars;
    readonly ParserSettings m_ParserSettings;
    readonly List<char> m_ValidSelectorChars;
    readonly List<char> m_FormatOptionsTerminatorChars;

    /// <summary>
    /// Raised when an error occurs during parsing.
    /// </summary>
    public event EventHandler<ParsingErrorEventArgs> OnParsingFailure;

    /// <summary>
    /// Creates a new instance of a <see cref="Parser"/>.
    /// </summary>
    /// <param name="smartSettings">
    /// The <see cref="SmartSettings"/> to use, or <see langword="null"/> for default settings.
    /// Any changes after passing settings as a parameter may not have effect.
    /// </param>
    public Parser(SmartSettings smartSettings = null)
    {
        Settings = smartSettings ?? new SmartSettings();
        m_ParserSettings = Settings.Parser;
        m_OperatorChars = m_ParserSettings.OperatorChars();
        m_CustomOperatorChars = m_ParserSettings.CustomOperatorChars();
        m_FormatOptionsTerminatorChars = m_ParserSettings.FormatOptionsTerminatorChars();

        m_ValidSelectorChars = new List<char>();
        m_ValidSelectorChars.AddRange(m_ParserSettings.SelectorChars());
        m_ValidSelectorChars.AddRange(m_ParserSettings.OperatorChars());
        m_ValidSelectorChars.AddRange(m_ParserSettings.CustomSelectorChars());

        m_InputFormat = string.Empty;
        m_ResultFormat = null;
    }
    /// <summary>
    /// The Container for indexes pointing to positions within the input format.
    /// </summary>
    struct IndexContainer
    {
        /// <summary>
        /// The length of the target object, where indexes will be used.
        /// E.g.: ReadOnlySpan&lt;char&gt;().Length or string.Length
        /// </summary>
        public int ObjectLength;

        /// <summary>
        /// The current index within the input format
        /// </summary>
        public int Current;

        /// <summary>
        /// The index within the input format after an item (like <see cref="Placeholder"/>, <see cref="Selector"/>, <see cref="LiteralText"/> etc.) was added.
        /// </summary>
        public int LastEnd;

        /// <summary>
        /// The start index of the formatter name within the input format.
        /// </summary>
        public int NamedFormatterStart;

        /// <summary>
        /// The start index of the formatter options within the input format.
        /// </summary>
        public int NamedFormatterOptionsStart;

        /// <summary>
        /// The end index of the formatter options within the input format.
        /// </summary>
        public int NamedFormatterOptionsEnd;

        /// <summary>
        /// The index of the operator within the input format.
        /// </summary>
        public int Operator;

        /// <summary>
        /// The current index of the selector <b>across all</b> <see cref="Placeholder"/>s.
        /// </summary>
        public int Selector;

        /// <summary>
        /// Adds a number to number to the index and returns the sum, but not more than <see cref="ObjectLength"/>.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <returns>The sum, but not more than <see cref="ObjectLength"/></returns>
        public int SafeAdd(int index, int add)
        {
            // The design is the way, that an end index
            // is always 1 above the last position.
            // Meaning that the maximum of 'FormatItem.EndIndex' equals 'inputFormat.Length'
            index += add;
            System.Diagnostics.Debug.Assert(index >= 0);
            return index < ObjectLength ? index : ObjectLength;
        }
    }

    /// <summary>
    /// Parses a format string.
    /// </summary>
    /// <param name="inputFormat">Format string to parse.</param>
    /// <returns>The <see cref="Format"/> for the parsed string.</returns>
    public Format ParseFormat(string inputFormat)
    {
        m_InputFormat = inputFormat;

        m_Index = new IndexContainer
        {
            ObjectLength = m_InputFormat.Length,
            Current = k_PositionUndefined, LastEnd = 0, NamedFormatterStart = k_PositionUndefined,
            NamedFormatterOptionsStart = k_PositionUndefined, NamedFormatterOptionsEnd = k_PositionUndefined,
            Operator = k_PositionUndefined, Selector = k_PositionUndefined
        };

        // Initialize - will be re-assigned with new placeholders
        m_ResultFormat = FormatPool.Pool.Get().Initialize(Settings, m_InputFormat);

        // Store parsing errors until parsing is finished:
        var parsingErrors = ParsingErrorsPool.Pool.Get().Initialize(m_ResultFormat);

        Placeholder currentPlaceholder = null;

        // Used for nested placeholders
        var nestedDepth = 0;

        for (m_Index.Current = 0; m_Index.Current < m_InputFormat.Length; m_Index.Current++)
        {
            var inputChar = m_InputFormat[m_Index.Current];
            if (currentPlaceholder == null)
            {
                // UNITY - Disabled HTML support. We are more likely to be parsing rich text when we encounter <

                // We're parsing literal text with an HTML tag
                //if (m_ParserSettings.ParseInputAsHtml && inputChar == '<')
                //{
                //    ParseHtmlTags();
                //    continue;
                //}

                if (inputChar == m_ParserSettings.PlaceholderBeginChar)
                {
                    AddLiteralCharsParsedBefore();

                    if (EscapeLikeStringFormat(m_ParserSettings.PlaceholderBeginChar)) continue;

                    CreateNewPlaceholder(ref nestedDepth, out currentPlaceholder);
                }
                else if (inputChar == m_ParserSettings.PlaceholderEndChar)
                {
                    AddLiteralCharsParsedBefore();

                    if (EscapeLikeStringFormat(m_ParserSettings.PlaceholderEndChar)) continue;

                    // Make sure that this is a nested placeholder before we un-nest it:
                    if (HasProcessedTooMayClosingBraces(parsingErrors)) continue;

                    // End of the placeholder's Format, _resultFormat will change to parent.parent
                    FinishPlaceholderFormat(ref nestedDepth);
                }
                else if (inputChar == m_ParserSettings.CharLiteralEscapeChar && m_ParserSettings.ConvertCharacterStringLiterals ||
                         !Settings.StringFormatCompatibility && inputChar == m_ParserSettings.CharLiteralEscapeChar)
                {
                    ParseAlternativeEscaping();
                }
                else if (m_Index.NamedFormatterStart != k_PositionUndefined && !ParseNamedFormatter())
                {
                    // continue the loop
                }
            }
            else
            {
                // Placeholder is NOT null, so that means
                // we're parsing the selectors:
                ParseSelector(ref currentPlaceholder, parsingErrors, ref nestedDepth);
            }
        }

        // We're at the end of the input string

        // 1. Is the last item a placeholder, that is not finished yet?
        if (m_ResultFormat.ParentPlaceholder != null || currentPlaceholder != null)
        {
            parsingErrors.AddIssue(m_ResultFormat, m_ParsingErrorText[ParsingError.MissingClosingBrace], m_InputFormat.Length,
                m_InputFormat.Length);
            m_ResultFormat.EndIndex = m_InputFormat.Length;
        }
        else if (m_Index.LastEnd != m_InputFormat.Length)
        {
            // 2. The last item must be a literal, so add it
            m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.LastEnd, m_InputFormat.Length));
        }

        // This may happen with a missing closing brace, e.g. "{0:yyyy/MM/dd HH:mm:ss"
        while (m_ResultFormat.ParentPlaceholder != null)
        {
            m_ResultFormat = m_ResultFormat.ParentPlaceholder.Parent;
            m_ResultFormat.EndIndex = m_InputFormat.Length;
        }

        // Check for any parsing errors:
        if (parsingErrors.HasIssues)
        {
            OnParsingFailure?.Invoke(this,
                new ParsingErrorEventArgs(parsingErrors, Settings.Parser.ErrorAction == ParseErrorAction.ThrowError));

            return HandleParsingErrors(parsingErrors, m_ResultFormat);
        }

        ParsingErrorsPool.Pool.Release(parsingErrors);
        return m_ResultFormat;
    }

    /// <summary>
    /// Adds a new <see cref="LiteralText"/> item, if there are characters left to process.
    /// Sets <see cref="IndexContainer.LastEnd"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddLiteralCharsParsedBefore()
    {
        // Finish the last text item:
        if (m_Index.Current != m_Index.LastEnd)
        {
            m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.LastEnd, m_Index.Current));
        }

        m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 1);
    }

    /// <summary>
    /// Checks, whether we are on top level and still there was a closing brace.
    /// In this case we add the redundant brace as literal and create a <see cref="ParsingError"/>.
    /// </summary>
    /// <param name="parsingErrors">The list of <see cref="ParsingErrors"/>.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool HasProcessedTooMayClosingBraces(ParsingErrors parsingErrors)
    {
        if (m_ResultFormat.ParentPlaceholder != null) return false;

        // Don't swallow-up redundant closing braces, but treat them as literals
        m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.Current, m_Index.Current + 1));

        parsingErrors.AddIssue(m_ResultFormat, m_ParsingErrorText[ParsingError.TooManyClosingBraces], m_Index.Current,
            m_Index.Current + 1);
        return true;
    }

    /// <summary>
    /// In case of string.Format compatibility, we escape the brace
    /// and treat it as a literal character.
    /// </summary>
    /// <param name="brace">The brace { or } to process.</param>
    /// <returns><see langword="true"/>, if escaping was done.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool EscapeLikeStringFormat(char brace)
    {
        if (!Settings.StringFormatCompatibility) return false;

        if (m_Index.LastEnd < m_InputFormat.Length && m_InputFormat[m_Index.LastEnd] == brace)
        {
            m_Index.Current = m_Index.SafeAdd(m_Index.Current, 1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new <see cref="Placeholder"/>, adds it to the current format and sets values in <see cref="IndexContainer"/>.
    /// </summary>
    /// <param name="nestedDepth">The counter for nesting levels.</param>
    /// <param name="newPlaceholder"></param>
    /// <returns>The new <see cref="Placeholder"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CreateNewPlaceholder(ref int nestedDepth, out Placeholder newPlaceholder)
    {
        nestedDepth++;
        newPlaceholder = PlaceholderPool.Pool.Get().Initialize(m_ResultFormat, m_Index.Current, nestedDepth);
        m_ResultFormat.Items.Add(newPlaceholder);
        m_ResultFormat.HasNested = true;
        m_Index.Operator = m_Index.SafeAdd(m_Index.Current, 1);
        m_Index.Selector = 0;
        m_Index.NamedFormatterStart = k_PositionUndefined;
    }

    /// <summary>
    /// Finishes the current placeholder <see cref="Format"/>.
    /// </summary>
    /// <param name="nestedDepth">The counter for nesting levels.</param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void FinishPlaceholderFormat(ref int nestedDepth)
    {
        Assert.IsNotNull(m_ResultFormat.ParentPlaceholder);

        nestedDepth--;
        m_ResultFormat.EndIndex = m_Index.Current;
        m_ResultFormat.ParentPlaceholder !.EndIndex = m_Index.SafeAdd(m_Index.Current, 1);
        m_ResultFormat = m_ResultFormat.ParentPlaceholder.Parent;
        m_Index.NamedFormatterStart = m_Index.NamedFormatterOptionsStart = m_Index.NamedFormatterOptionsEnd = k_PositionUndefined;
    }

    /// <summary>
    /// Processes the character if alternative escaping is used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ParseAlternativeEscaping()
    {
        // 2021-05-03/axuno: Removed "index.NamedFormatterStart = PositionUndefined"

        // See what is the next character
        var indexNextChar = m_Index.SafeAdd(m_Index.Current, 1);
        if (indexNextChar >= m_InputFormat.Length)
            throw new ArgumentException($"Unrecognized escape sequence at the end of the literal");

        // **** Alternative brace escaping with { or } following the escape character ****
        if (m_InputFormat[indexNextChar] == m_ParserSettings.PlaceholderBeginChar || m_InputFormat[indexNextChar] == m_ParserSettings.PlaceholderEndChar)
        {
            // Finish the last text item:
            if (m_Index.Current != m_Index.LastEnd) m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.LastEnd, m_Index.Current));
            m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 1);

            m_Index.Current++;
        }
        else
        {
            // **** Escaping of character literals like \t, \n, \v etc. ****

            // Finish the last text item:
            if (m_Index.Current != m_Index.LastEnd) m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.LastEnd, m_Index.Current));

            // Is this a unicode escape sequence?
            if (m_InputFormat[indexNextChar] == 'u')
            {
                // The next 4 characters must represent the unicode
                m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 6);
                m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.Current, m_Index.LastEnd));
            }
            else
            {
                // Next add the character literal INCLUDING the escape character, which LiteralText will expect
                m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 2);
                m_ResultFormat.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, m_ResultFormat, m_InputFormat, m_Index.Current, m_Index.LastEnd));
            }

            m_Index.Current = m_Index.SafeAdd(m_Index.Current, 1);
        }
    }

    /// <summary>
    /// Handles named formatters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool ParseNamedFormatter()
    {
        var inputChar = m_InputFormat[m_Index.Current];
        if (inputChar == m_ParserSettings.FormatterOptionsBeginChar)
        {
            var emptyName = m_Index.NamedFormatterStart == m_Index.Current;
            if (emptyName)
            {
                m_Index.NamedFormatterStart = k_PositionUndefined;
                return false;
            }

            // Note: This short-circuits the Parser.ParseFormat main loop
            ParseFormatOptions();
        }
        else if (inputChar == m_ParserSettings.FormatterOptionsEndChar || inputChar == m_ParserSettings.FormatterNameSeparator)
        {
            if (inputChar == m_ParserSettings.FormatterOptionsEndChar)
            {
                var hasOpeningParenthesis = m_Index.NamedFormatterOptionsStart != k_PositionUndefined;

                // ensure no trailing chars past ')'
                var nextCharIndex = m_Index.SafeAdd(m_Index.Current, 1);
                var nextCharIsValid = nextCharIndex < m_InputFormat.Length &&
                    (m_InputFormat[nextCharIndex] == m_ParserSettings.FormatterNameSeparator || m_InputFormat[nextCharIndex] == m_ParserSettings.PlaceholderEndChar);

                if (!hasOpeningParenthesis || !nextCharIsValid)
                {
                    m_Index.NamedFormatterStart = k_PositionUndefined;
                    return false;
                }

                m_Index.NamedFormatterOptionsEnd = m_Index.Current;

                if (m_InputFormat[nextCharIndex] == m_ParserSettings.FormatterNameSeparator) m_Index.Current++; // Consume the ':'
            }

            var nameIsEmpty = m_Index.NamedFormatterStart == m_Index.Current;
            var missingClosingParenthesis =
                m_Index.NamedFormatterOptionsStart != k_PositionUndefined &&
                m_Index.NamedFormatterOptionsEnd == k_PositionUndefined;
            if (nameIsEmpty || missingClosingParenthesis)
            {
                m_Index.NamedFormatterStart = k_PositionUndefined;
                return false;
            }

            m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 1);

            var parentPlaceholder = m_ResultFormat.ParentPlaceholder;

            if (m_Index.NamedFormatterOptionsStart == k_PositionUndefined)
            {
                if (parentPlaceholder != null)
                {
                    parentPlaceholder.FormatterNameStartIndex = m_Index.NamedFormatterStart;
                    parentPlaceholder.FormatterNameLength = m_Index.Current - m_Index.NamedFormatterStart;
                }
            }
            else
            {
                if (parentPlaceholder != null)
                {
                    parentPlaceholder.FormatterNameStartIndex = m_Index.NamedFormatterStart;
                    parentPlaceholder.FormatterNameLength = m_Index.NamedFormatterOptionsStart - m_Index.NamedFormatterStart;

                    // Save the formatter options with CharLiteralEscapeChar removed
                    parentPlaceholder.FormatterOptionsStartIndex = m_Index.NamedFormatterOptionsStart + 1;
                    parentPlaceholder.FormatterOptionsLength = m_Index.NamedFormatterOptionsEnd - (m_Index.NamedFormatterOptionsStart + 1);
                }
            }

            // Set start index to start of formatter option arguments,
            // with {0:default:N2} the start index is on the second colon
            m_ResultFormat.StartIndex = m_Index.LastEnd;

            m_Index.NamedFormatterStart = k_PositionUndefined;
        }

        return true;
    }

    /// <summary>
    /// Handles the selectors.
    /// </summary>
    /// <param name="currentPlaceholder"></param>
    /// <param name="parsingErrors"></param>
    /// <param name="nestedDepth"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ParseSelector(ref Placeholder currentPlaceholder, ParsingErrors parsingErrors, ref int nestedDepth)
    {
        if (currentPlaceholder == null)
        {
            throw new ArgumentNullException(nameof(currentPlaceholder), $"Unexpected null reference");
        }

        var inputChar = m_InputFormat[m_Index.Current];
        if (m_OperatorChars.Contains(inputChar) || m_CustomOperatorChars.Contains(inputChar))
        {
            // Add the selector:
            if (m_Index.Current != m_Index.LastEnd) // if equal, we're already parsing a selector
            {
                currentPlaceholder.AddSelector(SelectorPool.Pool.Get().Initialize(Settings, currentPlaceholder, m_InputFormat, m_Index.LastEnd, m_Index.Current, m_Index.Operator, m_Index.Selector));
                m_Index.Selector++;
                m_Index.Operator = m_Index.Current;
            }

            m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 1);
        }
        else if (inputChar == m_ParserSettings.FormatterNameSeparator)
        {
            // Add the selector:
            AddLastSelector(ref currentPlaceholder, parsingErrors);

            // Start the format:
            var newFormat = FormatPool.Pool.Get().Initialize(Settings, currentPlaceholder, m_Index.Current + 1);
            currentPlaceholder.Format = newFormat;
            //FormatPool.Pool.Release(_resultFormat); // return to pool before reassigning
            m_ResultFormat = newFormat;
            currentPlaceholder = null;
            // named formatters will not be parsed with string.Format compatibility switched ON.
            // But this way we can handle e.g. Smart.Format("{Date:yyyy/MM/dd HH:mm:ss}") like string.Format
            m_Index.NamedFormatterStart = Settings.StringFormatCompatibility ? k_PositionUndefined : m_Index.LastEnd;
            m_Index.NamedFormatterOptionsStart = k_PositionUndefined;
            m_Index.NamedFormatterOptionsEnd = k_PositionUndefined;
        }
        else if (inputChar == m_ParserSettings.PlaceholderEndChar)
        {
            AddLastSelector(ref currentPlaceholder, parsingErrors);

            // End the placeholder with no format:
            nestedDepth--;
            currentPlaceholder.EndIndex = m_Index.SafeAdd(m_Index.Current, 1);
            //_resultFormat = currentPlaceholder.Parent;  // removed 2021-08-08: The parent always is the _resultFormat
            currentPlaceholder = null;
        }
        else
        {
            // Ensure the selector characters are valid:
            if (!m_ValidSelectorChars.Contains(inputChar))
                parsingErrors.AddIssue(m_ResultFormat,
                    $"'0x{Convert.ToUInt32(inputChar):X}': " +
                    m_ParsingErrorText[ParsingError.InvalidCharactersInSelector],
                    m_Index.Current, m_Index.SafeAdd(m_Index.Current, 1));
        }
    }

    /// <summary>
    /// Adds a <see cref="Selector"/> to the current <see cref="Placeholder"/>
    /// because the current character ':' or '}' indicates the end of a selector.
    /// </summary>
    /// <param name="currentPlaceholder"></param>
    /// <param name="parsingErrors"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddLastSelector(ref Placeholder currentPlaceholder, ParsingErrors parsingErrors)
    {
        if (m_Index.Current != m_Index.LastEnd ||
            currentPlaceholder.Selectors.Count > 0 && currentPlaceholder.Selectors[^1].Length > 0 &&
            m_Index.Current - m_Index.Operator == 1 &&
            (m_InputFormat[m_Index.Operator] == m_ParserSettings.ListIndexEndChar ||
             m_InputFormat[m_Index.Operator] == m_ParserSettings.NullableOperator))
            currentPlaceholder.AddSelector(SelectorPool.Pool.Get().Initialize(Settings, currentPlaceholder, m_InputFormat, m_Index.LastEnd, m_Index.Current, m_Index.Operator, m_Index.Selector));
        else if (m_Index.Operator != m_Index.Current) // the selector only contains illegal ("trailing") operator characters
            parsingErrors.AddIssue(m_ResultFormat,
                $"'0x{Convert.ToInt32(m_InputFormat[m_Index.Operator]):X}': " +
                m_ParsingErrorText[ParsingError.TrailingOperatorsInSelector],
                m_Index.Operator, m_Index.Current);
        m_Index.LastEnd = m_Index.SafeAdd(m_Index.Current, 1);
    }

    /// <summary>
    /// Parses all option characters.
    /// This short-circuits the Parser.ParseFormat main loop.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ParseFormatOptions()
    {
        m_Index.NamedFormatterOptionsStart = m_Index.Current;

        var nextChar = m_InputFormat[m_Index.SafeAdd(m_Index.Current, 1)];
        // Handle empty options()
        if (m_FormatOptionsTerminatorChars.Contains(nextChar)) return;

        while (++m_Index.Current < m_Index.ObjectLength)
        {
            nextChar = m_InputFormat[m_Index.SafeAdd(m_Index.Current, 1)];
            // Skip escaped terminating characters
            if (m_InputFormat[m_Index.Current] == m_ParserSettings.CharLiteralEscapeChar &&
                (m_FormatOptionsTerminatorChars.Contains(nextChar) ||
                 EscapedLiteral.TryGetChar(nextChar, out _, true)))
            {
                m_Index.Current = m_Index.SafeAdd(m_Index.Current, 1);
                if (m_FormatOptionsTerminatorChars.Contains(
                    m_InputFormat[m_Index.SafeAdd(m_Index.Current, 1)]))
                {
                    return;
                }

                continue;
            }

            // End of parsing options, when the NEXT character is terminating,
            // because this character will be handled in the Parser.ParseFormat main loop.
            if (m_FormatOptionsTerminatorChars.Contains(m_InputFormat[m_Index.Current + 1]))
            {
                return;
            }
        }
    }

    // 'style' and 'script' tags may contain curly or square braces, which SmartFormat uses to identify Placeholders.
    // Also, comments may contain any characters, which could mix up the parser.
    // That's why the parser will treat all content inside 'style' and 'script' tags as LiteralText,
    // if ParserSettings.ParseInputAsHtml is true.
    //private void ParseHtmlTags()
    //{
    //    // The tags we will be parsing with this method
    //    var scriptTagName = "script".AsSpan();
    //    var styleTagName = "style".AsSpan();

    //    // The first position is the start of an HTML tag
    //    // Move forward to the tag name
    //    m_Index.Current++;

    //    // Is it a script tag starting with <script
    //    var currentTagName = ReadOnlySpan<char>.Empty;
    //    if (m_InputFormat.AsSpan(m_Index.Current).StartsWith(scriptTagName, StringComparison.InvariantCultureIgnoreCase))
    //    {
    //        currentTagName = scriptTagName;
    //        m_Index.Current += currentTagName.Length; // move behind tag name
    //    }

    //    // Is it a style tag starting with <style
    //    if (m_InputFormat.AsSpan(m_Index.Current).StartsWith(styleTagName, StringComparison.InvariantCultureIgnoreCase))
    //    {
    //        currentTagName = styleTagName;
    //        m_Index.Current += currentTagName.Length; // move behind tag name
    //    }

    //    // Not a tag we should parse, stop processing
    //    if (currentTagName == ReadOnlySpan<char>.Empty) return;

    //    // Initialize quoting variables
    //    var isQuoting = false;
    //    var endQuoteChar = '\"';

    //    // Parse characters inside script or style tag
    //    while (true)
    //    {
    //        // done
    //        if (m_Index.Current >= m_InputFormat.Length) return;

    //        // text inside quotes (e.g.: const variable = "</script>"; could mix-up the parser
    //        switch (isQuoting)
    //        {
    //            case false when m_InputFormat[m_Index.Current] == '\'' || m_InputFormat[m_Index.Current] == '\"':
    //                isQuoting = !isQuoting;
    //                endQuoteChar = m_InputFormat[m_Index.Current]; // start and end quoting char must be equal
    //                m_Index.Current++;
    //                continue;
    //            case true when m_InputFormat[m_Index.Current] == endQuoteChar:
    //                isQuoting = !isQuoting;
    //                m_Index.Current++;
    //                continue;
    //            case true:
    //                m_Index.Current++;
    //                continue;
    //        }

    //        // Is it a self-closing tag like <script/>
    //        if (m_InputFormat[m_Index.Current] == '/' && m_InputFormat[m_Index.Current + 1] == '>' && m_InputFormat
    //            .AsSpan(m_Index.Current - 1 - currentTagName.Length)
    //            .StartsWith(currentTagName, StringComparison.InvariantCultureIgnoreCase))
    //        {
    //            m_Index.Current++;
    //            return;
    //        }

    //        // Is it the begin of </script> or </style>?
    //        if (m_InputFormat[m_Index.Current] == '<'
    //            && m_InputFormat[m_Index.Current + 1] == '/'
    //            && currentTagName != ReadOnlySpan<char>.Empty
    //            && m_InputFormat
    //                .AsSpan(m_Index.Current + 2)
    //                .StartsWith(currentTagName, StringComparison.InvariantCultureIgnoreCase))
    //        {
    //            m_Index.Current = m_Index.SafeAdd(m_Index.Current, 2 + currentTagName.Length); // move behind tag name
    //            if (m_Index.Current < m_InputFormat.Length && m_InputFormat[m_Index.Current] == '>') // closing char
    //                return;
    //        }

    //        if (m_InputFormat.Length > m_Index.Current)
    //        {
    //            m_Index.Current++;
    //            continue;
    //        }

    //        // We get here, when a script or style tag is not closed
    //        return;
    //    }
    //}

    /// <summary>
    /// Errors that can occur while parsing a format string.
    /// </summary>
    public enum ParsingError
    {
        /// <summary>
        /// Too many closing braces.
        /// </summary>
        TooManyClosingBraces = 1,
        /// <summary>
        /// Trailing operators in the selector.
        /// </summary>
        TrailingOperatorsInSelector,
        /// <summary>
        /// Invalid characters in the selector.
        /// </summary>
        InvalidCharactersInSelector,
        /// <summary>
        /// Missing closing brace.
        /// </summary>
        MissingClosingBrace
    }

    /// <summary>
    /// Supplies error text for the <see cref="Parser"/>.
    /// </summary>
    public class ParsingErrorText
    {
        readonly Dictionary<ParsingError, string> m_Erors = new() {
            {ParsingError.TooManyClosingBraces, "Format string has too many closing braces"},
            {ParsingError.TrailingOperatorsInSelector, "There are illegal trailing operators in the selector"},
            {ParsingError.InvalidCharactersInSelector, "Invalid character in the selector"},
            {ParsingError.MissingClosingBrace, "Format string is missing a closing brace"}
        };

        /// <summary>
        /// CTOR.
        /// </summary>
        internal ParsingErrorText()
        {
        }

        /// <summary>
        /// Gets the string representation of the ParsingError enum.
        /// </summary>
        /// <param name="parsingErrorKey">Error whose message text to retrieve.</param>
        /// <returns>The string representation of the ParsingError enum</returns>
        public string this[ParsingError parsingErrorKey] => m_Erors[parsingErrorKey];
    }

    /// <summary>
    /// Handles <see cref="ParsingError"/>s as defined by the parser's error action.
    /// </summary>
    /// <param name="parsingErrors"></param>
    /// <param name="currentResult"></param>
    /// <returns>The <see cref="Format"/> which will be further processed by the formatter.</returns>
    Format HandleParsingErrors(ParsingErrors parsingErrors, Format currentResult)
    {
        switch (Settings.Parser.ErrorAction)
        {
            case ParseErrorAction.ThrowError:
                throw parsingErrors;
            case ParseErrorAction.MaintainTokens:
                // Replace erroneous Placeholders with tokens as LiteralText
                // Placeholder without issues are left unmodified
                for (var i = 0; i < currentResult.Items.Count; i++)
                {
                    if (currentResult.Items[i] is Placeholder ph && HasIssueInRange(parsingErrors.Issues, ph.StartIndex, ph.EndIndex))
                    {
                        var parent = ph.Format ?? FormatPool.Pool.Get().Initialize(Settings, ph.BaseString);
                        currentResult.Items[i] = LiteralTextPool.Pool.Get().Initialize(Settings, parent, parent.BaseString, ph.StartIndex, ph.EndIndex);
                    }
                }
                return currentResult;
            case ParseErrorAction.Ignore:
                // Replace erroneous Placeholders with an empty LiteralText
                for (var i = 0; i < currentResult.Items.Count; i++)
                {
                    if (currentResult.Items[i] is Placeholder ph && HasIssueInRange(parsingErrors.Issues, ph.StartIndex, ph.EndIndex))
                    {
                        var parent = ph.Format ?? FormatPool.Pool.Get().Initialize(Settings, ph.BaseString);
                        currentResult.Items[i] = LiteralTextPool.Pool.Get().Initialize(Settings, parent, parent.BaseString, ph.StartIndex, ph.StartIndex);
                    }
                }
                return currentResult;
            case ParseErrorAction.OutputErrorInResult:
                var fmt = FormatPool.Pool.Get().Initialize(Settings, parsingErrors.Message, 0, parsingErrors.Message.Length);
                fmt.Items.Add(LiteralTextPool.Pool.Get().Initialize(Settings, fmt, parsingErrors.Message, 0, parsingErrors.Message.Length));
                return fmt;
            default:
                throw new ArgumentException("Illegal type for ParsingErrors", parsingErrors);
        }
    }

    static bool HasIssueInRange(List<ParsingErrors.ParsingIssue> issues, int startIndex, int endIndex)
    {
        for (var j = 0; j < issues.Count; j++)
        {
            var issue = issues[j];
            if (issue.Index >= startIndex && issue.Index <= endIndex)
                return true;
        }

        return false;
    }
}
