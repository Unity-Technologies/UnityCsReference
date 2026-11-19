// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Output;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Formatting;

/// <summary>
/// Contains extra information about the item currently being formatted.
/// These objects are not often used, so they are all wrapped up here.
/// </summary>
public class FormatDetails
{
    /// <summary>
    /// Initializes the <see cref="FormatDetails"/> instance.
    /// </summary>
    /// <param name="formatter">Formatter responsible for the formatting operation.</param>
    /// <param name="originalFormat">Parsed format produced by the parser.</param>
    /// <param name="originalArgs">Arguments referenced by the format string.</param>
    /// <param name="provider">Format provider used for culture-specific formatting.</param>
    /// <param name="output">Destination that receives the formatted result.</param>
    /// <returns>This <see cref="FormatDetails"/> instance.</returns>
    public FormatDetails Initialize(SmartFormatter formatter, Format originalFormat, IList<object> originalArgs,
        IFormatProvider provider, IOutput output)
    {
        Formatter = formatter;
        OriginalFormat = originalFormat;
        OriginalArgs = originalArgs;
        Provider = provider;
        Output = output;
        FormattingException = null;

        return this;
    }

    /// <summary>
    /// The original formatter responsible for formatting this item.
    /// It can be used for evaluating nested formats.
    /// </summary>
    public SmartFormatter Formatter { get; private set; }

    /// <summary>
    /// The original parsed <see cref="Format"/> produced by the <see cref="Parser"/>.
    /// </summary>
    public Format OriginalFormat { get; private set; }

    /// <summary>
    /// The original set of arguments passed to the format method.
    /// These provide global-access to the original arguments.
    /// </summary>
    public IList<object> OriginalArgs { get; private set; }

    /// <summary>
    /// The <see cref="IFormatProvider"/> that can be used to determine how to
    /// format items such as numbers, dates, and anything else that
    /// might be culture-specific.
    /// </summary>
    public IFormatProvider Provider { get; internal set; }

    /// <summary>
    /// The <see cref="IOutput"/> where the formatting result is written.
    /// </summary>
    public IOutput Output { get; private set; }

    /// <summary>
    /// If ErrorAction is set to OutputErrorsInResult, this will
    /// contain the exception that caused the formatting error.
    /// </summary>
    public FormattingException FormattingException { get; set; }

    /// <summary>
    /// Contains case-sensitivity and other settings.
    /// </summary>
    public SmartSettings Settings => Formatter.Settings;

    /// <summary>
    /// Clears all internal objects.
    /// </summary>
    internal void Clear()
    {
        Formatter = null;
        OriginalFormat = null;
        OriginalArgs = null;
        Output = null;
        Provider = null;
        FormattingException = null;
    }
}
