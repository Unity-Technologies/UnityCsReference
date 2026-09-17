// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Multiplayer.PlayMode.Editor;
using Unity.PlayMode.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Multiplayer.Center.Editor
{
    // partial so the [OnCodeLoaded] source generator can emit this type's lifecycle registration.
    partial class QuickStartSamplesManager
    {
        // Runs on every code load rather than from a static constructor, which only runs once per
        // editor session and so would stop re-registering after a code reload.
        [OnCodeLoaded]
        static void InitializeOnLoad()
        {
            //Register delay call to check if we queued an init step before domain re-load
            EditorApplication.delayCall += ExecuteInitStepsForSample;
        }

        const string k_LogTag = "[" + nameof(QuickStartSamplesManager) + "]";
        const string k_QuickStartPackageID = "com.unity.multiplayer.center.quickstart";
        const string k_InitializationStepID = "QuickStart-Initialization";
        const string k_ExecutionStepID = "QuickStart-ExecuteStepsFor";
        static readonly TimeSpan k_TimeoutInterval = TimeSpan.FromSeconds(10);

        readonly Dictionary<string, Sample> m_ImportedSamples = new();

        public QuickStartSamplesManager()
        {
            if (!IsQuickStartsInstalled(out PackageInfo quickstart)) { return; }

            foreach (var sample in GetSamplesFrom(quickstart))
            {
                var sampleName = System.IO.Path.GetFileName(sample.resolvedPath);
                var package = PackageInfo.FindForPackageName(sampleName);
                if (package != null && package.source == PackageSource.Embedded)
                {
                    m_ImportedSamples[sampleName] = sample;
                }
            }
        }

        ILogger Logger => Debug.unityLogger;

        public void Install(string sampleId)
        {
            Events.registeringPackages -= OnRegisteringPackages;
            AssetDatabase.importPackageCompleted -= OnReImportPackageCompleted;

            if (!VerifyQuickStartsAreInstalled(sampleId, out var quickstart)) { return; }

            Events.registeringPackages += OnRegisteringPackages;
            AssetDatabase.importPackageCompleted += OnReImportPackageCompleted;

            foreach (var sample in GetSamplesFrom(quickstart))
            {
                if (System.IO.Path.GetFileName(sample.resolvedPath) != sampleId) { continue; }

                EditorPrefs.SetString(k_InitializationStepID, sampleId);
                if (sample.Import(Sample.ImportOptions.HideImportWindow |
                                  Sample.ImportOptions.OverridePreviousImports))
                {
                    m_ImportedSamples[sampleId] = sample;
                    return;
                }

                Logger.LogError(k_LogTag, L10n.Tr($"Installing the sample {sampleId} has failed." +
                                                  " Make sure that the sample exists in the quick starts package!", null));
            }

            Logger.LogWarning(k_LogTag, L10n.Tr("Sample with given id could not be located", null) + sampleId);
        }

        void OnReImportPackageCompleted(string a)
        {
            OnPackageImport(PackageInfo.GetAllRegisteredPackages());
        }

        void OnRegisteringPackages(PackageRegistrationEventArgs reg)
        {
            OnPackageImport(reg.added);
        }

        void OnPackageImport(IEnumerable<PackageInfo>  packages)
        {
            var importedPackage = EditorPrefs.GetString(k_InitializationStepID, null);
            if (packages != null)
            {
                foreach (var packageInfo in packages)
                {
                    if (packageInfo.name != importedPackage)
                    {
                        continue;
                    }

                    EditorPrefs.SetString(k_InitializationStepID, null);
                    EditorPrefs.SetString(k_ExecutionStepID, importedPackage);
                }
            }

            // A bunch of things do not work properly from the importPackageCompleted callback,
            // let's register to the next editor update instead.
            EditorApplication.delayCall += ExecuteInitStepsForSample;
        }

        public bool IsInstalled(string sampleId)
        {
            return m_ImportedSamples.ContainsKey(sampleId);
        }

        static IEnumerable<Sample> GetSamplesFrom(PackageInfo package)
        {
            return Sample.FindByPackage(package.name, package.version);
        }

        internal static bool IsQuickStartsInstalled(out PackageInfo quickstart)
        {
            quickstart = PackageInfo.FindForPackageName(k_QuickStartPackageID);
            return quickstart != null;
        }

        bool VerifyQuickStartsAreInstalled(string sampleId, out PackageInfo quickstart)
        {
            if (IsQuickStartsInstalled(out quickstart)) { return true; }

            var message =
                $"Installing the sample {sampleId} will require the installation of {k_QuickStartPackageID} too.";
            // not installed
            // prompt user to notify that the quickstarts package will be installed with this action too
            Logger.LogWarning(k_LogTag, message);

            var result = EditorDialog.DisplayDecisionDialog(
                "Missing required package.",
                message, "Install", "Cancel");

            // if canceled
            if (!result) { return false; }

            var addRequest = AwaitQuickstartsAddRequest();

            // if not a successful installation
            if (addRequest.Status != StatusCode.Success) { return false; }

            quickstart = addRequest.Result;
            return true;
        }

        public static AddRequest StartQuickstartsAddRequest() => Client.Add(k_QuickStartPackageID);

        public static AddRequest AwaitQuickstartsAddRequest()
        {
            // create the add request
            var addRequest = Client.Add(k_QuickStartPackageID);

            // wait for request to time out or complete
            var timeout = k_TimeoutInterval;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (timeout > TimeSpan.Zero && addRequest.Status == StatusCode.InProgress)
            {
                timeout -= stopwatch.Elapsed;
                stopwatch.Restart();
            }

            stopwatch.Stop();
            return addRequest;
        }

        static void ExecuteInitStepsForSample()
        {
            var sampleName = EditorPrefs.GetString(k_ExecutionStepID, null);
            EditorPrefs.SetString(k_ExecutionStepID, null);

            if (string.IsNullOrEmpty(sampleName))
                return;

            // load the playmode scenario associated with the sample
            var sampleRoot = $"Packages/{sampleName}";
            var scenarios = AssetDatabase.FindAssets($"t:{nameof(PlayModeScenario)}", new[] { sampleRoot });
            if (scenarios.Length > 0)
            {
                var scenario = AssetDatabase.LoadAssetAtPath<PlayModeScenario>(AssetDatabase.GUIDToAssetPath(scenarios[0]));
                PlayModeScenarioManager.ActiveScenario = scenario;

                if (TryGetScenePathToOpen(GetMainEditorInitialScene(scenario), sampleRoot, out var scenePath))
                {
                    // Prompt to save the current scene before replacing it (skip in batch mode — no GUI)
                    if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            }
            else
            {
                Debug.unityLogger.LogError(k_LogTag,
                    "No multiplayer scenarios were found in the sample! " +
                    "Ensure that each sample has a default scenario!");
            }

            // ping readme, deferred once more so the sample's asset import is guaranteed to have
            // completed before the lookup runs.
            EditorApplication.delayCall += () =>
            {
                if (!TryFindSampleReadme(sampleRoot, out var readme))
                    return;

                Selection.activeObject = readme;
                ProjectWindowUtil.ShowCreatedAsset(readme);
                EditorGUIUtility.PingObject(readme);
            };
        }

        internal static bool TryFindSampleReadme(string sampleRoot, out UnityEngine.Object readme)
        {
            readme = null;

            foreach (var candidate in new[] { "Readme.asset", "README.md", "Readme.md" })
            {
                readme = AssetDatabase.LoadMainAssetAtPath($"{sampleRoot}/{candidate}");
                if (readme != null)
                    return true;
            }

            Debug.unityLogger.LogWarning(k_LogTag, $"No readme was found in '{sampleRoot}'.");
            return false;
        }

        internal static SceneAsset GetMainEditorInitialScene(PlayModeScenario scenario)
        {
            if (scenario is not OrchestratedScenario orchestratedScenario)
                return null;

            foreach (var instance in orchestratedScenario.Settings.GetAllInstanceItems())
            {
                if (instance.IsInstanceType(typeof(MainEditorController)))
                    return instance.GetSettings<MainEditorController.InstanceSettings>().InitialScene;
            }

            return null;
        }

        internal static bool TryGetScenePathToOpen(SceneAsset initialScene, string sampleRoot, out string scenePath)
        {
            scenePath = null;

            if (initialScene == null)
            {
                Debug.unityLogger.LogWarning(k_LogTag,
                    $"The play mode scenario in '{sampleRoot}' has no initial scene set;" +
                    " leaving the current scene open.");
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(initialScene);
            if (!assetPath.StartsWith(sampleRoot + "/", StringComparison.Ordinal))
            {
                Debug.unityLogger.LogWarning(k_LogTag,
                    $"The play mode scenario in '{sampleRoot}' names an initial scene outside that folder" +
                    $" (resolved to '{assetPath}'); leaving the current scene open.");
                return false;
            }

            scenePath = assetPath;
            return true;
        }

        public void RemoveSample(string sampleId)
        {
            Logger.LogWarning(k_LogTag, $"Removing sample {sampleId}");

            var sample = PackageInfo.FindForPackageName(sampleId);

            // adapted from UpmClient.cs;

            try
            {
                foreach (var file in Directory.GetFiles(sample.resolvedPath, "*",
                             System.IO.SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }

                Directory.Delete(sample.resolvedPath, true);

                //resolve packages once locally embedded package has been removed
                EditorApplication.delayCall += Client.Resolve;
            }
            catch (IOException e)
            {
                Logger.Log(k_LogTag, $"Cannot remove embedded sample {sampleId}: {e.Message}");
            }
        }
    }
}
