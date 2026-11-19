// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Specifies the comparison operator used by a condition to compare a value against the condition's reference value.
    /// </summary>
    /// <remarks>
    /// A condition records the operator and the reference value; it does not evaluate them.
    /// The consuming runtime performs the comparison. Ordering operators such as <see cref="Less"/>
    /// only apply to values whose type supports ordering, such as numeric types; other types support
    /// <see cref="Equal"/> and <see cref="NotEqual"/> only.
    /// </remarks>
    public enum ConditionComparison
    {
        /// <summary>
        /// The value equals the reference value.
        /// </summary>
        Equal,

        /// <summary>
        /// The value does not equal the reference value.
        /// </summary>
        NotEqual,

        /// <summary>
        /// The value is less than the reference value.
        /// </summary>
        Less,

        /// <summary>
        /// The value is less than or equal to the reference value.
        /// </summary>
        LessOrEqual,

        /// <summary>
        /// The value is greater than the reference value.
        /// </summary>
        Greater,

        /// <summary>
        /// The value is greater than or equal to the reference value.
        /// </summary>
        GreaterOrEqual,
    }
}
