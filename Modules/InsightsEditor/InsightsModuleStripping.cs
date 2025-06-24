// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Modules;

namespace UnityEditor.InsightsEditor;

[InitializeOnLoad]
internal class InsightsModuleStripping
{
    static InsightsModuleStripping()
    {
        UnityEditorInternal.AssemblyStripper.onCollectIncludedModules += AddInsightsModule;
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
