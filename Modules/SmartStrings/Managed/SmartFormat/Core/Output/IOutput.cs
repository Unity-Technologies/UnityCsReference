// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.Core.Output;

/// <summary>
/// Represents a destination for formatted text.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// Writes a string to the output.
    /// </summary>
    /// <param name="text">Text to write.</param>
    /// <param name="formattingInfo">Formatting context for the write operation.</param>
    void Write(string text, IFormattingInfo formattingInfo = null);

    /// <summary>
    /// Writes a <see cref="ReadOnlySpan{T}"/> of characters to the output.
    /// </summary>
    /// <param name="text">Characters to write.</param>
    /// <param name="formattingInfo">Formatting context for the write operation.</param>
    void Write(ReadOnlySpan<char> text, IFormattingInfo formattingInfo = null);

    /// <summary>
    /// Writes a <see cref="char"/> repeated <paramref name="count"/> times to the output.
    /// </summary>
    /// <param name="value">Character to write.</param>
    /// <param name="count">Number of times to write <paramref name="value"/>.</param>
    /// <param name="formattingInfo">Formatting context for the write operation.</param>
    void Write(char value, int count, IFormattingInfo formattingInfo = null);
}
