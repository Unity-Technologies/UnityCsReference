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
using UnityEditor.VersionControl;
using UnityEditor.Modules;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using Object = UnityEngine.Object;
using UnityEditor.Connect;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        class Styles
        {
            public static readonly GUIContent invalidColorSpaceMessage = EditorGUIUtility.TextContent("In order to build a player go to 'Player Settings...' to resolve the incompatibility between the Color Space and the current settings.");
            public GUIStyle selected = "OL SelectedRow";
            public GUIStyle box = "OL Box";
            public GUIStyle title = EditorStyles.boldLabel;
            public GUIStyle evenRow = "CN EntryBackEven";
            public GUIStyle oddRow = "CN EntryBackOdd";
            public GUIStyle platformSelector = "PlayerSettingsPlatform";
            public GUIStyle toggle = "Toggle";
            public GUIStyle levelString = "PlayerSettingsLevel";
            public GUIStyle levelStringCounter = new GUIStyle("Label");
            public Vector2 toggleSize;

            public GUIContent noSessionDialogText = EditorGUIUtility.TextContent("In order to publish your build to UDN, you need to sign in via the AssetStore and tick the 'Stay signed in' checkbox.");
            public GUIContent platformTitle = EditorGUIUtility.TextContent("Platform|Which platform to build for");
            public GUIContent switchPlatform = EditorGUIUtility.TextContent("Switch Platform");
            public GUIContent build = EditorGUIUtility.TextContent("Build");
            public GUIContent export = EditorGUIUtility.TextContent("Export");
            public GUIContent buildAndRun = EditorGUIUtility.TextContent("Build And Run");
            public GUIContent scenesInBuild = EditorGUIUtility.TextContent("Scenes In Build|Which scenes to include in the build");

            public Texture2D activePlatformIcon = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image as Texture2D;

            public const float kButtonWidth = 110;

            // List of platforms that appear in the window. To add one, add it here.
            // Later on, we'll let the users add their own.
            const string kShopURL = "https://store.unity3d.com/shop/";
            const string kDownloadURL = "http://unity3d.com/unity/download/";
            const string kMailURL = "http://unity3d.com/company/sales?type=sales";
            // ADD_NEW_PLATFORM_HERE
            public GUIContent[,] notLicensedMessages =
            {
                { EditorGUIUtility.TextContent("Your license does not cover Standalone Publishing."), new GUIContent(""), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover iOS Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Apple TV Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Android Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Tizen Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Xbox One Publishing."), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
                { EditorGUIUtility.TextContent("Your license does not cover PS Vita Publishing."), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
                { EditorGUIUtility.TextContent("Your license does not cover PS4 Publishing."), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Wii U Publishing."), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Universal Windows Platform Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Windows Phone 8 Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Nintendo 3DS Publishing"), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Facebook Publishing"), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Nintendo Switch Publishing"), EditorGUIUtility.TextContent("Contact sales"), new GUIContent(kMailURL) },
            };

            // ADD_NEW_PLATFORM_HERE
            private GUIContent[,] buildTargetNotInstalled =
            {
                { EditorGUIUtility.TextContent("Standalone Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("iOS Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Apple TV Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Android Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Tizen is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Xbox One Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("PS Vita Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("PS4 Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Wii U Player is not supported in this build.\nDownload a build that supports it."),  null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Universal Windows Platform Player is not supported in\nthis build.\n\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Windows Phone 8 Player is not supported\nin this build.\n\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Nintendo 3DS is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Facebook is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Nintendo Switch is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
            };
            public GUIContent GetTargetNotInstalled(int index, int item)
            {
                if (index >= buildTargetNotInstalled.GetLength(0)) index = 0;
                return buildTargetNotInstalled[index, item];
            }

            public GUIContent GetDownloadErrorForTarget(BuildTarget target)
            {
                return null;
            }

            // string and matching enum values for standalone subtarget dropdowm
            public GUIContent debugBuild = EditorGUIUtility.TextContent("Development Build");
            public GUIContent profileBuild = EditorGUIUtility.TextContent("Autoconnect Profiler");
            public GUIContent allowDebugging = EditorGUIUtility.TextContent("Script Debugging");
            public GUIContent waitForManagedDebugger = EditorGUIUtility.TextContent("Wait For Managed Debugger|Show a dialog where you can attach a managed debugger before any script execution.");
            public GUIContent symlinkiOSLibraries = EditorGUIUtility.TextContent("Symlink Unity libraries");
            public GUIContent explicitNullChecks = EditorGUIUtility.TextContent("Explicit Null Checks");
            public GUIContent explicitDivideByZeroChecks = EditorGUIUtility.TextContent("Divide By Zero Checks");
            public GUIContent enableHeadlessMode = EditorGUIUtility.TextContent("Headless Mode");
            public GUIContent buildScriptsOnly = EditorGUIUtility.TextContent("Scripts Only Build");
            public GUIContent learnAboutUnityCloudBuild = EditorGUIUtility.TextContent("Learn about Unity Cloud Build");
            public GUIContent compressionMethod = EditorGUIUtility.TextContent("Compression Method|Compression applied to Player data (scenes and resources).\nDefault - none or default platform compression.\nLZ4 - fast compression suitable for Development Builds.\nLZ4HC - higher compression rate variance of LZ4, causes longer build times. Works best for Release Builds.");

            public Compression[] compressionTypes =
            {
                Compression.None,
                Compression.Lz4,
                Compression.Lz4HC
            };

            public GUIContent[] compressionStrings =
            {
                EditorGUIUtility.TextContent("Default"),
                EditorGUIUtility.TextContent("LZ4"),
                EditorGUIUtility.TextContent("LZ4HC"),
            };

            public Styles()
            {
                levelStringCounter.alignment = TextAnchor.MiddleRight;

                if (Unsupported.IsDeveloperBuild() && (
                        buildTargetNotInstalled.GetLength(0) != notLicensedMessages.GetLength(0) ||
                        buildTargetNotInstalled.GetLength(0) != BuildPlatforms.instance.buildPlatforms.Length))
                    Debug.LogErrorFormat("Build platforms and messages are desynced in BuildPlayerWindow! ({0} vs. {1} vs. {2}) DON'T SHIP THIS!", buildTargetNotInstalled.GetLength(0), notLicensedMessages.GetLength(0), BuildPlatforms.instance.buildPlatforms.Length);
            }
        }


        Vector2 scrollPosition = new Vector2(0, 0);


        private const string kEditorBuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
        internal const string kSettingDebuggingWaitForManagedDebugger = "WaitForManagedDebugger";

        static Styles styles = null;

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
            var buildTarget = CalculateSelectedBuildTarget();
            var buildLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);
            BuildPlayerAndRun(!BuildLocationIsValid(buildLocation));
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
            titleContent = new GUIContent("Build Settings");
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

        internal static BuildTarget CalculateSelectedBuildTarget()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return DesktopStandaloneBuildWindowExtension.GetBestStandaloneTarget(EditorUserBuildSettings.selectedStandaloneTarget);
                case BuildTargetGroup.Facebook:
                    return EditorUserBuildSettings.selectedFacebookTarget;
                default:
                    if (BuildPlatforms.instance == null)
                        throw new System.Exception("Build platforms are not initialized.");
                    BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromTargetGroup(targetGroup);
                    if (platform == null)
                        throw new System.Exception("Could not find build platform for target group " + targetGroup);
                    return platform.defaultTarget;
            }
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
                    if (!BuildPipeline.IsBuildTargetCompatibleWithOS(gt.defaultTarget))
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
            BuildTarget selectedTarget = CalculateSelectedBuildTarget();
            BuildTargetGroup selectedTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            GUILayout.BeginHorizontal();

            GUI.enabled = BuildPipeline.IsBuildTargetSupported(selectedTargetGroup, selectedTarget) && EditorUserBuildSettings.activeBuildTargetGroup != selectedTargetGroup;
            if (GUILayout.Button(styles.switchPlatform, GUILayout.Width(Styles.kButtonWidth)))
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(selectedTargetGroup, selectedTarget);
                GUIUtility.ExitGUI();
            }

            GUI.enabled = BuildPipeline.IsBuildTargetSupported(selectedTargetGroup, selectedTarget);
            if (GUILayout.Button(new GUIContent("Player Settings..."), GUILayout.Width(Styles.kButtonWidth)))
            {
                Selection.activeObject = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                EditorWindow.GetWindow<InspectorWindow>();
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
            EditorGUILayout.HelpBox(EditorGUIUtility.TextContent("Unable to access Unity services. Please log in, or request membership to this project to use these services.").text, MessageType.Warning);
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
            bool enabled = EditorUserBuildSettings.selectedBuildTargetGroup == bp.targetGroup;
            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, enabled, false);
                GUI.Label(new Rect(r.x + 3, r.y + 3, 32, 32), title.image, GUIStyle.none);

                if (EditorUserBuildSettings.activeBuildTargetGroup == bp.targetGroup)
                    GUI.Label(new Rect(r.xMax - styles.activePlatformIcon.width - 8, r.y + 3 + (32 - styles.activePlatformIcon.height) / 2,
                            styles.activePlatformIcon.width, styles.activePlatformIcon.height),
                        styles.activePlatformIcon, GUIStyle.none);
            }

            if (GUI.Toggle(r, enabled, title.text, styles.platformSelector))
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
                styles.toggleSize = styles.toggle.CalcSize(new GUIContent("X"));
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

                    if (Provider.enabled && GUILayout.Button("Check out"))
                    {
                        Asset asset = Provider.GetAssetByPath(kEditorBuildSettingsPath);
                        var assetList = new AssetList();
                        assetList.Add(asset);
                        Provider.Checkout(assetList, CheckoutMode.Asset);
                    }
                    GUILayout.Label(message);
                    GUI.enabled = false;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Open Scenes"))
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

        static void RepairSelectedBuildTargetGroup()
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if ((int)group == 0 || BuildPlatforms.instance.BuildPlatformIndexFromTargetGroup(group) < 0)
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
        }

        static bool IsAnyStandaloneModuleLoaded()
        {
            return ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)) ||
                ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSX)) ||
                ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows));
        }

        static bool IsColorSpaceValid(BuildPlatform platform)
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                var hasMinGraphicsAPI = true;
                var hasMinOSVersion = true;

                if (platform.targetGroup == BuildTargetGroup.iOS)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
                    hasMinGraphicsAPI = !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);

                    Version requiredVersion = new Version(8, 0);
                    Version minimumVersion = new Version(6, 0);
                    Version requestedVersion = string.IsNullOrEmpty(PlayerSettings.iOS.targetOSVersionString) ? minimumVersion : new Version(PlayerSettings.iOS.targetOSVersionString);
                    hasMinOSVersion = requestedVersion >= requiredVersion;
                }
                else if (platform.targetGroup == BuildTargetGroup.tvOS)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.tvOS);
                    hasMinGraphicsAPI = !apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }
                else if (platform.targetGroup == BuildTargetGroup.Android)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                    hasMinGraphicsAPI = (apis.Contains(GraphicsDeviceType.Vulkan) || apis.Contains(GraphicsDeviceType.OpenGLES3)) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                    hasMinOSVersion = (int)PlayerSettings.Android.minSdkVersion >= 18;
                }
                else if (platform.targetGroup == BuildTargetGroup.WebGL)
                {
                    var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
                    hasMinGraphicsAPI = apis.Contains(GraphicsDeviceType.OpenGLES3) && !apis.Contains(GraphicsDeviceType.OpenGLES2);
                }

                return hasMinGraphicsAPI && hasMinOSVersion;
            }
            else
            {
                return true;
            }
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
            { "Facebook", "Facebook-Games"}
        };
        static public string GetPlaybackEngineDownloadURL(string moduleName)
        {
            if (moduleName == "PS4" || moduleName == "PSP2" || moduleName == "XboxOne")
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

            return string.Format("http://{0}.unity3d.com/{1}/{2}/{3}/UnitySetup-{4}-Support-for-Editor-{5}{6}", prefix, suffix, revision, folder, moduleName, shortVersion, extension);
        }

        bool IsModuleInstalled(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            bool licensed = BuildPipeline.LicenseCheck(buildTarget);

            string moduleName = Modules.ModuleManager.GetTargetStringFrom(buildTargetGroup, buildTarget);

            return licensed &&
                !string.IsNullOrEmpty(moduleName) &&
                Modules.ModuleManager.GetBuildPostProcessor(moduleName) == null &&
                (BuildTargetGroup.Standalone != EditorUserBuildSettings.selectedBuildTargetGroup ||
                 !IsAnyStandaloneModuleLoaded());
        }

        void ShowBuildTargetSettings()
        {
            EditorGUIUtility.labelWidth = Mathf.Min(180, (position.width - 265) * 0.47f);

            BuildTarget buildTarget = CalculateSelectedBuildTarget();
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

            if (IsModuleInstalled(buildTargetGroup, buildTarget))
            {
                GUILayout.Label("No " + BuildPlatforms.instance.GetModuleDisplayName(buildTargetGroup, buildTarget) + " module loaded.");
                if (GUILayout.Button("Open Download Page", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    string url = GetPlaybackEngineDownloadURL(moduleName);
                    Help.BrowseURL(url);
                }
                GUIBuildButtons(false, false, false, platform);
                return;
            }
            else if (Application.HasProLicense() && !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(buildTarget))
            {
                // Show copy for using personal edition build targets with pro edition editor
                string infoText = string.Format("{0} is not included in your Unity Pro license. " +
                        "Your {0} build will include a Unity Personal Edition splash screen." +
                        "\n\nYou must be eligible to use Unity Personal Edition to use this build option. " +
                        "Please refer to our EULA for further information.",
                        BuildPlatforms.instance.GetBuildTargetDisplayName(buildTargetGroup, buildTarget));

                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(infoText, EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("EULA", EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/legal/eula");
                if (GUILayout.Button(string.Format("Add {0} to your Unity Pro license", BuildPlatforms.instance.GetBuildTargetDisplayName(buildTargetGroup, buildTarget)), EditorStyles.miniButton))
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
                int targetGroup = BuildPlatforms.instance.BuildPlatformIndexFromTargetGroup(platform.targetGroup);

                GUILayout.Label(styles.notLicensedMessages[targetGroup, 0], EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (styles.notLicensedMessages[targetGroup, 1].text.Length != 0)
                {
                    if (GUILayout.Button(styles.notLicensedMessages[targetGroup, 1]))
                    {
                        Application.OpenURL(styles.notLicensedMessages[targetGroup, 2].text);
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
            switch (platform.targetGroup)
            {
                case BuildTargetGroup.iOS:
                case BuildTargetGroup.tvOS:
                {
                    if (Application.platform == RuntimePlatform.OSXEditor)
                        EditorUserBuildSettings.symlinkLibraries = EditorGUILayout.Toggle(styles.symlinkiOSLibraries, EditorUserBuildSettings.symlinkLibraries);
                }
                break;
            }

            GUI.enabled = true;

            bool enableBuildButton = buildWindowExtension != null ? buildWindowExtension.EnabledBuildButton() : true;
            bool enableBuildAndRunButton = false;

            bool shouldDrawDebuggingToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawScriptDebuggingCheckbox() : true;
            bool shouldDrawExplicitNullChecksToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawExplicitNullCheckbox() : false;
            bool shouldDrawDivideByZeroChecksToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawExplicitDivideByZeroCheckbox() : false;
            bool shouldDrawDevelopmentPlayerToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawDevelopmentPlayerCheckbox() : true;
            bool enableHeadlessModeToggle = (buildTarget == BuildTarget.StandaloneLinux || buildTarget == BuildTarget.StandaloneLinux64 || buildTarget == BuildTarget.StandaloneLinuxUniversal);

            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(buildTargetGroup, buildTarget);
            bool enableBuildScriptsOnly = (postprocessor != null ? postprocessor.SupportsScriptsOnlyBuild() : false);
            bool canInstallInBuildFolder = false;

            if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
            {
                bool shouldDrawConnectProfilerToggle = buildWindowExtension != null ? buildWindowExtension.ShouldDrawProfilerCheckbox() : true;

                GUI.enabled = shouldDrawDevelopmentPlayerToggle;
                if (shouldDrawDevelopmentPlayerToggle)
                    EditorUserBuildSettings.development = EditorGUILayout.Toggle(styles.debugBuild, EditorUserBuildSettings.development);

                bool developmentBuild = EditorUserBuildSettings.development;

                GUI.enabled = developmentBuild;

                if (shouldDrawConnectProfilerToggle)
                {
                    if (!GUI.enabled)
                    {
                        if (!developmentBuild)
                            styles.profileBuild.tooltip = "Profiling only enabled in Development Player";
                    }
                    else
                        styles.profileBuild.tooltip = "";
                    EditorUserBuildSettings.connectProfiler = EditorGUILayout.Toggle(styles.profileBuild, EditorUserBuildSettings.connectProfiler);
                }

                GUI.enabled = developmentBuild;
                if (shouldDrawDebuggingToggle)
                {
                    EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle(styles.allowDebugging, EditorUserBuildSettings.allowDebugging);

                    // Not all platforms have native dialog implemented in Runtime\Misc\GiveDebuggerChanceToAttachIfRequired.cpp
                    // Display this option only for developer builds
                    if (EditorUserBuildSettings.allowDebugging && Unsupported.IsDeveloperBuild())
                    {
                        var buildTargetName = BuildPipeline.GetBuildTargetName(buildTarget);

                        bool value = EditorGUILayout.Toggle(styles.waitForManagedDebugger, EditorUserBuildSettings.GetPlatformSettings(buildTargetName, kSettingDebuggingWaitForManagedDebugger) == "true");
                        EditorUserBuildSettings.SetPlatformSettings(buildTargetName, kSettingDebuggingWaitForManagedDebugger, value.ToString().ToLower());
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

                if (enableBuildScriptsOnly)
                    EditorUserBuildSettings.buildScriptsOnly = EditorGUILayout.Toggle(styles.buildScriptsOnly, EditorUserBuildSettings.buildScriptsOnly);

                GUI.enabled = !developmentBuild;
                if (enableHeadlessModeToggle)
                    EditorUserBuildSettings.enableHeadlessMode = EditorGUILayout.Toggle(styles.enableHeadlessMode, EditorUserBuildSettings.enableHeadlessMode && !developmentBuild);

                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                if (postprocessor != null && postprocessor.SupportsLz4Compression())
                {
                    var cmpIdx = Array.IndexOf(styles.compressionTypes, EditorUserBuildSettings.GetCompressionType(buildTargetGroup));
                    if (cmpIdx == -1)
                        cmpIdx = 1; // Lz4 by default.
                    cmpIdx = EditorGUILayout.Popup(styles.compressionMethod, cmpIdx, styles.compressionStrings);
                    EditorUserBuildSettings.SetCompressionType(buildTargetGroup, styles.compressionTypes[cmpIdx]);
                }

                canInstallInBuildFolder = Unsupported.IsDeveloperBuild() && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTargetGroup, buildTarget);

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

                int targetGroup = BuildPlatforms.instance.BuildPlatformIndexFromTargetGroup(platform.targetGroup);

                GUILayout.Label(styles.GetTargetNotInstalled(targetGroup, 0));
                if (styles.GetTargetNotInstalled(targetGroup, 1) != null)
                    if (GUILayout.Button(styles.GetTargetNotInstalled(targetGroup, 1)))
                        Application.OpenURL(styles.GetTargetNotInstalled(targetGroup, 2).text);

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (buildTarget == BuildTarget.Android)
                AndroidPublishGUI();

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
                EditorUserBuildSettings.installInBuildFolder = GUILayout.Toggle(EditorUserBuildSettings.installInBuildFolder, "Install in Builds folder\n(for debugging with source code)", GUILayout.ExpandWidth(false));
            else
                EditorUserBuildSettings.installInBuildFolder = false;

            if ((buildWindowExtension != null) && Unsupported.IsDeveloperBuild())
                buildWindowExtension.ShowInternalPlatformBuildOptions();


            // Disable the 'Build' and 'Build And Run' buttons when the project setup doesn't satisfy the platform requirements
            if (!IsColorSpaceValid(platform) && enableBuildButton && enableBuildAndRunButton)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(Styles.invalidColorSpaceMessage.text, MessageType.Warning);
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.LinkLabel(styles.learnAboutUnityCloudBuild))
            {
                Application.OpenURL(string.Format("{0}/from/editor/buildsettings?upid={1}&pid={2}&currentplatform={3}&selectedplatform={4}&unityversion={5}",
                        UnityEditorInternal.WebURLs.cloudBuildPage, PlayerSettings.cloudProjectId, PlayerSettings.productGUID, EditorUserBuildSettings.activeBuildTarget, CalculateSelectedBuildTarget(), Application.unityVersion));
            }
            GUILayout.EndHorizontal();
            // Space 6 for alignment with platform column and to reduce missclicks with Build And Run button
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIContent buildButton = styles.build;
            if (platform.targetGroup == BuildTargetGroup.Android
                && EditorUserBuildSettings.exportAsGoogleAndroidProject
                && EditorUserBuildSettings.androidBuildSystem != AndroidBuildSystem.Internal)
                buildButton = styles.export;

            // Build Button
            GUI.enabled = enableBuildButton;
            if (GUILayout.Button(buildButton, GUILayout.Width(Styles.kButtonWidth)))
            {
                CallBuildMethods(true, BuildOptions.ShowBuiltPlayer);
                GUIUtility.ExitGUI();
            }
            // Build and Run button
            GUI.enabled = enableBuildAndRunButton;
            if (GUILayout.Button(styles.buildAndRun, GUILayout.Width(Styles.kButtonWidth)))
            {
                BuildPlayerAndRun(true);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }
    }
}
