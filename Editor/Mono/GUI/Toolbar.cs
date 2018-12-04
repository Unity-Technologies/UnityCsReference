// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Web;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    class Toolbar : GUIView
    {
        // Number of buttons present in the tools toolbar.
        const int k_ToolCount = 7;

        // Count of the transform tools + custom editor tool.
        const int k_TransformToolCount = 6;

        void InitializeToolIcons()
        {
            if (s_ToolIcons != null)
                return;

            s_ToolIcons = new GUIContent[k_TransformToolCount * 2];

            int index = 0;

            s_ToolIcons[index++] = EditorGUIUtility.TrIconContent("MoveTool", "Move Tool");
            s_ToolIcons[index++] = EditorGUIUtility.TrIconContent("RotateTool", "Rotate Tool");
            s_ToolIcons[index++] = EditorGUIUtility.TrIconContent("ScaleTool", "Scale Tool");
            s_ToolIcons[index++] = EditorGUIUtility.TrIconContent("RectTool", "Rect Tool");
            s_ToolIcons[index++] = EditorGUIUtility.TrIconContent("TransformTool", "Move, Rotate or Scale selected objects.");
            s_ToolIcons[index++] = EditorGUIUtility.TrTextContent("Editor tool");

            s_ToolIcons[index++] = EditorGUIUtility.IconContent("MoveTool On");
            s_ToolIcons[index++] = EditorGUIUtility.IconContent("RotateTool On");
            s_ToolIcons[index++] = EditorGUIUtility.IconContent("ScaleTool On");
            s_ToolIcons[index++] = EditorGUIUtility.IconContent("RectTool On");
            s_ToolIcons[index++] = EditorGUIUtility.IconContent("TransformTool On");
            s_ToolIcons[index] = EditorGUIUtility.TrTextContent("Editor tool");

            s_CustomToolIcon = EditorGUIUtility.TrIconContent("CustomTool", "Available Custom Editor Tools");

            index = 0;

            s_ToolControlNames = new string[k_ToolCount];
            s_ToolControlNames[index++] = "ToolbarPersistentToolsPan";
            s_ToolControlNames[index++] = "ToolbarPersistentToolsTranslate";
            s_ToolControlNames[index++] = "ToolbarPersistentToolsRotate";
            s_ToolControlNames[index++] = "ToolbarPersistentToolsScale";
            s_ToolControlNames[index++] = "ToolbarPersistentToolsRect";
            s_ToolControlNames[index++] = "ToolbarPersistentToolsTransform";
            s_ToolControlNames[index] = "ToolbarPersistentToolsCustom";

            s_ShownToolIcons = new GUIContent[k_ToolCount];

            string viewToolsTooltipText = "Hand Tool";

            s_ViewToolIcons = new GUIContent[]
            {
                EditorGUIUtility.TrIconContent("ViewToolOrbit", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolMove", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolZoom", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolOrbit", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolOrbit", "Orbit the Scene view."),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolMove On", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolZoom On", viewToolsTooltipText),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On"),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On", viewToolsTooltipText)
            };

            s_LayerContent = EditorGUIUtility.TrTextContent("Layers", "Which layers are visible in the Scene views.");

            s_PlayIcons = new GUIContent[]
            {
                EditorGUIUtility.TrIconContent("PlayButton", "Play"),
                EditorGUIUtility.TrIconContent("PauseButton", "Pause"),
                EditorGUIUtility.TrIconContent("StepButton", "Step"),
                EditorGUIUtility.TrIconContent("PlayButtonProfile", "Profiler Play"),
                EditorGUIUtility.IconContent("PlayButton On"),
                EditorGUIUtility.IconContent("PauseButton On"),
                EditorGUIUtility.IconContent("StepButton On"),
                EditorGUIUtility.IconContent("PlayButtonProfile On")
            };

            s_CloudIcon = EditorGUIUtility.IconContent("CloudConnect");
            s_AccountContent = EditorGUIUtility.TrTextContent("Account");
        }

        static GUIContent[] s_ToolIcons;
        static string[] s_ToolControlNames;
        static GUIContent[] s_ViewToolIcons;
        static GUIContent   s_LayerContent;
        static GUIContent[] s_PlayIcons;
        static GUIContent s_CustomToolIcon;
        private static GUIContent s_AccountContent;
        static GUIContent   s_CloudIcon;
        internal static event Action<Rect> toolSettingsGui;

        static class Styles
        {
            public readonly static GUIStyle collabButtonStyle = "OffsetDropDown";
            public static readonly GUIStyle dropdown = "Dropdown";
            public static readonly GUIStyle appToolbar = "AppToolbar";
            public static readonly GUIStyle command = "Command";
            public static readonly GUIStyle commandLeft = "CommandLeft";
            public static readonly GUIStyle commandMid = "CommandMid";
            public static readonly GUIStyle commandRight = "CommandRight";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.modifierKeysChanged += Repaint;

            // when undo or redo is done, we need to reset global tools rotation
            Undo.undoRedoPerformed += OnSelectionChange;

            UnityConnect.instance.StateChanged += OnUnityConnectStateChanged;

            get = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.modifierKeysChanged -= Repaint;
            Undo.undoRedoPerformed -= OnSelectionChange;

            UnityConnect.instance.StateChanged -= OnUnityConnectStateChanged;
        }

        // The actual array we display. We build this every frame to make sure it looks correct i.r.t. selection :)
        static GUIContent[] s_ShownToolIcons;

        public static Toolbar get = null;
        public static bool requestShowCollabToolbar = false;
        public static bool isLastShowRequestPartial = true;

        internal static string lastLoadedLayoutName
        {
            get
            {
                if (get == null)
                {
                    return "Layout";
                }
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
        private static List<SubToolbar> s_SubToolbars = new List<SubToolbar>();

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
            RepaintToolbar();
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

        protected override void OldOnGUI()
        {
            const float space = 10;
            const float largeSpace = 20;
            const float standardButtonWidth = 32;
            const float dropdownWidth = 80;
            const float playPauseStopWidth = 140;

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
            DoToolButtons(EditorToolGUI.GetThickArea(pos));

            ReserveWidthRight(largeSpace, ref pos);

            int playModeControlsStart = Mathf.RoundToInt((position.width - playPauseStopWidth) / 2);

            pos.x += pos.width;
            pos.width = (playModeControlsStart - pos.x) - largeSpace;
            DoToolSettings(EditorToolGUI.GetThickArea(pos));

            // Position centered controls.
            pos = new Rect(playModeControlsStart, 0, 140, 0);

            GUILayout.BeginArea(EditorToolGUI.GetThickArea(pos));
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
            DoLayoutDropDown(EditorToolGUI.GetThinArea(pos));

            ReserveWidthLeft(space, ref pos);

            ReserveWidthLeft(dropdownWidth, ref pos);
            DoLayersDropDown(EditorToolGUI.GetThinArea(pos));

            ReserveWidthLeft(largeSpace, ref pos);

            ReserveWidthLeft(dropdownWidth, ref pos);
            if (EditorGUI.DropdownButton(EditorToolGUI.GetThinArea(pos), s_AccountContent, FocusType.Passive, Styles.dropdown))
            {
                ShowUserMenu(EditorToolGUI.GetThinArea(pos));
            }


            ReserveWidthLeft(space, ref pos);

            ReserveWidthLeft(32, ref pos);
            if (GUI.Button(EditorToolGUI.GetThinArea(pos), s_CloudIcon))
                UnityConnectServiceCollection.instance.ShowService(HubAccess.kServiceName, true, "cloud_icon"); // Should show hub when it's done

            foreach (SubToolbar subToolbar in s_SubToolbars)
            {
                ReserveWidthLeft(space, ref pos);
                ReserveWidthLeft(subToolbar.Width, ref pos);
                subToolbar.OnGUI(EditorToolGUI.GetThinArea(pos));
            }


            EditorGUI.ShowRepaints();
            Highlighter.ControlHighlightGUI(this);
        }

        void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            if (!UnityConnect.instance.online || UnityConnect.instance.isDisableUserLogin)
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Sign in..."));

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade to Pro"));
                }
            }
            else
            {
                string accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPortal);
                if (UnityConnect.instance.loggedIn)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Go to account"), false, () => UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));

                if (UnityConnect.instance.loggedIn)
                {
                    string name = "Sign out " + UnityConnect.instance.userInfo.displayName;
                    menu.AddItem(new GUIContent(name), false, () => { UnityConnect.instance.Logout(); });
                }
                else
                    menu.AddItem(EditorGUIUtility.TrTextContent("Sign in..."), false, () => { UnityConnect.instance.ShowLogin(); });

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade to Pro"), false, () => Application.OpenURL("https://store.unity3d.com/"));
                }
            }

            menu.DropDown(dropDownRect);
        }


        void DoToolButtons(Rect rect)
        {
            const int builtinIconsLength = 6;

            // Handle temporary override with ALT
            GUI.changed = false;

            int displayTool = Tools.viewToolActive ? 0 : (int)Tools.current;

            for (int i = 1; i < builtinIconsLength; i++)
            {
                s_ShownToolIcons[i] = s_ToolIcons[i - 1 + (i == displayTool ? s_ShownToolIcons.Length - 1 : 0)];
                s_ShownToolIcons[i].tooltip = s_ToolIcons[i - 1].tooltip;
            }

            var lastCustomTool = EditorToolContext.GetLastCustomTool();

            if (lastCustomTool != null)
                s_ShownToolIcons[builtinIconsLength] = lastCustomTool.toolbarIcon ?? s_CustomToolIcon;
            else
                s_ShownToolIcons[builtinIconsLength] = s_CustomToolIcon;

            s_ShownToolIcons[0] = s_ViewToolIcons[(int)Tools.viewTool + (displayTool == 0 ? s_ShownToolIcons.Length - 1 : 0)];

            displayTool = GUI.Toolbar(rect, displayTool, s_ShownToolIcons, s_ToolControlNames, Styles.command, GUI.ToolbarButtonSize.FitToContents);

            if (GUI.changed)
            {
                var evt = Event.current;

                if (displayTool == (int)Tool.Custom &&
                    (
                        EditorToolContext.GetLastCustomTool() == null
                        || evt.button == 1
                        || (evt.button == 0 && evt.modifiers == EventModifiers.Alt))
                )
                {
                    EditorToolGUI.DoToolHistoryContextMenu();
                }
                else
                {
                    Tools.current = (Tool)displayTool;
                    Tools.ResetGlobalHandleRotation();
                }
            }
        }

        void DoToolSettings(Rect rect)
        {
            if (toolSettingsGui != null)
            {
                toolSettingsGui(rect);
                return;
            }

            const float pivotButtonsWidth = 128;
            rect.width = pivotButtonsWidth;
            rect = EditorToolGUI.GetThinArea(rect);
            EditorToolGUI.DoBuiltinToolSettings(rect);
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
            GUILayout.Toggle(isOrWillEnterPlaymode, s_PlayIcons[buttonOffset], Styles.commandLeft);
            GUI.backgroundColor = Color.white;
            if (GUI.changed)
            {
                TogglePlaying();
                GUIUtility.ExitGUI();
            }

            // Pause game
            GUI.changed = false;

            GUI.SetNextControlName("ToolbarPlayModePauseButton");
            bool isPaused = GUILayout.Toggle(EditorApplication.isPaused, s_PlayIcons[buttonOffset + 1], Styles.commandMid);
            if (GUI.changed)
            {
                EditorApplication.isPaused = isPaused;
                GUIUtility.ExitGUI();
            }

            // Step playmode
            GUI.SetNextControlName("ToolbarPlayModeStepButton");
            if (GUILayout.Button(s_PlayIcons[buttonOffset + 2], Styles.commandRight))
            {
                EditorApplication.Step();
                GUIUtility.ExitGUI();
            }

        }

        void DoLayersDropDown(Rect rect)
        {
            if (EditorGUI.DropdownButton(rect, s_LayerContent, FocusType.Passive, Styles.dropdown))
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
            if (EditorGUI.DropdownButton(rect, GUIContent.Temp(lastLoadedLayoutName), FocusType.Passive, Styles.dropdown))
            {
                Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                rect.x = temp.x;
                rect.y = temp.y;
                EditorUtility.Internal_DisplayPopupMenu(rect, "Window/Layouts", this, 0);
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

        static internal void AddSubToolbar(SubToolbar subToolbar)
        {
            s_SubToolbars.Add(subToolbar);
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
