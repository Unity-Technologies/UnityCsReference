// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;
using UnityEngine.Bindings;

namespace UnityEditor.iOS
{
    [VisibleToOtherModules]
    internal interface ISwiftPackageProvider : IOrderedCallback
    {
        // Called on the main thread before the build program generates the Xcode project.
        XcodeSwiftPackage[] OnCollectSwiftPackages(SwiftPackageProviderContext context);
    }

    [VisibleToOtherModules]
    internal struct SwiftPackageProviderContext
    {
        public BuildTarget platform { get; }

        public SwiftPackageProviderContext(BuildTarget platform)
        {
            this.platform = platform;
        }
    }

    [VisibleToOtherModules]
    internal enum XcodeSwiftPackageRequirementKind
    {
        ExactVersion,
        UpToNextMajorVersion,
        UpToNextMinorVersion,
        VersionRange,
        Branch,
        Revision,
    }

    // The fields Xcode stores for each kind: version for ExactVersion, minimumVersion for the UpToNext kinds,
    // minimumVersion and maximumVersion for VersionRange, and branch or revision for the others.
    [VisibleToOtherModules]
    internal struct XcodeSwiftPackageRequirement
    {
        public XcodeSwiftPackageRequirementKind kind { get; set; }
        public string version { get; set; }
        public string minimumVersion { get; set; }
        public string maximumVersion { get; set; }
        public string branch { get; set; }
        public string revision { get; set; }
    }

    [VisibleToOtherModules]
    internal struct XcodeSwiftPackageProduct
    {
        public string name { get; set; }
        public bool weakLink { get; set; }
    }

    [VisibleToOtherModules]
    internal struct XcodeSwiftPackage
    {
        public string url { get; set; }
        public XcodeSwiftPackageRequirement requirement { get; set; }
        public XcodeSwiftPackageProduct[] products { get; set; }
    }
}
