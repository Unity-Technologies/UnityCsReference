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
    static class PlayModeScenarioUtils
    {
        internal const string k_ConfigAssetsPath = "Assets/Settings/PlayMode";
        internal const int k_MaxScenarioConfigName = 64;

        /// <summary>
        /// Will get invoked when a ScenarioConfig is added or removed.
        /// </summary>
        internal static event Action AssetsChanged;

        private static List<PlayModeScenario> s_AllConfigs;

        [Obsolete("Use PlayModeScenarioManager.RegisterScenarioType<T> instead.", false)]
        internal static void RegisterPlayModeConfigurationType<T>(string label, string newItemName = "NewPlayModeScenario") where T : PlayModeScenario
            => PlayModeScenarioManager.RegisterScenarioType<T>(label, newItemName);

        internal static bool IsPlayModeConfigAsset(string assetPath)
        {
            return typeof(PlayModeScenario).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        struct ScenarioComparer : IComparer<PlayModeScenario>
        {
            public int Compare(PlayModeScenario x, PlayModeScenario y)
            {
                if (x == ScenarioManagerProvider.instance.DefaultScenarioInstance)
                    return -1;
                if (y == ScenarioManagerProvider.instance.DefaultScenarioInstance)
                    return 1;
                return EditorUtility.NaturalCompare(x.name, y.name);
            }
        }

        internal static List<PlayModeScenario> GetAllConfigs()
        {
            if (s_AllConfigs == null)
            {
                // We sort the scenarios by name, with the DefaultScenario always being first
                var scenarios = new SortedSet<PlayModeScenario>(new ScenarioComparer());
                scenarios.Add(ScenarioManagerProvider.instance.DefaultScenarioInstance);
                foreach (var scenario in EnumerateLoadedScenarios())
                    scenarios.Add(scenario);
                foreach (var scenario in EnumerateAssetScenarios())
                    scenarios.Add(scenario);

                s_AllConfigs = [.. scenarios];
            }

            return s_AllConfigs;
        }

        static IEnumerable<PlayModeScenario> EnumerateLoadedScenarios()
        {
            foreach (var obj in Resources.FindObjectsOfTypeAll<PlayModeScenario>())
            {
                if (obj == null || !PlayModeScenarioManager.IsScenarioTypeRegistered(obj.GetType()))
                    continue;

                yield return obj;
            }
        }

        static IEnumerable<PlayModeScenario> EnumerateAssetScenarios()
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(PlayModeScenario).FullName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = (PlayModeScenario)AssetDatabase.LoadAssetAtPath(path, typeof(PlayModeScenario));
                if (config == null || !PlayModeScenarioManager.IsScenarioTypeRegistered(config.GetType()))
                    continue;

                yield return config;
            }
        }

        internal static PlayModeScenario CreatePlayModeConfig(string name, Type type)
        {
            Assert.IsTrue(typeof(PlayModeScenario).IsAssignableFrom(type), $"Type '{type}' is not a PlayModeConfiguration");

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

            var config = ScriptableObject.CreateInstance(type) as PlayModeScenario;
            config.name = name;

            try
            {
                AssetDatabase.CreateAsset(config, $"{k_ConfigAssetsPath}/{config.name}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                ScriptableObject.DestroyImmediate(config);
                Debug.LogWarning($"Failed to create scenario '{name}'. The name may contain invalid characters. Error: {ex.Message}");
                return null;
            }

            return config;
        }

        /// <summary>
        /// Copies the given PlayModeConfiguration and increments the number at the end if necessary.
        /// Behaviour is the same as in ProjectBrowser.
        /// </summary>
        /// <param name="conf"></param>
        internal static void CopyPlayModeConfiguration(PlayModeScenario conf)
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
                if (importedAssets.Length > 0 || deletedAssets.Length > 0)
                {
                    ClearCache();
                    AssetsChanged?.Invoke();
                }
            }
        }
    }
}
