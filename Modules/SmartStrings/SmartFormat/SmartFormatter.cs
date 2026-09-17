// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Output;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Extensions;
using Unity.SmartStrings.Pooling.SmartPools;
using Unity.SmartStrings.Utilities;

namespace Unity.SmartStrings;

/// <summary>
/// Constructs formatted strings by invoking each registered source and formatter extension.
/// </summary>
[Serializable]
public class SmartFormatter : ISerializationCallbackReceiver
{
    [SerializeField] SmartSettings m_SmartSettings = new SmartSettings();
    [SerializeReference] List<ISource> m_Sources = new();
    [SerializeReference] List<IFormatter> m_Formatters = new();

    /// <summary>
    /// The <see cref="Core.Parsing.Parser" /> this formatter uses to parse format strings.
    /// </summary>
    public Parser Parser { get; private set; }

    /// <summary>
    /// The <see cref="SmartSettings" /> that control parsing and formatting for this formatter.
    /// </summary>
    public SmartSettings Settings { get => m_SmartSettings; internal set => m_SmartSettings = value; }

    /// <summary>
    /// Creates a formatter that uses the specified settings.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="SmartSettings"/> to use, or <see langword="null"/> for default settings.
    /// Any changes after passing settings as a parameter may not have effect.
    /// </param>
    public SmartFormatter(SmartSettings settings = null)
    {
        m_SmartSettings = settings ?? new SmartSettings();
        Parser = new Parser(Settings);
    }

    /// <summary>
    /// Raised when an error occurs during formatting.
    /// </summary>
    public event EventHandler<FormattingErrorEventArgs> OnFormattingFailure;

    /// <summary>
    /// Gets the list of <see cref="ISource" /> source extensions.
    /// </summary>
    // Deserialization can leave a SerializeReference list null, so every reader goes through this.
    internal List<ISource> SourceExtensions => m_Sources ??= new List<ISource>();

    /// <summary>
    /// Gets the <see cref="ISource" /> source extensions registered with this formatter.
    /// </summary>
    /// <returns>A read-only list of the registered <see cref="ISource"/> extensions.</returns>
    public IReadOnlyList<ISource> GetSourceExtensions() => SourceExtensions.AsReadOnly();

    /// <summary>
    /// Gets the list of <see cref="IFormatter" /> formatter extensions.
    /// </summary>
    internal List<IFormatter> FormatterExtensions => m_Formatters ??= new List<IFormatter>();

    /// <summary>
    /// Gets the <see cref="IFormatter" /> formatter extensions registered with this formatter.
    /// </summary>
    /// <returns>A read-only list of the registered <see cref="IFormatter"/> extensions.</returns>
    public IReadOnlyList<IFormatter> GetFormatterExtensions() => FormatterExtensions.AsReadOnly();

    /// <summary>
    /// Adds <see cref="ISource"/> extensions to the <see cref="GetSourceExtensions()"/> list of this formatter,
    /// if the <see cref="Type"/> has not been added before. <see cref="WellKnownExtensionTypes.Sources"/> are inserted
    /// at the recommended position, all others are added at the end of the list.
    /// 
    /// If the extension implements <see cref="IInitializer"/>, <see cref="IInitializer.Initialize"/> will be invoked.
    /// 
    /// 
    /// Extensions implementing <see cref="ISource"/> <b>and</b> <see cref="IFormatter"/>
    /// will be auto-registered for both.
    /// 
    /// </summary>
    /// <param name="sourceExtensions"><see cref="ISource"/> extensions in an arbitrary order.</param>
    /// <returns>This <see cref="SmartFormatter"/> instance.</returns>
    public SmartFormatter AddExtensions(params ISource[] sourceExtensions)
    {
        foreach (var source in sourceExtensions)
        {
            var index = WellKnownExtensionTypes.GetIndexToInsert(SourceExtensions, source);
            _ = InsertExtension(index, source);

            // Also add the class as a formatter, if possible
            if (source is IFormatter formatter && FormatterExtensions.TrueForAll(fx => fx.GetType() != formatter.GetType())) AddExtensions(formatter);
        }

        return this;
    }

