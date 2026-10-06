// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/MonoImporter.bindings.h")]
    [global::UnityEngine.NativeClass("MonoImporter", PersistentTypeId = 1035)]
    [ExcludeFromPreset]
    public class MonoImporter : AssetImporter
    {
        internal MonoImporter(global::UnityEngine.EntityId id) : base(id) {}
        public MonoImporter() {}
        public extern void SetDefaultReferences(string[] name, Object[] target);

        [FreeFunction("MonoImporterBindings::GetDefaultReferencesInternal")]
        internal static extern void GetDefaultReferencesInternal([NotNull] MonoScript script, [Out,NotNull] List<string> names, [Out,NotNull] List<Object> targets);

        [FreeFunction("MonoImporterBindings::GetAllRuntimeMonoScripts")]
        public static extern MonoScript[] GetAllRuntimeMonoScripts();

        [FreeFunction("MonoImporterBindings::SetMonoScriptExecutionOrder")]
        public static extern void SetExecutionOrder([NotNull] MonoScript script, int order);

        [FreeFunction("MonoImporterBindings::GetExecutionOrder")]
        public static extern int GetExecutionOrder([NotNull] MonoScript script);

        public extern MonoScript GetScript();

        public Object GetDefaultReference(string name)
        {
            return GetDefaultReference(name, out _);
        }
        internal extern Object GetDefaultReference(string name, out EntityId instanceId);

        public extern void SetIcon(Texture2D icon);
        public extern Texture2D GetIcon();
    }
}
