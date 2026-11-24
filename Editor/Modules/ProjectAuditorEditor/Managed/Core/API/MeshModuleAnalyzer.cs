// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by MeshModule to a MeshModuleAnalyzer's Analyze() method.
    /// </summary>
    public class MeshAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// The name of a Mesh asset to be analyzed.
        /// </summary>
        public string Name;

        /// <summary>
        /// The Mesh asset to be analyzed.
        /// </summary>
        public Mesh Mesh;

        /// <summary>
        /// The Mesh asset's AssetImporter
        /// </summary>
        /// <remarks>
        /// Meshes can be created from source assets by a number of different types of importer in Unity. Therefore,
        /// it's important to check the results of any attempts to cast this AssetImporter to an inherited importer type
        /// to ensure the cast was successful.
        /// </remarks>
        public AssetImporter Importer;

        /// <summary>
        /// An estimate of the Mesh's runtime memory footprint.
        /// </summary>
        public long Size;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by MeshModule
    /// </summary>
    public abstract class MeshModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(MeshAnalysisContext context);
    }
}
