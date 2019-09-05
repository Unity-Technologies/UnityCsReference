// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/BuildPipeline/ModuleMetadata.h")]
    internal enum ModuleIncludeSetting
    {
        Auto = 0,
        ForceExclude = 1,
        ForceInclude = 2
    }

    [StaticAccessor("ModuleMetadata::Get()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/BuildPipeline/ModuleMetadata.h")]
    [NativeHeader("Editor/Mono/ModuleMetadata.bindings.h")]
    internal sealed class ModuleMetadata
    {
        [FreeFunction("ModuleMetadataBindings::GetModuleNames")]
        public extern static string[] GetModuleNames();

        [FreeFunction("ModuleMetadataBindings::GetModuleDependencies")]
        public extern static string[] GetModuleDependencies(string moduleName);

        [FreeFunction("ModuleMetadataBindings::IsStrippableModule")]
        extern public static bool IsStrippableModule(string moduleName);

        public static UnityType[] GetModuleTypes(string moduleName)
        {
            var runtimeTypeIndices = GetModuleTypeIndices(moduleName);
            return runtimeTypeIndices.Select(index => UnityType.GetTypeByRuntimeTypeIndex(index)).ToArray();
        }

        [NativeName("GetModuleIncludeSetting")]
        extern public static ModuleIncludeSetting GetModuleIncludeSettingForModule(string module);

        [FreeFunction("ModuleMetadataBindings::SetModuleIncludeSettingForModule")]
        extern public static void SetModuleIncludeSettingForModule(string module, ModuleIncludeSetting setting);

        [FreeFunction("ModuleMetadataBindings::GetModuleIncludeSettingForObject")]
        extern internal static ModuleIncludeSetting GetModuleIncludeSettingForObject(Object o);

        [FreeFunction("ModuleMetadataBindings::GetModuleForObject")]
        extern internal static string GetModuleForObject(Object o);

        [FreeFunction("ModuleMetadataBindings::GetModuleTypeIndices")]
        extern internal static uint[] GetModuleTypeIndices(string moduleName);

        extern public static string GetICallModule(string icall);
    }
}
