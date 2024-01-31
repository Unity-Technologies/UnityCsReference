// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    internal enum ScriptTemplate
    {
        CSharp_NewBehaviourScript = 0,
        CSharp_NewPlayableBehaviour,
        Shader_NewSurfaceShader,
        CSharp_NewSceneTemplatePipelineScript,
        CSharp_NewPlayableAsset,
        CSharp_NewScriptableObjectScript,
        Shader_NewUnlitShader,
        AsmDef_NewAssembly,
        AsmDef_NewEditModeTestAssembly,
        AsmDef_NewTestAssembly,
        AsmRef_NewAssemblyReference,
        CSharp_NewEmptyScript,
        CSharp_NewTestScript,
        Shader_NewImageEffectShader,
        Shader_NewComputeShader,
        Shader_NewRayTracingShader,
        CSharp_NewStateMachineBehaviourScript,
        CSharp_NewSubStateMachineBehaviourScript,
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
