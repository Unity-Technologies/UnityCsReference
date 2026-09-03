// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: _3DPhysics not yet converted
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.PhysicsEditor;

[NativeHeader("Modules/Physics/PhysicsBackendSystem.h")]
internal partial class PhysicsBackendStripping
{
    const string k_PhysicsModuleName = "Physics";
    const string k_PhysicsManagerAssetPath = "ProjectSettings/DynamicsManager.asset";
    const uint k_FallbackIntegrationId = 0xDECAFBAD;

    [OnCodeLoaded]
    static void Initialize()
    {
        AssemblyStripper.onCollectIncludedModules += AddPhysicsBackendModule;
    }

    [OnCodeUnloading]
    static void Teardown()
    {
        AssemblyStripper.onCollectIncludedModules -= AddPhysicsBackendModule;
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

        // If the current backend module name is null/empty this means we have a backend that is not registered via a module
        // or the serialized ID does not match a backend that is currently present in the user's project
        var currentBackendModuleName = GetIntegrationUnityModuleName(activeIntegration.id);
        if (string.IsNullOrEmpty(currentBackendModuleName))
            return;

        adder.AddModule(currentBackendModuleName);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
