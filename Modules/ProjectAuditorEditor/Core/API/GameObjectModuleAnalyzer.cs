// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Describes where a GameObject is stored.
    /// </summary>
    public enum GameObjectSource
    {
        /// <summary>
        /// The GameObject belongs to a Scene.
        /// </summary>
        Scene,
        /// <summary>
        /// The GameObject belongs to a Prefab.
        /// </summary>
        Prefab
    }

    /// <summary>
    /// A context object passed by GameObjectModule to an GameObjectModuleAnalyzer's Analyze() method.
    /// </summary>
    public class GameObjectAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The GameObject to analyze.
        /// </summary>
        public GameObject GameObject;

        /// <summary>
        /// The owner of this GameObject (Scene or Prefab).
        /// </summary>
        public GameObjectSource Source;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by GameObjectModule
    /// </summary>
    public abstract class GameObjectModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to initialize the analyzer at the start of analysis
        /// </summary>
        internal virtual void OnAnalysisStarted() {}

        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItemBuilder objects</returns>
        public abstract IEnumerable<ReportItemBuilder> Analyze(GameObjectAnalysisContext context);
    }
}
