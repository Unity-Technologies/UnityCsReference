// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class PackageManagerProvider
    {
        internal static string type = "packages";
        internal static string displayName = "Packages";

        private static PackageManager.Requests.ListRequest s_ListRequest = null;
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

            s_ListRequest = s_ListRequest ?? PackageManager.Client.List(true);
            s_SearchRequest = s_SearchRequest ?? PackageManager.Client.SearchAll(Utils.runningTests);

            if (s_SearchRequest == null || s_ListRequest == null)
                yield break;

            while (!s_SearchRequest.IsCompleted || !s_ListRequest.IsCompleted)
                yield return null;

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
            var installedPackage = s_ListRequest.Result.FirstOrDefault(l => l.name == pi.name);
            var status = installedPackage != null ? (installedPackage.version == pi.version ?
                " - <i>In Project</i>" : " - <b>Update Available</b>") : "";
            if (String.IsNullOrEmpty(pi.displayName))
                return $"{pi.name}@{pi.version}{status}";
            return $"{FormatName(pi)}{status}";
        }

        private static bool IsPackageInstalled(PackageManager.PackageInfo pi, out string version)
        {
            version = null;
            var installedPackage = s_ListRequest.Result.FirstOrDefault(l => l.name == pi.name);
            if (installedPackage == null)
                return false;
            version = installedPackage.version;
            return true;
        }

        private static bool IsPackageInstalled(PackageManager.PackageInfo pi)
        {
            return IsPackageInstalled(pi, out _);
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
                new SearchAction(type, "open", null, "Open in Package Manager", OpenPackageInPackageManager),
                new SearchAction(type, "install", null, "Install", InstallPackage, IsCanBeInstalled),
                new SearchAction(type, "update", null, "Update", InstallPackage, IsCanBeUpdated),
                new SearchAction(type, "remove", null, "Remove", RemovePackage, IsRemoveActionEnabled)
            };
        }

        private static bool IsCanBeUpdated(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                var packageInfo = (PackageManager.PackageInfo)item.data;
                if (!IsPackageInstalled(packageInfo, out var installedVersion) || installedVersion == packageInfo.version)
                    return false;
            }

            return true;
        }

        private static bool IsCanBeInstalled(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                var packageInfo = (PackageManager.PackageInfo)item.data;
                if (IsPackageInstalled(packageInfo))
                    return false;
            }

            return true;
        }

        private static void OpenPackageInPackageManager(SearchItem item)
        {
            var packageInfo = (PackageManager.PackageInfo)item.data;
            PackageManager.UI.Window.Open(packageInfo.name);
        }

        private static void InstallPackage(SearchItem item)
        {
            var packageInfo = (PackageManager.PackageInfo)item.data;
            if (EditorUtility.DisplayDialog("About to install package " + item.id,
                "Are you sure you want to install the following package?\r\n\r\n" +
                FormatName(packageInfo), "Install...", "Cancel"))
            {
                WaitForRequest(PackageManager.Client.Add(item.id), $"Installing {item.id}...", 25);
            }
        }

        private static void RemovePackage(SearchItem item)
        {
            var packageInfo = (PackageManager.PackageInfo)item.data;
            WaitForRequestBase(PackageManager.Client.Remove(packageInfo.name), $"Removing {packageInfo.packageId}...", 1);
        }

        private static bool IsRemoveActionEnabled(IReadOnlyCollection<SearchItem> items)
        {
            foreach (var item in items)
            {
                var packageInfo = (PackageManager.PackageInfo)item.data;
                if (!IsPackageInstalled(packageInfo))
                    return false;
            }
            return true;
        }
    }
}
