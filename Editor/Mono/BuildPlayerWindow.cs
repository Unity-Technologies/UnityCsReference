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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Modules;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using Object = UnityEngine.Object;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.Connect;
using UnityEditor.Utils;
using UnityEditor.Build.Profile;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        class Styles
        {
            public GUIContent invalidColorSpaceMessage = EditorGUIUtility.TrTextContent("In order to build a player, go to 'Player Settings...' to resolve the incompatibility between the Color Space and the current settings.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIContent invalidLightmapEncodingMessage = EditorGUIUtility.TrTextContent("In order to build a player, go to 'Player Settings...' to resolve the incompatibility between the Lightmap Encoding value you have selected and the current settings.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
            public GUIContent invalidHDRCubemapEncodingMessage = EditorGUIUtility.TrTextContent("In order to build a player, go to 'Player Settings...' to resolve the incompatibility between the HDR Cubemap Encoding value you have selected and the current settings.", EditorGUIUtility.GetHelpIcon(MessageType.Warning));
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
            public string EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
            public string infoText = L10n.Tr("{0} is not included in your Unity Pro license. Your {0} build will include a Unity Personal Edition splash screen.\n\nYou must be eligible to use Unity Personal Edition to use this build option. Please refer to our EULA for further information.");
            public GUIContent eula = EditorGUIUtility.TrTextContent("Eula");
            public string addToYourPro = L10n.Tr("Add {0} to your Unity Pro license");
            public GUIContent installInBuildFolder = EditorGUIUtility.TrTextContent("Install into source code 'build' folder", "Install into source checkout 'build' folder, for debugging with source code");
            public GUIContent installInBuildFolderHelp = EditorGUIUtility.TrIconContent("_Help", "Open documentation about source code building and debugging");

            public Texture2D activePlatformIcon = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image as Texture2D;

            public const float kButtonWidth = 110;

            public string shopURL = "https://store.unity.com/products/unity-pro";

            public GUIContent GetDownloadErrorForTarget(BuildTarget target)
            {
                return null;
            }

            // string and matching enum values for standalone subtarget dropdowm
            public GUIContent debugBuild = EditorGUIUtility.TrTextContent("Development Build");
            public GUIContent autoconnectProfiler = EditorGUIUtility.TrTextContent("Autoconnect Profiler", "When the build is started, an open Profiler Window will automatically connect to the Player and start profiling. The \"Build And Run\" option will also automatically open the Profiler Window.");
            public GUIContent autoconnectProfilerDisabled = EditorGUIUtility.TrTextContent("Autoconnect Profiler", "Profiling is only enabled in a Development Player.");
            public GUIContent buildWithDeepProfiler = EditorGUIUtility.TrTextContent("Deep Profiling Support", "Build Player with Deep Profiling Support. This might affect Player performance.");
            public GUIContent buildWithDeepProfilerDisabled = EditorGUIUtility.TrTextContent("Deep Profiling Support", "Profiling is only enabled in a Development Player.");
            public GUIContent allowDebugging = EditorGUIUtility.TrTextContent("Script Debugging", "Enable this setting to allow your script code to be debugged.");
            public GUIContent waitForManagedDebugger = EditorGUIUtility.TrTextContent("Wait For Managed Debugger", "Show a dialog where you can attach a managed debugger before any script execution. Can also use volume Up or Down button to confirm on Android.");
            public GUIContent managedDebuggerFixedPort = EditorGUIUtility.TrTextContent("Managed Debugger Fixed Port", "Use the specified port to attach to the managed debugger. If 0, the port will be automatically selected.");
            public GUIContent explicitNullChecks = EditorGUIUtility.TrTextContent("Explicit Null Checks");
            public GUIContent explicitDivideByZeroChecks = EditorGUIUtility.TrTextContent("Divide By Zero Checks");
            public GUIContent explicitArrayBoundsChecks = EditorGUIUtility.TrTextContent("Array Bounds Checks");
            public GUIContent learnAboutUnityCloudBuild = EditorGUIUtility.TrTextContent("Learn about Unity Build Automation");
            public GUIContent compressionMethod = EditorGUIUtility.TrTextContent("Compression Method", "Compression applied to Player data (scenes and resources).\nDefault - none or default platform compression.\nLZ4 - fast compression suitable for Development Builds.\nLZ4HC - higher compression rate variance of LZ4, causes longer build times. Works best for Release Builds.");

            public readonly GUIContent assetImportOverrides = EditorGUIUtility.TrTextContent("Asset Import Overrides", "Asset import overrides for local development. Reducing maximum texture size or compression settings can speed up asset imports and platform switches.");
            public readonly GUIContent maxTextureSize = EditorGUIUtility.TrTextContent("Max Texture Size", "Maximum texture import size for local development. Reducing maximum texture size can speed up asset imports and platform switches.");
            public readonly GUIContent[] maxTextureSizeLabels =
            {
                EditorGUIUtility.TrTextContent("No Override", "Use maximum texture size as specified in per-texture import settings."),
                EditorGUIUtility.TrTextContent("Max 2048", "Make imported textures never exceed 2048 pixels in width or height."),
                EditorGUIUtility.TrTextContent("Max 1024", "Make imported textures never exceed 1024 pixels in width or height."),
                EditorGUIUtility.TrTextContent("Max 512", "Make imported textures never exceed 512 pixels in width or height."),
                EditorGUIUtility.TrTextContent("Max 256", "Make imported textures never exceed 256 pixels in width or height."),
                EditorGUIUtility.TrTextContent("Max 128", "Make imported textures never exceed 128 pixels in width or height."),
                EditorGUIUtility.TrTextContent("Max 64", "Make imported textures never exceed 64 pixels in width or height."),
            };
            public readonly int[] maxTextureSizeValues =
            {
                0,
                2048,
                1024,
                512,
                256,
                128,
                64,
            };
            public readonly GUIContent[] textureCompressionLabels =
            {
                EditorGUIUtility.TrTextContent("No Override", "Do not modify texture import compression settings."),
                EditorGUIUtility.TrTextContent("Force Fast Compressor", "Use a faster but lower quality texture compression mode for all compressed textures. Turn off Crunch compression."),
                EditorGUIUtility.TrTextContent("Force Uncompressed", "Do not compress textures."),
                EditorGUIUtility.TrTextContent("Force No Crunch", "Disable crunch compression on textures.")
            };
            public readonly int[] textureCompressionValues =
            {
                (int)OverrideTextureCompression.NoOverride,
                (int)OverrideTextureCompression.ForceFastCompressor,
                (int)OverrideTextureCompression.ForceUncompressed,
                (int)OverrideTextureCompression.ForceNoCrunchCompression
            };

            public readonly GUIContent textureCompression = EditorGUIUtility.TrTextContent("Texture Compression", "Texture compression override for local development. Fast or Uncompressed can speed up asset imports and platform switches.");
            public readonly GUIContent applyOverrides = EditorGUIUtility.TrTextContent("Apply Overrides", "Apply asset import override settings");

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

            public static GUIStyle boldFoldout;

            static Styles()
            {
                boldFoldout = new GUIStyle(EditorStyles.foldout) {fontStyle = FontStyle.Bold};
            }
        }


        Vector2 scrollPosition = new Vector2(0, 0);
        Vector2 buildTargetSettingsScrollPosition = new Vector2(0, 0);
        const string kEditorBuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

        static Styles styles;

        static bool isEditorinstalledWithHub = IsEditorInstalledWithHub();

        internal static event Action<BuildProfile> drawingMultiplayerBuildOptions;

        [UsedImplicitly, RequiredByNativeCode]
        public static void ShowBuildPlayerWindow()
        {
            EditorUserBuildSettings.selectedBuildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            GetWindow<BuildPlayerWindow>(false, "Build Settings");
        }

        internal static bool WillDrawMultiplayerBuildOptions() => drawingMultiplayerBuildOptions != null;

        internal static void DrawMultiplayerBuildOption(BuildProfile buildProfile)
        {
            drawingMultiplayerBuildOptions?.Invoke(buildProfile);
        }

        static bool BuildLocationIsValid(string path)
        {
            return path.Length > 0 && Directory.Exists(FileUtil.DeleteLastPathNameComponent(path));
        }

        [RequiredByNativeCode]
        static bool BuildPlayerAndRunEnabled()
        {
            var buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            NamedBuildTarget namedBuildTarget = EditorUserBuildSettingsUtils.CalculateSelectedNamedBuildTarget();
            BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget);
            string module = ModuleManager.GetTargetStringFrom(buildTarget);
            IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(module);

            bool buildPlayerAndRunEnabled = buildWindowExtension != null ? buildWindowExtension.EnabledBuildAndRunButton() && !(EditorUserBuildSettings.installInBuildFolder) : !(EditorUserBuildSettings.installInBuildFolder);
            return buildPlayerAndRunEnabled;
        }

        // This overload is used by the Build & Run menu item & hot key - prompt for location only if the configured
        // output location is not valid.
        [UsedImplicitly, RequiredByNativeCode]
        static void BuildPlayerAndRun()
        {
            var buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            var lastBuildLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);
            bool buildLocationIsValid = BuildLocationIsValid(lastBuildLocation);

            if (buildLocationIsValid && BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                string buildLocation = iBuildTarget.BuildPlatformProperties?.ValidateBuildLocation();
                if (buildLocation != null)
                    EditorUserBuildSettings.SetBuildLocation(buildTarget, buildLocation);
            }

            BuildPlayerAndRunInternal(!buildLocationIsValid);
        }

        // This overload is used by the default player window, to always prompt for build location
        static void BuildPlayerAndRunInternal(bool askForBuildLocation)
        {
            CallBuildMethods(askForBuildLocation, BuildOptions.AutoRunPlayer | BuildOptions.StrictMode);
        }

        public BuildPlayerWindow()
        {
            s_CurrOverrideMaxTextureSize = -1;
            minSize = new Vector2(640, 580);
            position = new Rect(50, 50, minSize.x, minSize.y);
            titleContent = EditorGUIUtility.TrTextContent("Build Settings");
        }

        BuildPlayerSceneTreeView m_TreeView;
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

            Rect rect = GUILayoutUtility.GetRect(0, position.width, 0, position.height, GUILayout.MinHeight(20));
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

                if (EditorSceneManager.IsAuthoringScene(scene))
                    continue;

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

        static int s_CurrOverrideMaxTextureSize = -1;
        static OverrideTextureCompression s_CurrOverrideTextureCompression;

        static bool hasAssetImportOverrideChanges =>
            s_CurrOverrideMaxTextureSize != EditorUserBuildSettings.overrideMaxTextureSize ||
            s_CurrOverrideTextureCompression != EditorUserBuildSettings.overrideTextureCompression;

        static void ApplyAssetImportOverridesToSettingsAsset()
        {
            EditorUserBuildSettings.overrideMaxTextureSize = s_CurrOverrideMaxTextureSize;
            EditorUserBuildSettings.overrideTextureCompression = s_CurrOverrideTextureCompression;
        }

        static void DrawOverrideLine()
        {
            var rect = EditorGUILayout.s_LastRect;
            var prevMargin = EditorGUIUtility.leftMarginCoord;
            EditorGUIUtility.leftMarginCoord = 2;
            EditorGUI.DrawOverrideBackgroundApplicable(rect);
            EditorGUIUtility.leftMarginCoord = prevMargin;
        }

        SavedBool m_ShowOverrides;

        void AssetImportOverridesGui()
        {
            if (s_CurrOverrideMaxTextureSize < 0)
            {
                // fetch initial values
                s_CurrOverrideMaxTextureSize = EditorUserBuildSettings.overrideMaxTextureSize;
                s_CurrOverrideTextureCompression = EditorUserBuildSettings.overrideTextureCompression;
            }

            if (m_ShowOverrides == null)
                m_ShowOverrides = new SavedBool("BuildSettingsWindow.ShowImportOverrides", true);

            GUILayout.Space(5);
            m_ShowOverrides.value = EditorGUILayout.Foldout(m_ShowOverrides.value, styles.assetImportOverrides, true, Styles.boldFoldout);
            if (m_ShowOverrides.value)
            {
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 125;
                s_CurrOverrideMaxTextureSize = EditorGUILayout.IntPopup(
                    styles.maxTextureSize,
                    s_CurrOverrideMaxTextureSize,
                    styles.maxTextureSizeLabels,
                    styles.maxTextureSizeValues);
                if (s_CurrOverrideMaxTextureSize != 0)
                    DrawOverrideLine();

                s_CurrOverrideTextureCompression = (OverrideTextureCompression)EditorGUILayout.IntPopup(
                    styles.textureCompression,
                    (int)s_CurrOverrideTextureCompression,
                    styles.textureCompressionLabels,
                    styles.textureCompressionValues);
                if (s_CurrOverrideTextureCompression != OverrideTextureCompression.NoOverride)
                    DrawOverrideLine();
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
            else
            {
                // when the settings are folded, but there are some overrides, then display the blue override
                // indicator on the side of the foldout itself
                if (s_CurrOverrideMaxTextureSize != 0 || s_CurrOverrideTextureCompression != OverrideTextureCompression.NoOverride)
                    DrawOverrideLine();
            }
        }

        void ActiveBuildTargetsGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginVertical(GUILayout.Width(255));
            GUILayout.Label(styles.platformTitle, styles.title);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "OL Box");

            // Draw enabled build targets first, then draw disabled build targets
            bool even = false;
            for (int requireEnabled = 0; requireEnabled < 2; requireEnabled++)
            {
                bool showRequired = requireEnabled == 0;
                foreach (BuildPlatform gt in BuildPlatforms.instance.buildPlatforms)
                {
                    // All installed build targets will be shown on the first pass (showRequired = true)
                    if (gt.installed != showRequired)
                        continue;

                    // Some build targets are not publicly available, show them only when they are installed
                    if (!gt.installed && gt.hideInUi)
                        continue;

                    // Some build targets are only compatible with specific OS
                    if (BuildTargetDiscovery.TryGetBuildTarget(gt.defaultTarget, out IBuildTarget iBuildTarget) && !(iBuildTarget.BuildPlatformProperties?.CanBuildOnCurrentHostPlatform ?? true))
                        continue;

                    GUI.contentColor = gt.installed ? Color.white : new Color(1, 1, 1, 0.5f);
                    ShowOption(gt, gt.title, even ? styles.evenRow : styles.oddRow);
                    even = !even;
                }
                GUI.contentColor = Color.white;
            }

            GUILayout.EndScrollView();

            AssetImportOverridesGui();
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            // Switch build target
            BuildTarget selectedTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            GUI.enabled = BuildPipeline.IsBuildPlatformSupported(selectedTarget);
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Player Settings..."), GUILayout.Width(Styles.kButtonWidth)))
            {
                SettingsService.OpenProjectSettings("Project/Player");
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            // Apply import overrides
            if (hasAssetImportOverrideChanges)
            {
                if (GUILayout.Button(styles.applyOverrides, GUILayout.Width(Styles.kButtonWidth)))
                {
                    ApplyAssetImportOverridesToSettingsAsset();
                    AssetDatabase.Refresh();
                }
            }

            GUILayout.EndHorizontal();

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
            bool selected = bp.IsSelected();
            bool active = bp.IsActive();

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

                if (active)
                    GUI.Label(new Rect(r.xMax - styles.activePlatformIcon.width - 8, r.y + 3 + (32 - styles.activePlatformIcon.height) / 2,
                        styles.activePlatformIcon.width, styles.activePlatformIcon.height),
                        styles.activePlatformIcon, GUIStyle.none);
            }

            if (GUI.Toggle(r, selected, title.text, styles.platformSelector))
            {
                if (!selected)
                {
                    bp.Select();

                    // Repaint inspectors, as they may be showing platform target specific things.
                    Object[] inspectors = Resources.FindObjectsOfTypeAll(typeof(InspectorWindow));
                    for (int i = 0; i < inspectors.Length; i++)
                    {
                        InspectorWindow inspector = inspectors[i] as InspectorWindow;
                        if (inspector != null)
                            inspector.Repaint();
                    }

                    // We also need to repaint project settings window.
                    Object[] projecSettingsWindows = Resources.FindObjectsOfTypeAll(typeof(ProjectSettingsWindow));
                    for (int i = 0; i < projecSettingsWindows.Length; i++)
                    {
                        ProjectSettingsWindow projecSettingsWindow = projecSettingsWindows[i] as ProjectSettingsWindow;
                        if (projecSettingsWindow != null)
                            projecSettingsWindow.Repaint();
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

            GUILayout.BeginHorizontal(GUILayout.Height(400));
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

        static bool IsAnyStandaloneModuleLoaded()
        {
            return ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneLinux64) ||
                ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneOSX) ||
                ModuleManager.IsPlatformSupportLoadedByBuildTarget(BuildTarget.StandaloneWindows);
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
            { "OSXStandalone", "Mac-Mono" },
            { "OSXDedicatedServer", "Mac-Server" },
            { "WindowsStandalone", "Windows-Mono" },
            { "WindowsDedicatedServer", "Windows-Server" },
            { "LinuxStandalone", "Linux-Mono" },
            { "LinuxDedicatedServer", "Linux-Server" },
            { "UWP", "Universal-Windows-Platform"}
        };
        static public string GetPlaybackEngineDownloadURL(string moduleName)
        {
#pragma warning disable CS0618 // Member is obsolete
            if (!BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(BuildTargetDiscovery.GetBuildTargetByName(moduleName)))
#pragma warning restore CS0618
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
                if (moduleName == "Android" || moduleName == "Mac-Mono" || moduleName == "Windows-Mono")
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

        internal static string GetUnityHubModuleDownloadURL(string moduleName)
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

        internal static bool IsModuleNotInstalled(NamedBuildTarget namedBuildTarget, BuildTarget buildTarget)
        {
            bool licensed = BuildPipeline.LicenseCheck(buildTarget);
            bool installed = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget).installed;

            string moduleName = ModuleManager.GetTargetStringFrom(buildTarget);

            return !installed ||
                (licensed && !string.IsNullOrEmpty(moduleName) &&
                ModuleManager.GetBuildPostProcessor(moduleName) == null &&
                (BuildTargetGroup.Standalone != EditorUserBuildSettings.selectedBuildTargetGroup ||
                    !IsAnyStandaloneModuleLoaded()));
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

            return File.Exists(path);
        }

        void ShowBuildTargetSettings()
        {
            EditorGUIUtility.labelWidth = Mathf.Min(180, (position.width - 265) * 0.47f);

            BuildTarget buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            NamedBuildTarget namedBuildTarget = EditorUserBuildSettingsUtils.CalculateSelectedNamedBuildTarget();
            BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget);
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(buildTarget);
            bool licensed = BuildPipeline.LicenseCheck(buildTarget);

            // Draw the group name (text & icon separately to have some space between them)
            var titleIconSize = 16;
            Rect r = GUILayoutUtility.GetRect(50, titleIconSize);
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(r.x, r.y, titleIconSize, titleIconSize), platform.smallIcon);
            r.x += titleIconSize + 5;
            GUI.Label(r, platform.title.text, styles.title);

            GUILayout.Space(10);

            string moduleName = ModuleManager.GetTargetStringFrom(buildTarget);
            if (namedBuildTarget == NamedBuildTarget.Server)
                moduleName = moduleName.Replace("Standalone", "DedicatedServer");

            if (IsModuleNotInstalled(namedBuildTarget, buildTarget))
            {
                ShowNoModuleLabel(namedBuildTarget, buildTarget, moduleName,
                    styles.noModuleLoaded,
                    styles.openDownloadPage,
                    styles.installModuleWithHub,
                    styles.EditorWillNeedToBeReloaded);
                GUIBuildButtons(false, false, false, platform, postprocessor);
                return;
            }
            else if (Application.HasProLicense() && !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(buildTarget))
            {
                // Show copy for using personal edition build targets with pro edition editor
                string infoText = string.Format(styles.infoText,
                    BuildPlatforms.instance.GetBuildTargetDisplayName(namedBuildTarget, buildTarget));

                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(infoText, EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(styles.eula, EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/legal/eula");
                if (GUILayout.Button(string.Format(styles.addToYourPro, BuildPlatforms.instance.GetBuildTargetDisplayName(namedBuildTarget, buildTarget)), EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/get-unity");
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUIContent error = styles.GetDownloadErrorForTarget(buildTarget);
            if (error != null)
            {
                GUILayout.Label(error, EditorStyles.wordWrappedLabel);
                GUIBuildButtons(false, false, false, platform, postprocessor);
                return;
            }

            // Draw not licensed buy now UI
            if (!licensed)
            {
                string niceName = BuildPipeline.GetBuildTargetGroupDisplayName(namedBuildTarget.ToBuildTargetGroup());
                string licenseMsg = "Your license does not cover {0} Publishing.";
                string buttonMsg = "Go to Our Online Store";
                string licenseURL = styles.shopURL;
                if (BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsConsole))
                {
                    licenseMsg += " Please see the {0} section of the Platform Module Installation documentation for more details.";
                    buttonMsg = "Platform Module Installation";
                    licenseURL = "https://unity3d.com/platform-installation";
                }
                else if (BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsStandalonePlatform))
                    buttonMsg = "";

                GUIContent[] notLicensedMessage =
                {
                    EditorGUIUtility.TextContent(string.Format(L10n.Tr(licenseMsg), niceName)),
                    EditorGUIUtility.TextContent(L10n.Tr(buttonMsg)),
                    new GUIContent(licenseURL)
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
                GUIBuildButtons(false, false, false, platform, postprocessor);
                return;
            }

            buildTargetSettingsScrollPosition = GUILayout.BeginScrollView(buildTargetSettingsScrollPosition);

            // FIXME: WHY IS THIS ALL IN ONE FUNCTION?!
            // Draw the side bar to the right. Different options like specific Standalone player to build, profiling and debugging options, etc.
            string module = ModuleManager.GetTargetStringFrom(buildTarget);
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

            bool canInstallInBuildFolder = false;

            if (BuildPipeline.IsBuildPlatformSupported(buildTarget))
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

                        bool shouldDrawManagedDebuggerFixedPort = buildWindowExtension != null ? buildWindowExtension.ShouldDrawManagedDebuggerFixedPort() : false;
                        if (EditorUserBuildSettings.allowDebugging && shouldDrawManagedDebuggerFixedPort)
                        {
                            EditorUserBuildSettings.managedDebuggerFixedPort = EditorGUILayout.IntField(styles.managedDebuggerFixedPort, EditorUserBuildSettings.managedDebuggerFixedPort);
                        }
                    }

                    if (EditorUserBuildSettings.allowDebugging && PlayerSettings.GetScriptingBackend(namedBuildTarget) == ScriptingImplementation.IL2CPP)
                    {
                        var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget);
                        bool isDebuggerUsable = apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0 ||
                            apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard;

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

                GUI.enabled = true;

                if (postprocessor != null && postprocessor.SupportsLz4Compression())
                {
                    var cmpIdx = Array.IndexOf(styles.compressionTypes, EditorUserBuildSettings.GetCompressionType(namedBuildTarget.ToBuildTargetGroup()));
                    if (cmpIdx == -1)
                        cmpIdx = Array.IndexOf(styles.compressionTypes, postprocessor.GetDefaultCompression());
                    if (cmpIdx == -1)
                        cmpIdx = 1; // Lz4 by default.
                    cmpIdx = EditorGUILayout.Popup(styles.compressionMethod, cmpIdx, styles.compressionStrings);
                    EditorUserBuildSettings.SetCompressionType(namedBuildTarget.ToBuildTargetGroup(), styles.compressionTypes[cmpIdx]);
                }

                canInstallInBuildFolder = Unsupported.IsSourceBuild() && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTarget);

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

                GUILayout.Label(string.Format(L10n.Tr("{0} is not supported in this build.\nDownload a build that supports it."), BuildPipeline.GetBuildTargetGroupDisplayName(namedBuildTarget.ToBuildTargetGroup())));

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            var subtarget = StandaloneBuildSubtarget.Default;
            if (namedBuildTarget == NamedBuildTarget.Standalone)
                subtarget = StandaloneBuildSubtarget.Player;
            else if (namedBuildTarget == NamedBuildTarget.Server)
                subtarget = StandaloneBuildSubtarget.Server;

            drawingMultiplayerBuildOptions?.Invoke(BuildProfileContext.instance.GetForClassicPlatform(buildTarget, subtarget));

            GUILayout.EndScrollView();

            GUIBuildButtons(buildWindowExtension, enableBuildButton, enableBuildAndRunButton,
                canInstallInBuildFolder, platform, postprocessor);
        }

        internal static void ShowNoModuleLabel(
            NamedBuildTarget namedBuildTarget,
            BuildTarget buildTarget,
            string moduleName,
            string noModuleLoaded,
            GUIContent openDownloadPage,
            GUIContent installModuleWithHub,
            string editorWillNeedToBeReloaded)
        {
            GUILayout.Label(EditorGUIUtility.TextContent(string.Format(noModuleLoaded, BuildPlatforms.instance.GetModuleDisplayName(namedBuildTarget, buildTarget))));
            string url = "";
#pragma warning disable CS0618 // Member is obsolete
            if (!isEditorinstalledWithHub || !BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(buildTarget))
#pragma warning restore CS0618
            {
                if (GUILayout.Button(openDownloadPage, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    url = GetPlaybackEngineDownloadURL(moduleName);
                    Help.BrowseURL(url);
                }
            }
            else
            {
                if (GUILayout.Button(installModuleWithHub, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    url = GetUnityHubModuleDownloadURL(moduleName);
                    Help.BrowseURL(url);
                }
            }

            GUILayout.Label(editorWillNeedToBeReloaded, EditorStyles.wordWrappedMiniLabel);
        }

        private static void GUIBuildButtons(bool enableBuildButton,
            bool enableBuildAndRunButton,
            bool canInstallInBuildFolder,
            BuildPlatform platform,
            IBuildPostprocessor postprocessor)
        {
            GUIBuildButtons(null, enableBuildButton, enableBuildAndRunButton, canInstallInBuildFolder, platform, postprocessor);
        }

        private static void GUIBuildButtons(IBuildWindowExtension buildWindowExtension,
            bool enableBuildButton,
            bool enableBuildAndRunButton,
            bool canInstallInBuildFolder,
            BuildPlatform platform,
            IBuildPostprocessor postprocessor)
        {
            GUILayout.FlexibleSpace();


            if (canInstallInBuildFolder)
            {
                GUILayout.BeginHorizontal();
                EditorUserBuildSettings.installInBuildFolder = GUILayout.Toggle(EditorUserBuildSettings.installInBuildFolder, styles.installInBuildFolder, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(styles.installInBuildFolderHelp, EditorStyles.iconButton))
                {
                    var path = Path.Combine(Unsupported.GetBaseUnityDeveloperFolder(), "Documentation/BuildDocs/view");
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
            if (enableBuildButton && enableBuildAndRunButton)
            {
                if (!IsVirtualTexturingSettingsValid(platform))
                {
                    enableBuildAndRunButton = false;
                    enableBuildButton = false;
                    EditorGUILayout.HelpBox(styles.invalidVirtualTexturingSettingMessage);
                }
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                enableBuildAndRunButton = false;
                enableBuildButton = false;
                EditorGUILayout.HelpBox(styles.compilingMessage);
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.LinkButton(styles.learnAboutUnityCloudBuild))
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

            // Run last build button(s)
            if (buildWindowExtension != null && buildWindowExtension.ShouldDrawRunLastBuildButton())
            {
                buildWindowExtension.DoRunLastBuildButtonGui();
            }

            // Switching build target in the editor
            BuildTarget selectedTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget(platform.namedBuildTarget);

            bool selectedTargetIsActive = platform.IsActive();

            if (selectedTargetIsActive)
            {
                // Build Button
                GUI.enabled = enableBuildButton;

                {
                    Rect buildRect = GUILayoutUtility.GetRect(buildButton, EditorStyles.dropDownToggleButton,
                        GUILayout.Width(Styles.kButtonWidth));
                    Rect buildRectPopupButton = buildRect;
                    buildRectPopupButton.x += buildRect.width - 16;
                    buildRectPopupButton.width = 16;

                    if (EditorGUI.DropdownButton(buildRectPopupButton, GUIContent.none, FocusType.Passive,
                        GUIStyle.none))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Clean Build…"), false,
                            () =>
                            {
                                CallBuildMethods(askForBuildLocation,
                                    BuildOptions.ShowBuiltPlayer | BuildOptions.CleanBuildCache);
                            });
                        menu.AddItem(new GUIContent("Force skip data build"), false,
                            () =>
                            {
                                CallBuildMethods(askForBuildLocation,
                                    BuildOptions.ShowBuiltPlayer | BuildOptions.BuildScriptsOnly);
                            });
                        menu.DropDown(buildRect);
                    }
                    else if (GUI.Button(buildRect, buildButton, EditorStyles.dropDownToggleButton))
                    {
                        CallBuildMethods(askForBuildLocation, BuildOptions.ShowBuiltPlayer);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                GUI.enabled = BuildPipeline.IsBuildPlatformSupported(selectedTarget);
                if (GUILayout.Button(styles.switchPlatform, GUILayout.Width(Styles.kButtonWidth)))
                {
                    ApplyAssetImportOverridesToSettingsAsset();
                    platform.SetActive();
                    GUIUtility.ExitGUI();
                }
            }

            // Build and Run button
            GUI.enabled = enableBuildAndRunButton && selectedTargetIsActive;
            if (GUILayout.Button(buildAndRunButton, GUILayout.Width(Styles.kButtonWidth)))
            {
                BuildPlayerAndRunInternal(askForBuildLocation);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }
    }
}
