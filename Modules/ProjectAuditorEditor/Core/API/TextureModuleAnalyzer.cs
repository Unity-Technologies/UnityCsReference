// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by a TextureModule to a TextureModuleAnalyzer's Analyze() method.
    /// </summary>
    public class TextureAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The name of a texture asset in the project
        /// </summary>
        public string Name;

        /// <summary>
        /// The texture asset to be analyzed.
        /// </summary>
        public Texture Texture;

        /// <summary>
        /// The TextureImporter used to import the texture to be analyzed.
        /// </summary>
        public TextureImporter Importer;

        /// <summary>
        /// The texture importer's platform settings, matching the target analysis platform.
        /// </summary>
        public TextureImporterPlatformSettings ImporterPlatformSettings;

        /// <summary>
        /// An estimate of the texture's runtime memory footprint.
        /// </summary>
        public long Size;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by TextureModule
    /// </summary>
    public abstract class TextureModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(TextureAnalysisContext context);
    }
}
