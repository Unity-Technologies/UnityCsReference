// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: EngineDiagnostics not yet converted
using UnityEditor.Modules;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.InsightsEditor;

internal partial class InsightsModuleStripping
{
    [OnCodeLoaded]
    static void Initialize()
    {
        UnityEditorInternal.AssemblyStripper.onCollectIncludedModules += AddInsightsModule;
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        UnityEditorInternal.AssemblyStripper.onCollectIncludedModules -= AddInsightsModule;
    }

    static void AddInsightsModule(IPreStrippingModuleAdder adder)
    {
        if (!EngineDiagnostics.EngineDiagnosticsSettings.enabled)
        {
            return;
        }

        if (!EngineDiagnostics.EngineDiagnosticsSettings.IsFeatureSupported(EditorUserBuildSettings.activeBuildTarget))
        {
            return;
        }

        adder.AddModule("Insights");
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
