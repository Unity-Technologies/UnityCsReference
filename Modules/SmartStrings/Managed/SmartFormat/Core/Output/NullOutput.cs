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
/// Noop implementation of <see cref="IOutput"/>
/// </summary>
/// <remarks>
/// Useful for performance tests excluding the result string generation.
/// </remarks>
internal class NullOutput : IOutput
{
    /// <summary>
    /// Creates a new instance of <see cref="NullOutput"/>.
    /// </summary>
    public NullOutput()
    {
        // Nothing to do here
    }

    ///<inheritdoc/>
    public void Write(string text, IFormattingInfo formattingInfo = null)
    {
        // Nothing to do here
    }

    ///<inheritdoc/>
    public void Write(ReadOnlySpan<char> text, IFormattingInfo formattingInfo = null)
    {
        // Nothing to do here
    }

    ///<inheritdoc/>
    public void Write(char value, int count, IFormattingInfo formattingInfo = null)
    {
        // Nothing to do here
    }

    /// <summary>
    /// Always return <see cref="string.Empty"/>.
    /// </summary>
    public override string ToString()
    {
        return string.Empty;
    }
}
