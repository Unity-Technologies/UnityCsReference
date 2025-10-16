// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace Unity.ProjectAuditor.Editor.Core
{
    class AssemblyDependencyNode : DependencyNode
    {
        readonly string m_Name;

        public AssemblyDependencyNode(string name, string[] deps = null)
        {
            m_Name = name;
            if (deps != null)
                AddChildren(deps.Select(d => new AssemblyDependencyNode(d)).ToArray<DependencyNode>());
        }

        public override string GetName()
        {
            return m_Name;
        }

        public override string GetPrettyName()
        {
            return m_Name;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
