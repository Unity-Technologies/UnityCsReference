// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    internal enum ScriptTemplate
    {
        CSharp_NewBehaviourScript = 0,
        CSharp_NewTestScript,
        Shader_NewSurfaceShader,
        Shader_NewUnlitShader,
        Shader_NewImageEffectShader,
        CSharp_NewStateMachineBehaviourScript,
        CSharp_NewSubStateMachineBehaviourScript,
        CSharp_NewPlayableBehaviour,
        CSharp_NewPlayableAsset,
        Shader_NewComputeShader,
        AsmDef_NewAssembly,
        AsmDef_NewEditModeTestAssembly,
        AsmDef_NewTestAssembly,
        AsmRef_NewAssemblyReference,
        Shader_NewRayTracingShader,
        Count
    }

    [NativeHeader("Editor/Src/Commands/AssetsMenuUtility.h")]
    internal static class AssetsMenuUtility
    {
        public static extern bool SelectionHasImmutable();
        public static string GetScriptTemplatePath(ScriptTemplate template)
        {
            return GetScriptTemplatePath((int)template);
        }

        private static extern string GetScriptTemplatePath(int index);
    }
}
