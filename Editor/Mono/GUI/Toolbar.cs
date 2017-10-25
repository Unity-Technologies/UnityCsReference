// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Collaboration;
using UnityEditor.Web;

namespace UnityEditor
{
    class Toolbar : GUIView
    {
        void InitializeToolIcons()
        {
            if (s_ToolIcons != null)
                return;

            s_ToolIcons = new GUIContent[]
            {
                EditorGUIUtility.IconContent("MoveTool", "|Move Tool"),
                EditorGUIUtility.IconContent("RotateTool", "|Rotate Tool"),
                EditorGUIUtility.IconContent("ScaleTool", "|Scale Tool"),
                EditorGUIUtility.IconContent("RectTool", "|Rect Tool"),
                EditorGUIUtility.IconContent("TransformTool", "|Move, Rotate or Scale selected objects."),
                EditorGUIUtility.IconContent("MoveTool On"),
                EditorGUIUtility.IconContent("RotateTool On"),
                EditorGUIUtility.IconContent("ScaleTool On"),
                EditorGUIUtility.IconContent("RectTool On"),
                EditorGUIUtility.IconContent("TransformTool On"),
            };

            string viewToolsTooltipText = "|Hand Tool";

            s_ViewToolIcons = new GUIContent[]
            {
                EditorGUIUtility.IconContent("ViewToolOrbit", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolMove", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolZoom", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolOrbit", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolOrbit", "|Orbit the Scene view."),
                EditorGUIUtility.IconContent("ViewToolOrbit On", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolMove On", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolZoom On", viewToolsTooltipText),
                EditorGUIUtility.IconContent("ViewToolOrbit On"),
                EditorGUIUtility.IconContent("ViewToolOrbit On", viewToolsTooltipText)
            };

            s_PivotIcons = new GUIContent[]
            {
                EditorGUIUtility.TextContentWithIcon("Center|Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.", "ToolHandleCenter"),
                EditorGUIUtility.TextContentWithIcon("Pivot|Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.", "ToolHandlePivot"),
            };

            s_PivotRotation = new GUIContent[]
            {
                EditorGUIUtility.TextContentWithIcon("Local|Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.", "ToolHandleLocal"),
                EditorGUIUtility.TextContentWithIcon("Global|Toggle Tool Handle Rotation\n\nTool handles are in global rotation.", "ToolHandleGlobal")
            };

            s_LayerContent = EditorGUIUtility.TextContent("Layers|Which layers are visible in the Scene views.");

            s_PlayIcons = new GUIContent[]
            {
                EditorGUIUtility.IconContent("PlayButton", "|Play"),
                EditorGUIUtility.IconContent("PauseButton", "|Pause"),
                EditorGUIUtility.IconContent("StepButton", "|Step"),
                EditorGUIUtility.IconContent("PlayButtonProfile", "|Profiler Play"),
                EditorGUIUtility.IconContent("PlayButton On"),
                EditorGUIUtility.IconContent("PauseButton On"),
                EditorGUIUtility.IconContent("StepButton On"),
                EditorGUIUtility.IconContent("PlayButtonProfile On")
            };

            s_CloudIcon = EditorGUIUtility.IconContent("CloudConnect");
            s_AccountContent = new GUIContent("Account");


            // Must match enum CollabToolbarState
            s_CollabIcons = new GUIContent[]
            {
                EditorGUIUtility.TextContentWithIcon("Collab| You need to enable collab.", "CollabNew"),
                EditorGUIUtility.TextContentWithIcon("Collab| You are up to date.", "Collab"),
                EditorGUIUtility.TextContentWithIcon("Collab| Please fix your conflicts prior to publishing.", "CollabConflict"),
                EditorGUIUtility.TextContentWithIcon("Collab| Last operation failed. Please retry later.", "CollabError"),
                EditorGUIUtility.TextContentWithIcon("Collab| Please update, there are server changes.", "CollabPull"),
                EditorGUIUtility.TextContentWithIcon("Collab| You have files to publish.", "CollabPush"),
                EditorGUIUtility.TextContentWithIcon("Collab| Operation in progress.", "CollabProgress"),
                EditorGUIUtility.TextContentWithIcon("Collab| Collab is disabled.", "CollabNew"),
                EditorGUIUtility.TextContentWithIcon("Collab| Please check your network connection.", "CollabNew")
            };
        }

