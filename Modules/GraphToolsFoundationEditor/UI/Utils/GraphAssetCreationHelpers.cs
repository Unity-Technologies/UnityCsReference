// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class DoCreateAsset_Internal : EndNameEditAction
    {
        ICommandTarget m_CommandTarget;
        GraphAsset m_Asset;
        GraphTemplate m_Template;
        Action m_Callback;

        public void SetUp(ICommandTarget target, GraphAsset asset, GraphTemplate template, Action callback = null)
        {
            m_CommandTarget = target;
            m_Template = template;
            m_Asset = asset;
            m_Callback = callback;
        }

        internal void CreateAndLoadAsset_Internal(string pathName)
        {
            m_Asset.CreateGraph(m_Template.StencilType);

            m_Asset.CreateFile(pathName, false);
            m_Template?.InitBasicGraph(m_Asset.GraphModel);
            m_Asset.Save();
            m_Asset = m_Asset.Import();

            m_CommandTarget?.Dispatch(new LoadGraphCommand(m_Asset.GraphModel));
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            CreateAndLoadAsset_Internal(pathName);
            m_Callback?.Invoke();
        }

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
        {
            Selection.activeObject = null;
        }
    }

    /// <summary>
    /// Helper methods to create graph assets.
    /// </summary>
    static class GraphAssetCreationHelpers
    {
        public static void CreateInProjectWindow<TGraphAssetType>(GraphTemplate template, ICommandTarget target, string path, Action endActionCallback = null)
            where TGraphAssetType : GraphAsset
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetType>();

            var endAction = ScriptableObject.CreateInstance<DoCreateAsset_Internal>();
            endAction.SetUp(target, asset, template, endActionCallback);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                endAction,
                $"{path}/{template.DefaultAssetName}.{template.GraphFileExtension}",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }

        /// <summary>
        /// Creates a graph asset.
        /// </summary>
        /// <param name="graphAssetType">The graph asset type.</param>
        /// <param name="stencilType">The type of the stencil.</param>
        /// <param name="name">The name of the graph.</param>
        /// <param name="assetPath">The asset path of the graph. If there is already a file at this path, it will be overwritten.</param>
        /// <param name="graphTemplate">The template of the graph.</param>
        /// <returns>The created graph asset.</returns>
        public static GraphAsset CreateGraphAsset(Type graphAssetType, Type stencilType, string name, string assetPath,
            GraphTemplate graphTemplate = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetType) ||
                !typeof(GraphAsset).IsAssignableFrom(graphAssetType))
                return null;

            var graphAsset = ScriptableObject.CreateInstance(graphAssetType) as GraphAsset;

            if (graphAsset != null)
            {
                graphAsset.Name = name;
                graphAsset.CreateGraph(stencilType);

                graphAsset.CreateFile(assetPath, true);
                graphTemplate?.InitBasicGraph(graphAsset.GraphModel);
                graphAsset.Save();
                graphAsset = graphAsset.Import();
            }

            return graphAsset;
        }

        /// <summary>
        /// Creates a graph asset using a prompt box.
        /// <param name="graphAssetType">The graph asset type.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="title">The title of the file. It will be part of the asset path.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <returns>The created graph asset.</returns>
        /// </summary>
        public static GraphAsset PromptToCreateGraphAsset(Type graphAssetType, GraphTemplate template, string title, string prompt, Func<string, bool> validatePathCallback = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetType) ||
                !typeof(GraphAsset).IsAssignableFrom(graphAssetType))
                return null;

            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, template.GraphFileExtension, prompt);

            if (path.Length != 0 && (validatePathCallback == null || validatePathCallback(path)))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var asset = CreateGraphAsset(graphAssetType, template.StencilType, fileName, path, template);
                return asset;
            }

            return null;
        }
    }
}
