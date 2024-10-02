// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using System;

namespace UnityEditor.Build
{
    internal delegate void GetScriptCompilationDefinesDelegate(BuildTarget target, HashSet<string> defines);

    [RequiredByNativeCode]
    internal class BuildDefines
    {
        public static event GetScriptCompilationDefinesDelegate getScriptCompilationDefinesDelegates;

        [RequiredByNativeCode]
        public static string[] GetScriptCompilationDefines(BuildTarget target, string[] defines)
        {
            var hashSet = new HashSet<string>(defines);
            if (getScriptCompilationDefinesDelegates != null)
                getScriptCompilationDefinesDelegates(target, hashSet);
            var array = new string[hashSet.Count];
            hashSet.CopyTo(array);
            return array;
        }

        [RequiredByNativeCode]
        public static string[] GetBuildProfileScriptDefines()
        {
            // Profile scripting defines are cached when profile is first activated
            // and when the domain is unloaded. Workaround for active build profile
            // not reachable when AssetDatabase.IsReadOnly() is true.
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return EditorUserBuildSettings.GetActiveProfileScriptingDefines();

            var profile = EditorUserBuildSettings.activeBuildProfile;
            if (profile == null)
                return EditorUserBuildSettings.GetActiveProfileScriptingDefines();

            return profile.scriptingDefines;
        }
    }
}
