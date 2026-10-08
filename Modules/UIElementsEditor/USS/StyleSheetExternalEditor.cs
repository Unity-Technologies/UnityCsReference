// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static partial class StyleSheetExternalEditor
    {
        [AutoStaticsCleanupOnCodeReload]
        internal static Func<string, int, bool> s_OpenFileCallback = OpenFileAtLine;

        internal static void RestoreTestCallbacks()
        {
            s_OpenFileCallback = OpenFileAtLine;
        }

        /// <summary>
        /// Whether the stylesheet asset at the given path has a file the external script editor could open.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static bool CanOpen(string assetPath) => TryGetFullPath(assetPath, out _);

        /// <summary>
        /// Opens a stylesheet asset in the external script editor, at the given line when it has a real one.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static bool TryOpen(string assetPath, int line)
        {
            if (!TryGetFullPath(assetPath, out var fullPath))
                return false;

            // USS lines are 1-based, and a rule authored in memory has none yet; -1 means "no target line".
            return s_OpenFileCallback(fullPath, line > 0 ? line : -1);
        }

        // On macOS the script editor is started through the OS, which gives it "/" as its working directory,
        // so a project-relative path never resolves.
        static bool TryGetFullPath(string assetPath, out string fullPath)
        {
            fullPath = string.IsNullOrEmpty(assetPath) ? null : FileUtil.PathToAbsolutePath(assetPath);
            return fullPath != null && File.Exists(fullPath);
        }

        static bool OpenFileAtLine(string fullPath, int line) =>
            InternalEditorUtility.OpenFileAtLineExternal(fullPath, line, -1);
    }
}
