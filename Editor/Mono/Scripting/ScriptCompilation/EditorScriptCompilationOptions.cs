// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Keep in sync with EditorScriptCompilationOptions in native
    [VisibleToOtherModules("UnityEditor.BurstModule")]
    [Flags]
    enum EditorScriptCompilationOptions
    {
        BuildingEmpty = 0,
        // 1 << 0 was BuildingDevelopmentBuild; removed, the Development Build setting no longer affects script compilation.
        BuildingForEditor = 1 << 1,
        BuildingEditorOnlyAssembly = 1 << 2,
        BuildingForIl2Cpp = 1 << 3,
        BuildingWithAsserts = 1 << 4,
        BuildingIncludingTestAssemblies = 1 << 5,
        // 1 << 6 was BuildingPredefinedAssembliesAllowUnsafeCode; removed — unsafe is always enabled.
        BuildingForHeadlessPlayer = 1 << 7,
        BuildingUseDeterministicCompilation = 1 << 9,
        BuildingWithoutScriptUpdater = 1 << 11,
        BuildingExtractTypeDB = 1 << 12,
        BuildingSkipCompile = 1 << 13,
        BuildingCleanCompilation = 1 << 14,
        BuildingWithoutOptimization = 1 << 15,
        BuildingWithInstrumentation = 1 << 16,
    }
}
