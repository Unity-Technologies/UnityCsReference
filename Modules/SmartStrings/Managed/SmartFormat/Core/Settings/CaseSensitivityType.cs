// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

namespace Unity.SmartStrings.Core.Settings;

/// <summary>
/// Options for whether strings are processed case-sensitively.
/// </summary>
public enum CaseSensitivityType
{
    /// <summary>
    /// Distinguishes between uppercase and lowercase characters.
    /// </summary>
    CaseSensitive,

    /// <summary>
    /// Ignores differences between uppercase and lowercase characters.
    /// </summary>
    CaseInsensitive
}