        static GUIContent[] s_ToolIcons;
        static readonly string[] s_ToolControlNames = new[]
        {
            "ToolbarPersistentToolsPan",
            "ToolbarPersistentToolsTranslate",
            "ToolbarPersistentToolsRotate",
            "ToolbarPersistentToolsScale",
            "ToolbarPersistentToolsRect",
            "ToolbarPersistentToolsTransform",
        };
        static GUIContent[] s_ViewToolIcons;
        static GUIContent[] s_PivotIcons;
        static GUIContent[] s_PivotRotation;
        static GUIContent   s_LayerContent;
        static GUIContent[] s_PlayIcons;
        private static GUIContent s_AccountContent;
        static GUIContent   s_CloudIcon;
        // Must match s_CollabIcon array
        enum CollabToolbarState
        {
            NeedToEnableCollab,
            UpToDate,
            Conflict,
            OperationError,
            ServerHasChanges,
            FilesToPush,
            InProgress,
            Disabled,
            Offline
        }

        CollabToolbarState m_CollabToolbarState = CollabToolbarState.UpToDate;
        static GUIContent[] s_CollabIcons;
        const float kCollabButtonWidth = 78.0f;
        ButtonWithAnimatedIconRotation m_CollabButton;
        string m_DynamicTooltip;
        static bool m_ShowCollabTooltip = false;

        GUIContent currentCollabContent
        {
            get
            {
                GUIContent content = new GUIContent(s_CollabIcons[(int)m_CollabToolbarState]);
                if (!m_ShowCollabTooltip)
                {
                    content.tooltip = null;
                }
                else if (m_DynamicTooltip != "")
                {
                    content.tooltip = m_DynamicTooltip;
                }

                if (Collab.instance.AreTestsRunning())
                {
                    content.text = "CTF";
                }

                return content;
            }
        }

        static class Styles
        {
            public readonly static GUIStyle collabButtonStyle = new GUIStyle("Dropdown") { padding = { left = 24 } }; // move text to make room for rotating icon
            public static readonly GUIStyle dropdown = "Dropdown";
            public static readonly GUIStyle appToolbar = "AppToolbar";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Disable clipping on the root element to fix case 931831
            visualTree.clippingOptions = VisualElement.ClippingOptions.NoClipping;
            EditorApplication.modifierKeysChanged += Repaint;
            // when undo or redo is done, we need to reset global tools rotation
            Undo.undoRedoPerformed += OnSelectionChange;

            UnityConnect.instance.StateChanged += OnUnityConnectStateChanged;
            UnityConnect.instance.UserStateChanged += OnUnityConnectUserStateChanged;

            get = this;
            Collab.instance.StateChanged += OnCollabStateChanged;

            if (m_CollabButton == null)
            {
                const int repaintsPerSecond = 20;
                const float animSpeed = 500f;
                const bool mouseDownButton = true;
                m_CollabButton = new ButtonWithAnimatedIconRotation(() => (float)EditorApplication.timeSinceStartup * animSpeed, Repaint, repaintsPerSecond, mouseDownButton);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.modifierKeysChanged -= Repaint;
            Undo.undoRedoPerformed -= OnSelectionChange;
            UnityConnect.instance.StateChanged -= OnUnityConnectStateChanged;
            UnityConnect.instance.UserStateChanged -= OnUnityConnectUserStateChanged;

            Collab.instance.StateChanged -= OnCollabStateChanged;

            if (m_CollabButton != null)
                m_CollabButton.Clear();
        }

        // The actual array we display. We build this every frame to make sure it looks correct i.r.t. selection :)
        static GUIContent[] s_ShownToolIcons = { null, null, null, null, null, null };

        public static Toolbar get = null;
        public static bool requestShowCollabToolbar = false;

        internal static string lastLoadedLayoutName
        {
            get
            {
                return string.IsNullOrEmpty(get.m_LastLoadedLayoutName) ? "Layout" : get.m_LastLoadedLayoutName;
            }
            set
            {
                get.m_LastLoadedLayoutName = value;
                get.Repaint();
            }
        }
        [SerializeField]
        private string m_LastLoadedLayoutName;

        override protected bool OnFocus()
        {
            return false;
        }

        void OnSelectionChange()
        {
            Tools.OnSelectionChange();
            Repaint();
        }


        protected void OnUnityConnectStateChanged(ConnectInfo state)
        {
            UpdateCollabToolbarState();
            RepaintToolbar();
        }

        protected void OnUnityConnectUserStateChanged(UserInfo state)
        {
            UpdateCollabToolbarState();
        }


