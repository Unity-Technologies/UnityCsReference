// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace UnityEditor.Video
{
    // Bundles video test assets as streaming assets when -videoTestAssetsPath <dir> is
    // passed to the Editor command line. The entire directory tree is bundled, making
    // the contents accessible at runtime via GetStreamingAssetsPath().
    class VideoTestBuildPostprocessor : BuildPlayerProcessor
    {
        const string k_ArgName       = "-videoTestAssetsPath";
        const string k_StreamingDest = "Modules/Video/Tests/Assets";

        public override int callbackOrder => 0;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            string assetsPath = GetArgValue(k_ArgName);
            if (string.IsNullOrEmpty(assetsPath))
                return;

            UnityEngine.Debug.Log($"[VideoTestBuildPostprocessor] CWD={Directory.GetCurrentDirectory()} arg={assetsPath}");

            string resolved = ResolveAssetsPath(assetsPath);
            if (resolved == null)
            {
                UnityEngine.Debug.LogWarning($"[VideoTestBuildPostprocessor] Path not found: {resolved}");
                return;
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(resolved, k_StreamingDest);
            UnityEngine.Debug.Log($"[VideoTestBuildPostprocessor] Bundled '{resolved}' → StreamingAssets/{k_StreamingDest}");
        }

        // Unity changes CWD to the project folder after createProject, so relative paths
        // fail. Walk up from the editor executable to find the source root.
        static string ResolveAssetsPath(string assetsPath)
        {
            if (Path.IsPathRooted(assetsPath) && Directory.Exists(assetsPath))
                return assetsPath;

            string cwdCandidate = Path.GetFullPath(assetsPath);
            if (Directory.Exists(cwdCandidate))
                return cwdCandidate;

            string dir = Path.GetDirectoryName(UnityEditor.EditorApplication.applicationPath);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, assetsPath);
                if (Directory.Exists(candidate))
                    return candidate;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir)
                    break;
                dir = parent;
            }
            return null;
        }

        static string GetArgValue(string argName)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(argName, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
