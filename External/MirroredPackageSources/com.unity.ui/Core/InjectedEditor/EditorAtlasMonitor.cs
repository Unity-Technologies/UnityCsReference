using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

// The TODOs in this file will be implemented once com.unity.ui has the ability to define editor content.

namespace UnityEditor.UIElements
{
    // TODO: Remove this.
    internal static class EditorAtlasMonitorBridge
    {
        public static void StaticInit()
        {
            EditorAtlasMonitor.StaticInit();
        }

        public static Action<string[], string[], string[], string[]>  OnPostprocessAllAssets;
        public static Action<Texture2D> OnPostprocessTexture;
    }

    // TODO: Add [InitializeOnLoad].
    static class EditorAtlasMonitor
    {
        // TODO: Remove this.
        static TexturePostProcessor s_TexturePostProcessor = new TexturePostProcessor();

        // TODO: Turn the method into a static constructor.
        public static void StaticInit()
        {
            RenderChain.OnPreRender += OnPreRender;
        }

        public static void OnPreRender()
        {
            bool colorSpaceChanged = CheckForColorSpaceChange();
            bool importedTextureChanged = CheckForImportedTextures();
            bool importedVectorImageChanged = CheckForImportedVectorImages();
            if (colorSpaceChanged || importedTextureChanged)
            {
                UIRAtlasManager.MarkAllForReset();
                VectorImageManager.MarkAllForReset();
            }
            else if (colorSpaceChanged || importedVectorImageChanged)
                VectorImageManager.MarkAllForReset();
        }

        // TODO: Derive from UnityEditor.AssetPostprocessor
        private class TexturePostProcessor
        {
            // TODO: Remove this constructor.
            public TexturePostProcessor()
            {
                EditorAtlasMonitorBridge.OnPostprocessTexture = OnPostprocessTexture;
                EditorAtlasMonitorBridge.OnPostprocessAllAssets = OnPostprocessAllAssets;
            }

            public void OnPostprocessTexture(Texture2D texture)
            {
                ++importedTexturesCount;
            }

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (var assetPath in importedAssets)
                    if (System.IO.Path.GetExtension(assetPath) == ".svg")
                        ++importedVectorImagesCount;
            }

            public static int importedTexturesCount;
            public static int importedVectorImagesCount;
        }

        private static ColorSpace m_LastColorSpace;
        private static int m_LastImportedTexturesCount;
        private static int m_LastImportedVectorImagesCount;

        private static bool CheckForColorSpaceChange()
        {
            ColorSpace activeColorSpace = QualitySettings.activeColorSpace;
            if (m_LastColorSpace == activeColorSpace)
                return false;

            m_LastColorSpace = activeColorSpace;
            return true;
        }

        private static bool CheckForImportedTextures()
        {
            int importedTexturesCount = TexturePostProcessor.importedTexturesCount;
            if (m_LastImportedTexturesCount == importedTexturesCount)
                return false;

            m_LastImportedTexturesCount = importedTexturesCount;

            return true;
        }

        private static bool CheckForImportedVectorImages()
        {
            int importedVectorImagesCount = TexturePostProcessor.importedVectorImagesCount;
            if (m_LastImportedVectorImagesCount == importedVectorImagesCount)
                return false;

            m_LastImportedVectorImagesCount = importedVectorImagesCount;

            return true;
        }
    }
}