        Rect GetThinArea(Rect pos)
        {
            return new Rect(pos.x, 7, pos.width, 18);
        }

        Rect GetThickArea(Rect pos)
        {
            return new Rect(pos.x, 5, pos.width, 24);
        }

        void ReserveWidthLeft(float width, ref Rect pos)
        {
            pos.x -= width;
            pos.width = width;
        }

        void ReserveWidthRight(float width, ref Rect pos)
        {
            pos.x += pos.width;
            pos.width = width;
        }

        void ReserveRight(float width, ref Rect pos)
        {
            pos.x += width;
        }

        void ReserveBottom(float height, ref Rect pos)
        {
            pos.y += height;
        }

        protected override void OldOnGUI()
        {
            float space = 10;
            float largeSpace = 20;
            float standardButtonWidth = 32;
            float pivotButtonWidth = 64;
            float dropdownWidth = 80;

            InitializeToolIcons();

            bool isOrWillEnterPlaymode = EditorApplication.isPlayingOrWillChangePlaymode;
            if (isOrWillEnterPlaymode)
                GUI.color = HostView.kPlayModeDarken;

            if (Event.current.type == EventType.Repaint)
                Styles.appToolbar.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            // Position left aligned controls controls - start from left to right.
            Rect pos = new Rect(0, 0, 0, 0);

            ReserveWidthRight(space, ref pos);

            ReserveWidthRight(standardButtonWidth * s_ShownToolIcons.Length, ref pos);
            DoToolButtons(GetThickArea(pos));

            ReserveWidthRight(largeSpace, ref pos);

            ReserveWidthRight(pivotButtonWidth * 2, ref pos);
            DoPivotButtons(GetThinArea(pos));

            // Position centered controls.
            float playPauseStopWidth = 100;
            pos = new Rect(Mathf.RoundToInt((position.width - playPauseStopWidth) / 2), 0, 140, 0);

            GUILayout.BeginArea(GetThickArea(pos));
            GUILayout.BeginHorizontal();
            {
                DoPlayButtons(isOrWillEnterPlaymode);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Position right aligned controls controls - start from right to left.
            pos = new Rect(position.width, 0, 0, 0);

            // Right spacing side
            ReserveWidthLeft(space, ref pos);

            ReserveWidthLeft(dropdownWidth, ref pos);
            DoLayoutDropDown(GetThinArea(pos));

            ReserveWidthLeft(space, ref pos);

            ReserveWidthLeft(dropdownWidth, ref pos);
            DoLayersDropDown(GetThinArea(pos));

            ReserveWidthLeft(largeSpace, ref pos);

            ReserveWidthLeft(dropdownWidth, ref pos);
            if (EditorGUI.DropdownButton(GetThinArea(pos), s_AccountContent, FocusType.Passive, Styles.dropdown))
            {
                ShowUserMenu(GetThinArea(pos));
            }


            ReserveWidthLeft(space, ref pos);

            ReserveWidthLeft(32, ref pos);
            if (GUI.Button(GetThinArea(pos), s_CloudIcon))
                UnityConnectServiceCollection.instance.ShowService(HubAccess.kServiceName, true, "cloud_icon"); // Should show hub when it's done

            ReserveWidthLeft(space, ref pos);
            ReserveWidthLeft(kCollabButtonWidth, ref pos);
            DoCollabDropDown(GetThinArea(pos));


            EditorGUI.ShowRepaints();
            Highlighter.ControlHighlightGUI(this);
        }

        void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            if (!UnityConnect.instance.online)
            {
                menu.AddDisabledItem(new GUIContent("Go to account"));
                menu.AddDisabledItem(new GUIContent("Sign in..."));

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("Upgrade to Pro"));
                }
            }
            else
            {
                string accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPortal);
                if (UnityConnect.instance.loggedIn)
                    menu.AddItem(new GUIContent("Go to account"), false, () => UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl));
                else
                    menu.AddDisabledItem(new GUIContent("Go to account"));

