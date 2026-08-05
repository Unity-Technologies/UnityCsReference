// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.AssetAnalysis
{
    /// <summary>
    /// For building a shader dependency tree.
    /// </summary>
    public class ShaderDependencyNode : DependencyNode
    {
        internal override void BuildHierarchy(int depth, DependencyBuildContext context)
        {
            if (!context.ShaderToMaterials.TryGetValue(Location.Path, out var materialPaths))
                return;

            foreach (var path in materialPaths)
                AddChild(new AssetDependencyNode { Location = new Location(path) });
        }

        internal override string GetName()
        {
            return Location.Filename;
        }

        internal override string GetPrettyName()
        {
            return Location.Path;
        }

        internal override bool IsPerfCritical()
        {
            return false;
        }
    }
}
