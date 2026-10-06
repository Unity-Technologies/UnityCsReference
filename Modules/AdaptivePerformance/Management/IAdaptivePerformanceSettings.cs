// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
//using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.AdaptivePerformance
{
    // Changes to tooltips in this file should be reflected in ProviderSettingsEditor as well.

    /// <summary>
    /// Settings of indexer system.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceIndexerSettings
    {
        const string m_FeatureName = "Indexer";

        [SerializeField, Tooltip("Active")]
        bool m_Active = true;

        /// <summary>
        /// Returns true if Indexer was active, false otherwise.
        /// </summary>
        public bool active
        {
            get { return m_Active; }
            set
            {
                if (m_Active == value)
                    return;

                m_Active = value;
                AdaptivePerformanceAnalytics.SendAdaptiveFeatureUpdateEvent(m_FeatureName, m_Active);
            }
        }

        [SerializeField, Min(0), Tooltip("Thermal Action Delay")]
        float m_ThermalActionDelay = 10;

        /// <summary>
        /// Delay after any scaler is applied or unapplied because of thermal state.
        /// </summary>
        public float thermalActionDelay
        {
            get { return m_ThermalActionDelay; }
            set { m_ThermalActionDelay = value; }
        }

        [SerializeField, Min(0), Tooltip("Performance Action Delay")]
        float m_PerformanceActionDelay = 4;

        /// <summary>
        /// Delay after any scaler is applied or unapplied because of performance state.
        /// </summary>
        public float performanceActionDelay
        {
            get { return m_PerformanceActionDelay; }
            set { m_PerformanceActionDelay = value; }
        }
    }


    /// <summary>
    /// Scaler profiles are used to combine all settings of scalers into one profile to be able to change the settings of each scaler at once.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceScalerProfile : AdaptivePerformanceScalerSettings
    {
        /// <summary>
        /// Name of the Scaler Profile. Used to find profiles and switch them during runtime.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// List of custom scalers added to the provider settings via the scaler profile UI.
        /// </summary>
        public List<AdaptivePerformanceScaler>  AddedScalers
        {
            get { return m_AddedScalers; }
            set { m_AddedScalers = value; }
        }

        [SerializeField]
        List<AdaptivePerformanceScaler> m_AddedScalers = new List<AdaptivePerformanceScaler>();

        [SerializeField, Tooltip("Name of the scaler profile.")]
        string m_Name = "Default Scaler Profile";

        internal void EnableAddedScalers()
        {
            for(int i = 0; i < m_AddedScalers.Count; i++)
            {
                if (m_AddedScalers[i])
                {
                    m_AddedScalers[i].InitializeScaler();
                }
                else
                {
                    APLog.Debug("Null scaler is added to the scaler list");
                }
            }
        }

        internal void RemoveAllAddedScalersFromIndexer()
        {
            foreach (var scaler in m_AddedScalers)
            {
                if (scaler)
                {
                    scaler.RemoveScaler();
                }
            }

        }
    }

    /// <summary>
    /// Wrapper that redirects the name property to shared per-scaler storage while
    /// routing all per-mode properties (enabled, scale, bounds, target, ...) through
    /// the active mode's underlying settings struct. Uses a function to dynamically
    /// get the current base settings, so it automatically reflects operation mode changes.
    /// </summary>
    internal class ScalerSettingsEnabledWrapper : AdaptivePerformanceScalerSettingsBase
    {
        private System.Func<AdaptivePerformanceScalerSettingsBase> m_BaseSettingsGetter;
        private System.Func<string> m_NameGetter;
        private System.Action<string> m_NameSetter;
        private System.Func<AdaptivePerformanceScalerSettingsBase> m_NormalModeGetter;
        private System.Func<AdaptivePerformanceScalerSettingsBase> m_BatteryModeGetter;
        private System.Func<AdaptivePerformanceScalerSettingsBase> m_StableFrameModeGetter;

        public ScalerSettingsEnabledWrapper(System.Func<AdaptivePerformanceScalerSettingsBase> baseSettingsGetter, System.Func<string> nameGetter, System.Action<string> nameSetter, System.Func<AdaptivePerformanceScalerSettingsBase> normalModeGetter = null, System.Func<AdaptivePerformanceScalerSettingsBase> batteryModeGetter = null, System.Func<AdaptivePerformanceScalerSettingsBase> stableFrameModeGetter = null)
        {
            m_BaseSettingsGetter = baseSettingsGetter;
            m_NameGetter = nameGetter;
            m_NameSetter = nameSetter;
            m_NormalModeGetter = normalModeGetter;
            m_BatteryModeGetter = batteryModeGetter;
            m_StableFrameModeGetter = stableFrameModeGetter;
        }

        public override string name
        {
            get => m_NameGetter();
            set => m_NameSetter(value);
        }

        public override bool enabled
        {
            get => m_BaseSettingsGetter().enabled;
            set => m_BaseSettingsGetter().enabled = value;
        }

        public override float scale
        {
            get => m_BaseSettingsGetter().scale;
            set => m_BaseSettingsGetter().scale = value;
        }

        public override ScalerVisualImpact visualImpact
        {
            get => m_BaseSettingsGetter().visualImpact;
            set => m_BaseSettingsGetter().visualImpact = value;
        }

        public override ScalerTarget target
        {
            get => m_BaseSettingsGetter().target;
            set => m_BaseSettingsGetter().target = value;
        }

        public override int maxLevel
        {
            get => m_BaseSettingsGetter().maxLevel;
            set => m_BaseSettingsGetter().maxLevel = value;
        }

        public override float minBound
        {
            get => m_BaseSettingsGetter().minBound;
            set => m_BaseSettingsGetter().minBound = value;
        }

        public override float maxBound
        {
            get => m_BaseSettingsGetter().maxBound;
            set => m_BaseSettingsGetter().maxBound = value;
        }

        public override AdaptivePerformanceScalerSettingsBase GetNormalModeSetting()
        {
            return m_NormalModeGetter != null ? m_NormalModeGetter() : m_BaseSettingsGetter();
        }

        public override AdaptivePerformanceScalerSettingsBase GetBatteryModeSetting()
        {
            return m_BatteryModeGetter != null ? m_BatteryModeGetter() : m_BaseSettingsGetter();
        }

        public override AdaptivePerformanceScalerSettingsBase GetStableFrameModeSetting()
        {
            return m_StableFrameModeGetter != null ? m_StableFrameModeGetter() : m_BaseSettingsGetter();
        }
    }

    /// <summary>
    /// Settings of indexer system.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceScalerSettings
    {
        [SerializeField]
        OperationMode m_OperationMode = OperationMode.NormalMode;

        /// <summary>
        /// Current operation mode for the settings (NormalMode or BatteryMode).
        /// Controls which settings (normal or battery) are returned by scaler properties.
        /// Wrappers dynamically reflect the mode change, so no cache invalidation needed.
        /// </summary>
        public OperationMode OperationMode
        {
            get { return m_OperationMode; }
            set { m_OperationMode = value; }
        }

        /// <summary>
        /// Apply existing external settings to a scaler to override the existing settings.
        /// </summary>
        /// <param name="settings">Provide existing settings to replace the default settings.</param>
        public void ApplySettings(AdaptivePerformanceScalerSettings settings)
        {
            if (settings == null)
                return;

            ApplySettingsAllModes(AdaptiveFramerate, settings.AdaptiveFramerate);
            ApplySettingsAllModes(AdaptiveLOD, settings.AdaptiveLOD);
            ApplySettingsAllModes(AdaptiveLut, settings.AdaptiveLut);
            ApplySettingsAllModes(AdaptiveMSAA, settings.AdaptiveMSAA);
            ApplySettingsAllModes(AdaptiveResolution, settings.AdaptiveResolution);
            ApplySettingsAllModes(AdaptiveShadowCascade, settings.AdaptiveShadowCascade);
            ApplySettingsAllModes(AdaptiveShadowDistance, settings.AdaptiveShadowDistance);
            ApplySettingsAllModes(AdaptiveShadowmapResolution, settings.AdaptiveShadowmapResolution);
            ApplySettingsAllModes(AdaptiveShadowQuality, settings.AdaptiveShadowQuality);
            ApplySettingsAllModes(AdaptiveTransparency, settings.AdaptiveTransparency);
            ApplySettingsAllModes(AdaptiveSorting, settings.AdaptiveSorting);
            ApplySettingsAllModes(AdaptiveViewDistance, settings.AdaptiveViewDistance);
            ApplySettingsAllModes(AdaptivePhysics, settings.AdaptivePhysics);
            ApplySettingsAllModes(AdaptiveLayerCulling, settings.AdaptiveLayerCulling);
            ApplySettingsAllModes(AdaptiveDecals, settings.AdaptiveDecals);
            ApplySettingsAllModes(AdaptiveOnDemandRendering, settings.AdaptiveOnDemandRendering);
        }

        // Copies the underlying per-mode settings for every OperationMode, bypassing the
        // operation-mode routing of the wrapper accessors. Without this, ApplySettings
        // would silently drop a mode's data because the wrapper's mode-routed properties
        // only expose the currently-active mode's struct based on OperationMode.
        //
        // The 'name' field is routed by the wrapper to shared per-scaler storage and is
        // copied directly. 'enabled' lives per-mode on each PerModeScalerSettings.settings
        // .enabled and is copied per mode via ApplySettingsBase along with the other
        // mode-specific visual fields (scale, visualImpact, target, minBound, maxBound,
        // maxLevel) on the raw structs returned by GetNormalModeSetting() /
        // GetBatteryModeSetting() / GetStableFrameModeSetting().
        void ApplySettingsAllModes(AdaptivePerformanceScalerSettingsBase destination, AdaptivePerformanceScalerSettingsBase sources)
        {
            if (destination == null || sources == null)
                return;
            destination.name = sources.name;
            ApplySettingsBase(destination.GetNormalModeSetting(),      sources.GetNormalModeSetting());
            ApplySettingsBase(destination.GetBatteryModeSetting(),     sources.GetBatteryModeSetting());
            ApplySettingsBase(destination.GetStableFrameModeSetting(), sources.GetStableFrameModeSetting());
        }

        void ApplySettingsBase(AdaptivePerformanceScalerSettingsBase destination, AdaptivePerformanceScalerSettingsBase sources)
        {
            destination.enabled = sources.enabled;
            destination.scale = sources.scale;
            destination.visualImpact = sources.visualImpact;
            destination.target = sources.target;
            destination.minBound = sources.minBound;
            destination.maxBound = sources.maxBound;
            destination.maxLevel = sources.maxLevel;
        }


        // === Per-mode scaler storage ===
        //
        // Each scaler is described by:
        //   - PerModeScalerSettings : one row per (scalerKey, mode) — bounds, scale,
        //                             visualImpact, target that may differ per mode.
        //   - SharedScalerState     : one row per scalerKey — displayName (the bit that
        //                             doesn't vary by mode). enabled is NOT here — it
        //                             lives per-mode on each PerModeScalerSettings
        //                             .settings.enabled.
        //
        // The legacy m_AdaptiveX* fields are kept around so MigrateToPerModeLayout can
        // copy data out of them on first load. They can be removed in a follow-up
        // Unity version after the migration grace period.

        /// <summary>
        /// Per-(scaler, mode) tuneable settings. One row per scaler per OperationMode.
        /// </summary>
        [System.Serializable]
        internal class PerModeScalerSettings
        {
            public string scalerKey;
            public OperationMode mode;
            public AdaptivePerformanceScalerSettingsBase settings;
        }

        /// <summary>
        /// Per-scaler state that does NOT differ by mode (displayName).
        /// Note: enabled lives per-mode on each PerModeScalerSettings.settings.enabled, not here.
        /// </summary>
        [System.Serializable]
        internal class SharedScalerState
        {
            public string scalerKey;
            public string displayName;
        }

        /// <summary>
        /// String keys for the built-in scalers. Used as the lookup key in
        /// <see cref="m_PerModeSettings"/> and <see cref="m_SharedScalerState"/>.
        /// </summary>
        internal static class ScalerKeys
        {
            public const string AdaptiveFramerate           = "AdaptiveFramerate";
            public const string AdaptiveResolution          = "AdaptiveResolution";
            public const string AdaptiveBatching            = "AdaptiveBatching";
            public const string AdaptiveLOD                 = "AdaptiveLOD";
            public const string AdaptiveLut                 = "AdaptiveLut";
            public const string AdaptiveMSAA                = "AdaptiveMSAA";
            public const string AdaptiveShadowCascade       = "AdaptiveShadowCascade";
            public const string AdaptiveShadowDistance      = "AdaptiveShadowDistance";
            public const string AdaptiveShadowmapResolution = "AdaptiveShadowmapResolution";
            public const string AdaptiveShadowQuality       = "AdaptiveShadowQuality";
            public const string AdaptiveSorting             = "AdaptiveSorting";
            public const string AdaptiveTransparency        = "AdaptiveTransparency";
            public const string AdaptiveViewDistance        = "AdaptiveViewDistance";
            public const string AdaptivePhysics             = "AdaptivePhysics";
            public const string AdaptiveDecals              = "AdaptiveDecals";
            public const string AdaptiveLayerCulling        = "AdaptiveLayerCulling";
            public const string AdaptiveOnDemandRendering           = "AdaptiveOnDemandRendering";
        }

        // Single source of truth for the per-(scaler, mode) inline defaults. Each
        // factory produces a fresh struct so callers can mutate the result without
        // aliasing. Used by FindOrCreatePerMode to lazy-create a per-mode row when
        // none exists, and by Migrate to seed BatteryMode rows for legacy assets
        // (which only stored a single normal-mode struct per scaler).
        [NoAutoStaticsCleanup] 
        static readonly Dictionary<string, Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>>
            k_ScalerDefaults = new Dictionary<string, Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>>
        {
            {
                ScalerKeys.AdaptiveFramerate,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    {
                        OperationMode.NormalMode,
                        () => new AdaptivePerformanceScalerSettingsBase
                        {
                            name = "Adaptive Framerate",
                            enabled = false,
                            scale = 1.0f,
                            visualImpact = ScalerVisualImpact.High,
                            target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate,
                            minBound = 15,
                            maxBound = 60,
                            maxLevel = 60 - 15,
                        }
                    },
                    {
                        OperationMode.BatteryMode,
                        () => new AdaptivePerformanceScalerSettingsBase
                        {
                            name = "Adaptive Framerate",
                            enabled = false,
                            scale = 1.0f,
                            visualImpact = ScalerVisualImpact.High,
                            target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate,
                            minBound = 30,
                            maxBound = 30,
                            maxLevel = 1,
                        }
                    },
                    {
                        OperationMode.StableFrameMode,
                        () => new AdaptivePerformanceScalerSettingsBase
                        {
                            name = "Adaptive Framerate",
                            enabled = false,
                            scale = 1.0f,
                            visualImpact = ScalerVisualImpact.High,
                            target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate,
                            minBound = 15,
                            maxBound = 60,
                            maxLevel = 60 - 15,
                        }
                    },
                }
            },
            {
                ScalerKeys.AdaptiveResolution,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Resolution", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.FillRate | ScalerTarget.GPU, maxLevel = 9, minBound = 0.5f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Resolution", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.FillRate | ScalerTarget.GPU, maxLevel = 1, minBound = 0.5f, maxBound = 0.5f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Resolution", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.FillRate | ScalerTarget.GPU, maxLevel = 9, minBound = 0.5f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveBatching,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Batching", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Batching", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Batching", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveLOD,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive LOD", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.4f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive LOD", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0.4f, maxBound = 0.4f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive LOD", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.4f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveLut,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Lut", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Lut", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Lut", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveMSAA,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive MSAA", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.FillRate, maxLevel = 2, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive MSAA", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.FillRate, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive MSAA", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.FillRate, maxLevel = 2, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveShadowCascade,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Cascade", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 2, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Cascade", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Cascade", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 2, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveShadowDistance,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.15f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0.15f, maxBound = 0.15f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.15f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveShadowmapResolution,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadowmap Resolution", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.15f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadowmap Resolution", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0.15f, maxBound = 0.15f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadowmap Resolution", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.GPU, maxLevel = 3, minBound = 0.15f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveShadowQuality,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Quality", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 3, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Quality", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Shadow Quality", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU | ScalerTarget.CPU, maxLevel = 3, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveSorting,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Sorting", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Sorting", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Sorting", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveTransparency,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Transparency", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Transparency", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0, maxBound = 0 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Transparency", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveViewDistance,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive View Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 40, minBound = 50f, maxBound = 1000 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive View Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 50f, maxBound = 50f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive View Distance", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.GPU, maxLevel = 40, minBound = 50f, maxBound = 1000 } },
                }
            },
            {
                ScalerKeys.AdaptivePhysics,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Physics", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.CPU, maxLevel = 5, minBound = 0.5f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Physics", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0.5f, maxBound = 0.5f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Physics", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Low,
                          target = ScalerTarget.CPU, maxLevel = 5, minBound = 0.5f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveDecals,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Decals", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU, maxLevel = 20, minBound = 0.01f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Decals", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU, maxLevel = 1, minBound = 0.01f, maxBound = 0.01f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Decals", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.GPU, maxLevel = 20, minBound = 0.01f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveLayerCulling,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Layer Culling", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 40, minBound = 0.01f, maxBound = 1 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Layer Culling", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 1, minBound = 0.01f, maxBound = 0.01f } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Layer Culling", enabled = false, scale = 1, visualImpact = ScalerVisualImpact.Medium,
                          target = ScalerTarget.CPU, maxLevel = 40, minBound = 0.01f, maxBound = 1 } },
                }
            },
            {
                ScalerKeys.AdaptiveOnDemandRendering,
                new Dictionary<OperationMode, Func<AdaptivePerformanceScalerSettingsBase>>
                {
                    { OperationMode.NormalMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive On Demand Rendering", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate, minBound = 1, maxBound = 60, maxLevel = 59 } },
                    { OperationMode.BatteryMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive On Demand Rendering", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate, minBound = 1, maxBound = 60, maxLevel = 59 } },
                    { OperationMode.StableFrameMode, () => new AdaptivePerformanceScalerSettingsBase
                        { name = "Adaptive Rendering", enabled = false, scale = 1.0f, visualImpact = ScalerVisualImpact.High,
                          target = ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate, minBound = 1, maxBound = 60, maxLevel = 59 } },
                }
            },
        };

        [SerializeField] List<PerModeScalerSettings> m_PerModeSettings = new List<PerModeScalerSettings>();
        [SerializeField] List<SharedScalerState> m_SharedScalerState = new List<SharedScalerState>();
        [SerializeField] bool m_PerModeLayoutMigrated = false;

        // Runtime-only cache: rebuilt after deserialization since wrappers capture
        // references to entries in the (re-deserialized) per-mode collections.
        readonly Dictionary<string, ScalerSettingsEnabledWrapper> m_WrapperCache =
            new Dictionary<string, ScalerSettingsEnabledWrapper>();

        PerModeScalerSettings FindOrCreatePerMode(string key, OperationMode mode)
        {
            for (int i = 0; i < m_PerModeSettings.Count; i++)
            {
                var row = m_PerModeSettings[i];
                if (row.scalerKey == key && row.mode == mode)
                    return row;
            }
            var fresh = new PerModeScalerSettings
            {
                scalerKey = key,
                mode = mode,
                settings = k_ScalerDefaults[key][mode](),
            };
            m_PerModeSettings.Add(fresh);
            return fresh;
        }

        SharedScalerState FindOrCreateShared(string key)
        {
            for (int i = 0; i < m_SharedScalerState.Count; i++)
            {
                var row = m_SharedScalerState[i];
                if (row.scalerKey == key)
                    return row;
            }
            var fresh = new SharedScalerState
            {
                scalerKey = key,
                displayName = k_ScalerDefaults[key][OperationMode.NormalMode]().name,
            };
            m_SharedScalerState.Add(fresh);
            return fresh;
        }

        /// <summary>
        /// Builds and caches a wrapper for the given scaler key. The wrapper captures
        /// references to entries in m_PerModeSettings / m_SharedScalerState so reads and
        /// writes through the wrapper hit the new storage directly.
        /// </summary>
        ScalerSettingsEnabledWrapper GetWrapper(string key)
        {
            MigrateToPerModeLayout();

            if (m_WrapperCache.TryGetValue(key, out var cached))
                return cached;

            var shared      = FindOrCreateShared(key);
            var normal      = FindOrCreatePerMode(key, OperationMode.NormalMode);
            var battery     = FindOrCreatePerMode(key, OperationMode.BatteryMode);
            var stableFrame = FindOrCreatePerMode(key, OperationMode.StableFrameMode);

            var wrapper = new ScalerSettingsEnabledWrapper(
                () =>
                {
                    switch (OperationMode)
                    {
                        case OperationMode.BatteryMode:     return battery.settings;
                        case OperationMode.StableFrameMode: return stableFrame.settings;
                        default:                            return normal.settings;
                    }
                },
                () => shared.displayName,
                (val) => shared.displayName = val,
                () => normal.settings,
                () => battery.settings,
                () => stableFrame.settings
            );
            m_WrapperCache[key] = wrapper;
            return wrapper;
        }

        /// <summary>
        /// Setter shared by all converted scaler properties. Routes the assigned value
        /// to the row matching the current OperationMode and syncs the shared per-scaler
        /// fields. Same mode-selection logic as the wrapper getter so a write hits the
        /// same backing struct that a read would return.
        /// </summary>
        void AssignScaler(string key, AdaptivePerformanceScalerSettingsBase value)
        {
            MigrateToPerModeLayout();

            var shared = FindOrCreateShared(key);
            // value.enabled is carried inside the assigned struct, so it lands per-mode.
            FindOrCreatePerMode(key, OperationMode).settings = value;
            shared.displayName = value.name;
        }

        /// <summary>
        /// One-time migration of legacy per-scaler m_AdaptiveX* fields into the
        /// m_PerModeSettings / m_SharedScalerState collections. Idempotent via
        /// m_PerModeLayoutMigrated. Safe to call on every load.
        /// </summary>
        internal void MigrateToPerModeLayout()
        {
            if (m_PerModeLayoutMigrated)
                return;

            // Trunk-era assets only stored one struct per scaler whose .enabled / .name
            // were the source of truth; install that struct as-is as the NormalMode row.
            // The BatteryMode row is left for FindOrCreatePerMode to lazy-create from
            // k_ScalerDefaults the first time it's read — battery settings start at the
            // per-mode factory defaults rather than mirroring the user's normal-mode tuning.
            void Migrate(string key, AdaptivePerformanceScalerSettingsBase normal)
            {
                if (normal == null)
                    return;
                m_PerModeSettings.Add(new PerModeScalerSettings
                    { scalerKey = key, mode = OperationMode.NormalMode, settings = normal });
                m_SharedScalerState.Add(new SharedScalerState
                    { scalerKey = key, displayName = normal.name });
            }

            Migrate(ScalerKeys.AdaptiveFramerate,           m_AdaptiveFramerate);
            Migrate(ScalerKeys.AdaptiveResolution,          m_AdaptiveResolution);
            Migrate(ScalerKeys.AdaptiveLOD,                 m_AdaptiveLOD);
            Migrate(ScalerKeys.AdaptiveLut,                 m_AdaptiveLut);
            Migrate(ScalerKeys.AdaptiveMSAA,                m_AdaptiveMSAA);
            Migrate(ScalerKeys.AdaptiveShadowCascade,       m_AdaptiveShadowCascade);
            Migrate(ScalerKeys.AdaptiveShadowDistance,      m_AdaptiveShadowDistance);
            Migrate(ScalerKeys.AdaptiveShadowmapResolution, m_AdaptiveShadowmapResolution);
            Migrate(ScalerKeys.AdaptiveShadowQuality,       m_AdaptiveShadowQuality);
            Migrate(ScalerKeys.AdaptiveSorting,             m_AdaptiveSorting);
            Migrate(ScalerKeys.AdaptiveTransparency,        m_AdaptiveTransparency);
            Migrate(ScalerKeys.AdaptiveViewDistance,        m_AdaptiveViewDistance);
            Migrate(ScalerKeys.AdaptivePhysics,             m_AdaptivePhysics);
            Migrate(ScalerKeys.AdaptiveDecals,              m_AdaptiveDecals);
            Migrate(ScalerKeys.AdaptiveLayerCulling,        m_AdaptiveLayerCulling);
            Migrate(ScalerKeys.AdaptiveOnDemandRendering,           m_AdaptiveOnDemandRendering);

            m_PerModeLayoutMigrated = true;
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the application update rate using Application.TargetFramerate")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveFramerate = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Framerate",
            enabled = false,
            scale = 1.0f,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate,
            minBound = 15,
            maxBound = 60,
            maxLevel = 60 - 15
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the application update rate using <see cref="Application.targetFrameRate"/>.
        /// Returns the active setting based on current OperationMode with enabled field redirected to separate storage.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveFramerate
        {
            get => GetWrapper(ScalerKeys.AdaptiveFramerate);
            set => AssignScaler(ScalerKeys.AdaptiveFramerate, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the resolution of all render targets that allow dynamic resolution.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveResolution = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Resolution",
            enabled = false,
            scale = 1.0f,
            visualImpact = ScalerVisualImpact.Low,
            target =  ScalerTarget.FillRate | ScalerTarget.GPU,
            maxLevel = 9,
            minBound = 0.5f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the resolution of all render targets that allow dynamic resolution.
        /// Returns the active setting based on current OperationMode with enabled field redirected to separate storage.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveResolution
        {
            get => GetWrapper(ScalerKeys.AdaptiveResolution);
            set => AssignScaler(ScalerKeys.AdaptiveResolution, value);
        }

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to control if dynamic batching is enabled.
        /// Returns the active setting based on current OperationMode with enabled field redirected to separate storage.
        /// </summary>
        [System.Obsolete("AdaptiveBatching is obsolete.", true)]
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveBatching
        {
            get => GetWrapper(ScalerKeys.AdaptiveBatching);
            set => AssignScaler(ScalerKeys.AdaptiveBatching, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer for adjusting at what distance LODs are switched.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveLOD = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive LOD",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.GPU,
            maxLevel = 3,
            minBound = 0.4f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> for adjusting at what distance LODs are switched.
        /// Returns the active setting based on current OperationMode with enabled field redirected to separate storage.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveLOD
        {
            get => GetWrapper(ScalerKeys.AdaptiveLOD);
            set => AssignScaler(ScalerKeys.AdaptiveLOD, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the size of the palette used for color grading in URP.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveLut = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Lut",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target =  ScalerTarget.GPU | ScalerTarget.CPU,
            maxLevel = 1,
            minBound = 0,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the size of the palette used for color grading in URP.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveLut
        {
            get => GetWrapper(ScalerKeys.AdaptiveLut);
            set => AssignScaler(ScalerKeys.AdaptiveLut, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the level of antialiasing.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveMSAA = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive MSAA",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target =  ScalerTarget.GPU | ScalerTarget.FillRate,
            maxLevel = 2,
            minBound = 0,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the level of antialiasing.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveMSAA
        {
            get => GetWrapper(ScalerKeys.AdaptiveMSAA);
            set => AssignScaler(ScalerKeys.AdaptiveMSAA, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the number of shadow cascades to be used.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveShadowCascade = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Shadow Cascade",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target =  ScalerTarget.GPU | ScalerTarget.CPU,
            maxLevel = 2,
            minBound = 0,
            maxBound = 1,
        };

        const string obsoleteMsg = "AdaptiveShadowCascades has been renamed. Please use AdaptiveShadowCascade. (UnityUpgradable) -> AdaptiveShadowCascade";
        /// <summary>
        /// Obsolete: Please use <see cref="AdaptiveShadowCascade"/>.
        /// </summary>
        [Obsolete(obsoleteMsg, false)] // ap-obsolete-001 - once removed, ensure all instances of ap-obsolete-001 are removed
        public AdaptivePerformanceScalerSettingsBase AdaptiveShadowCascades => AdaptiveShadowCascade;

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the number of shadow cascades to be used.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveShadowCascade
        {
            get => GetWrapper(ScalerKeys.AdaptiveShadowCascade);
            set => AssignScaler(ScalerKeys.AdaptiveShadowCascade, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change the distance at which shadows are rendered.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveShadowDistance = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Shadow Distance",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Low,
            target =  ScalerTarget.GPU,
            maxLevel = 3,
            minBound = 0.15f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change the distance at which shadows are rendered.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveShadowDistance
        {
            get => GetWrapper(ScalerKeys.AdaptiveShadowDistance);
            set => AssignScaler(ScalerKeys.AdaptiveShadowDistance, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the resolution of shadow maps.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveShadowmapResolution = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Shadowmap Resolution",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Low,
            target =  ScalerTarget.GPU,
            maxLevel = 3,
            minBound = 0.15f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the resolution of shadow maps.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveShadowmapResolution
        {
            get => GetWrapper(ScalerKeys.AdaptiveShadowmapResolution);
            set => AssignScaler(ScalerKeys.AdaptiveShadowmapResolution, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the quality of shadows.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveShadowQuality = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Shadow Quality",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.GPU | ScalerTarget.CPU,
            maxLevel = 3,
            minBound = 0,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the quality of shadows.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveShadowQuality
        {
            get => GetWrapper(ScalerKeys.AdaptiveShadowQuality);
            set => AssignScaler(ScalerKeys.AdaptiveShadowQuality, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change if objects in the scene are sorted by depth before rendering to reduce overdraw.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveSorting = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Sorting",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target =  ScalerTarget.CPU,
            maxLevel = 1,
            minBound = 0,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change if objects in the scene are sorted by depth before rendering to reduce overdraw.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveSorting
        {
            get => GetWrapper(ScalerKeys.AdaptiveSorting);
            set => AssignScaler(ScalerKeys.AdaptiveSorting, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to disable transparent objects rendering")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveTransparency = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Transparency",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.GPU,
            maxLevel = 1,
            minBound = 0,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to disable transparent objects rendering.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveTransparency
        {
            get => GetWrapper(ScalerKeys.AdaptiveTransparency);
            set => AssignScaler(ScalerKeys.AdaptiveTransparency, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change the view distance")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveViewDistance = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive View Distance",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.GPU,
            maxLevel = 40,
            minBound = 50f,
            maxBound = 1000,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change the view distance.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveViewDistance
        {
            get => GetWrapper(ScalerKeys.AdaptiveViewDistance);
            set => AssignScaler(ScalerKeys.AdaptiveViewDistance, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change physics properties")]
        AdaptivePerformanceScalerSettingsBase m_AdaptivePhysics = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Physics",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Low,
            target =  ScalerTarget.CPU,
            maxLevel = 5,
            minBound = 0.5f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change physics properties.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptivePhysics
        {
            get => GetWrapper(ScalerKeys.AdaptivePhysics);
            set => AssignScaler(ScalerKeys.AdaptivePhysics, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change decal properties")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveDecals = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Decals",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target = ScalerTarget.GPU,
            maxLevel = 20,
            minBound = 0.01f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change decal properties.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveDecals
        {
            get => GetWrapper(ScalerKeys.AdaptiveDecals);
            set => AssignScaler(ScalerKeys.AdaptiveDecals, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to change the layer culling distance")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveLayerCulling = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Layer Culling",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target = ScalerTarget.CPU,
            maxLevel = 40,
            minBound = 0.01f,
            maxBound = 1,
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change the layer culling distance.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveLayerCulling
        {
            get => GetWrapper(ScalerKeys.AdaptiveLayerCulling);
            set => AssignScaler(ScalerKeys.AdaptiveLayerCulling, value);
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to adjust the application update rate using Rendering.OnDemandRendering.renderFrameInterval")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveOnDemandRendering = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive On Demand Rendering",
            enabled = false,
            scale = 1.0f,
            visualImpact = ScalerVisualImpact.High,
            target =  ScalerTarget.CPU | ScalerTarget.GPU | ScalerTarget.FillRate,
            minBound = 1,
            maxBound = 60,
            maxLevel = 59
        };

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to adjust the application update rate using Rendering.OnDemandRendering.renderFrameInterval.
        /// Returns the active setting based on current OperationMode.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase AdaptiveOnDemandRendering
        {
            get => GetWrapper(ScalerKeys.AdaptiveOnDemandRendering);
            set => AssignScaler(ScalerKeys.AdaptiveOnDemandRendering, value);
        }

        private AdaptivePerformanceScalerSettingsBase[] m_DefaultScalerSettings = new AdaptivePerformanceScalerSettingsBase[16];
        private ReadOnlyCollection<AdaptivePerformanceScalerSettingsBase> m_ReadOnlyDefaultScalerSettings;

        void SyncDefaultScalerSettings()
        {
            m_DefaultScalerSettings[0] = AdaptiveFramerate;
            m_DefaultScalerSettings[1] = AdaptiveLOD;
            m_DefaultScalerSettings[2] = AdaptiveLut;
            m_DefaultScalerSettings[3] = AdaptiveMSAA;
            m_DefaultScalerSettings[4] = AdaptiveResolution;
            m_DefaultScalerSettings[5] = AdaptiveShadowCascade;
            m_DefaultScalerSettings[6] = AdaptiveShadowDistance;
            m_DefaultScalerSettings[7] = AdaptiveShadowmapResolution;
            m_DefaultScalerSettings[8] = AdaptiveShadowQuality;
            m_DefaultScalerSettings[9] = AdaptiveTransparency;
            m_DefaultScalerSettings[10] = AdaptiveSorting;
            m_DefaultScalerSettings[11] = AdaptiveViewDistance;
            m_DefaultScalerSettings[12] = AdaptivePhysics;
            m_DefaultScalerSettings[13] = AdaptiveLayerCulling;
            m_DefaultScalerSettings[14] = AdaptiveDecals;
            m_DefaultScalerSettings[15] = AdaptiveOnDemandRendering;
        }

        /// <summary>
        /// Returns the list of default scaler settings.
        /// </summary>
        public IReadOnlyList<AdaptivePerformanceScalerSettingsBase> DefaultScalerSettings
        {
            get
            {
                if (m_ReadOnlyDefaultScalerSettings == null)
                    m_ReadOnlyDefaultScalerSettings = Array.AsReadOnly(m_DefaultScalerSettings);

                SyncDefaultScalerSettings();
                return m_ReadOnlyDefaultScalerSettings;
            }
        }

        [NoAutoStaticsCleanup] // fixed compile-time list of built-in scaler types, never changes
        internal static readonly List<Type> k_DefaultScalerNames = new List<Type>
        {
            typeof(UnityEngine.AdaptivePerformance.AdaptiveFramerate),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveLOD),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveLut),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveMSAA),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveResolution),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveShadowCascade),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveShadowDistance),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveShadowmapResolution),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveShadowQuality),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveTransparency),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveSorting),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveViewDistance),
            typeof(UnityEngine.AdaptivePerformance.AdaptivePhysics),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveLayerCulling),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveDecals),
            typeof(UnityEngine.AdaptivePerformance.AdaptiveOnDemandRendering)

        };


    }
    /// <summary>
    /// Settings of indexer system.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceScalerSettingsBase
    {
        [SerializeField]
        string m_Name = "Base Scaler";

        /// <summary>
        /// Returns the name of the scaler.
        /// </summary>
        public virtual string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [SerializeField, Tooltip("Active")]
        bool m_Enabled = false;

        /// <summary>
        /// Returns true if Indexer was active, false otherwise.
        /// </summary>
        public virtual bool enabled
        {
            get { return m_Enabled; }
            set { m_Enabled = value; }
        }

        [SerializeField, Tooltip("Scale to control the quality impact for the scaler. No quality change when 1, improved quality when >1, and lowered quality when <1.")]
        float m_Scale = -1.0f;

        /// <summary>
        /// Scale to control the quality impact for the scaler. No quality change when 1, improved quality when bigger 1, and lowered quality when smaller 1.
        /// </summary>
        public virtual float scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        [SerializeField, Tooltip("Visual impact the scaler has on the application. The higher the value, the more impact the scaler has on the visuals.")]
        ScalerVisualImpact m_VisualImpact = ScalerVisualImpact.Low;

        /// <summary>
        /// Visual impact the scaler has on the application. The higher the value, the more impact the scaler has on the visuals.
        /// </summary>
        public virtual ScalerVisualImpact visualImpact
        {
            get { return m_VisualImpact; }
            set { m_VisualImpact = value; }
        }

        [SerializeField, Tooltip("Application bottleneck that the scaler targets. The target selected has the most impact on the quality control of this scaler.")]
        ScalerTarget m_Target = ScalerTarget.CPU;

        /// <summary>
        /// Application bottleneck that the scaler targets. The target selected has the most impact on the quality control of this scaler.
        /// </summary>
        public virtual ScalerTarget target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        [SerializeField, Tooltip("Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps.")]
        int m_MaxLevel = 1;

        /// <summary>
        /// Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps.
        /// </summary>
        public virtual int maxLevel
        {
            get { return m_MaxLevel; }
            set { m_MaxLevel = value; }
        }

        [SerializeField, Tooltip("Minimum value for the scale boundary.")]
        float m_MinBound = -1.0f;

        /// <summary>
        /// Minimum value for the scale boundary.
        /// </summary>
        public virtual float minBound
        {
            get { return m_MinBound; }
            set { m_MinBound = value; }
        }

        [SerializeField, Tooltip("Maximum value for the scale boundary.")]
        float m_MaxBound = -1.0f;

        /// <summary>
        /// Maximum value for the scale boundary.
        /// </summary>
        public virtual float maxBound
        {
            get { return m_MaxBound; }
            set { m_MaxBound = value; }
        }
        /// <summary>
        /// Creates a deepcopy of the current scaler settings.
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase Clone()
        {
            return new AdaptivePerformanceScalerSettingsBase
            {
                name = name,
                enabled = enabled,
                scale = scale,
                visualImpact = visualImpact,
                target = target,
                maxLevel = maxLevel,
                minBound = minBound,
                maxBound = maxBound,
            };
        }

        /// <summary>
        /// Gets the normal mode settings. For wrappers, this returns the underlying normal mode field.
        /// For regular settings objects, this returns itself.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase GetNormalModeSetting()
        {
            return this;
        }

        /// <summary>
        /// Gets the battery mode settings. For wrappers, this returns the underlying battery mode field.
        /// For regular settings objects, this returns itself.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase GetBatteryModeSetting()
        {
            return this;
        }

        /// <summary>
        /// Gets the stable frame mode settings. For wrappers, this returns the underlying stable frame mode field.
        /// For regular settings objects, this returns itself.
        /// </summary>
        public virtual AdaptivePerformanceScalerSettingsBase GetStableFrameModeSetting()
        {
            return this;
        }
    }

    /// <summary>
    /// Provider Settings Interface as base class of the provider. Used to control the Editor runtime asset instance which stores the Settings.
    /// </summary>
    public class IAdaptivePerformanceSettings : ScriptableObject
    {
        // Migrate every loaded scaler profile to the per-mode m_PerModeSettings layout.
        // Lives on the base class so every provider subclass inherits the migration —
        // without this, upgraded user projects on providers that don't explicitly call
        // it would silently lose their legacy scaler tuning. MigrateToPerModeLayout is
        // idempotent via its m_PerModeLayoutMigrated flag, so calling it on every load
        // is safe.
        protected virtual void Awake()
        {
            if (ScalerProfiles == null)
                return;
            foreach (var profile in ScalerProfiles)
                profile?.MigrateToPerModeLayout();
        }

        [SerializeField, Tooltip("Enable Logging in Devmode")]
        bool m_Logging = true;

        /// <summary>
        ///  Control debug logging.
        ///  This setting only affects development builds. All logging is disabled in release builds.
        ///  This setting can also be controlled after startup using <see cref="IDevelopmentSettings.Logging"/>.
        ///  Logging is disabled by default.
        /// </summary>
        /// <value>Set this to true to enable debug logging, or false to disable it. It is false by default.</value>
        public bool logging
        {
            get { return m_Logging; }
            set { m_Logging = value; }
        }

        [SerializeField, Tooltip("Automatic Performance Mode")]
        bool m_AutomaticPerformanceModeEnabled = true;

        /// <summary>
        /// The initial value of <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/>.
        /// </summary>
        /// <value>Set this to true to enable Automatic Performance Mode, or false to disable it. It is true by default.</value>
        public bool automaticPerformanceMode
        {
            get { return m_AutomaticPerformanceModeEnabled; }
            set { m_AutomaticPerformanceModeEnabled = value; }
        }

        [SerializeField, Tooltip("Automatic Game Mode")]
        bool m_AutomaticGameModeEnabled = false;

        /// <summary>
        /// Whether automated target frame rate based on device GameMode settings should be used.
        /// </summary>
        public bool automaticGameMode
        {
            get { return m_AutomaticGameModeEnabled; }
            set { m_AutomaticGameModeEnabled = value; }
        }

        [SerializeField, Tooltip("Enables the CPU and GPU boost mode before engine startup to decrease startup time.")]
        bool m_EnableBoostOnStartup = true;

        /// <summary>
        /// Whether CPU and GPU boost mode should be enabled on application startup.
        /// </summary>
        public bool enableBoostOnStartup
        {
            get { return m_EnableBoostOnStartup; }
            set { m_EnableBoostOnStartup = value; }
        }

        [SerializeField, Min(1), Tooltip("Logging Frequency (Development mode only)")]
        int m_StatsLoggingFrequencyInFrames = 50;

        /// <summary>
        /// Adjust the frequency in frames at which the application logs frame statistics to the console.
        /// This is only relevant when logging is enabled. See <see cref="IDevelopmentSettings.Logging"/>.
        /// This setting can also be controlled after startup using <see cref="IDevelopmentSettings.LoggingFrequencyInFrames"/>.
        /// </summary>
        /// <value>Logging frequency in frames (default: 50)</value>
        public int statsLoggingFrequencyInFrames
        {
            get { return m_StatsLoggingFrequencyInFrames; }
            set { m_StatsLoggingFrequencyInFrames = value; }
        }

        [SerializeField, Tooltip("Indexer Settings")]
        AdaptivePerformanceIndexerSettings m_IndexerSettings;

        /// <summary>
        /// Settings of indexer system.
        /// </summary>
        public AdaptivePerformanceIndexerSettings indexerSettings
        {
            get { return m_IndexerSettings; }
            set { m_IndexerSettings = value; }
        }

        /// <summary>
        /// Settings of scaler system.
        /// </summary>
        public AdaptivePerformanceScalerSettings scalerSettings
        {
            get { return ActiveScalerProfile; }
            set
            {
                // Support backward compatibility - if a base type is assigned, wrap it in a profile
                if (value is AdaptivePerformanceScalerProfile profile)
                {
                    ActiveScalerProfile = profile;
                }
                else if (value != null)
                {
                    // Legacy code assigning AdaptivePerformanceScalerSettings - migrate to profile
                    if (ActiveScalerProfile == null && m_scalerProfileList != null && m_scalerProfileList.Length > 0)
                    {
                        ActiveScalerProfile = m_scalerProfileList[0];
                    }
                    if (ActiveScalerProfile != null)
                    {
                        ActiveScalerProfile.ApplySettings(value);
                    }
                }
                else
                {
                    ActiveScalerProfile = null;
                }
            }
        }
        /// <summary>
        /// List of created scaler profiles for this provider.
        /// </summary>
        public AdaptivePerformanceScalerProfile[] ScalerProfiles
        {
            get { return m_scalerProfileList; }
        }

        /// <summary>
        /// The currently active scaler profile, or <c>null</c> if no profile has been loaded.
        /// Only one scaler profile is active at a time. Call <see cref="LoadScalerProfile"/> to
        /// activate one. The getter does not auto-bind to the default profile: a previous lazy
        /// initialization here ran before the scalers were actually bound to the profile, which
        /// caused LoadScalerProfile's "already loaded" early-out (a Name comparison against
        /// ActiveScalerProfile) to short-circuit on first call and skip ApplyScalerProfileToAllScalers,
        /// leaving every default scaler with its inline-default settings (base names, default bounds,
        /// disabled) until something else triggered a re-apply. Returning null until LoadScalerProfile
        /// runs makes "no profile loaded" an honest, observable state and lets the early-out work.
        /// </summary>
        public AdaptivePerformanceScalerProfile ActiveScalerProfile
        {
            get => m_ActiveScalerProfile;
            set => m_ActiveScalerProfile = value;
        }

        /// <summary>
        /// The currently active mode provider, or <c>null</c> if no provider has been set.
        /// </summary>
        public IAdaptivePerformanceModeProvider ActiveModeProvider
        {
            get
            {
                return m_ActiveModeProvider;
            }
            set
            {
                m_ActiveModeProvider = value;
            }
        }

        /// <summary>
        /// This is to contain the scalers via scanning the users assembly for backward compatibility
        /// </summary>
        [VisibleToOtherModules("UnityEditor.AdaptivePerformanceModule")]
        internal List<AdaptivePerformanceScaler> AddedScalerViaScan
        {
            get { return m_AddedScalerViaScan; }
            set { m_AddedScalerViaScan = value; }
        }

        [SerializeField]
        List<AdaptivePerformanceScaler> m_AddedScalerViaScan = new List<AdaptivePerformanceScaler>();

        AdaptivePerformanceScalerProfile m_ActiveScalerProfile = null;

        [SerializeField]
        AdaptivePerformanceBatteryModeProvider m_BatteryModeProvider = new AdaptivePerformanceBatteryModeProvider();
        [SerializeField]
        AdaptivePerformanceNormalModeProvider m_NormalModeProvider = new AdaptivePerformanceNormalModeProvider();
        [SerializeField]
        AdaptivePerformanceStableFrameModeProvider m_StableFrameModeProvider = new AdaptivePerformanceStableFrameModeProvider();

        IAdaptivePerformanceModeProvider m_ActiveModeProvider;

        [SerializeField]
        OperationMode m_IndexerOperationMode = OperationMode.NormalMode;

        /// <summary>
        /// Returns the default battery mode provider, which is used to control the behavior of the indexer in battery mode.
        /// </summary>
        public AdaptivePerformanceBatteryModeProvider BatteryModeProvider
        {
            get => m_BatteryModeProvider;
        }
        /// <summary>
        /// Returns the default normal mode provider, which is used to control the behavior of the indexer in normal mode.
        /// </summary>
        public AdaptivePerformanceNormalModeProvider NormalModeProvider
        {
            get => m_NormalModeProvider;
        }

        /// <summary>
        /// Returns the default stable frame mode provider, which is used to control the behavior of the indexer in stable frame mode.
        /// </summary>
        public AdaptivePerformanceStableFrameModeProvider StableFrameModeProvider
        {
            get => m_StableFrameModeProvider;
        }

        /// <summary>
        /// Returns the current operation mode of the indexer.
        /// Setting this also propagates the value to <see cref="ActiveScalerProfile"/>'s
        /// <see cref="AdaptivePerformanceScalerSettings.OperationMode"/> so scaler getters
        /// reflect the active mode at runtime.
        /// </summary>
        public OperationMode IndexerOperationMode
        {
            get => m_IndexerOperationMode;
            internal set
            {
                m_IndexerOperationMode = value;
                if (ActiveScalerProfile != null)
                    ActiveScalerProfile.OperationMode = value;
            }
        }

        /// <summary>
        /// Add a new scaler profile.
        /// </summary>
        /// <param name="name"></param>
        public void AddScalerProfileWithDefaultScalers(string name = "")
        {
            foreach (var profile in ScalerProfiles)
            {
                if (profile.Name == name)
                {
                    Debug.LogWarning($"{profile.Name} already exists in the profile list");
                    return;
                }
            }
            Array.Resize(ref m_scalerProfileList, m_scalerProfileList.Length + 1);
            m_scalerProfileList[^1] = new AdaptivePerformanceScalerProfile();
            if (!String.IsNullOrEmpty(name))
            {
                m_scalerProfileList[^1].Name = name;
            }
        }
        /// <summary>
        /// Delete a scaler profile at the given index.
        /// </summary>
        /// <param name="index"></param>
        public void DeleteScalerProfileAt(int index)
        {
            if (index >= ScalerProfiles.Length || index < 0) return;
            AdaptivePerformanceScalerProfile[] modifiedList = new AdaptivePerformanceScalerProfile[ScalerProfiles.Length - 1];
            int j = 0;
            for (int i = 0; i < ScalerProfiles.Length; i++)
            {
                if (i != index)
                {
                    modifiedList[j] = ScalerProfiles[i];
                    j++;
                }
            }
            m_scalerProfileList = modifiedList;
        }

        /// <summary>
        /// Load a scaler profile from the settings. Unity update the values of all scalers in the profile to new ones.
        /// This is a heavy operation and should not be used per frame and only in load operations as it causes hitching and possible screen artifacts depending on which scalers are used in a scene.
        /// </summary>
        /// <param name="scalerProfileName">Supply the name of the scaler. You can query a list of available scaler profiles via <see cref="IAdaptivePerformanceSettings.GetAvailableScalerProfiles"/>.</param>
        public void LoadScalerProfile(string scalerProfileName)
        {
            if (scalerProfileName == null || scalerProfileName.Length <= 0)
            {
                APLog.Debug("Scaler profile name empty. Can not load and apply profile.");
                return;
            }
            if (m_scalerProfileList.Length <= 0)
            {
                APLog.Debug("No scaler profiles available. Can not load and apply profile. Add more profiles in the Adaptive Performance settings.");
                return;
            }

            if (ActiveScalerProfile != null && ActiveScalerProfile.Name == scalerProfileName)
            {
                APLog.Debug("The " +ActiveScalerProfile.Name + " scaler profile is already loaded.");
                return;
            }

            if (m_scalerProfileList.Length == 1)
                APLog.Debug("Only default scaler profile available. Reset all scalers to default profile.");

            for (int i = 0; i < m_scalerProfileList.Length; i++)
            {
                AdaptivePerformanceScalerProfile scalerProfile = m_scalerProfileList[i];
                if (scalerProfile == null)
                {
                    APLog.Debug("Scaler profile is null. Can not load and apply profile. Check Adaptive Performance settings.");
                    return;
                }
                if (scalerProfile.Name == null || scalerProfile.Name.Length <= 0)
                {
                    APLog.Debug("Scaler profile name is null or empty. Can not load and apply profile. Check Adaptive Performance settings.");
                    return;
                }
                if (scalerProfile.Name == scalerProfileName)
                {
                    if (ActiveScalerProfile != null)
                    {
                        ActiveScalerProfile.RemoveAllAddedScalersFromIndexer();
                    }

                    // If a scaler profile has custom scaler, prioritize using them and remove the scanned ones.
                    if (scalerProfile.AddedScalers != null && scalerProfile.AddedScalers.Count > 0)
                    {
                        scalerProfile.EnableAddedScalers();
                        for (int j = 0; j < AddedScalerViaScan.Count; j++)
                        {
                            AddedScalerViaScan[j].RemoveScaler();
                        }
                    }
                    ActiveScalerProfile = scalerProfile;
                    // Sync the new profile's mode to the indexer's current mode so that
                    // scalerSettings reads/writes target the correct mode's settings.
                    // Without this, the new profile retains its serialized OperationMode
                    // (typically NormalMode), which can cause subsequent runtime mutations
                    // through scalerSettings to silently modify the wrong mode.
                    ActiveScalerProfile.OperationMode = m_IndexerOperationMode;
                    break;
                }
            }
            if (ApplyScalerProfileToAllScalers())
                APLog.Debug($"Scaler profile {scalerProfileName} loaded.");

            // Battery Mode force-enables scalers at runtime; a profile switch wipes that.
            // Gate on the mode (not the active provider instance, which callers may replace)
            // and let the battery provider re-force its scalers from the freshly-applied profile.
            if (IndexerOperationMode == OperationMode.BatteryMode)
                BatteryModeProvider?.OnScalerProfileChanged();
            else if (IndexerOperationMode == OperationMode.StableFrameMode)
                StableFrameModeProvider?.OnScalerProfileChanged();
        }

        bool ApplyScalerProfileToAllScalers()
        {
            bool success = false;

            if (Holder.Instance == null || Holder.Instance.Indexer == null)
                return success;

            List<AdaptivePerformanceScaler> allScalers = new List<AdaptivePerformanceScaler>();
            List<AdaptivePerformanceScaler> scalers = new List<AdaptivePerformanceScaler>();
            Holder.Instance.Indexer.GetUnappliedScalers(ref scalers);
            allScalers.AddRange(scalers);
            Holder.Instance.Indexer.GetAppliedScalers(ref scalers);
            allScalers.AddRange(scalers);
            Holder.Instance.Indexer.GetDisabledScalers(ref scalers);
            allScalers.AddRange(scalers);

            if (allScalers.Count <= 0)
            {
                APLog.Debug($"No scalers found. No scaler profile applied.");
                return success;
            }

            PropertyInfo[] properties = typeof(AdaptivePerformanceScalerSettings).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                var aScaler = allScalers.Find(s => s.GetType().ToString().Contains(property.Name));
                if (aScaler)
                {
                    System.Reflection.PropertyInfo prop = typeof(AdaptivePerformanceScalerSettings).GetProperty(property.Name);
                    var value = prop.GetValue(scalerSettings);
                    if (value is AdaptivePerformanceScalerSettingsBase settingsBase)
                    {
                        aScaler.Deactivate();
                        // Apply normal mode settings
                        aScaler.ApplyDefaultSetting(settingsBase.GetNormalModeSetting());
                        // Apply battery mode settings
                        aScaler.ApplyBatteryModeSetting(settingsBase.GetBatteryModeSetting());
                        // Apply stable frame mode settings
                        aScaler.ApplyStableFrameModeSetting(settingsBase.GetStableFrameModeSetting());
                        // Pull the profile's enabled intent into the scaler instance.
                        // Without this, default scalers created via ScriptableObject.CreateInstance
                        // retain their m_ScalerEnabled = false default and remain permanently
                        // disabled regardless of what the profile specifies.
                        aScaler.Enabled = settingsBase.enabled;
                        aScaler.Activate();
                        success = true;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Returns a list of all available scaler profiles.
        /// </summary>
        /// <returns></returns>
        public string[] GetAvailableScalerProfiles()
        {
            string[] scalerNames = new string[m_scalerProfileList.Length];
            if (m_scalerProfileList.Length <= 0)
            {
                APLog.Debug("No scaler profiles available. You can not load and apply profiles. Add more profiles in the Adaptive Performance settings.");
                return scalerNames;
            }
            for (int i = 0; i < m_scalerProfileList.Length; i++)
            {
                AdaptivePerformanceScalerProfile scalerProfile = m_scalerProfileList[i];
                scalerNames[i] = scalerProfile.Name;
            }
            return scalerNames;
        }

        [SerializeField] AdaptivePerformanceScalerProfile[] m_scalerProfileList = new AdaptivePerformanceScalerProfile[] { new AdaptivePerformanceScalerProfile {} };

        /// <summary>
        /// Default scaler profile index.
        /// </summary>
        public int defaultScalerProfilerIndex
        {
            get { return m_DefaultScalerProfilerIndex; }
            set { m_DefaultScalerProfilerIndex = value; }
        }
        [SerializeField] internal int m_DefaultScalerProfilerIndex = 0;

        // Default values set when a new Adaptive Performance setting is created
        [SerializeField] int k_AssetVersion = 3;

        /// <summary>
        /// When Unity enables the serialized object it upgrades old files to the new format in the editor and saves the assets. Empty during runtime.
        /// </summary>
        public void OnEnable()
        {
            if (k_AssetVersion < 3)
            {
                k_AssetVersion = 2;
            }
        }
    }
}
