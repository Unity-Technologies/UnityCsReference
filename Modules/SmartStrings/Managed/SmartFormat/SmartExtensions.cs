// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System.IO;
using System.Text;
using Unity.SmartStrings.Core.Output;

namespace Unity.SmartStrings;

/// <summary>
/// Provides extension methods that apply Smart String formatting to <see cref="StringBuilder"/>, <see cref="TextWriter"/>, and <see cref="string"/>.
/// </summary>
public static class SmartExtensions
{
    /// <summary> Appends a formatted string, using the same semantics as <see cref="Smart"/>.Format. </summary>
    /// <param name="sb">Target <see cref="StringBuilder"/> for the formatted output.</param>
    /// <param name="format">Composite format string that defines how the arguments are formatted.</param>
    /// <param name="args">Arguments to insert into the formatted output.</param>
    public static void AppendSmart(this StringBuilder sb, string format, params object[] args)
    {
        var output = new StringOutput(sb);
        Smart.Default.FormatInto(output, format, args);
    }

    /// <summary> Appends a formatted string followed by a line terminator, using the same semantics as <see cref="Smart"/>.Format. </summary>
    /// <param name="sb">Target <see cref="StringBuilder"/> for the formatted output.</param>
    /// <param name="format">Composite format string that defines how the arguments are formatted.</param>
    /// <param name="args">Arguments to insert into the formatted output.</param>
    public static void AppendLineSmart(this StringBuilder sb, string format, params object[] args)
    {
        AppendSmart(sb, format, args);
        sb.AppendLine();
    }

    /// <summary> Writes a formatted string, using the same semantics as <see cref="Smart"/>.Format. </summary>
    /// <param name="writer">Target <see cref="TextWriter"/> for the formatted output.</param>
    /// <param name="format">Composite format string that defines how the arguments are formatted.</param>
    /// <param name="args">Arguments to insert into the formatted output.</param>
    public static void WriteSmart(this TextWriter writer, string format, params object[] args)
    {
        var output = new TextWriterOutput(writer);
        Smart.Default.FormatInto(output, format, args);
    }

    /// <summary> Writes a formatted string followed by a line terminator, using the same semantics as <see cref="Smart"/>.Format. </summary>
    /// <param name="writer">Target <see cref="TextWriter"/> for the formatted output.</param>
    /// <param name="format">Composite format string that defines how the arguments are formatted.</param>
    /// <param name="args">Arguments to insert into the formatted output.</param>
    public static void WriteLineSmart(this TextWriter writer, string format, params object[] args)
    {
        WriteSmart(writer, format, args);
        writer.WriteLine();
    }

    /// <summary> Formats the specified arguments, using this string as the composite format string. </summary>
    /// <param name="format">Composite format string that defines how the arguments are formatted.</param>
    /// <param name="args">Arguments to insert into the formatted output.</param>
    /// <returns>A new string with the format items replaced by the formatted arguments.</returns>
    public static string FormatSmart(this string format, params object[] args)
    {
        return Smart.Format(format, args);
    }
}
