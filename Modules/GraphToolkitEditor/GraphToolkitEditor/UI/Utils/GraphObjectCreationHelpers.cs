// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    class DoCreateAsset : AssetCreationEndAction
    {
        GraphObject m_Object;
        GraphTemplate m_Template;
        Action<GraphObject> m_Callback;

        public void SetUp(GraphObject graphObject, GraphTemplate template, Action<GraphObject> callback = null)
        {
            m_Template = template;
            m_Object = graphObject;
            m_Callback = callback;
        }

        // used in tests
        public void CreateAsset(string pathName)
        {
            m_Object.CreateMainGraph(m_Template.GraphModelType);
            m_Template?.InitBasicGraph(m_Object.GraphModel);

            if (pathName != null)
            {
                var filePath = m_Object.AttachToAssetFile(pathName, false);

                m_Object = GraphObject.LoadGraphObjectAtPath(filePath, m_Object.GetType());
                if (m_Object == null)
                    throw new Exception($"Failed to create graph object at path {filePath}");
            }
        }

        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            CreateAsset(pathName);
            m_Callback?.Invoke(m_Object);
        }

        public override void Cancelled(EntityId entityId, string pathName, string resourceFile)
        {
            Selection.activeObject = null;
        }

    }

    /// <summary>
    /// Helper methods to create graph assets.
    /// </summary>
    [UnityRestricted]
    internal static class GraphObjectCreationHelpers
    {
        /// <summary>
        /// Creates a new graph object and starts the process of renaming it in the Project window.
        /// </summary>
        /// <param name="template">The <see cref="GraphTemplate"/> to use.</param>
        /// <param name="path">The path at which to create the graph.</param>
        /// <param name="endActionCallback">Action to call after asset renaming is finished. Typically used to open the new graph in an EditorWindow.</param>
        /// <typeparam name="TGraphObject">The graph object type to create.</typeparam>
        public static void CreateInProjectWindow<TGraphObject>(GraphTemplate template, string path, Action<GraphObject> endActionCallback = null)
            where TGraphObject : GraphObject
        {
            var graphObject = ScriptableObject.CreateInstance<TGraphObject>();

            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(graphObject, template, endActionCallback);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                graphObject.GetEntityId(),
                endAction,
                $"{path}/{template.NewAssetName}.{template.GraphFileExtension}",
                AssetPreview.GetMiniThumbnail(graphObject),
                null);
        }

        /// <summary>
        /// Creates a graph object in an asset.
        /// </summary>
        /// <param name="graphObjectType">The graph object type.</param>
        /// <param name="graphModelType">The type of the graph model to create.</param>
        /// <param name="name">The name of the graph.</param>
        /// <param name="assetPath">The asset path of the graph. If there is already a file at this path, it will be overwritten. If null, the graph will not be saved to a file.</param>
        /// <param name="graphTemplate">The template of the graph.</param>
        /// <returns>The created graph object.</returns>
        public static GraphObject CreateGraphObject(Type graphObjectType, Type graphModelType, string name, string assetPath,
            GraphTemplate graphTemplate = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphObjectType) ||
                !typeof(GraphObject).IsAssignableFrom(graphObjectType))
                return null;

            var graphObject = ScriptableObject.CreateInstance(graphObjectType) as GraphObject;
            if (graphObject == null)
                return graphObject;

            graphObject.CreateMainGraph(graphModelType);
            graphObject.name = name;
            graphTemplate?.InitBasicGraph(graphObject.GraphModel);

            if (assetPath != null)
            {
                assetPath = graphObject.AttachToAssetFile(assetPath, true);
                graphObject.UnloadObject();
                graphObject = GraphObject.LoadGraphObjectAtPath(assetPath, graphObjectType);
                if (graphObject == null)
                    throw new Exception($"Failed to create graph object at path {assetPath}");
            }

            graphObject.Save();

            return graphObject;
        }

        /// <summary>
        /// Creates a graph object using a prompt box.
        /// </summary>
        /// <param name="graphObjectType">The graph object type.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="title">The title of the window to display.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <param name="validatePathCallback">Function to validate the path</param>
        /// <returns>The created graph object.</returns>
        public static GraphObject PromptToCreateGraphObject(Type graphObjectType, GraphTemplate template, string title = null, string prompt = null, Func<string, bool> validatePathCallback = null)
        {
            const string promptToCreateTitle = "Create {0}";
            const string promptToCreate = "Create a new {0}";

            title ??= string.Format(promptToCreateTitle, template.GraphTypeName);
            prompt ??= string.Format(promptToCreate, template.GraphTypeName);

            return PromptToCreateGraphObject(graphObjectType, template.GraphModelType, title, template.NewAssetName, template.GraphFileExtension, prompt, validatePathCallback, template);
        }

        /// <summary>
        /// Creates a graph object using a prompt box.
        /// </summary>
        /// <param name="graphObjectType">The graph object type.</param>
        /// <param name="graphModelType">The graph model type.</param>
        /// <param name="title">The title of the window to display.</param>
        /// <param name="defaultAssetName">The placeholder text to display in the "Save As" text field. This is the name of file to be saved.</param>
        /// <param name="fileExtension">The file extension to use in the saved file path.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <param name="validatePathCallback">Function to validate the path</param>
        /// <param name="graphTemplate">Template for the created <see cref="GraphObject"/></param>
        /// <returns>The created graph object.</returns>
        public static GraphObject PromptToCreateGraphObject(Type graphObjectType, Type graphModelType, string title, string defaultAssetName, string fileExtension, string prompt, Func<string, bool> validatePathCallback = null, GraphTemplate graphTemplate = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphObjectType) ||
                !typeof(GraphObject).IsAssignableFrom(graphObjectType))
                return null;

            var path = EditorUtility.SaveFilePanelInProject(title, defaultAssetName, fileExtension, prompt);
            if (path.Length != 0 && (validatePathCallback == null || validatePathCallback(path)))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var graphObject = CreateGraphObject(graphObjectType, graphModelType, fileName, path, graphTemplate);

                return graphObject;
            }

            return null;
        }

        /// <summary>
        /// Converts an graph object to a local subgraph.
        /// </summary>
        /// <param name="sourceObjectGraph">The graph object to convert.</param>
        /// <param name="parentGraphModel">The <see cref="GraphModel"/> to parent the new local subgraph to.</param>
        /// <param name="template">Template for the created <see cref="GraphObject"/></param>
        /// <returns>The created graph object.</returns>
        public static GraphModel ConvertAssetToLocalGraph(GraphObject sourceObjectGraph, GraphModel parentGraphModel, GraphTemplate template = null)
        {
            if (sourceObjectGraph is null || sourceObjectGraph.GraphModel.IsLocalSubgraph || parentGraphModel is null)
                return null;

            var newLocalGraph = parentGraphModel.CreateLocalSubgraph(
                template?.GraphModelType ?? sourceObjectGraph.GraphModel.GetType(),
                sourceObjectGraph.name,
                template);

            newLocalGraph.CloneGraph(sourceObjectGraph.GraphModel, true);

            return newLocalGraph;
        }

        /// <summary>
        /// Converts a local subgraph to an graph object and stores it in an asset.
        /// </summary>
        /// <param name="originalLocalGraph">Original graph to convert</param>
        /// <param name="title">The title in the prompt box.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <param name="path">The path of the newly created asset.</param>
        /// <param name="validatePathCallback">A callback to validate the path.</param>
        /// <param name="template">Template for the created <see cref="GraphObject"/></param>
        /// <returns>The created graph object.</returns>
        public static GraphObject ConvertLocalToAssetGraph(GraphModel originalLocalGraph, string title, string prompt, string path, GraphTemplate template = null, Func<string, bool> validatePathCallback = null)
        {
            var graphAssetType = originalLocalGraph.GetPreferredSubGraphObjectType();
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetType) ||
                !typeof(GraphObject).IsAssignableFrom(graphAssetType))
                return null;

            if (string.IsNullOrEmpty(path))
            {
                var extension = template?.GraphFileExtension ?? Path.GetExtension(originalLocalGraph.GraphObject.FilePath).TrimStart('.');
                path = EditorUtility.SaveFilePanelInProject(title, originalLocalGraph.Name, extension, prompt);
            }

            if (path.Length != 0 && (validatePathCallback == null || validatePathCallback(path)))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var newAssetGraph = CreateGraphObject(
                    graphAssetType,
                    template?.GraphModelType ?? originalLocalGraph.GetType(),
                    fileName,
                    path,
                    template);

                newAssetGraph.GraphModel.CloneGraph(originalLocalGraph, true);
                // At this point, newAssetGraph.LocalSubgraphs is populated with all the local subgraphs that were nested in the originalLocalGraph.

                return newAssetGraph;
            }

            return null;
        }
    }
}
