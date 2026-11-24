// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters.ImportBlocks;

namespace UnityEditor.AssetImporters
{
    // Backs the pipeline inspector's Execution view (AssetImporterEditor.CreateExecutionView).
    static class ImportPipelineOrderUtility
    {
        static readonly string[] k_ModelCallbacks =
        {
            "OnPreprocessAsset",
            "OnPreprocessModel",
            "OnPreprocessMaterialDescription",
            "OnPostprocessMaterial",
            "OnAssignMaterialModel",
            "OnPostprocessGameObjectWithUserProperties",
            "OnPreprocessCameraDescription",
            "OnPreprocessLightDescription",
            "OnPostprocessMeshHierarchy",
            "OnPostprocessGameObjectWithAnimatedUserProperties",
            "OnPreprocessAnimation",
            "OnPostprocessAnimation",
            "OnPostprocessModel",
        };

        static readonly string[] k_TextureCallbacks =
        {
            "OnPreprocessAsset",
            "OnPreprocessTexture",
            "OnPostprocessTexture",
            "OnPostprocessSprites",
            "OnPostprocessCubemap",
            "OnPostprocessTexture2DArray",
            "OnPostprocessTexture3D",
        };

        public static string[] GetCallbacksForImporter(AssetImporter importer)
        {
            if (importer is ModelImporter)
                return k_ModelCallbacks;
            if (importer is TextureImporter)
                return k_TextureCallbacks;
            return null;
        }

        public static List<IBlock> CollectBlocks(AssetImporter importer)
        {
            var result = new List<IBlock>();
            if (importer is ModelImporter model && model.blockCollection != null)
            {
                foreach (var block in model.blockCollection.Traverse<IBlock>())
                    result.Add(block);
            }
            else if (importer is TextureImporter texture && texture.blockCollection != null)
            {
                foreach (var block in texture.blockCollection.Traverse<IBlock>())
                    result.Add(block);
            }
            return result;
        }

        public static Type GetBlockInterfaceForCallback(string callbackName)
        {
            return typeof(IBlock).Assembly.GetType($"{typeof(IBlock).Namespace}.I{callbackName}");
        }
    }
}
