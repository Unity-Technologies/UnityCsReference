// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Compilation;
using UnityEditor.MSBuild;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

enum CompileTarget
{
    // Maps to the Debug configuration used by the Editor
    EditorDebug,
    // Maps to the Release configuration used by the Editor
    EditorRelease,
    PlayerDebug,
    PlayerRelease,
    PlayerWithTests
}

class MsBuildCompilation
{
    public void Initialize(bool createInitCsprojs, MSBuildCompilationOptions compilationOptions)
    {
        if (createInitCsprojs)
            ProjectGenerator.Instance.GenerateUnityProjectIfMissing(Path.Combine("Assets", "Runtime", "Runtime.csproj"));

        UnityEditorMSBuildPropsTargetsGeneration.UpdateGeneratedMSBuildFileIfNeeded(EditorUserBuildSettings.activeBuildTarget, compilationOptions);
    }

    public void RequestMsBuildScriptCompilation(bool restore, string reason = null)
    {
        throw new NotImplementedException();
    }

    public bool HaveScriptsForEditorBeenCompiledSinceLastDomainReload()
    {
        throw new NotImplementedException();
    }

    public bool IsCompiling()
    {
        throw new NotImplementedException();
    }

    public EditorCompilation.CompileStatus Compile(BuildTarget buildTarget, CompileTarget target, MSBuildCompilationOptions compilationOptions)
    {
        throw new NotImplementedException();
    }

    public bool TryGetLastBuildResult(CompileTarget target, out CompilationDoneResult? result)
    {
        throw new NotImplementedException();
    }

    public EditorCompilation.CompileStatus TickCompilationPipeline(BuildTarget buildTarget, bool allowBlocking, CompileTarget target, MSBuildCompilationOptions compilationOptions)
    {
        throw new NotImplementedException();
    }

    private static bool IsEditorTarget(CompileTarget target)
    {
        throw new NotImplementedException();
    }

    private static string GetMsBuildConfiguration(CompileTarget target, BuildTarget buildTarget)
    {
        throw new NotImplementedException();
    }
}
