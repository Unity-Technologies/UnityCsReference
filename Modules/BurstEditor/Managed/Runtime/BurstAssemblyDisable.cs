// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Allow disabling of the Burst compiler for specific assemblies.
    ///  ProjectSettings/Burst_DisableAssembliesForEditorCompilation.json
    ///  ProjectSettings/Burst_DisableAssembliesForPlayerCompilation.json
    ///  ProjectSettings/Burst_DisableAssembliesForPlayerCompilation_{platform}.json // if exists taken in preference to the one immediately above
    /// </summary>
    internal static class BurstAssemblyDisable
    {
        public enum DisableType
        {
            Editor,
            Player
        }

        private static string GetPath(DisableType type, string platformIdentifier)
        {
            if (DisableType.Editor == type)
            {
                return "ProjectSettings/Burst_DisableAssembliesForEditorCompilation.json";
            }
            var platformSpecicPath = $"ProjectSettings/Burst_DisableAssembliesForPlayerCompilation_{platformIdentifier}.json";
            if (File.Exists(platformSpecicPath))
            {
                return platformSpecicPath;
            }
            return "ProjectSettings/Burst_DisableAssembliesForPlayerCompilation.json";
        }

        public static string[] GetDisabledAssemblies(DisableType type, string platformIdentifier)
        {
            var pathForSettings = GetPath(type, platformIdentifier);
            if (!File.Exists(pathForSettings))
            {
                return Array.Empty<string>();
            }

            var settings = new BackwardsCompatWrapper();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(pathForSettings), settings);
            if (settings == null || settings.MonoBehaviour == null || settings.MonoBehaviour.DisabledAssemblies == null)
            {
                return Array.Empty<string>();
            }
            return settings.MonoBehaviour.DisabledAssemblies;
        }
    }

    /// <summary>
    /// Settings file -
    ///
    ///{
    /// "MonoBehaviour": {
    ///  "DisabledAssemblies":
    ///	  [
    ///	   "Example.Assembly"
    ///   ]
    /// }
    ///} 
    /// </summary>
    [Serializable]
    class BackwardsCompatWrapper
    {
        public BurstDisableSettings MonoBehaviour;
    }
    [Serializable]
    class BurstDisableSettings
    {
        public string[] DisabledAssemblies;
    }
}
