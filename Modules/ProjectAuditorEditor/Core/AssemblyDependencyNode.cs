// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AssemblyDependencyNode : DependencyNode
    {
        readonly string m_Name;

        public AssemblyDependencyNode(string name, IReadOnlyList<string> deps = null)
        {
            m_Name = name;
            if (deps != null)
            {
                foreach (var dep in deps)
                    AddChild(new AssemblyDependencyNode(dep));
            }
        }

        internal override string GetName()
        {
            return m_Name;
        }

        internal override string GetPrettyName()
        {
            return m_Name;
        }

        internal override bool IsPerfCritical()
        {
            return false;
        }
    }
}
