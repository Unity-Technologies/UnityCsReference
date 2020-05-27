// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEditor.ShortcutManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;
using Directory = System.IO.Directory;
using UnityObject = UnityEngine.Object;
using JSONObject = System.Collections.IDictionary;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal static class WindowLayout
    {
        struct LayoutViewInfo
        {
            public LayoutViewInfo(object key, float defaultSize, bool usedByDefault)
            {
                this.key = key;
                used = usedByDefault;
                this.defaultSize = defaultSize;

                className = String.Empty;
                type = null;
                isContainer = false;
                extendedData = null;
            }

            public object key;
            public string className;
            public Type type;
            public bool used;
            public float defaultSize;
            public bool isContainer;
            public JSONObject extendedData;

            public float size
            {
                get
                {
                    if (!used)
                        return 0;
                    return defaultSize;
                }
            }
        }

        private const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";
        private const string kLastLayoutName = "LastLayout.dwlt";
        private const string kDefaultLayoutName = "Default.wlt";
        // Backward compatibility: name of the old (non mode specific) per project layout
        internal const string kOldCurrentLayoutPath = "Library/CurrentLayout.dwlt";
        internal static string layoutsPreferencesPath => FileUtil.CombinePaths(InternalEditorUtility.unityPreferencesFolder, "Layouts");
        internal static string layoutsModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, ModeService.currentId);
        internal static string layoutsDefaultModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, "default");
        internal static string layoutsProjectPath => Directory.GetCurrentDirectory() + "/Library";
        // Backward compatibility: property for old global layout (for default mode only)
        internal static string OldGlobalLayoutPath => Path.Combine(layoutsPreferencesPath, "__Current__.dwlt");
        internal static string ProjectLayoutPath => GetProjectLayoutPerMode(ModeService.currentId);
        internal static string LastLayoutPath => Path.Combine(layoutsModePreferencesPath, kLastLayoutName);

        [UsedImplicitly, RequiredByNativeCode]
        public static void LoadDefaultWindowPreferences()
        {
            LoadCurrentModeLayout(keepMainWindow: false);
        }

        public static void LoadCurrentModeLayout(bool keepMainWindow)
        {
            InitializeLayoutPreferencesFolder();

            var layoutData = ModeService.GetModeDataSection(ModeDescriptor.LayoutKey) as JSONObject;
            if (layoutData == null)
                LoadProjectLayout(keepMainWindow);
            else
            {
                var projectLayoutExists = File.Exists(ProjectLayoutPath);
                if ((projectLayoutExists && Convert.ToBoolean(layoutData["restore_saved_layout"]))
                    || !LoadModeDynamicLayout(keepMainWindow, layoutData))
                    LoadProjectLayout(keepMainWindow);
            }
        }

        private static bool LoadModeDynamicLayout(bool keepMainWindow, JSONObject layoutData)
        {
            const string k_TopViewClassName = "top_view";
            const string k_CenterViewClassName = "center_view";
            const string k_BottomViewClassName = "bottom_view";

            LayoutViewInfo topViewInfo = new LayoutViewInfo(k_TopViewClassName, MainView.kToolbarHeight, true);
            LayoutViewInfo bottomViewInfo = new LayoutViewInfo(k_BottomViewClassName, MainView.kStatusbarHeight, true);
            LayoutViewInfo centerViewInfo = new LayoutViewInfo(k_CenterViewClassName, 0, true);

            var availableEditorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray();

            if (!GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref centerViewInfo))
                return false;

            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref topViewInfo);
            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref bottomViewInfo);

            var mainWindow = GenerateLayout(keepMainWindow, availableEditorWindowTypes, centerViewInfo, topViewInfo, bottomViewInfo);
            if (mainWindow)
                mainWindow.m_DontSaveToLayout = !Convert.ToBoolean(layoutData["restore_saved_layout"]);

            return mainWindow != null;
        }

        private static View LoadLayoutView<T>(Type[] availableEditorWindowTypes, LayoutViewInfo viewInfo, float width, float height) where T : View
        {
            if (!viewInfo.used)
                return null;
            View view = null;
            if (viewInfo.isContainer)
            {
                bool useTabs = viewInfo.extendedData.Contains("tabs") && Convert.ToBoolean(viewInfo.extendedData["tabs"]);
                bool useSplitter = viewInfo.extendedData.Contains("vertical") || viewInfo.extendedData.Contains("horizontal");
                bool isVertical = viewInfo.extendedData.Contains("vertical") && Convert.ToBoolean(viewInfo.extendedData["vertical"]);

                if (useSplitter)
                {
                    var splitView = ScriptableObject.CreateInstance<SplitView>();
                    splitView.vertical = isVertical;
                    view = splitView;
                }
                else if (useTabs)
                {
                    var dockAreaView = ScriptableObject.CreateInstance<DockArea>();
                    view = dockAreaView;
                }

                view.position = new Rect(0, 0, width, height);

                var childrenData = viewInfo.extendedData["children"] as IList;
                if (childrenData == null)
                    throw new InvalidDataException("Invalid split view data");

                int childIndex = 0;
                foreach (var childData in childrenData)
                {
                    var lvi = new LayoutViewInfo(childIndex, useTabs ? 1f : 1f / childrenData.Count, true);
                    if (!ParseViewData(availableEditorWindowTypes, childData, ref lvi))
                        continue;

                    var cw = useTabs ? width : (isVertical ? width : width * lvi.size);
                    var ch = useTabs ? height : (isVertical ? height * lvi.size : height);
                    if (useTabs)
                    {
                        (view as DockArea).AddTab((EditorWindow)ScriptableObject.CreateInstance(lvi.type));
                    }
                    else
                    {
                        view.AddChild(LoadLayoutView<HostView>(availableEditorWindowTypes, lvi, cw, ch));
                        view.children[childIndex].position = new Rect(0, 0, cw, ch);
                    }

                    childIndex++;
                }
            }
            else
            {
                if (viewInfo.type != null)
                {
                    var hostView = ScriptableObject.CreateInstance<HostView>();
                    hostView.SetActualViewInternal(ScriptableObject.CreateInstance(viewInfo.type) as EditorWindow, true);
                    view = hostView;
                }
                else
                    view = ScriptableObject.CreateInstance<T>();
            }

            return view;
        }

        private static ContainerWindow GenerateLayout(bool keepMainWindow, Type[] availableEditorWindowTypes, LayoutViewInfo center, LayoutViewInfo top, LayoutViewInfo bottom)
        {
            ContainerWindow mainContainerWindow = null;
            var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode != ShowMode.MainWindow)
                    continue;

                mainContainerWindow = window;
                break;
            }

            if (keepMainWindow && mainContainerWindow == null)
            {
                Debug.LogWarning($"No main window to restore layout from while loading dynamic layout for mode {ModeService.currentId}");
                return null;
            }

            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                if (!mainContainerWindow)
                    mainContainerWindow = ScriptableObject.CreateInstance<ContainerWindow>();

                mainContainerWindow.windowID = $"MainView_{ModeService.currentId}";
                mainContainerWindow.LoadGeometry(true);

                var width = mainContainerWindow.position.width;
                var height = mainContainerWindow.position.height;

                // Create center view
                View centerView = LoadLayoutView<DockArea>(availableEditorWindowTypes, center, width, height);
                var topView = LoadLayoutView<Toolbar>(availableEditorWindowTypes, top, width, height);
                var bottomView = LoadLayoutView<AppStatusBar>(availableEditorWindowTypes, bottom, width, height);

                var main = ScriptableObject.CreateInstance<MainView>();
                main.useTopView = top.used;
                main.useBottomView = bottom.used;
                main.topViewHeight = top.size;
                main.bottomViewHeight = bottom.size;

                // Top View
                if (topView)
                {
                    topView.position = new Rect(0, 0, width, top.size);
                    main.AddChild(topView);
                }

                // Center View
                var centerViewHeight = height - bottom.size - top.size;
                centerView.position = new Rect(0, top.size, width, centerViewHeight);
                main.AddChild(centerView);

                // Bottom View
                if (bottomView)
                {
                    bottomView.position = new Rect(0, height - bottom.size, width, bottom.size);
                    main.AddChild(bottomView);
                }

                if (mainContainerWindow.rootView)
                    ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

                mainContainerWindow.rootView = main;
                mainContainerWindow.rootView.position = new Rect(0, 0, width, height);
                mainContainerWindow.Show(ShowMode.MainWindow, true, true, true);
                mainContainerWindow.DisplayAllViews();
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }

            return mainContainerWindow;
        }

        private static bool GetLayoutViewInfo(JSONObject layoutData, Type[] availableEditorWindowTypes, ref LayoutViewInfo viewInfo)
        {
            if (!layoutData.Contains(viewInfo.key))
                return false;

            var viewData = layoutData[viewInfo.key];
            return ParseViewData(availableEditorWindowTypes, viewData, ref viewInfo);
        }

        private static bool ParseViewData(Type[] availableEditorWindowTypes, object viewData, ref LayoutViewInfo viewInfo)
        {
            if (viewData is string)
            {
                viewInfo.className = Convert.ToString(viewData);
                viewInfo.used = !String.IsNullOrEmpty(viewInfo.className);
                if (!viewInfo.used)
                    return true;
            }
            else if (viewData is IDictionary)
            {
                var viewExpandedData = viewData as IDictionary;

                if (viewExpandedData.Contains("children") || viewExpandedData.Contains("vertical") || viewExpandedData.Contains("horizontal") || viewExpandedData.Contains("tabs"))
                {
                    viewInfo.isContainer = true;
                    viewInfo.className = String.Empty;
                }
                else
                {
                    viewInfo.isContainer = false;
                    viewInfo.className = Convert.ToString(viewExpandedData["class_name"]);
                }

                if (viewExpandedData.Contains("size"))
                    viewInfo.defaultSize = Convert.ToSingle(viewExpandedData["size"]);
                viewInfo.extendedData = viewExpandedData;
            }
            else
            {
                viewInfo.className = String.Empty;
                viewInfo.type = null;
                viewInfo.used = false;
                return true;
            }

            if (String.IsNullOrEmpty(viewInfo.className))
                return true;

            foreach (var t in availableEditorWindowTypes)
            {
                if (t.Name != viewInfo.className)
                    continue;
                viewInfo.type = t;
                break;
            }

            if (viewInfo.type == null)
            {
                Debug.LogWarning($"Invalid layout view {viewInfo.key} with type {viewInfo.className} for mode {ModeService.currentId}");
                return false;
            }

            return true;
        }

        private static void LoadProjectLayout(bool keepMainWindow)
        {
            var projectLayoutExists = File.Exists(ProjectLayoutPath);
            if (!projectLayoutExists)
            {
                var currentLayoutPath = GetCurrentLayoutPath();
                FileUtil.CopyFileOrDirectory(currentLayoutPath, ProjectLayoutPath);
            }

            Debug.Assert(File.Exists(ProjectLayoutPath));

            // Load the current project layout
            LoadWindowLayout(ProjectLayoutPath, !projectLayoutExists, false, keepMainWindow);
        }

        [UsedImplicitly, RequiredByNativeCode]
        public static void SaveDefaultWindowPreferences()
        {
            // Do not save layout to default if running tests
            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            SaveCurrentLayoutPerMode(ModeService.currentId);
        }

        internal static void SaveCurrentLayoutPerMode(string modeId)
        {
            // Save Project Current Layout
            SaveWindowLayout(FileUtil.CombinePaths(Directory.GetCurrentDirectory(), GetProjectLayoutPerMode(modeId)));

            // Save Global Last Layout
            SaveWindowLayout(FileUtil.CombinePaths(layoutsPreferencesPath, modeId, kLastLayoutName));
        }

        internal static string GetCurrentLayoutPath()
        {
            var currentLayoutPath = ProjectLayoutPath;

            // Make sure we have a current layout file created
            if (!File.Exists(ProjectLayoutPath))
            {
                currentLayoutPath = GetDefaultLayoutPath();
                if (File.Exists(LastLayoutPath))
                {
                    // First we try to load the last layout (per mode)
                    currentLayoutPath = LastLayoutPath;
                }
                else if (ModeService.currentId == ModeService.k_DefaultModeId)
                {
                    // Backward compatibility check:
                    // Old non mode Library\CurrentLayout.dwlt
                    if (File.Exists(kOldCurrentLayoutPath))
                    {
                        currentLayoutPath = kOldCurrentLayoutPath;
                    }
                    else
                    {
                        // Older non mode <Prefs>\__Current__.dwlt
                        if (File.Exists(OldGlobalLayoutPath))
                        {
                            currentLayoutPath = OldGlobalLayoutPath;
                        }
                    }
                }
            }

            return currentLayoutPath;
        }

        internal static string GetDefaultLayoutPath()
        {
            return Path.Combine(layoutsModePreferencesPath, kDefaultLayoutName);
        }

        internal static string GetProjectLayoutPerMode(string modeId)
        {
            return $"Library/CurrentLayout-{modeId}.dwlt";
        }

        private static void InitializeLayoutPreferencesFolder()
        {
            string defaultLayoutPath = GetDefaultLayoutPath();
            string layoutResourcesPath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts");

            if (!Directory.Exists(layoutsPreferencesPath))
                Directory.CreateDirectory(layoutsPreferencesPath);

            if (!Directory.Exists(layoutsModePreferencesPath))
            {
                // Make sure we have a valid default mode folder initialized with the proper default layouts.
                if (layoutsDefaultModePreferencesPath == layoutsModePreferencesPath)
                {
                    // Backward compatibility: if the default layout folder doesn't exists but some layouts have been
                    // saved be sure to copy them to the "default layout per mode folder".
                    FileUtil.CopyFileOrDirectory(layoutResourcesPath, layoutsDefaultModePreferencesPath);
                    var defaultModeUserLayouts = Directory.GetFiles(layoutsPreferencesPath, "*.wlt");
                    foreach (var layoutPath in defaultModeUserLayouts)
                    {
                        var fileName = Path.GetFileName(layoutPath);
                        var dst = Path.Combine(layoutsDefaultModePreferencesPath, fileName);
                        if (!File.Exists(dst))
                            FileUtil.CopyFileIfExists(layoutPath, dst, false);
                    }
                }
                else
                {
                    Directory.CreateDirectory(layoutsModePreferencesPath);
                }
            }

            // Make sure we have the default layout file in the preferences folder
            if (!File.Exists(defaultLayoutPath))
            {
                var defaultModeLayoutPath = ModeService.GetDefaultModeLayout();
                if (!File.Exists(defaultModeLayoutPath))
                {
                    // No mode default layout, use the editor_resources Default:
                    defaultModeLayoutPath = Path.Combine(layoutResourcesPath, kDefaultLayoutName);
                }
                // If not copy our default file to the preferences folder
                FileUtil.CopyFileOrDirectory(defaultModeLayoutPath, defaultLayoutPath);
            }
            Debug.Assert(File.Exists(defaultLayoutPath));
        }

        static WindowLayout()
        {
            EditorApplication.update -= DelayReloadWindowLayoutMenu;
            EditorApplication.update += DelayReloadWindowLayoutMenu;
        }

        internal static void DelayReloadWindowLayoutMenu()
        {
            EditorApplication.update -= DelayReloadWindowLayoutMenu;
            if (ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
            {
                ReloadWindowLayoutMenu();
                EditorUtility.Internal_UpdateAllMenus();
            }
        }

        internal static void ReloadWindowLayoutMenu()
        {
            Menu.RemoveMenuItem("Window/Layouts");

            if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
                return;

            int layoutMenuItemPriority = 20;

            // Get user saved layouts
            if (Directory.Exists(layoutsModePreferencesPath))
            {
                var layoutPaths = Directory.GetFiles(layoutsModePreferencesPath).Where(path => path.EndsWith(".wlt")).ToArray();
                foreach (var layoutPath in layoutPaths)
                {
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", false, layoutMenuItemPriority++, () => LoadWindowLayout(layoutPath, false), null);
                }

                layoutMenuItemPriority += 500;
            }

            // Get mode layouts
            var modeLayoutPaths = ModeService.GetModeDataSection(ModeService.currentIndex, ModeDescriptor.LayoutsKey) as IList<object>;
            if (modeLayoutPaths != null)
            {
                foreach (var layoutPath in modeLayoutPaths.Cast<string>())
                {
                    if (!File.Exists(layoutPath))
                        continue;
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", Toolbar.lastLoadedLayoutName == name, layoutMenuItemPriority++, () => LoadWindowLayout(layoutPath, false), null);
                }
            }

            layoutMenuItemPriority += 500;

            Menu.AddMenuItem("Window/Layouts/Save Layout...", "", false, layoutMenuItemPriority++, SaveGUI, null);
            Menu.AddMenuItem("Window/Layouts/Delete Layout...", "", false, layoutMenuItemPriority++, DeleteGUI, null);
            Menu.AddMenuItem("Window/Layouts/Revert Factory Settings...", "", false, layoutMenuItemPriority++, () => RevertFactorySettings(false), null);

            Menu.AddMenuItem("Window/Layouts/More/Save to disk...", "", false, 998, () => SaveToFile(), null);
            Menu.AddMenuItem("Window/Layouts/More/Load from disk...", "", false, 999, () => LoadFromFile(), null);
        }

        internal static EditorWindow FindEditorWindowOfType(Type type)
        {
            UnityObject[] obj = Resources.FindObjectsOfTypeAll(type);
            if (obj.Length > 0)
                return obj[0] as EditorWindow;
            return null;
        }

        internal static void CheckWindowConsistency()
        {
            UnityObject[] wins = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));

            foreach (EditorWindow win in wins)
            {
                if (win.m_Parent == null)
                {
                    Debug.LogError("Invalid editor window " + win.GetType());
                }
            }
        }

        internal static EditorWindow TryGetLastFocusedWindowInSameDock()
        {
            // Get type of window that was docked together with game view and was focused before play mode
            Type type = null;
            string windowTypeName = WindowFocusState.instance.m_LastWindowTypeInSameDock;
            if (windowTypeName != "")
                type = Type.GetType(windowTypeName);

            // Also get the PlayModeView Window
            var playModeView = PlayModeView.GetMainPlayModeView();
            if (type != null && playModeView && playModeView != null && playModeView.m_Parent is DockArea)
            {
                // Get all windows of that type
                object[] potentials = Resources.FindObjectsOfTypeAll(type);

                DockArea dock = playModeView.m_Parent as DockArea;

                // Find the one that is actually docked together with the GameView
                for (int i = 0; i < potentials.Length; i++)
                {
                    EditorWindow window = potentials[i] as EditorWindow;
                    if (window && window.m_Parent == dock)
                        return window;
                }
            }

            return null;
        }

        internal static void SaveCurrentFocusedWindowInSameDock(EditorWindow windowToBeFocused)
        {
            if (windowToBeFocused.m_Parent != null && windowToBeFocused.m_Parent is DockArea)
            {
                DockArea dock = windowToBeFocused.m_Parent as DockArea;

                // Get currently focused window/tab in that dock
                EditorWindow actualView = dock.actualView;
                if (actualView)
                    WindowFocusState.instance.m_LastWindowTypeInSameDock = actualView.GetType().ToString();
            }
        }

        internal static void FindFirstGameViewAndSetToMaximizeOnPlay()
        {
            GameView gameView = (GameView)FindEditorWindowOfType(typeof(GameView));
            if (gameView)
                gameView.maximizeOnPlay = true;
        }

        internal static EditorWindow TryFocusAppropriateWindow(bool enteringPlaymode)
        {
            if (enteringPlaymode)
            {
                var playModeView = PlayModeView.GetMainPlayModeView();
                if (playModeView)
                {
                    SaveCurrentFocusedWindowInSameDock(playModeView);
                    playModeView.Focus();
                }

                return playModeView;
            }
            else
            {
                // If we can retrieve what window type was active when we went into play mode,
                // go back to focus a window of that type.
                EditorWindow window = TryGetLastFocusedWindowInSameDock();
                if (window)
                    window.ShowTab();
                return window;
            }
        }

        internal static EditorWindow GetMaximizedWindow()
        {
            UnityObject[] maximized = Resources.FindObjectsOfTypeAll(typeof(MaximizedHostView));
            if (maximized.Length != 0)
            {
                MaximizedHostView maximizedView = maximized[0] as MaximizedHostView;
                if (maximizedView.actualView)
                    return maximizedView.actualView;
            }

            return null;
        }

        internal static EditorWindow ShowAppropriateViewOnEnterExitPlaymode(bool entering)
        {
            // Prevent trying to go into the same state as we're already in, as it will break things
            if (WindowFocusState.instance.m_CurrentlyInPlayMode == entering)
                return null;

            WindowFocusState.instance.m_CurrentlyInPlayMode = entering;

            EditorWindow window = null;

            EditorWindow maximized = GetMaximizedWindow();

            if (entering)
            {
                WindowFocusState.instance.m_WasMaximizedBeforePlay = (maximized != null);

                // If a view is already maximized before entering play mode,
                // just keep that maximized view, no matter if it's the game view or some other.
                // Trust that user has a good reason (desire by Ethan etc.)
                if (maximized != null)
                    return maximized;
            }
            else
            {
                // If a view was already maximized before entering play mode,
                // then it was kept when switching to play mode, and can simply still be kept when exiting
                if (WindowFocusState.instance.m_WasMaximizedBeforePlay)
                    return maximized;
            }

            // Unmaximize if maximized
            if (maximized)
                Unmaximize(maximized);

            // Try finding and focusing appropriate window/tab
            window = TryFocusAppropriateWindow(entering);
            if (window)
                return window;

            // If we are entering Play more and no Game View was found, create one
            if (entering)
            {
                // Try to create and focus a Game View tab docked together with the Scene View tab
                EditorWindow sceneView = FindEditorWindowOfType(typeof(SceneView));
                GameView gameView;
                if (sceneView && sceneView.m_Parent is DockArea)
                {
                    DockArea dock = sceneView.m_Parent as DockArea;
                    if (dock)
                    {
                        WindowFocusState.instance.m_LastWindowTypeInSameDock = sceneView.GetType().ToString();
                        gameView = ScriptableObject.CreateInstance<GameView>();
                        dock.AddTab(gameView);
                        return gameView;
                    }
                }

                // If no Scene View was found at all, just create a floating Game View
                gameView = ScriptableObject.CreateInstance<GameView>();
                gameView.Show(true);
                gameView.Focus();

                return gameView;
            }

            return window;
        }

        internal static bool IsMaximized(EditorWindow window)
        {
            return window.m_Parent is MaximizedHostView;
        }

        public static void Unmaximize(EditorWindow win)
        {
            HostView maximizedHostView = win.m_Parent;
            if (maximizedHostView == null)
            {
                Debug.LogError("Host view was not found");
                RevertFactorySettings();
                return;
            }

            UnityObject[] newWindows = InternalEditorUtility.LoadSerializedFileAndForget(Path.Combine(layoutsProjectPath, kMaximizeRestoreFile));

            if (newWindows.Length < 2)
            {
                Debug.Log("Maximized serialized file backup not found");
                RevertFactorySettings();
                return;
            }

            SplitView oldRoot = newWindows[0] as SplitView;
            EditorWindow oldWindow = newWindows[1] as EditorWindow;

            if (oldRoot == null)
            {
                Debug.Log("Maximization failed because the root split view was not found");
                RevertFactorySettings();
                return;
            }

            ContainerWindow parentWindow = win.m_Parent.window;
            if (parentWindow == null)
            {
                Debug.Log("Maximization failed because the root split view has no container window");
                RevertFactorySettings();
                return;
            }

            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                // Put the loaded SplitView where the MaximizedHostView was
                if (maximizedHostView.parent)
                {
                    int i = maximizedHostView.parent.IndexOfChild(maximizedHostView);
                    Rect r = maximizedHostView.position;
                    View parent = maximizedHostView.parent;
                    parent.RemoveChild(i);
                    parent.AddChild(oldRoot, i);
                    oldRoot.position = r;

                    // Move the Editor Window to the right spot in the
                    DockArea newDockArea = oldWindow.m_Parent as DockArea;

                    int oldDockAreaIndex = newDockArea.m_Panes.IndexOf(oldWindow);

                    maximizedHostView.actualView = null;
                    win.m_Parent = null;
                    newDockArea.AddTab(oldDockAreaIndex, win);
                    newDockArea.RemoveTab(oldWindow);
                    UnityObject.DestroyImmediate(oldWindow);

                    foreach (UnityObject o in newWindows)
                    {
                        EditorWindow curWin = o as EditorWindow;
                        if (curWin != null)
                            curWin.MakeParentsSettingsMatchMe();
                    }

                    parent.Initialize(parent.window);

                    //If parent window had to be resized, call this to make sure new size gets propagated
                    parent.position = parent.position;
                    oldRoot.Reflow();
                }
                else
                {
                    throw new Exception();
                }

                // Kill the maximizedMainView
                UnityObject.DestroyImmediate(maximizedHostView);

                win.Focus();

                parentWindow.DisplayAllViews();
                win.m_Parent.MakeVistaDWMHappyDance();
            }
            catch (Exception ex)
            {
                Debug.Log("Maximization failed: " + ex);
                RevertFactorySettings();
            }

            try
            {
                // Weird bug on AMD graphic cards under OSX Lion: Sometimes when unmaximizing we get stray white rectangles.
                // work around that by issuing an extra repaint (case 438764)
                if (Application.platform == RuntimePlatform.OSXEditor && SystemInfo.operatingSystem.Contains("10.7") && SystemInfo.graphicsDeviceVendor.Contains("ATI"))
                {
                    foreach (GUIView v in Resources.FindObjectsOfTypeAll(typeof(GUIView)))
                        v.Repaint();
                }
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }
        }

        internal static void MaximizeGestureHandler()
        {
            if (Event.current.type != EditorGUIUtility.magnifyGestureEventType || GUIUtility.hotControl != 0)
                return;

            var mouseOverWindow = EditorWindow.mouseOverWindow;
            if (mouseOverWindow == null)
                return;

            var args = new ShortcutArguments { stage = ShortcutStage.End };
            if (IsMaximized(mouseOverWindow))
                args.context = Event.current.delta.x < -0.05f ? mouseOverWindow : null;
            else
                args.context = Event.current.delta.x > 0.05f ? mouseOverWindow : null;

            if (args.context != null)
                MaximizeKeyHandler(args);
        }

        [Shortcut("Window/Maximize View", KeyCode.Space, ShortcutModifiers.Shift)]
        [FormerlyPrefKeyAs("Window/Maximize View", "# ")]
        internal static void MaximizeKeyHandler(ShortcutArguments args)
        {
            if (args.context is PreviewWindow)
                return;

            var mouseOverWindow = EditorWindow.mouseOverWindow;

            if (mouseOverWindow != null)
            {
                if (IsMaximized(mouseOverWindow))
                    Unmaximize(mouseOverWindow);
                else
                    Maximize(mouseOverWindow);
            }
        }

        public static void AddSplitViewAndChildrenRecurse(View splitview, ArrayList list)
        {
            list.Add(splitview);
            DockArea dock = splitview as DockArea;
            if (dock != null)
            {
                list.AddRange(dock.m_Panes);
                list.Add(dock.actualView);
            }

            foreach (View child in splitview.children)
                AddSplitViewAndChildrenRecurse(child, list);
        }

        public static void SaveSplitViewAndChildren(View splitview, EditorWindow win, string path)
        {
            ArrayList all = new ArrayList();

            AddSplitViewAndChildrenRecurse(splitview, all);
            all.Remove(splitview);
            all.Remove(win);
            all.Insert(0, splitview);
            all.Insert(1, win);

            InternalEditorUtility.SaveToSerializedFileAndForget(all.ToArray(typeof(UnityObject)) as UnityObject[], path, true);
        }

        public static void Maximize(EditorWindow win)
        {
            bool maximizePending = MaximizePrepare(win);
            if (maximizePending)
                MaximizePresent(win);
        }

        public static bool MaximizePrepare(EditorWindow win)
        {
            // Find Root SplitView
            View itor = win.m_Parent.parent;
            View rootSplit = itor;
            while (itor != null && itor is SplitView)
            {
                rootSplit = itor;
                itor = itor.parent;
            }

            // Make sure it has a dockarea
            DockArea dockArea = win.m_Parent as DockArea;
            if (dockArea == null)
                return false;

            if (itor == null)
                return false;

            var mainView = rootSplit.parent as MainView;
            if (mainView == null)
                return false;

            ContainerWindow parentWindow = win.m_Parent.window;
            if (parentWindow == null)
                return false;

            int oldDockIndex = dockArea.m_Panes.IndexOf(win);
            if (oldDockIndex == -1)
                return false;

            dockArea.selected = oldDockIndex;

            // Save current state to disk
            SaveSplitViewAndChildren(rootSplit, win, Path.Combine(layoutsProjectPath, kMaximizeRestoreFile));

            // Remove the window from the HostView now in order to invoke OnBecameInvisible before OnBecameVisible
            dockArea.actualView = null;

            dockArea.m_Panes[oldDockIndex] = null;

            MaximizedHostView maximizedHostView = ScriptableObject.CreateInstance<MaximizedHostView>();

            int i = itor.IndexOfChild(rootSplit);
            Rect p = rootSplit.position;
            itor.RemoveChild(rootSplit);
            itor.AddChild(maximizedHostView, i);

            maximizedHostView.actualView = win;
            maximizedHostView.position = p; // Must be set after actualView so that value is propagated

            var gv = win as GameView;
            if (gv != null)
                maximizedHostView.EnableVSync(gv.vSyncEnabled);

            UnityObject.DestroyImmediate(rootSplit, true);
            return true;
        }

        public static void MaximizePresent(EditorWindow win)
        {
            ContainerWindow.SetFreezeDisplay(true);

            win.Focus();

            CheckWindowConsistency();

            ContainerWindow parentWindow = win.m_Parent.window;
            parentWindow.DisplayAllViews();

            win.m_Parent.MakeVistaDWMHappyDance();

            ContainerWindow.SetFreezeDisplay(false);

            win.OnMaximized();
        }

        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated)
        {
            return LoadWindowLayout(path, newProjectLayoutWasCreated, true, false);
        }

        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow)
        {
            Console.WriteLine($"[LAYOUT] About to load {path}, keepMainWindow={keepMainWindow}");

            bool mainWindowMaximized = false;
            Rect mainWindowPosition = new Rect();
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode == ShowMode.MainWindow)
                {
                    mainWindowPosition = window.position;
                    mainWindowMaximized = window.maximized;
                }
            }

            bool layoutLoadingIssue = false;

            // Load new windows and show them
            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                CloseWindows(keepMainWindow);

                ContainerWindow mainWindowToSetSize = null;
                ContainerWindow mainWindow = null;

                UnityObject[] remainingContainers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
                foreach (ContainerWindow window in remainingContainers)
                {
                    if (mainWindow == null && window.showMode == ShowMode.MainWindow)
                        mainWindow = window;
                    else
                        window.Close();
                }

                // Load data
                UnityObject[] loadedWindows = InternalEditorUtility.LoadSerializedFileAndForget(path);

                if (loadedWindows == null || loadedWindows.Length == 0)
                {
                    throw new ArgumentException("Window layout at '" + path + "' could not be loaded.");
                }

                List<UnityObject> newWindows = new List<UnityObject>();

                // At this point, unparented editor windows are neither desired nor desirable.
                // This can be caused by (legacy) serialization of FallbackEditorWindows or
                // other serialization hiccups (note that unparented editor windows should not exist in theory).
                // Same goes for empty DockAreas (no panes).  Leave them behind.
                for (int i = 0; i < loadedWindows.Length; i++)
                {
                    UnityObject o = loadedWindows[i];

                    EditorWindow editorWin = o as EditorWindow;
                    if (editorWin != null)
                    {
                        if (editorWin.m_Parent == null)
                        {
                            UnityObject.DestroyImmediate(editorWin, true);
                            Console.WriteLine("LoadWindowLayout: Removed unparented EditorWindow while reading window layout: window #" + i + ", type=" +
                                o.GetType() + ", instanceID=" + o.GetInstanceID());
                            layoutLoadingIssue = true;
                            continue;
                        }
                    }
                    else
                    {
                        ContainerWindow cw = o as ContainerWindow;
                        if (cw != null && cw.rootView == null)
                        {
                            cw.Close();
                            UnityObject.DestroyImmediate(cw, true);
                            continue;
                        }

                        DockArea dockArea = o as DockArea;
                        if (dockArea != null && dockArea.m_Panes.Count == 0)
                        {
                            dockArea.Close(null);
                            UnityObject.DestroyImmediate(dockArea, true);
                            continue;
                        }

                        // Host views that do not hold any containers are not desirable at this stage
                        HostView hostview = o as HostView;
                        if (hostview != null && hostview.actualView == null)
                        {
                            UnityObject.DestroyImmediate(hostview, true);
                            continue;
                        }
                    }

                    newWindows.Add(o);
                }

                for (int i = 0; i < newWindows.Count; i++)
                {
                    ContainerWindow cur = newWindows[i] as ContainerWindow;
                    if (cur != null && cur.showMode == ShowMode.MainWindow)
                    {
                        if (mainWindow == null)
                        {
                            mainWindow = cur;
                        }
                        else
                        {
                            mainWindow.rootView = cur.rootView;
                            UnityObject.DestroyImmediate(cur, true);
                            cur = mainWindow;
                            newWindows[i] = null;
                        }

                        if (mainWindowPosition.width != 0.0)
                        {
                            mainWindowToSetSize = cur;
                            mainWindowToSetSize.position = mainWindowPosition;
                        }

                        break;
                    }
                }

                for (int i = 0; i < newWindows.Count; i++)
                {
                    UnityObject o = newWindows[i];
                    if (!o)
                        continue;

                    if (newProjectLayoutWasCreated)
                    {
                        MethodInfo method = o.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        method?.Invoke(o, null);
                    }
                }

                if (mainWindowToSetSize)
                    mainWindowToSetSize.position = mainWindowPosition;

                // Always show main window before other windows. So that other windows can
                // get their parent/owner.
                if (!mainWindow)
                {
                    Debug.LogError("Error while reading window layout: no main window found");
                    throw new Exception();
                }

                mainWindow.Show(mainWindow.showMode, loadPosition: true, displayImmediately: true, setFocus: true);
                if (mainWindow.maximized != mainWindowMaximized)
                    mainWindow.ToggleMaximize();

                // Make sure to restore the save to layout flag when loading a layout from a file.
                if (keepMainWindow)
                    mainWindow.m_DontSaveToLayout = false;

                // Show other windows
                for (int i = 0; i < newWindows.Count; i++)
                {
                    if (newWindows[i] == null)
                        continue;

                    EditorWindow win = newWindows[i] as EditorWindow;
                    if (win)
                        win.minSize = win.minSize; // Causes minSize to be propagated upwards to parents!

                    ContainerWindow containerWindow = newWindows[i] as ContainerWindow;
                    if (containerWindow && containerWindow != mainWindow)
                        containerWindow.Show(containerWindow.showMode, loadPosition: false, displayImmediately: true, setFocus: true);
                }

                // Unmaximize maximized PlayModeView window if maximize on play is enabled
                PlayModeView playModeView = GetMaximizedWindow() as PlayModeView;
                if (playModeView != null && playModeView.maximizeOnPlay)
                    Unmaximize(playModeView);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load window layout: " + ex);

                int option = 0;

                UnityObject[] containerWindows = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));

                // Only show dialog if an actual window is present. If not, revert to default immediately
                if (!Application.isTestRun && containerWindows.Length > 0)
                    option = EditorUtility.DisplayDialogComplex("Failed to load window layout", "This can happen if layout contains custom windows and there are compile errors in the project.", "Load Default Layout", "Quit", "Revert Factory Settings");

                switch (option)
                {
                    case 0:
                        LoadDefaultLayout();
                        break;
                    case 1:
                        EditorApplication.Exit(0);
                        break;
                    case 2:
                        RevertFactorySettings();
                        break;
                }

                return false;
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);

                if (setLastLoadedLayoutName && Path.GetExtension(path) == ".wlt")
                    Toolbar.lastLoadedLayoutName = Path.GetFileNameWithoutExtension(path);
                else
                    Toolbar.lastLoadedLayoutName = null;
            }

            if (layoutLoadingIssue)
                Debug.Log("The editor layout could not be fully loaded, this can happen when the layout contains EditorWindows not available in this project");

            return true;
        }

        internal static void LoadDefaultLayout()
        {
            InitializeLayoutPreferencesFolder();

            FileUtil.DeleteFileOrDirectory(ProjectLayoutPath);
            FileUtil.CopyFileOrDirectory(GetDefaultLayoutPath(), ProjectLayoutPath);
            Debug.Assert(File.Exists(ProjectLayoutPath));

            LoadWindowLayout(ProjectLayoutPath, true);
        }

        public static void CloseWindows()
        {
            CloseWindows(false);
        }

        private static void CloseWindows(bool keepMainWindow)
        {
            try
            {
                // Close any existing tooltips
                TooltipView.Close();
            }
            catch (Exception)
            {
                // ignored
            }

            // Close all container windows
            ContainerWindow mainWindow = null;
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                try
                {
                    if (window.showMode != ShowMode.MainWindow || !keepMainWindow || mainWindow != null)
                    {
                        window.Close();
                        UnityObject.DestroyImmediate(window, true);
                    }
                    else
                    {
                        UnityObject.DestroyImmediate(window.rootView, true);
                        window.rootView = null;
                        mainWindow = window;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            // Double check correct closing
            UnityObject[] oldWindows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            if (oldWindows.Length != 0)
            {
                string output = "";
                foreach (EditorWindow killme in oldWindows)
                {
                    output += "\n" + killme.GetType().Name;
                    UnityObject.DestroyImmediate(killme, true);
                }

                Debug.LogError("Failed to destroy editor windows: #" + oldWindows.Length + output);
            }

            UnityObject[] oldViews = Resources.FindObjectsOfTypeAll(typeof(View));
            if (oldViews.Length != 0)
            {
                foreach (View killme in oldViews)
                    UnityObject.DestroyImmediate(killme, true);
            }

            UnityObject[] toolbars = Resources.FindObjectsOfTypeAll(typeof(EditorToolbar));
            foreach (var killme in toolbars)
                UnityObject.DestroyImmediate(killme, true);
        }

        public static void SaveWindowLayout(string path)
        {
            Console.WriteLine($"[LAYOUT] About to save layout {path}");
            TooltipView.Close();

            ArrayList all = new ArrayList();
            UnityObject[] windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            UnityObject[] views = Resources.FindObjectsOfTypeAll(typeof(View));
            UnityObject[] toolbars = Resources.FindObjectsOfTypeAll(typeof(EditorToolbar));

            foreach (ContainerWindow w in containers)
            {
                // skip ContainerWindows that are "dont save me"
                if (w.m_DontSaveToLayout)
                    continue;
                all.Add(w);
            }

            foreach (View w in views)
            {
                // skip Views that belong to "dont save me" container
                if (w.window != null && w.window.m_DontSaveToLayout)
                    continue;
                all.Add(w);
            }

            foreach (EditorWindow w in windows)
            {
                // skip EditorWindows that belong to "dont save me" container
                if (w.m_Parent != null && w.m_Parent.window != null && w.m_Parent.window.m_DontSaveToLayout)
                    continue;
                all.Add(w);
            }

            foreach (EditorToolbar toolbar in toolbars)
            {
                if (toolbar.m_DontSaveToLayout)
                    continue;
                all.Add(toolbar);
            }

            var parentLayoutFolder = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(parentLayoutFolder))
            {
                if (!Directory.Exists(parentLayoutFolder))
                    Directory.CreateDirectory(parentLayoutFolder);
                InternalEditorUtility.SaveToSerializedFileAndForget(all.ToArray(typeof(UnityObject)) as UnityObject[], path, true);
            }
        }

        internal static View FindMainView()
        {
            UnityEngine.Object[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode == ShowMode.MainWindow)
                    return window.rootView;
            }

            Debug.LogError("No Main View found!");
            return null;
        }

        public static void SaveGUI()
        {
            UnityEditor.SaveWindowLayout.Show(FindMainView().screenPosition);
        }

        public static void LoadFromFile()
        {
            var layoutFilePath = EditorUtility.OpenFilePanelWithFilters("Load layout from disk...", "", new[] {"Layout", "wlt"});
            if (String.IsNullOrEmpty(layoutFilePath))
                return;

            if (LoadWindowLayout(layoutFilePath, false))
                Debug.Log("Loaded layout from " + layoutFilePath);
        }

        public static void SaveToFile()
        {
            var layoutFilePath = EditorUtility.SaveFilePanel("Save layout to disk...", "", "layout", "wlt");
            if (String.IsNullOrEmpty(layoutFilePath))
                return;

            SaveWindowLayout(layoutFilePath);
            Debug.Log("Saved layout to " + layoutFilePath);
        }

        public static void DeleteGUI()
        {
            DeleteWindowLayout.Show(FindMainView().screenPosition);
        }

        public static void RevertFactorySettings(bool quitOnCancel = true)
        {
            if (!EditorUtility.DisplayDialog("Revert All Window Layouts",
                "Unity is about to delete all window layout and restore them to the default settings",
                "Continue", quitOnCancel ? "Quit" : "Cancel"))
            {
                if (quitOnCancel)
                    EditorApplication.Exit(0);
                return;
            }

            FileUtil.DeleteFileOrDirectory(layoutsPreferencesPath);
            FileUtil.DeleteFileOrDirectory(ProjectLayoutPath);
            FileUtil.DeleteFileOrDirectory(GetProjectLayoutPerMode("default"));
            ModeService.ChangeModeById("default");

            LoadCurrentModeLayout(true);
            ReloadWindowLayoutMenu();
            EditorUtility.Internal_UpdateAllMenus();
            ShortcutIntegration.instance.RebuildShortcuts();
        }
    }

    [EditorWindowTitle(title = "Save Layout")]
    internal class SaveWindowLayout : EditorWindow
    {
        bool m_DidFocus;
        const int k_Offset = 20;
        const int k_Width = 200;
        const int k_Height = 48;
        const int k_HelpBoxHeight = 40;

        static readonly ReadOnlyCollection<char> k_InvalidChars = new ReadOnlyCollection<char>(Path.GetInvalidFileNameChars());
        static StringBuilder s_CurrentInvalidChars = new StringBuilder(k_InvalidChars.Count);
        static string s_InvalidCharsFormatString = L10n.Tr("Invalid characters: {0}");
        static string s_LayoutName = Toolbar.lastLoadedLayoutName;

        internal static SaveWindowLayout Show(Rect r)
        {
            SaveWindowLayout w = GetWindowWithRect<SaveWindowLayout>(new Rect(r.xMax - (k_Width - k_Offset), r.y + k_Offset, k_Width, k_Height), true, L10n.Tr("Save Layout"));
            w.m_Parent.window.m_DontSaveToLayout = true;
            return w;
        }

        private static void UpdateCurrentInvalidChars()
        {
            s_CurrentInvalidChars.Clear();
            // This approach will get the invalid characters in the layout name in they order they appear.
            // This approach would help locate invalid characters faster (in theory) and makes more sense to display them this way if a few unique characters were being typed in a row.

            // We loop through the characters in the name of the layout.
            for (int i = 0; i < s_LayoutName.Length; ++i)
            {
                bool wasAdded = false;
                bool isInvalidChr = false;

                // We loop through the invalid characters, trying to see if the current character in the layout name is invalid.
                for (int j = 0; j < k_InvalidChars.Count && !isInvalidChr; ++j)
                {
                    if (s_LayoutName[i] == k_InvalidChars[j])
                    {
                        isInvalidChr = true;

                        // We loop through the invalid characters to see if the current invalid character was already added.
                        for (int k = 0; k < s_CurrentInvalidChars.Length && !wasAdded; ++k)
                        {
                            if (s_CurrentInvalidChars[k] == k_InvalidChars[j])
                            {
                                wasAdded = true;
                            }
                        }
                    }
                }

                if (!wasAdded && isInvalidChr)
                {
                    s_CurrentInvalidChars.Append(s_LayoutName[i]);
                }
            }
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            GUI.SetNextControlName("m_PreferencesName");
            EditorGUI.BeginChangeCheck();
            s_LayoutName = EditorGUILayout.TextField(s_LayoutName);
            s_LayoutName = s_LayoutName.TrimEnd();
            if (EditorGUI.EndChangeCheck())
            {
                UpdateCurrentInvalidChars();
            }

            if (!m_DidFocus)
            {
                m_DidFocus = true;
                EditorGUI.FocusTextInControl("m_PreferencesName");
            }

            if (s_CurrentInvalidChars.Length != 0)
            {
                EditorGUILayout.HelpBox(string.Format(s_InvalidCharsFormatString, s_CurrentInvalidChars), MessageType.Warning);
                minSize = new Vector2(k_Width, k_Height + k_HelpBoxHeight);
            }
            else
            {
                minSize = new Vector2(k_Width, k_Height);
            }

            bool canSaveLayout = s_LayoutName.Length > 0 && s_CurrentInvalidChars.Length == 0;
            EditorGUI.BeginDisabled(!canSaveLayout);

            if (GUILayout.Button("Save") || hitEnter && canSaveLayout)
            {
                Close();

                if (!Directory.Exists(WindowLayout.layoutsModePreferencesPath))
                    Directory.CreateDirectory(WindowLayout.layoutsModePreferencesPath);

                string path = Path.Combine(WindowLayout.layoutsModePreferencesPath, s_LayoutName + ".wlt");
                Toolbar.lastLoadedLayoutName = s_LayoutName;
                WindowLayout.SaveWindowLayout(path);
                WindowLayout.ReloadWindowLayoutMenu();
                EditorUtility.Internal_UpdateAllMenus();
                ShortcutIntegration.instance.RebuildShortcuts();
                GUIUtility.ExitGUI();
            }
            else
            {
                m_DidFocus = false;
            }

            EditorGUI.EndDisabled();
        }
    }

    [EditorWindowTitle(title = "Delete Layout")]
    internal class DeleteWindowLayout : EditorWindow
    {
        internal string[] m_Paths;
        const int k_MaxLayoutNameLength = 15;
        const int k_Offset = 20;
        const int k_Width = 200;
        const int k_Height = 175;
        Vector2 m_ScrollPos;

        internal static DeleteWindowLayout Show(Rect r)
        {
            DeleteWindowLayout w = GetWindowWithRect<DeleteWindowLayout>(new Rect(r.xMax - (k_Width - k_Offset), r.y + k_Offset, k_Width, k_Height), true, L10n.Tr("Delete Layout"));
            w.m_Parent.window.m_DontSaveToLayout = true;
            return w;
        }

        private void InitializePaths()
        {
            string[] allPaths = Directory.GetFiles(WindowLayout.layoutsModePreferencesPath);
            ArrayList filteredFiles = new ArrayList();
            foreach (string path in allPaths)
            {
                string name = Path.GetFileName(path);
                if (Path.GetExtension(name) == ".wlt")
                    filteredFiles.Add(path);
            }

            m_Paths = filteredFiles.ToArray(typeof(string)) as string[];
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
        }

        void OnGUI()
        {
            if (m_Paths == null)
                InitializePaths();
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            foreach (string path in m_Paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.Length > k_MaxLayoutNameLength)
                    name = name.Substring(0, k_MaxLayoutNameLength) + "...";
                if (GUILayout.Button(name))
                {
                    if (Toolbar.lastLoadedLayoutName == name)
                        Toolbar.lastLoadedLayoutName = null;

                    File.Delete(path);
                    WindowLayout.ReloadWindowLayoutMenu();
                    EditorUtility.Internal_UpdateAllMenus();
                    ShortcutIntegration.instance.RebuildShortcuts();
                    InitializePaths();
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    internal static class CreateBuiltinWindows
    {
        [MenuItem("Window/General/Scene %1", false, 1)]
        static void ShowSceneView()
        {
            EditorWindow.GetWindow<SceneView>();
        }

        [MenuItem("Window/General/Game %2", false, 2)]
        static void ShowGameView()
        {
            EditorWindow.GetWindow<GameView>();
        }

        [MenuItem("Window/General/Inspector %3", false, 3)]
        static void ShowInspector()
        {
            EditorWindow.GetWindow<InspectorWindow>();
        }

        [MenuItem("Window/General/Hierarchy %4", false, 4)]
        static void ShowNewHierarchy()
        {
            EditorWindow.GetWindow<SceneHierarchyWindow>();
        }

        [MenuItem("Window/General/Project %5", false, 5)]
        static void ShowProject()
        {
            EditorWindow.GetWindow<ProjectBrowser>();
        }

        [MenuItem("Window/Animation/Animation %6", false, 1)]
        static void ShowAnimationWindow()
        {
            EditorWindow.GetWindow<AnimationWindow>();
        }

        [MenuItem("Window/Audio/Audio Mixer %8", false, 1)]
        static void ShowAudioMixer()
        {
            AudioMixerWindow.CreateAudioMixerWindow();
        }

        // Version Control is registered from native code (EditorWindowController.cpp), for license check
        // [MenuItem ("Window/Version Control", false, 2010)]
        static void ShowVersionControl()
        {
            EditorWindow.GetWindow<WindowPending>();
        }

        [MenuItem("Window/2D/Sprite Packer", false, 1)]
        static void ShowSpritePackerWindow()
        {
            EditorWindow.GetWindow<Sprites.PackerWindow>();
        }

        [MenuItem("Window/General/Console %#c", false, 6)]
        static void ShowConsole()
        {
            EditorWindow.GetWindow<ConsoleWindow>();
        }
    }

    internal class WindowFocusState : ScriptableObject
    {
        private static WindowFocusState m_Instance;

        internal string m_LastWindowTypeInSameDock = "";
        internal bool m_WasMaximizedBeforePlay = false;
        internal bool m_CurrentlyInPlayMode = false;

        internal static WindowFocusState instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindObjectOfType(typeof(WindowFocusState)) as WindowFocusState;
                if (m_Instance == null)
                    m_Instance = CreateInstance<WindowFocusState>();
                return m_Instance;
            }
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            m_Instance = this;
        }
    }
} // namespace
