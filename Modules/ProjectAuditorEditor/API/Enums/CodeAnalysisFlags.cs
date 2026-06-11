// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Options for the compilation mode Project Auditor should use when performing code analysis.
    /// </summary>
    /// <remarks>
    /// Setting the compilation mode can affect how long analysis takes, which assemblies are analysed and whether certain scripting define symbols are considered.
    /// </remarks>
    [Flags]
    public enum CodeAnalysisFlags
    {
        /// <summary>
        ///   <para>None</para>
        /// </summary>
        /// <remarks>
        /// No code will be compiled for analysis.
        /// </remarks>
        None = 0,
        /// <summary>
        ///   <para>Player</para>
        /// </summary>
        /// <remarks>
        /// Code will be compiled for analysis as it would be when making a Player build for the specified target platform.
        /// </remarks>
        Player = 1 << 0,
        /// <summary>
        ///   <para>Editor</para>
        /// </summary>
        /// <remarks>
        /// Analysis will be performed on Editor code assemblies. Select this option to analyze custom Editor code, including packages.
        /// </remarks>
        Editor = 1 << 1,

        [Obsolete("DevelopmentBuild flag is deprecated as a result of the DEVELOPMENT_BUILD C# preprocessor directive being deprecated. It will be removed in a future release. Please use DebugManagedCodeVariant instead.")]
        DevelopmentBuild = 1 << 2,
        /// <summary>
        ///   <para>Tests</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include code inside test assemblies.
        /// </remarks>
        Tests = 1 << 3,
        /// <summary>
        ///   <para>Packages</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include package code.
        /// </remarks>
        Packages = 1 << 4,

        /// <summary>
        ///   <para>DebugManagedCodeVariant</para>
        /// </summary>
        /// <remarks>
        /// Analysis will include code compiled for the Debug managed code variant, which includes code under the DEBUG, UNITY_ENABLE_CHECKS and UNITY_INCLUDE_INSTRUMENTATION C# preprocessor directives.
        /// </remarks>
        DebugManagedCodeVariant = 1 << 5,

        /// <summary>
        ///   <para>All</para>
        /// </summary>
        /// <remarks>
        /// Project Auditor analyzes all code.
        /// </remarks>
        All = ~None
    }

    // Keep combination enums out of the main enum, otherwise EditorGUILayout.EnumFlagsField shows them in the dropdown UI in the preferences.
    internal static class CodeAnalysisFlagsExtensions
    {
        internal static CodeAnalysisFlags Default => CodeAnalysisFlags.Player | CodeAnalysisFlags.Editor;
    }
}
