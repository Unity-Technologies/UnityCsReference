// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.AssetAnalysis
{
    internal class ShaderDependencyNode : DependencyNode
    {
        public override void BuildHierarchy(int depth, DependencyBuildContext context)
        {
            if (!context.ShaderToMaterials.TryGetValue(Location.Path, out var materialPaths))
                return;

            foreach (var path in materialPaths)
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
