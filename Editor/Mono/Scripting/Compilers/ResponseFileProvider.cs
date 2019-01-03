// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class ResponseFileProvider
    {
        private const string k_AssetsFolder = "Assets";
        public abstract string ResponseFileName { get; }

        public abstract string[] ObsoleteResponseFileNames { get; }

        public string ProjectPath
        {
            get; set;
        }

        protected ResponseFileProvider()
        {
            var dataPath = Application.dataPath;
            ProjectPath = Path.GetDirectoryName(dataPath);
        }

        public List<string> Get(string folderToLookForResponseFilesIn)
        {
            if (!string.IsNullOrEmpty(folderToLookForResponseFilesIn) && !Path.IsPathRooted(folderToLookForResponseFilesIn))
            {
                folderToLookForResponseFilesIn = AssetPath.Combine(ProjectPath, folderToLookForResponseFilesIn);
            }

            var result = new List<string>();

            var folderResponseFile = GetCompilerSpecific(folderToLookForResponseFilesIn);
            if (!string.IsNullOrEmpty(folderResponseFile))
            {
                AddIfNotNull(result, folderResponseFile);
            }
            else
            {
                AddIfNotNull(result, GetDefaultResponseFiles());
            }

            return result;
        }

        protected string GetCompilerSpecific(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            //We only look for the specific response file in the folder.
            var responseFilePath = AssetPath.Combine(path, ResponseFileName);

            if (File.Exists(responseFilePath))
            {
                return responseFilePath;
            }
            return null;
        }

        protected string GetDefaultResponseFiles()
        {
            var rootResponseFilePath = AssetPath.Combine(ProjectPath, k_AssetsFolder, ResponseFileName);
            if (File.Exists(rootResponseFilePath))
            {
                return rootResponseFilePath;
            }

            foreach (var obsoleteResponseFileName in ObsoleteResponseFileNames)
            {
                var obsoleteResponseFilePath = AssetPath.Combine(ProjectPath, k_AssetsFolder, obsoleteResponseFileName);
                if (File.Exists(obsoleteResponseFilePath))
                {
                    return obsoleteResponseFilePath;
                }
            }
            return null;
        }

        private static void AddIfNotNull<T>(List<T> list, T element)
        {
            if (element != null)
            {
                list.Add(element);
            }
        }
    }
}
