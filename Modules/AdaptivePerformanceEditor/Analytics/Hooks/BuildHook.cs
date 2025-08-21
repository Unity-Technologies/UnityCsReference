// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

namespace UnityEditor.AdaptivePerformance.Editor.Analytics.Hooks
{
    class BuildHook : IProcessSceneWithReport
    {
        int IOrderedCallback.callbackOrder => 1;
        AdaptivePerformanceUsage m_UsageEvent;

        void IProcessSceneWithReport.OnProcessScene(Scene scene, BuildReport report)
        {
            if (report is null)
                return;

            if (!EditorAnalytics.enabled)
                return;

            m_UsageEvent = new AdaptivePerformanceUsage(new AdaptivePerformanceUsage.AdaptivePerformanceUsageAnalyticsArgs()
            {
                eventType = nameof(AdaptivePerformanceUsage.EventType.BuildPlayer),
                buildGuid = report.summary.guid.ToString(),
                targetPlatform = report.summary.platform.ToString(),
                targetPlatformVersion = GetTargetPlatformVersion(report.summary.platform),
                apProvidersInfo = GetInstalledProviders(report.summary.platformGroup)
            });
            var result = EditorAnalytics.SendAnalytic(m_UsageEvent);
            if(result != AnalyticsResult.Ok)
            {
                Debug.LogWarning($"Failed to send Adaptive Performance build analytics event: {result}");
            }
        }

        string GetTargetPlatformVersion(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return PlayerSettings.Android.targetSdkVersion.ToString();
                case BuildTarget.iOS:
                    return PlayerSettings.iOS.targetOSVersionString;
                case BuildTarget.tvOS:
                    return PlayerSettings.tvOS.targetOSVersionString;
                case BuildTarget.VisionOS:
                    return PlayerSettings.VisionOS.targetOSVersionString;
                case BuildTarget.PS4:
                case BuildTarget.PS5:
                    return PlayerSettings.PS4.SdkOverride;
                default:
                    return String.Empty;
            }
        }

        AdaptivePerformanceUsage.AdaptivePerformanceInfo[] GetInstalledProviders(BuildTargetGroup buildTarget)
        {
            var allLoaders = AdaptivePerformancePackageMetadataStore.GetLoadersForBuildTarget(buildTarget);
            var loaders = new List<AdaptivePerformanceUsage.AdaptivePerformanceInfo>();
            foreach (var i in allLoaders)
            {
                if (AdaptivePerformancePackageMetadataStore.IsPackageInstalled(i.packageId))
                {
                    var info = new AdaptivePerformanceUsage.AdaptivePerformanceInfo
                    {
                        name = i.loaderName,
                        active = AdaptivePerformancePackageMetadataStore.IsLoaderAssigned(i.loaderType, buildTarget)
                    };
                    loaders.Add(info);
                }
            }
            return loaders.Count > 0 ? loaders.ToArray() : null;
        }
    }
}
