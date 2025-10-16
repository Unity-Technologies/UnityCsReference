// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AnalysisPlatformAttribute : Attribute
    {
        public BuildTarget Platform { get; }

        public AnalysisPlatformAttribute(BuildTarget platform)
        {
            this.Platform = platform;
        }
    }
}
