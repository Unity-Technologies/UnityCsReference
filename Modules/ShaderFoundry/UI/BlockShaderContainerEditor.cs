// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.CodeEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderFoundry
{
    internal static class EnumerableExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> sequence)
        {
            foreach (T _ in sequence)
                return false;
            return true;
        }
    }

    [CustomEditor(typeof(BlockShaderContainer))]
    internal class BlockShaderContainerEditor : Editor, IHasCustomMenu
    {
        internal const string kDefaultIconPath = "BlockShaderContainer Icon";
        internal const string kShaderInterfaceAssetIconPath = "ShaderBlockInterface Icon";
        internal const string kBlockLibraryAssetIconPath = "ShaderBlockLibrary Icon";
        internal const string kTemplateAssetIconPath = "ShaderBlockTemplate Icon";
        internal const string kBlockSequenceAssetIconPath = "BlockSequence Icon";

        // A convenience property for accessing the editor's target.
        // We do not support multi-target editing.
        private BlockShaderContainer Target => target as BlockShaderContainer;

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            // We could return null instead of the default icon, but that ends up adding a border to the icon.
            // To preserve the original icon, we explicitly return the default texture in this method.
            Texture2D defaultTexture = EditorGUIUtility.FindTexture(kDefaultIconPath);
            BlockShaderContainer containerArtifact = Target;
            if (containerArtifact == null || containerArtifact.GetContainer() == null)
                return defaultTexture;

            // If the asset contains only one type of language construct, give it a special icon
            ShaderContainer container = containerArtifact.GetContainer();
            bool hasBlocks = !container.GetLocalBlocks().Empty();
            bool hasBlockSequences = !container.GetLocalBlockSequences().Empty();
            bool hasTemplates = !container.GetLocalTemplates().Empty();
            bool hasShaderInterfaces = !container.GetLocalBlockShaderInterfaces().Empty();
            if (hasBlocks && !hasBlockSequences && !hasTemplates && !hasShaderInterfaces)
                return EditorGUIUtility.FindTexture(kBlockLibraryAssetIconPath);
            else if (hasBlockSequences && !hasBlocks && !hasTemplates && !hasShaderInterfaces)
                return EditorGUIUtility.FindTexture(kBlockSequenceAssetIconPath);
            else if (hasTemplates && !hasBlockSequences && !hasBlocks && !hasShaderInterfaces)
                return EditorGUIUtility.FindTexture(kTemplateAssetIconPath);
            else if (hasShaderInterfaces && !hasBlockSequences && !hasBlocks && !hasTemplates)
                return EditorGUIUtility.FindTexture(kShaderInterfaceAssetIconPath);
            else
                return defaultTexture;
        }

        // Entry point for rendering the inspector GUI
        public override VisualElement CreateInspectorGUI()
        {
            return BlockShaderContainerInspectorHelper.CreateGUI(Target);
        }

        // Handles adding custom entries to the "..." inspector menu
        public void AddItemsToMenu(GenericMenu menu)
        {
            List<BlockShaderSourceArtifact> generatedShaders = LoadGeneratedShaders();
            GUIContent openGeneratedShadersLabel = new GUIContent("Open Generated ShaderLab Shaders");
            if (generatedShaders.Count > 0)
                menu.AddItem(openGeneratedShadersLabel, false, OpenGeneratedShaders);
            else
                menu.AddDisabledItem(openGeneratedShadersLabel);
        }

        // Opens all generated SL shaders for the target asset in a text editor
        private void OpenGeneratedShaders()
        {
            const string tempDirectory = "Temp";

            // Write to a temp file and open it in a text editor
            List<BlockShaderSourceArtifact> generatedShaders = LoadGeneratedShaders();
            List<string> generatedFilePaths = new List<string>(generatedShaders.Count);
            foreach (BlockShaderSourceArtifact sourceObject in generatedShaders)
            {
                string filePath = $"{tempDirectory}/{sourceObject.shaderName}.shader";
                System.IO.File.WriteAllText(filePath, sourceObject.shaderSource);
                generatedFilePaths.Add(filePath);
            }

            // Open each file in a text editor
            foreach (string filePath in generatedFilePaths)
                CodeEditor.CurrentEditor.OpenProject(filePath);
        }

        // Loads all BlockShaderSourceArtifacts in the target asset into memory
        private List<BlockShaderSourceArtifact> LoadGeneratedShaders()
        {
            string assetPath = AssetDatabase.GetAssetPath(Target);
            List<BlockShaderSourceArtifact> foundShaders = new List<BlockShaderSourceArtifact>();
            Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object assetObject in assetObjects)
            {
                if (assetObject is BlockShaderSourceArtifact generatedShader)
                    foundShaders.Add(generatedShader);
            }
            return foundShaders;
        }
    }
}
