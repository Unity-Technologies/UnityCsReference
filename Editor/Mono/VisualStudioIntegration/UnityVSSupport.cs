// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UnityEditorInternal;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.VisualStudioIntegration
{
    internal class UnityVSSupport
    {
        private static bool m_ShouldUnityVSBeActive;
        public static string s_UnityVSBridgeToLoad;
        private static bool? s_IsUnityVSEnabled;
        private static string s_AboutLabel;

        [RequiredByNativeCode]
        public static void InitializeUnityVSSupport()
        {
            Initialize(null);
        }

        public static void Initialize()
        {
            Initialize(null);
        }

        public static void Initialize(string editorPath)
        {
            var externalEditor = editorPath ?? ScriptEditorUtility.GetExternalScriptEditor();

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                InitializeVSForMac(externalEditor);
                return;
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
                InitializeVisualStudio(externalEditor);
        }

        private static void InitializeVSForMac(string externalEditor)
        {
            Version vsfmVersion;
            if (!IsVSForMac(externalEditor, out vsfmVersion))
                return;

            m_ShouldUnityVSBeActive = true;

            var bridgeFile = GetVSForMacBridgeAssembly(externalEditor, vsfmVersion);
            if (string.IsNullOrEmpty(bridgeFile) || !File.Exists(bridgeFile))
            {
                Console.WriteLine("Unable to find Tools for Unity bridge dll for Visual Studio for Mac " + externalEditor);
                return;
            }

            s_UnityVSBridgeToLoad = bridgeFile;
            InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileNameWithoutExtension(bridgeFile), bridgeFile);
        }

        private static bool IsVSForMac(string externalEditor, out Version vsfmVersion)
        {
            vsfmVersion = null;

            if (!externalEditor.ToLower().EndsWith("visual studio.app"))
                return false;

            // We need to extract the version used by VS for Mac
            // to lookup its addin registry
            try
            {
                return GetVSForMacVersion(externalEditor, out vsfmVersion);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to read Visual Studio for Mac information: {0}", e);
                return false;
            }
        }

        private static bool GetVSForMacVersion(string externalEditor, out Version vsfmVersion)
        {
            vsfmVersion = null;

            // Read the full VS for Mac version from the plist, it will look like this:
            //
            // <key>CFBundleShortVersionString</key>
            // <string>X.X.X.X</string>

            var plist = Path.Combine(externalEditor, "Contents/Info.plist");
            if (!File.Exists(plist))
                return false;

            const string versionStringRegex = @"\<key\>CFBundleShortVersionString\</key\>\s+\<string\>(?<version>\d+\.\d+\.\d+\.\d+?)\</string\>";

            var file = File.ReadAllText(plist);
            var match = Regex.Match(file, versionStringRegex);
            var versionGroup = match.Groups["version"];
            if (!versionGroup.Success)
                return false;

            vsfmVersion = new Version(versionGroup.Value);
            return true;
        }

        private static string GetVSForMacBridgeAssembly(string externalEditor, Version vsfmVersion)
        {
            // Check first if we're overriden
            // Useful when developing UnityVS for Mac
            var bridge = Environment.GetEnvironmentVariable("VSTUM_BRIDGE");
            if (!string.IsNullOrEmpty(bridge) && File.Exists(bridge))
                return bridge;

            // Look for installed addin
            const string addinBridge = "Editor/SyntaxTree.VisualStudio.Unity.Bridge.dll";
            const string addinName = "MonoDevelop.Unity";

            // Check if we're installed in the user addins repository
            // ~/Library/Application Support/VisualStudio/X.0/LocalInstall/Addins
            var localAddins = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library/Application Support/VisualStudio/" + vsfmVersion.Major + ".0" + "/LocalInstall/Addins");

            // In the user addins repository, the addins are suffixed by their versions, like `MonoDevelop.Unity.1.0`
            // When installing another local user addin, MD will remove files inside the folder
            // So we browse all VSTUM addins, and return the one with a bridge, which is the one MD will load
            if (Directory.Exists(localAddins))
            {
                foreach (var folder in Directory.GetDirectories(localAddins, addinName + "*", SearchOption.TopDirectoryOnly))
                {
                    bridge = Path.Combine(folder, addinBridge);
                    if (File.Exists(bridge))
                        return bridge;
                }
            }

            // Check in Visual Studio.app/
            // In that case the name of the addin is used
            bridge = Path.Combine(externalEditor, "Contents/Resources/lib/monodevelop/AddIns/" + addinName + "/" + addinBridge);
            if (File.Exists(bridge))
                return bridge;

            return null;
        }

        private static void InitializeVisualStudio(string externalEditor)
        {
            if (externalEditor.EndsWith("UnityVS.OpenFile.exe"))
            {
                externalEditor = SyncVS.FindBestVisualStudio();
                if (externalEditor != null)
                    ScriptEditorUtility.SetExternalScriptEditor(externalEditor);
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

        [RequiredByNativeCode]
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
            if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.WindowsEditor)
                return;

            // We reload the domain because selecting a different editor requires loading a different UnityVS
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

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => GetAssemblyLocation(a) == s_UnityVSBridgeToLoad);
            if (assembly == null)
                return "";

            var sb = new StringBuilder("Microsoft Visual Studio Tools for Unity ");
            sb.Append(assembly.GetName().Version);
            sb.Append(" enabled");

            return sb.ToString();
        }
    }
}
