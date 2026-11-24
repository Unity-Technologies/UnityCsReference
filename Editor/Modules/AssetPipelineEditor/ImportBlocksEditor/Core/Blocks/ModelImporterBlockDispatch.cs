// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal static class ModelImporterBlockDispatch
    {
        internal static void BlockPreprocessModel(ModelImporter importer, AssetImportContext ctx)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessModel>())
                b.OnPreprocessModel(importer, ctx);
        }

        internal static void BlockPostprocessModel(AssetImportContext ctx, GameObject root)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessModel>())
                b.OnPostprocessModel(root, ctx);
        }

        internal static void BlockPostprocessMeshHierarchy(AssetImportContext ctx, GameObject root)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessMeshHierarchy>())
                b.OnPostprocessMeshHierarchy(root, ctx);
        }

        internal static bool BlockHasAssignMaterial()
            => AssetImportCallbackDispatch.CurrentBlockDispatch().Contains<IOnAssignMaterialModel>();

        // Unlike the postprocessor phases, blocks run as a chain: each receives the current material and
        // may replace it. Null means the block phase had no opinion and dispatch falls through.
        internal static Material BlockAssignMaterial(AssetImportContext ctx, Renderer renderer, Material material)
        {
            var current = material;
            bool assigned = false;
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnAssignMaterialModel>())
            {
                var result = b.OnAssignMaterialModel(current, renderer, ctx);
                if (result != null)
                {
                    current = result;
                    assigned = true;
                }
            }
            return assigned ? current : null;
        }

        internal static void BlockPreprocessAnimation(ModelImporter importer, AssetImportContext ctx)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessAnimation>())
                b.OnPreprocessAnimation(importer, ctx);
        }

        internal static void BlockPostprocessAnimation(AssetImportContext ctx, GameObject root, AnimationClip clip)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessAnimation>())
                b.OnPostprocessAnimation(root, clip, ctx);
        }

        internal static bool BlockHasPostprocessGameObjectWithUserProperties()
            => AssetImportCallbackDispatch.CurrentBlockDispatch().Contains<IOnPostprocessGameObjectWithUserProperties>();

        internal static void BlockPostprocessGameObjectWithUserProperties(AssetImportContext ctx, GameObject root, string[] propNames, object[] values)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessGameObjectWithUserProperties>())
                b.OnPostprocessGameObjectWithUserProperties(root, propNames, values, ctx);
        }

        internal static bool BlockHasPostprocessGameObjectWithAnimatedUserProperties()
            => AssetImportCallbackDispatch.CurrentBlockDispatch().Contains<IOnPostprocessGameObjectWithAnimatedUserProperties>();

        // Caller owns the single conversion and copy-back.
        internal static void BlockPostprocessGameObjectWithAnimatedUserProperties(AssetImportContext ctx, GameObject root, EditorCurveBinding[] bindings)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessGameObjectWithAnimatedUserProperties>())
                b.OnPostprocessGameObjectWithAnimatedUserProperties(root, bindings, ctx);
        }

        internal static void BlockPreprocessMaterialDescription(AssetImportContext ctx, MaterialDescription description, Material material, AnimationClip[] animations)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessMaterialDescription>())
                b.OnPreprocessMaterialDescription(description, material, animations, ctx);
        }

        internal static void BlockPostprocessMaterial(AssetImportContext ctx, Material material)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessMaterial>())
                b.OnPostprocessMaterial(material, ctx);
        }

        internal static void BlockPreprocessCameraDescription(AssetImportContext ctx, CameraDescription description, Camera camera, AnimationClip[] animations)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessCameraDescription>())
                b.OnPreprocessCameraDescription(description, camera, animations, ctx);
        }

        internal static void BlockPreprocessLightDescription(AssetImportContext ctx, LightDescription description, Light light, AnimationClip[] animations)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessLightDescription>())
                b.OnPreprocessLightDescription(description, light, animations, ctx);
        }
    }
}
