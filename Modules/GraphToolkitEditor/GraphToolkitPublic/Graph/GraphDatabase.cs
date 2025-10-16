// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides functionality needed to access, and perform operations on, graph assets.
    /// </summary>
    /// <remarks>
    /// The <c>GraphDatabase</c> class is similar to Unity's <see cref="UnityEditor.AssetDatabase"/>, but it is tailored for graph-based tools.
    /// Use this class to create, load, and save <see cref="Graph"/> instances and their associated assets.
    /// This API supports typical asset workflows such as creating new graph assets in the Project window, accessing graphs by path or GUID,
    /// and ensuring changes to graph data are saved.
    /// <br/>
    /// <br/>
    /// Use <see cref="PromptInProjectBrowserToCreateNewAsset{T}"/> to create and name a new asset,
    /// <see cref="CreateGraph{T}"/> to generate an asset file, and <see cref="LoadGraph{T}"/> to retrieve an existing one.
    /// <br/>
    /// <br/>
    /// Use <see cref="SaveGraphIfDirty"/> to persist graph data changes, and <see cref="LoadGraphForImporter{T}"/> to load a clean instance during import.
    /// </remarks>
    public static partial class GraphDatabase
    {
        /// <summary>
        /// Creates a new graph asset and activates the naming field in the Project Browser.
        /// </summary>
        /// <typeparam name="T">
        /// The type of graph to create. Must inherit from <see cref="Graph"/> and have a public parameterless constructor.
        /// </typeparam>
        /// <param name="defaultName">The default name for the new asset if the user does not rename it. Defaults to "New Graph" if not specified.</param>
        /// <remarks>
        /// Use this method to create a new graph asset directly from the editor UI.
        /// This action opens the Project Browser with the asset selected and its name field ready for editing.
        /// If the user does not provide a name, the system uses the value from <c>defaultName</c>.
        /// This method streamlines asset creation by combining instantiation and naming in one step.
        /// </remarks>
        public static void PromptInProjectBrowserToCreateNewAsset<T>(string defaultName = "New Graph") where T : Graph, new()
        {
            PublicGraphFactory.PromptInProjectBrowserToCreateNewAsset<T>(defaultName);
        }

        /// <summary>
        /// Creates a new graph asset of type <typeparamref name="T"/> at the specified file path.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="Graph"/> to create. Must inherit from <see cref="Graph"/> and have a public parameterless constructor.
        /// </typeparam>
        /// <param name="assetPath">The relative path for the new asset (e.g., "Assets/Graphs/MyGraph.mygraph").</param>
        /// <returns>The created graph instance.</returns>
        /// <remarks>
        /// Use this method to programmatically create a new graph asset of type <typeparamref name="T"/> at a specific location in the project.
        /// The path must be relative to the Unity project folder and must include a valid file extension recognized by the graph importer.
        /// If an asset already exists at the specified <paramref name="assetPath"/>, this method overwrites it.
        /// This method works similarly to <see cref="UnityEditor.AssetDatabase.CreateAsset"/> but is scoped for <see cref="Graph"/> assets.
        /// </remarks>
        public static T CreateGraph<T>(string assetPath) where T : Graph, new()
        {
            CheckFilePathAndGraphType<T>(assetPath);

            var graphObject = ScriptableObject.CreateInstance<GraphObjectImp>();
            graphObject.GraphType = typeof(T);
            graphObject.CreateMainGraph(typeof(GraphModelImp));
            graphObject.AttachToAssetFile(assetPath, true);
            graphObject.DestroyObjects();

            return LoadGraph<T>(assetPath);
        }

        static void CheckFilePathAndGraphType<T>(string assetPath) where T : Graph
        {
            var fileExtension = Path.GetExtension(assetPath);

            if (string.IsNullOrEmpty(fileExtension) || fileExtension.Length < 2)
            {
                throw new ArgumentException(
                    $"The assetPath {assetPath} is missing an extension. Add the extension to the assetPath");
            }

            var typeByExtension = PublicGraphFactory.GetGraphTypeByExtension(fileExtension);
            if (typeByExtension == null)
            {
                throw new ArgumentException(
                    $"assetPath {assetPath} has an unknown extension. You need to register the extension with a GraphAttribute");
            }

            if (!typeof(T).IsAssignableFrom(typeByExtension))
            {
                throw new ArgumentException(
                    $"assetPath {assetPath} extension does not match type {typeof(T).FullName}. Make sure the extension is registered to the graph type {typeByExtension.FullName}");
            }
        }

        /// <summary>
        /// Loads a <see cref="Graph"/> of type <typeparamref name="T"/> from the asset at the specified path.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Graph"/> to load.</typeparam>
        /// <param name="assetPath">The relative path to the graph asset (for example, "Assets/Graphs/MyGraph.mygraph").</param>
        /// <returns>The loaded graph instance, or <c>null</c> if no matching graph is found.</returns>
        /// <remarks>
        /// Use this method to load a graph asset of type <typeparamref name="T"/> from a given asset path.
        /// The <paramref name="assetPath"/> must be relative to the Unity project folder.
        /// This method returns the graph object currently loaded in memory, which might differ from the version on disk if the asset was modified
        /// or opened in an editor. This behavior is similar to <see cref="UnityEditor.AssetDatabase.LoadAssetAtPath"/>.
        /// For deterministic loading during import, use <see cref="LoadGraphForImporter{T}"/> instead.
        /// </remarks>
        public static T LoadGraph<T>(string assetPath) where T : Graph
        {
            CheckFilePathAndGraphType<T>(assetPath);

            var graphObject = GraphObject.LoadGraphObjectAtPath<GraphObjectImp>(assetPath);

            return (graphObject?.GraphModel as GraphModelImp)?.Graph as T;
        }

        /// <summary>
        /// Saves the asset of the specified <see cref="Graph"/> to disk if it has unsaved changes.
        /// </summary>
        /// <param name="graph">The graph to save.</param>
        /// <remarks>
        /// Use this method to persist any pending modifications made to a <see cref="Graph"/> instance.
        /// It prevents data loss by ensuring the asset on disk reflects the in-memory graph state.
        /// This method is similar to <see cref="UnityEditor.AssetDatabase.SaveAssetIfDirty(UnityEngine.Object)"/> and only performs a save if the graph is marked dirty.
        /// </remarks>
        public static void SaveGraphIfDirty(Graph graph)
        {
            graph.CheckImplementation();
            graph?.m_Implementation.GraphObject?.Save();
        }

        /// <summary>
        /// Loads a fresh instance of the <see cref="Graph"/> of type <typeparamref name="T"/> from disk for use in the asset import pipeline.
        /// </summary>
        /// <param name="assetPath">The path to the graph asset file.</param>
        /// <typeparam name="T">The type of graph to load.</typeparam>
        /// <returns>A new instance of the graph read directly from disk.</returns>
        /// <remarks>
        /// Use this method to load a <see cref="Graph"/> instance from disk without referencing the in-memory version. This ensures consistent, deterministic
        /// behavior required by Unity’s asset import pipeline. This method is intended for importers, it bypasses
        /// any in-memory modifications that may have occurred. Unlike <see cref="LoadGraph{T}"/>, this method always returns a clean copy of the graph as it exists on disk.
        /// </remarks>
        public static T LoadGraphForImporter<T>(string assetPath) where T : Graph
        {
            CheckFilePathAndGraphType<T>(assetPath);
            var graphObject = GraphObject.LoadGraphObjectCopyAtPathAndForget(assetPath, typeof(GraphObjectImp)) as GraphObjectImp;

            return (graphObject?.GraphModel as GraphModelImp)?.Graph as T;
        }

        /// <summary>
        /// Retrieves the globally unique identifier (GUID) for the asset associated with the specified <see cref="Graph"/>.
        /// </summary>
        /// <param name="graph">The graph whose asset GUID you want to retrieve.</param>
        /// <returns>The <see cref="GUID"/> of the graph asset.</returns>
        /// <remarks>
        /// Use this method to get a persistent identifier for a graph asset. The <see cref="GUID"/> allows reliable tracking, referencing,
        /// and linking to graph assets across different Unity sessions.
        /// </remarks>
        public static GUID GetGraphAssetGUID(Graph graph)
        {
            graph.CheckImplementation();
            return graph.m_Implementation.GraphObject?.AssetFileGuid ?? default;
        }

        /// <summary>
        /// Retrieves the file path of the asset associated with the specified <see cref="Graph"/>.
        /// </summary>
        /// <param name="graph">The graph whose asset path you want to retrieve.</param>
        /// <returns>The asset's file path.</returns>
        /// <remarks>
        /// Use this method to get the relative file path of the graph asset within the Unity project (for example,
        /// <c>"Assets/Graphs/MyGraph.mygraph"</c>). Do not use <c>AssetDatabase.GetAssetPath</c> with graph objects, as it will not return the correct result.
        /// </remarks>
        public static string GetGraphAssetPath(Graph graph)
        {
            graph.CheckImplementation();
            return graph.m_Implementation.GraphObject?.FilePath ?? string.Empty;
        }
    }
}
