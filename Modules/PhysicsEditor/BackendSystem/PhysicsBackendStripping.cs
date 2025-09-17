// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.PhysicsEditor;

[InitializeOnLoad]
[NativeHeader("Modules/Physics/PhysicsBackendSystem.h")]
internal class PhysicsBackendStripping
{
    const string k_PhysicsModuleName = "Physics";
    const string k_PhysicsManagerAssetPath = "ProjectSettings/DynamicsManager.asset";
    const uint k_FallbackIntegrationId = 0xDECAFBAD;

    static PhysicsBackendStripping()
    {
        AssemblyStripper.onCollectIncludedModules += AddPhysicsBackendModule;
    }

    [FreeFunction("Physics::BackendSystem::GetIntegrationUnityModuleName")]
    extern static string GetIntegrationUnityModuleName(uint id);

    static void AddPhysicsBackendModule(IPreStrippingModuleAdder adder)
    {
        if (ModuleMetadata.GetModuleIncludeSettingForModule(k_PhysicsModuleName) == ModuleIncludeSetting.ForceExclude)
            return;

        var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(k_PhysicsManagerAssetPath);
        var physMgr = new SerializedObject(assetsAtPath);

        var activeIntegration = Physics.GetCurrentIntegrationInfo();

        //if the current backend is the fallback then it is marked as belonging to the 'Physics' module
        if (activeIntegration.id == k_FallbackIntegrationId)
            return;

        // If the target backend doesn't have the correct license requirement, we do not want to continue
        if (!activeIntegration.HasRequiredEntitlements())
            throw new BuildFailedException($"You do not meet the required license to build the current Physics System." +
                $" Havok Physics for Unity is available to Pro, Enterprise, and Unity Industrial Collection users. Please choose a different Physics System and reboot the Editor");

        // If the current backend module name is null/empty this means we have a backend that is not registered via a module
        // or the serialized ID does not match a backend that is currently present in the user's project
        var currentBackendModuleName = GetIntegrationUnityModuleName(activeIntegration.id);
        if (string.IsNullOrEmpty(currentBackendModuleName))
            return;

        adder.AddModule(currentBackendModuleName);
    }
}
