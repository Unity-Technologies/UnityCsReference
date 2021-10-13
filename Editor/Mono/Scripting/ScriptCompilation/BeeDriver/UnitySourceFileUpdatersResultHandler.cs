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

        private readonly UnityScriptUpdaterConsentAPI ConstentAPI;

        public UnitySourceFileUpdatersResultHandler()
        {
            ConstentAPI = new UnityScriptUpdaterConsentAPI();
        }

        public override void ProcessUpdaterResults(SourceFileUpdaterBase.Update[] updates)
        {
            NPath libraryPackageCache = "Library/PackageCache";

            var problemUpdates = new List<(SourceFileUpdaterBase.Update update, Exception exception)>();

            void ExecuteUpdates(IEnumerable<SourceFileUpdaterBase.Update> updates)
            {
                foreach (var update in updates)
                {
                    try
                    {
                        Console.WriteLine(update.originalFileWithError);
                        new NPath(update.tempFileWithNewContents).Copy(update.originalFileWithError);
                    }
                    catch (Exception e)
                    {
                        problemUpdates.Add((update, e));
                    }
                }
            }

            var packageResolvePathsAndNames = PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p => new NPath(p.resolvedPath).ToString().Contains("Library/PackageCache")).Select(p => (p.resolvedPath, p.name)).ToArray();

            string VirtualizedPathFor(NPath file)
            {
                foreach (var packageResolvePathAndName in packageResolvePathsAndNames)
                    if (file.IsChildOf(packageResolvePathAndName.resolvedPath))
                        return $"Packages/{packageResolvePathAndName.name}/{file.RelativeTo(packageResolvePathAndName.resolvedPath)}";
                throw new ArgumentException($"Failed to virtualize path: {file}");
            }

            var(immutablePackageUpdates, nonImmutableUpdates) = updates.SplitBy(u => new NPath(u.originalFileWithError).IsChildOf(libraryPackageCache));

            Console.WriteLine("[API Updater] Updated Files:");
            if (immutablePackageUpdates.Any())
            {
                var virtualizedPackageFiles = immutablePackageUpdates.Select(u => VirtualizedPathFor(u.originalFileWithError)).ToArray();
                ImmutableAssets.SetAssetsAllowedToBeModified(virtualizedPackageFiles);
                ExecuteUpdates(immutablePackageUpdates);
            }

            if (nonImmutableUpdates.Any())
            {
                var nonImmutableTargetFiles = nonImmutableUpdates.Select(u => new NPath(u.originalFileWithError)).ToArray();

                if (MayOverwrite(nonImmutableTargetFiles) && PrepareForOverwritingUpdatedFiles(nonImmutableTargetFiles))
                    ExecuteUpdates(nonImmutableUpdates);
            }

            if (problemUpdates.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Unable to update the following files. Are they marked readonly?");
                foreach (var problem in problemUpdates)
                    sb.AppendLine(problem.update.originalFileWithError + " " + problem.exception.Message);

                Debug.LogError(sb.ToString());
            }

            Console.WriteLine("Finished running ScriptUpdaters");
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool PrepareForOverwritingUpdatedFiles(NPath[] destFiles)
        {
            if (!APIUpdaterManager.WaitForVCSServerConnection())
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
