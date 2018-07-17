// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.bindings.h")]
    [ExcludeFromPreset]
    public class MonoImporter : AssetImporter
    {
        public extern void SetDefaultReferences(string[] name, Object[] target);

        [FreeFunction("MonoImporterBindings::GetAllRuntimeMonoScripts")]
        public static extern MonoScript[] GetAllRuntimeMonoScripts();

        [FreeFunction("MonoImporterBindings::SetMonoScriptExecutionOrder")]
        public static extern void SetExecutionOrder(MonoScript script, int order);

        // Call when icon set by SetIconForObject should be copied to monoImporter for persistence across project reloads
        // This function will reimport the asset and is therefore slow.
        [FreeFunction("MonoImporterBindings::CopyMonoScriptIconToImporters")]
        internal static extern void CopyMonoScriptIconToImporters(MonoScript script);

        [FreeFunction("MonoImporterBindings::GetExecutionOrder")]
        public static extern int GetExecutionOrder(MonoScript script);

        public extern MonoScript GetScript();

        public extern Object GetDefaultReference(string name);
    }
}
