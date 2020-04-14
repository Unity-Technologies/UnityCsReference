// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.IO;
using System.Linq;
using UnityEngine;
using JetBrains.Annotations;

namespace UnityEditor
{
    // Proxy class used to install from anywhere Quick Search package.
    static class QuickSearch
    {
        [UsedImplicitly, MenuItem("Help/Quick Search &'", priority = 9000)]
        private static void OpenQuickSearch()
        {
            const string k_QuickSearchPackageId = "com.unity.quicksearch";

            // If the quick search package is installed it will execute the follow command.
            // Otherwise we will ask the user if he wants to install the package.
            if (CommandService.Exists(nameof(OpenQuickSearch)))
                CommandService.Execute(nameof(OpenQuickSearch), CommandHint.Menu);
            else
            {
                // Search for the latest version of the package.

                var searchLatestRequest = PackageManager.Client.Search(k_QuickSearchPackageId);
                if (!WaitForRequest(searchLatestRequest, "Searching for latest version of Quick Search..."))
                {
                    Debug.LogError($"Cannot find the Quick Search package ({searchLatestRequest.Status}).");
                    return;
                }

                var quickSearchPackage = searchLatestRequest.Result.FirstOrDefault(p => p.name == k_QuickSearchPackageId);
                if (quickSearchPackage != null && EditorUtility.DisplayDialog(
                    $"Shoot! {quickSearchPackage.displayName} is not installed yet!",
                    $"Do you want to install {quickSearchPackage.displayName} ({quickSearchPackage.versions.verified}) and be more productive?" +
                    $"\r\n\r\nPackage Description: {quickSearchPackage.description}", "Yes", "No"))
                {
                    // Install a token that will be read by the quick search package once
                    // installed so it can launch itself automatically the first time.
                    var quickSearchFirstUseTokenPath = Utils.Paths.Combine(Application.dataPath, "..", "Library", "~quicksearch.new");
                    File.Create(Path.GetFullPath(quickSearchFirstUseTokenPath)).Dispose();

                    // Add quick search package entry. the added package will
                    // be compiled and a domain reload will occur.
                    var packageIdToInstall = $"{quickSearchPackage.name}@{quickSearchPackage.versions.verified}";
                    var addQuickSearchRequest = PackageManager.Client.Add(packageIdToInstall);
                    if (!WaitForRequest(addQuickSearchRequest, $"Installing {quickSearchPackage.displayName}..."))
                        Debug.LogError($"Failed to install {packageIdToInstall}");
                }
            }
        }

        private static bool WaitForRequest<T>(PackageManager.Requests.Request<T> request, string msg, int loopDelay = 20)
        {
            var progress = 0.0f;
            while (!request.IsCompleted)
            {
                Thread.Sleep(loopDelay);
                EditorUtility.DisplayProgressBar("Unity Package Manager", msg, Mathf.Min(1.0f, progress++ / 100f));
            }
            EditorUtility.ClearProgressBar();

            return request.Status == PackageManager.StatusCode.Success && request.Result != null;
        }
    }
}
