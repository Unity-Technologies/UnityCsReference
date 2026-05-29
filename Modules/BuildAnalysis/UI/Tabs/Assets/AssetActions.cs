// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Shared entry points for asset-row actions (Inspector Select button + future row context menu).
    /// The two action implementations are exposed as swappable delegates so unit tests can capture
    /// invocations without reaching into the Editor APIs.
    /// </summary>
    internal static class AssetActions
    {
        internal static Action<string> ShowInProjectImpl = DefaultShowInProject;
        internal static Action<string> CopyPathImpl = DefaultCopyPath;

        public static void ShowInProject(string assetPath) => ShowInProjectImpl(assetPath);
        public static void CopyPath(string assetPath) => CopyPathImpl(assetPath);

        private static void DefaultShowInProject(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
                return;

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private static void DefaultCopyPath(string assetPath)
        {
            EditorGUIUtility.systemCopyBuffer = assetPath ?? string.Empty;
        }
    }
}
