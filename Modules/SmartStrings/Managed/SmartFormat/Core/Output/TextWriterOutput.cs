// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.IO;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.Core.Output;

/// <summary>
/// Wraps a <see cref="TextWriter"/> so that it can be used for output.
/// </summary>
public class TextWriterOutput : IOutput
{
    /// <summary>
    /// Creates a new instance of <see cref="TextWriterOutput"/>.
    /// </summary>
    /// <param name="output"><see cref="TextWriter"/> to use for output.</param>
    public TextWriterOutput(TextWriter output)
    {
        Output = output;
    }

    /// <summary>
    /// <see cref="TextWriter"/> that receives the formatted text.
    /// </summary>
    public TextWriter Output { get; }

    ///<inheritdoc/>
    public void Write(string text, IFormattingInfo formattingInfo = null)
    {
        Output.Write(text);
    }

    ///<inheritdoc/>
    public void Write(ReadOnlySpan<char> text, IFormattingInfo formattingInfo = null)
    {
        Output.Write(text);
    }

    ///<inheritdoc/>
    public void Write(char value, int count, IFormattingInfo formattingInfo = null)
    {
        for (var i = 0; i < count; i++)
            Output.Write(value);
    }
}
