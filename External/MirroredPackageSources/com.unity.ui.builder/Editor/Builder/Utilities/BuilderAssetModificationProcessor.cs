using UnityEditor;
using System.Collections.Generic;

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
            // On a duplication, this function is called with ZERO
            // paths. Because, of course.
            foreach (var modificationProcessor in m_ModificationProcessors)
                modificationProcessor.OnAssetChange();

            return paths;
        }
    }
}
