// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
    internal class BuildPlayerWindow : EditorWindow
    {
        public class SceneSorter : IComparer
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer.Compare(System.Object x, System.Object y)
            {
                return ((new CaseInsensitiveComparer()).Compare(y, x));
            }
        }
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
                { EditorGUIUtility.TextContent("Your license does not cover Windows Store Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover Windows Phone 8 Publishing."), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
                { EditorGUIUtility.TextContent("Your license does not cover SamsungTV Publishing"), EditorGUIUtility.TextContent("Go to Our Online Store"), new GUIContent(kShopURL) },
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
                { EditorGUIUtility.TextContent("Windows Store Player is not supported in\nthis build.\n\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("Windows Phone 8 Player is not supported\nin this build.\n\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
                { EditorGUIUtility.TextContent("SamsungTV Player is not supported in this build.\nDownload a build that supports it."), null, new GUIContent(kDownloadURL) },
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
            public GUIContent symlinkiOSLibraries = EditorGUIUtility.TextContent("Symlink Unity libraries");
            public GUIContent explicitNullChecks = EditorGUIUtility.TextContent("Explicit Null Checks");
            public GUIContent explicitDivideByZeroChecks = EditorGUIUtility.TextContent("Divide By Zero Checks");
            public GUIContent enableHeadlessMode = EditorGUIUtility.TextContent("Headless Mode");
            public GUIContent buildScriptsOnly = EditorGUIUtility.TextContent("Scripts Only Build");
            public GUIContent learnAboutUnityCloudBuild = EditorGUIUtility.TextContent("Learn about Unity Cloud Build");

            public Styles()
            {
                levelStringCounter.alignment = TextAnchor.MiddleRight;

                if (Unsupported.IsDeveloperBuild() && (
                        buildTargetNotInstalled.GetLength(0) != notLicensedMessages.GetLength(0) ||
                        buildTargetNotInstalled.GetLength(0) != BuildPlatforms.instance.buildPlatforms.Length))
                    Debug.LogErrorFormat("Build platforms and messages are desynced in BuildPlayerWindow! ({0} vs. {1} vs. {2}) DON'T SHIP THIS!", buildTargetNotInstalled.GetLength(0), notLicensedMessages.GetLength(0), BuildPlatforms.instance.buildPlatforms.Length);
            }
        }

        ListViewState lv = new ListViewState();
        bool[] selectedLVItems = new bool[] {};
        bool[] selectedBeforeDrag;
        int initialSelectedLVItem = -1;

        Vector2 scrollPosition = new Vector2(0, 0);

        private const string kAssetsFolder = "Assets/";

        private const string kEditorBuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

        static Styles styles = null;

        static void ShowBuildPlayerWindow()
        {
            EditorUserBuildSettings.selectedBuildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            EditorWindow.GetWindow<BuildPlayerWindow>(true, "Build Settings");
        }

        static void BuildPlayerAndRun()
        {
            if (!BuildPlayerWithDefaultSettings(false, BuildOptions.AutoRunPlayer))
            {
                ShowBuildPlayerWindow();
            }
        }

        static void BuildPlayerAndSelect()
        {
            if (!BuildPlayerWithDefaultSettings(false, BuildOptions.ShowBuiltPlayer))
            {
                ShowBuildPlayerWindow();
            }
        }

        public BuildPlayerWindow()
        {
            position = new Rect(50, 50, 540, 530);
            minSize = new Vector2(630, 580);
            titleContent = new GUIContent("Build Settings");
        }

        static bool BuildPlayerWithDefaultSettings(bool askForBuildLocation, BuildOptions forceOptions)
        {
            return BuildPlayerWithDefaultSettings(askForBuildLocation, forceOptions, true);
        }

        static bool IsMetroPlayer(BuildTarget target)
        {
            return target == BuildTarget.WSAPlayer;
        }

        static bool BuildPlayerWithDefaultSettings(bool askForBuildLocation, BuildOptions forceOptions, bool first)
        {
            bool updateExistingBuild = false;

            if (!UnityConnect.instance.canBuildWithUPID)
            {
                if (!EditorUtility.DisplayDialog("Missing Project ID", "Because you are not a member of this project this build will not access Unity services.\nDo you want to continue?", "Yes", "No"))
                    return false;
            }
            BuildTarget buildTarget = CalculateSelectedBuildTarget();
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (!BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
                return false;
            string module = ModuleManager.GetTargetStringFrom(EditorUserBuildSettings.selectedBuildTargetGroup, buildTarget);
            IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(module);
            if (buildWindowExtension != null && (forceOptions & BuildOptions.AutoRunPlayer) != 0 && !buildWindowExtension.EnabledBuildAndRunButton())
                return false;

            if (Unsupported.IsBleedingEdgeBuild())
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("This version of Unity is a BleedingEdge build that has not seen any manual testing.");
                sb.AppendLine("You should consider this build unstable.");
                sb.AppendLine("We strongly recommend that you use a normal version of Unity instead.");

                if (EditorUtility.DisplayDialog("BleedingEdge Build", sb.ToString(), "Cancel", "OK"))
                    return false;
            }

            // Pick location for the build
            string newLocation = "";
            bool installInBuildFolder = EditorUserBuildSettings.installInBuildFolder && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTargetGroup, buildTarget) && (Unsupported.IsDeveloperBuild()
                                                                                                                                                                               || IsMetroPlayer(buildTarget));

            BuildOptions options = forceOptions;
            // Android build compresses data with lz4 by default when building from a Build Window.
            if (buildTarget == BuildTarget.Android)
                options |= BuildOptions.CompressWithLz4;

            bool developmentBuild = EditorUserBuildSettings.development;
            if (developmentBuild)
                options |= BuildOptions.Development;
            if (EditorUserBuildSettings.allowDebugging && developmentBuild)
                options |= BuildOptions.AllowDebugging;
            if (EditorUserBuildSettings.symlinkLibraries)
                options |= BuildOptions.SymlinkLibraries;

            if (buildTarget == BuildTarget.Android)
            {
                if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
                    options |= BuildOptions.AcceptExternalModificationsToPlayer;
            }

            if (EditorUserBuildSettings.enableHeadlessMode)
                options |= BuildOptions.EnableHeadlessMode;
            if (EditorUserBuildSettings.connectProfiler && (developmentBuild || buildTarget == BuildTarget.WSAPlayer))
                options |= BuildOptions.ConnectWithProfiler;
            if (EditorUserBuildSettings.buildScriptsOnly)
                options |= BuildOptions.BuildScriptsOnly;

            if (installInBuildFolder)
                options |= BuildOptions.InstallInBuildFolder;

            if (!installInBuildFolder)
            {
                if (askForBuildLocation && !PickBuildLocation(buildTargetGroup, buildTarget, options, out updateExistingBuild))
                    return false;
                newLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);

                if (newLocation.Length == 0)
                {
                    return false;
                }

                if (!askForBuildLocation)
                {
                    switch (UnityEditorInternal.InternalEditorUtility.BuildCanBeAppended(buildTarget, newLocation))
                    {
                        case CanAppendBuild.Unsupported:
                            break;
                        case CanAppendBuild.Yes:
                            updateExistingBuild = true;
                            break;
                        case CanAppendBuild.No:
                            if (!PickBuildLocation(buildTargetGroup, buildTarget, options, out updateExistingBuild))
                                return false;

                            newLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);
                            if (newLocation.Length == 0 || !System.IO.Directory.Exists(FileUtil.DeleteLastPathNameComponent(newLocation)))
                                return false;

                            break;
                    }
                }
            }
            if (updateExistingBuild)
                options |= BuildOptions.AcceptExternalModificationsToPlayer;

            // Build a list of scenes that are enabled
            ArrayList scenesList = new ArrayList();
            EditorBuildSettingsScene[] editorScenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in editorScenes)
            {
                if (scene.enabled)
                    scenesList.Add(scene.path);
            }

            string[] scenes = scenesList.ToArray(typeof(string)) as string[];

            // See if we need to switch platforms and delay the build.  We do this whenever
            // we're trying to build for a target different from the active one so as to ensure
            // that the compiled script code we have loaded is built for the same platform we
            // are building for.  As we can't reload while our editor stuff is still executing,
            // we need to defer to after the next script reload then.
            bool delayToAfterScriptReload = false;
            if (EditorUserBuildSettings.activeBuildTarget != buildTarget ||
                EditorUserBuildSettings.activeBuildTargetGroup != buildTargetGroup)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(buildTargetGroup, buildTarget))
                {
                    // Switching the build target failed.  No point in trying to continue
                    // with a build.
                    Debug.LogErrorFormat("Could not switch to build target '{0}', '{1}'.",
                        BuildPipeline.GetBuildTargetGroupDisplayName(buildTargetGroup),
                        BuildPlatforms.instance.GetBuildTargetDisplayName(buildTarget));
                    return false;
                }

                if (EditorApplication.isCompiling)
                    delayToAfterScriptReload = true;
            }

            // Trigger build.
            // Note: report will be null, if delayToAfterScriptReload = true
            var report = BuildPipeline.BuildPlayerInternalNoCheck(scenes, newLocation, null, buildTargetGroup, buildTarget, options, delayToAfterScriptReload);


            return report == null || report.totalErrors == 0;
        }

        void ActiveScenesGUI()
        {
            int i, index;
            int enabledCounter = 0;
            int prevSelectedRow = lv.row;
            bool shiftIsDown = Event.current.shift;
            bool ctrlIsDown = EditorGUI.actionKey;

            Event evt = Event.current;

            Rect scenesInBuildRect = GUILayoutUtility.GetRect(styles.scenesInBuild, styles.title);
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            lv.totalRows = scenes.Count;

            if (selectedLVItems.Length != scenes.Count)
            {
                System.Array.Resize(ref selectedLVItems, scenes.Count);
            }

            int[] enabledCount = new int[scenes.Count];
            for (i = 0; i < enabledCount.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                enabledCount[i] = enabledCounter;
                if (scene.enabled)
                    enabledCounter++;
            }

            foreach (ListViewElement el in ListViewGUILayout.ListView(lv,
                         ListViewOptions.wantsReordering | ListViewOptions.wantsExternalFiles, styles.box))
            {
                EditorBuildSettingsScene scene = scenes[el.row];

                var sceneExists = File.Exists(scene.path);
                using (new EditorGUI.DisabledScope(!sceneExists))
                {
                    bool selected = selectedLVItems[el.row];
                    if (selected && evt.type == EventType.Repaint)
                        styles.selected.Draw(el.position, false, false, false, false);

                    if (!sceneExists)
                        scene.enabled = false;
                    Rect toggleRect = new Rect(el.position.x + 4, el.position.y, styles.toggleSize.x, styles.toggleSize.y);
                    EditorGUI.BeginChangeCheck();
                    scene.enabled = GUI.Toggle(toggleRect, scene.enabled, "");
                    if (EditorGUI.EndChangeCheck() && selected)
                    {
                        // Set all selected scenes to the same state as current scene
                        for (int j = 0; j < scenes.Count; ++j)
                            if (selectedLVItems[j])
                                scenes[j].enabled = scene.enabled;
                    }

                    GUILayout.Space(styles.toggleSize.x);

                    string nicePath = scene.path;
                    if (nicePath.StartsWith(kAssetsFolder))
                        nicePath = nicePath.Substring(kAssetsFolder.Length);

                    const string unityExtension = ".unity";
                    if (nicePath.EndsWith(unityExtension, StringComparison.InvariantCultureIgnoreCase))
                        nicePath = nicePath.Substring(0, nicePath.Length - unityExtension.Length);

                    Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(nicePath), styles.levelString);
                    if (Event.current.type == EventType.Repaint)
                        styles.levelString.Draw(r, EditorGUIUtility.TempContent(nicePath), false, false, selected, false);

                    GUILayout.Label(scene.enabled ? enabledCount[el.row].ToString() : "", styles.levelStringCounter, GUILayout.MaxWidth(36));
                }

                if (ListViewGUILayout.HasMouseUp(el.position) && !shiftIsDown && !ctrlIsDown)
                {
                    if (!shiftIsDown && !ctrlIsDown)
                        ListViewGUILayout.MultiSelection(prevSelectedRow, el.row, ref initialSelectedLVItem, ref selectedLVItems);
                }
                else if (ListViewGUILayout.HasMouseDown(el.position))
                {
                    if (!selectedLVItems[el.row] || shiftIsDown || ctrlIsDown)
                        ListViewGUILayout.MultiSelection(prevSelectedRow, el.row, ref initialSelectedLVItem, ref selectedLVItems);

                    lv.row = el.row;

                    selectedBeforeDrag = new bool[selectedLVItems.Length];
                    selectedLVItems.CopyTo(selectedBeforeDrag, 0);
                    selectedBeforeDrag[lv.row] = true;
                }
            }

            GUI.Label(scenesInBuildRect, styles.scenesInBuild, styles.title);

            // "Select All"
            if (GUIUtility.keyboardControl == lv.ID)
            {
                if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SelectAll")
                {
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SelectAll")
                {
                    for (i = 0; i < selectedLVItems.Length; i++)
                        selectedLVItems[i] = true;

                    lv.selectionChanged = true;

                    Event.current.Use();
                    GUIUtility.ExitGUI();
                }
            }

            if (lv.selectionChanged)
            {
                ListViewGUILayout.MultiSelection(prevSelectedRow, lv.row, ref initialSelectedLVItem, ref selectedLVItems);
            }

            // external file(s) is dragged in
            if (lv.fileNames != null)
            {
                System.Array.Sort(lv.fileNames);
                int k = 0;
                for (i = 0; i < lv.fileNames.Length; i++)
                {
                    if (lv.fileNames[i].EndsWith("unity", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string scenePath = FileUtil.GetProjectRelativePath(lv.fileNames[i]);
                        if (scenePath == string.Empty) // it was relative already
                            scenePath = lv.fileNames[i];

                        if (scenes.Any(s => s.path == scenePath))
                            continue;

                        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene();
                        newScene.path = scenePath;
                        newScene.enabled = true;
                        scenes.Insert(lv.draggedTo + (k++), newScene);
                    }
                }


                if (k != 0)
                {
                    System.Array.Resize(ref selectedLVItems, scenes.Count);

                    for (i = 0; i < selectedLVItems.Length; i++)
                        selectedLVItems[i] = (i >= lv.draggedTo) && (i < lv.draggedTo + k);
                }

                lv.draggedTo = -1;
            }

            if (lv.draggedTo != -1)
            {
                List<EditorBuildSettingsScene> selectedScenes = new List<EditorBuildSettingsScene>();

                // First pick out selected items from array
                index = 0;
                for (i = 0; i < selectedLVItems.Length; i++, index++)
                {
                    if (selectedBeforeDrag[i])
                    {
                        selectedScenes.Add(scenes[index]);
                        scenes.RemoveAt(index);
                        index--;

                        if (lv.draggedTo >= i)
                            lv.draggedTo--;
                    }
                }

                lv.draggedTo = (lv.draggedTo > scenes.Count) || (lv.draggedTo < 0) ? scenes.Count : lv.draggedTo;

                // Add selected items into dragged position
                scenes.InsertRange(lv.draggedTo, selectedScenes);

                for (i = 0; i < selectedLVItems.Length; i++)
                    selectedLVItems[i] = (i >= lv.draggedTo) && (i < lv.draggedTo + selectedScenes.Count);
            }

            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete) && GUIUtility.keyboardControl == lv.ID)
            {
                index = 0;
                for (i = 0; i < selectedLVItems.Length; i++, index++)
                {
                    if (selectedLVItems[i])
                    {
                        scenes.RemoveAt(index);
                        index--;
                    }

                    selectedLVItems[i] = false;
                }

                lv.row = 0;

                evt.Use();
            }

            EditorBuildSettings.scenes = scenes.ToArray();
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
            Repaint();
            GUIUtility.ExitGUI();
        }

        static BuildTarget CalculateSelectedBuildTarget()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            switch (targetGroup)
            {
                case BuildTargetGroup.Standalone:
                    return EditorUserBuildSettings.selectedStandaloneTarget;
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
            EditorGUILayout.HelpBox("Because you are not a member of this project this build will not access Unity services.", MessageType.Warning);
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
                lv.rowHeight = (int)styles.levelString.CalcHeight(new GUIContent("X"), 100);
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

            GUILayout.BeginHorizontal(GUILayout.Height(301));
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
                ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)) ||
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

                return hasMinGraphicsAPI && hasMinOSVersion;
            }
            else
            {
                return true;
            }
        }

        static public string GetPlaybackEngineDownloadURL(string moduleName)
        {
            string fullVersion = InternalEditorUtility.GetUnityVersionFull();
            string revision = "";
            string shortVersion = "";

            int idx = fullVersion.LastIndexOf('_');
            if (idx != -1)
            {
                revision = fullVersion.Substring(idx + 1);
                shortVersion = fullVersion.Substring(0, idx);
            }

            if (moduleName == "PS4" || moduleName == "PSP2" || moduleName == "XboxOne")
                return "https://unity3d.com/platform-installation";

            var moduleNames = new Dictionary<string, string>()
            {
                { "SamsungTV", "Samsung-TV" },
                { "tvOS", "AppleTV" },
                { "OSXStandalone", "Mac" },
                { "WindowsStandalone", "Windows" },
                { "LinuxStandalone", "Linux" },
                { "Facebook", "Facebook-Games"}
            };

            if (moduleNames.ContainsKey(moduleName))
            {
                moduleName = moduleNames[moduleName];
            }

            string prefix = "Unknown";
            string suffix = "Unknown";
            string folder = "Unknown";

            if (shortVersion.IndexOf('a') != -1 || shortVersion.IndexOf('b') != -1)
            {
                prefix = "beta";
                suffix = "download";
            }
            else
            {
                prefix = "download";
                suffix = "download_unity";
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                folder = "TargetSupportInstaller";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                folder = "MacEditorTargetInstaller";
            }

            string url = "http://" + prefix + ".unity3d.com/" + suffix + "/" + revision + "/" + folder + "/UnitySetup-" + moduleName + "-Support-for-Editor-" + shortVersion;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                url += ".exe";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                url += ".pkg";
            }

            return url;
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
                        BuildPlatforms.instance.GetBuildTargetDisplayName(buildTarget));

                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(infoText, EditorStyles.wordWrappedMiniLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("EULA", EditorStyles.miniButton))
                    Application.OpenURL("http://unity3d.com/legal/eula");
                if (GUILayout.Button(string.Format("Add {0} to your Unity Pro license", BuildPlatforms.instance.GetBuildTargetDisplayName(buildTarget)), EditorStyles.miniButton))
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
            // Draw the side bar to the right. Different options like streaming web player, Specific Standalone player to build etc.
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
                    EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle(styles.allowDebugging, EditorUserBuildSettings.allowDebugging);

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
                && EditorUserBuildSettings.exportAsGoogleAndroidProject)
                buildButton = styles.export;

            if (platform.targetGroup == BuildTargetGroup.iOS)
                if (Application.platform != RuntimePlatform.OSXEditor)
                    enableBuildAndRunButton = false;

            // Build Button
            GUI.enabled = enableBuildButton;
            if (GUILayout.Button(buildButton, GUILayout.Width(Styles.kButtonWidth)))
            {
                BuildPlayerWithDefaultSettings(true, BuildOptions.ShowBuiltPlayer);
                GUIUtility.ExitGUI();
            }
            // Build and Run button
            GUI.enabled = enableBuildAndRunButton;
            if (GUILayout.Button(styles.buildAndRun, GUILayout.Width(Styles.kButtonWidth)))
            {
                BuildPlayerWithDefaultSettings(true, BuildOptions.AutoRunPlayer);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }

        private static bool PickBuildLocation(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options, out bool updateExistingBuild)
        {
            updateExistingBuild = false;
            var previousPath = EditorUserBuildSettings.GetBuildLocation(target);

            // When exporting Eclipse project, we're saving a folder, not file,
            // deal with it separately:
            if (target == BuildTarget.Android
                && EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                var exportProjectTitle  = "Export Google Android Project";
                var exportProjectFolder = EditorUtility.SaveFolderPanel(exportProjectTitle, previousPath, "");

                EditorUserBuildSettings.SetBuildLocation(target, exportProjectFolder);
                return true;
            }

            string extension = PostprocessBuildPlayer.GetExtensionForBuildTarget(targetGroup, target, options);

            string defaultFolder = FileUtil.DeleteLastPathNameComponent(previousPath);
            string defaultName = FileUtil.GetLastPathNameComponent(previousPath);
            string title = "Build " + BuildPlatforms.instance.GetBuildTargetDisplayName(target);

            string path = EditorUtility.SaveBuildPanel(target, title, defaultFolder, defaultName, extension, out updateExistingBuild);

            if (path == string.Empty)
                return false;

            // Enforce extension if needed
            if (extension != string.Empty && FileUtil.GetPathExtension(path).ToLower() != extension)
                path += '.' + extension;

            // A path may not be empty initially, but it could contain, e.g., a drive letter (as in Windows),
            // so even appending an extention will work fine, but in reality the name will be, for example,
            // G:/
            //Debug.Log(path);

            string currentlyChosenName = FileUtil.GetLastPathNameComponent(path);
            if (currentlyChosenName == string.Empty)
                return false; // No nameless projects, please

            // We don't want to re-create a directory that already exists, this may
            // result in access-denials that will make users unhappy.
            string check_dir = extension != string.Empty ? FileUtil.DeleteLastPathNameComponent(path) : path;
            if (!Directory.Exists(check_dir))
                Directory.CreateDirectory(check_dir);

            // On OSX we've got replace/update dialog, for other platforms warn about deleting
            // files in target folder.
            if ((target == BuildTarget.iOS) && (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.WindowsEditor))
                if (!FolderIsEmpty(path) && !UserWantsToDeleteFiles(path))
                    return false;

            EditorUserBuildSettings.SetBuildLocation(target, path);
            return true;
        }

        static bool FolderIsEmpty(string path)
        {
            if (!Directory.Exists(path))
                return true;

            return (Directory.GetDirectories(path).Length == 0)
                && (Directory.GetFiles(path).Length == 0);
        }

        static bool UserWantsToDeleteFiles(string path)
        {
            string text =
                "WARNING: all files and folders located in target folder: '" + path + "' will be deleted by build process.";
            return EditorUtility.DisplayDialog("Deleting existing files", text, "OK", "Cancel");
        }
    }
}