                if (UnityConnect.instance.loggedIn)
                {
                    string name = "Sign out " + UnityConnect.instance.userInfo.displayName;
                    menu.AddItem(new GUIContent(name), false, () => {UnityConnect.instance.Logout(); });
                }
                else
                    menu.AddItem(new GUIContent("Sign in..."), false, () => {UnityConnect.instance.ShowLogin(); });

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Upgrade to Pro"), false, () => Application.OpenURL("https://store.unity3d.com/"));
                }
            }

            menu.DropDown(dropDownRect);
        }


        void DoToolButtons(Rect rect)
        {
            // Handle temporary override with ALT
            GUI.changed = false;

            int displayTool = Tools.viewToolActive ? 0 : (int)Tools.current;

            // Change the icon to match the correct view tool
            for (int i = 1; i < s_ShownToolIcons.Length; i++)
            {
                s_ShownToolIcons[i] = s_ToolIcons[i - 1 + (i == displayTool ? s_ShownToolIcons.Length - 1 : 0)];
                s_ShownToolIcons[i].tooltip = s_ToolIcons[i - 1].tooltip;
            }
            s_ShownToolIcons[0] = s_ViewToolIcons[(int)Tools.viewTool + (displayTool == 0 ? s_ShownToolIcons.Length - 1 : 0)];

            displayTool = GUI.Toolbar(rect, displayTool, s_ShownToolIcons, s_ToolControlNames, "Command", GUI.ToolbarButtonSize.FitToContents);
            if (GUI.changed)
            {
                Tools.current = (Tool)displayTool;
                Tools.ResetGlobalHandleRotation();
            }
        }

        void DoPivotButtons(Rect rect)
        {
            GUI.SetNextControlName("ToolbarToolPivotPositionButton");
            Tools.pivotMode = (PivotMode)EditorGUI.CycleButton(new Rect(rect.x, rect.y, rect.width / 2, rect.height), (int)Tools.pivotMode, s_PivotIcons, "ButtonLeft");
            if (Tools.current == Tool.Scale && Selection.transforms.Length < 2)
                GUI.enabled = false;
            GUI.SetNextControlName("ToolbarToolPivotOrientationButton");
            PivotRotation tempPivot = (PivotRotation)EditorGUI.CycleButton(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), (int)Tools.pivotRotation, s_PivotRotation, "ButtonRight");
            if (Tools.pivotRotation != tempPivot)
            {
                Tools.pivotRotation = tempPivot;
                if (tempPivot == PivotRotation.Global)
                    Tools.ResetGlobalHandleRotation();
            }

            if (Tools.current == Tool.Scale)
                GUI.enabled = true;

            if (GUI.changed)
                Tools.RepaintAllToolViews();
        }

        void DoPlayButtons(bool isOrWillEnterPlaymode)
        {
            // Enter / Exit Playmode
            bool isPlaying = EditorApplication.isPlaying;
            GUI.changed = false;

            int buttonOffset = isPlaying ? 4 : 0;

            Color c = GUI.color + new Color(.01f, .01f, .01f, .01f);
            GUI.contentColor = new Color(1.0f / c.r, 1.0f / c.g, 1.0f / c.g, 1.0f / c.a);
            GUI.SetNextControlName("ToolbarPlayModePlayButton");
            GUILayout.Toggle(isOrWillEnterPlaymode, s_PlayIcons[buttonOffset], "CommandLeft");
            GUI.backgroundColor = Color.white;
            if (GUI.changed)
            {
                TogglePlaying();
                GUIUtility.ExitGUI();
            }

            // Pause game
            GUI.changed = false;

            GUI.SetNextControlName("ToolbarPlayModePauseButton");
            bool isPaused = GUILayout.Toggle(EditorApplication.isPaused, s_PlayIcons[buttonOffset + 1], "CommandMid");
            if (GUI.changed)
            {
                EditorApplication.isPaused = isPaused;
                GUIUtility.ExitGUI();
            }

            // Step playmode
            GUI.SetNextControlName("ToolbarPlayModeStepButton");
            if (GUILayout.Button(s_PlayIcons[buttonOffset + 2], "CommandRight"))
            {
                EditorApplication.Step();
                GUIUtility.ExitGUI();
            }

        }

        void DoLayersDropDown(Rect rect)
        {
            GUIStyle dropStyle = "DropDown";
            if (EditorGUI.DropdownButton(rect, s_LayerContent, FocusType.Passive, dropStyle))
            {
                if (LayerVisibilityWindow.ShowAtPosition(rect))
                {
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DoLayoutDropDown(Rect rect)
        {
            // Layout DropDown
            if (EditorGUI.DropdownButton(rect, GUIContent.Temp(lastLoadedLayoutName), FocusType.Passive, "DropDown"))
            {
                Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                rect.x = temp.x;
                rect.y = temp.y;
                EditorUtility.Internal_DisplayPopupMenu(rect, "Window/Layouts", this, 0);
            }
        }

        void ShowPopup(Rect rect)
        {
            // window should be centered on the button
            ReserveRight(kCollabButtonWidth / 2, ref rect);
            ReserveBottom(5, ref rect);
            // calculate screen rect before saving assets since it might open the AssetSaveDialog window
            var screenRect = GUIUtility.GUIToScreenRect(rect);
            // save all the assets
            AssetDatabase.SaveAssets();
            if (CollabToolbarWindow.ShowCenteredAtPosition(screenRect))
            {
                GUIUtility.ExitGUI();
            }
        }

        void DoCollabDropDown(Rect rect)
        {
            UpdateCollabToolbarState();
            bool showPopup = requestShowCollabToolbar;
            requestShowCollabToolbar = false;

            bool enable = !EditorApplication.isPlaying;

            using (new EditorGUI.DisabledScope(!enable))
            {
                bool animate = m_CollabToolbarState == CollabToolbarState.InProgress;

                EditorGUIUtility.SetIconSize(new Vector2(12, 12));
                if (m_CollabButton.OnGUI(rect, currentCollabContent, animate, Styles.collabButtonStyle))
                {
                    showPopup = true;
                }
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }

            if (showPopup)
            {
                ShowPopup(rect);
            }
        }

        public void OnCollabStateChanged(CollabInfo info)
        {
            UpdateCollabToolbarState();
        }

        public void UpdateCollabToolbarState()
        {
            var currentCollabState = CollabToolbarState.UpToDate;
            bool networkAvailable = UnityConnect.instance.connectInfo.online && UnityConnect.instance.connectInfo.loggedIn;
            m_DynamicTooltip = "";

            if (networkAvailable)
            {
                Collab collab = Collab.instance;
                CollabInfo currentInfo = collab.collabInfo;
                int errorCode = 0;
                int errorPriority = (int)UnityConnect.UnityErrorPriority.None;
                int errorBehaviour = (int)UnityConnect.UnityErrorBehaviour.Hidden;
                string errorMsg = "";
                string errorShortMsg = "";
                bool error = false;
                if (collab.GetError((int)(UnityConnect.UnityErrorFilter.ByContext | UnityConnect.UnityErrorFilter.ByChild), out errorCode, out errorPriority, out errorBehaviour, out errorMsg, out errorShortMsg))
                {
                    error = (errorPriority <= (int)UnityConnect.UnityErrorPriority.Error);
                    m_DynamicTooltip = errorShortMsg;
                }

                if (!currentInfo.ready)
                {
                    currentCollabState = CollabToolbarState.InProgress;
                }
                else if (error)
                {
                    currentCollabState = CollabToolbarState.OperationError;
                }
                else if (currentInfo.inProgress)
                {
                    currentCollabState = CollabToolbarState.InProgress;
                }
                else
                {
                    bool collabEnable = Collab.instance.IsCollabEnabledForCurrentProject();

                    if (UnityConnect.instance.projectInfo.projectBound == false || !collabEnable)
                    {
                        currentCollabState = CollabToolbarState.NeedToEnableCollab;
                    }
                    else if (currentInfo.update)
                    {
                        currentCollabState = CollabToolbarState.ServerHasChanges;
                    }
                    else if (currentInfo.conflict)
                    {
                        currentCollabState = CollabToolbarState.Conflict;
                    }
                    else if (currentInfo.publish)
                    {
                        currentCollabState = CollabToolbarState.FilesToPush;
                    }
                }
            }
            else
            {
                currentCollabState = CollabToolbarState.Offline;
            }

            if (currentCollabState != m_CollabToolbarState ||
                CollabToolbarWindow.s_ToolbarIsVisible == m_ShowCollabTooltip)
            {
                m_CollabToolbarState = currentCollabState;
                m_ShowCollabTooltip = !CollabToolbarWindow.s_ToolbarIsVisible;
                RepaintToolbar();
            }
        }


        // Repaints all views, called from C++ when playmode entering is aborted
        // and when the user clicks on the playmode button.
        static void InternalWillTogglePlaymode()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        static void TogglePlaying()
        {
            bool willPlay = !EditorApplication.isPlaying;
            EditorApplication.isPlaying = willPlay;

            InternalWillTogglePlaymode();
        }

        static internal void RepaintToolbar()
        {
            if (get != null)
                get.Repaint();
        }

        public float CalcHeight()
        {
            return 30;
        }
    }
} // namespace
