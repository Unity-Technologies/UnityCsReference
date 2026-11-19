// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Base class that implements common <see cref="IFormatter"/> functionality.
/// </summary>
/// <example>
/// This example shows how to create a formatter to format an integer that represents bytes.
/// <code source="../../../../../../Modules/SmartStrings/Tests/UTFTests/SmartFormat.Samples/ByteFormatter.cs"/>
/// </example>
[Serializable]
public abstract partial class FormatterBase : IFormatter
{
    [Tooltip("The name to use when explicitly calling the extension. For example, \"{0:list: N2}\" will explicitly call the \"list\" extension. ")]
    [SerializeField] string m_Name;

    [Tooltip("Any extensions marked as CanAutoDetect will be called implicitly (when no formatter name is specified in the input format string)." +
        "For example, \"{0:N2}\" will implicitly call extensions marked as CanAutoDetect. " +
        "When disabled, the formatter can only be called by its name in the input format string." +
        "If more than one registered Formatter can auto-detect, the first one in the formatter list will be used.")]
    [SerializeField] bool m_CanAutoDetect;

    /// <inheritdoc/>
    public string Name
    {
        get => m_Name;
        set => m_Name = value;
    }

    /// <inheritdoc/>
    public virtual bool CanAutoDetect { get => m_CanAutoDetect; set => m_CanAutoDetect = value; }

    /// <summary>
    /// Default name to use when <see cref="Name"/> is <see langword="null"/>.
    /// </summary>
    public abstract string DefaultName { get; }

    /// <summary>
    /// Creates a new instance of the formatter.
    /// </summary>
    public FormatterBase()
    {
        m_Name = DefaultName;
    }

    /// <inheritdoc/>
    public abstract bool TryEvaluateFormat(IFormattingInfo formattingInfo);
}
