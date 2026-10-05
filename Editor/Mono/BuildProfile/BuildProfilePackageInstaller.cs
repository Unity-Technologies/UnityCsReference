// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using ProgressEntry = UnityEditor.Build.Profile.BuildProfilePackageAddInfo.ProgressEntry;
using ProgressState = UnityEditor.Build.Profile.BuildProfilePackageAddInfo.ProgressState;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Runs the package installations requested by build profile creation. Profiles created in one
    /// batch ask for the same packages, so they join a single request instead of issuing one each,
    /// and every joiner is told about its progress and completion.
    /// </summary>
    static partial class BuildProfilePackageInstaller
    {
        internal class PackageRequest
        {
            public PackageManager.Requests.AddAndRemoveRequest packageAddRequest;
            public string[] packageQualifiedNames;
            public Action<ProgressEntry> onPackageAddProgress;
            public Action onPackageAddComplete;

            public void HandlePackageAddProgress(PackageManager.ProgressUpdateEventArgs progress)
            {
                int readyPackageNum = 0;
                bool packageInstalling = false;
                bool packageDownloading = false;
                bool packageErr = false;
                string packageNames = "";
                int packagesDownloadingCnt = 0;
                int packagesInstallingCnt = 0;
                int packagesErrCnt = 0;

                ProgressEntry info;
                foreach (var entry in progress.entries)
                {
                    if (Array.IndexOf(packageQualifiedNames, entry.name) >= 0)
                    {
                        if (packageNames.Length > 0)
                            packageNames += " ";
                        switch (entry.state)
                        {
                            case PackageManager.ProgressState.Ready:
                                readyPackageNum += 1;
                                break;
                            case PackageManager.ProgressState.Error:
                                packageErr = true;
                                packagesErrCnt++;
                                break;
                            case PackageManager.ProgressState.Downloading:
                                packageDownloading = true;
                                packagesDownloadingCnt++;
                                break;
                            case PackageManager.ProgressState.Installing:
                                packageInstalling = true;
                                packagesInstallingCnt++;
                                break;
                        }
                        packageNames += entry.name;
                    }
                }

                bool done = (readyPackageNum == packageQualifiedNames.Length);

                if (packageErr)
                {
                    info = new ProgressEntry(ProgressState.PackageError, packageNames, packagesErrCnt);
                }
                else if (packageInstalling)
                {
                    info = new ProgressEntry(ProgressState.PackageInstalling, packageNames, packagesInstallingCnt);
                }
                else if (packageDownloading)
                {
                    info = new ProgressEntry(ProgressState.PackageDownloading, packageNames, packagesDownloadingCnt);
                }
                else
                {
                    info = new ProgressEntry(done ? ProgressState.ConfigurationRunning : ProgressState.ConfigurationPending, packageNames, 0);
                }

                onPackageAddProgress?.Invoke(info);
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static readonly Dictionary<string, PackageRequest> s_InFlightPackageRequests = new();

        /// <summary>
        /// Begins installing <paramref name="packagesToAdd"/>, or joins the request already
        /// installing them. Both callbacks are invoked for every joiner.
        /// </summary>
        /// <returns>
        /// The key identifying the request.
        /// </returns>
        public static string RequestPackageInstallation(
            BuildTargetDiscovery.PlatformPackageIdentifier[] packagesToAdd,
            Action<ProgressEntry> onPackageAddProgress,
            Action onPackageAddComplete)
        {
            var key = GetPackageRequestKey(packagesToAdd);
            if (s_InFlightPackageRequests.TryGetValue(key, out var inFlight))
            {
                inFlight.onPackageAddProgress += onPackageAddProgress;
                inFlight.onPackageAddComplete += onPackageAddComplete;
                return key;
            }

            var installIdentifiers = new string[packagesToAdd.Length];
            var packageNames = new string[packagesToAdd.Length];
            for (int i = 0; i < packagesToAdd.Length; i++)
            {
                installIdentifiers[i] = packagesToAdd[i].GetInstallIdentifier();
                packageNames[i] = packagesToAdd[i].name;
            }

            var request = new PackageRequest
            {
                packageAddRequest = PackageManager.Client.AddAndRemove(installIdentifiers),
                packageQualifiedNames = packageNames,
                onPackageAddProgress = onPackageAddProgress,
                onPackageAddComplete = onPackageAddComplete,
            };

            SubscribeRequestCallbacks();
            s_InFlightPackageRequests[key] = request;
            request.packageAddRequest.progressUpdated += request.HandlePackageAddProgress;
            return key;
        }

        public static void RemovePackageAddCallbacks(
            string key,
            Action<ProgressEntry> onPackageAddProgress,
            Action onPackageAddComplete)
        {
            if (key == null || !s_InFlightPackageRequests.TryGetValue(key, out var inFlight))
                return;

            inFlight.onPackageAddProgress -= onPackageAddProgress;
            inFlight.onPackageAddComplete -= onPackageAddComplete;
        }

        static string GetPackageRequestKey(BuildTargetDiscovery.PlatformPackageIdentifier[] packagesToAdd)
        {
            var installIdentifiers = new string[packagesToAdd.Length];
            for (int i = 0; i < packagesToAdd.Length; i++)
                installIdentifiers[i] = packagesToAdd[i].GetInstallIdentifier();

            Array.Sort(installIdentifiers, StringComparer.Ordinal);
            return string.Join("\n", installIdentifiers);
        }

        static void CheckCompletion()
        {
            List<string> completed = null;
            foreach (var entry in s_InFlightPackageRequests)
            {
                if (entry.Value.packageAddRequest.IsCompleted)
                    (completed ??= new List<string>()).Add(entry.Key);
            }

            if (completed == null)
                return;

            foreach (var key in completed)
            {
                var packageAdd = s_InFlightPackageRequests[key];
                s_InFlightPackageRequests.Remove(key);
                packageAdd.packageAddRequest.progressUpdated -= packageAdd.HandlePackageAddProgress;

                if (packageAdd.packageAddRequest.Status >= PackageManager.StatusCode.Failure)
                    Debug.LogError(packageAdd.packageAddRequest.Error.message);

                packageAdd.onPackageAddComplete?.Invoke();
            }

            if (s_InFlightPackageRequests.Count == 0)
                UnsubscribeRequestCallbacks();
        }

        static void SubscribeRequestCallbacks()
        {
            EditorApplication.update -= CheckCompletion;
            EditorApplication.update += CheckCompletion;
            AssemblyReloadEvents.beforeAssemblyReload -= UnsubscribeRequestCallbacks;
            AssemblyReloadEvents.beforeAssemblyReload += UnsubscribeRequestCallbacks;
        }

        /// <summary>
        /// Detaches the native-backed callbacks before a domain reload — otherwise the native
        /// delegate lists keep GC handles to objects from the previous domain, producing
        /// "invalid GC handle ... from a previous domain" warnings when they are later released.
        /// </summary>
        static void UnsubscribeRequestCallbacks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= UnsubscribeRequestCallbacks;
            EditorApplication.update -= CheckCompletion;

            foreach (var entry in s_InFlightPackageRequests)
                entry.Value.packageAddRequest.progressUpdated -= entry.Value.HandlePackageAddProgress;
        }
    }
}
