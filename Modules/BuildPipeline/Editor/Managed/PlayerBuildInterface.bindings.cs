// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.Player
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
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScriptCompilationResult
    {
        [NativeName("assemblies")]
        internal string[] m_Assemblies;
        public ReadOnlyCollection<string> assemblies { get { return Array.AsReadOnly(m_Assemblies); } }

        [Ignore]
        internal TypeDB m_TypeDB;
        public TypeDB typeDB { get { return m_TypeDB; } }
    }

    [NativeHeader("Modules/BuildPipeline/Editor/Public/PlayerBuildInterface.h")]
    public class PlayerBuildInterface
    {
        [FreeFunction(Name = "BuildPipeline::CompilePlayerScripts")]
        extern private static ScriptCompilationResult CompilePlayerScriptsNative(ScriptCompilationSettings input, string outputFolder, bool editorScripts);

        public static ScriptCompilationResult CompilePlayerScripts(ScriptCompilationSettings input, string outputFolder)
        {
            return CompilePlayerScriptsInternal(input, outputFolder, false);
        }

        internal static ScriptCompilationResult CompilePlayerScriptsInternal(ScriptCompilationSettings input, string outputFolder, bool editorScripts)
        {
            input.m_ResultTypeDB = new TypeDB();
            ScriptCompilationResult result = CompilePlayerScriptsNative(input, outputFolder, editorScripts);
            result.m_TypeDB = result.m_Assemblies.Length != 0 ? input.m_ResultTypeDB : null;
            return result;
        }
    }
}
