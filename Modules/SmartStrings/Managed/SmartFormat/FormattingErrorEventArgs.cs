// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;

namespace Unity.SmartStrings;

/// <summary>
/// Supplies information about formatting errors.
/// </summary>
public class FormattingErrorEventArgs : EventArgs
{
    internal FormattingErrorEventArgs(string rawText, int errorIndex, bool ignoreError)
    {
        Placeholder = rawText;
        ErrorIndex = errorIndex;
        IgnoreError = ignoreError;
    }

    /// <summary>
    /// Raw text of the format item that caused the error.
    /// </summary>
    public string Placeholder { get; }

    /// <summary>
    /// Character index in the format string where the error occurred.
    /// </summary>
    public int ErrorIndex { get; }

    /// <summary>
    /// Whether the error is ignored instead of raising an exception.
    /// </summary>
    public bool IgnoreError { get; }
}
