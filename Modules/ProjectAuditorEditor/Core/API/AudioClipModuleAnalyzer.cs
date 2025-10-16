// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by AudioClipModule to an AudioClipModuleAnalyzer's Analyze() method.
    /// </summary>
    public class AudioClipAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The name of an AudioClip in the project.
        /// </summary>
        public string Name;

        /// <summary>
        /// The AudioClip to be analyzed.
        /// </summary>
        public AudioClip AudioClip;

        /// <summary>
        /// The AudioImporter used to import the AudioClip to be analyzed.
        /// </summary>
        public AudioImporter Importer;

        /// <summary>
        /// The AudioImporter's sample settings.
        /// </summary>
        public AudioImporterSampleSettings SampleSettings;

        /// <summary>
        /// The file size of the imported AudioClip.
        /// </summary>
        public long ImportedSize;

        /// <summary>
        /// An estimate of the runtime memory footprint of this AudioClip, when it's playing.
        /// </summary>
        public long RuntimeSize;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by AudioClipModule
    /// </summary>
    public abstract class AudioClipModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(AudioClipAnalysisContext context);
    }
}
