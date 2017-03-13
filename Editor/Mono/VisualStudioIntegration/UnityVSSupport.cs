// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.VisualStudioIntegration
{
    internal class UnityVSSupport
    {
        private static bool m_ShouldUnityVSBeActive;
        public static string s_UnityVSBridgeToLoad;
        private static bool? s_IsUnityVSEnabled;
        private static string s_AboutLabel;

        public static void Initialize()
        {
            Initialize(null);
        }

        public static void Initialize(string editorPath)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return;

            const string kscriptsdefaultapp = "kScriptsDefaultApp";

            var externalEditor = editorPath ?? EditorPrefs.GetString(kscriptsdefaultapp);

            if (externalEditor.EndsWith("UnityVS.OpenFile.exe"))
            {
                externalEditor = SyncVS.FindBestVisualStudio();
                if (externalEditor != null)
                    EditorPrefs.SetString(kscriptsdefaultapp, externalEditor);
            }

            VisualStudioVersion vsVersion;
            if (!IsVisualStudio(externalEditor, out vsVersion))
                return;

            m_ShouldUnityVSBeActive = true;

            var bridgeFile = GetVstuBridgeAssembly(vsVersion);
            if (bridgeFile == null)
            {
                Console.WriteLine("Unable to find bridge dll in registry for Microsoft Visual Studio Tools for Unity for " + externalEditor);
                return;
            }
            if (!File.Exists(bridgeFile))
            {
                Console.WriteLine("Unable to find bridge dll on disk for Microsoft Visual Studio Tools for Unity for " + bridgeFile);
                return;
            }
            s_UnityVSBridgeToLoad = bridgeFile;
            InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileNameWithoutExtension(bridgeFile), bridgeFile);
        }

        static bool IsVisualStudio(string externalEditor, out VisualStudioVersion vsVersion)
        {
            if (string.IsNullOrEmpty(externalEditor))
            {
                vsVersion = VisualStudioVersion.Invalid;
                return false;
            }

            // If it's a VS found through envvars or the registry
            var matches = SyncVS.InstalledVisualStudios.Where(kvp => kvp.Value.Any(v => UnityEditor.Utils.Paths.AreEqual(v.Path, externalEditor, true))).ToArray();
            if (matches.Length > 0)
            {
                vsVersion = matches[0].Key;
                return true;
            }

            // If it's a side-by-side VS selected manually
            if (externalEditor.EndsWith("devenv.exe", StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetVisualStudioVersion(externalEditor, out vsVersion))
                    return true;
            }

            vsVersion = VisualStudioVersion.Invalid;
            return false;
        }

        private static bool TryGetVisualStudioVersion(string externalEditor, out VisualStudioVersion vsVersion)
        {
            switch (ProductVersion(externalEditor).Major)
            {
                case 9:
                    vsVersion = VisualStudioVersion.VisualStudio2008;
                    return true;
                case 10:
                    vsVersion = VisualStudioVersion.VisualStudio2010;
                    return true;
                case 11:
                    vsVersion = VisualStudioVersion.VisualStudio2012;
                    return true;
                case 12:
                    vsVersion = VisualStudioVersion.VisualStudio2013;
                    return true;
                case 14:
                    vsVersion = VisualStudioVersion.VisualStudio2015;
                    return true;
                case 15:
                    vsVersion = VisualStudioVersion.VisualStudio2017;
                    return true;
            }

            vsVersion = VisualStudioVersion.Invalid;
            return false;
        }

        private static Version ProductVersion(string externalEditor)
        {
            try
            {
                return new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(externalEditor).ProductVersion);
            }
            catch (Exception)
            {
                return new Version(0, 0);
            }
        }

        //Called by UnityVS through reflection
        static public bool ShouldUnityVSBeActive()
        {
            return m_ShouldUnityVSBeActive;
        }

        static string GetAssemblyLocation(System.Reflection.Assembly a)
        {
            try
            {
                return a.Location;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        static public bool IsUnityVSEnabled()
        {
            if (!s_IsUnityVSEnabled.HasValue)
                s_IsUnityVSEnabled = m_ShouldUnityVSBeActive && AppDomain.CurrentDomain.GetAssemblies().Any(a => GetAssemblyLocation(a) == s_UnityVSBridgeToLoad);

            return s_IsUnityVSEnabled.Value;
        }

        private static string GetVstuBridgeAssembly(VisualStudioVersion version)
        {
            try
            {
                var vsVersion = string.Empty;

                switch (version)
                {
                    // Starting with VS 15, the registry key is using the VS version
                    // to avoid taking a dependency on the product name
                    case VisualStudioVersion.VisualStudio2017:
                        vsVersion = "15.0";
                        break;
                    // VS 2015 and under are still installed in the registry
                    // using their project names
                    case VisualStudioVersion.VisualStudio2015:
                        vsVersion = "2015";
                        break;
                    case VisualStudioVersion.VisualStudio2013:
                        vsVersion = "2013";
                        break;
                    case VisualStudioVersion.VisualStudio2012:
                        vsVersion = "2012";
                        break;
                    case VisualStudioVersion.VisualStudio2010:
                        vsVersion = "2010";
                        break;
                }

                // search first for the current user with a fallback to machine wide setting
                return GetVstuBridgePathFromRegistry(vsVersion, true)
                    ?? GetVstuBridgePathFromRegistry(vsVersion, false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetVstuBridgePathFromRegistry(string vsVersion, bool currentUser)
        {
            var registryKey = string.Format(@"{0}\Software\Microsoft\Microsoft Visual Studio {1} Tools for Unity",
                    currentUser ? "HKEY_CURRENT_USER" : "HKEY_LOCAL_MACHINE",
                    vsVersion);

            return (string)Registry.GetValue(registryKey, "UnityExtensionPath", null);
        }

        public static void ScriptEditorChanged(string editorPath)
        {
            //on windows, we reload the domain, because selecting a different editor, requires loading a different UnityVS.
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return;

            Initialize(editorPath);

            InternalEditorUtility.RequestScriptReload();
        }

        public static string GetAboutWindowLabel()
        {
            if (s_AboutLabel != null)
                return s_AboutLabel;

            s_AboutLabel = CalculateAboutWindowLabel();
            return s_AboutLabel;
        }

        private static string CalculateAboutWindowLabel()
        {
            if (!IsUnityVSEnabled())
                return "";

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.Location == s_UnityVSBridgeToLoad);
            if (assembly == null)
                return "";

            var sb = new StringBuilder("Microsoft Visual Studio Tools for Unity ");
            sb.Append(assembly.GetName().Version);
            sb.Append(" enabled");

            return sb.ToString();
        }
    }
}
