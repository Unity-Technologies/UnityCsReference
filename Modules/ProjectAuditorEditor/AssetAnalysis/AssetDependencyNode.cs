// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.AssetAnalysis
{
    internal class AssetDependencyNode : DependencyNode
    {
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

        public override void BuildHierarchy(int depth, DependencyBuildContext context)
        {
            // Prevent infinite recursion on cyclic dependencies
            if (depth > k_MaxDepth)
                return;

            if (!context.AssetDependencyReverseLookup.TryGetValue(Location.Path, out var assetPaths))
                return;

            foreach (var path in assetPaths)
            {
                var child = new AssetDependencyNode { Location = new Location(path) };
                AddChild(child);
                child.BuildHierarchy(depth + 1, context);
            }
        }
    }
}
