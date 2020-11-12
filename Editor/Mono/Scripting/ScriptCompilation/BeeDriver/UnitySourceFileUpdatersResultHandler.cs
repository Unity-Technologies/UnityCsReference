// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.BeeDriver;
using NiceIO;
using UnityEditor.PackageManager;
using UnityEditor.ScriptUpdater;
using UnityEditorInternal.APIUpdating;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class UnitySourceFileUpdatersResultHandler : SourceFileUpdatersResultHandler
    {
        bool m_HaveConsentToOverwriteUserScripts;
        NPath ProjectRoot { get; }
        private readonly UnityScriptUpdaterConsentAPI ConstentAPI;

        public UnitySourceFileUpdatersResultHandler(NPath projectRoot)
        {
            ProjectRoot = projectRoot;
            ConstentAPI = new UnityScriptUpdaterConsentAPI();
        }

        struct UpdatedFile
        {
            public NPath virtualPath;
            public NPath projectRelativePhysicalPath;
            public NPath updatedPath;
        }

        public override void ProcessUpdaterResults(SourceFileUpdaterBase.Task[] succesfullyFinishedTasks)
        {
            var resolvedPathsToPackages = PackageManager.PackageInfo.GetAll().Where(LivesInPackageCache).ToDictionary(p => new NPath(p.resolvedPath).RelativeTo(NPath.CurrentDirectory), p => p);

            foreach (var task in succesfullyFinishedTasks)
            {
                var files = new NPath(task.TempOutputDirectory).Files(true);
                if (!files.Any())
                    continue;

                var firstFileProjectRelative = files.First().RelativeTo(task.TempOutputDirectory);
                var matchingPackage = resolvedPathsToPackages.FirstOrDefault(kvp => firstFileProjectRelative.IsChildOf(kvp.Key));

                var updatedFiles = files.Select(p =>
                {
                    var projectRelativePhysicalPath = p.RelativeTo(task.TempOutputDirectory);
                    return new UpdatedFile()
                    {
                        projectRelativePhysicalPath = projectRelativePhysicalPath,
                        virtualPath = matchingPackage.Value != null
                            ? new NPath($"Packages/{matchingPackage.Value.name}/{projectRelativePhysicalPath.RelativeTo(matchingPackage.Value.resolvedPath)}")
                            : null,
                        updatedPath = p
                    };
                }).ToArray();

                Console.WriteLine("[API Updater] Updated Files:");

                var updatedPackageCacheFiles = updatedFiles.Where(a => a.virtualPath != null).ToArray();
                if (updatedPackageCacheFiles.Any())
                {
                    ImmutableAssets.SetAssetsAllowedToBeModified(updatedPackageCacheFiles.Select(p => p.virtualPath.ToString()).ToArray());

                    foreach (var updatedPackageCacheFile in updatedPackageCacheFiles)
                    {
                        Console.WriteLine(updatedPackageCacheFile.projectRelativePhysicalPath);
                        updatedPackageCacheFile.updatedPath.Copy(ProjectRoot.Combine(updatedPackageCacheFile.projectRelativePhysicalPath));
                    }
                }

                var myNonPackageUpdatedFiles = updatedFiles.Where(a => a.virtualPath == null).ToArray();
                if (myNonPackageUpdatedFiles.Any())
                {
                    var relativeVersionedFiles = myNonPackageUpdatedFiles.Select(p => p.projectRelativePhysicalPath).ToArray();
                    if (MayOverwrite(relativeVersionedFiles))
                    {
                        if (PrepareForOverwritingUpdatedFiles(relativeVersionedFiles))
                        {
                            var problemFiles = new List<(NPath, Exception)>();
                            foreach (var updatedNonPackageFile in myNonPackageUpdatedFiles)
                            {
                                Console.WriteLine(updatedNonPackageFile.projectRelativePhysicalPath);
                                try
                                {
                                    updatedNonPackageFile.updatedPath.Copy(ProjectRoot.Combine(updatedNonPackageFile.projectRelativePhysicalPath));
                                }
                                catch (Exception e)
                                {
                                    problemFiles.Add((updatedNonPackageFile.projectRelativePhysicalPath, e));
                                }
                            }

                            if (problemFiles.Any())
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("Unable to update the following files. Are they marked readonly?");
                                foreach (var(file, exception) in problemFiles)
                                    sb.AppendLine($"{file} {exception.Message}");

                                Debug.LogError(sb.ToString());
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Finished running ScriptUpdaters");
        }

        private bool LivesInPackageCache(PackageManager.PackageInfo arg)
        {
            return new NPath(arg.resolvedPath).MakeAbsolute(NPath.CurrentDirectory).IsChildOf(NPath.CurrentDirectory.Combine("Library/PackageCache"));
        }

        bool MayOverwrite(NPath[] files)
        {
            if (m_HaveConsentToOverwriteUserScripts)
                return true;
            var result = ConstentAPI.AskFor(files);
            switch (result)
            {
                case ScriptUpdaterConsentType.ConsentOnce:
                    return true;
                case ScriptUpdaterConsentType.ConsentForRestOfCompilation:
                    m_HaveConsentToOverwriteUserScripts = true;
                    return true;
                case ScriptUpdaterConsentType.NoConsent:
                    return false;
                default:
                    throw new InvalidOperationException(result.ToString());
            }
        }

        private bool PrepareForOverwritingUpdatedFiles(NPath[] destFiles)
        {
            if (!APIUpdaterManager.WaitForVCSServerConnection(true))
            {
                //if we fail to connect to the vcs server, we shouldn't overwrite versioned files.
                //in this case we will just not do that, continue the build as normal. We have protection for only trying to script update
                //an assembly once, and the resulting behaviour will be that the user just gets to see the compiler errors and needs to fix
                //them herself.
                return false;
            }

            if (!AssetDatabase.MakeEditable(destFiles.Select(d => d.ToString()).ToArray()))
            {
                Debug.LogError($"Failed to make VCS provider make the scripts to be update editable.{Environment.NewLine}" + string.Join(Environment.NewLine, destFiles.Select(d => d.ToString())));
                return false;
            }

            return true;
        }

        internal enum ScriptUpdaterConsentType
        {
            ConsentOnce,
            ConsentForRestOfCompilation,
            NoConsent
        }
    }
}
