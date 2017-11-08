// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager
{
    public interface IShouldIncludeInBuildCallback
    {
        string PackageName { get; }
        bool ShouldIncludeInBuild(string path);
    }
}
