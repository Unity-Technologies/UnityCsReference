using System.Collections.Generic;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal interface IBuilderPerFileAssetPostprocessor
    {
        void OnPostProcessAsset(string assetPath);
    }

    internal interface IBuilderAssetPostprocessor
    {
        void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths);
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

        private static readonly HashSet<IBuilderAssetPostprocessor> m_Processors =
            new HashSet<IBuilderAssetPostprocessor>();

        public static void Register(IBuilderPerFileAssetPostprocessor processor)
        {
            m_PerFileProcessors.Add(processor);
        }

        public static void Register(IBuilderAssetPostprocessor processor)
        {
            m_Processors.Add(processor);
        }

        public static void Register(IBuilderOneTimeAssetPostprocessor processor)
        {
            m_OneTimeProcessors.Add(processor);
        }

        public static void Unregister(IBuilderPerFileAssetPostprocessor processor)
        {
            m_PerFileProcessors.Remove(processor);
        }

        public static void Unregister(IBuilderAssetPostprocessor processor)
        {
            m_Processors.Remove(processor);
        }

        public static void Unregister(IBuilderOneTimeAssetPostprocessor processor)
        {
            m_OneTimeProcessors.Remove(processor);
        }

        static bool IsBuilderFile(string assetPath)
        {
            if (assetPath.EndsWith(BuilderConstants.UxmlExtension)
                || assetPath.EndsWith(BuilderConstants.UssExtension)
                || assetPath.EndsWith(BuilderConstants.TssExtension))
                return true;

            return false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var processor in m_OneTimeProcessors)
                processor.OnPostProcessAsset();

            foreach (var processor in m_Processors)
                processor.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

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
