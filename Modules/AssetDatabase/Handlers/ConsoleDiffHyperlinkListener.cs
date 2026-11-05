// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.AssetPipeline.Handlers;

// Opens any console hyperlink carrying leftPath and rightPath in the user's configured diff tool.
internal static partial class ConsoleDiffHyperlinkListener
{
    const string k_LeftFileKey = "leftPath";
    const string k_RightFileKey = "rightPath";

    [OnCodeLoaded]
    static void Initialize()
    {
        EditorGUI.hyperLinkClicked += OnHyperLinkClicked;
    }

    [OnCodeUnloading]
    static void Shutdown()
    {
        EditorGUI.hyperLinkClicked -= OnHyperLinkClicked;
    }

    static void OnHyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
    {
        if (args?.hyperLinkData == null)
            return;

        if (!args.hyperLinkData.TryGetValue(k_LeftFileKey, out string leftPath) ||
            !args.hyperLinkData.TryGetValue(k_RightFileKey, out string rightPath))
            return;

        leftPath = ResolveProjectPath(leftPath);
        rightPath = ResolveProjectPath(rightPath);

        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            Debug.LogWarning($"Cannot open the diff tool from hyperlink: one or both files no longer exist ({leftPath}, {rightPath})");
            return;
        }

        string error = EditorUtility.InvokeDiffTool(
            Path.GetFileName(leftPath), leftPath,
            Path.GetFileName(rightPath), rightPath,
            string.Empty, string.Empty, false);

        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning(error);
    }

    static string ResolveProjectPath(string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
        return Path.GetFullPath(path).Replace('\\', '/');
    }
}
