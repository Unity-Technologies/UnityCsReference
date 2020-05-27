// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using Object = UnityEngine.Object;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using UnityEditor.Connect;
using UnityEditor.Utils;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        class Styles
        {
            public GUIContent invalidColorSpaceMessage = EditorGUIUtility.TrTextContent("In order to build a player, go to 'Player Settings...' to resolve the incompatibility between the Color Space and the current settings.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIContent invalidLightmapEncodingMessage = EditorGUIUtility.TrTextContent("In order to build a player, go to 'Player Settings...' to resolve the incompatibility between the selected Lightmap Encoding and the current settings.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIContent invalidVirtualTexturingSettingMessage = EditorGUIUtility.TrTextContent("Cannot build player because Virtual Texturing is enabled, but the target platform or graphics API does not support Virtual Texturing. Go to Player Settings to resolve the incompatibility.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIContent compilingMessage = EditorGUIUtility.TrTextContent("Cannot build player while editor is importing assets or compiling scripts.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIStyle title = EditorStyles.boldLabel;
            public GUIStyle evenRow = "CN EntryBackEven";
            public GUIStyle oddRow = "CN EntryBackOdd";
            public GUIStyle platformSelector = "PlayerSettingsPlatform";

            public GUIContent platformTitle = EditorGUIUtility.TrTextContent("Platform", "Which platform to build for");
            public GUIContent switchPlatform = EditorGUIUtility.TrTextContent("Switch Platform");
            public GUIContent build = EditorGUIUtility.TrTextContent("Build");
            public GUIContent buildAndRun = EditorGUIUtility.TrTextContent("Build And Run");
            public GUIContent scenesInBuild = EditorGUIUtility.TrTextContent("Scenes In Build", "Which scenes to include in the build");
            public GUIContent checkOut = EditorGUIUtility.TrTextContent("Check out");
            public GUIContent addOpenSource = EditorGUIUtility.TrTextContent("Add Open Scenes");
            public string noModuleLoaded = L10n.Tr("No {0} module loaded.");
            public GUIContent openDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
            public GUIContent installModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
            public string infoText = L10n.Tr("{0} is not included in your Unity Pro license. Your {0} build will include a Unity Personal Edition splash screen.\n\nYou must be eligible to use Unity Personal Edition to use this build option. Please refer to our EULA for further information.");
            public GUIContent eula = EditorGUIUtility.TrTextContent("Eula");
            public string addToYourPro = L10n.Tr("Add {0} to your Unity Pro license");

            public Texture2D activePlatformIcon = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image as Texture2D;

            public const float kButtonWidth = 110;

            public string shopURL = "https://store.unity3d.com/shop/";

            public GUIContent GetDownloadErrorForTarget(BuildTarget target)
            {
                return null;
            }

            // string and matching enum values for standalone subtarget dropdowm
            public GUIContent debugBuild = EditorGUIUtility.TrTextContent("Development Build");
            public GUIContent autoconnectProfiler = EditorGUIUtility.TrTextContent("Autoconnect Profiler", "When the build is started, an open Profiler Window will automatically connect to the Player and start profiling. The \"Build And Run\" option will also automatically open the Profiler Window.");
            public GUIContent autoconnectProfilerDisabled = EditorGUIUtility.TrTextContent("Autoconnect Profiler", "Profiling is only enabled in a Development Player.");
            public GUIContent buildWithDeepProfiler = EditorGUIUtility.TrTextContent("Deep Profiling Support", "Build Player with Deep Profiling Support. This might affect Player performance.");
            public GUIContent buildWithDeepProfilerDisabled = EditorGUIUtility.TrTextContent("Deep Profiling", "Profiling is only enabled in a Development Player.");
            public GUIContent allowDebugging = EditorGUIUtility.TrTextContent("Script Debugging");
            public GUIContent waitForManagedDebugger = EditorGUIUtility.TrTextContent("Wait For Managed Debugger", "Show a dialog where you can attach a managed debugger before any script execution.");
            public GUIContent explicitNullChecks = EditorGUIUtility.TrTextContent("Explicit Null Checks");
            public GUIContent explicitDivideByZeroChecks = EditorGUIUtility.TrTextContent("Divide By Zero Checks");
            public GUIContent explicitArrayBoundsChecks = EditorGUIUtility.TrTextContent("Array Bounds Checks");
            public GUIContent learnAboutUnityCloudBuild = EditorGUIUtility.TrTextContent("Learn about Unity Cloud Build");
            public GUIContent compressionMethod = EditorGUIUtility.TrTextContent("Compression Method", "Compression applied to Player data (scenes and resources).\nDefault - none or default platform compression.\nLZ4 - fast compression suitable for Development Builds.\nLZ4HC - higher compression rate variance of LZ4, causes longer build times. Works best for Release Builds.");

            public Compression[] compressionTypes =
            {
                Compression.None,
                Compression.Lz4,
                Compression.Lz4HC
            };

            public GUIContent[] compressionStrings =
            {
                EditorGUIUtility.TrTextContent("Default"),
                EditorGUIUtility.TrTextContent("LZ4"),
                EditorGUIUtility.TrTextContent("LZ4HC"),
            };
        }


        Vector2 scrollPosition = new Vector2(0, 0);

        private const string kEditorBuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

        static Styles styles = null;

        static bool isEditorinstalledWithHub = IsEditorInstalledWithHub();

        public static void ShowBuildPlayerWindow()
        {
            EditorUserBuildSettings.selectedBuildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            EditorWindow.GetWindow<BuildPlayerWindow>(true, "Build Settings");
        }

        static bool BuildLocationIsValid(string path)
        {
            return path.Length > 0 && System.IO.Directory.Exists(FileUtil.DeleteLastPathNameComponent(path));
        }

        // This overload is used by the Build & Run menu item & hot key - prompt for location only if the configured
        // output location is not valid.
        static void BuildPlayerAndRun()
        {
            var buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            var lastBuildLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);
            bool buildLocationIsValid = BuildLocationIsValid(lastBuildLocation);

            if (buildLocationIsValid && (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64))
            {
                // Case 1208041: Windows Standalone .exe name depends on productName player setting
                var newBuildLocation = Path.Combine(Path.GetDirectoryName(lastBuildLocation), Paths.MakeValidFileName(PlayerSettings.productName) + ".exe").Replace(Path.DirectorySeparatorChar, '/');
                EditorUserBuildSettings.SetBuildLocation(buildTarget, newBuildLocation);
            }

            BuildPlayerAndRun(!buildLocationIsValid);
        }

        // This overload is used by the default player window, to always prompt for build location
        static void BuildPlayerAndRun(bool askForBuildLocation)
        {
            CallBuildMethods(askForBuildLocation, BuildOptions.AutoRunPlayer | BuildOptions.StrictMode);
        }

        public BuildPlayerWindow()
        {
            position = new Rect(50, 50, 540, 530);
            minSize = new Vector2(630, 580);
            titleContent = EditorGUIUtility.TrTextContent("Build Settings");
        }

        BuildPlayerSceneTreeView m_TreeView = null;
        [SerializeField]
        IMGUI.Controls.TreeViewState m_TreeViewState;
        void ActiveScenesGUI()
        {
            if (m_TreeView == null)
            {
                if (m_TreeViewState == null)
                    m_TreeViewState = new IMGUI.Controls.TreeViewState();
                m_TreeView = new BuildPlayerSceneTreeView(m_TreeViewState);
                m_TreeView.Reload();
            }
            Rect scenesInBuildRect = GUILayoutUtility.GetRect(styles.scenesInBuild, styles.title);
            GUI.Label(scenesInBuildRect, styles.scenesInBuild, styles.title);

            Rect rect = GUILayoutUtility.GetRect(0, position.width, 0, position.height);
            m_TreeView.OnGUI(rect);
        }

        void OnDisable()
        {
            if (m_TreeView != null)
                m_TreeView.UnsubscribeListChange();
        }

        void AddOpenScenes()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool isSceneAdded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.path.Length == 0 && !EditorSceneManager.SaveScene(scene, "", false))
                    continue;

                if (list.Any(s => s.path == scene.path))
                    continue;

                GUID newGUID;
                GUID.TryParse(scene.guid, out newGUID);
                var buildSettingsScene = (newGUID == default(GUID)) ?
                    new EditorBuildSettingsScene(scene.path, true) :
                    new EditorBuildSettingsScene(newGUID, true);
                list.Add(buildSettingsScene);
                isSceneAdded = true;
            }

            if (!isSceneAdded)
                return;

            EditorBuildSettings.scenes = list.ToArray();
            m_TreeView.Reload();
            Repaint();
            GUIUtility.ExitGUI();
        }

        // TODO: Move this into platform extension dll
        private bool IsBuildTargetCompatibleWithOS(BuildTarget target)
        {
            // Windows Store can only be used on Windows
            if (target == BuildTarget.WSAPlayer && SystemInfo.operatingSystemFamily != OperatingSystemFamily.Windows)
                return false;

            return true;
        }

        private void ActiveBuildTargetsGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginVertical(GUILayout.Width(255));
            GUILayout.Label(styles.platformTitle, styles.title);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "OL Box");

            // Draw enabled build targets first, then draw disabled build targets
            for (int requireEnabled = 0; requireEnabled < 2; requireEnabled++)
            {
                bool showRequired = requireEnabled == 0;
                bool even = false;
                foreach (BuildPlatform gt in BuildPlatforms.instance.buildPlatforms)
                {
                    if (IsBuildTargetGroupSupported(gt.targetGroup, gt.defaultTarget) != showRequired)
                        continue;

                    // Some build targets are not publicly available, show them only when they are actually in use
                    if (!IsBuildTargetGroupSupported(gt.targetGroup, gt.defaultTarget) && !gt.forceShowTarget)
                        continue;

                    // Some build targets are only compatible with specific OS
                    if (!IsBuildTargetCompatibleWithOS(gt.defaultTarget))
                        continue;

                    ShowOption(gt, gt.title, even ? styles.evenRow : styles.oddRow);
                    even = !even;
                }
                GUI.contentColor = Color.white;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Switching build target in the editor
            BuildTarget selectedTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            BuildTargetGroup selectedTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            GUILayout.BeginHorizontal();

            GUI.enabled = BuildPipeline.IsBuildTargetSupported(selectedTargetGroup, selectedTarget);
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Player Settings..."), GUILayout.Width(Styles.kButtonWidth)))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        void ShowAlert()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent("Unable to access Unity services. Please log in, or request membership to this project to use these services.").text, MessageType.Warning);
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        void ShowOption(BuildPlatform bp, GUIContent title, GUIStyle background)
        {
            Rect r = GUILayoutUtility.GetRect(50, 36);
            r.x += 1;
            r.y += 1;
            bool valid = BuildPipeline.LicenseCheck(bp.defaultTarget);
            GUI.contentColor = new Color(1, 1, 1, valid ? 1 : .7f);
            bool selected = EditorUserBuildSettings.selectedBuildTargetGroup == bp.targetGroup;

            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, selected, false);

                Texture image = null;

                if (selected && title.image)
                {
                    image = EditorUtility.GetIconInActiveState(title.image);
                }

                if (image == null)
                {
                    image = title.image;
                }

                GUI.Label(new Rect(r.x + 3, r.y + 3, 32, 32), image, GUIStyle.none);

                if (EditorUserBuildSettings.activeBuildTargetGroup == bp.targetGroup)
                    GUI.Label(new Rect(r.xMax - styles.activePlatformIcon.width - 8, r.y + 3 + (32 - styles.activePlatformIcon.height) / 2,
                        styles.activePlatformIcon.width, styles.activePlatformIcon.height),
                        styles.activePlatformIcon, GUIStyle.none);
            }

            if (GUI.Toggle(r, selected, title.text, styles.platformSelector))
            {
                if (EditorUserBuildSettings.selectedBuildTargetGroup != bp.targetGroup)
                {
                    EditorUserBuildSettings.selectedBuildTargetGroup = bp.targetGroup;

                    // Repaint inspectors, as they may be showing platform target specific things.
                    Object[] inspectors = Resources.FindObjectsOfTypeAll(typeof(InspectorWindow));
                    for (int i = 0; i < inspectors.Length; i++)
                    {
                        InspectorWindow inspector = inspectors[i] as InspectorWindow;
                        if (inspector != null)
                            inspector.Repaint();
                    }
                }
            }
        }

        void OnGUI()
        {
            if (styles == null)
            {
                styles = new Styles();
            }

            if (!UnityConnect.instance.canBuildWithUPID)
            {
                ShowAlert();
            }
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            string message = "";
            var buildSettingsLocked = !AssetDatabase.IsOpenForEdit(kEditorBuildSettingsPath, out message, StatusQueryOptions.UseCachedIfPossible);

            using (new EditorGUI.DisabledScope(buildSettingsLocked))
            {
                ActiveScenesGUI();
                // Clear all and Add Current Scene
                GUILayout.BeginHorizontal();
                if (buildSettingsLocked)
                {
                    GUI.enabled = true;
                    if (GUILayout.Button(styles.checkOut))
                        AssetDatabase.MakeEditable(kEditorBuildSettingsPath);
                    GUILayout.Label(message);
                    GUI.enabled = false;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(styles.addOpenSource))
                    AddOpenScenes();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal(GUILayout.Height(351));
            ActiveBuildTargetsGUI();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            ShowBuildTargetSettings();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        internal static bool IsBuildTargetGroupSupported(BuildTargetGroup targetGroup, BuildTarget target)
        {
            if (targetGroup == BuildTargetGroup.Standalone)
                return true;
            else
                return BuildPipeline.IsBuildTargetSupported(targetGroup, target);
        }

        static bool IsAnyStandaloneModuleLoaded()
        {
            return ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneLinux64) ||
                ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneOSX) ||
                ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneWindows);
        }

        static bool IsColorSpaceValid(BuildPlatform platform)
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                var hasMinGraphicsAPI = true;

                var apis = PlayerSettings.GetGraphicsAPIs(platform.defaultTarget);
                if (platform.targetGroup == BuildTargetGroup.Android)
                {
                    hasMinGraphicsAPI = (apis.Contains(GraphicsDeviceType.Vulkan) || apis.Contains(GraphicsDeviceType.OpenGLES3)) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }
                else if (platform.targetGroup == BuildTargetGroup.iOS || platform.targetGroup == BuildTargetGroup.tvOS)
                {
                    hasMinGraphicsAPI = !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }
                else if (platform.targetGroup == BuildTargetGroup.WebGL)
                {
                    // must have OpenGLES3-only
                    hasMinGraphicsAPI = apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }

                return hasMinGraphicsAPI;
            }
            else
            {
                return true;
            }
        }

        static bool IsLightmapEncodingValid(BuildPlatform platform)
        {
            if (PlayerSettings.GetLightmapEncodingQualityForPlatformGroup(platform.targetGroup) != LightmapEncodingQuality.Low)
            {
                var hasMinGraphicsAPI = true;

                if (platform.targetGroup == BuildTargetGroup.iOS)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
                    hasMinGraphicsAPI = apis.Contains(GraphicsDeviceType.Metal) && !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }
                else if (platform.targetGroup == BuildTargetGroup.tvOS)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.tvOS);
                    hasMinGraphicsAPI = apis.Contains(GraphicsDeviceType.Metal) && !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }
                else if (platform.targetGroup == BuildTargetGroup.Android)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                    hasMinGraphicsAPI = (apis.Contains(GraphicsDeviceType.Vulkan) || apis.Contains(GraphicsDeviceType.OpenGLES3)) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }

                return hasMinGraphicsAPI;
            }
            else
            {
                return true;
            }
        }

        static bool IsVirtualTexturingSettingsValid(BuildPlatform platform)
        {
            if (!PlayerSettings.GetVirtualTexturingSupportEnabled())
            {
                return true;
            }

            if (!UnityEngine.Rendering.VirtualTexturingEditor.Building.IsPlatformSupportedForPlayer(platform.defaultTarget))
            {
                return false;
            }

            GraphicsDeviceType[] gfxTypes = PlayerSettings.GetGraphicsAPIs(platform.defaultTarget);
            bool supportedAPI = true;
            foreach (GraphicsDeviceType api in gfxTypes)
            {
                supportedAPI &= UnityEngine.Rendering.VirtualTexturingEditor.Building.IsRenderAPISupported(api, platform.defaultTarget, false);
            }

            return supportedAPI;
        }

        // Major.Minor.Micro followed by one of abxfp followed by an identifier, optionally suffixed with " (revisionhash)"
        static Regex s_VersionPattern = new Regex(@"(?<shortVersion>\d+\.\d+\.\d+(?<suffix>((?<alphabeta>[abx])|[fp])[^\s]*))( \((?<revision>[a-fA-F\d]+)\))?",
            RegexOptions.Compiled);
        static Dictionary<string, string> s_ModuleNames = new Dictionary<string, string>()
        {
            { "tvOS", "AppleTV" },
            { "OSXStandalone", "Mac" },
            { "WindowsStandalone", "Windows" },
            { "LinuxStandalone", "Linux" },
            { "UWP", "Universal-Windows-Platform"}
        };
        static public string GetPlaybackEngineDownloadURL(string moduleName)
        {
            if (moduleName == "PS4" || moduleName == "XboxOne")
                return "https://unity3d.com/platform-installation";

            string fullVersion = InternalEditorUtility.GetFullUnityVersion();
            string revision = "";
            string shortVersion = "";
            Match versionMatch = s_VersionPattern.Match(fullVersion);
            if (!versionMatch.Success || !versionMatch.Groups["shortVersion"].Success || !versionMatch.Groups["suffix"].Success)
                Debug.LogWarningFormat("Error parsing version '{0}'", fullVersion);

            if (versionMatch.Groups["shortVersion"].Success)
                shortVersion = versionMatch.Groups["shortVersion"].Value;
            if (versionMatch.Groups["revision"].Success)
                revision = versionMatch.Groups["revision"].Value;

            if (s_ModuleNames.ContainsKey(moduleName))
                moduleName = s_ModuleNames[moduleName];

            string prefix = "download";
            string suffix = "download_unity";
            string folder = "Unknown";
            string extension = string.Empty;

            if (versionMatch.Groups["alphabeta"].Success)
            {
                // These releases are hosted on the beta site
                prefix = "beta";
                suffix = "download";
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                folder = "TargetSupportInstaller";
                extension = ".exe";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                folder = "MacEditorTargetInstaller";
                extension = ".pkg";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                if (moduleName == "Android" || moduleName == "Mac" || moduleName == "Windows")
                {
                    folder = "MacEditorTargetInstaller";
                    extension = ".pkg";
                }
                else
                {
                    folder = "LinuxEditorTargetInstaller";
                    extension = ".tar.xz";
                }
            }

            return string.Format("http://{0}.unity3d.com/{1}/{2}/{3}/UnitySetup-{4}-Support-for-Editor-{5}{6}", prefix, suffix, revision, folder, moduleName, shortVersion, extension);
        }

        static string GetUnityHubModuleDownloadURL(string moduleName)
        {
            string fullVersion = InternalEditorUtility.GetFullUnityVersion();
            string revision = "";
            string shortVersion = "";
            Match versionMatch = s_VersionPattern.Match(fullVersion);
            if (!versionMatch.Success || !versionMatch.Groups["shortVersion"].Success || !versionMatch.Groups["suffix"].Success)
                Debug.LogWarningFormat("Error parsing version '{0}'", fullVersion);

            if (versionMatch.Groups["shortVersion"].Success)
                shortVersion = versionMatch.Groups["shortVersion"].Value;
            if (versionMatch.Groups["revision"].Success)
                revision = versionMatch.Groups["revision"].Value;

            if (s_ModuleNames.ContainsKey(moduleName))
                moduleName = s_ModuleNames[moduleName];

            return string.Format("unityhub://{0}/{1}/module={2}", shortVersion, revision, moduleName.ToLower());
        }

        bool IsModuleNotInstalled(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            bool licensed = BuildPipeline.LicenseCheck(buildTarget);

            string moduleName = Modules.ModuleManager.GetTargetStringFrom(buildTargetGroup, buildTarget);

            return licensed &&
                !string.IsNullOrEmpty(moduleName) &&
                Modules.ModuleManager.GetBuildPostProcessor(moduleName) == null &&
                (BuildTargetGroup.Standalone != EditorUserBuildSettings.selectedBuildTargetGroup ||
                    !IsAnyStandaloneModuleLoaded());
        }

        static bool IsEditorInstalledWithHub()
        {
            var applicationFolderPath = Directory.GetParent(EditorApplication.applicationPath).FullName;
            var path = "";

            if (Application.platform == RuntimePlatform.OSXEditor)
                path = Path.Combine(applicationFolderPath, "modules.json");
            else if (Application.platform == RuntimePlatform.WindowsEditor)
                path = Path.Combine(Directory.GetParent(applicationFolderPath).FullName, "modules.json");
            else if (Application.platform == RuntimePlatform.LinuxEditor)
                path = Path.Combine(Directory.GetParent(applicationFolderPath).FullName, "modules.json");

            return System.IO.File.Exists(path);
        }

        void ShowBuildTargetSettings()
        {
            EditorGUIUtility.labelWidth = Mathf.Min(180, (position.width - 265) * 0.47f);

            BuildTarget buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromTargetGroup(buildTargetGroup);
            bool licensed = BuildPipeline.LicenseCheck(buildTarget);

            // Draw the group name
            GUILayout.Space(18);

            // Draw icon and text of title separately so we can control the space between them
            Rect r = GUILayoutUtility.GetRect(50, 36);
            r.x += 1;
            GUI.Label(new Rect(r.x + 3, r.y + 3, 32, 32), platform.title.image, GUIStyle.none);
            GUI.Toggle(r, false, platform.title.text, styles.platformSelector);

            GUILayout.Space(10);

            if (platform.targetGroup == BuildTargetGroup.WebGL && !BuildPipeline.IsBuildTargetSupported(platform.targetGroup, buildTarget))
            {
                if (IntPtr.Size == 4)
                {
                    GUILayout.Label("Building for WebGL requires a 64-bit Unity editor.");
                    GUIBuildButtons(false, false, false, platform);
                    return;
                }
            }

            string moduleName = Modules.ModuleManager.GetTargetStringFrom(buildTargetGroup, buildTarget);

            if (IsModuleNotInstalled(buildTargetGroup, buildTarget))
            {
                GUILayout.Label(EditorGUIUtility.TextContent(string.Format(styles.noModuleLoaded, BuildPlatforms.instance.GetModuleDisplayName(buildTargetGroup, buildTarget))));
                string url = "";

                if (!isEditorinstalledWithHub || (moduleName == "PS4" || moduleName == "XboxOne"))
                {
                    if (GUILayout.Button(styles.openDownloadPage, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        url = GetPlaybackEngineDownloadURL(moduleName);
                        Help.BrowseURL(url);
                    }
                }
                else
                {
                    if (GUILayout.Button(styles.installModuleWithHub, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        url = GetUnityHubModuleDownloadURL(moduleName);
                        Help.BrowseURL(url);
                    }
                }
                GUIBuildButtons(false, false, false, platform);
                return;
            }
            else if (Application.HasProLicense() && !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(buildTarget))
            {
                // Show copy for using personal edition build targets with pro edition editor
                string infoText = string.Format(styles.infoText,
                    BuildPlatforms.instance.GetBuildTargetDisplayName(buildTargetGroup, buildTarget));

                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(infoText, EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(styles.eula, EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/legal/eula");
                if (GUILayout.Button(string.Format(styles.addToYourPro, BuildPlatforms.instance.GetBuildTargetDisplayName(buildTargetGroup, buildTarget)), EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/get-unity");
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUIContent error = styles.GetDownloadErrorForTarget(buildTarget);
            if (error != null)
            {
                GUILayout.Label(error, EditorStyles.wordWrappedLabel);
                GUIBuildButtons(false, false, false, platform);
                return;
            }

            // Draw not licensed buy now UI
            if (!licensed)
            {
                string niceName = BuildPipeline.GetBuildTargetGroupDisplayName(buildTargetGroup);
                string licenseMsg = "Your license does not cover {0} Publishing.";
                string buttonMsg = "Go to Our Online Store";
                if (BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsConsole))
                {
                    licenseMsg += "Please see the {0} section of the Platform Module Installation documentation for more details.";
                    buttonMsg = "Platform Module Installation";
                }
                else if (BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsStandalonePlatform))
                    buttonMsg = "";

                GUIContent[] notLicensedMessage =
                {
                    EditorGUIUtility.TextContent(string.Format(L10n.Tr(licenseMsg), niceName)),
                    EditorGUIUtility.TextContent(L10n.Tr(buttonMsg)),
                    new GUIContent(styles.shopURL)
                };

                GUILayout.Label(notLicensedMessage[0], EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (notLicensedMessage[1].text.Length != 0)
                {
                    if (GUILayout.Button(notLicensedMessage[1]))
                    {
                        Application.OpenURL(notLicensedMessage[2].text);
                    }
                }
                GUILayout.EndHorizontal();
                GUIBuildButtons(false, false, false, platform);
                return;
            }

            // FIXME: WHY IS THIS ALL IN ONE FUNCTION?!
            // Draw the side bar to the right. Different options like specific Standalone player to build, profiling and debugging options, etc.
            string module = ModuleManager.GetTargetStringFrom(platform.targetGroup, buildTarget);
            IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(module);
            if (buildWindowExtension != null)
                buildWindowExtension.ShowPlatformBuildOptions();

            GUI.changed = false;
            GUI.enabled = true;

            bool enableBuildButton = buildWindowExtension != null ? buildWindowExtension.EnabledBuildButton() : true;
            bool enableBuildAndRunButton = false;

            bool shouldDrawDebuggingToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawScriptDebuggingCheckbox() : true;
            bool shouldDrawExplicitNullChecksToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawExplicitNullCheckbox() : false;
            bool shouldDrawDivideByZeroChecksToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawExplicitDivideByZeroCheckbox() : false;
            bool shouldDrawArrayBoundsChecksToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawExplicitArrayBoundsCheckbox() : false;
            bool shouldDrawDevelopmentPlayerToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawDevelopmentPlayerCheckbox() : true;

            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(buildTargetGroup, buildTarget);
            bool enableBuildScriptsOnly = (postprocessor != null ? postprocessor.SupportsScriptsOnlyBuild() : false);
            bool canInstallInBuildFolder = false;

            if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
            {
                bool shouldDrawProfilerToggles = buildWindowExtension != null ? buildWindowExtension.ShouldDrawProfilerCheckbox() : true;

                GUI.enabled = shouldDrawDevelopmentPlayerToggle;
                if (shouldDrawDevelopmentPlayerToggle)
                    EditorUserBuildSettings.development = EditorGUILayout.Toggle(styles.debugBuild, EditorUserBuildSettings.development);

                bool developmentBuild = EditorUserBuildSettings.development;

                GUI.enabled = developmentBuild;

                if (shouldDrawProfilerToggles)
                {
                    var profilerDisabled = !GUI.enabled && !developmentBuild;

                    var autoConnectLabel = profilerDisabled ? styles.autoconnectProfilerDisabled : styles.autoconnectProfiler;
                    EditorUserBuildSettings.connectProfiler = EditorGUILayout.Toggle(autoConnectLabel, EditorUserBuildSettings.connectProfiler);

                    var buildWithDeepProfilerLabel = profilerDisabled ? styles.buildWithDeepProfilerDisabled : styles.buildWithDeepProfiler;
                    EditorUserBuildSettings.buildWithDeepProfilingSupport = EditorGUILayout.Toggle(buildWithDeepProfilerLabel, EditorUserBuildSettings.buildWithDeepProfilingSupport);
                }

                GUI.enabled = developmentBuild;
                if (shouldDrawDebuggingToggle)
                {
                    using (new EditorGUI.DisabledScope(buildWindowExtension != null ? buildWindowExtension.ShouldDisableManagedDebuggerCheckboxes() : false))
                    {
                        EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle(styles.allowDebugging, EditorUserBuildSettings.allowDebugging);

                        // Not all platforms have native dialog implemented in Runtime\Misc\GiveDebuggerChanceToAttachIfRequired.cpp
                        // Display this option only for developer builds
                        bool shouldDrawWaitForManagedDebugger = buildWindowExtension != null ? buildWindowExtension.ShouldDrawWaitForManagedDebugger() : false;

                        if (EditorUserBuildSettings.allowDebugging && shouldDrawWaitForManagedDebugger)
                        {
                            EditorUserBuildSettings.waitForManagedDebugger = EditorGUILayout.Toggle(styles.waitForManagedDebugger, EditorUserBuildSettings.waitForManagedDebugger);
                        }
                    }

                    if (EditorUserBuildSettings.allowDebugging && PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP)
                    {
                        var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
                        bool isDebuggerUsable = apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0;

                        if (!isDebuggerUsable)
                            EditorGUILayout.HelpBox("Script debugging is only supported with IL2CPP on .NET 4.x and .NET Standard 2.0 API Compatibility Levels.", MessageType.Warning);
                    }
                }


                if (shouldDrawExplicitNullChecksToggle)
                {
                    // Force 'ExplicitNullChecks' to true if it's a development build.
                    GUI.enabled = !developmentBuild;
                    if (GUI.enabled == false)
                    {
                        EditorUserBuildSettings.explicitNullChecks = true;
                    }
                    EditorUserBuildSettings.explicitNullChecks = EditorGUILayout.Toggle(styles.explicitNullChecks, EditorUserBuildSettings.explicitNullChecks);
                    // Undo force from above
                    GUI.enabled = developmentBuild;
                }

                if (shouldDrawDivideByZeroChecksToggle)
                {
                    // Force 'explicitDivideByZeroChecks' to true if it's a development build.
                    GUI.enabled = !developmentBuild;
                    if (GUI.enabled == false)
                    {
                        EditorUserBuildSettings.explicitDivideByZeroChecks = true;
                    }
                    EditorUserBuildSettings.explicitDivideByZeroChecks = EditorGUILayout.Toggle(styles.explicitDivideByZeroChecks, EditorUserBuildSettings.explicitDivideByZeroChecks);
                    // Undo force from above
                    GUI.enabled = developmentBuild;
                }

                if (shouldDrawArrayBoundsChecksToggle)
                {
                    // Force 'explicitArrayBoundsChecks' to true if it's a development build.
                    GUI.enabled = !developmentBuild;
                    if (GUI.enabled == false)
                    {
                        EditorUserBuildSettings.explicitArrayBoundsChecks = true;
                    }
                    EditorUserBuildSettings.explicitArrayBoundsChecks = EditorGUILayout.Toggle(styles.explicitArrayBoundsChecks, EditorUserBuildSettings.explicitArrayBoundsChecks);
                    // Undo force from above
                    GUI.enabled = developmentBuild;
                }

                if (buildWindowExtension != null && enableBuildScriptsOnly)
                    buildWindowExtension.DoScriptsOnlyGUI();

                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                if (postprocessor != null && postprocessor.SupportsLz4Compression())
                {
                    var cmpIdx = Array.IndexOf(styles.compressionTypes, EditorUserBuildSettings.GetCompressionType(buildTargetGroup));
                    if (cmpIdx == -1)
                        cmpIdx = Array.IndexOf(styles.compressionTypes, postprocessor.GetDefaultCompression());
                    if (cmpIdx == -1)
                        cmpIdx = 1; // Lz4 by default.
                    cmpIdx = EditorGUILayout.Popup(styles.compressionMethod, cmpIdx, styles.compressionStrings);
                    EditorUserBuildSettings.SetCompressionType(buildTargetGroup, styles.compressionTypes[cmpIdx]);
                }

                canInstallInBuildFolder = Unsupported.IsSourceBuild() && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTargetGroup, buildTarget);

                if (enableBuildButton)
                {
                    enableBuildAndRunButton = buildWindowExtension != null ? buildWindowExtension.EnabledBuildAndRunButton()
                        && !(EditorUserBuildSettings.installInBuildFolder) : !(EditorUserBuildSettings.installInBuildFolder);
                }
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                GUILayout.Label(string.Format(L10n.Tr("{0} is not supported in this build.\nDownload a build that supports it."), BuildPipeline.GetBuildTargetGroupDisplayName(buildTargetGroup)));

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUIBuildButtons(buildWindowExtension, enableBuildButton, enableBuildAndRunButton,
                canInstallInBuildFolder, platform);
        }

        private static void GUIBuildButtons(bool enableBuildButton,
            bool enableBuildAndRunButton,
            bool canInstallInBuildFolder,
            BuildPlatform platform)
        {
            GUIBuildButtons(null, enableBuildButton, enableBuildAndRunButton, canInstallInBuildFolder, platform);
        }

        private static void GUIBuildButtons(IBuildWindowExtension buildWindowExtension,
            bool enableBuildButton,
            bool enableBuildAndRunButton,
            bool canInstallInBuildFolder,
            BuildPlatform platform)
        {
            GUILayout.FlexibleSpace();


            if (canInstallInBuildFolder)
            {
                GUILayout.BeginHorizontal();
                EditorUserBuildSettings.installInBuildFolder = GUILayout.Toggle(EditorUserBuildSettings.installInBuildFolder, "Install in Builds folder\n(for debugging with source code)", GUILayout.ExpandWidth(false));

                var content = new GUIContent(EditorGUI.GUIContents.helpIcon);
                content.tooltip = "Open documentation for debugging source code";
                if (GUILayout.Button(content, EditorStyles.iconButton))
                {
                    var path = Path.Combine(Directory.GetParent(EditorApplication.applicationPath).FullName, "../../Documentation/BuildDocs/view_build_docs");
                    if (Application.platform == RuntimePlatform.WindowsEditor)
                        System.Diagnostics.Process.Start(path + ".cmd");
                    else
                        System.Diagnostics.Process.Start("/bin/bash", path);
                }
                GUILayout.EndHorizontal();
            }
            else
                EditorUserBuildSettings.installInBuildFolder = false;

            if ((buildWindowExtension != null) && Unsupported.IsSourceBuild())
                buildWindowExtension.ShowInternalPlatformBuildOptions();


            if (buildWindowExtension != null)
                buildWindowExtension.ShowPlatformBuildWarnings();

            // Disable the 'Build' and 'Build And Run' buttons when the project setup doesn't satisfy the platform requirements
            if (!IsColorSpaceValid(platform) && enableBuildButton && enableBuildAndRunButton)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(styles.invalidColorSpaceMessage);
            }
            else if (!IsLightmapEncodingValid(platform) && enableBuildButton && enableBuildAndRunButton)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(styles.invalidLightmapEncodingMessage);
            }
            else if (!IsVirtualTexturingSettingsValid(platform) && enableBuildButton && enableBuildAndRunButton)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(styles.invalidVirtualTexturingSettingMessage);
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(styles.compilingMessage);
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.LinkLabel(styles.learnAboutUnityCloudBuild))
            {
                Application.OpenURL(string.Format("{0}/from/editor/buildsettings?upid={1}&pid={2}&currentplatform={3}&selectedplatform={4}&unityversion={5}",
                    UnityEditorInternal.WebURLs.cloudBuildPage, CloudProjectSettings.projectId, PlayerSettings.productGUID, EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget(), Application.unityVersion));
            }
            GUILayout.EndHorizontal();
            // Space 6 for alignment with platform column and to reduce missclicks with Build And Run button
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIContent buildButton = null;
            GUIContent buildAndRunButton = null;
            bool askForBuildLocation = true;
            if (buildWindowExtension != null)
            {
                buildWindowExtension.GetBuildButtonTitles(out buildButton, out buildAndRunButton);
                askForBuildLocation = buildWindowExtension.AskForBuildLocation();
            }

            buildButton = buildButton ?? styles.build;
            buildAndRunButton = buildAndRunButton ?? styles.buildAndRun;

            // Switching build target in the editor
            BuildTarget selectedTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            BuildTargetGroup selectedTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            bool selectedTargetIsActive = EditorUserBuildSettings.activeBuildTarget == selectedTarget &&
                EditorUserBuildSettings.activeBuildTargetGroup == selectedTargetGroup;

            if (selectedTargetIsActive)
            {
                // Build Button
                GUI.enabled = enableBuildButton;
                if (GUILayout.Button(buildButton, GUILayout.Width(Styles.kButtonWidth)))
                {
                    CallBuildMethods(askForBuildLocation, BuildOptions.ShowBuiltPlayer);
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                GUI.enabled = BuildPipeline.IsBuildTargetSupported(selectedTargetGroup, selectedTarget) && EditorUserBuildSettings.activeBuildTargetGroup != selectedTargetGroup;
                if (GUILayout.Button(styles.switchPlatform, GUILayout.Width(Styles.kButtonWidth)))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(selectedTargetGroup, selectedTarget);
                    GUIUtility.ExitGUI();
                }
            }

            // Build and Run button
            GUI.enabled = enableBuildAndRunButton && selectedTargetIsActive;
            if (GUILayout.Button(buildAndRunButton, GUILayout.Width(Styles.kButtonWidth)))
            {
                BuildPlayerAndRun(askForBuildLocation);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }
    }
}
