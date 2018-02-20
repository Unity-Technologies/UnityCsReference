// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Win32;
using UnityEditor.Callbacks;
using UnityEditor.VisualStudioIntegration;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal enum VisualStudioVersion
    {
        Invalid = 0,
        VisualStudio2008 = 9,
        VisualStudio2010 = 10,
        VisualStudio2012 = 11,
        VisualStudio2013 = 12,
        VisualStudio2015 = 14,
        VisualStudio2017 = 15,
    }

    internal class VisualStudioPath
    {
        public string Path { get; set; }
        public string Edition { get; set; }

        public VisualStudioPath(string path, string edition = "")
        {
            Path = path;
            Edition = edition;
        }
    }

    [InitializeOnLoad]
    internal class SyncVS : AssetPostprocessor
    {
        static bool s_AlreadySyncedThisDomainReload;

        static SyncVS()
        {
            Synchronizer = new SolutionSynchronizer(Directory.GetParent(Application.dataPath).FullName, new SolutionSynchronizationSettings());
            try
            {
                InstalledVisualStudios = GetInstalledVisualStudios() as Dictionary<VisualStudioVersion, VisualStudioPath[]>;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error detecting Visual Studio installations: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                InstalledVisualStudios = new Dictionary<VisualStudioVersion, VisualStudioPath[]>();
            }

            SetVisualStudioAsEditorIfNoEditorWasSet();

            UnityVSSupport.Initialize();
        }

        private static void SetVisualStudioAsEditorIfNoEditorWasSet()
        {
            var externalEditor = EditorPrefs.GetString("kScriptsDefaultApp");
            var bestVisualStudio = FindBestVisualStudio();
            if (externalEditor == "" && bestVisualStudio != null)
                EditorPrefs.SetString("kScriptsDefaultApp", bestVisualStudio);
        }

        public static string FindBestVisualStudio()
        {
            var vs = InstalledVisualStudios.OrderByDescending(kvp => kvp.Key).Select(kvp2 => kvp2.Value).FirstOrDefault();
            return vs == null ? null : vs.Last().Path;
        }

        private static readonly SolutionSynchronizer Synchronizer;
        internal static Dictionary<VisualStudioVersion, VisualStudioPath[]> InstalledVisualStudios { get; private set; }

        internal class SolutionSynchronizationSettings : DefaultSolutionSynchronizationSettings
        {
            public override int VisualStudioVersion
            {
                get
                {
                    var vs = ScriptEditorUtility.GetExternalScriptEditor();
                    if (InstalledVisualStudios.ContainsKey(UnityEditor.VisualStudioVersion.VisualStudio2008) &&
                        (vs != String.Empty) &&
                        PathsAreEquivalent(InstalledVisualStudios[UnityEditor.VisualStudioVersion.VisualStudio2008].Last().Path, vs))
                        return 9;

                    return 10;
                }
            }

            public override string SolutionTemplate
            {
                get { return EditorPrefs.GetString("VSSolutionText", base.SolutionTemplate); }
            }

            public override string GetProjectHeaderTemplate(ScriptingLanguage language)
            {
                return EditorPrefs.GetString("VSProjectHeader", base.GetProjectHeaderTemplate(language));
            }

            public override string GetProjectFooterTemplate(ScriptingLanguage language)
            {
                return EditorPrefs.GetString("VSProjectFooter", base.GetProjectFooterTemplate(language));
            }

            public override string EditorAssemblyPath
            {
                get { return UnityEditorInternal.InternalEditorUtility.GetEditorAssemblyPath(); }
            }

            public override string EngineAssemblyPath
            {
                get { return UnityEditorInternal.InternalEditorUtility.GetEngineAssemblyPath(); }
            }

            public override string[] Defines
            {
                get { return EditorUserBuildSettings.activeScriptCompilationDefines; }
            }

            protected override string FrameworksPath()
            {
                return EditorApplication.applicationContentsPath;
            }

            internal static bool IsOSX
            {
                get { return System.Environment.OSVersion.Platform == System.PlatformID.Unix; }
            }

            internal static bool IsWindows
            {
                get { return !IsOSX && System.IO.Path.DirectorySeparatorChar == '\\' && System.Environment.NewLine == "\r\n"; }
            }
        }

        public static bool ProjectExists()
        {
            return Synchronizer.SolutionExists();
        }

        public static void CreateIfDoesntExist()
        {
            if (!Synchronizer.SolutionExists())
            {
                Synchronizer.Sync();
            }
        }

        class BuildTargetChangedHandler : Build.IActiveBuildTargetChanged
        {
            public int callbackOrder { get { return 0; } }

            public void OnActiveBuildTargetChanged(BuildTarget oldTarget, BuildTarget newTarget)
            {
                SyncVisualStudioProjectIfItAlreadyExists();
            }
        }

        public static void SyncVisualStudioProjectIfItAlreadyExists()
        {
            if (Synchronizer.SolutionExists())
            {
                Synchronizer.Sync();
            }
        }

        // For the time being this doesn't use the callback
        public static void PostprocessSyncProject(
            string[] importedAssets,
            string[] addedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            Synchronizer.SyncIfNeeded(addedAssets.Union(deletedAssets.Union(movedAssets.Union(movedFromAssetPaths))));
        }

        [MenuItem("Assets/Open C# Project")]
        static void SyncAndOpenSolution()
        {
            SyncSolution();
            OpenProjectFileUnlessInBatchMode();
        }

        public static void SyncSolution()
        {
            // Ensure that the mono islands are up-to-date
            AssetDatabase.Refresh();

            Synchronizer.Sync();
        }

        public static void SyncIfFirstFileOpenSinceDomainLoad()
        {
            if (s_AlreadySyncedThisDomainReload)
                return;

            s_AlreadySyncedThisDomainReload = true;
            Synchronizer.Sync();
        }

        private static void OpenProjectFileUnlessInBatchMode()
        {
            if (InternalEditorUtility.inBatchMode)
                return;

            InternalEditorUtility.OpenFileAtLineExternal("", -1);
        }

        /// <summary>
        /// Detects Visual Studio installations using the Windows registry
        /// </summary>
        /// <returns>
        /// The detected Visual Studio installations
        /// </returns>
        private static IDictionary<VisualStudioVersion, VisualStudioPath[]> GetInstalledVisualStudios()
        {
            var versions = new Dictionary<VisualStudioVersion, VisualStudioPath[]>();

            if (SolutionSynchronizationSettings.IsWindows)
            {
                foreach (VisualStudioVersion version in Enum.GetValues(typeof(VisualStudioVersion)))
                {
                    if (version > VisualStudioVersion.VisualStudio2015)
                        continue;

                    try
                    {
                        // Try COMNTOOLS environment variable first
                        string key = Environment.GetEnvironmentVariable(string.Format("VS{0}0COMNTOOLS", (int)version));
                        if (!string.IsNullOrEmpty(key))
                        {
                            string path = UnityEditor.Utils.Paths.Combine(key, "..", "IDE", "devenv.exe");
                            if (File.Exists(path))
                            {
                                versions[version] = new[] { new VisualStudioPath(path) };
                                continue;
                            }
                        }

                        // Try the proper registry key
                        key = GetRegistryValue(
                                string.Format(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\{0}.0", (int)version), "InstallDir");

                        // Try to fallback to the 32bits hive
                        if (string.IsNullOrEmpty(key))
                            key = GetRegistryValue(
                                    string.Format(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\{0}.0", (int)version), "InstallDir");

                        if (!string.IsNullOrEmpty(key))
                        {
                            string path = UnityEditor.Utils.Paths.Combine(key, "devenv.exe");
                            if (File.Exists(path))
                            {
                                versions[version] = new[] { new VisualStudioPath(path) };
                                continue;
                            }
                        }

                        // Fallback to debugger key
                        key = GetRegistryValue(
                                // VS uses this key for the local debugger path
                                string.Format(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\{0}.0\Debugger", (int)version), "FEQARuntimeImplDll");
                        if (!string.IsNullOrEmpty(key))
                        {
                            string path = DeriveVisualStudioPath(key);
                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                                versions[version] = new[] { new VisualStudioPath(DeriveVisualStudioPath(key)) };
                        }
                    }
                    catch
                    {
                        // This can happen with a registry lookup failure
                    }
                }

                var requiredWorkloads = new[] {"Microsoft.VisualStudio.Workload.ManagedGame"};
                var raw = VisualStudioUtil.FindVisualStudioDevEnvPaths((int)VisualStudioVersion.VisualStudio2017, requiredWorkloads);

                var visualStudioPaths = VisualStudioUtil.ParseRawDevEnvPaths(raw)
                    .Where(vs => !requiredWorkloads.Except(vs.Workloads).Any()) // All required workloads must be present
                    .Select(vs => new VisualStudioPath(vs.DevEnvPath, vs.Edition))
                    .ToArray();

                if (visualStudioPaths.Length != 0)
                {
                    versions[VisualStudioVersion.VisualStudio2017] = visualStudioPaths;
                }
            }

            return versions;
        }

        static string GetRegistryValue(string path, string key)
        {
            try
            {
                return Microsoft.Win32.Registry.GetValue(path, key, null) as string;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Derives the Visual Studio installation path from the debugger path
        /// </summary>
        /// <returns>
        /// The Visual Studio installation path (to devenv.exe)
        /// </returns>
        /// <param name='debuggerPath'>
        /// The debugger path from the windows registry
        /// </param>
        private static string DeriveVisualStudioPath(string debuggerPath)
        {
            string startSentinel = DeriveProgramFilesSentinel();
            string endSentinel = "Common7";
            bool started = false;
            string[] tokens = debuggerPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            // Walk directories in debugger path, chop out "Program Files\INSTALLATION\PATH\HERE\Common7"
            foreach (var token in tokens)
            {
                if (!started && string.Equals(startSentinel, token, StringComparison.OrdinalIgnoreCase))
                {
                    started = true;
                    continue;
                }
                if (started)
                {
                    path = Path.Combine(path, token);
                    if (string.Equals(endSentinel, token, StringComparison.OrdinalIgnoreCase))
                        break;
                }
            }

            return UnityEditor.Utils.Paths.Combine(path, "IDE", "devenv.exe");
        }

        /// <summary>
        /// Derives the program files sentinel for grabbing the VS installation path.
        /// </summary>
        /// <remarks>
        /// From a path like 'c:\Archivos de programa (x86)', returns 'Archivos de programa'
        /// </remarks>
        private static string DeriveProgramFilesSentinel()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault();

            if (!string.IsNullOrEmpty(path))
            {
                // This needs to be the "real" Program Files regardless of 64bitness
                int index = path.LastIndexOf("(x86)");
                if (0 <= index)
                    path = path.Remove(index);
                return path.TrimEnd();
            }

            return "Program Files";
        }

        /// <summary>
        /// Checks whether two paths are equivalent
        /// </summary>
        /// <returns>
        /// Whether the paths are equivalent
        /// </returns>
        /// <param name='aPath'>
        /// A path
        /// </param>
        /// <param name='zPath'>
        /// Another path
        /// </param>
        private static bool PathsAreEquivalent(string aPath, string zPath)
        {
            if (aPath == null && zPath == null)
                return true;
            if (string.IsNullOrEmpty(aPath) || string.IsNullOrEmpty(zPath))
                return false;

            aPath = Path.GetFullPath(aPath);
            zPath = Path.GetFullPath(zPath);

            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            if (!(SolutionSynchronizationSettings.IsOSX || SolutionSynchronizationSettings.IsWindows))
                comparison = StringComparison.Ordinal; // Linux

            aPath = aPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            zPath = zPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return string.Equals(aPath, zPath, comparison);
        }

        internal static bool CheckVisualStudioVersion(int major, int minor, int build)
        {
            int haveMinor = -1;
            int haveBuild = -1;

            switch (major)
            {
                case 11: // Visual Studio 2012, getting it's version is different from others
                {
                    // we'll grab version from (replace 11.0 with highest found 11.*):
                    // HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\DevDiv\vc\Servicing\11.0\RuntimeDebug\Version
                    Microsoft.Win32.RegistryKey servicing = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\DevDiv\vc\Servicing");
                    if (servicing == null) return false;
                    foreach (string name in servicing.GetSubKeyNames())
                    {
                        if (name.StartsWith("11.") && name.Length > 3)
                            try
                            {
                                int foundMinor = Convert.ToInt32(name.Substring(3));
                                if (foundMinor > haveMinor)
                                    haveMinor = foundMinor;
                            }
                            catch (System.Exception)
                            {}
                    }
                    if (haveMinor < 0) return false;
                    Microsoft.Win32.RegistryKey key = servicing.OpenSubKey(string.Format(@"11.{0}\RuntimeDebug", haveMinor));
                    if (key == null) return false;
                    string value = key.GetValue("Version", null) as string;
                    if (value == null) return false;
                    string[] components = value.Split('.');
                    if (components == null || components.Length < 3) return false;
                    try
                    {
                        haveBuild = Convert.ToInt32(components[2]);
                    }
                    catch (System.Exception)
                    {
                        return false;
                    }
                }
                break;
                default:
                    return false;
            }

            return haveMinor > minor || (haveMinor == minor && haveBuild >= build);
        }
    }
}
