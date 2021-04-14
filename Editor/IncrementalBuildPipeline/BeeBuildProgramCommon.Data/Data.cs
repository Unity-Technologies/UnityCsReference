// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace BeeBuildProgramCommon.Data
{
    public class PackageInfo
    {
        public string Name;
        public string ResolvedPath;
        public bool Immutable;
    }

    public class ConfigurationData
    {
        public string Il2CppDir;
        public string UnityLinkerPath;
        public string Il2CppPath;
        public string NetCoreRunPath;
        public string EditorContentsPath;
        public PackageInfo[] Packages;
        public string UnityVersion;
        public string UnitySourceCodePath;
        public bool AdvancedLicense;
    }
}
