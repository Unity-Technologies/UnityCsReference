// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AdaptivePerformance not yet converted
using System;
using System.Collections.Generic;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.AdaptivePerformance.UI.Editor
{
    /// <summary>
    /// A toggle to enable adaptive performance in build profile window
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class BuildProfileAdaptivePerformanceToggle : VisualElement
    {
        private Toggle m_EnableAdaptivePerformanceToggle;
        public BuildProfileAdaptivePerformanceProviderUI m_AdaptivePerformanceProviderUI;
        public static readonly string adaptivePerformanceLabelText = L10n.Tr("Adaptive Performance Settings", null);

        readonly string k_LabelText = L10n.Tr("Enable Adaptive Performance", null);
        const string k_BuildProfileAdaptivePerformanceUIUSS = "AdaptivePerformance/StyleSheets/BuildProfileAdaptivePerformanceUI/BuildProfileAdaptivePerformanceUI.uss";
        const string k_BuildProfileAdaptivePerformanceUIUXML = "AdaptivePerformance/UXML/BuildProfileAdaptivePerformanceUI/BuildProfileAdaptivePerformanceUI.uxml";
        BuildProfile m_BuildProfile;
        private VisualElement m_AdaptivePerformanceProviderElement;

        public BuildProfileAdaptivePerformanceToggle(BuildProfile profile)
        {
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            AdaptivePerformancePackageMetadataStore.InitKnownPluginPackages();
            #pragma warning restore UAL0015
            m_BuildProfile = profile;
            m_AdaptivePerformanceProviderUI = new BuildProfileAdaptivePerformanceProviderUI(m_BuildProfile);
            var buildProfileUI = EditorGUIUtility.LoadRequired(k_BuildProfileAdaptivePerformanceUIUXML) as VisualTreeAsset;
            var buildProfileUSS = EditorGUIUtility.LoadRequired(k_BuildProfileAdaptivePerformanceUIUSS) as StyleSheet;
            buildProfileUI.CloneTree(this);
            this.styleSheets.Add(buildProfileUSS);
            m_AdaptivePerformanceProviderElement = this.Q<VisualElement>("adaptivePerformance-provider-container");
            m_EnableAdaptivePerformanceToggle = this.Q<Toggle>("enable-adaptivePerformance-toggle");
            m_EnableAdaptivePerformanceToggle.label = k_LabelText;
            m_EnableAdaptivePerformanceToggle.value = m_BuildProfile.platformBuildProfile?.adaptivePerformanceEnabled ?? false;
            m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.None;
            m_EnableAdaptivePerformanceToggle.RegisterValueChangedCallback(UpdataProvider);
            if (m_BuildProfile.platformBuildProfile?.adaptivePerformanceEnabled == true)
            {
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.Flex;
                if (m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>() == null)
                {
                    InitializeSettingsAndUI();
                }
                else
                {
                    m_AdaptivePerformanceProviderUI.CreateUI();
                }
            }
            m_AdaptivePerformanceProviderElement.Add(m_AdaptivePerformanceProviderUI);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        // Structural fingerprint of this toggle's AP graph the last time it rebuilt itself.
        // Used to filter out unrelated Undo.undoRedoPerformed events (which fire for ANY
        // editor action) so we only run the expensive RefreshFromProfile path when the AP
        // graph on this profile actually changed.
        int m_LastStructureHash;

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Undo/redo (and paste, which goes through the undo system) can mutate the profile's
            // Adaptive Performance sub-assets out from under this toggle. Subscribe so the UI
            // rebuilds itself from the current profile state instead of showing stale content
            // until the user switches profiles and back.
            m_LastStructureHash = CaptureAdaptivePerformanceStructureHash(m_BuildProfile);
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (m_BuildProfile == null) return;
            // undoRedoPerformed fires for every undo/redo across the whole editor (moving a
            // GameObject, editing an unrelated field, etc.). RefreshFromProfile is expensive
            // - UpdateMetadata + Clear + CreateUI rebuild custom editors and IMGUI containers -
            // so only rebuild when the AP graph on this profile has actually changed shape.
            var currentHash = CaptureAdaptivePerformanceStructureHash(m_BuildProfile);
            if (currentHash == m_LastStructureHash) return;
            m_LastStructureHash = currentHash;
            RefreshFromProfile();
        }

        // Structural signature of the AP graph: enabled flag + a hash over each sub-asset's
        // (type, instanceID). Changes when a sub-asset is added, removed, or replaced (paste
        // and its undo both destroy + recreate sub-assets, so instance IDs shift and the hash
        // changes). Cheap to compute - no allocation past the enumerator, no string work.
        static int CaptureAdaptivePerformanceStructureHash(BuildProfile profile)
        {
            if (profile == null) return 0;
            var hash = new HashCode();
            hash.Add(profile.platformBuildProfile?.adaptivePerformanceEnabled ?? false);
            foreach (var subAsset in EnumerateAdaptivePerformanceSubAssets(profile))
            {
                hash.Add(subAsset.GetType());
                hash.Add(subAsset.GetEntityId());
            }
            return hash.ToHashCode();
        }

        void RefreshFromProfile()
        {
            // Defensive: this callback lives on a static event and can fire during teardown
            // if the DetachFromPanelEvent hasn't reached us yet. Any of these fields may be
            // null-Object wrappers by then.
            if (m_BuildProfile == null) return;
            if (m_EnableAdaptivePerformanceToggle == null || m_AdaptivePerformanceProviderUI == null) return;
            var enabled = m_BuildProfile.platformBuildProfile?.adaptivePerformanceEnabled ?? false;
            // SetValueWithoutNotify so this refresh does not re-trigger UpdataProvider and
            // re-run the enable/disable side-effects (which would also re-write the profile).
            m_EnableAdaptivePerformanceToggle.SetValueWithoutNotify(enabled);

            m_AdaptivePerformanceProviderUI.UpdateMetadata();
            m_AdaptivePerformanceProviderUI.Clear();
            if (enabled)
            {
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.Flex;
                if (m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>() != null)
                    m_AdaptivePerformanceProviderUI.CreateUI();
            }
            else
            {
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Enumerates the AP sub-assets attached to <paramref name="profile"/> in a
        /// deterministic order: general, manager, loaders, container, per-provider
        /// (settings then scalers). Used by the paste feature to build a copyable
        /// payload without doing an open-ended graph traversal that could pull in
        /// unrelated ScriptableObject sub-assets. Not intended as a general extension
        /// point; new AP sub-asset types must be added here explicitly.
        /// </summary>
        public static IEnumerable<ScriptableObject> EnumerateAdaptivePerformanceSubAssets(BuildProfile profile)
        {
            if (profile == null) yield break;
            var path = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(path)) yield break;

            // Query AssetDatabase once and pick out the three top-level AP components in a
            // single pass. profile.GetComponent<T>() runs LoadAllAssetsAtPath internally, so
            // calling it three times triples the asset-database traffic - measurable when this
            // enumeration runs from the global Undo.undoRedoPerformed handler on every editor
            // undo/redo event.
            AdaptivePerformanceGeneralSettings general = null;
            AdaptivePerformanceManagerSettings manager = null;
            BuildProfileProviderContainer container = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                switch (asset)
                {
                    case AdaptivePerformanceGeneralSettings g: general ??= g; break;
                    case AdaptivePerformanceManagerSettings m: manager ??= m; break;
                    case BuildProfileProviderContainer c: container ??= c; break;
                }
            }

            if (general != null) yield return general;
            if (manager != null)
            {
                yield return manager;
                foreach (var loader in manager.loaders)
                    if (loader != null) yield return loader;
            }
            if (container != null)
            {
                yield return container;
                foreach (var providerSettings in container.adaptivePerformanceProviderSettings)
                {
                    if (providerSettings == null) continue;
                    yield return (ScriptableObject)providerSettings;
                    foreach (var scaler in providerSettings.AddedScalerViaScan)
                        if (scaler != null) yield return scaler;
                    foreach (var scalerProfile in providerSettings.ScalerProfiles)
                        foreach (var addedScaler in scalerProfile.AddedScalers)
                            if (addedScaler != null) yield return addedScaler;
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="typeName"/> is a loader or provider settings type
        /// registered as supported on <paramref name="buildTarget"/>'s build target group.
        /// Returns false for unknown loader/provider types or types unsupported on the given target.
        /// Callers should only invoke this for types identified as loader/provider; other types
        /// (infrastructure such as general/manager/container, scalers) are platform-agnostic.
        /// </summary>
        public static bool IsLoaderOrProviderSettingsSupportedForBuildTarget(string typeName, BuildTarget buildTarget)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            // If the target's group cannot be resolved (e.g. BuildTarget.NoTarget maps to
            // BuildTargetGroup.Unknown), GetLoadersForBuildTarget returns an empty list and
            // this method reports "unsupported" for every type. Paste callers should treat
            // that as intentional rejection.
            var supportedLoaders = AdaptivePerformancePackageMetadataStore.GetLoadersForBuildTarget(buildTargetGroup);
            foreach (var loader in supportedLoaders)
            {
                if (loader.loaderType == typeName) return true;
                var pkg = AdaptivePerformancePackageMetadataStore.GetMetadataForPackage(loader.packageId);
                if (pkg != null && pkg.settingsType == typeName) return true;
            }
            return false;
        }

        public static void RemoveAllSettingsFromBuildProfile(BuildProfile profile) =>
            RemoveAllSettingsFromBuildProfile(profile, registerUndo: false);

        /// <summary>
        /// When registerUndo is true, sub-asset destruction is routed through
        /// Undo.DestroyObjectImmediate so the operation participates in the undo stack.
        /// The caller is responsible for grouping this into an undo transaction.
        /// </summary>
        public static void RemoveAllSettingsFromBuildProfile(BuildProfile profile, bool registerUndo)
        {
            if (registerUndo)
            {
                RemoveAllSettingsFromBuildProfileWithUndo(profile);
                return;
            }

            var generalSetting = profile.GetComponent<AdaptivePerformanceGeneralSettings>();
            if ( generalSetting != null)
            {
                DestroySubAsset(profile, generalSetting, registerUndo: false);
            }
            var managerSetting = profile.GetComponent<AdaptivePerformanceManagerSettings>();
            if (managerSetting != null)
            {
                foreach (var loader in managerSetting.loaders)
                {
                    if (loader == null) continue;
                    DestroySubAsset(profile, loader, registerUndo: false);
                }
                managerSetting.loaders.Clear();
                DestroySubAsset(profile, managerSetting, registerUndo: false);
            }
            var providerSettingContainer = profile.GetComponent<BuildProfileProviderContainer>();
            if (providerSettingContainer != null)
            {
                foreach (var providerSettings in providerSettingContainer.adaptivePerformanceProviderSettings)
                {
                    // The setting might have been destroyed by removing the package.
                    if (providerSettings == null)
                    {
                        continue;
                    }

                    foreach (var scaler in providerSettings.AddedScalerViaScan)
                    {
                        if (scaler == null) continue;
                        DestroySubAsset(profile, scaler, registerUndo: false);
                    }
                    providerSettings.AddedScalerViaScan.Clear();

                    foreach (var profiles in providerSettings.ScalerProfiles)
                    {
                        foreach (var addedScaler in profiles.AddedScalers)
                        {
                            if (addedScaler == null) continue;
                            DestroySubAsset(profile, addedScaler, registerUndo: false);
                        }
                        profiles.AddedScalers.Clear();
                    }
                    DestroySubAsset(profile, providerSettings as ScriptableObject, registerUndo: false);
                }
                providerSettingContainer.adaptivePerformanceProviderSettings.Clear();
                DestroySubAsset(profile, providerSettingContainer, registerUndo: false);
            }
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        // Undo-safe variant: collects parent-child relationships up front (before any destroy
        // invalidates the traversal), then destroys PARENTS FIRST so the snapshot captured by
        // Undo.DestroyObjectImmediate holds valid PPtrs to still-alive children. Unity replays
        // undo in reverse destroy order (children first, then parents), so restored parents
        // find their children by the same InstanceIDs and refs re-link cleanly.
        static void RemoveAllSettingsFromBuildProfileWithUndo(BuildProfile profile)
        {
            var generalSetting = profile.GetComponent<AdaptivePerformanceGeneralSettings>();
            var managerSetting = profile.GetComponent<AdaptivePerformanceManagerSettings>();
            var providerSettingContainer = profile.GetComponent<BuildProfileProviderContainer>();

            var loaders = new List<AdaptivePerformanceLoader>();
            if (managerSetting != null)
                foreach (var loader in managerSetting.loaders)
                    if (loader != null) loaders.Add(loader);

            var providerScalers = new List<(IAdaptivePerformanceSettings settings, List<UnityEngine.Object> scalers)>();
            if (providerSettingContainer != null)
            {
                foreach (var providerSettings in providerSettingContainer.adaptivePerformanceProviderSettings)
                {
                    if (providerSettings == null) continue;
                    var scalers = new List<UnityEngine.Object>();
                    foreach (var scaler in providerSettings.AddedScalerViaScan)
                        if (scaler != null) scalers.Add(scaler);
                    foreach (var scalerProfile in providerSettings.ScalerProfiles)
                        foreach (var addedScaler in scalerProfile.AddedScalers)
                            if (addedScaler != null) scalers.Add(addedScaler);
                    providerScalers.Add((providerSettings, scalers));
                }
            }

            // Destroy parents first. Their pre-destroy snapshot has valid PPtrs to children,
            // which the children (destroyed next) still hold matching InstanceIDs for.
            if (generalSetting != null) Undo.DestroyObjectImmediate(generalSetting);
            if (managerSetting != null) Undo.DestroyObjectImmediate(managerSetting);
            if (providerSettingContainer != null) Undo.DestroyObjectImmediate(providerSettingContainer);
            foreach (var (settings, _) in providerScalers)
                Undo.DestroyObjectImmediate(settings as ScriptableObject);

            // Now destroy the children.
            foreach (var loader in loaders)
                Undo.DestroyObjectImmediate(loader);
            foreach (var (_, scalers) in providerScalers)
                foreach (var scaler in scalers)
                    Undo.DestroyObjectImmediate(scaler);
        }

        static void DestroySubAsset(BuildProfile profile, UnityEngine.Object subAsset, bool registerUndo)
        {
            if (subAsset == null) return;
            if (registerUndo)
            {
                Undo.DestroyObjectImmediate(subAsset);
                return;
            }
            // Legacy path: match the original behavior (RemoveComponent for top-level
            // components, RemoveObjectFromAsset for children), followed by DestroyImmediate.
            switch (subAsset)
            {
                case AdaptivePerformanceGeneralSettings general:
                    profile.RemoveComponent(general);
                    break;
                case AdaptivePerformanceManagerSettings manager:
                    profile.RemoveComponent(manager);
                    break;
                case BuildProfileProviderContainer container:
                    profile.RemoveComponent(container);
                    break;
                default:
                    AssetDatabase.RemoveObjectFromAsset(subAsset);
                    break;
            }
            ScriptableObject.DestroyImmediate(subAsset, true);
        }

        void UpdataProvider(ChangeEvent<bool> evt)
        {
            m_BuildProfile.platformBuildProfile.adaptivePerformanceEnabled = evt.newValue;
            EditorUtility.SetDirty(m_BuildProfile);
            if (evt.newValue == false)
            {
                m_AdaptivePerformanceProviderUI.UpdateMetadata();
                m_AdaptivePerformanceProviderUI.Clear();
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.None;
            }
            else
            {
                InitializeSettingsAndUI();
            }
            EditorUtilities.EnableAPModule(evt.newValue);
        }

        public void InitializeSettingsAndUI()
        {
            EditorUtilities.CheckEnableFrameTimingState(m_BuildProfile);
            EditorUtilities.CheckEnableThermalStateForIOS(m_BuildProfile);
            AddAdaptivePerformanceGeneralSettingsObject(m_BuildProfile);
            m_AdaptivePerformanceProviderUI.CreateUI();
            m_AdaptivePerformanceProviderUI.SelectDefaultProvider();
            m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.Flex;
        }

        public static void AddAdaptivePerformanceGeneralSettingsObject(BuildProfile profile)
        {
            var generalSetting = profile.GetComponent<AdaptivePerformanceGeneralSettings>();
            if (generalSetting == null)
            {
                generalSetting = ScriptableObject.CreateInstance<AdaptivePerformanceGeneralSettings>();
                generalSetting.hideFlags = HideFlags.HideInInspector;
                profile.AddComponent(generalSetting);
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
