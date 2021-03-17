// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriver
    {
        public static BeeBuildProgramCommon.Data.PackageInfo[] GetPackageInfos(string projectDirectory)
        {
            return PackageManager.PackageInfo.GetAll().Select(p =>
            {
                NPath resolvedPath = new NPath(p.resolvedPath);
                if (resolvedPath.IsChildOf(projectDirectory))
                    resolvedPath = resolvedPath.RelativeTo(projectDirectory);

                return new BeeBuildProgramCommon.Data.PackageInfo()
                {
                    Name = p.name,
                    ResolvedPath = resolvedPath.ToString(),
                    Immutable = (p.source != PackageSource.Embedded && p.source != PackageSource.Local)
                };
            }).ToArray();
        }

        public static BeeDriver Make(RunnableProgram buildProgram, EditorCompilation editorCompilation, string dagName, string dagDirectory = null, bool useScriptUpdater = true)
        {
            return Make(buildProgram, dagName, dagDirectory, useScriptUpdater, editorCompilation.projectDirectory);
        }

        public static BeeDriver Make(RunnableProgram buildProgram, string dagName,
            string dagDirectory, bool useScriptUpdater, string projectDirectory, ProgressAPI progressAPI = null)
        {
            var sourceFileUpdaters = useScriptUpdater
                ? new[] {new UnityScriptUpdater(projectDirectory)}
            : Array.Empty<SourceFileUpdaterBase>();

            var processSourceFileUpdatersResult = new UnitySourceFileUpdatersResultHandler();

            NPath dagDir = dagDirectory ?? "Library/Bee";
            dagDir.CreateDirectory();
            var result = new BeeDriver(buildProgram, UnityBeeBackendProgram(), projectDirectory, dagName, dagDir.ToString(), sourceFileUpdaters, processSourceFileUpdatersResult, progressAPI ?? new UnityProgressAPI("Script Compilation"), profilerOutputFile: $"{dagDir}/fullprofile.json");

            result.DataForBuildProgram.Add(new BeeBuildProgramCommon.Data.ConfigurationData
            {
                Il2CppDir = IL2CPPUtils.GetIl2CppFolder(),
                NetCoreRunPath = NetCoreRunProgram.NetCoreRunPath,
                EditorContentsPath = EditorApplication.applicationContentsPath,
                ProjectDirectory = projectDirectory,
                Packages = UnityBeeDriver.GetPackageInfos(NPath.CurrentDirectory.ToString()),
                UnityVersion = Application.unityVersion,
                UnitySourceCodePath = Unsupported.IsSourceBuild() ? Unsupported.GetBaseUnityDeveloperFolder() : null
            });
            return result;
        }

        private static RunnableProgram UnityBeeBackendProgram()
        {
            var executable = new NPath($"{EditorApplication.applicationContentsPath}/bee_backend{BeeScriptCompilation.ExecutableExtension}").ToString();
            return new SystemProcessRunnableProgram(executable, alwaysEnvironmentVariables: new Dictionary<string, string>() {{ "BEE_CACHE_BEHAVIOUR", "_"}}, stdOutMode: StdOutMode.LogStdOutOnFinish);
        }
    }
}
