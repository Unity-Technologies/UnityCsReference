// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by AssetsModule to an AssetsModuleAnalyzer's Analyze() method.
    /// </summary>
    public class AssetAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// A path to an asset, of any type, in the project.
        /// </summary>
        public string AssetPath;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by AssetsModule
    /// </summary>
    public abstract class AssetsModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(AssetAnalysisContext context);
    }
}
