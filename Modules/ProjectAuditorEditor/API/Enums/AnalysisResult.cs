// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Identifiers for the results of analysis for a Module and for a whole Report
    /// </summary>
    public enum AnalysisResult
    {
        /// <summary>
        /// Analysis is still in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Analysis completed successfully
        /// </summary>
        Success,

        /// <summary>
        /// Analysis failed
        /// </summary>
        Failure,

        /// <summary>
        /// Analysis was cancelled by the user
        /// </summary>
        Cancelled
    }
}
