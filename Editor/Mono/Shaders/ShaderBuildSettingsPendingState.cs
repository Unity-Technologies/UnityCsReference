// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Shaders
{
    // Object used to handle undo/redo operations.
    class ShaderBuildSettingsPendingState : ScriptableObject
    {
        [SerializeField] internal bool m_FastBuildEnabled;
        [SerializeField] internal List<ShaderBuildSettings.KeywordDeclarationOverride> m_KeywordDeclarationOverrides = new();
        [SerializeField] internal List<string> m_ShaderConstantDefines = new();
        [SerializeField] internal List<ShaderBuildSettings.ShaderCompilerSettings> m_CompilerBackendSettings = new();
        [SerializeField] internal bool m_EnableDebugSymbols;
        [SerializeField] internal ShaderBuildSettings.ShaderOptimizationLevel m_OptimizationLevel;
        [SerializeField] internal ShaderBuildSettings.ShaderOptimizationLevel m_OptLevelBeforeDebugForced;
    }
}
