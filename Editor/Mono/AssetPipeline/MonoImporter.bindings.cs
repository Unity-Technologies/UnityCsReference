// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/AssetPipeline/MonoImporter.bindings.h")]
    public class MonoImporter : AssetImporter
    {
        public extern void SetDefaultReferences(string[] name, Object[] target);

        [FreeFunction]
        public static extern MonoScript[] GetAllRuntimeMonoScripts();

        [FreeFunction("SetMonoScriptExecutionOrder")]
        public static extern void SetExecutionOrder(MonoScript script, int order);

        // Call when icon set by SetIconForObject should be copied to monoImporter for persistance across project reloads
        // This function will reimport the asset and is therefore slow.
        [FreeFunction]
        internal static extern void CopyMonoScriptIconToImporters(MonoScript script);

        [FreeFunction]
        public static extern int GetExecutionOrder(MonoScript script);

        public extern MonoScript GetScript();

        public extern Object GetDefaultReference(string name);
    }
}
