// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;
using UnityEngine.AdaptivePerformance;
using System.Runtime.CompilerServices;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

[assembly: InternalsVisibleTo("Unity.AdaptivePerformance.Editor.Tests")]
namespace UnityEditor.AdaptivePerformance.Editor
{
    class AdaptivePerformanceGeneralBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 0;  }
        }

        void CleanOldSettings()
        {
            BuildHelpers.CleanOldSettings<AdaptivePerformanceGeneralSettings>();
        }


        public void OnPreprocessBuild(BuildReport report)
        {
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            AdaptivePerformanceGeneralSettings settings = null;

            if (activeProfile != null && activeProfile.platformBuildProfile?.adaptivePerformanceEnabled ==  true)
            {
                settings = activeProfile.GetComponent<AdaptivePerformanceGeneralSettings>();
            }

            if (settings == null)
            {
                AdaptivePerformanceGeneralSettingsPerBuildTarget buildTargetSettings = null;
                EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey,
                    out buildTargetSettings);
                if (buildTargetSettings == null || buildTargetSettings.EnableAdaptivePerformance == false)
                    return;

                settings = buildTargetSettings.SettingsForBuildTarget(report.summary.platformGroup);
            }

            if (settings == null)
                return;

            bool enabled = EditorUtilities.CheckEnableFrameTimingState(activeProfile);
            if (!enabled)
            {
                Debug.Log("Frame timing stats is not enabled for adaptive performance");
            }

            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();

            // Always remember to clean up preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            EditorUtilities.AddToPreloadedAssetList(settings);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to clean up preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }
    }
}
