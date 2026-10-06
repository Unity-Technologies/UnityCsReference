// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Lets a source stop formatter extensions from running for the placeholder it resolved.
/// </summary>
/// <remarks>
/// <see cref="SmartFormatter"/> passes a <see cref="Formatting.FormattingInfo"/>, which implements this interface,
/// as the <see cref="ISelectorInfo"/> argument of <see cref="ISource.TryEvaluateSelector"/>. Cast that argument to
/// <see cref="IFormattingExtensionsToggle"/> and set <see cref="DisableFormattingExtensions"/> to <see langword="true"/>
/// when default formatting can't reasonably handle the value the source found. The source can then write the output
/// itself with <see cref="IFormattingInfo.Write(string)"/>, or write nothing.
/// </remarks>
/// <example>
/// <code source="../../../../../Modules/SmartStrings/Tests/UTFTests/SmartFormat.Samples/MaskedSource.cs"/>
/// </example>
/// <seealso cref="ISource"/>
/// <seealso cref="Formatting.FormattingInfo"/>
public interface IFormattingExtensionsToggle
{
    /// <summary>
    /// Gets or sets whether formatter extensions are skipped for the current placeholder.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false"/>, and it resets for every placeholder. When a source sets it to
    /// <see langword="true"/> in <see cref="ISource.TryEvaluateSelector"/>, no formatter runs for that placeholder,
    /// so only what the source writes appears in the output.
    /// </remarks>
    bool DisableFormattingExtensions { get; set; }
}
