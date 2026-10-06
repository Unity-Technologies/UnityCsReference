// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Text;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Output;

/// <summary>
/// Wraps a <see cref="StringBuilder"/> so it can be used for output.
/// </summary>
/// <remarks>
/// <see cref="StringBuilder"/>, <see cref="UnicodeEncoding"/>
/// and <see langword="string"/> objects use <b>UTF-16</b> encoding to store characters.
/// </remarks>
public class StringOutput : IOutput
{
    readonly StringBuilder m_Output;

    /// <summary>
    /// Creates a new instance of <see cref="StringOutput"/>.
    /// </summary>
    public StringOutput()
    {
        m_Output = new StringBuilder();
    }

    /// <summary>
    /// Creates a new instance of <see cref="StringOutput"/> with the given capacity.
    /// </summary>
    /// <param name="capacity">The estimated capacity for the result string. Essential for performance and GC pressure.</param>
    public StringOutput(int capacity)
    {
        m_Output = new StringBuilder(capacity);
    }

    /// <summary>
    /// Creates a new instance of <see cref="StringOutput"/> using the given <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="output">Existing <see cref="StringBuilder"/> to append output to.</param>
    public StringOutput(StringBuilder output)
    {
        m_Output = output;
    }

    /// <summary>
    /// Writes text to the <see cref="StringBuilder"/> object.
    /// </summary>
    /// <param name="text">Text to write.</param>
    /// <param name="formattingInfo">This parameter from <see cref="IOutput"/> will not be used here.</param>
    public void Write(string text, IFormattingInfo formattingInfo = null)
    {
        m_Output.Append(text);
    }

    /// <summary>
    /// Writes text to the <see cref="StringBuilder"/> object.
    /// </summary>
    /// <param name="text">Characters to write.</param>
    /// <param name="formattingInfo">This parameter from <see cref="IOutput"/> will not be used here.</param>
    public void Write(ReadOnlySpan<char> text, IFormattingInfo formattingInfo = null)
    {
        m_Output.Append(text);
    }

    /// <summary>
    /// Writes a character repeated <paramref name="count"/> times to the <see cref="StringBuilder"/> object.
    /// </summary>
    /// <param name="value">Character to write.</param>
    /// <param name="count">Number of times to write <paramref name="value"/>.</param>
    /// <param name="formattingInfo">This parameter from <see cref="IOutput"/> will not be used here.</param>
    public void Write(char value, int count, IFormattingInfo formattingInfo = null)
    {
        m_Output.Append(value, count);
    }

    /// <summary>
    /// Clears the <see cref="StringBuilder"/> used to create the output.
    /// This method gets called by <see cref="StringOutputPool"/> when it releases an instance.
    /// </summary>
    public void Clear()
    {
        m_Output.Clear();
    }

    /// <summary>
    /// Returns the results of the <see cref="StringBuilder"/>.
    /// </summary>
    public override string ToString()
    {
        return m_Output.ToString();
    }
}
