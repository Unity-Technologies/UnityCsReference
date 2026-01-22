// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Player;
using UnityEditor.VisualStudioIntegration;
using UnityEngine;

namespace UnityEditor
{
    internal class BuildPlayerDataExtractor
    {
        private readonly IDirectoryIO m_DirectoryIO;
        private readonly IFileIO m_FileIO;

        public BuildPlayerDataExtractor()
        {
            m_DirectoryIO = new DirectoryIOProvider();
            m_FileIO = new FileIOProvider();
        }

        public AssemblyInfoManaged[] ExtractAssemblyTypeInfo(bool isEditor)
        {
            var buildPlayerDataGeneratorHelper = new BuildPlayerDataGenerator();
            return ExtractAssemblyTypeInfoFromFiles(buildPlayerDataGeneratorHelper.GetTypeDbFilePaths(isEditor));
        }

        public  AssemblyInfoManaged[] ExtractAssemblyTypeInfoFromFiles(string[] typeDbJsonPaths)
        {
            List<AssemblyInfoManaged> assemblyInfoResults = new List<AssemblyInfoManaged>(typeDbJsonPaths.Length);
            foreach (var typeDbFile in typeDbJsonPaths)
            {
                var assemblyInfos = JsonUtility.FromJson<ExtractRoot<AssemblyInfoManaged>>(File.ReadAllText(typeDbFile));
                assemblyInfoResults.AddRange(assemblyInfos.root);
            }
            return assemblyInfoResults.ToArray();
        }

        public void ExtractPlayerRuntimeInitializeOnLoadMethods(string jsonPath)
        {
            m_DirectoryIO.CreateDirectory(jsonPath);

            var path = Path.Combine(BuildPlayerDataGenerator.OutputPath, "Player");
            var sourceRuntimeInitializeOnLoads = Path.Combine(path, "RuntimeInitializeOnLoads.json");
            if (!m_DirectoryIO.Exists(path) || !m_FileIO.Exists(sourceRuntimeInitializeOnLoads))
            {
                return;
            }

            m_FileIO.Copy(sourceRuntimeInitializeOnLoads, Path.Combine(jsonPath, "RuntimeInitializeOnLoads.json"), true);
        }
    }
}
