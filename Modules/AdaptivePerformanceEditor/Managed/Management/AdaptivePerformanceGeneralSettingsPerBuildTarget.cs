// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AdaptivePerformance;

using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEngine.Scripting;

namespace UnityEditor.AdaptivePerformance.Editor
{
    /// <summary>
    /// Container class that holds general settings for each build target group installed in Unity.
    /// </summary>
    public class AdaptivePerformanceGeneralSettingsPerBuildTarget : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<BuildTargetGroup> Keys = new List<BuildTargetGroup>();

        [SerializeField]
        List<AdaptivePerformanceGeneralSettings> Values = new List<AdaptivePerformanceGeneralSettings>();
        Dictionary<BuildTargetGroup, AdaptivePerformanceGeneralSettings> Settings = new Dictionary<BuildTargetGroup, AdaptivePerformanceGeneralSettings>();

        [SerializeField]
        bool m_EnableAdaptivePerformance = false;

        [SerializeField] string m_Version;
        private const string k_Version = "6000.3";

        internal string Version
        {
            get { return m_Version; }
            set
            {
                m_Version = value;
                EditorUtility.SetDirty(this);
            }
        }

        internal bool EnableAdaptivePerformance
        {
            get { return m_EnableAdaptivePerformance; }
            set
            {
                m_EnableAdaptivePerformance = value;
                EditorUtility.SetDirty(this);
            }
        }

        [RequiredByNativeCode(optional: true)]
        internal static void TryInitializeGeneralSettings()
        {
            AdaptivePerformanceGeneralSettingsPerBuildTarget generalSettings = null;
            EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out generalSettings);
            if (generalSettings != null && generalSettings.EnableAdaptivePerformance && AdaptivePerformanceGeneralSettings.Instance == null)
            {
                AdaptivePerformanceGeneralSettings.Instance = AdaptivePerformanceGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }
        }

        void OnEnable()
        {
            foreach (var setting in Settings.Values)
            {
                var assignedSettings = setting.AssignedSettings;
                if (assignedSettings == null)
                    continue;
                List<AdaptivePerformanceLoader> filteredLoaders = new List<AdaptivePerformanceLoader>();
                foreach (var loader in assignedSettings.loaders)
                {
                    if (loader != null)
                    {
                        filteredLoaders.Add(loader);
                    }
                }
                assignedSettings.loaders = filteredLoaders;
            }
            // If adaptive performance is disabled, make AdaptivePerformanceGeneralSettings.Instance = null;
            if (m_EnableAdaptivePerformance)
            {
                AdaptivePerformanceGeneralSettings.Instance = AdaptivePerformanceGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            }
            else
            {
                AdaptivePerformanceGeneralSettings.Instance = null;
            }
        }

        private void OnValidate()
        {
            if (m_Version != k_Version)
            {
                m_Version = k_Version;
                m_EnableAdaptivePerformance = true;
            }
        }

        internal static bool ContainsLoaderForAnyBuildTarget(string loaderTypeName)
        {
            AdaptivePerformanceGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
                return false;

            foreach (var settings in buildTargetSettings.Settings.Values)
            {
                if (AdaptivePerformancePackageMetadataStore.IsLoaderAssigned(settings.Manager, loaderTypeName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Set specific settings for a given build target.
        /// </summary>
        /// <param name="targetGroup">An enum that specifies which platform group this build is for.</param>
        /// <param name="settings">An instance of <see cref="AdaptivePerformanceGeneralSettings"/> to assign for the given key.</param>
        public void SetSettingsForBuildTarget(BuildTargetGroup targetGroup, AdaptivePerformanceGeneralSettings settings)
        {
            // Ensures the editor's "runtime instance" is the most current for standalone settings
            if (targetGroup == BuildTargetGroup.Standalone)
                AdaptivePerformanceGeneralSettings.Instance = settings;
            Settings[targetGroup] = settings;
        }

        /// <summary>Get specific settings for a given build target.</summary>
        /// <param name="targetGroup">An enum that specifies which platform group this build is for.</param>
        /// <returns>The instance of <see cref="AdaptivePerformanceGeneralSettings"/> assigned to the key, or null if no instance is assigned.</returns>
        public AdaptivePerformanceGeneralSettings SettingsForBuildTarget(BuildTargetGroup targetGroup)
        {
            AdaptivePerformanceGeneralSettings ret = null;
            Settings.TryGetValue(targetGroup, out ret);
            return ret;
        }

        /// <summary>Serialization override.</summary>
        public void OnBeforeSerialize()
        {
            Keys.Clear();
            Values.Clear();

            foreach (var kv in Settings)
            {
                Keys.Add(kv.Key);
                Values.Add(kv.Value);
            }
        }

        /// <summary>Serialization override.</summary>
        public void OnAfterDeserialize()
        {
            Settings = new Dictionary<BuildTargetGroup, AdaptivePerformanceGeneralSettings>();
            for (int i = 0; i < Math.Min(Keys.Count, Values.Count); i++)
            {
                Settings.Add(Keys[i], Values[i]);
            }
        }

        /// <summary>Given a build target, get the general settings container assigned to it.</summary>
        /// <param name="targetGroup">An enum that specifies which platform group this build is for.</param>
        /// <returns>The instance of <see cref="AdaptivePerformanceGeneralSettings"/> assigned to the key, or null if no instance is assigned.</returns>
        public static AdaptivePerformanceGeneralSettings AdaptivePerformanceGeneralSettingsForBuildTarget(BuildTargetGroup targetGroup)
        {
            AdaptivePerformanceGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
                return null;

            return buildTargetSettings.SettingsForBuildTarget(targetGroup);
        }
    }
}
