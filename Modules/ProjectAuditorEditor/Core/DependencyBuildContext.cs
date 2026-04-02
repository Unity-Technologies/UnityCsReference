// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Core
{
    // Precomputed lookup tables for DependencyNode.BuildHierarchy
    internal class DependencyBuildContext
    {
        // For code analysis: callee full name -> all (callee, caller) call pairs
        public readonly Dictionary<string, HashSet<CallInfo>> BucketedCalls;

        // Asset path -> asset paths of assets depending on it
        public readonly Dictionary<string, HashSet<string>> AssetDependencyReverseLookup;

        // Shader asset path -> asset paths of materials that use it
        public readonly Dictionary<string, HashSet<string>> ShaderToMaterials;

        // Texture asset path -> asset paths of materials that reference it
        public readonly Dictionary<string, HashSet<string>> TextureToMaterials;

        // Sprite atlas -> its asset path
        public readonly Dictionary<SpriteAtlas, string> SpriteAtlasPaths;

        public DependencyBuildContext(
            Dictionary<string, HashSet<CallInfo>> bucketedCalls,
            Dictionary<string, HashSet<string>> assetDependencyReverseLookup,
            Dictionary<string, HashSet<string>> shaderToMaterials,
            Dictionary<string, HashSet<string>> textureToMaterials,
            Dictionary<SpriteAtlas, string> spriteAtlasPaths)
        {
            BucketedCalls = bucketedCalls;
            AssetDependencyReverseLookup = assetDependencyReverseLookup;
            ShaderToMaterials = shaderToMaterials;
            TextureToMaterials = textureToMaterials;
            SpriteAtlasPaths = spriteAtlasPaths;
        }
    }
}