    /// <summary>
    /// Adds <see cref="IFormatter"/> extensions to the <see cref="GetFormatterExtensions()"/> list of this formatter,
    /// if the <see cref="Type"/> has not been added before. <see cref="WellKnownExtensionTypes.Formatters"/> are inserted
    /// at the recommended position, all others are added at the end of the list.
    /// 
    /// If the extension implements <see cref="IInitializer"/>, <see cref="IInitializer.Initialize"/> will be invoked.
    /// 
    /// 
    /// Extensions implementing <see cref="ISource"/> <b>and</b> <see cref="IFormatter"/>
    /// will be auto-registered for both.
    /// 
    /// </summary>
    /// <param name="formatterExtensions"><see cref="IFormatter"/> extensions in an arbitrary order.</param>
    /// <returns>This <see cref="SmartFormatter"/> instance.</returns>
    public SmartFormatter AddExtensions(params IFormatter[] formatterExtensions)
    {
        foreach (var formatter in formatterExtensions)
        {
            var index = WellKnownExtensionTypes.GetIndexToInsert(FormatterExtensions, formatter);
            _ = InsertExtension(index, formatter);

            // Also add the class as a source, if possible
            if (formatter is ISource source && SourceExtensions.TrueForAll(sx => sx.GetType() != source.GetType())) AddExtensions(source);
        }

        return this;
    }

    /// <summary>
    /// Adds the <see cref="ISource"/> extensions at the <paramref name="position"/> of the <see cref="GetSourceExtensions()"/> list of this formatter,
    /// if the <see cref="Type"/> has not been added before.
    /// If the extension implements <see cref="IInitializer"/>, <see cref="IInitializer.Initialize"/> will be invoked.
    /// </summary>
    /// <param name="position">The position in the <see cref="SourceExtensions"/> list where new extensions will be added.</param>
    /// <param name="sourceExtension">Source extension to insert.</param>
    /// <returns>This <see cref="SmartFormatter"/> instance.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///        <paramref name="position" /> is less than 0.
    ///         -or-
    ///         <paramref name="position" /> is greater than the number of items in <see cref="SourceExtensions" />.
    /// </exception>
    public SmartFormatter InsertExtension(int position, ISource sourceExtension)
    {
        if (SourceExtensions.Exists(sx => sx.GetType() == sourceExtension.GetType())) return this;

        if (sourceExtension is IInitializer sourceToInitialize)
            sourceToInitialize.Initialize(this);

        SourceExtensions.Insert(position, sourceExtension);

        return this;
    }

    /// <summary>
    /// Adds the <see cref="IFormatter"/> extension at the <paramref name="position"/> of the <see cref="GetFormatterExtensions()"/> list of this formatter,
    /// if the <see cref="Type"/> has not been added before.
    /// If the extension implements <see cref="IInitializer"/>, <see cref="IInitializer.Initialize"/> will be invoked.
    /// </summary>
    /// <param name="position">The position in the formatter extensions list where the new extension is added.</param>
    /// <param name="formatterExtension">Formatter extension to insert.</param>
    /// <returns>This <see cref="SmartFormatter"/> instance.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///        <paramref name="position" /> is less than 0.
    ///         -or-
    ///         <paramref name="position" /> is greater than the number of items in <see cref="GetFormatterExtensions()" />.
    /// </exception>
    public SmartFormatter InsertExtension(int position, IFormatter formatterExtension)
    {
        if (FormatterExtensions.Exists(sx => sx.GetType() == formatterExtension.GetType())) return this;

        // Extension name is in use by a different type
        if (FormatterExtensions.Exists(fx => fx.Name.Equals(formatterExtension.Name)))
            throw new ArgumentException($"Formatter '{formatterExtension.GetType().Name}' uses existing name.", nameof(formatterExtension));

        if (formatterExtension is IInitializer formatterToInitialize)
            formatterToInitialize.Initialize(this);

        FormatterExtensions.Insert(position, formatterExtension);

        return this;
    }

