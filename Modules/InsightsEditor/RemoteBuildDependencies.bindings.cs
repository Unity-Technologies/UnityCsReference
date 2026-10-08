// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.InsightsEditor
{
    internal enum SwiftRequirementKind
    {
        Unset = 0,
        ExactVersion = 1,
        UpToNextMajorFrom = 2,
        UpToNextMinorFrom = 3,
        VersionRange = 4,
        Branch = 5,
        Revision = 6
    }

    [NativeHeader("Modules/InsightsEditor/EditorConfiguration/ConfigData.h")]
    internal struct SwiftRequirement
    {
        public SwiftRequirementKind kind;
        // The version, minimum version, branch or revision, depending on kind.
        public string value;
        // Only set for VersionRange.
        public string maximumVersion;
    }

    [NativeHeader("Modules/InsightsEditor/EditorConfiguration/ConfigData.h")]
    internal struct SwiftProduct
    {
        public string name;
        public bool weakLink;
    }

    [NativeHeader("Modules/InsightsEditor/EditorConfiguration/ConfigData.h")]
    internal struct SwiftPackage
    {
        public string url;
        public SwiftRequirement requirement;
        public SwiftProduct[] products;
    }

    [NativeHeader("Modules/InsightsEditor/EditorConfiguration/ConfigData.h")]
    internal struct MavenPackage
    {
        // group:artifact:version, as Gradle takes it.
        public string mavenCoordinate;
        public string[] repositories;
    }

    // Native packages the remote-settings service asked the Android and iOS builds to declare.
    [NativeHeader("Modules/InsightsEditor/EditorConfiguration/EditorConfigurationService.h")]
    [StaticAccessor("UnityEngine::Insights::EditorConfigurationService", StaticAccessorType.DoubleColon)]
    internal static class RemoteBuildDependencies
    {
        internal static extern MavenPackage[] GetAndroidDependencies();
        internal static extern SwiftPackage[] GetIosDependencies();
    }
}
