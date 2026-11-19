// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using NiceIO;
using UnityEditor.Scripting;
using UnityEditorInternal.APIUpdating;
using UnityEngine;

namespace UnityEditor.ScriptUpdater
{
    // A source file the script updater fixed: the new contents, and the file they replace.
    readonly struct ScriptUpdate
    {
        public readonly string originalFileRelativePath;
        public readonly string updatedFileFullPath;

        public ScriptUpdate(string originalFile, string updatedFile)
        {
            originalFileRelativePath = originalFile;
            updatedFileFullPath = updatedFile;
        }
    }

    // Applies script updater results for both compilation pipelines (BeeDriver and MSBuild):
    // asks for consent, handles version control and immutable packages, then overwrites the files.
    // Create one per compilation; "Yes, for these and other files" consent lasts as long as it does.
    class ScriptUpdatesHandler
    {
        const string k_LibraryPackageCache = "Library/PackageCache/";

        readonly UnityScriptUpdaterConsentAPI m_ConsentAPI = new UnityScriptUpdaterConsentAPI();
        bool m_HaveConsentToOverwriteUserScripts;

        // Returns true when any file was overwritten, meaning the compilation should run again.
        public bool Apply(ICollection<ScriptUpdate> updates)
        {
            var problemUpdates = new List<(ScriptUpdate update, Exception exception)>();
            bool didUpdate = false;
            void ExecuteUpdates(IEnumerable<ScriptUpdate> updatesToExecute)
            {
                foreach (var update in updatesToExecute)
                {
                    didUpdate = true;
                    try
                    {
                        Console.WriteLine(update.originalFileRelativePath);
                        new NPath(update.updatedFileFullPath).Copy(update.originalFileRelativePath);
                    }
                    catch (Exception e)
                    {
                        problemUpdates.Add((update, e));
                    }
                }
            }

            var(immutablePackageUpdates, nonImmutableUpdates) = updates.SplitBy(u => u.originalFileRelativePath.StartsWith(k_LibraryPackageCache));

            Console.WriteLine("[API Updater] Updated Files:");
            if (immutablePackageUpdates.Count > 0)
            {
                APIUpdaterHelper.HandlePackageFilePaths(OriginalFilesOf(immutablePackageUpdates));
                ExecuteUpdates(immutablePackageUpdates);
            }

            if (nonImmutableUpdates.Count > 0)
            {
                var nonImmutableTargetFiles = OriginalFilesOf(nonImmutableUpdates);
                if (MayOverwrite(nonImmutableTargetFiles) && PrepareForOverwritingUpdatedFiles(nonImmutableTargetFiles))
                    ExecuteUpdates(nonImmutableUpdates);
            }

            if (problemUpdates.Count > 0)
            {
                var sb = new StringBuilder("Unable to update the following files. Are they marked readonly?\n");
                foreach (var problem in problemUpdates)
                    sb.AppendLine(problem.update.originalFileRelativePath + " " + problem.exception.Message);

                Debug.LogError(sb.ToString());
            }

            Console.WriteLine("Finished running ScriptUpdaters");
            return didUpdate;
        }

        static string[] OriginalFilesOf(IReadOnlyList<ScriptUpdate> updates)
        {
            var files = new string[updates.Count];
            for (int i = 0; i < updates.Count; ++i)
                files[i] = updates[i].originalFileRelativePath;
            return files;
        }

        bool MayOverwrite(string[] files)
        {
            if (m_HaveConsentToOverwriteUserScripts)
                return true;
            var result = m_ConsentAPI.AskFor(files);
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
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        static bool PrepareForOverwritingUpdatedFiles(string[] destFiles)
        {
            if (!APIUpdaterManager.WaitForVCSServerConnection())
            {
                //if we fail to connect to the vcs server, we shouldn't overwrite versioned files.
                //in this case we will just not do that, continue the build as normal. We have protection for only trying to script update
                //an assembly once, and the resulting behaviour will be that the user just gets to see the compiler errors and needs to fix
                //them herself.
                return false;
            }

            if (!AssetDatabase.MakeEditable(destFiles))
            {
                Debug.LogError($"Failed to make the scripts to be editable for update.{Environment.NewLine}{string.Join(Environment.NewLine, destFiles)}");
                return false;
            }

            return true;
        }
    }
}