    /// <summary>
    /// Searches for a Source Extension of the given type, and returns it.
    /// Returns <see langword="null"/> if the type cannot be found.
    /// </summary>
    /// <typeparam name="T">Source extension type to find.</typeparam>
    /// <returns>The class implementing <see cref="ISource"/> if found, else <see langword="null"/>.</returns>
    public T GetSourceExtension<T>() where T : class, ISource
    {
        return SourceExtensions.Find(s => s is T) as T;
    }

    /// <summary>
    /// Searches for a Formatter Extension of the given type, and returns it.
    /// Returns <see langword="null"/> if the type cannot be found.
    /// </summary>
    /// <typeparam name="T">Formatter extension type to find.</typeparam>
    /// <returns>The class implementing <see cref="IFormatter"/> if found, else <see langword="null"/>.</returns>
    public T GetFormatterExtension<T>() where T : class, IFormatter
    {
        return FormatterExtensions.Find(f => f is T) as T;
    }

    /// <summary>
    /// Removes Source Extension of the given type.
    /// </summary>
    /// <typeparam name="T">Source extension type to remove.</typeparam>
    /// <returns><see langword="true"/>, if the extension was found and could be removed.</returns>
    public bool RemoveSourceExtension<T>() where T : class, ISource
    {
        var source = SourceExtensions.Find(s => s is T) as T;
        return source is not null && SourceExtensions.Remove(source);
    }

