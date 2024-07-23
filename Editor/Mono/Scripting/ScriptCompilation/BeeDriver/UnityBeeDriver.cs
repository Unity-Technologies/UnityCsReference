// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bee.BeeDriver;
using BeeBuildProgramCommon.Data;
using NiceIO;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriver
    {
        internal static readonly string BeeBackendExecutable = new NPath($"{EditorApplication.applicationContentsPath}/bee_backend{BeeScriptCompilation.ExecutableExtension}").ToString();

        [Serializable]
        internal class BeeBackendInfo
        {
            public string UnityVersion;
            public string BeeBackendHash;
        }

        internal static string BeeBackendHash
        {
            get
            {
                // Using SessionState, that way we won't need to rehash on domain reload.
                var hash = SessionState.GetString(nameof(BeeBackendHash), string.Empty);
                if (!string.IsNullOrEmpty(hash))
                    return hash;

                using var hasher = new SHA256Managed();
                using var stream = File.OpenRead(BeeBackendExecutable);
                var bytes = hasher.ComputeHash(stream);

                var sb = new StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                hash = sb.ToString();

                SessionState.SetString(nameof(BeeBackendHash), hash);

                return hash;
            }
        }

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

        private static void RecreateDagDirectoryIfNeeded(NPath dagDirectory)
        {
            var beeBackendInfoPath = dagDirectory.Combine("bee_backend.info");
            var currentInfo = new BeeBackendInfo()
            {
                BeeBackendHash = BeeBackendHash,
                UnityVersion = Application.unityVersion
            };

            var diskInfo = new BeeBackendInfo();

            // Clear dag directory if it was produced with a different bee_backend, to avoid problem where bee_backend sometimes looses track of files.
            if (dagDirectory.Exists())
            {
                // When used DeleteMode.Normal, it sometimes was causing an error on Windows:
                //    Win32Exception: The directory is not empty.
                //  at NiceIO.NPath + WindowsFileSystem.Directory_Delete(NiceIO.NPath path, System.Boolean recursive)[0x000f4] in C:\buildslave\unity\build\External\NiceIO\NiceIO.cs:1792

                // Since we're recreating a directory anyways, using DeleteMode.Soft should be fine.
                var deleteMode = DeleteMode.Soft;
                if (beeBackendInfoPath.Exists())
                {
                    var contents = beeBackendInfoPath.ReadAllText();
                    EditorJsonUtility.FromJsonOverwrite(contents, diskInfo);

                    // Note: We're clearing dag directory only when bee backend hash has changed, it's fine for Unity version to be different.
                    //       Unity version is used here for informational purposes, so we can clearly see from which Unity version the user was upgrading
                    if (string.IsNullOrEmpty(diskInfo.BeeBackendHash) ||
                        !diskInfo.BeeBackendHash.Equals(currentInfo.BeeBackendHash))
                    {
                        Console.WriteLine($"Clearing Bee directory '{dagDirectory}', since bee backend hash ('{beeBackendInfoPath}') is different, previous hash was {diskInfo.BeeBackendHash} (Unity version: {diskInfo.UnityVersion}), current hash is {currentInfo.BeeBackendHash} (Unity version: {currentInfo.UnityVersion}).");
                        dagDirectory.Delete(deleteMode);
                    }
                }
                else
                {
                    Console.WriteLine($"Clearing Bee directory '{dagDirectory}', since bee backend information ('{beeBackendInfoPath}') is missing.");
                    dagDirectory.Delete(deleteMode);
                }
            }

            dagDirectory.CreateDirectory();

            // Update info, if at least of one the fields is different
            if (string.IsNullOrEmpty(diskInfo.BeeBackendHash) ||
                string.IsNullOrEmpty(diskInfo.UnityVersion) ||
                !diskInfo.BeeBackendHash.Equals(currentInfo.BeeBackendHash) ||
                !diskInfo.UnityVersion.Equals(currentInfo.UnityVersion))
            {
                beeBackendInfoPath.WriteAllText(EditorJsonUtility.ToJson(currentInfo, true));
            }
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

            RecreateDagDirectoryIfNeeded(dagDir);
            NPath profilerOutputFile = UnityBeeDriverProfilerSession.GetTraceEventsOutputForNewBeeDriver() ?? $"{dagDir}/fullprofile.json";
            var result = new BeeDriver(buildProgram, UnityBeeBackendProgram(), projectDirectory, dagName, dagDir.ToString(), sourceFileUpdaters, processSourceFileUpdatersResult, progressAPI ?? new UnityProgressAPI("Script Compilation"), profilerOutputFile: profilerOutputFile.ToString());

            result.DataForBuildProgram.Add(new BeeBuildProgramCommon.Data.ConfigurationData
            {
                Il2CppDir = IL2CPPUtils.GetIl2CppFolder(),
                Il2CppPath = IL2CPPUtils.GetExePath("il2cpp"),
                UnityLinkerPath = IL2CPPUtils.GetExePath("UnityLinker"),
                NetCoreRunPath = NetCoreRunProgram.NetCoreRunPath,
                EditorContentsPath = EditorApplication.applicationContentsPath,
                Packages = UnityBeeDriver.GetPackageInfos(NPath.CurrentDirectory.ToString()),
                UnityVersion = Application.unityVersion,
                UnitySourceCodePath = Unsupported.IsSourceBuild() ? Unsupported.GetBaseUnityDeveloperFolder() : null,
                AdvancedLicense = PlayerSettings.advancedLicense,
                EmitDataForBeeWhy = (Debug.GetDiagnosticSwitch("EmitDataForBeeWhy").value as bool?)?? false,
            });
            return result;
        }

        private static RunnableProgram UnityBeeBackendProgram()
        {
            return new SystemProcessRunnableProgram(BeeBackendExecutable, alwaysEnvironmentVariables: new Dictionary<string, string>()
            {
                { "BEE_CACHE_BEHAVIOUR", "_"},
                { "CHROMETRACE_TIMEOFFSET","unixepoch"}
            }, stdOutMode: StdOutMode.LogStdOutOnFinish | StdOutMode.LogStartArgumentsAndExitcode);
        }
    }
}
