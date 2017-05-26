// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditorInternal;
using UnityEditor.VersionControl;
using UnityEditor.Connect;
using UnityEditor.Web;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    internal class WindowLayout
    {
        static internal PrefKey s_MaximizeKey = new PrefKey("Window/Maximize View", "# ");

        private const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";

        /// Used by EditorWindowController to display the window
        static void ShowWindowImmediate(EditorWindow win)
        {
            win.Show(true);
        }

        static internal EditorWindow FindEditorWindowOfType(System.Type type)
        {
            UnityObject[] obj = Resources.FindObjectsOfTypeAll(type);
            if (obj.Length > 0)
                return obj[0] as EditorWindow;
            else
                return null;
        }

        static IEnumerable<T> FindEditorWindowsOfType<T>() where T : class
        {
            foreach (UnityObject obj in Resources.FindObjectsOfTypeAll(typeof(T)))
                if (obj is T)
                    yield return obj as T;
        }

        static internal void CheckWindowConsistency()
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

        static internal EditorWindow TryGetLastFocusedWindowInSameDock()
        {
            // Get type of window that was docked together with game view and was focused before play mode
            System.Type type = null;
            string windowTypeName = WindowFocusState.instance.m_LastWindowTypeInSameDock;
            if (windowTypeName != "")
                type = System.Type.GetType(windowTypeName);

            // Also get the GameView
            GameView gameView = FindEditorWindowOfType(typeof(GameView)) as GameView;
            if (type != null && gameView && gameView.m_Parent != null && gameView.m_Parent is DockArea)
            {
                // Get all windows of that type
                object[] potentials = Resources.FindObjectsOfTypeAll(type);

                DockArea dock = gameView.m_Parent as DockArea;

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

        static internal void SaveCurrentFocusedWindowInSameDock(EditorWindow windowToBeFocused)
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

        static internal void FindFirstGameViewAndSetToMaximizeOnPlay()
        {
            GameView gameView = (GameView)FindEditorWindowOfType(typeof(GameView));
            if (gameView)
                gameView.maximizeOnPlay = true;
        }

        static internal EditorWindow TryFocusAppropriateWindow(bool enteringPlaymode)
        {
            if (enteringPlaymode)
            {
                GameView gameView = (GameView)FindEditorWindowOfType(typeof(GameView));
                if (gameView)
                {
                    SaveCurrentFocusedWindowInSameDock(gameView);
                    gameView.Focus();
                }
                return gameView;
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

        static internal EditorWindow GetMaximizedWindow()
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

        static internal EditorWindow ShowAppropriateViewOnEnterExitPlaymode(bool entering)
        {
            // Prevent trying to go into the same state as we're already in, as it wil break things
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

        static internal bool IsMaximized(EditorWindow window)
        {
            return window.m_Parent is MaximizedHostView;
        }

        static internal void MaximizeKeyHandler()
        {
            if ((s_MaximizeKey.activated || Event.current.type == EditorGUIUtility.magnifyGestureEventType) && GUIUtility.hotControl == 0)
            {
                EventType type = Event.current.type;
                Event.current.Use();
                EditorWindow mouseOver = EditorWindow.mouseOverWindow;
                if (mouseOver)
                {
                    if (!(mouseOver is PreviewWindow))
                    {
                        if (type == EditorGUIUtility.magnifyGestureEventType)
                        {
                            if (Event.current.delta.x < -0.05)
                            {
                                if (IsMaximized(mouseOver))
                                    Unmaximize(mouseOver);
                            }
                            else if (Event.current.delta.x > 0.05)
                            {
                                if (!IsMaximized(mouseOver))
                                    Maximize(mouseOver);
                            }
                        }
                        else
                        {
                            if (IsMaximized(mouseOver))
                                Unmaximize(mouseOver);
                            else
                                Maximize(mouseOver);
                        }
                    }
                }
            }
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
                    throw new System.Exception();
                }

                // Kill the maximizedMainView
                UnityObject.DestroyImmediate(maximizedHostView);

                win.Focus();

                parentWindow.DisplayAllViews();
                win.m_Parent.MakeVistaDWMHappyDance();
            }
            catch (System.Exception ex)
            {
                Debug.Log("Maximization failed: " + ex);
                RevertFactorySettings();
            }

            try
            {
                // Weird bug on AMD graphic cards under OSX Lion: Sometimes when unmaximizing we get stray white rectangles.
                // work around that by issueing an extra repaint (case 438764)
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

        public static void AddSplitViewAndChildrenRecurse(View splitview, ArrayList list)
        {
            list.Add(splitview);
            DockArea dock = splitview as DockArea;
            if (dock != null)
                list.AddRange(dock.m_Panes);

            HostView host = splitview as DockArea;
            if (host != null)
                list.Add(dock.actualView);

            foreach (View child in splitview.children)
            {
                AddSplitViewAndChildrenRecurse(child, list);
            }
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
        }

        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated)
        {
            Rect mainWindowPosition = new Rect();
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode == ShowMode.MainWindow)
                {
                    mainWindowPosition = window.position;
                }
            }

            bool layoutLoadingIssue = false;

            // Load new windows and show them
            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                CloseWindows();

                // Load data
                UnityObject[] loadedWindows = InternalEditorUtility.LoadSerializedFileAndForget(path);
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
                                o.GetType().ToString() + ", instanceID=" + o.GetInstanceID());
                            layoutLoadingIssue = true;
                            continue;
                        }
                    }
                    else
                    {
                        DockArea dockArea = o as DockArea;
                        if (dockArea != null && dockArea.m_Panes.Count == 0)
                        {
                            dockArea.Close(null);
                            Console.WriteLine("LoadWindowLayout: Removed empty DockArea while reading window layout: window #" + i + ", instanceID=" +
                                o.GetInstanceID());
                            layoutLoadingIssue = true;
                            continue;
                        }
                    }

                    newWindows.Add(o);
                }

                ContainerWindow mainWindowToSetSize = null;
                ContainerWindow mainWindow = null;

                for (int i = 0; i < newWindows.Count; i++)
                {
                    ContainerWindow cur = newWindows[i] as ContainerWindow;
                    if (cur != null && cur.showMode == ShowMode.MainWindow)
                    {
                        mainWindow = cur;
                        if (mainWindowPosition.width != 0.0)
                        {
                            mainWindowToSetSize = cur;
                            mainWindowToSetSize.position = mainWindowPosition;
                        }
                    }
                }

                for (int i = 0; i < newWindows.Count; i++)
                {
                    UnityObject o = newWindows[i];
                    if (o == null)
                    {
                        Console.WriteLine("LoadWindowLayout: Error while reading window layout: window #" + i + " is null");
                        layoutLoadingIssue = true;
                        // Keep going
                    }
                    else if (o.GetType() == null)
                    {
                        Console.WriteLine("LoadWindowLayout: Error while reading window layout: window #" + i + " type is null, instanceID=" + o.GetInstanceID());
                        layoutLoadingIssue = true;
                        // Keep going
                    }
                    else
                    {
                        if (newProjectLayoutWasCreated)
                        {
                            MethodInfo method = o.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (method != null)
                                method.Invoke(o, null);
                        }
                    }
                }

                if (mainWindowToSetSize)
                {
                    mainWindowToSetSize.position = mainWindowPosition;
                    mainWindowToSetSize.OnResize();
                }

                // Always show main window before other windows. So that other windows can
                // get their parent/owner.
                if (mainWindow == null)
                {
                    Debug.LogError("Error while reading window layout: no main window found");
                    throw new System.Exception();
                }
                mainWindow.Show(mainWindow.showMode, true, true);

                // Show other windows
                for (int i = 0; i < newWindows.Count; i++)
                {
                    EditorWindow win = newWindows[i] as EditorWindow;
                    if (win)
                        win.minSize = win.minSize; // Causes minSize to be propagated upwards to parents!

                    ContainerWindow containerWindow = newWindows[i] as ContainerWindow;
                    if (containerWindow && containerWindow != mainWindow)
                        containerWindow.Show(containerWindow.showMode, true, true);
                }

                // Unmaximize maximized GameView if maximize on play is enabled
                GameView gameView = GetMaximizedWindow() as GameView;
                if (gameView != null && gameView.maximizeOnPlay)
                    Unmaximize(gameView);

                // For new projects, show services window if and only if online and logged in
                if (newProjectLayoutWasCreated)
                {
                    if (UnityConnect.instance.online && UnityConnect.instance.loggedIn && UnityConnect.instance.shouldShowServicesWindow)
                    {
                        UnityConnectServiceCollection.instance.ShowService(HubAccess.kServiceName, true, "new_project_created");
                    }
                    else
                    {
                        UnityConnectServiceCollection.instance.CloseServices();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to load window layout: " + ex);

                int option = 0;
                if (!Application.isTestRun)
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

                if (Path.GetExtension(path) == ".wlt")
                    Toolbar.lastLoadedLayoutName = Path.GetFileNameWithoutExtension(path);
                else
                    Toolbar.lastLoadedLayoutName = null;
            }

            if (layoutLoadingIssue)
                Debug.Log("The editor layout could not be fully loaded, this can happen when the layout contains EditorWindows not available in this project");

            return true;
        }

        private static void LoadDefaultLayout()
        {
            InternalEditorUtility.LoadDefaultLayout();
        }

        public static void CloseWindows()
        {
            try
            {
                // Close any existing tooltips
                TooltipView.Close();
            }
            catch (System.Exception) {}

            // Close all container windows
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                try
                {
                    window.Close();
                }
                catch (System.Exception) {}
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
                string output = "";
                foreach (View killme in oldViews)
                {
                    output += "\n" + killme.GetType().Name;
                    UnityObject.DestroyImmediate(killme, true);
                }
                Debug.LogError("Failed to destroy views: #" + oldViews.Length + output);
            }
        }

        public static void SaveWindowLayout(string path)
        {
            TooltipView.Close();

            ArrayList all = new ArrayList();
            UnityObject[] windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            UnityObject[] views = Resources.FindObjectsOfTypeAll(typeof(View));

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

            InternalEditorUtility.SaveToSerializedFileAndForget(all.ToArray(typeof(UnityObject)) as UnityObject[], path, true);
        }

        public static void EnsureMainWindowHasBeenLoaded()
        {
            var mainViews = Resources.FindObjectsOfTypeAll<MainView>();
            if (mainViews.Length == 0)
            {
                MainView.MakeMain();
            }
        }

        internal static MainView FindMainView()
        {
            var mainViews = Resources.FindObjectsOfTypeAll<MainView>();
            if (mainViews.Length == 0)
            {
                Debug.LogError("No Main View found!");
                return null;
            }
            return mainViews[0];
        }

        public static void SaveGUI()
        {
            View mainView = FindMainView();
            Rect rect = mainView.screenPosition;
            SaveWindowLayout w = EditorWindow.GetWindowWithRect<SaveWindowLayout>(new Rect(rect.xMax - 180, rect.y + 20, 200, 48), true, "Save Window Layout");
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        private static void RevertFactorySettings()
        {
            InternalEditorUtility.RevertFactoryLayoutSettings(true);
        }

        internal static string layoutsPreferencesPath
        {
            get { return InternalEditorUtility.unityPreferencesFolder + "/Layouts"; }
        }

        internal static string layoutsProjectPath
        {
            get { return Directory.GetCurrentDirectory() + "/Library"; }
        }

        public static void DeleteGUI()
        {
            View mainView = FindMainView();
            Rect rect = mainView.screenPosition;
            DeleteWindowLayout w = EditorWindow.GetWindowWithRect<DeleteWindowLayout>(new Rect(rect.xMax - 180, rect.y + 20, 200, 150), true, "Delete Window Layout");
            w.m_Parent.window.m_DontSaveToLayout = true;
        }
    }

    [EditorWindowTitle(title = "Save Layout")]
    internal class SaveWindowLayout : EditorWindow
    {
        internal string m_LayoutName = Toolbar.lastLoadedLayoutName;
        internal bool didFocus = false;

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
            m_LayoutName = EditorGUILayout.TextField(m_LayoutName);

            if (!didFocus)
            {
                didFocus = true;
                EditorGUI.FocusTextInControl("m_PreferencesName");
            }

            GUI.enabled = m_LayoutName.Length != 0;
            if (GUILayout.Button("Save") || hitEnter)
            {
                Close();
                string path = Path.Combine(WindowLayout.layoutsPreferencesPath, m_LayoutName + ".wlt");
                Toolbar.lastLoadedLayoutName = m_LayoutName;
                WindowLayout.SaveWindowLayout(path);
                InternalEditorUtility.ReloadWindowLayoutMenu();
                GUIUtility.ExitGUI();
            }
        }
    }

    [EditorWindowTitle(title = "Delete Layout")]
    internal class DeleteWindowLayout : EditorWindow
    {
        internal string[] m_Paths;
        private const int kMaxLayoutNameLength = 15;

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
        }

        private void InitializePaths()
        {
            string[] allPaths = Directory.GetFiles(WindowLayout.layoutsPreferencesPath);
            ArrayList filteredFiles = new ArrayList();
            foreach (string path in allPaths)
            {
                string name = Path.GetFileName(path);
                if (Path.GetExtension(name) == ".wlt")
                    filteredFiles.Add(path);
            }

            m_Paths = filteredFiles.ToArray(typeof(string)) as string[];
        }

        private Vector2 m_ScrollPos;

        void OnGUI()
        {
            if (m_Paths == null)
                InitializePaths();
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            foreach (string path in m_Paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.Length > kMaxLayoutNameLength)
                    name = name.Substring(0, kMaxLayoutNameLength) + "...";
                if (GUILayout.Button(name))
                {
                    if (Toolbar.lastLoadedLayoutName == name)
                        Toolbar.lastLoadedLayoutName = null;

                    System.IO.File.Delete(path);
                    InternalEditorUtility.ReloadWindowLayoutMenu();
                    InitializePaths();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    internal class CreateBuiltinWindows
    {
        [MenuItem("Window/Scene %1", false, 2000)]
        static void ShowSceneView()
        {
            EditorWindow.GetWindow<SceneView>();
        }

        [MenuItem("Window/Game %2", false, 2001)]
        static void ShowGameView()
        {
            EditorWindow.GetWindow<GameView>();
        }

        [MenuItem("Window/Inspector %3", false, 2002)]
        static void ShowInspector()
        {
            EditorWindow.GetWindow<InspectorWindow>();
        }

        [MenuItem("Window/Hierarchy %4", false, 2003)]
        static void ShowNewHierarchy()
        {
            EditorWindow.GetWindow<SceneHierarchyWindow>();
        }

        [MenuItem("Window/Project %5", false, 2004)]
        static void ShowProject()
        {
            EditorWindow.GetWindow<ProjectBrowser>();
        }

        [MenuItem("Window/Animation %6", false, 2006)]
        static void ShowAnimationWindow()
        {
            EditorWindow.GetWindow<AnimationWindow>();
        }

        // Profiler is registered from native code (EditorWindowController.cpp), for license check
        //[MenuItem ("Window/Profiler %7", false, 2007)]
        static void ShowProfilerWindow()
        {
            EditorWindow.GetWindow<ProfilerWindow>();
        }

        [MenuItem("Window/Audio Mixer %8", false, 2008)]
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

        [MenuItem("Window/Sprite Packer", false, 2014)]
        static void ShowSpritePackerWindow()
        {
            EditorWindow.GetWindow<Sprites.PackerWindow>();
        }

        [MenuItem("Window/Console %#c", false, 2200)]
        static void ShowConsole()
        {
            EditorWindow.GetWindow<ConsoleWindow>();
        }

        [MenuItem("Window/Experimental/Look Dev", false, 2015)]
        static void ShowLookDevTool()
        {
            EditorWindow.GetWindow<LookDevView>();
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
                    m_Instance = ScriptableObject.CreateInstance<WindowFocusState>();
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
