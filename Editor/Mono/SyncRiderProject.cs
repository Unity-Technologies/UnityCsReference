// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class SyncRiderProject
    {
        static SyncRiderProject()
        {
            ScriptEditorUtility.RegisterIde(PathCallback);
        }

        static ScriptEditorUtility.Installation[] PathCallback()
        {
            return RiderPathLocator.GetAllRiderPaths()
                .Select(riderInfo => new ScriptEditorUtility.Installation
                {
                    Path = riderInfo.Path,
                    Name = riderInfo.Presentation
                })
                .ToArray();
        }
    }

    /// <summary>
    /// This code is a modified version of the JetBrains resharper-unity plugin listed here:
    /// https://github.com/JetBrains/resharper-unity/blob/master/unity/JetBrains.Rider.Unity.Editor/EditorPlugin/RiderPathLocator.cs
    /// </summary>
    internal static class RiderPathLocator
    {
        static RiderInfo[] CollectAllRiderPathsLinux()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
                return new RiderInfo[0];
            const string pathToBuildTxt = "../../build.txt";
            //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/173.3994.1125/bin/rider.sh
            //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/.channel.settings.json
            var toolboxRiderRootPath = Path.Combine(home, @".local/share/JetBrains/Toolbox/apps/Rider");
            var paths = CollectPathsFromToolbox(toolboxRiderRootPath, "bin", "rider.sh", false)
                .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToList();


            // $Home/.local/share/applications/jetbrains-rider.desktop
            var shortcut = new FileInfo(Path.Combine(home, @".local/share/applications/jetbrains-rider.desktop"));

            if (shortcut.Exists)
            {
                var lines = File.ReadAllLines(shortcut.FullName);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("Exec=\""))
                        continue;
                    var path = line.Split('"').Where((item, index) => index == 1).SingleOrDefault();
                    if (string.IsNullOrEmpty(path))
                        continue;
                    var buildTxtPath = Path.Combine(path, pathToBuildTxt);
                    var buildNumber = GetBuildNumber(buildTxtPath);
                    if (paths.Any(a => a.Path == path)) // avoid adding similar build as from toolbox
                        continue;
                    paths.Add(new RiderInfo(buildNumber, path, false));
                }
            }

            return paths.ToArray();
        }

        static RiderInfo[] CollectRiderInfosMac()
        {
            var pathToBuildTxt = "Contents/Resources/build.txt";

            // "/Applications/*Rider*.app"
            var folder = new DirectoryInfo("/Applications");
            var results = folder.GetDirectories("*Rider*.app")
                .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a.FullName, pathToBuildTxt)), a.FullName, false))
                .ToList();

            // /Users/user/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/181.3870.267/Rider EAP.app
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                var toolboxRiderRootPath =
                    Path.Combine(home, @"Library/Application Support/JetBrains/Toolbox/apps/Rider");
                var paths = CollectPathsFromToolbox(toolboxRiderRootPath, "", "Rider*.app", true)
                    .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true));
                results.AddRange(paths);
            }

            return results.ToArray();
        }

        static string GetBuildNumber(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists)
                return File.ReadAllText(file.FullName);
            return string.Empty;
        }

        static RiderInfo[] CollectRiderInfosWindows()
        {
            var pathToBuildTxt = "../../build.txt";

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var toolboxRiderRootPath = Path.Combine(localAppData, @"JetBrains\Toolbox\apps\Rider");
            var installPathsToolbox =
                CollectPathsFromToolbox(toolboxRiderRootPath, "bin", "rider64.exe", false).ToList();
            var installInfosToolbox = installPathsToolbox
                .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToList();

            var installPaths = new List<string>();
            const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            CollectPathsFromRegistry(registryKey, installPaths);
            const string wowRegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            CollectPathsFromRegistry(wowRegistryKey, installPaths);

            var installInfos = installPaths
                .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, false)).ToList();
            installInfos.AddRange(installInfosToolbox);

            return installInfos.ToArray();
        }

        static void CollectPathsFromRegistry(string registryKey, List<string> installPaths)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key == null) return;
                foreach (var subkeyName in key.GetSubKeyNames().Where(a => a.Contains("Rider")))
                {
                    using (var subkey = key.OpenSubKey(subkeyName))
                    {
                        var folderObject = subkey?.GetValue("InstallLocation");
                        if (folderObject == null) continue;
                        var folder = folderObject.ToString();
                        var possiblePath = Path.Combine(folder, @"bin\rider64.exe");
                        if (File.Exists(possiblePath))
                            installPaths.Add(possiblePath);
                    }
                }
            }
        }

        public static RiderInfo[] GetAllRiderPaths()
        {
            try
            {
                switch (SystemInfo.operatingSystemFamily)
                {
                    case OperatingSystemFamily.Windows:
                    {
                        return CollectRiderInfosWindows();
                    }
                    case OperatingSystemFamily.MacOSX:
                    {
                        return CollectRiderInfosMac();
                    }
                    case OperatingSystemFamily.Linux:
                    {
                        return CollectAllRiderPathsLinux();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }


            return new RiderInfo[0];
        }

        static string[] CollectPathsFromToolbox(
            string toolboxRiderRootPath,
            string dirName,
            string searchPattern,
            bool isMac)
        {
            if (!Directory.Exists(toolboxRiderRootPath))
                return new string[0];

            var channelFiles = Directory.GetDirectories(toolboxRiderRootPath)
                .Select(b => Path.Combine(b, ".channel.settings.json")).Where(File.Exists).ToArray();

            var paths = channelFiles.SelectMany(a => {
                try
                {
                    var channelDir = Path.GetDirectoryName(a);
                    var json = File.ReadAllText(a).Replace("active-application", "active_application");
                    var toolbox = ToolboxInstallData.FromJson(json);
                    var builds = toolbox.active_application.builds;
                    if (builds.Any())
                    {
                        var build = builds.First();
                        var folder = Path.Combine(Path.Combine(channelDir, build), dirName);
                        if (!isMac)
                            return new[] {Path.Combine(folder, searchPattern)};
                        return new DirectoryInfo(folder).GetDirectories(searchPattern).Select(f => f.FullName);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning("Failed to get RiderPath via .channel.settings.json");
                }

                return new string[0];
            })
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();
            return paths;
        }

        [Serializable]
        class ToolboxInstallData
        {
            public ActiveApplication active_application = null;

            public static ToolboxInstallData FromJson(string json)
            {
                return JsonUtility.FromJson<ToolboxInstallData>(json);
            }
        }

        [Serializable]
        class ActiveApplication
        {
            public List<string> builds = null;
        }

        public struct RiderInfo
        {
            public string Presentation;
            public string BuildVersion;
            public string Path;

            public RiderInfo(string buildVersion, string path, bool isToolbox)
            {
                BuildVersion = buildVersion;
                Path = new FileInfo(path).FullName; // normalize separators

                var version = string.Empty;
                if (buildVersion.Length > 3)
                    version = buildVersion.Substring(3);

                var presentation = "Rider " + version;
                if (isToolbox)
                    presentation += " (JetBrains Toolbox)";

                Presentation = presentation;
            }
        }
    }
}
