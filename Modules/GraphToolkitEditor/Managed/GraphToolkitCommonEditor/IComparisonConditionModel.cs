// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for condition models that expose an editable comparison operator, so that the comparison command
    /// and dropdown UI can be shared between them.
    /// </summary>
    interface IComparisonConditionModel
    {
        /// <summary>
        /// The comparison operator.
        /// </summary>
        ConditionComparison Comparison { get; set; }
    }
}
