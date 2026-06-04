// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    /// <summary>
    /// Base abstract class that provides some common functionality for providers seeking to integrate with management assisted build.
    /// </summary>
    /// <typeparam name="T">The type parameter that will be used as the base type of the settings.</typeparam>
    public abstract class AdaptivePerformanceBuildHelper<T>  : IPreprocessBuildWithReport, IPostprocessBuildWithReport where T : UnityEngine.Object
    {
        /// <summary>Override of base IPreprocessBuildWithReport.</summary>
        /// <returns>The callback order.</returns>
        public virtual int callbackOrder { get { return 0; } }

        /// <summary>Override of base IPreprocessBuildWithReport.</summary>
        /// <returns>String that specifies the key to be used to set or get settings in EditorBuildSettings.</returns>
        public abstract string BuildSettingsKey { get; }

        bool IsProviderEnabled(AdaptivePerformanceGeneralSettings generalSettings)
        {
            if (generalSettings.Manager == null)
            {
                Debug.Log("Adaptive Performance manager not found");
                return false;
            }

            foreach (var loader in generalSettings.Manager.loaders)
            {
                if (loader is T)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Helper function to return current settings for a specific build target.</summary>
        ///
        /// <param name="buildTargetGroup">An enum that specifies which platform group this build is for.</param>
        /// <returns>A Unity object that represents the settings instance data for that build target, or null if not found.</returns>
        public virtual UnityEngine.Object SettingsForBuildTargetGroup(BuildTargetGroup buildTargetGroup)
        {

            // Always remember to clean up preloaded assets before build to make sure we don't
            // dirty the current builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            var activeProfile = BuildProfile.GetActiveBuildProfile();
            T settings = null;
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
                                if (providerSettings != null && providerSettings.GetType() == typeof(T))
                                {
                                    settings = providerSettings as T;
                                }
                            }
                        }
                    }
                }
            }

            // fall back to project settings.
            if (settings == null)
            {
                EditorBuildSettings.TryGetConfigObject(BuildSettingsKey,
                    out settings);
                if (settings == null)
                    return null;

                var generalSetting = AdaptivePerformanceGeneralSettingsPerBuildTarget.AdaptivePerformanceGeneralSettingsForBuildTarget(buildTargetGroup);
                if (!generalSetting)
                {
                    Debug.Log("generalSetting not found");
                    return null;
                }

                if(!IsProviderEnabled(generalSetting))
                {
                    return null;
                }
            }

            EditorUtilities.AddToPreloadedAssetList(settings);
            return settings;
        }

        void CleanOldSettings()
        {
            BuildHelpers.CleanOldSettings<T>();
        }

        void SetSettingsForRuntime(UnityEngine.Object settingsObj)
        {
            // Always remember to clean up preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            if (settingsObj == null)
                return;

            if (!(settingsObj is T))
            {
                Type typeOfT = typeof(T);
                Debug.LogErrorFormat("Settings object is not of type {0}. No settings will be copied to runtime.", typeOfT.Name);
                return;
            }
            EditorUtilities.AddToPreloadedAssetList(settingsObj);
        }

        /// <summary>
        /// Override of base IPreprocessBuildWithReport.
        /// </summary>
        /// <param name="report">BuildReport instance passed in from the build pipeline.</param>
        public virtual void OnPreprocessBuild(BuildReport report)
        {
            SetSettingsForRuntime(SettingsForBuildTargetGroup(report.summary.platformGroup));
        }

        /// <summary>
        /// Override of base IPostprocessBuildWithReport.
        /// </summary>
        /// <param name="report">BuildReport instance passed in from build pipeline.</param>
        public virtual void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to clean up preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }
    }

    /// <summary>
    /// Helper utilities for build-time modifications.
    /// </summary>
    public static class AdaptivePerformanceBuildUtils
    {
        /// <summary>
        /// Add key to boot.config
        /// </summary>
        /// <param name="path">Player build path</param>
        /// <param name="bootConfigKey">Key to add or update</param>
        /// <param name="wantedSettingValue">Value for the key</param>
        public static void UpdateBootConfigBoostSetting(string path, string bootConfigKey, string wantedSettingValue)
        {
            string bootConfig = Path.Combine(path, "src/main/assets/bin/Data/boot.config");
            if (!File.Exists(bootConfig))
                return;
            var lines = File.ReadAllLines(bootConfig);
            string searchSetting = $"{bootConfigKey}=";
            int i;
            for (i = 0; i < lines.Length; ++i)
                if (lines[i].StartsWith(searchSetting))
                    break;
            wantedSettingValue = searchSetting + wantedSettingValue;
            if (i >= lines.Length)
                File.AppendAllLines(bootConfig, new[] { wantedSettingValue });
            else
            {
                if (lines[i] != wantedSettingValue)
                {
                    lines[i] = wantedSettingValue;
                    File.WriteAllLines(bootConfig, lines);
                }
            }
        }

        /// <summary>
        /// Get value for boost mode on startup for given settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string GetWantedStartupBoostSetting(UnityEngine.AdaptivePerformance.IAdaptivePerformanceSettings settings)
        {
            if (settings == null)
                return "1";
            bool enabled = settings.enableBoostOnStartup;
            return enabled ? "1" : "0";
        }
        /// <summary>
        /// Add the custom scaler in users' assets to the provider settings.
        /// Only needed if you want to scan for custom scalers during build time.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool AddCustomScalerToProviderSetting(UnityEngine.AdaptivePerformance.IAdaptivePerformanceSettings settings)
        {
            return EditorUtilities.AddCustomScalerToProviderSetting(settings);
        }

        /// <summary>
        /// Checks the "Adjust iOS FPS based on thermal state" Player Setting and, if it is enabled,
        /// prompts the user (via dialog in interactive sessions) to disable it so the Adaptive
        /// Performance Apple provider can manage thermal mitigation.
        /// </summary>
        /// <param name="profile">Optional active build profile. When supplied, the profile's player
        /// settings are inspected and updated; otherwise the project-wide PlayerSettings are used.</param>
        /// <param name="forcePrompt">When <c>true</c>, bypass the per-session "asked once" suppression
        /// and always re-display the dialog (when not in batch mode). Build-time callers should pass
        /// <c>true</c> so a misconfiguration is surfaced even if the user already dismissed the
        /// dialog earlier in the editor session.</param>
        /// <returns><c>true</c> if the setting is still enabled (i.e. the user declined to disable
        /// it, or the dialog could not be shown in batch mode); <c>false</c> if the setting is
        /// already disabled or was disabled in response to the prompt.</returns>
        public static bool CheckEnableThermalStateForIOS(BuildProfile profile = null, bool forcePrompt = false)
        {
            return EditorUtilities.CheckEnableThermalStateForIOS(profile, forcePrompt);
        }
    }
}
