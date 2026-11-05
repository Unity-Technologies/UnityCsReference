// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    class IssueOverrides
    {
        readonly HashSet<(string descriptorId, string path, int line, string moduleName)> m_Overrides = new HashSet<(string, string, int, string)>();
        readonly HashSet<(string descriptorId, string path, int line)> m_OverriddenIssues = new HashSet<(string, string, int)>();

        public IssueOverrides(IEnumerable<Report.IssueOverride> storedOverrides, ICollection<string> analyzedModuleNames)
        {
            foreach (var storedOverride in storedOverrides)
            {
                if (!analyzedModuleNames.Contains(storedOverride.moduleName))
                    Add(storedOverride.location, storedOverride.descriptorId, storedOverride.moduleName);
            }
        }

        public void Add(Location location, string overriddenDescriptorId, string moduleName)
        {
            if (location == null || string.IsNullOrEmpty(location.Path))
                return;

            m_Overrides.Add((overriddenDescriptorId, location.Path, location.Line, moduleName));
            m_OverriddenIssues.Add((overriddenDescriptorId, location.Path, location.Line));
        }

        public bool IsOverridden(string descriptorId, Location location)
        {
            return descriptorId != null && location != null && m_OverriddenIssues.Contains((descriptorId, location.Path, location.Line));
        }

        public List<Report.IssueOverride> ToList()
        {
            var list = new List<Report.IssueOverride>(m_Overrides.Count);
            foreach (var (descriptorId, path, line, moduleName) in m_Overrides)
            {
                list.Add(new Report.IssueOverride { descriptorId = descriptorId, location = new Location(path, line), moduleName = moduleName });
            }

            return list;
        }
    }
}
