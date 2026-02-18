// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEngine;

namespace Unity.PlayMode.Editor
{
    static class PlayModeConfigurationUtils
    {
        internal struct PlayModeConfigurationTypeData
        {
            public Type ConfigurationType;
            public string Label;
            public string NewItemName;
        }

        internal const string k_ConfigAssetsPath = "Assets/Settings/PlayMode";
        internal const int k_MaxScenarioConfigName = 64;

        /// <summary>
        /// Will get invoked when a ScenarioConfig is added or removed.
        /// </summary>
        internal static event Action ConfigurationAddedOrRemoved;

        private static Dictionary<Type, PlayModeConfigurationTypeData> s_ConfigurationTypes = new();
        private static List<PlayModeConfiguration> s_AllConfigs;

        internal static void RegisterPlayModeConfigurationType<T>(string label, string newItemName = "NewPlayModeConfiguration") where T : PlayModeConfiguration
        {
            if (typeof(T).IsAbstract)
            {
                Debug.LogError($"Type '{typeof(T)}' is abstract. Only concrete types are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return;
            }

            s_ConfigurationTypes[typeof(T)] = new PlayModeConfigurationTypeData
                {
                    ConfigurationType = typeof(T),
                    Label = label,
                    NewItemName = newItemName
                };

            PlayModeButtonsExtension.Initialize(); // Refresh the buttons if needed
        }
        internal static IEnumerable<PlayModeConfigurationTypeData> GetPlayModeConfigurationTypes() => s_ConfigurationTypes.Values;
        internal static int ConfigurationTypesCount => s_ConfigurationTypes.Count;

        internal static bool IsPlayModeConfigAsset(string assetPath)
        {
            return typeof(PlayModeConfiguration).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        internal static List<PlayModeConfiguration> GetAllConfigs()
        {
            if (s_AllConfigs == null)
            {
                s_AllConfigs = new List<PlayModeConfiguration>();
                var guids = AssetDatabase.FindAssets($"t:{typeof(PlayModeConfiguration).FullName}");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var config = (PlayModeConfiguration)AssetDatabase.LoadAssetAtPath(path, typeof(PlayModeConfiguration));
                    if (config == null)
                        continue;
                    s_AllConfigs.Add(config);
                }

                s_AllConfigs.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            }

            return s_AllConfigs;
        }

        internal static PlayModeConfiguration CreatePlayModeConfig(string name, Type type)
        {
            Assert.IsTrue(typeof(PlayModeConfiguration).IsAssignableFrom(type), $"Type '{type}' is not a PlayModeConfiguration");

            if (!Directory.Exists(k_ConfigAssetsPath))
                Directory.CreateDirectory(k_ConfigAssetsPath);

            if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0)
            {
                Debug.LogWarning("Scenario names cannot contain slashes.");
                return null;
            }
            // If the scenario name exceeds the limit, trim it to k_MaxScenarioConfigName length and log a warning in the console
            if (name.Length > k_MaxScenarioConfigName)
            {
                Debug.LogWarning($"Scenario name '{name}' is too long and has been trimmed to {k_MaxScenarioConfigName} characters.");
                name = name.Substring(0, k_MaxScenarioConfigName) + "...";
            }

            var config = ScriptableObject.CreateInstance(type) as PlayModeConfiguration;
            config.name = name;

            AssetDatabase.CreateAsset(config, $"{k_ConfigAssetsPath}/{config.name}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return config;
        }

        /// <summary>
        /// Copies the given PlayModeConfiguration and increments the number at the end if necessary.
        /// Behaviour is the same as in ProjectBrowser.
        /// </summary>
        /// <param name="conf"></param>
        internal static void CopyPlayModeConfiguration(PlayModeConfiguration conf)
        {
            var path = AssetDatabase.GetAssetPath(conf);
            var last = path.Split(' ')[^1];
            var withoutExtension = last.Split('.')[0];

            // True if the path already has a space followed with a number
            // for example "MyConfig 1.asset"
            var pathAlreadyHasCounter = int.TryParse(withoutExtension, out var counter);

            // make sure we have at least 1
            counter = Math.Max(counter, 1);

            var pathIsValid = false;
            while (pathIsValid == false)
            {
                var newPath = path.Replace(".asset", $" {counter}.asset");
                if (pathAlreadyHasCounter)
                    newPath = path.Replace(last, $"{counter}.asset");

                pathIsValid = !AssetDatabase.AssetPathExists(newPath);
                if (pathIsValid)
                {
                    AssetDatabase.CopyAsset(path, newPath);
                }
                counter++;
            }
        }

        private static void ClearCache()
        {
            s_AllConfigs = null;
        }

        /// <summary>
        /// Tracks changes to ScenarioConfigs in the project.
        /// </summary>
        private class ConfigAssetsTracker : AssetPostprocessor
        {
            public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var needsUpdate = false;

                foreach (var changedAsset in importedAssets)
                {
                    if (IsPlayModeConfigAsset(changedAsset))
                    {
                        needsUpdate = true;
                        break;
                    }
                }

                // If something got deleted we have to refresh, because we cannot check
                // if the deleted asset was a ScenarioConfig.
                if (deletedAssets.Length > 0)
                    needsUpdate = true;

                if (needsUpdate)
                {
                    ClearCache();
                    ConfigurationAddedOrRemoved?.Invoke();
                }
            }
        }
    }
}