    /// <summary>
    /// Removes the Formatter Extension of the given type.
    /// </summary>
    /// <typeparam name="T">Formatter extension type to remove.</typeparam>
    /// <returns><see langword="true"/>, if the extension was found and could be removed.</returns>
    public bool RemoveFormatterExtension<T>() where T : class, IFormatter
    {
        var format = FormatterExtensions.Find(f => f is T) as T;
        return format is not null && FormatterExtensions.Remove(format);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(string format, params object[] args)
    {
        return Format(null, format, (IList<object>)args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(string format, IList<object> args)
    {
        return Format(null, format, args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="provider">The <see cref="IFormatProvider" /> to use.</param>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(IFormatProvider provider, string format, params object[] args)
    {
        return Format(provider, format, (IList<object>)args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="provider">The <see cref="IFormatProvider" /> to use.</param>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(IFormatProvider provider, string format, IList<object> args)
    {
        var formatParsed = Parser.ParseFormat(format); // The parser gets the Format from the pool

        var zsOutput = new StringOutput(formatParsed.Length + formatParsed.Items.Count * 8);

        var current = args?.Count > 0 ? args[0] : args; // The first item is the default.

        var formatDetails = FormatDetailsPool.Pool.Get().Initialize(this, formatParsed, args, provider, zsOutput);
        try
        {
            Format(formatDetails, formatParsed, current);
        }
        finally
        {
            FormatDetailsPool.Pool.Release(formatDetails);
            FormatPool.Pool.Release(formatParsed);
        }

        return zsOutput.ToString();
    }

    #region ** Format overloads with cached Format **

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="formatParsed">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(Format formatParsed, params object[] args)
    {
        return Format(null, formatParsed, (IList<object>)args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="formatParsed">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(Format formatParsed, IList<object> args)
    {
        return Format(null, formatParsed, args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="provider">The <see cref="IFormatProvider" /> to use.</param>
    /// <param name="formatParsed">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(IFormatProvider provider, Format formatParsed, params object[] args)
    {
        return Format(provider, formatParsed, (IList<object>)args);
    }

    /// <summary>
    /// Replaces one or more format items in a specified string with the string representation of a specific object.
    /// </summary>
    /// <param name="provider">The <see cref="IFormatProvider" /> to use.</param>
    /// <param name="formatParsed">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The object to format.</param>
    /// <returns>Returns the formatted input with items replaced with their string representation.</returns>
    public string Format(IFormatProvider provider, Format formatParsed, IList<object> args)
    {
        var zsOutput = new StringOutput(formatParsed.Length + formatParsed.Items.Count * 8);
        var current = args == null ? null : args.Count > 0 ? args[0] : args; // The first item is the default.
        var formatDetails = FormatDetailsPool.Pool.Get().Initialize(this, formatParsed, args, provider, zsOutput);
        try
        {
            Format(formatDetails, formatParsed, current);
        }
        finally
        {
            FormatDetailsPool.Pool.Release(formatDetails);
        }
        return zsOutput.ToString();
    }

    #endregion

    /// <summary>
    /// Formats the specified <see cref="FormattingInfo" />.
    /// </summary>
    /// <param name="formattingInfo">Formatting context to process.</param>
    public virtual void Format(FormattingInfo formattingInfo)
    {
        // Before we start, make sure we have at least one source extension and one formatter extension:
        CheckForExtensions();
        if (formattingInfo.Format is null) return;

        foreach (var item in formattingInfo.Format.Items)
        {
            if (item is LiteralText literalItem)
            {
                formattingInfo.Write(literalItem.AsSpan());
                continue;
            }

            // Otherwise, the item must be a placeholder.
            var placeholder = (Placeholder)item;
            var childFormattingInfo = formattingInfo.CreateChild(placeholder);
            try
            {
                // Note: If there is no selector (like {:0.00}),
                // FormattingInfo.CurrentValue is left unchanged
                EvaluateSelectors(childFormattingInfo);
            }
            catch (Exception ex)
            {
                // An error occurred while evaluation selectors
                var errorIndex = placeholder.Format?.StartIndex ?? placeholder.Selectors[^1].EndIndex;
                FormatError(item, ex, errorIndex, childFormattingInfo);
                continue;
            }

            try
            {
                EvaluateFormatters(childFormattingInfo);
            }
            catch (Exception ex)
            {
                // An error occurred while evaluating formatters
                var errorIndex = placeholder.Format?.StartIndex ?? placeholder.Selectors[^1].EndIndex;
                FormatError(item, ex, errorIndex, childFormattingInfo);
            }
        }
    }

    void Format(FormatDetails formatDetails, Format format, object current)
    {
        var formattingInfo = FormattingInfoPool.Pool.Get().Initialize(formatDetails, format, current);
        Format(formattingInfo);
        FormattingInfoPool.Pool.Release(formattingInfo);
    }

    /// <summary>
    /// Writes the formatting result into an <see cref="IOutput"/> instance.
    /// </summary>
    /// <param name="output">The <see cref="IOutput"/> where the result is written to.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The objects to format.</param>
    public void FormatInto(IOutput output, string format, params object[] args)
    {
        FormatInto(output, format, (IList<object>)args);
    }

    /// <summary>
    /// Writes the formatting result into an <see cref="IOutput"/> instance.
    /// </summary>
    /// <param name="output">The <see cref="IOutput"/> where the result is written to.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The objects to format.</param>
    public void FormatInto(IOutput output, string format, IList<object> args)
    {
        FormatInto(output, null, format, args);
    }

    /// <summary>
    /// Writes the formatting result into an <see cref="IOutput"/> instance.
    /// </summary>
    /// <param name="output">The <see cref="IOutput"/> where the result is written to.</param>
    /// <param name="provider">The <see cref="IFormatProvider"/> to use.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The objects to format.</param>
    public void FormatInto(IOutput output, IFormatProvider provider, string format, IList<object> args)
    {
        var formatParsed = Parser.ParseFormat(format); // The parser gets the Format from the pool
        try
        {
            FormatInto(output, provider, formatParsed, args);
        }
        finally
        {
            FormatPool.Pool.Release(formatParsed);
        }
    }

    /// <summary>
    /// Writes the formatting result into an <see cref="IOutput"/> instance.
    /// </summary>
    /// <param name="output">The <see cref="IOutput"/> where the result is written to.</param>
    /// <param name="provider">The <see cref="IFormatProvider"/> to use.</param>
    /// <param name="format">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The objects to format.</param>
    public void FormatInto(IOutput output, IFormatProvider provider, Format format, params object[] args)
    {
        FormatInto(output, provider, format, (IList<object>)args);
    }

    /// <summary>
    /// Writes the formatting result into an <see cref="IOutput"/> instance.
    /// </summary>
    /// <param name="output">The <see cref="IOutput"/> where the result is written to.</param>
    /// <param name="provider">The <see cref="IFormatProvider"/> to use.</param>
    /// <param name="formatParsed">An instance of <see cref="Core.Parsing.Format"/> that was returned by <see cref="Unity.SmartStrings.Core.Parsing.Parser.ParseFormat"/>.</param>
    /// <param name="args">The objects to format.</param>
    public void FormatInto(IOutput output, IFormatProvider provider, Format formatParsed, IList<object> args)
    {
        var current = args.Count > 0 ? args[0] : args; // The first item is the default.

        var formatDetails = FormatDetailsPool.Pool.Get().Initialize(this, formatParsed, args, provider, output);
        try
        {
            Format(formatDetails, formatParsed, current);
        }
        finally
        {
            FormatDetailsPool.Pool.Release(formatDetails);
        }
    }

    void FormatError(FormatItem errorItem, Exception innerException, int startIndex,
        IFormattingInfo formattingInfo)
    {
        OnFormattingFailure?.Invoke(this,
            new FormattingErrorEventArgs(errorItem.RawText, startIndex,
                Settings.Formatter.ErrorAction != FormatErrorAction.ThrowError));
        switch (Settings.Formatter.ErrorAction)
        {
            case FormatErrorAction.Ignore:
                return;
            case FormatErrorAction.ThrowError:
                throw innerException as FormattingException ??
                      new FormattingException(errorItem, innerException, startIndex);
            case FormatErrorAction.OutputErrorInResult:
                formattingInfo.FormatDetails.FormattingException =
                    innerException as FormattingException ??
                    new FormattingException(errorItem, innerException, startIndex);
                formattingInfo.Write(innerException.Message);
                formattingInfo.FormatDetails.FormattingException = null;
                break;
            case FormatErrorAction.MaintainTokens:
                formattingInfo.Write(formattingInfo.Placeholder?.RawText ?? "'null'");
                break;
        }
    }

    void CheckForExtensions()
    {
        if (SourceExtensions.Count == 0)
            throw new InvalidOperationException(
                "No source extensions are available. Please add at least one source extension, such as the DefaultSource.");
        if (FormatterExtensions.Count == 0)
            throw new InvalidOperationException(
                "No formatter extensions are available. Please add at least one formatter extension, such as the DefaultFormatter.");
    }

    void EvaluateSelectors(FormattingInfo formattingInfo)
    {
        if (formattingInfo.Placeholder is null) return;

        var firstSelector = true;
        foreach (var selector in formattingInfo.Placeholder.Selectors)
        {
            // Don't evaluate empty selectors
            // (used e.g. for Settings.Parser.NullableOperator and Settings.Parser.ListIndexEndChar final operators)
            if (selector.Length == 0) continue;

            formattingInfo.Selector = selector;
            // Do not evaluate alignment-only selectors
            if (formattingInfo.SelectorOperator.Length > 0 &&
                formattingInfo.SelectorOperator[0] == Settings.Parser.AlignmentOperator) continue;

            formattingInfo.Result = null;

            var handled = InvokeSourceExtensions(formattingInfo);
            if (handled) formattingInfo.CurrentValue = formattingInfo.Result;

            if (firstSelector)
            {
                firstSelector = false;
                // Handle "nested scopes" by traversing the stack:
                var parentFormattingInfo = formattingInfo;
                while (!handled && parentFormattingInfo.Parent != null)
                {
                    parentFormattingInfo = parentFormattingInfo.Parent;
                    parentFormattingInfo.Selector = selector;
                    parentFormattingInfo.Result = null;
                    handled = InvokeSourceExtensions(parentFormattingInfo);
                    if (handled) formattingInfo.CurrentValue = parentFormattingInfo.Result;
                }
            }

            if (!handled)
                throw formattingInfo.FormattingException($"No source extension could handle the selector named \"{selector.RawText}\"",
                    selector);
        }
    }

    bool InvokeSourceExtensions(FormattingInfo formattingInfo)
    {
        // less GC than using Linq
        foreach (var sourceExtension in SourceExtensions)
        {
            var handled = sourceExtension.TryEvaluateSelector(formattingInfo);
            if (handled) return true;
        }

        return false;
    }

    /// <summary>
    /// Try to get a suitable formatter.
    /// </summary>
    /// <param name="formattingInfo"></param>
    /// <exception cref="FormattingException"></exception>
    void EvaluateFormatters(FormattingInfo formattingInfo)
    {
        var handled = InvokeFormatterExtensions(formattingInfo);
        if (!handled)
            throw formattingInfo.FormattingException("No suitable Formatter could be found", formattingInfo.Format);
    }

    /// <summary>
    /// First check whether the named formatter name exist in of the <see cref="FormatterExtensions" />,
    /// next check whether the named formatter is able to process the format.
    /// </summary>
    /// <param name="formattingInfo"></param>
    /// <returns>True if an FormatterExtension was found, else False.</returns>
    bool InvokeFormatterExtensions(FormattingInfo formattingInfo)
    {
        if (formattingInfo.Placeholder is null)
        {
            throw new ArgumentException(
                $"{nameof(formattingInfo)}.{nameof(formattingInfo.Placeholder)} must not be null.");
        }

        var formatterName = formattingInfo.Placeholder.FormatterName;
        var comparison = Settings.GetCaseSensitivityComparison();

        // Compatibility mode does not support formatter extensions except this one:
        if (Settings.StringFormatCompatibility)
        {
            return
                FormatterExtensions.Find(fe => fe is DefaultFormatter)
                    .TryEvaluateFormat(formattingInfo);
        }

        // Try to evaluate using the not empty formatter name from the format string
        if (formatterName != string.Empty)
        {
            IFormatter formatterExtension = null;
            // less GC than using Linq
            foreach (var fe in FormatterExtensions)
            {
                if (!fe.Name.Equals(formatterName, comparison)) continue;

                formatterExtension = fe;
                break;
            }

            if (formatterExtension is null)
                throw formattingInfo.FormattingException($"No formatter with name '{formatterName}' found",
                    formattingInfo.Format, formattingInfo.Selector?.SelectorIndex ?? -1);

            return formatterExtension.TryEvaluateFormat(formattingInfo);
        }

        // Go through all (implicit) formatters which contain an empty name
        // much higher performance and less GC than using Linq
        foreach (var fe in FormatterExtensions)
        {
            if (!fe.CanAutoDetect) continue;
            if (fe.TryEvaluateFormat(formattingInfo)) return true;
        }

        return false;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        Parser = new Parser(Settings);

        // An extension whose type no longer resolves deserializes as null, and would fail every later format call.
        SourceExtensions.RemoveAll(source => source == null);
        FormatterExtensions.RemoveAll(formatter => formatter == null);

        // We initialize each time to set the non serialized values.
        foreach (var formatter in FormatterExtensions)
        {
            if (formatter is IInitializer initializer)
                initializer.Initialize(this);
        }

        foreach (var source in SourceExtensions)
        {
            if (source is IInitializer initializer)
                initializer.Initialize(this);
        }
    }
}
