// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Contains code shared by buildsystem programs and player build programs
using System;
using NiceIO;

namespace UnityEditorInternal
{
    internal class BuildEditorShared
    {
        public static NPath GetModulePlatformResourcesDirectory(NPath platformBuildDirectory)
        {
            return platformBuildDirectory.Combine("Modules");
        }
    }

    [Serializable]
    internal class ModulePlatformResources
    {
        [Serializable]
        internal class ModuleInformation
        {
            public string Name = null;
            public string[] Dependencies = null;

            public override string ToString()
            {
                return $"{Name} ({Dependencies?.Length})";
            }
        }

        public ModuleInformation[] Values = null;
    }
}
