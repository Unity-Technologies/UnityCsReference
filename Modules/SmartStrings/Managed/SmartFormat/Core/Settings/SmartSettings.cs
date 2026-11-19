// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Unity.SmartStrings.Core.Settings;

/// <summary>
/// <see cref="Smart" /> settings to be applied for parsing and formatting.
/// <see cref="SmartSettings"/> are used to initialize instances.
/// Properties should be considered as 'init-only' like implemented in C# 9.
/// Any changes after passing settings as argument to CTORs may not have effect,
/// unless explicitly mentioned.
/// </summary>
[Serializable]
public class SmartSettings
{
    [Tooltip("Determines whether placeholders are case-sensitive or not.")]
    [SerializeField] CaseSensitivityType m_CaseSensitivity = CaseSensitivityType.CaseSensitive;

    [SerializeField] ParserSettings m_Parser = new();
    [SerializeField] FormatterSettings m_Formatter = new();

    /// <summary>
    /// Gets the thread safety mode.
    /// Thread safety is relevant for global caching, lists and object pools,
    /// which can be filled from different threads concurrently.
    /// <see langword="true"/> does <b>not</b> guarantee thread safety of all classes.
    /// Always <see langword="false"/>: the shared object pools are single-threaded,
    /// so formatting from multiple threads concurrently is not supported.
    /// </summary>
    internal static bool IsThreadSafeMode { get; } = false; // UNITY - Disabled but left in case we want to use it in the future.

    /// <summary>
    /// Uses <c>string.Format</c>-compatible escaping of curly braces, {{ and }},
    /// instead of the <c>Smart.Format</c> default escaping, \{ and \}.
    /// Custom formatters cannot be parsed / used, if set to <see langword="true"/>.
    /// Default is <see langword="false"/>.
    /// </summary>
    internal bool StringFormatCompatibility { get; set; }
    /// <summary>
    /// Determines whether placeholders are case-sensitive or not.
    /// The default is <see cref="CaseSensitivityType.CaseSensitive"/>.
    /// </summary>
    public CaseSensitivityType CaseSensitivity { get => m_CaseSensitivity; set => m_CaseSensitivity = value; }
    /// <summary>
    /// Gets the <see cref="StringComparer"/> that belongs to the <see cref="CaseSensitivity"/> setting.
    /// </summary>
    /// <returns>The <see cref="StringComparer"/> that belongs to the <see cref="CaseSensitivity"/> setting.</returns>
    public IEqualityComparer<string> GetCaseSensitivityComparer()
    {
        return CaseSensitivity == CaseSensitivityType.CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Gets the <see cref="StringComparison"/> that belongs to the <see cref="CaseSensitivity"/> setting.
    /// </summary>
    /// <returns>The <see cref="StringComparison"/> that belongs to the <see cref="CaseSensitivity"/> setting.</returns>
    public StringComparison GetCaseSensitivityComparison()
    {
        return CaseSensitivity == CaseSensitivityType.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Settings that configure parsing.
    /// Set only during initialization.
    /// </summary>
    public ParserSettings Parser { get => m_Parser; set => m_Parser = value; }

    /// <summary>
    /// Settings that configure formatting.
    /// Set only during initialization.
    /// </summary>
    public FormatterSettings Formatter { get => m_Formatter; set => m_Formatter = value; }

}
