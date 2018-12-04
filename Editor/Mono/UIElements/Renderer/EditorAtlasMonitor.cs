// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal class EditorAtlasMonitor : IAtlasMonitor
    {
        static EditorAtlasMonitor()
        {
            s_Monitors = new Dictionary<UIRAtlasManager, EditorAtlasMonitor>();
            UIRAtlasManager.atlasManagerCreated += OnAtlasManagerCreated;
            UIRAtlasManager.atlasManagerDisposed += OnAtlasManagerDisposed;
        }

        private static Dictionary<UIRAtlasManager, EditorAtlasMonitor> s_Monitors;

        private static void OnAtlasManagerCreated(UIRAtlasManager atlasManager)
        {
            Assert.IsFalse(s_Monitors.ContainsKey(atlasManager));
            s_Monitors.Add(atlasManager, new EditorAtlasMonitor(atlasManager));
        }

        private static void OnAtlasManagerDisposed(UIRAtlasManager atlasManager)
        {
            bool removedMonitor = s_Monitors.Remove(atlasManager);
            Assert.IsTrue(removedMonitor);
        }

        public EditorAtlasMonitor(UIRAtlasManager atlasManager)
        {
            atlasManager.AddMonitor(this);
        }

        private class TexturePostProcessor : UnityEditor.AssetPostprocessor
        {
            public void OnPostprocessTexture(Texture2D texture)
            {
                ++importedTexturesCount;
            }

            public static int importedTexturesCount;
        }

        private ColorSpace m_LastColorSpace;
        private int m_LastImportedTexturesCount;

        public bool RequiresReset()
        {
            bool colorSpaceChanged = CheckForColorSpaceChange();
            bool importedTextures = CheckForImportedTextures();

            return colorSpaceChanged || importedTextures;
        }

        private bool CheckForColorSpaceChange()
        {
            ColorSpace activeColorSpace = QualitySettings.activeColorSpace;
            if (m_LastColorSpace == activeColorSpace)
                return false;

            m_LastColorSpace = activeColorSpace;
            return true;
        }

        private bool CheckForImportedTextures()
        {
            int importedTexturesCount = TexturePostProcessor.importedTexturesCount;
            if (m_LastImportedTexturesCount == importedTexturesCount)
                return false;

            m_LastImportedTexturesCount = importedTexturesCount;

            return true;
        }
    }
}
