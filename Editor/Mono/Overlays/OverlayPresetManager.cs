// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Overlays
{
    // This is responsible for loading and saving OverlayPresets. It does not serialize presets in the manager, but
    // rather loads and saves from the various locations that presets can exist.
    sealed class OverlayPresetManager : ScriptableSingleton<OverlayPresetManager>
    {
        static Dictionary<Type, Dictionary<string, OverlayPreset>> loadedPresets => instance.m_Presets;

        [NonSerialized]
        Dictionary<Type, Dictionary<string, OverlayPreset>> m_Presets;

        const string k_FileExtension = "overlay";
        const string k_PresetAssetsName = "OverlayPresets.asset";
        static string k_PreferencesAssetPath => Path.Combine(InternalEditorUtility.unityPreferencesFolder, "OverlayPresets/" + k_PresetAssetsName);
        static string k_ResourcesAssetPath => Path.Combine(EditorApplication.applicationContentsPath, "Resources/OverlayPresets/" + k_PresetAssetsName);
        static string preferencesPath => FileUtil.CombinePaths(InternalEditorUtility.unityPreferencesFolder, "OverlayPresets");

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanUpPresets;
            ReloadAllPresets();
        }

        internal static void SaveOverlayStateToFile(string path, EditorWindow window)
        {
            var preset = CreateInstance<OverlayPreset>();
            preset.name = Path.GetFileNameWithoutExtension(path);
            preset.targetWindowType = window.GetType();
            window.overlayCanvas.CopySaveData(out var saveData);
            preset.saveData = saveData;

            try
            {
                SaveToFile(new List<OverlayPreset> {preset}, path);
            }
            finally
            {
                DestroyImmediate(preset);
            }
        }

        internal static OverlayPreset CreatePresetFromOverlayState(string presetName, EditorWindow window)
        {
            var windowType = window.GetType();
            if (!TryGetPreset(windowType, presetName, out var preset))
            {
                preset = CreateInstance<OverlayPreset>();
                preset.name = presetName;
                preset.targetWindowType = windowType;

                AddPreset(preset);
            }

            window.overlayCanvas.CopySaveData(out var data);
            preset.saveData = data;
            SaveAllPreferences();

            return preset;
        }

        static void AddPreset(OverlayPreset preset)
        {
            if (!loadedPresets.TryGetValue(preset.targetWindowType, out var presets))
                loadedPresets.Add(preset.targetWindowType, presets = new Dictionary<string, OverlayPreset>());

            if (!presets.ContainsKey(preset.name))
                presets.Add(preset.name, preset);
            else
                presets[preset.name] = preset;
        }

        // used by tests
        internal static void RevertPreferencesPresetsToDefault()
        {
            if (File.Exists(k_PreferencesAssetPath))
                FileUtil.DeleteFileOrDirectory(k_PreferencesAssetPath);
            FileUtil.CopyFileOrDirectory(k_ResourcesAssetPath, k_PreferencesAssetPath);
        }

        static void SaveAllPreferences()
        {
            List<OverlayPreset> presets = new List<OverlayPreset>();
            foreach (var presetsForWindow in loadedPresets)
            {
                foreach (var preset in presetsForWindow.Value)
                {
                    presets.Add(preset.Value);
                }
            }
            SaveToFile(presets, k_PreferencesAssetPath);
        }

        internal static void DeletePreset(OverlayPreset preset)
        {
            if (preset != null && preset.targetWindowType != null
                && loadedPresets.TryGetValue(preset.targetWindowType, out var presets)
                && presets.Remove(preset.name))
            {
                DestroyImmediate(preset);
                SaveAllPreferences();
            }
        }

        internal static bool TryGetPreset(Type windowType, string presetName, out OverlayPreset preset)
        {
            var currentType = windowType;
            while (currentType != null)
            {
                if (loadedPresets.TryGetValue(currentType, out var p))
                    if (p.TryGetValue(presetName, out preset))
                        return true;

                currentType = currentType.BaseType;
            }
            foreach (var i in windowType.GetInterfaces())
            {
                if (loadedPresets.TryGetValue(i, out var p))
                    if (p.TryGetValue(presetName, out preset))
                        return true;
            }

            preset = null;
            return false;
        }

        public static bool Exists(Type windowType, string presetName)
        {
            return TryGetPreset(windowType, presetName, out _);
        }

        public static OverlayPreset GetDefaultPreset(Type windowType)
        {
            var presets = GetAllPresets(windowType);
            return presets?.FirstOrDefault();
        }

        internal static IEnumerable<OverlayPreset> GetAllPresets(Type windowType)
        {
            List<OverlayPreset> presets = new List<OverlayPreset>();
            foreach (var i in windowType.GetInterfaces())
            {
                if (loadedPresets.TryGetValue(i, out var p))
                    presets.AddRange(p.Values);
            }

            while (windowType != null && typeof(EditorWindow).IsAssignableFrom(windowType))
            {
                if (loadedPresets.TryGetValue(windowType, out var p))
                    presets.AddRange(p.Values);

                windowType = windowType.BaseType;
            }

            return presets;
        }

        internal static void ReloadAllPresets()
        {
            CleanUpPresets();
            instance.m_Presets = LoadAllPresets();
        }

        static void CleanUpPresets()
        {
            // Ensure that no zombie overlay presets remains
            foreach (var preset in Resources.FindObjectsOfTypeAll<OverlayPreset>())
            {
                if (!EditorUtility.IsPersistent(preset))
                    DestroyImmediate(preset);
            }
        }

        static Dictionary<Type, Dictionary<string, OverlayPreset>> LoadAllPresets()
        {
            var results = new Dictionary<Type, Dictionary<string, OverlayPreset>>();

            if (!Directory.Exists(preferencesPath))
                Directory.CreateDirectory(preferencesPath);
            if (!File.Exists(k_PreferencesAssetPath))
                RevertPreferencesPresetsToDefault();

            List<Object> loaded = new List<Object>(64);

            // load preference based presets
            var builtin = InternalEditorUtility.LoadSerializedFileAndForget(k_PreferencesAssetPath);

            // this is necessary for users who tried out overlays preview builds. the registered class ID changed
            // from 13987 to 13988 during development. we correct that case here. can remove this check in 2022.
            if (builtin.Length < 1)
            {
                RevertPreferencesPresetsToDefault();
                builtin = InternalEditorUtility.LoadSerializedFileAndForget(k_PreferencesAssetPath);
            }

            loaded.AddRange(builtin);

            foreach (var rawPreset in loaded)
            {
                var preset = rawPreset as OverlayPreset;

                if (preset != null && preset.targetWindowType != null)
                {
                    if (!results.TryGetValue(preset.targetWindowType, out var presets))
                    {
                        presets = new Dictionary<string, OverlayPreset>();
                        results.Add(preset.targetWindowType, presets);
                    }

                    if (presets.ContainsKey(preset.name))
                    {
                        Debug.LogWarning($"Failed to load the overlay preset with name {preset.name}. A preset with that name already existed in that window type.");
                        continue;
                    }
                    presets.Add(preset.name, preset);
                }
            }

            return results;
        }

        internal static void SaveToFile(IList<OverlayPreset> presets, string path)
        {
            var parentLayoutFolder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(parentLayoutFolder))
            {
                if (!Directory.Exists(parentLayoutFolder))
                    Directory.CreateDirectory(parentLayoutFolder);
                InternalEditorUtility.SaveToSerializedFileAndForget(presets.Cast<Object>().ToArray(), path, true);
            }
        }

        internal static OverlayPreset LoadFromFile(string path)
        {
            if (Path.GetExtension(path) != "." + k_FileExtension)
            {
                Debug.LogFormat(L10n.Tr("Overlay preset files must have the {0} extension to be valid."), k_FileExtension);
                return null;
            }
            var rawPresets = InternalEditorUtility.LoadSerializedFileAndForget(path);
            if (rawPresets != null)
            {
                //Get the first preset in the file. .overlay files should only ever contained one
                foreach (var rawPreset in rawPresets)
                {
                    if (rawPreset is OverlayPreset preset)
                    {
                        preset.name = Path.GetFileNameWithoutExtension(path);
                        return preset;
                    }
                }
            }

            return null;
        }

        public static void GenerateMenu(GenericMenu menu, string pathPrefix, EditorWindow window)
        {
            var presets = GetAllPresets(window.GetType());
            foreach (var preset in presets)
            {
                menu.AddItem(new GUIContent(pathPrefix + preset.name), false, () =>
                {
                    window.overlayCanvas.ApplyPreset(preset);
                });
            }

            menu.AddSeparator(pathPrefix);

            menu.AddItem(EditorGUIUtility.TrTextContent($"{pathPrefix}Save Preset..."), false, () =>
            {
                SaveOverlayPreset.ShowWindow(window, name =>
                {
                    var preset = CreatePresetFromOverlayState(name, window);
                    SaveAllPreferences();
                    window.overlayCanvas.ApplyPreset(preset);
                });
            });

            menu.AddItem(EditorGUIUtility.TrTextContent($"{pathPrefix}Save Preset To File..."), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Save window preset to disk...", "", "NewOverlayPreset", k_FileExtension);
                if (!string.IsNullOrEmpty(path))
                {
                    SaveOverlayStateToFile(path, window);
                    EditorUtility.RevealInFinder(path);
                }
            });

            menu.AddItem(EditorGUIUtility.TrTextContent($"{pathPrefix}Load Preset From File..."), false, () =>
            {
                var filePath = EditorUtility.OpenFilePanel("Load preset from disk...", "", k_FileExtension);
                if (!string.IsNullOrEmpty(filePath))
                {
                    var preset = LoadFromFile(filePath);
                    bool failed = false;
                    if (preset == null)
                    {
                        EditorUtility.DisplayDialog(
                            L10n.Tr("Load Overlay Preset From Disk"),
                            string.Format(L10n.Tr("Failed to load the chosen preset, the file may not be a .{0} or was corrupted."), k_FileExtension),
                            L10n.Tr("Ok"));
                        failed = true;
                    }
                    else if (!preset.CanApplyToWindow(window.GetType()))
                    {
                        EditorUtility.DisplayDialog(
                            L10n.Tr("Load Overlay Preset From Disk"),
                            string.Format(L10n.Tr("Trying to load an overlay preset with the name {0}. This preset targets the window type {0} which isn't valid for this window."), preset.targetWindowType),
                            L10n.Tr("Ok"));
                        failed = true;
                    }

                    if (Exists(preset.targetWindowType, preset.name))
                    {
                        if (!EditorUtility.DisplayDialog(
                            L10n.Tr("Load Overlay Preset From Disk"),
                            string.Format(L10n.Tr("Trying to load an overlay preset with the name {0}. This name is already in used in the window, do you want to overwrite it?"), preset.name),
                            L10n.Tr("Yes"), L10n.Tr("No")))
                        {
                            failed = true;
                        }
                    }

                    if (failed)
                    {
                        if (!EditorUtility.IsPersistent(preset))
                            DestroyImmediate(preset);
                        return;
                    }

                    AddPreset(preset);
                    window.overlayCanvas.ApplyPreset(preset);
                }
            });

            foreach (var preset in presets)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent($"{pathPrefix}Delete Preset/{preset.name}"), false, () =>
                {
                    DeletePreset(preset);
                    window.overlayCanvas.SetLastAppliedPresetName(OverlayCanvas.k_DefaultPresetName);
                    window.overlayCanvas.afterOverlaysInitialized.Invoke();
                });
            }

            menu.AddItem(EditorGUIUtility.TrTextContent($"{pathPrefix}Revert All Saved Presets"), false, () =>
            {
                if (EditorUtility.DisplayDialog(
                    L10n.Tr("Revert All Saved Presets"),
                    L10n.Tr("Unity is about to delete all overlay presets that are not loaded from files in project and restore default settings."),
                    L10n.Tr("Continue"), L10n.Tr("Cancel")))
                {
                    RevertPreferencesPresetsToDefault();
                    ReloadAllPresets();
                    window.overlayCanvas.ApplyPreset(GetDefaultPreset(window.GetType()));
                }
            });
        }
    }
}
