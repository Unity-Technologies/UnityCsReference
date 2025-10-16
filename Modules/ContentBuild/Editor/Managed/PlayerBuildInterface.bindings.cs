// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEditor.Build.Player
{
    public enum ScriptCompilationOptions
    {
        None = 0,
        DevelopmentBuild = 1 << 0,
        Assertions = 1 << 1
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScriptCompilationSettings
    {
        [NativeName("subtarget")]
        internal int m_Subtarget;
        public int subtarget
        {
            get { return m_Subtarget; }
            set { m_Subtarget = value; }
        }

        [NativeName("target")]
        internal BuildTarget m_Target;
        public BuildTarget target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        [NativeName("group")]
        internal BuildTargetGroup m_Group;
        public BuildTargetGroup group
        {
            get { return m_Group; }
            set { m_Group = value; }
        }

        [NativeName("options")]
        internal ScriptCompilationOptions m_Options;
        public ScriptCompilationOptions options
        {
            get { return m_Options; }
            set { m_Options = value; }
        }

        [NativeName("resultTypeDB")]
        internal TypeDB m_ResultTypeDB;

        [NativeName("extraScriptingDefines")]
        internal string[] m_ExtraScriptingDefines;
        public string[] extraScriptingDefines
        {
            get { return m_ExtraScriptingDefines; }
            set { m_ExtraScriptingDefines = value; }
        }
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScriptCompilationResult
    {
        [NativeName("assemblies")]
        internal string[] m_Assemblies;
        public ReadOnlyCollection<string> assemblies { get { return Array.AsReadOnly(m_Assemblies); } }

        internal bool success;

        [Ignore]
        internal TypeDB m_TypeDB;
        public TypeDB typeDB { get { return m_TypeDB; } }
    }

    [NativeHeader("Modules/ContentBuild/Editor/Public/PlayerBuildInterface.h")]
    public static class PlayerBuildInterface
    {
        public static Func<IEnumerable<string>> ExtraTypesProvider;

        [FreeFunction(Name = "BuildPipeline::CompilePlayerScripts")]
        extern private static ScriptCompilationResult CompilePlayerScriptsNative(ScriptCompilationSettings input, string outputFolder, bool editorScripts);

        //Exposed for Tests and SBP BuildPlayerScripts task.
        //Advanced users could potentially use this, to be able to trigger a player compilation step in isolation from a full build.
        public static ScriptCompilationResult CompilePlayerScripts(ScriptCompilationSettings input, string outputFolder)
        {
            return CompilePlayerScriptsInternal(input, outputFolder, false);
        }

        // Signature exposed for testing a "editor" compilation, see CompilePlayerScriptsOutputTests (note, it is confusing to use a method called "CompilePlayerScripts" to invoke CompileScriptsForEditorSync!)
        internal static ScriptCompilationResult CompilePlayerScriptsInternal(ScriptCompilationSettings input, string outputFolder, bool editorScripts)
        {
            input.m_ResultTypeDB = new TypeDB();
            ScriptCompilationResult result = CompilePlayerScriptsNative(input, outputFolder, editorScripts);
            result.m_TypeDB = result.m_Assemblies.Length != 0 ? input.m_ResultTypeDB : null;
            return result;
        }
    }
}
