// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager
{
    [System.Obsolete(@"IShouldIncludeInBuildCallback interface is deprecated and will be removed in a later version, use PluginImporter.SetIncludeInBuildDelegate instead.", false)]
    public interface IShouldIncludeInBuildCallback
    {
        string PackageName { get; }
        bool ShouldIncludeInBuild(string path);
    }
}
