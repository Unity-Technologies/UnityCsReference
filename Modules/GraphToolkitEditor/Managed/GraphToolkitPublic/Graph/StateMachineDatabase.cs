// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Provides functionality needed to access, and perform operations on, state machine assets.
    /// </summary>
    /// <remarks>
    /// The <c>StateMachineDatabase</c> class is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase"/>.
    /// Use this class to create, load, and save <see cref="StateMachine"/> instances and their associated assets.
    /// <br/>
    /// <br/>
    /// Use <see cref="PromptInProjectBrowserToCreateNewAsset{T}"/> to create and name a new asset,
    /// <see cref="CreateStateMachine{T}"/> to generate an asset file, and <see cref="LoadStateMachine{T}"/> to retrieve an existing one.
    /// <br/>
    /// <br/>
    /// Use <see cref="SaveStateMachine"/> to persist state machine data changes, and <see cref="LoadStateMachineForImporter{T}"/> to load a clean instance during import.
    /// </remarks>
    public static class StateMachineDatabase
    {
        /// <summary>
        /// Creates a new state machine asset and activates the naming field in the Project Browser.
        /// </summary>
        /// <typeparam name="T">
        /// The type of state machine to create. Must inherit from <see cref="StateMachine"/> and have a public parameterless constructor.
        /// </typeparam>
        /// <param name="defaultName">The default name for the new asset if the user does not rename it. Defaults to "New State Machine" if not specified.</param>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.PromptInProjectBrowserToCreateNewAsset{T}"/>.
        /// </remarks>
        public static void PromptInProjectBrowserToCreateNewAsset<T>(string defaultName = "New State Machine") where T : StateMachine, new()
        {
            PublicGraphFactory.PromptInProjectBrowserToCreateNewAsset(defaultName, typeof(T));
        }

        /// <summary>
        /// Creates a new state machine asset of type <typeparamref name="T"/> at the specified file path.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the <see cref="StateMachine"/> to create. Must inherit from <see cref="StateMachine"/> and have a public parameterless constructor.
        /// </typeparam>
        /// <param name="assetPath">The relative path for the new asset (e.g., "Assets/Graphs/MyStateMachine.mystatemachine").</param>
        /// <returns>The created state machine instance.</returns>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.CreateGraph{T}"/>.
        /// </remarks>
        public static T CreateStateMachine<T>(string assetPath) where T : StateMachine, new()
        {
            GraphDatabase.CheckFilePathAndType(assetPath, typeof(T));

            var graphObject = ScriptableObject.CreateInstance<GraphObjectImp>();
            try
            {
                graphObject.GraphType = typeof(T);
                graphObject.CreateMainGraph(typeof(StateMachineImp));
            }
            catch // CreateMainGraph calls OnEnable on the graph model, which can throw if users attempt to modify the graph in OnEnable.
            {
                if (graphObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(graphObject);
                }

                throw;
            }

            assetPath = graphObject.AttachToAssetFile(assetPath, true);
            graphObject.DestroyObjects();

            if (!File.Exists(graphObject.FilePath))
            {
                UnityEngine.Object.DestroyImmediate(graphObject);
                return null;
            }

            return LoadStateMachine<T>(assetPath);
        }

        /// <summary>
        /// Loads a <see cref="StateMachine"/> of type <typeparamref name="T"/> from the asset at the specified path.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="StateMachine"/> to load.</typeparam>
        /// <param name="assetPath">The relative path to the state machine asset.</param>
        /// <returns>The loaded state machine instance, or <c>null</c> if no matching state machine is found.</returns>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.LoadGraph{T}"/>.
        /// </remarks>
        public static T LoadStateMachine<T>(string assetPath) where T : StateMachine
        {
            GraphDatabase.CheckFilePathAndType(assetPath, typeof(T));

            var graphObject = GraphObject.LoadGraphObjectAtPath<GraphObjectImp>(assetPath);

            return (graphObject?.GraphModel as GraphModelImp)?.Graph as T;
        }

        /// <summary>
        /// Loads a fresh instance of the <see cref="StateMachine"/> of type <typeparamref name="T"/> from disk for use in the asset import pipeline.
        /// </summary>
        /// <param name="assetPath">The path to the state machine asset file.</param>
        /// <typeparam name="T">The type of state machine to load.</typeparam>
        /// <returns>A new instance of the state machine read directly from disk.</returns>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.LoadGraphForImporter{T}"/>.
        /// </remarks>
        public static T LoadStateMachineForImporter<T>(string assetPath) where T : StateMachine
        {
            GraphDatabase.CheckFilePathAndType(assetPath, typeof(T));
            var graphObject = GraphObject.LoadGraphObjectCopyAtPathAndForget(assetPath, typeof(GraphObjectImp)) as GraphObjectImp;

            return (graphObject?.GraphModel as GraphModelImp)?.Graph as T;
        }

        /// <summary>
        /// Saves the asset of the specified <see cref="StateMachine"/> to disk if it has unsaved changes.
        /// </summary>
        /// <param name="stateMachine">The state machine to save.</param>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.SaveGraph(Graph)"/>.
        /// </remarks>
        public static void SaveStateMachine(StateMachine stateMachine)
        {
            stateMachine.CheckImplementation();
            stateMachine.m_Implementation.GraphObject?.Save();
        }

        /// <summary>
        /// Retrieves the globally unique identifier (GUID) for the asset associated with the specified <see cref="StateMachine"/>.
        /// </summary>
        /// <param name="stateMachine">The state machine whose asset GUID you want to retrieve.</param>
        /// <returns>The <see cref="GUID"/> of the state machine asset.</returns>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.GetGraphAssetGUID(Graph)"/>.
        /// </remarks>
        public static GUID GetStateMachineAssetGUID(StateMachine stateMachine)
        {
            stateMachine.CheckImplementation();
            return stateMachine.m_Implementation.GraphObject?.AssetFileGuid ?? default;
        }

        /// <summary>
        /// Retrieves the file path of the asset associated with the specified <see cref="StateMachine"/>.
        /// </summary>
        /// <param name="stateMachine">The state machine whose asset path you want to retrieve.</param>
        /// <returns>The asset's file path.</returns>
        /// <remarks>
        /// This is the <see cref="StateMachine"/> counterpart of <see cref="GraphDatabase.GetGraphAssetPath(Graph)"/>.
        /// </remarks>
        public static string GetStateMachineAssetPath(StateMachine stateMachine)
        {
            stateMachine.CheckImplementation();
            return stateMachine.m_Implementation.GraphObject?.FilePath ?? string.Empty;
        }
    }
}
