// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.PhysicsEditor;

[InitializeOnLoad]
[NativeHeader("Modules/Physics/CommandLayer/PhysicsBackendSystem.h")]
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

        uint serializedBackendId = physMgr.FindProperty("m_CurrentBackendId").uintValue;
        uint activeIntegrationId = Physics.GetCurrentIntegrationInfo().id;
        
        // Check if the ID for the backend currently active is the same as the one selected in the settings
        // If not ensure we use the one selected in the settings inside the build
        uint targetId = serializedBackendId == activeIntegrationId ? activeIntegrationId : serializedBackendId;

        //if the current backend is the fallback then it is marked as belonging to the 'Physics' module
        if (targetId == k_FallbackIntegrationId)
            return;

        //if the current backend module name is null/empty this means we have a backend that is not registered via a module
        //or the serialized ID does not match a backend that is currently present in the user's project
        var currentBackendModuleName = GetIntegrationUnityModuleName(targetId);
        if (string.IsNullOrEmpty(currentBackendModuleName))
            return;

        adder.AddModule(currentBackendModuleName);
    }
}
