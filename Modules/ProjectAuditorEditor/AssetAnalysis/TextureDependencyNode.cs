// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.AssetAnalysis
{
    internal class TextureDependencyNode : DependencyNode
    {
        public override void BuildHierarchy(int depth, DependencyBuildContext context)
        {
            var uniquePaths = new HashSet<string>();

            if (context.TextureToMaterials.TryGetValue(Location.Path, out var materialPaths))
            {
                foreach (var path in materialPaths)
                    uniquePaths.Add(path);
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Location.Path);
            if (sprite != null)
            {
                foreach (var (atlasAsset, atlasPath) in context.SpriteAtlasPaths)
                {
                    if (atlasAsset.CanBindTo(sprite))
                        uniquePaths.Add(atlasPath);
                }
            }

            foreach (var path in uniquePaths)
                AddChild(new AssetDependencyNode { Location = new Location(path) });
        }

        public override string GetName()
        {
            return Location.Filename;
        }

        public override string GetPrettyName()
        {
            return Location.Path;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
