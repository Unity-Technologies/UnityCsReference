using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal interface IBuilderPerFileAssetPostprocessor
    {
        void OnPostProcessAsset(string assetPath);
    }

    internal interface IBuilderOneTimeAssetPostprocessor
    {
        void OnPostProcessAsset();
    }

    internal class BuilderAssetPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<IBuilderPerFileAssetPostprocessor> m_PerFileProcessors =
            new HashSet<IBuilderPerFileAssetPostprocessor>();

        private static readonly HashSet<IBuilderOneTimeAssetPostprocessor> m_OneTimeProcessors =
            new HashSet<IBuilderOneTimeAssetPostprocessor>();

        public static void Register(IBuilderPerFileAssetPostprocessor processor)
        {
            m_PerFileProcessors.Add(processor);
        }

        public static void Register(IBuilderOneTimeAssetPostprocessor processor)
        {
            m_OneTimeProcessors.Add(processor);
        }

        public static void Unregister(IBuilderPerFileAssetPostprocessor processor)
        {
            m_PerFileProcessors.Remove(processor);
        }

        public static void Unregister(IBuilderOneTimeAssetPostprocessor processor)
        {
            m_OneTimeProcessors.Remove(processor);
        }

        static bool IsBuilderFile(string assetPath)
        {
            if (assetPath.EndsWith(".uxml") || assetPath.EndsWith(".uss"))
                return true;

            return false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var processor in m_OneTimeProcessors)
                processor.OnPostProcessAsset();

            foreach (string assetPath in importedAssets)
            {
                if (!IsBuilderFile(assetPath))
                    continue;

                foreach (var processor in m_PerFileProcessors)
                    processor.OnPostProcessAsset(assetPath);
            }
        }
    }
}
