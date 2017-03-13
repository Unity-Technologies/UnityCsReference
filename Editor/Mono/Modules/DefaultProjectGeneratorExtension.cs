// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Modules
{
    internal abstract class DefaultProjectGeneratorExtension : IProjectGeneratorExtension
    {
        public virtual void GenerateCSharpProject(CSharpProject project, string assemblyName, IEnumerable<string> sourceFiles, IEnumerable<string> defines, IEnumerable<CSharpProject> additionalProjectReferences)
        {
        }
    }
}
