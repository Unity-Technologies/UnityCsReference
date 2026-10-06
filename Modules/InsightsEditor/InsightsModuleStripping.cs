// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Modules;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.InsightsEditor;

internal partial class InsightsModuleStripping
{
    [OnCodeLoaded]
    static void Initialize()
    {
        UnityEditorInternal.AssemblyStripper.onCollectIncludedModules += OnCollectIncludedModules;
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        UnityEditorInternal.AssemblyStripper.onCollectIncludedModules -= OnCollectIncludedModules;
    }

    static void OnCollectIncludedModules(IPreStrippingModuleAdder adder)
    {
        if (!EngineDiagnostics.EngineDiagnosticsSettings.IsFeatureSupported(EditorUserBuildSettings.activeBuildTarget))
            return;

        // Re-resolve so the inclusion decision is made from the same requirements that
        // InsightsSettings::Transfer serializes into the build.
        InsightsRequirementsResolver.SyncRequirements();

        if (EngineDiagnostics.EngineDiagnosticsSettings.GetInsightsModuleEnabled())
            adder.AddModule("Insights");
    }
}
