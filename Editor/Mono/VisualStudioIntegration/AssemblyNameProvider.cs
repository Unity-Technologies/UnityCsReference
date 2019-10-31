// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Compilation;

namespace UnityEditor.VisualStudioIntegration
{
    interface IAssemblyNameProvider
    {
        string GetAssemblyNameFromScriptPath(string path);
        IEnumerable<Compilation.Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution);
        IEnumerable<string> GetAllAssetPaths();
        UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath);
        ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
    }

    class AssemblyNameProvider : IAssemblyNameProvider
    {
        public string GetAssemblyNameFromScriptPath(string path)
        {
            return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
        }

        public IEnumerable<Compilation.Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
        {
            return CompilationPipeline.GetAssemblies()
                .Where(i => 0 < i.sourceFiles.Length && i.sourceFiles.Any(shouldFileBePartOfSolution));
        }

        public IEnumerable<string> GetAllAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths();
        }

        public UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
        }

        public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return CompilationPipeline.ParseResponseFile(
                responseFilePath,
                projectDirectory,
                systemReferenceDirectories
            );
        }
    }
}
