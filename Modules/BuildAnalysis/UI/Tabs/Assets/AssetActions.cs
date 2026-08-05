// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Shared entry points for asset-row actions
    /// The action implementations are exposed as swappable delegates so unit tests can capture
    /// invocations without reaching into the Editor APIs.
    /// </summary>
    internal static class AssetActions
    {
        // User-facing message when a build-report asset can no longer be resolved in the project.
        internal const string k_MissingAssetMessage =
            "Asset not found. It may have been renamed, moved, or deleted since this build was analyzed.";

        internal static Action<string> ShowInProjectImpl = DefaultShowInProject;
        internal static Action<string> CopyPathImpl = DefaultCopyPath;
        internal static Func<string, bool> CanShowInProjectImpl = DefaultCanShowInProject;
        internal static Action<string> NotifyMissingImpl = DefaultNotifyMissing;

        public static void ShowInProject(string assetPath) => ShowInProjectImpl(assetPath);
        public static void CopyPath(string assetPath) => CopyPathImpl(assetPath);

        // Whether "Show in Project" can resolve the asset; used to disable the menu item up front.
        public static bool CanShowInProject(string assetPath) => CanShowInProjectImpl(assetPath);

        private static bool DefaultCanShowInProject(string assetPath)
            => !string.IsNullOrEmpty(assetPath) && AssetDatabase.LoadMainAssetAtPath(assetPath) != null;

        private static void DefaultShowInProject(string assetPath)
        {
            var obj = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
            {
                NotifyMissingImpl(assetPath);
                return;
            }

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private static void DefaultCopyPath(string assetPath)
        {
            EditorGUIUtility.systemCopyBuffer = assetPath ?? string.Empty;
        }

        private static void DefaultNotifyMissing(string assetPath)
        {
            var windows = Resources.FindObjectsOfTypeAll<BuildAnalysisWindow>();
            if (windows.Length > 0)
            {
                var icon = EditorGUIUtility.FindTexture("console.warnicon");
                windows[0].ShowNotification(new GUIContent(k_MissingAssetMessage, icon));
            }
            else
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} {k_MissingAssetMessage}");
            }
        }
    }
}
