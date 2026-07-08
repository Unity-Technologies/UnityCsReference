// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by CodeModule to a CodeModulePrecompiledAssemblyAnalyzer's Analyze() method.
    /// </summary>
    public class PrecompiledAssemblyAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The path to the Assembly.
        /// </summary>
        public string AssemblyPath;

        /// <summary>
        /// The Target Framework version.
        /// </summary>
        public string TargetFramework;
    }

    /// <summary>
    /// Abstract base class for a Precompiled Assembly analyzer
    /// </summary>
    public abstract class CodeModulePrecompiledAssemblyAnalyzer : CodeModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues in precompiled assemblies, and construct a ReportItemBuilder object with
        /// basic information about a ReportItem object to describe the issue.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>A collection of ReportItemBuilder objects</returns>
        public abstract IEnumerable<ReportItemBuilder> Analyze(PrecompiledAssemblyAnalysisContext context);
    }
}
