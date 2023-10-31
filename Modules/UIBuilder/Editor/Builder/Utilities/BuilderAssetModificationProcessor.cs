// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal interface IBuilderAssetModificationProcessor
    {
        void OnAssetChange();
        AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath);
        AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option);
    }

    internal class BuilderAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static readonly HashSet<IBuilderAssetModificationProcessor> m_ModificationProcessors = new HashSet<IBuilderAssetModificationProcessor>();

        public static void Register(IBuilderAssetModificationProcessor modificationProcessor)
        {
            m_ModificationProcessors.Add(modificationProcessor);
        }

        public static void Unregister(IBuilderAssetModificationProcessor modificationProcessor)
        {
            m_ModificationProcessors.Remove(modificationProcessor);
        }

        public static bool IsProcessorRegistered(IBuilderAssetModificationProcessor modificationProcessor)
        {
            return m_ModificationProcessors.Contains(modificationProcessor);
        }

        static bool IsUxml(string assetPath)
        {
            if (assetPath.EndsWith("uxml") || assetPath.EndsWith("uxml.meta"))
                return true;

            return false;
        }

        static void OnWillCreateAsset(string assetPath)
        {
            if (!IsUxml(assetPath))
                return;

            foreach (var modificationProcessor in m_ModificationProcessors)
                modificationProcessor.OnAssetChange();
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (!IsUxml(assetPath))
                return AssetDeleteResult.DidNotDelete;

            foreach (var modificationProcessor in m_ModificationProcessors)
                modificationProcessor.OnAssetChange();

            foreach (var modificationProcessor in m_ModificationProcessors)
            {
                var result = modificationProcessor.OnWillDeleteAsset(assetPath, option);
                if (result != AssetDeleteResult.DidNotDelete)
                    return result;
            }

            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (!IsUxml(sourcePath))
                return AssetMoveResult.DidNotMove;

            foreach (var modificationProcessor in m_ModificationProcessors)
                modificationProcessor.OnAssetChange();

            foreach (var modificationProcessor in m_ModificationProcessors)
            {
                var result = modificationProcessor.OnWillMoveAsset(sourcePath, destinationPath);
                if (result != AssetMoveResult.DidNotMove)
                    return result;
            }

            return AssetMoveResult.DidNotMove;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            var usesUxml = false;
            foreach (var path in paths)
            {
                if (!IsUxml(path))
                    continue;
                usesUxml = true;
                break;
            }

            if (!usesUxml)
                return paths;

            foreach (var modificationProcessor in m_ModificationProcessors)
                modificationProcessor.OnAssetChange();

            return paths;
        }
    }
}
