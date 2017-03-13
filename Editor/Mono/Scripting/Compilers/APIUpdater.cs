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

            var checkoutTask = Provider.Checkout(assetList, CheckoutMode.Exact);
            checkoutTask.Wait();

            var failedToCheckout = checkoutTask.assetList.Where(a => (a.state & Asset.States.ReadOnly) == Asset.States.ReadOnly);
            if (!checkoutTask.success || failedToCheckout.Any())
            {
                Debug.LogErrorFormat("[API Updater] Files cannot be updated (failed to check out): {0}", failedToCheckout.Select(a => a.fullName + " (" + a.state + ")").Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                ScriptUpdatingManager.ReportExpectedUpdateFailure();
                return;
            }

            FileUtil.CopyDirectoryRecursive(tempOutputPath, ".", true);
            FileUtil.DeleteFileOrDirectory(tempOutputPath);
        }

        private const string tempOutputPath = "Temp/ScriptUpdater/";
    }
}
