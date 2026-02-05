// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.AdaptivePerformance.Basic;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Basic.Editor
{
    internal class BasicProviderBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// Override of <see cref="IPreprocessBuildWithReport"/> and <see cref="IPostprocessBuildWithReport"/>.
        /// </summary>
        public int callbackOrder
        {
            get { return 0; }
        }

        /// <summary>
        /// Clears out settings which could be left over from previous unsuccessfull runs.
        /// </summary>
        void CleanOldSettings()
        {
            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null)
                return;

            List<Object> curSettings = new List<Object>();

            foreach (Object asset in preloadedAssets)
            {
                if (asset != null && asset.GetType() != typeof(BasicProviderSettings))
                {
                    curSettings.Add(asset);
                }
            }

            if (curSettings.Count != preloadedAssets.Length)
            {
                PlayerSettings.SetPreloadedAssets(curSettings.ToArray());
            }
        }


        bool IsProviderEnabled(AdaptivePerformanceGeneralSettings generalSettings)
        {
            if (generalSettings.Manager == null)
            {
                Debug.Log("Adaptive Performance manager not found");
                return false;
            }

            foreach (var loader in generalSettings.Manager.loaders)
            {
                if (loader is BasicProviderLoader)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Override of <see cref="IPreprocessBuildWithReport"/>.
        /// use settings from build profile if enabled or fallback to project settings if any.
        /// </summary>
        /// <param name="report">Build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            // Always remember to clean up preloaded assets before build to make sure we don't
            // dirty the current builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            var activeProfile = BuildProfile.GetActiveBuildProfile();
            BasicProviderSettings settings = null;
            if (activeProfile != null && activeProfile.platformBuildProfile.adaptivePerformanceEnabled)
            {
                var generalSetting = activeProfile.GetComponent<AdaptivePerformanceGeneralSettings>();
                if (generalSetting != null)
                {
                    // if no loader, do not add provider settings. This is to accommodate case that user still want to
                    // edit the setting without enable the provider. The provider is only enabled when the loader is added.
                    if (IsProviderEnabled(generalSetting))
                    {
                        var container = activeProfile.GetComponent<BuildProfileProviderContainer>();
                        if (container != null)
                        {
                            foreach (var providerSettings in container.adaptivePerformanceProviderSettings)
                            {
                                if (providerSettings != null && providerSettings.GetType() == typeof(BasicProviderSettings))
                                {
                                    settings = providerSettings as BasicProviderSettings;
                                }
                            }
                        }
                    }
                }
            }

            // fall back to project settings.
            if (settings == null)
            {
                EditorBuildSettings.TryGetConfigObject(BasicProviderConstants.k_SettingKey,
                    out settings);
                if (settings == null)
                    return;

                AdaptivePerformanceGeneralSettingsPerBuildTarget buildTargetSettings = null;
                EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey,
                    out buildTargetSettings);
                if (buildTargetSettings == null || buildTargetSettings.EnableAdaptivePerformance == false)
                    return;

                var generalSetting = AdaptivePerformanceGeneralSettingsPerBuildTarget.AdaptivePerformanceGeneralSettingsForBuildTarget(report.summary.platformGroup);
                if (!generalSetting)
                {
                    Debug.Log("generalSetting not found");
                    return;
                }

                if(!IsProviderEnabled(generalSetting))
                {
                    return;
                }
            }
            EditorUtilities.AddCustomScalerToProviderSetting(settings);
            EditorUtilities.AddToPreloadedAssetList(settings);
        }

        /// <summary>Override of <see cref="IPostprocessBuildWithReport"/></summary>.
        /// <param name="report">Build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to clean up preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }
    }
}
