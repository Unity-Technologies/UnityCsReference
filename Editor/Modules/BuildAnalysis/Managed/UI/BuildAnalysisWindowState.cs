// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    [FilePath("Library/BuildAnalysisWindowState.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BuildAnalysisWindowState : ScriptableSingleton<BuildAnalysisWindowState>
    {
        [SerializeField] List<string> m_CollapsedBuilds = new List<string>();

        public IReadOnlyList<string> collapsedBuilds => m_CollapsedBuilds;

        public void SetCollapsedBuilds(List<string> collapsedBuilds)
        {
            m_CollapsedBuilds = collapsedBuilds;
            Save(true);
        }
    }
}
