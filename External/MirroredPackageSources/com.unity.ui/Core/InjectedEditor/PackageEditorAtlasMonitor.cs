using System;
using UnityEngine;
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
            PackageEditorAtlasMonitor.StaticInit();
        }

        public static Action<string[], string[], string[], string[]>  OnPostprocessAllAssets;
        public static Action<Texture2D> OnPostprocessTexture;
    }

    // TODO: Add [InitializeOnLoad].
    static class PackageEditorAtlasMonitor
    {
        // TODO: Remove this.
        static TexturePostProcessor s_TexturePostProcessor = new TexturePostProcessor();

        // TODO: Turn the method into a static constructor.
        public static void StaticInit()
        {
            Panel.beforeAnyRepaint += OnBeforeRepaint;
        }

        static void OnBeforeRepaint(Panel source)
        {
            bool colorSpaceChanged = CheckForColorSpaceChange();
            bool importedTextureChanged = CheckForImportedTextures();
            bool importedVectorImageChanged = CheckForImportedVectorImages();
            bool renderTexturesTrashed = CheckForRenderTexturesTrashed();

            bool resetAtlases = colorSpaceChanged || importedTextureChanged || importedVectorImageChanged || renderTexturesTrashed;
            bool resetRenderChains = renderTexturesTrashed;

            if (resetAtlases || resetRenderChains)
            {
                if (resetAtlases && !resetRenderChains)
                {
                    // If the render chain was to be reset, it would implicitly reset the VectorImageManagers.
                    for (int i = 0; i < VectorImageManager.instances.Count; ++i)
                        VectorImageManager.instances[i].Reset();
                }

                var it = UIElementsUtility.GetPanelsIterator();
                while (it.MoveNext())
                {
                    Panel panel = it.Current.Value;
                    if (resetRenderChains)
                        (panel.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater)?.DestroyRenderChain();

                    if (resetAtlases)
                        panel.atlas?.Reset();
                }
            }
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

        private static ColorSpace s_LastColorSpace;
        private static int s_LastImportedTexturesCount;
        private static int s_LastImportedVectorImagesCount;
        private static RenderTexture s_RenderTexture;

        private static bool CheckForColorSpaceChange()
        {
            ColorSpace activeColorSpace = QualitySettings.activeColorSpace;
            if (s_LastColorSpace == activeColorSpace)
                return false;

            s_LastColorSpace = activeColorSpace;
            return true;
        }

        private static bool CheckForImportedTextures()
        {
            int importedTexturesCount = TexturePostProcessor.importedTexturesCount;
            if (s_LastImportedTexturesCount == importedTexturesCount)
                return false;

            s_LastImportedTexturesCount = importedTexturesCount;

            return true;
        }

        private static bool CheckForImportedVectorImages()
        {
            int importedVectorImagesCount = TexturePostProcessor.importedVectorImagesCount;
            if (s_LastImportedVectorImagesCount == importedVectorImagesCount)
                return false;

            s_LastImportedVectorImagesCount = importedVectorImagesCount;

            return true;
        }

        // This check isn't required for the "legacy" atlas manager, since it could detect this condition and rebuild
        // the texture. However, the use done by the shader info allocator would not allow this and requires a rebuild.
        private static bool CheckForRenderTexturesTrashed()
        {
            if (s_RenderTexture == null)
            {
                s_RenderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32);
                s_RenderTexture.Create();
                return true;
            }

            if (!s_RenderTexture.IsCreated())
            {
                s_RenderTexture.Create();
                return true;
            }

            return false;
        }
    }
}
