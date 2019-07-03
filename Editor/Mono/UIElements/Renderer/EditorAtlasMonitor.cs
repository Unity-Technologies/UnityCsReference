// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal static class EditorAtlasMonitor
    {
        static EditorAtlasMonitor()
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

        private class TexturePostProcessor : UnityEditor.AssetPostprocessor
        {
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
