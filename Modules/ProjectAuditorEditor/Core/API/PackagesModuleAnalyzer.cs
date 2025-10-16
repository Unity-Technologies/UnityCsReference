// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by PackagesModule to a PackagesModuleAnalyzer's Analyze() method.
    /// </summary>
    public class PackageAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// Information about a Unity package to be analyzed.
        /// </summary>
        public PackageInfo PackageInfo;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by PackagesModule
    /// </summary>
    public abstract class PackagesModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(PackageAnalysisContext context);
    }
}
