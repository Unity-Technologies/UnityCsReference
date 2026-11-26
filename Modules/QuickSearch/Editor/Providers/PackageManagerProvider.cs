// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.PackageManager;
using UnityEngine;
using Unity.Collections;

namespace UnityEditor.Search.Providers
{
    static partial class PackageManagerProvider
    {
        internal static readonly string type = "packages";
        internal static readonly string displayName = "Packages";

        [AutoStaticsCleanupOnCodeReload]
        private static PackageManager.Requests.ListRequest s_ListRequest = null;
        [AutoStaticsCleanupOnCodeReload]
        private static PackageManager.Requests.SearchRequest s_SearchRequest = null;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                priority = 90,
                filterId = "pkg:",
                active = false,
                isExplicitProvider = true,

                onEnable = () =>
                {
                },

                onDisable = () =>
                {
                    s_ListRequest = null;
                    s_SearchRequest = null;
                },

                fetchItems = (context, items, provider) => SearchPackages(context, provider),

                fetchThumbnail = (item, context) => (item.thumbnail = item.score == 0 ? Icons.packageUpdate : Icons.packageInstalled)
            };
        }

        private static IEnumerable<SearchItem> SearchPackages(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            s_ListRequest = s_ListRequest ?? PackageManager.Client.List(offlineMode: true, includeIndirectDependencies: true);
            s_SearchRequest = s_SearchRequest ?? PackageManager.Client.SearchAll(Utils.runningTests);

            if (s_SearchRequest == null || s_ListRequest == null)
                yield break;

            while (!s_SearchRequest.IsCompleted || !s_ListRequest.IsCompleted)
            {
                if (s_SearchRequest.Status == StatusCode.Failure)
                    yield break;
                if (s_ListRequest.Status == StatusCode.Failure)
                    yield break;
                yield return null;
            }

            if (s_SearchRequest.Status == StatusCode.Failure || s_ListRequest.Status == StatusCode.Failure)
                yield break;

            if (s_SearchRequest.Result == null || s_ListRequest.Result == null)
                yield break;

            foreach (var p in s_SearchRequest.Result)
            {
                if (p.keywords.Contains(context.searchQuery) ||
                    SearchUtils.MatchSearchGroups(context, p.description.ToLowerInvariant(), true) ||
                    SearchUtils.MatchSearchGroups(context, p.name.ToLowerInvariant(), true))
                    yield return provider.CreateItem(context, p.packageId, String.IsNullOrEmpty(p.resolvedPath) ? 0 : 1, FormatLabel(p), FormatDescription(p), null, p);
            }
        }

        private static string FormatName(PackageManager.PackageInfo pi)
        {
            if (String.IsNullOrEmpty(pi.displayName))
                return $"{pi.name}@{pi.version}";
            return $"{pi.displayName} ({pi.name}@{pi.version})";
        }

        private static string FormatLabel(PackageManager.PackageInfo pi)
        {
            #pragma warning disable UAC2001 // Avoid Linq
            var installedPackage = s_ListRequest.Result.FirstOrDefault(l => l.name == pi.name);
#pragma warning restore UAC2001
            var status = installedPackage != null ? (installedPackage.version == pi.version ?
                " - <i>In Project</i>" : " - <b>Update Available</b>") : "";
            if (String.IsNullOrEmpty(pi.displayName))
                return $"{pi.name}@{pi.version}{status}";
            return $"{FormatName(pi)}{status}";
        }

        private static bool TryGetInstalledPackage(PackageManager.PackageInfo pi, out PackageManager.PackageInfo installedPackage)
        {
            installedPackage = null;
            var installedPackages = s_ListRequest?.Result;
            if (installedPackages == null)
                return false;
            foreach (var p in installedPackages)
            {
                if (p.name == pi.name)
                {
                    installedPackage = p;
                    return true;
                }
            }
            return false;
        }

        private static string FormatDescription(PackageManager.PackageInfo pi)
        {
            return pi.description.Replace("\r", "").Replace("\n", "");
        }

        private static bool WaitForRequestBase(PackageManager.Requests.Request request, string msg, int loopDelay)
        {
            var progress = 0.0f;
            while (!request.IsCompleted)
            {
                Thread.Sleep(loopDelay);
                EditorUtility.DisplayProgressBar("Unity Package Manager", msg, Mathf.Min(1.0f, progress++ / 100f));
            }
            EditorUtility.ClearProgressBar();

            return request.Status == PackageManager.StatusCode.Success;
        }

        private static bool WaitForRequest<T>(PackageManager.Requests.Request<T> request, string msg, int loopDelay = 20)
        {
            return WaitForRequestBase(request, msg, loopDelay) && request.Result != null;
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "open", null, "Open in Package Manager", OpenPackageInPackageManager, HasPackageInfo),
                new SearchAction(type, "install", null, "Install", InstallPackage, IsCanBeInstalled),
                new SearchAction(type, "update", null, "Update", InstallPackage, IsCanBeUpdated),
                new SearchAction(type, "remove", null, "Remove", RemovePackage, IsRemoveActionEnabled)
            };
        }

        private static bool IsCanBeUpdated(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                if (item.data is not PackageManager.PackageInfo packageInfo)
                    return false;
                if (!TryGetInstalledPackage(packageInfo, out var installedPackage) || installedPackage.version == packageInfo.version)
                    return false;
            }

            return true;
        }

        private static bool IsCanBeInstalled(IReadOnlyCollection<SearchItem> items)
        {
            // Without the installed package list, we cannot tell if the package is already installed.
            if (s_ListRequest?.Result == null)
                return false;

            foreach (var item in items)
            {
                if (item.data is not PackageManager.PackageInfo packageInfo)
                    return false;
                if (TryGetInstalledPackage(packageInfo, out _))
                    return false;
            }

            return true;
        }

        private static bool HasPackageInfo(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                if (item.data is not PackageManager.PackageInfo)
                    return false;
            }
            return items.Count > 0;
        }

        private static void OpenPackageInPackageManager(SearchItem item)
        {
            if (item.data is not PackageManager.PackageInfo packageInfo)
                return;
            PackageManager.UI.Window.Open(packageInfo.name);
        }

        private static void InstallPackage(SearchItem item)
        {
            if (item.data is not PackageManager.PackageInfo packageInfo)
                return;

            if (EditorDialog.DisplayDecisionDialog(
                titleText: "About to install package " + item.id,
                messageText: "Are you sure you want to install the following package?\r\n\r\n" +
                             FormatName(packageInfo),
                yesButtonText: "Install...",
                noButtonText: "Cancel"))
            {
                WaitForRequest(PackageManager.Client.Add(item.id), $"Installing {item.id}...", 25);
            }
        }

        private static void RemovePackage(SearchItem item)
        {
            if (item.data is not PackageManager.PackageInfo packageInfo || !CanBeRemoved(packageInfo))
                return;

            if (EditorDialog.DisplayDecisionDialog(
                titleText: "About to remove package " + packageInfo.packageId,
                messageText: "Are you sure you want to remove the following package?\r\n\r\n" +
                             FormatName(packageInfo),
                yesButtonText: "Remove",
                noButtonText: "Cancel"))
            {
                WaitForRequestBase(PackageManager.Client.Remove(packageInfo.name), $"Removing {packageInfo.packageId}...", 1);
            }
        }

        private static bool CanBeRemoved(PackageManager.PackageInfo packageInfo)
        {
            // Packages only pulled in as dependencies cannot be removed from the manifest.
            return TryGetInstalledPackage(packageInfo, out var installedPackage) && installedPackage.isDirectDependency;
        }

        private static bool IsRemoveActionEnabled(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                if (item.data is not PackageManager.PackageInfo packageInfo || !CanBeRemoved(packageInfo))
                    return false;
            }
            return true;
        }

        internal static class TestHelper
        {
            public static PackageManager.Requests.ListRequest listRequest
            {
                get => s_ListRequest;
                set => s_ListRequest = value;
            }

            public static bool CanBeRemoved(PackageManager.PackageInfo packageInfo) => PackageManagerProvider.CanBeRemoved(packageInfo);
        }
    }
}
