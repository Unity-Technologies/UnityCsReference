// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Evaluates a selector.
/// </summary>
/// <remarks>
/// Implement this interface, or derive from <see cref="Source"/>, to resolve placeholder
/// selectors from your own value types, then register the source on the
/// <see cref="Unity.SmartStrings.SmartFormatter"/>. The module does not ship a
/// reflection-based source; the example shows how to write one that looks up members by name.
/// </remarks>
/// <example>
/// <code source="../../../../../../Modules/SmartStrings/Tests/UTFTests/SmartFormat.Samples/ReflectionSource.cs"/>
/// </example>
public interface ISource
{
    /// <summary>
    /// Evaluates the <see cref="Selector" /> based on the <see cref="ISelectorInfo.CurrentValue" />.
    /// </summary>
    /// <param name="selectorInfo">Selector to evaluate, along with its current value and result.</param>
    /// <returns><see langword="true"/> if the <see cref="Selector"/> was evaluated and <see cref="ISelectorInfo.Result" /> was set; otherwise, <see langword="false"/>.</returns>
    bool TryEvaluateSelector(ISelectorInfo selectorInfo);
}
