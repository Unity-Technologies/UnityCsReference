// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Android;
using UnityEditor.iOS;

namespace UnityEditor.InsightsEditor
{
    internal class RemoteGradleDependencyProvider : IAndroidGradleDependencyProvider
    {
        public int callbackOrder => 0;

        public string[] OnCollectGradleDependencies(AndroidGradleDependencyProviderContext context)
        {
            var packages = RemoteBuildDependencies.GetAndroidDependencies();
            var dependencies = new string[packages.Length];
            for (int i = 0; i < packages.Length; ++i)
            {
                dependencies[i] = packages[i].mavenCoordinate;
            }

            return dependencies;
        }

        public string[] OnCollectMavenRepositories(AndroidGradleDependencyProviderContext context)
        {
            var repositories = new List<string>();
            foreach (var package in RemoteBuildDependencies.GetAndroidDependencies())
            {
                repositories.AddRange(package.repositories ?? Array.Empty<string>());
            }

            return repositories.ToArray();
        }
    }

    internal class RemoteSwiftPackageProvider : ISwiftPackageProvider
    {
        public int callbackOrder => 0;

        // The service describes iOS builds only.
        public XcodeSwiftPackage[] OnCollectSwiftPackages(SwiftPackageProviderContext context)
        {
            if (context.platform != BuildTarget.iOS)
                return Array.Empty<XcodeSwiftPackage>();

            var packages = new List<XcodeSwiftPackage>();
            foreach (var package in RemoteBuildDependencies.GetIosDependencies())
            {
                if (!TryConvertRequirement(package.requirement, out var requirement))
                    continue;

                var products = new XcodeSwiftPackageProduct[package.products?.Length ?? 0];
                for (int i = 0; i < products.Length; ++i)
                {
                    products[i] = new XcodeSwiftPackageProduct
                    {
                        name = package.products[i].name,
                        weakLink = package.products[i].weakLink,
                    };
                }

                packages.Add(new XcodeSwiftPackage
                {
                    url = package.url,
                    requirement = requirement,
                    products = products,
                });
            }

            return packages.ToArray();
        }

        static bool TryConvertRequirement(SwiftRequirement requirement, out XcodeSwiftPackageRequirement converted)
        {
            switch (requirement.kind)
            {
                case SwiftRequirementKind.ExactVersion:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.ExactVersion,
                        version = requirement.value,
                    };
                    return true;
                case SwiftRequirementKind.UpToNextMajorFrom:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.UpToNextMajorVersion,
                        minimumVersion = requirement.value,
                    };
                    return true;
                case SwiftRequirementKind.UpToNextMinorFrom:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.UpToNextMinorVersion,
                        minimumVersion = requirement.value,
                    };
                    return true;
                case SwiftRequirementKind.VersionRange:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.VersionRange,
                        minimumVersion = requirement.value,
                        maximumVersion = requirement.maximumVersion,
                    };
                    return true;
                case SwiftRequirementKind.Branch:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.Branch,
                        branch = requirement.value,
                    };
                    return true;
                case SwiftRequirementKind.Revision:
                    converted = new XcodeSwiftPackageRequirement
                    {
                        kind = XcodeSwiftPackageRequirementKind.Revision,
                        revision = requirement.value,
                    };
                    return true;
                default:
                    converted = default;
                    return false;
            }
        }
    }
}
