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
        public static extern void SetExecutionOrder([NotNull("NullExceptionObject")] MonoScript script, int order);

        // The same functionality is now available using public APIs, so this method should be removed.
        // As a warning to Asset Store developers, the underlying C++ function displays the same message as a warning,
        // since the [Obsolete] attribute will not be triggered when using this method via reflection.
        // TODO(@markv): remove this method in Unity 2022.2
        [System.Obsolete("CopyMonoScriptIconToImporters is deprecated and will be removed in a future version of Unity. Please use https://docs.unity3d.com/ScriptReference/MonoImporter.SetIcon.html or https://docs.unity3d.com/ScriptReference/PluginImporter.SetIcon.html instead.")]
        [FreeFunction("MonoImporterBindings::CopyMonoScriptIconToImporters")]
        internal static extern void CopyMonoScriptIconToImporters([NotNull("NullExceptionObject")] MonoScript script);

        [FreeFunction("MonoImporterBindings::GetExecutionOrder")]
        public static extern int GetExecutionOrder([NotNull("NullExceptionObject")] MonoScript script);

        public extern MonoScript GetScript();

        public extern Object GetDefaultReference(string name);

        public extern void SetIcon(Texture2D icon);
        public extern Texture2D GetIcon();
    }
}
