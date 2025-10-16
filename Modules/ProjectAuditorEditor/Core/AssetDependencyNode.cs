// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor.Core
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
    }
}
