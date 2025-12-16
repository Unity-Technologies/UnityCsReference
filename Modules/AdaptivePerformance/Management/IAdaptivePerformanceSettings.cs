// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
//using UnityEditor;
using UnityEngine;

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

        [SerializeField, Tooltip("Thermal Action Delay")]
        float m_ThermalActionDelay = 10;

        /// <summary>
        /// Delay after any scaler is applied or unapplied because of thermal state.
        /// </summary>
        public float thermalActionDelay
        {
            get { return m_ThermalActionDelay; }
            set { m_ThermalActionDelay = value; }
        }

        [SerializeField, Tooltip("Performance Action Delay")]
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

        [SerializeField, Tooltip("Name of the scaler profile.")]
        string m_Name = "Default Scaler Profile";
    }

    /// <summary>
    /// Settings of indexer system.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceScalerSettings
    {
        /// <summary>
        /// Apply existing external settings to a scaler to override the existing settings.
        /// </summary>
        /// <param name="settings">Provide existing settings to replace the default settings.</param>
        public void ApplySettings(AdaptivePerformanceScalerSettings settings)
        {
            if (settings == null)
                return;

            ApplySettingsBase(AdaptiveFramerate, settings.AdaptiveFramerate);
            ApplySettingsBase(AdaptiveBatching, settings.AdaptiveBatching);
            ApplySettingsBase(AdaptiveLOD, settings.AdaptiveLOD);
            ApplySettingsBase(AdaptiveLut, settings.AdaptiveLut);
            ApplySettingsBase(AdaptiveMSAA, settings.AdaptiveMSAA);
            ApplySettingsBase(AdaptiveResolution, settings.AdaptiveResolution);
            ApplySettingsBase(AdaptiveShadowCascade, settings.AdaptiveShadowCascade);
            ApplySettingsBase(AdaptiveShadowDistance, settings.AdaptiveShadowDistance);
            ApplySettingsBase(AdaptiveShadowmapResolution, settings.AdaptiveShadowmapResolution);
            ApplySettingsBase(AdaptiveShadowQuality, settings.AdaptiveShadowQuality);
            ApplySettingsBase(AdaptiveTransparency, settings.AdaptiveTransparency);
            ApplySettingsBase(AdaptiveSorting, settings.AdaptiveSorting);
            ApplySettingsBase(AdaptiveViewDistance, settings.AdaptiveViewDistance);
            ApplySettingsBase(AdaptivePhysics, settings.AdaptivePhysics);
            ApplySettingsBase(AdaptiveLayerCulling, settings.AdaptiveLayerCulling);
            ApplySettingsBase(AdaptiveDecals, settings.AdaptiveDecals);
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveFramerate
        {
            get { return m_AdaptiveFramerate; }
            set { m_AdaptiveFramerate = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveResolution
        {
            get { return m_AdaptiveResolution; }
            set { m_AdaptiveResolution = value; }
        }

        [SerializeField, Tooltip("Settings for a scaler used by the Indexer to control if dynamic batching is enabled.")]
        AdaptivePerformanceScalerSettingsBase m_AdaptiveBatching = new AdaptivePerformanceScalerSettingsBase
        {
            name = "Adaptive Batching",
            enabled = false,
            scale = 1,
            visualImpact = ScalerVisualImpact.Medium,
            target =  ScalerTarget.CPU,
            maxLevel = 1,
            minBound = 0,
            maxBound = 1,
        };
        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to control if dynamic batching is enabled.
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveBatching
        {
            get { return m_AdaptiveBatching; }
            set { m_AdaptiveBatching = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveLOD
        {
            get { return m_AdaptiveLOD; }
            set { m_AdaptiveLOD = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveLut
        {
            get { return m_AdaptiveLut; }
            set { m_AdaptiveLut = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveMSAA
        {
            get { return m_AdaptiveMSAA; }
            set { m_AdaptiveMSAA = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveShadowCascade
        {
            get { return m_AdaptiveShadowCascade; }
            set { m_AdaptiveShadowCascade = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveShadowDistance
        {
            get { return m_AdaptiveShadowDistance; }
            set { m_AdaptiveShadowDistance = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveShadowmapResolution
        {
            get { return m_AdaptiveShadowmapResolution; }
            set { m_AdaptiveShadowmapResolution = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveShadowQuality
        {
            get { return m_AdaptiveShadowQuality; }
            set { m_AdaptiveShadowQuality = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveSorting
        {
            get { return m_AdaptiveSorting; }
            set { m_AdaptiveSorting = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveTransparency
        {
            get { return m_AdaptiveTransparency; }
            set { m_AdaptiveTransparency = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveViewDistance
        {
            get { return m_AdaptiveViewDistance; }
            set { m_AdaptiveViewDistance = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptivePhysics
        {
            get { return m_AdaptivePhysics; }
            set { m_AdaptivePhysics = value; }
        }

        /// <summary>
        /// A scaler setting used by <see cref="AdaptivePerformanceIndexer"/> to change decal properties.
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveDecals
        {
            get { return m_AdaptiveDecals; }
            set { m_AdaptiveDecals = value; }
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
        /// </summary>
        public AdaptivePerformanceScalerSettingsBase AdaptiveLayerCulling
        {
            get { return m_AdaptiveLayerCulling; }
            set { m_AdaptiveLayerCulling = value; }
        }
    }
    /// <summary>
    /// Settings of indexer system.
    /// </summary>
    [System.Serializable]
    public class AdaptivePerformanceScalerSettingsBase
    {
        [SerializeField, Tooltip("Name of the scaler.")]
        string m_Name = "Base Scaler";

        /// <summary>
        /// Returns the name of the scaler.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
            }
        }

        [SerializeField, Tooltip("Active")]
        bool m_Enabled = false;

        /// <summary>
        /// Returns true if Indexer was active, false otherwise.
        /// </summary>
        public bool enabled
        {
            get { return m_Enabled; }
            set { m_Enabled = value; }
        }

        [SerializeField, Tooltip("Scale to control the quality impact for the scaler. No quality change when 1, improved quality when >1, and lowered quality when <1.")]
        float m_Scale = -1.0f;

        /// <summary>
        /// Scale to control the quality impact for the scaler. No quality change when 1, improved quality when bigger 1, and lowered quality when smaller 1.
        /// </summary>
        public float scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        [SerializeField, Tooltip("Visual impact the scaler has on the application. The higher the value, the more impact the scaler has on the visuals.")]
        ScalerVisualImpact m_VisualImpact = ScalerVisualImpact.Low;

        /// <summary>
        /// Visual impact the scaler has on the application. The higher the value, the more impact the scaler has on the visuals.
        /// </summary>
        public ScalerVisualImpact visualImpact
        {
            get { return m_VisualImpact; }
            set { m_VisualImpact = value; }
        }

        [SerializeField, Tooltip("Application bottleneck that the scaler targets. The target selected has the most impact on the quality control of this scaler.")]
        ScalerTarget m_Target = ScalerTarget.CPU;

        /// <summary>
        /// Application bottleneck that the scaler targets. The target selected has the most impact on the quality control of this scaler.
        /// </summary>
        public ScalerTarget target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        [SerializeField, Tooltip("Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps.")]
        int m_MaxLevel = 1;

        /// <summary>
        /// Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps.
        /// </summary>
        public int maxLevel
        {
            get { return m_MaxLevel; }
            set { m_MaxLevel = value; }
        }

        [SerializeField, Tooltip("Minimum value for the scale boundary.")]
        float m_MinBound = -1.0f;

        /// <summary>
        /// Minimum value for the scale boundary.
        /// </summary>
        public float minBound
        {
            get { return m_MinBound; }
            set { m_MinBound = value; }
        }

        [SerializeField, Tooltip("Maximum value for the scale boundary.")]
        float m_MaxBound = -1.0f;

        /// <summary>
        /// Maximum value for the scale boundary.
        /// </summary>
        public float maxBound
        {
            get { return m_MaxBound; }
            set { m_MaxBound = value; }
        }
    }

    /// <summary>
    /// Provider Settings Interface as base class of the provider. Used to control the Editor runtime asset instance which stores the Settings.
    /// </summary>
    public class IAdaptivePerformanceSettings : ScriptableObject
    {
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

        [SerializeField, Tooltip("Logging Frequency (Development mode only)")]
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


        [SerializeField, Tooltip("Scaler Settings")]
        AdaptivePerformanceScalerSettings m_ScalerSettings;

        /// <summary>
        /// Settings of scaler system.
        /// </summary>
        public AdaptivePerformanceScalerSettings scalerSettings
        {
            get { return m_ScalerSettings; }
            set { m_ScalerSettings = value; }
        }

        /// <summary>
        /// Load a scaler profile from the settings. Unity update the values of all scalers in the profile to new ones.
        /// This is a heavy operation using reflection and should not be used per frame and only in load operations as it causes hitching and possible screen artifacts depending on which scalers are used in a scene.
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
                    scalerSettings.ApplySettings(scalerProfile);
                    break;
                }
            }
            if (ApplyScalerProfileToAllScalers())
                APLog.Debug($"Scaler profile {scalerProfileName} loaded.");
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
                    aScaler.Deactivate();
                    aScaler.ApplyDefaultSetting((AdaptivePerformanceScalerSettingsBase)value);
                    aScaler.Activate();
                    success = true;
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
