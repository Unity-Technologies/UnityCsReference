// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// For building an asset dependency tree.
    /// </summary>
    public class AssetDependencyNode : DependencyNode
    {
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
