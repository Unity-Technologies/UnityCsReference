// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.InsightsEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.EngineDiagnostics
{
    [VisibleToOtherModules]
    [Serializable]
    [NativeHeader("Modules/UnityConnect/Insights/InsightsSettings.h")]
    internal enum BuildProfileEngineDiagnosticsState
    {
        /// <summary>
        /// Engine Diagnostics are disabled
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// Engine Diagnostics are enabled
        /// </summary>
        Enabled = 1,
        /// <summary>
        /// Engine Diagnostics will use value set in Project Settings.
        /// </summary>
        ProjectSettings = 2
    }

    [NativeHeader("Modules/UnityConnect/Insights/InsightsSettings.h")]
    [NativeHeader("Modules/Insights/CollectionRequirement.h")]
    [StaticAccessor("GetInsightsSettings()")]
    public static class EngineDiagnosticsSettings
    {
        [VisibleToOtherModules]
        [NativeProperty("EngineDiagnosticsEnabled")]
        public static extern bool enabled { get; set; }
        [VisibleToOtherModules]
        internal static extern bool GetEngineDiagnosticsEnabledDefaultBuildValue();
        [VisibleToOtherModules]
        internal static extern bool IsFeatureSupported(BuildTarget target);
        [VisibleToOtherModules]
        internal static extern bool GetInsightsModuleEnabled();
        [VisibleToOtherModules]
        internal static extern void SetCustomEventUrl(string url);
        [VisibleToOtherModules]
        internal static extern CollectionRequirement[] GetCollectionRequirements();
        [VisibleToOtherModules]
        internal static extern void SetCollectionRequirements(CollectionRequirement[] requirements);
        [VisibleToOtherModules]
        internal static extern CollectionRequirement[] GetProjectPackages();
        [VisibleToOtherModules]
        internal static extern void SetProjectPackages(CollectionRequirement[] packages);

        [RequiredByNativeCode]
        internal static void NotifyEngineDiagnosticsSettingsChanged(bool enabled) =>
            InsightsEditorUtils.NotifyEngineDiagnosticsSettingsChanged(enabled);

        // Called by InsightsSettings::Transfer when serializing for a build.
        [RequiredByNativeCode]
        internal static void SyncRequirements() =>
            InsightsRequirementsResolver.SyncRequirements();
    }
}
