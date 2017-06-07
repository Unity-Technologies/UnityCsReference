// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.Player
{
    public enum ScriptCompilationOptions
    {
        None = 0,
        DevelopmentBuild = 1 << 0,
        Assertions = 1 << 1
    }

    [UsedByNativeCode]
    [Serializable]
    public struct ScriptCompilationSettings
    {
        public string outputFolder;
        public BuildTarget target;
        public BuildTargetGroup targetGroup;
        public ScriptCompilationOptions options;
        internal TypeDB resultTypeDB;
    }

    [UsedByNativeCode]
    [Serializable]
    public struct ScriptCompilationResult
    {
        public string[] assemblies;
        [Ignore]
        public TypeDB typeDB;
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Public/PlayerBuildInterface.h")]
    public class PlayerBuildInterface
    {
        [FreeFunction(Name = "BuildPipeline::CompilePlayerScripts")]
        extern private static ScriptCompilationResult CompilePlayerScriptsNative(ScriptCompilationSettings input, bool editorScripts);

        public static ScriptCompilationResult CompilePlayerScripts(ScriptCompilationSettings input)
        {
            return CompilePlayerScriptsInternal(input, false);
        }

        internal static ScriptCompilationResult CompilePlayerScriptsInternal(ScriptCompilationSettings input, bool editorScripts)
        {
            input.resultTypeDB = new TypeDB();
            ScriptCompilationResult result = CompilePlayerScriptsNative(input, editorScripts);
            result.typeDB = result.assemblies.Length != 0 ? input.resultTypeDB : null;
            return result;
        }
    }
}
