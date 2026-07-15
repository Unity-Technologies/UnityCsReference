// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    /// <summary>
    /// A context object passed by AnimationModule to an AnimationClipAnalyzer's Analyze() method.
    /// </summary>
    public class AnimationClipAnalysisContext : AssetAnalysisContext
    {
        /// <summary>
        /// The animation clip.
        /// </summary>
        public AnimationClip Clip;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by AnimationModule.
    /// </summary>
    public abstract class AnimationClipAnalyzer : AnimationModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(AnimationClipAnalysisContext context);
    }
}
