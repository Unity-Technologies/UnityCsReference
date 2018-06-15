// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal class APIUpdaterHelper
    {
        public static void UpdateScripts(string responseFile, string sourceExtension)
        {
            if (!ScriptUpdatingManager.WaitForVCSServerConnection(true))
            {
                return;
            }

            var outputPath = Provider.enabled ? tempOutputPath : ".";
            RunUpdatingProgram(
                "ScriptUpdater.exe",
                sourceExtension
                + " "
                + CommandLineFormatter.PrepareFileName(MonoInstallationFinder.GetFrameWorksFolder())
                + " "
                + outputPath
                + " "
                + responseFile);
        }

        private static void RunUpdatingProgram(string executable, string arguments)
        {
            var scriptUpdater = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable;
            var program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, scriptUpdater, arguments, false, null);
            program.LogProcessStartInfo();
            program.Start();
            program.WaitForExit();

            Console.WriteLine(string.Join(Environment.NewLine, program.GetStandardOutput()));

            HandleUpdaterReturnValue(program);
        }

        private static void HandleUpdaterReturnValue(ManagedProgram program)
        {
            if (program.ExitCode == 0)
            {
                Console.WriteLine(string.Join(Environment.NewLine, program.GetErrorOutput()));
                UpdateFilesInVCIfNeeded();
                return;
            }

            ScriptUpdatingManager.ReportExpectedUpdateFailure();
            if (program.ExitCode > 0)
                ReportAPIUpdaterFailure(program.GetErrorOutput());
            else
                ReportAPIUpdaterCrash(program.GetErrorOutput());
        }

        private static void ReportAPIUpdaterCrash(IEnumerable<string> errorOutput)
        {
            Debug.LogErrorFormat("Failed to run script updater.{0}Please, report a bug to Unity with these details{0}{1}", Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
        }

        private static void ReportAPIUpdaterFailure(IEnumerable<string> errorOutput)
        {
            var msg = string.Format("APIUpdater encountered some issues and was not able to finish.{0}{1}", Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
            ScriptUpdatingManager.ReportGroupedAPIUpdaterFailure(msg);
        }

        private static void UpdateFilesInVCIfNeeded()
        {
            if (!Provider.enabled)
                return;

            var files = Directory.GetFiles(tempOutputPath, "*.*", SearchOption.AllDirectories);

            var assetList = new AssetList();
            foreach (string f in files)
                assetList.Add(Provider.GetAssetByPath(f.Replace(tempOutputPath, "")));

            // Verify that all the files are also in assetList
            // This is required to ensure the copy temp files to destination loop is only working on version controlled files
            // Provider.GetAssetByPath() can fail i.e. the asset database GUID can not be found for the input asset path
            foreach (var f in files)
            {
                var rawAssetPath = f.Replace(tempOutputPath, "");
                // VCS assets path separator is '/' , file path might be '\' or '/'
                var assetPath = (Path.DirectorySeparatorChar == '\\') ? rawAssetPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : rawAssetPath;
                var foundAsset = assetList.Where(asset => (asset.path == assetPath));
                if (!foundAsset.Any())
                {
                    Debug.LogErrorFormat("[API Updater] Files cannot be updated (failed to add file to list): {0}", rawAssetPath);
                    ScriptUpdatingManager.ReportExpectedUpdateFailure();
                    return;
                }
            }

            var checkoutTask = Provider.Checkout(assetList, CheckoutMode.Exact);
            checkoutTask.Wait();

            // Verify that all the files we need to operate on are now editable according to version control
            // One of these states:
            // 1) UnderVersionControl & CheckedOutLocal
            // 2) UnderVersionControl & AddedLocal
            // 3) !UnderVersionControl
            var notEditable = assetList.Where(asset => asset.IsUnderVersionControl && !asset.IsState(Asset.States.CheckedOutLocal) && !asset.IsState(Asset.States.AddedLocal));
            if (!checkoutTask.success || notEditable.Any())
            {
                Debug.LogErrorFormat("[API Updater] Files cannot be updated (failed to check out): {0}", notEditable.Select(a => a.fullName + " (" + a.state + ")").Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                ScriptUpdatingManager.ReportExpectedUpdateFailure();
                return;
            }

            // Verify that all the files we need to copy are now writable
            // Problems after API updating during ScriptCompilation if the files are not-writable
            notEditable = assetList.Where(asset => ((File.GetAttributes(asset.path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly));
            if (notEditable.Any())
            {
                Debug.LogErrorFormat("[API Updater] Files cannot be updated (files not writable): {0}", notEditable.Select(a => a.fullName).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                ScriptUpdatingManager.ReportExpectedUpdateFailure();
                return;
            }

            // Copy the temp files to the destination : note this operates on "files"
            // Earlier have verified a one-to-one correspondence between assetList and "files"
            foreach (var sourceFileName in files)
            {
                var destFileName = sourceFileName.Replace(tempOutputPath, "");
                File.Copy(sourceFileName,  destFileName, true);
            }
            FileUtil.DeleteFileOrDirectory(tempOutputPath);
        }

        private const string tempOutputPath = "Temp/ScriptUpdater/";
    }
}
