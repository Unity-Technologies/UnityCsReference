// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by ShadersModule to a ShaderModuleAnalyzer's Analyze() method.
    /// </summary>
    public class ShaderAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// A path to a shader asset in the project.
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// The shader object to be analyzed.
        /// </summary>
        public Shader Shader;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by ShaderModule
    /// </summary>
    public abstract class ShaderModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(ShaderAnalysisContext context);
    }
}
