// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;

namespace Unity.SmartStrings.Core.Settings;

/// <summary>
/// Class for <see cref="SmartFormatter"/> settings.
/// Properties should be considered as 'init-only' like implemented in C# 9.
/// Any changes after passing settings as argument to CTORs may not have effect.
/// </summary>
[Serializable]
public class FormatterSettings
{
    [SerializeField] FormatErrorAction m_ErrorAction = FormatErrorAction.ThrowError;

    [Tooltip("The character which is used for pre-aligning or post-aligning items (e.g.: {Placeholder,10} for an alignment width of 10). Default is space.")]
    [SerializeField] char m_AlignmentFillCharacter = ' ';

    /// <summary>
    /// Behavior that the <see cref="SmartFormatter" /> applies when a formatting error occurs.
    /// The default is <see cref="FormatErrorAction.ThrowError"/>.
    /// </summary>
    public FormatErrorAction ErrorAction { get => m_ErrorAction; set => m_ErrorAction = value; }

    /// <summary>
    /// Character used to pre-align or post-align items, for example <c>{Placeholder,10}</c> for an alignment width of 10.
    /// The default is the space character (0x20).
    /// </summary>
    public char AlignmentFillCharacter { get => m_AlignmentFillCharacter; set => m_AlignmentFillCharacter = value; }
}
