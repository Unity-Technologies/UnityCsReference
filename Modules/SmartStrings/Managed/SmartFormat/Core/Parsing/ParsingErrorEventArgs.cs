// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Supplies information about parsing errors.
/// </summary>
public class ParsingErrorEventArgs : EventArgs
{
    internal ParsingErrorEventArgs(ParsingErrors errors, bool throwsException)
    {
        Errors = errors;
        ThrowsException = throwsException;
    }

    /// <summary>
    /// All parsing errors which occurred during parsing.
    /// </summary>
    public ParsingErrors Errors { get; internal set; }

    /// <summary>
    /// If <see langword="true"/>, errors will throw an exception.
    /// </summary>
    public bool ThrowsException { get; internal set; }
}
