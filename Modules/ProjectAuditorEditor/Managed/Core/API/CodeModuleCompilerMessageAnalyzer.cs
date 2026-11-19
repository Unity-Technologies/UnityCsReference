// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by CodeModule to a CompilerMessageAnalyzer's Analyze() method.
    /// </summary>
    internal class CompilerMessageAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The compiler message.
        /// </summary>
        public AssemblyUtils.CompilerMessage Message;
    }

    /// <summary>
    /// Abstract base class for a compiler message analyzer
    /// </summary>
    internal abstract class CodeModuleCompilerMessageAnalyzer : CodeModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to process messages emitted by the compiler, and construct a ReportItemBuilder object with
        /// basic information about a ReportItem object to describe the issue.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>A collection of ReportItemBuilder objects</returns>
        public abstract IEnumerable<ReportItemBuilder> Analyze(CompilerMessageAnalysisContext context);
    }
}
