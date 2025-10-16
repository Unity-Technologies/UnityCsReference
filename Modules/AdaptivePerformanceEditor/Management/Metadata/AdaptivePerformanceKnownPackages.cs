// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.AdaptivePerformance.Editor.Metadata
{
    internal class AdaptivePerformanceKnownPackages
    {
        class KnownLoaderMetadata : IAdaptivePerformanceLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
            public int priority { get; set; }
        }

        class KnownPackageMetadata : IAdaptivePerformancePackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public string licenseURL { get; set; }
            public string isDefaultPlatformProvider { get; set; }
            public string isDeprecated { get; set; }
            public List<IAdaptivePerformanceLoaderMetadata> loaderMetadata { get; set; }
        }

        class KnownPackage : IAdaptivePerformancePackage
        {
            public IAdaptivePerformancePackageMetadata metadata { get; set; }
            public bool PopulateNewSettingsInstance(ScriptableObject obj) { return true; }
        }

        private static Lazy<List<IAdaptivePerformancePackage>> s_KnownPackages = new Lazy<List<IAdaptivePerformancePackage>>(InitKnownPackages);

        internal static List<IAdaptivePerformancePackage> Packages => s_KnownPackages.Value;

        static List<IAdaptivePerformancePackage> InitKnownPackages()
        {
            List<IAdaptivePerformancePackage> packages = new List<IAdaptivePerformancePackage>();

            packages.Add(new KnownPackage() {
                metadata = new KnownPackageMetadata(){
                    packageName = "Adaptive Performance Samsung Android",
                    packageId = "com.unity.adaptiveperformance.samsung.android",
                    settingsType = "UnityEngine.AdaptivePerformance.Samsung.Android.SamsungAndroidProviderSettings",
                    licenseURL = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance.samsung.android@latest?subfolder=/license/LICENSE.html",
                    isDefaultPlatformProvider = "false",
                    isDeprecated = "true",
                    loaderMetadata = new List<IAdaptivePerformanceLoaderMetadata>()
                    {
                        new KnownLoaderMetadata() {
                            loaderName = "Samsung Android Provider",
                            loaderType = "UnityEngine.AdaptivePerformance.Samsung.Android.SamsungAndroidProviderLoader",
                            supportedBuildTargets = new List<BuildTargetGroup>()
                            {
                                BuildTargetGroup.Android
                            },
                            priority = 1,
                        },
                    }
                }
            });

            packages.Add(new KnownPackage() {
                metadata = new KnownPackageMetadata(){
                    packageName = "Adaptive Performance Android",
                    packageId = "com.unity.adaptiveperformance.google.android",
                    settingsType = "UnityEngine.AdaptivePerformance.Google.Android.GoogleAndroidProviderSettings",
                    licenseURL = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance.google.android@latest?subfolder=/license/LICENSE.html",
                    isDefaultPlatformProvider = "true",
                    isDeprecated = "false",
                    loaderMetadata = new List<IAdaptivePerformanceLoaderMetadata>()
                    {
                        new KnownLoaderMetadata() {
                            loaderName = "Android Provider",
                            loaderType = "UnityEngine.AdaptivePerformance.Google.Android.GoogleAndroidProviderLoader",
                            supportedBuildTargets = new List<BuildTargetGroup>()
                            {
                                BuildTargetGroup.Android
                            },
                            priority = 0,
                        },
                    }
                }
            });

            packages.Add(new KnownPackage()
            {
                metadata = new KnownPackageMetadata()
                {
                    packageName = "Adaptive Performance",
                    packageId = "com.unity.adaptiveperformance",
                    settingsType = "UnityEditor.AdaptivePerformance.Simulator.Editor.SimulatorProviderSettings",
                    licenseURL = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance@latest?subfolder=/license/LICENSE.html",
                    isDefaultPlatformProvider = "true",
                    isDeprecated = "false",
                    loaderMetadata = new List<IAdaptivePerformanceLoaderMetadata>()
                    {
                        new KnownLoaderMetadata() {
                            loaderName = "Device Simulator Provider",
                            loaderType = "UnityEditor.AdaptivePerformance.Simulator.Editor.SimulatorProviderLoader",
                            supportedBuildTargets = new List<BuildTargetGroup>()
                            {
                                BuildTargetGroup.Standalone
                            },
                            priority = Int32.MaxValue,
                        },
                    }
                }
            });

            packages.Add(new KnownPackage()
            {
                metadata = new KnownPackageMetadata()
                {
                    packageName = "Adaptive Performance Basic",
                    // Unique Id for saving the package metadata.
                    packageId = "com.unity.adaptiveperformance.basic",
                    settingsType = "UnityEngine.AdaptivePerformance.Basic.BasicProviderSettings",
                    licenseURL = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance@latest?subfolder=/license/LICENSE.html",
                    isDeprecated = "false",
                    loaderMetadata = new List<IAdaptivePerformanceLoaderMetadata>()
                    {
                        new KnownLoaderMetadata() {
                            loaderName = "Basic Provider",
                            loaderType = "UnityEngine.AdaptivePerformance.Basic.BasicProviderLoader",
                            supportedBuildTargets = new List<BuildTargetGroup>()
                            {
                                BuildTargetGroup.Standalone,
                                BuildTargetGroup.Android,
                                BuildTargetGroup.iOS,
                                BuildTargetGroup.tvOS,
                                BuildTargetGroup.VisionOS,
                                BuildTargetGroup.PS4,
                                BuildTargetGroup.PS5,
                                BuildTargetGroup.XboxOne,
                                BuildTargetGroup.GameCoreXboxSeries,
                                BuildTargetGroup.GameCoreXboxOne
                            },
                            priority = 5,
                        },
                    }
                }
            });
            return packages;
        }
    }
}
