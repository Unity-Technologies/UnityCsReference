// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by a MaterialModule to a MaterialModuleAnalyzer's Analyze() method.
    /// </summary>
    public class MaterialAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The name of a material asset in the project
        /// </summary>
        public string Name;

        /// <summary>
        /// The material asset to be analyzed.
        /// </summary>
        public Material Material;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by MaterialModule
    /// </summary>
    public abstract class MaterialModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(MaterialAnalysisContext context);
    }
}
