// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditorInternal
{
    internal static partial class ManagedCaptureStripping
    {
        // Supply the config to the linker. A valid, persisted OTA override wins; otherwise the
        // baked default (ManagedCaptureDefaultConfig.k_Json) is used.
        [OnCodeLoaded]
        static void Initialize()
        {
            AssemblyStripper.onProvideManagedCaptureConfig += ProvideConfig;
        }

        [OnCodeUnloading]
        static void Teardown()
        {
            AssemblyStripper.onProvideManagedCaptureConfig -= ProvideConfig;
        }

        // Provider handed to the linker, feature-gated with the same conditions as
        // InsightsModuleStripping (Engine Diagnostics enabled and supported on the active target).
        // When diagnostics is off the Insights module/bindings are stripped, so feeding managed-capture
        // rules to the linker would inject ManagedCaptureHooks calls into an SDK whose hook target no
        // longer ships. Returns null when the feature is off, or when the resolved config (baked or
        // OTA) has nothing to inject, so the linker skips injection entirely either way.
        static string ProvideConfig()
        {
            if (!UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled)
                return null;
            if (!UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.IsFeatureSupported(EditorUserBuildSettings.activeBuildTarget))
                return null;

            var config = GetConfig();
            return ManagedCaptureOtaConfig.IsValidConfig(config, out _) ? config : null;
        }

        // Config precedence handed to the linker: a valid persisted OTA override, else the baked default.
        internal static string GetConfig()
        {
            return ManagedCaptureOtaConfig.ReadPersistedOtaConfig() ?? ManagedCaptureDefaultConfig.k_Json;
        }
    }
}
