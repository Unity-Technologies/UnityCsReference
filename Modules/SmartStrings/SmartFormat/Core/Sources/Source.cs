// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// The base class for <see cref="ISource"/> extension classes.
/// </summary>
[Serializable]
public abstract class Source : ISource, IInitializer
{
    /// <summary>
    /// The instance of the current <see cref="SmartFormatter"/>.
    /// </summary>
    protected SmartFormatter m_Formatter;

    /// <summary>
    /// The instance of the current <see cref="SmartSettings"/>.
    /// </summary>
    protected SmartSettings m_SmartSettings;

    /// <inheritdoc />
    public abstract bool TryEvaluateSelector(ISelectorInfo selectorInfo);

    /// <inheritdoc />
    public virtual void Initialize(SmartFormatter smartFormatter)
    {
        m_Formatter = smartFormatter;
        m_SmartSettings = smartFormatter.Settings;
    }

    /// <summary>
    /// Checks whether any of the <see cref="Placeholder"/>'s <see cref="Placeholder.Selectors"/>,
    /// up to and including the selector at <see cref="ISelectorInfo.SelectorIndex"/>,
    /// has the nullable <c>?</c> as the first character of its operator.
    /// </summary>
    /// <param name="selectorInfo">The <see cref="ISelectorInfo"/> for the selector currently being evaluated.</param>
    /// <returns>
    /// <see langword="true"/> if any of those selectors has the nullable <c>?</c> as the first character of its operator.
    /// </returns>
    /// <remarks>
    /// A nullable operator on a later selector in the same <see cref="Placeholder"/> does not affect earlier selectors.
    /// The nullable operator '?' can be followed by a dot (like '?.') or a square brace (like '?[')
    /// </remarks>
    internal bool HasNullableOperator(ISelectorInfo selectorInfo)
    {
        if (m_SmartSettings != null && selectorInfo.Placeholder != null)
        {
            foreach (var s in selectorInfo.Placeholder.Selectors)
            {
                if (s.SelectorIndex <= selectorInfo.SelectorIndex
                    && s.OperatorLength > 1
                    && s.BaseString[s.OperatorStartIndex] == m_SmartSettings.Parser.NullableOperator)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// If any of the <see cref="Placeholder"/>'s <see cref="Placeholder.Selectors"/> has
    /// nullable <c>?</c> as their first operator, and <see cref="ISelectorInfo.CurrentValue"/>
    /// is <see langword="null"/>, <see cref="ISelectorInfo.Result"/> will be set to <see langword="null"/>.
    /// </summary>
    /// <param name="selectorInfo">Selector to evaluate for a nullable operator.</param>
    /// <returns>
    /// <see langword="true"/>, if any of the <see cref="Placeholder"/>'s
    /// <see cref="Placeholder.Selectors"/> has  nullable <c>?</c> as their first
    /// operator, and <see cref="ISelectorInfo.CurrentValue"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The nullable operator '?' can be followed by a dot (like '?.') or a square brace (like '.[')
    /// </remarks>
    protected virtual bool TrySetResultForNullableOperator(ISelectorInfo selectorInfo)
    {
        if (HasNullableOperator(selectorInfo) && selectorInfo.CurrentValue is null)
        {
            selectorInfo.Result = null;
            return true;
        }

        return false;
    }
}
