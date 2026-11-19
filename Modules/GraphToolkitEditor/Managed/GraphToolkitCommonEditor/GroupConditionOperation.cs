// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Specifies the logical operation a group condition applies to its nested conditions.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GroupConditionOperation"/> to interpret how an <see cref="IGroupCondition"/>
    /// combines the results of its nested conditions.
    /// </remarks>
    public enum GroupConditionOperation
    {
        /// <summary>
        /// The group is met when all its nested conditions are met.
        /// </summary>
        And,

        /// <summary>
        /// The group is met when at least one of its nested conditions is met.
        /// </summary>
        Or,
    }
}
