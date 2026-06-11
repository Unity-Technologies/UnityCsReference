// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Toolbars;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Overlays
{
    interface IOverlayPreset
    {
        SaveData[] saveData { get; }
        DynamicPanelContainerData[] dynamicPanelContainerData { get; }
        Type targetWindowType { get; }
        bool CanApplyToWindow(Type windowType);
        void ApplyCustomData(OverlayCanvas canvas);
        string name { get; }
    }

    sealed class DefaultOverlayPreset : IOverlayPreset
    {
        public SaveData[] saveData => Array.Empty<SaveData>();
        public DynamicPanelContainerData[] dynamicPanelContainerData => Array.Empty<DynamicPanelContainerData>();
        public Type targetWindowType => null;

        public bool CanApplyToWindow(Type windowType) => true;

        public void ApplyCustomData(OverlayCanvas canvas)
        {
            // We set these package overlays manually for backward compatibility now that we don't have a Default save file
            if (canvas.TryGetOverlay("Scene View/Navmesh Display", out var overlay)) overlay.displayed = false;
            if (canvas.TryGetOverlay("Scene View/Agent Display", out overlay)) overlay.displayed = false;
            if (canvas.TryGetOverlay("Scene View/Obstacle Display", out overlay)) overlay.displayed = false;
            if (canvas.TryGetOverlay("Scene View/Occlusion Culling", out overlay)) overlay.displayed = false;
        }

        public string name => OverlayPresetManager.defaultPresetName;
    }

    // This is responsible for loading and saving OverlayPresets. It does not serialize presets in the manager, but
    // rather loads and saves from the various locations that presets can exist.
    sealed class OverlayPresetManager : ScriptableSingleton<OverlayPresetManager>
    {
        static Dictionary<Type, Dictionary<string, OverlayPreset>> loadedPresets => instance.m_Presets;

        [NonSerialized]
        Dictionary<Type, Dictionary<string, OverlayPreset>> m_Presets;

        readonly static string[] k_ReservedNames =
        {
            UnityOnlyToolbarPreset.presetName
        };

        internal const string k_OverlayPresetExtension = "overlay";
        internal const string k_ToolbarPresetExtension = "preset";
        const string k_PresetAssetsName = "OverlayPresets.asset";
        internal const string defaultPresetName = "Default";
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
            var name = Path.GetFileNameWithoutExtension(path);
            if (!ValidatePresetName(name))
                return;

            var preset = CreateInstance<OverlayPreset>();
            preset.name = name;
            preset.targetWindowType = window.GetType();
            window.overlayCanvas.SavePreset(preset);

            try
            {
                SaveToFile(new List<OverlayPreset> {preset}, path);
            }
            finally
            {
                DestroyImmediate(preset);
            }
        }

        static bool IsReservedName(string presetName)
        {
            foreach (var reserved in k_ReservedNames)
                if (presetName.Equals(reserved, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        static bool ValidatePresetName(string presetName)
        {
            if (IsReservedName(presetName))
            {
                EditorUtility.DisplayDialog(L10n.Tr("Invalid Preset Name"), string.Format(L10n.Tr("Trying to create a preset with the reserved name [{0}]."), presetName), L10n.Tr("Ok"));
                return false;
            }

            return true;
        }

        internal static OverlayPreset CreatePresetFromOverlayState(string presetName, EditorWindow window, bool validateName = false)
        {
            if (validateName && !ValidatePresetName(presetName))
                return null;

            var windowType = window.GetType();
            if (!TryGetPreset(windowType, presetName, out var preset))
            {
                preset = CreateInstance<OverlayPreset>();
                preset.name = presetName;
                preset.targetWindowType = windowType;

                AddPreset(preset);
            }

            window.overlayCanvas.SavePreset(preset);
            SaveAllPreferences();

            return preset;
        }

        static void AddPreset(OverlayPreset preset)
        {
            EnsureNameWithinCharacterLimit(preset);

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

        public static IOverlayPreset GetDefaultPreset(Type windowType)
        {
            if (TryGetPreset(windowType, defaultPresetName, out OverlayPreset preset))
                return preset;

            return new DefaultOverlayPreset();
        }

        internal static List<IOverlayPreset> GetAllPresets(Type windowType, bool includeDefault = true)
        {
            List<IOverlayPreset> presets = new List<IOverlayPreset>();

            // Add a default preset if the user hasn't overwritten it
            if (includeDefault && !TryGetPreset(windowType, defaultPresetName, out OverlayPreset preset))
                presets.Add(new DefaultOverlayPreset());

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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                InternalEditorUtility.SaveToSerializedFileAndForget(presets.Cast<Object>().ToArray(), path, true);
#pragma warning restore UA2001
            }
        }

        internal const string presetOverCharLimitWarning = "The preset {0} was over the character limit of {1} and was truncated.";
        internal static void EnsureNameWithinCharacterLimit(OverlayPreset preset)
        {
            if (preset.name.Length <= SavePromptUtility.nameCharacterLimit)
                return;

            Debug.LogWarningFormat(presetOverCharLimitWarning, preset.name, SavePromptUtility.nameCharacterLimit);
            preset.name = preset.name.Substring(0, SavePromptUtility.nameCharacterLimit);
        }

        internal static OverlayPreset LoadFromFile(string path, string presetTypeExtension)
        {
            if (Path.GetExtension(path) != "." + presetTypeExtension)
            {
                var presetTypeName = presetTypeExtension.Equals(k_OverlayPresetExtension) ? "Overlay" : "Toolbar";
                Debug.LogFormat(L10n.Tr("{0} preset files must have the {1} extension to be valid."), presetTypeName, presetTypeExtension);
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

        static string CanCreatePreset(string name)
        {
            return SavePromptUtility.GetSaveError("Preset", name, (name) =>
            {
                if (IsReservedName(name))
                {
                    return string.Format(L10n.Tr("Preset Name is reserved"), name);
                }

                return null;
            });
        }

        static void ShowSavePresetWindow(EditorWindow window, Action<OverlayPreset> onCreated, Action onFailed = null)
        {
            PromptWindow.Show(L10n.Tr("Create Preset"),
                L10n.Tr("Create a preset"),
                L10n.Tr("Enter the name of the preset you want to create"),
                L10n.Tr("Preset Name"),
                window.overlayCanvas.lastAppliedPresetName,
                L10n.Tr("Create"),
                window,
                CanCreatePreset,
                (name) =>
                {
                    if (Exists(window.GetType(), name) &&
                        !EditorUtility.DisplayDialog(
                            L10n.Tr("Overwrite Preset?"),
                            string.Format(L10n.Tr("Do you want to overwrite '{0}' preset?"), name),
                            L10n.Tr("Overwrite"), L10n.Tr("Cancel")))
                    {
                        onFailed?.Invoke();
                        return;
                    }

                    var preset = CreatePresetFromOverlayState(name, window, true);
                    if (preset != null)
                    {
                        SaveAllPreferences();
                        onCreated?.Invoke(preset);
                    }
                    else
                    {
                        onFailed?.Invoke();
                    }
                },
                windowWidth:400f);
        }

        static void ApplyPreset(OverlayCanvas canvas, IOverlayPreset preset, Func<OverlayCanvas, bool> canvasChangedCheck = null)
        {
            CheckUnsavedChanges(canvas, canvasChangedCheck, () => canvas.ApplyPreset(preset));
        }

        static void CheckUnsavedChanges(OverlayCanvas canvas, Func<OverlayCanvas, bool> canvasChangeCheck, Action onContinue)
        {
            if (canvasChangeCheck == null || !canvasChangeCheck.Invoke(canvas))
            {
                onContinue?.Invoke();
                return;
            }

            var result = EditorDialog.DisplayComplexDecisionDialogWithOptOut(
                    L10n.Tr("Unsaved Changes"),
                    L10n.Tr("Your current toolbar preset has unsaved changes that will be overriden by your current action."),
                    L10n.Tr("Save changes..."),
                    L10n.Tr("Continue without saving"),
                    L10n.Tr("Cancel"),
                    DialogOptOutDecisionType.ForThisUser,
                    "overlays.presetDirtyWarningOptOut");

            switch (result)
            {
                // Save Changes
                case DialogResult.DefaultAction:
                    ShowSavePresetWindow(canvas.containerWindow, (name) => onContinue?.Invoke());
                    break;

                // Cancel
                case DialogResult.Cancel:
                    return;

                // Continue Unsaved
                case DialogResult.AlternateAction:
                    onContinue?.Invoke();
                    break;
            }
        }

        public static void GenerateMenu(AbstractGenericMenu menu, string pathPrefix, EditorWindow window, bool includeDefaultPreset, Func<OverlayCanvas, bool> canvasChangeCheck, params IOverlayPreset[] customPresets)
        {
            var presets = GetAllPresets(window.GetType(), includeDefaultPreset);
            var isToolbar = window is MainToolbarWindow;
            var overlayTargetType = isToolbar ? "Toolbar" : "Overlay";
            var presetExtension = isToolbar ? k_ToolbarPresetExtension : k_OverlayPresetExtension;

            foreach (var customPreset in customPresets)
            {
                // Ensure we remove custom presets if a user defined one has the same name already
                if (IsReservedName(customPreset.name) || presets.Find((preset) => preset.name == customPreset.name) == null)
                {
                    menu.AddItem(pathPrefix + customPreset.name, window.overlayCanvas.lastAppliedPresetName == customPreset.name, () =>
                    {
                        ApplyPreset(window.overlayCanvas, customPreset, canvasChangeCheck);
                    });
                }
            }

            foreach (var preset in presets)
            {
                if (IsReservedName(preset.name))
                    continue;

                menu.AddItem(pathPrefix + preset.name, window.overlayCanvas.lastAppliedPresetName == preset.name, () =>
                {
                    ApplyPreset(window.overlayCanvas, preset, canvasChangeCheck);
                });
            }

            menu.AddSeparator(pathPrefix);

            menu.AddItem(L10n.Tr($"{pathPrefix}Save Preset..."), false, () =>
            {
                ShowSavePresetWindow(window, preset =>
                {
                    window.overlayCanvas.ApplyPreset(preset);
                });
            });

            menu.AddItem(L10n.Tr($"{pathPrefix}Save Preset To File..."), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Save window preset to disk...", "", $"New{overlayTargetType}Preset", presetExtension);
                if (!string.IsNullOrEmpty(path))
                {
                    SaveOverlayStateToFile(path, window);
                    EditorUtility.RevealInFinder(path);
                }
            });

            menu.AddItem(L10n.Tr($"{pathPrefix}Load Preset From File..."), false, () =>
            {
                CheckUnsavedChanges(window.overlayCanvas, canvasChangeCheck, () =>
                {
                    var filePath = EditorUtility.OpenFilePanel("Load preset from disk...", "", presetExtension);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var preset = LoadFromFile(filePath, presetExtension);
                        bool failed = false;
                        if (preset == null)
                        {
                            EditorUtility.DisplayDialog(
                                L10n.Tr($"Load {overlayTargetType} Preset From Disk"),
                                string.Format(L10n.Tr("Failed to load the chosen preset. The file may not be a .{0} or it was corrupted."), presetExtension),
                                L10n.Tr("OK"));
                            failed = true;
                        }
                        else if (!preset.CanApplyToWindow(window.GetType()))
                        {
                            EditorUtility.DisplayDialog(
                                L10n.Tr($"Load {overlayTargetType} Preset From Disk"),
                                string.Format(L10n.Tr("Trying to load an {0} preset with the name {1}. This preset targets the window type {1}, which isn't valid for {2} window."), 
                                    overlayTargetType.ToLower(), preset.targetWindowType, window.GetType()),
                                L10n.Tr("OK"));
                            failed = true;
                        }

                        if (!failed && !ValidatePresetName(preset.name))
                        {
                            failed = true;
                        }

                        if (!failed && Exists(preset.targetWindowType, preset.name))
                        {
                            if (!EditorUtility.DisplayDialog(
                                L10n.Tr($"Load {overlayTargetType} Preset From Disk"),
                                string.Format(L10n.Tr("Trying to load an {0} preset with the name {1}. This name is already in use in the window. Do you want to overwrite it?"), overlayTargetType.ToLower(), preset.name),
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
                        ApplyPreset(window.overlayCanvas, preset);
                    }
                });
            });

            foreach (var rawPreset in presets)
            {
                // Only add the ability to delete asset presets and not the ones created from code
                if (rawPreset is OverlayPreset preset)
                {
                    menu.AddItem(L10n.Tr($"{pathPrefix}Delete Preset/{preset.name}"), false, () =>
                    {
	                    DeletePreset(preset);
	                    window.overlayCanvas.SetLastAppliedPresetName(OverlayCanvas.k_DefaultPresetName);
	                    window.overlayCanvas.afterOverlaysInitialized?.Invoke();
                    });
                }
            }

            menu.AddItem(L10n.Tr($"{pathPrefix}Revert All Saved Presets"), false, () =>
            {
                if (EditorUtility.DisplayDialog(
                    L10n.Tr("Revert All Saved Presets"),
                    L10n.Tr($"Unity is about to delete all {overlayTargetType.ToLower()} presets that are not loaded from files in project and restore default settings."),
                    L10n.Tr("Continue"), L10n.Tr("Cancel")))
                {
                    RevertPreferencesPresetsToDefault();
                    ReloadAllPresets();
                    ApplyPreset(window.overlayCanvas, GetDefaultPreset(window.GetType()));
                }
            });
        }
    }
}
