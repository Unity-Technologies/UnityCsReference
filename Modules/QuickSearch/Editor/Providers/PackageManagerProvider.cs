// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class PackageManagerProvider
    {
        internal static string type = "packages";
        internal static string displayName = "Packages";

        private static UnityEditor.PackageManager.Requests.ListRequest s_ListRequest = null;
        private static UnityEditor.PackageManager.Requests.SearchRequest s_SearchRequest = null;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                priority = 90,
                filterId = "pkg:",
                isExplicitProvider = true,

                onEnable = () =>
                {
                    s_ListRequest = UnityEditor.PackageManager.Client.List(true);
                    s_SearchRequest = UnityEditor.PackageManager.Client.SearchAll();
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

        private static string FormatName(UnityEditor.PackageManager.PackageInfo pi)
        {
            if (String.IsNullOrEmpty(pi.displayName))
                return $"{pi.name}@{pi.version}";
            return $"{pi.displayName} ({pi.name}@{pi.version})";
        }

        private static string FormatLabel(UnityEditor.PackageManager.PackageInfo pi)
        {
            var installedPackage = s_ListRequest.Result.FirstOrDefault(l => l.name == pi.name);
            var status = installedPackage != null ? (installedPackage.version == pi.version ?
                " - <i>In Project</i>" : " - <b>Update Available</b>") : "";
            if (String.IsNullOrEmpty(pi.displayName))
                return $"{pi.name}@{pi.version}{status}";
            return $"{FormatName(pi)}{status}";
        }

        private static string FormatDescription(UnityEditor.PackageManager.PackageInfo pi)
        {
            return pi.description.Replace("\r", "").Replace("\n", "");
        }

        private static bool WaitForRequestBase(UnityEditor.PackageManager.Requests.Request request, string msg, int loopDelay)
        {
            var progress = 0.0f;
            while (!request.IsCompleted)
            {
                Thread.Sleep(loopDelay);
                EditorUtility.DisplayProgressBar("Unity Package Manager", msg, Mathf.Min(1.0f, progress++ / 100f));
            }
            EditorUtility.ClearProgressBar();

            return request.Status == UnityEditor.PackageManager.StatusCode.Success;
        }

        private static bool WaitForRequest<T>(UnityEditor.PackageManager.Requests.Request<T> request, string msg, int loopDelay = 20)
        {
            return WaitForRequestBase(request, msg, loopDelay) && request.Result != null;
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "open", null, "Open in Package Manager")
                {
                    handler = (item) =>
                    {
                        var packageInfo = (UnityEditor.PackageManager.PackageInfo)item.data;
                        UnityEditor.PackageManager.UI.Window.Open(packageInfo.name);
                    }
                },
                new SearchAction(type, "install", null, "Install")
                {
                    handler = (item) =>
                    {
                        var packageInfo = (UnityEditor.PackageManager.PackageInfo)item.data;
                        if (EditorUtility.DisplayDialog("About to install package " + item.id,
                            "Are you sure you want to install the following package?\r\n\r\n" +
                            FormatName(packageInfo), "Install...", "Cancel"))
                        {
                            WaitForRequest(UnityEditor.PackageManager.Client.Add(item.id), $"Installing {item.id}...", 25);
                        }
                    }
                },
                new SearchAction(type, "remove", null, "Remove")
                {
                    handler = (item) =>
                    {
                        var packageInfo = (UnityEditor.PackageManager.PackageInfo)item.data;
                        WaitForRequestBase(UnityEditor.PackageManager.Client.Remove(packageInfo.name), $"Removing {packageInfo.packageId}...", 1);
                    }
                }
            };
        }
    }
}
