// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Build.Profile;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Assemblies;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal static class EditorUtilities
    {
        internal static readonly string[] s_DefaultGeneralSettingsPath = {"Adaptive Performance"};
        internal static readonly string[] s_DefaultLoaderPath = {"Adaptive Performance", "Provider"};
        internal static readonly string[] s_DefaultSettingsPath = {"Adaptive Performance", "Settings"};

        // SessionState keys used to suppress repeated prompts within a single editor session.
        // SessionState survives domain reloads but resets when the editor is restarted, so the
        // user is asked at most once per session per dialog.
        const string k_SessionKey_PromptedFrameTiming = "AdaptivePerformance.PromptedFrameTiming";
        const string k_SessionKey_PromptedThermalState = "AdaptivePerformance.PromptedThermalState";

        internal static bool AssetDatabaseHasInstanceOfType(string type)
        {
            var assets = AssetDatabase.FindAssets(String.Format("t:{0}", type));
            return assets != null && assets.Length > 0;
        }

        internal static T GetInstanceOfTypeFromAssetDatabase<T>() where T : class
        {
            var assets = AssetDatabase.FindAssets(String.Format("t:{0}", typeof(T).Name));
            if (assets != null && assets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
                return asset as T;
            }
            return null;
        }

        internal static ScriptableObject GetInstanceOfTypeWithNameFromAssetDatabase(string typeName)
        {
            var assets = AssetDatabase.FindAssets(String.Format("t:{0}", typeName));
            if (assets != null && assets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject));
                return asset as ScriptableObject;
            }
            return null;
        }

        internal static string GetAssetPathForComponents(string[] pathComponents, string root = "Assets")
        {
            if (pathComponents.Length <= 0)
                return null;

            string path = root;
            foreach (var pc in pathComponents)
            {
                string subFolder = Path.Combine(path, pc);
                bool shouldCreate = true;
                foreach (var f in AssetDatabase.GetSubFolders(path))
                {
                    if (String.Compare(Path.GetFullPath(f), Path.GetFullPath(subFolder), true) == 0)
                    {
                        shouldCreate = false;
                        break;
                    }
                }

                if (shouldCreate)
                    AssetDatabase.CreateFolder(path, pc);
                path = subFolder;
            }

            return path;
        }

        internal static string TypeNameToString(Type type)
        {
            return type == null ? "" : TypeNameToString(type.FullName);
        }

        internal static string TypeNameToString(string type)
        {
            string[] typeParts = type.Split(new char[] { '.' });
            if (typeParts == null || typeParts.Length  == 0 )
                return String.Empty;
            var matches =  Regex.Matches(typeParts[typeParts.Length - 1], "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)");
            List<string> wordList = new List<string>();
            foreach (object matchObj in matches) // MatchCollection elements are of type 'object'
            {
                if (matchObj is Match match)
                {
                    wordList.Add(match.Value);
                }
            }
            string[] words = wordList.ToArray();
            return string.Join(" ", words);
        }

        internal static ScriptableObject CreateScriptableObjectInstance(string typeName, string path)
        {
            ScriptableObject obj = ScriptableObject.CreateInstance(typeName) as ScriptableObject;
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = String.Format("{0}.asset", EditorUtilities.TypeNameToString(typeName));
                    string targetPath = Path.Combine(path, fileName);
                    obj.hideFlags = HideFlags.HideInInspector;
                    AssetDatabase.CreateAsset(obj, targetPath);
                    return obj;
                }
            }

            Debug.LogError($"We were unable to create an instance of the requested type {typeName}. Please make sure that all packages are updated to support this version of Adaptive Performance Provider Management. See the Unity documentation for Adaptive Performance Provider Management for information on resolving this issue.");
            return null;
        }

        internal static bool DialogForFrameTiming(BuildProfile profile, bool forcePrompt = false)
        {
            if (!Application.isBatchMode)
            {
                // Suppress the prompt if we have already asked the user this editor session,
                // regardless of whether they accepted or cancelled. Resets on editor restart.
                // Build-time callers pass forcePrompt: true so they always re-check, since the
                // build is the last opportunity to fix a misconfigured player setting.
                if (!forcePrompt && SessionState.GetBool(k_SessionKey_PromptedFrameTiming, false))
                    return false;

                bool Ok = false;
                bool prompted = false;
                try
                {
                    Ok = EditorUtility.DisplayDialog(L10n.Tr("Enable Frame Timing Stats "), L10n.Tr("Adaptive Performance requires Frame Timing Stats to be enabled. \"Ok\" to enable"),
                        L10n.Tr("Ok"), L10n.Tr("Cancel"));
                    prompted = true;
                }
                finally
                {
                    // Only mark the session bit when the dialog actually completed. If
                    // DisplayDialog threw, the user never made a decision, so leave the bit
                    // false so the next call re-prompts.
                    SessionState.SetBool(k_SessionKey_PromptedFrameTiming, prompted);
                }

                if (Ok)
                {
                    if (profile != null)
                    {
                        profile.playerSettings.SetEnableFrameTimingStats_Internal(true);
                    }
                    else
                    {
                        PlayerSettings.enableFrameTimingStats = true;
                    }
                    return true;
                }
            }
            else
            {
                Debug.Log("Frame timing manager is not enabled for Adaptive Performance in batch mode");
            }

            return false;
        }

        internal static bool DialogForThermalStats(BuildProfile profile, bool forcePrompt = false)
        {
            if (!Application.isBatchMode)
            {
                // Suppress the prompt if we have already asked the user this editor session,
                // regardless of whether they accepted or cancelled. Resets on editor restart.
                // Build-time callers pass forcePrompt: true so they always re-check, since the
                // build is the last opportunity to fix a misconfigured player setting.
                if (!forcePrompt && SessionState.GetBool(k_SessionKey_PromptedThermalState, false))
                    return true;

                bool Ok = false;
                bool prompted = false;
                try
                {
                    Ok = EditorUtility.DisplayDialog(L10n.Tr("Disable adjust IOS FPS Using Thermal State"), L10n.Tr("\"Adjust iOS FPS based on thermal state\" should be disabled in the Player Settings to ensure thermal mitigation works properly in the Adaptive Performance Apple Provider. Ok to disable"),
                        L10n.Tr("Ok"), L10n.Tr("Cancel"));
                    prompted = true;
                }
                finally
                {
                    // Only mark the session bit when the dialog actually completed. If
                    // DisplayDialog threw, the user never made a decision, so leave the bit
                    // false so the next call re-prompts.
                    SessionState.SetBool(k_SessionKey_PromptedThermalState, prompted);
                }

                if (Ok)
                {
                    if (profile != null)
                    {
                        profile.playerSettings.SetAdjustIOSFPSUsingThermalState_Internal(false);
                    }
                    else
                    {
                        PlayerSettings.adjustIOSFPSUsingThermalState = false;
                    }
                    return false;
                }
            }
            else
            {
                Debug.Log("Adjust IOS FPS Using Thermal State is not disabled for Adaptive Performance in batch mode");
            }

            return true;
        }

        internal static bool CheckEnableFrameTimingState(BuildProfile profile = null, bool forcePrompt = false)
        {
            if (profile != null && profile.playerSettings != null)
            {
                if (profile.playerSettings.GetEnableFrameTimingStats_Internal() == false)
                {
                    return DialogForFrameTiming(profile, forcePrompt);
                }
            }
            else if (PlayerSettings.enableFrameTimingStats == false)
            {
                return DialogForFrameTiming(null, forcePrompt);
            }

            return true;
        }

        internal static bool CheckEnableThermalStateForIOS(BuildProfile profile = null, bool forcePrompt = false)
        {
            // The "Adjust iOS FPS based on thermal state" setting only has effect on Apple
            // mobile platforms (iOS, tvOS, visionOS). Skip the check entirely on other targets
            // so users do not see an iOS-specific dialog when configuring Adaptive Performance
            // from the UI on a non-Apple-mobile build target or build profile.
            BuildTargetGroup targetGroup = profile != null
                ? BuildPipeline.GetBuildTargetGroup(profile.buildTarget)
                : EditorUserBuildSettings.selectedBuildTargetGroup;

            if (targetGroup != BuildTargetGroup.iOS &&
                targetGroup != BuildTargetGroup.tvOS &&
                targetGroup != BuildTargetGroup.VisionOS)
            {
                return false;
            }

            if (profile != null && profile.playerSettings != null)
            {
                if (profile.playerSettings.GetAdjustIOSFPSUsingThermalState_Internal() == true)
                {
                    return DialogForThermalStats(profile, forcePrompt);
                }
            }
            else if (PlayerSettings.adjustIOSFPSUsingThermalState == true)
            {
                return DialogForThermalStats(null, forcePrompt);
            }

            return false;
        }

        internal static void AddToPreloadedAssetList(UnityEngine.Object settings)
        {
            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
            foreach (UnityEngine.Object asset in preloadedAssets)
            {
                assets.Add(asset);
            }

            if (!assets.Contains(settings))
            {
                assets.Add(settings);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        internal static IAdaptivePerformanceSettings CloneProviderSettingsFromProjectSettings(string settingType)
        {
            string assetDir = "Assets/";
            assetDir = Path.Combine(assetDir, Path.Combine(s_DefaultSettingsPath));

            if(!AssetDatabase.IsValidFolder(assetDir))
            {
                return null;
            }

            var assetsIDs = AssetDatabase.FindAssets("",new[] {assetDir});

            IAdaptivePerformanceSettings copyObject = null;
            foreach (var guid in assetsIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IAdaptivePerformanceSettings;
                if (obj != null && obj.GetType().ToString() == settingType)
                {
                    copyObject = obj;
                }
            }

            return copyObject == null? null : ScriptableObject.Instantiate(copyObject);
        }

        internal static List<AdaptivePerformanceScaler> CloneCustomScalersFromProjectSettings(IAdaptivePerformanceSettings settings)
        {
            var copyObjects = new List<AdaptivePerformanceScaler>();
            for (int i = 0; i < settings.ScalerProfiles.Length; i++)
            {
                for (int j = 0; j < settings.ScalerProfiles[i].AddedScalers.Count; j++)
                {
                    if (settings.ScalerProfiles[i].AddedScalers[j] == null) continue;
                    var clone = ScriptableObject.Instantiate(settings.ScalerProfiles[i].AddedScalers[j]);
                    clone.hideFlags = HideFlags.HideInHierarchy;
                    settings.ScalerProfiles[i].AddedScalers[j] = clone;
                    copyObjects.Add(clone);
                }
            }
            return copyObjects;
        }

        internal static bool AddCustomScalerToProviderSetting(IAdaptivePerformanceSettings providerSettings)
        {
            Type ti = typeof(AdaptivePerformanceScaler);
            List<AdaptivePerformanceScaler> addedScalers = new List<AdaptivePerformanceScaler>();
            foreach (Assembly asm in CurrentAssemblies.GetLoadedAssemblies())
            {
                if (asm.GetName().Name == "UnityEngine.AdaptivePerformanceModule")
                {
                    continue;
                }

                Type[] types;
                try
                {
                #pragma warning disable UA2001 // Only for editor use.
                    types = asm.GetTypes();
                #pragma warning restore UA2001
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"Unable to scan assembly {asm.FullName} for Adaptive Performance scalers. {ex.Message}");
                    continue;
                }

                foreach (Type t in types)
                {
                    if (t == null)
                        continue;

                    if (ti.IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        addedScalers.Add((AdaptivePerformanceScaler)ScriptableObject.CreateInstance(t));
                    }
                }
            }

            if (addedScalers.Count == 0) return false;
            for (int j = 0; j < providerSettings.ScalerProfiles.Length; j++)
            {
                // Only assign the scanned scalers when no scaler is assigned via UI. Mixing them is a bad idea.
                // Since scripting settings will take control in this use case, all profiles will essentially own the same scaler instance.
                // Only the settings will be swapped like in the default scaler case.
                if (providerSettings.ScalerProfiles[j].AddedScalers.Count != 0)
                {
                    return false;
                }
            }

            if (providerSettings.AddedScalerViaScan != null && providerSettings.AddedScalerViaScan.Count > 0)
            {
                for (int i = 0; i < providerSettings.AddedScalerViaScan.Count; i++)
                {
                    var existingScaler = providerSettings.AddedScalerViaScan[i];
                    if (existingScaler == null)
                    {
                        continue;
                    }

                    AssetDatabase.RemoveObjectFromAsset(existingScaler);
                    ScriptableObject.DestroyImmediate(existingScaler, true);
                }

                providerSettings.AddedScalerViaScan.Clear();
            }

            providerSettings.AddedScalerViaScan = addedScalers;

            for (int i = 0; i < addedScalers.Count; i++)
            {
                addedScalers[i].hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(addedScalers[i], providerSettings);
            }

            EditorUtility.SetDirty(providerSettings);
            AssetDatabase.SaveAssetIfDirty(providerSettings);
            return true;
        }

        internal static AdaptivePerformanceGeneralSettings GetSettingsOrBuildProfilesSettings(BuildTargetGroup targetGroup = BuildTargetGroup.Unknown)
        {
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            AdaptivePerformanceGeneralSettings settings = null;

            if (activeProfile != null && activeProfile.platformBuildProfile?.adaptivePerformanceEnabled == true)
            {
                settings = activeProfile.GetComponent<AdaptivePerformanceGeneralSettings>();
            }

            if (settings == null)
            {
                AdaptivePerformanceGeneralSettingsPerBuildTarget buildTargetSettings = null;
                EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey,
                    out buildTargetSettings);
                if (buildTargetSettings == null || buildTargetSettings.EnableAdaptivePerformance == false)
                    return settings;

                settings = buildTargetSettings.SettingsForBuildTarget(targetGroup == BuildTargetGroup.Unknown ? AdaptivePerformanceSettingsManager.Instance.m_LastBuildTargetGroup : targetGroup);
            }
            return settings;
        }

        internal static void EnableAPModule(bool enable)
        {
            var packageID = "com.unity.modules.adaptiveperformance";

            if (enable)
            {
                Client.Add(packageID);
            }
            else
            {
                var settings = EditorUtilities.GetSettingsOrBuildProfilesSettings();
                if (settings == null)
                    Client.Remove(packageID); // neither classic platforms (Settings) nor build profiles are enabled
            }
        }
    }
}
