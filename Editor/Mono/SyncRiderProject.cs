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
                var toolboxRiderRootPath = Path.Combine(home, @"Library/Application Support/JetBrains/Toolbox/apps/Rider");
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
            var installPathsToolbox = CollectPathsFromToolbox(toolboxRiderRootPath, "bin", "rider64.exe", false).ToList();
            var installInfosToolbox = installPathsToolbox.Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToList();

            var installPaths = new List<string>();
            const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            CollectPathsFromRegistry(registryKey, installPaths);
            const string wowRegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            CollectPathsFromRegistry(wowRegistryKey, installPaths);

            var installInfos = installPaths.Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, false)).ToList();
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

            var channelDirs = Directory.GetDirectories(toolboxRiderRootPath);
            var paths = channelDirs.SelectMany(channelDir =>
            {
                try
                {
                    // use history.json - last entry stands for the active build https://jetbrains.slack.com/archives/C07KNP99D/p1547807024066500?thread_ts=1547731708.057700&cid=C07KNP99D
                    var historyFile = Path.Combine(channelDir, ".history.json");
                    if (File.Exists(historyFile))
                    {
                        var json = File.ReadAllText(historyFile);
                        var build = ToolboxHistory.GetLatestBuildFromJson(json);
                        if (build != null)
                        {
                            var buildDir = Path.Combine(channelDir, build);
                            var executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
                            if (executablePaths.Any())
                                return executablePaths;
                        }
                    }

                    var channelFile = Path.Combine(channelDir, ".channel.settings.json");
                    if (File.Exists(channelFile))
                    {
                        var json = File.ReadAllText(channelFile).Replace("active-application", "active_application");
                        var build = ToolboxInstallData.GetLatestBuildFromJson(json);
                        if (build != null)
                        {
                            var buildDir = Path.Combine(channelDir, build);
                            var executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
                            if (executablePaths.Any())
                                return executablePaths;
                        }
                    }

                    // changes in toolbox json files format may brake the logic above, so return all found Rider installations
                    return Directory.GetDirectories(channelDir)
                        .SelectMany(buildDir => GetExecutablePaths(dirName, searchPattern, isMac, buildDir));
                }
                catch (Exception e)
                {
                    // do not write to Debug.Log, just log it.
                    Console.WriteLine(e.Message);
                    Console.WriteLine($"Failed to get RiderPath from {channelDir}");
                }

                return new string[0];
            })
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();
            return paths;
        }

        static string[] GetExecutablePaths(string dirName, string searchPattern, bool isMac, string buildDir)
        {
            var folder = Path.Combine(buildDir, dirName);
            if (!isMac)
                return new[] { Path.Combine(folder, searchPattern) }.Where(File.Exists).ToArray();
            return new DirectoryInfo(folder).GetDirectories(searchPattern).Select(f => f.FullName)
                .Where(Directory.Exists).ToArray();
        }

        [Serializable]
        class ToolboxHistory
        {
            public List<ItemNode> history = null;

            public static string GetLatestBuildFromJson(string json)
            {
                try
                {
                    return JsonUtility.FromJson<ToolboxHistory>(json).history.LastOrDefault()?.item.build;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to get latest build from json {json}");
                }
                return null;
            }
        }

        [Serializable]
        class ItemNode
        {
            public BuildNode item = null;
        }

        [Serializable]
        class BuildNode
        {
            public string build = null;
        }

        [Serializable]
        class ToolboxInstallData
        {
            public ActiveApplication active_application = null;

            public static string GetLatestBuildFromJson(string json)
            {
                try
                {
                    var toolbox = JsonUtility.FromJson<ToolboxInstallData>(json);
                    var builds = toolbox.active_application.builds;
                    if (builds != null && builds.Any())
                        return builds.First();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to get latest build from json {json}");
                }
                return null;
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
