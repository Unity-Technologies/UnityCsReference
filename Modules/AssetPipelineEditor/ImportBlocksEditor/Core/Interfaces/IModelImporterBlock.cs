// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Marker interface for blocks that participate in <see cref="ModelImporter"/> import.
    /// Implement one or more of the hook interfaces below to run code during model import.
    /// Each hook mirrors the matching <see cref="AssetPostprocessor"/> model callback, with a trailing
    /// <see cref="AssetImportContext"/> for registering dependencies/objects.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IModelImporterBlock : IBlock
    {
    }

    /// <summary>Called before the model is imported, mirroring AssetPostprocessor.OnPreprocessModel.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessModel : IModelImporterBlock
    {
        public void OnPreprocessModel(ModelImporter modelImporter, AssetImportContext ctx);
    }

    /// <summary>Called after the model hierarchy is imported, mirroring AssetPostprocessor.OnPostprocessModel.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessModel : IModelImporterBlock
    {
        public void OnPostprocessModel(GameObject root, AssetImportContext ctx);
    }

    /// <summary>Called after the mesh hierarchy is built, mirroring AssetPostprocessor.OnPostprocessMeshHierarchy.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessMeshHierarchy : IModelImporterBlock
    {
        public void OnPostprocessMeshHierarchy(GameObject root, AssetImportContext ctx);
    }

    /// <summary>
    /// Called to assign a material to a renderer, mirroring AssetPostprocessor.OnAssignMaterialModel.
    /// Blocks run as a chain in collection order: each receives the current material and returns a
    /// replacement, or null to keep it. If nothing returned a material, dispatch falls through to
    /// the late asset postprocessors.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnAssignMaterialModel : IModelImporterBlock
    {
        public Material OnAssignMaterialModel(Material material, Renderer renderer, AssetImportContext ctx);
    }

    /// <summary>Called before animation is imported, mirroring AssetPostprocessor.OnPreprocessAnimation.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessAnimation : IModelImporterBlock
    {
        public void OnPreprocessAnimation(ModelImporter modelImporter, AssetImportContext ctx);
    }

    /// <summary>Called after an animation clip is imported, mirroring AssetPostprocessor.OnPostprocessAnimation.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessAnimation : IModelImporterBlock
    {
        public void OnPostprocessAnimation(GameObject root, AnimationClip clip, AssetImportContext ctx);
    }

    /// <summary>
    /// Called with a node's imported user-defined properties, mirroring
    /// AssetPostprocessor.OnPostprocessGameObjectWithUserProperties.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessGameObjectWithUserProperties : IModelImporterBlock
    {
        public void OnPostprocessGameObjectWithUserProperties(GameObject root, string[] propNames, object[] values, AssetImportContext ctx);
    }

    /// <summary>
    /// Called with a node's animated user-defined property bindings, mirroring
    /// AssetPostprocessor.OnPostprocessGameObjectWithAnimatedUserProperties.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessGameObjectWithAnimatedUserProperties : IModelImporterBlock
    {
        public void OnPostprocessGameObjectWithAnimatedUserProperties(GameObject root, EditorCurveBinding[] bindings, AssetImportContext ctx);
    }

    /// <summary>
    /// Called before a material is generated from a material description, mirroring
    /// AssetPostprocessor.OnPreprocessMaterialDescription.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessMaterialDescription : IModelImporterBlock
    {
        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations, AssetImportContext ctx);
    }

    /// <summary>Called after a material is imported, mirroring AssetPostprocessor.OnPostprocessMaterial.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessMaterial : IModelImporterBlock
    {
        public void OnPostprocessMaterial(Material material, AssetImportContext ctx);
    }

    /// <summary>
    /// Called before a camera is generated from a camera description, mirroring
    /// AssetPostprocessor.OnPreprocessCameraDescription.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessCameraDescription : IModelImporterBlock
    {
        public void OnPreprocessCameraDescription(CameraDescription description, Camera camera, AnimationClip[] animations, AssetImportContext ctx);
    }

    /// <summary>
    /// Called before a light is generated from a light description, mirroring
    /// AssetPostprocessor.OnPreprocessLightDescription.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessLightDescription : IModelImporterBlock
    {
        public void OnPreprocessLightDescription(LightDescription description, Light light, AnimationClip[] animations, AssetImportContext ctx);
    }
}
