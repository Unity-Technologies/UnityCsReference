// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEditor.EditorTools;
using UnityEditor.StyleSheets;
using UnityEditor.Experimental;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        static readonly EditorToolGUI.ReusableArrayPool<GUIContent> s_ButtonArrays = new EditorToolGUI.ReusableArrayPool<GUIContent>();
        static readonly EditorToolGUI.ReusableArrayPool<bool> s_BoolArrays = new EditorToolGUI.ReusableArrayPool<bool>();
        static readonly List<EditorTool> s_CustomEditorTools = new List<EditorTool>();
        static readonly List<EditorToolContext> s_CustomEditorContexts = new List<EditorToolContext>();

        static class Styles
        {
            public static GUIStyle command = "AppCommand";
        }

        public static void EditorToolbarForTarget(UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            EditorToolbarForTarget(null, target);
        }

        public static void EditorToolbarForTarget(GUIContent content, UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (target is Editor editor)
                EditorToolManager.GetComponentTools(x => x.inspector == editor, s_CustomEditorTools, true);
            else
                EditorToolManager.GetComponentTools(x => x.target == target, s_CustomEditorTools, true);

            using (new PrefixScope(content))
                EditorToolbar<EditorTool>(s_CustomEditorTools);

            s_CustomEditorTools.Clear();
        }

        public static void ToolContextToolbarForTarget(GUIContent content, UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (target is Editor editor)
                EditorToolManager.GetComponentContexts(x => x.inspector == editor, s_CustomEditorContexts);
            else
                EditorToolManager.GetComponentContexts(x => x.target == target, s_CustomEditorContexts);

            ToolContextToolbar(content, s_CustomEditorContexts);

            s_CustomEditorTools.Clear();
        }

        public static void EditorToolbar(params EditorTool[] tools)
        {
            EditorToolbar<EditorTool>(tools);
        }

        public static void EditorToolbar<T>(IList<T> tools) where T : EditorTool
        {
            T selected;

            if (EditorToolbar(null, EditorToolManager.activeTool as T, tools, out selected))
            {
                if (ToolManager.IsActiveTool(selected))
                    EditorToolManager.RestorePreviousTool();
                else
                    EditorToolManager.activeTool = selected;
            }
        }

        public static void ToolContextToolbar<T>(GUIContent content, IList<T> contexts) where T : EditorToolContext
        {
            T selected;
            if (EditorToolbar(content, EditorToolManager.activeToolContext as T, contexts, out selected))
            {
                if (EditorToolManager.activeToolContext == selected)
                    ToolManager.SetActiveContext<GameObjectToolContext>();
                else
                    EditorToolManager.activeToolContext = selected;
            }
        }

        struct PrefixScope : IDisposable
        {
            public PrefixScope(GUIContent label)
            {
                GUILayout.BeginHorizontal();

                if (label != null && label != GUIContent.none)
                    PrefixLabel(label);
            }

            public void Dispose()
            {
                GUILayout.EndHorizontal();
            }
        }

        internal static bool EditorToolbar<T>(GUIContent content, T selected, IList<T> tools, out T clicked) where T : IEditor
        {
            using (new PrefixScope(content))
            {
                int toolsLength = tools.Count;
                int index = -1;
                var buttons = s_ButtonArrays.Get(toolsLength);
                var enabled = s_BoolArrays.Get(toolsLength);
                clicked = selected;

                for (int i = 0; i < toolsLength; i++)
                {
                    // can happen if the user deletes a tool through scripting
                    if (tools[i] == null)
                    {
                        buttons[i] = GUIContent.none;
                        continue;
                    }

                    if (Equals(tools[i], selected))
                        index = i;

                    enabled[i] = tools[i] is EditorTool tool && !tool.IsAvailable() ? false : true;
                    buttons[i] = EditorToolUtility.GetToolbarIcon(tools[i]);
                }

                EditorGUI.BeginChangeCheck();

                index = GUILayout.Toolbar(index, buttons, enabled, Styles.command);

                if (EditorGUI.EndChangeCheck())
                {
                    clicked = tools[index];
                    return true;
                }
                return false;
            }
        }
    }

    static class EditorToolGUI
    {
        // The largest number of previous tools to show in the history dropdown
        const int k_MaxToolHistory = 6;
        // Number of buttons present in the tools toolbar.
        internal const int k_ToolbarButtonCount = 7;
        // Count of the transform tools + custom editor tool.
        const int k_TransformToolCount = 6;
        // The number of view tools (ViewTool enum, including ViewTool.None)
        const int k_ViewToolCount = 5;

        static class Styles
        {
            const string k_ViewTooltip = "Hand Tool";

            public static readonly GUIStyle command = "AppCommand";
            public static readonly GUIStyle dropdown = "Dropdown";
            public static readonly GUIStyle buttonLeft = "ButtonLeft";
            public static readonly GUIStyle buttonRight = "ButtonRight";

            public static GUIContent[] s_PivotIcons = new GUIContent[]
            {
                EditorGUIUtility.TrTextContentWithIcon("Center", "Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.", "ToolHandleCenter"),
                EditorGUIUtility.TrTextContentWithIcon("Pivot", "Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.", "ToolHandlePivot"),
            };

            public static GUIContent[] s_PivotRotation = new GUIContent[]
            {
                EditorGUIUtility.TrTextContentWithIcon("Local", "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.", "ToolHandleLocal"),
                EditorGUIUtility.TrTextContentWithIcon("Global", "Toggle Tool Handle Rotation\n\nTool handles are in global rotation.", "ToolHandleGlobal")
            };

            public static readonly GUIContent recentTools = EditorGUIUtility.TrTextContent("Recent");
            public static readonly GUIContent selectionTools = EditorGUIUtility.TrTextContent("Selection");
            public static readonly GUIContent availableTools = EditorGUIUtility.TrTextContent("Available");
            public static readonly GUIContent noToolsAvailable = EditorGUIUtility.TrTextContent("No custom tools available");

            public static readonly GUIContent[] toolIcons = new GUIContent[k_TransformToolCount * 2]
            {
                // First half of array is 'Off' state, second half is 'On' state
                EditorGUIUtility.TrIconContent("MoveTool", "Move Tool"),
                EditorGUIUtility.TrIconContent("RotateTool", "Rotate Tool"),
                EditorGUIUtility.TrIconContent("ScaleTool", "Scale Tool"),
                EditorGUIUtility.TrIconContent("RectTool", "Rect Tool"),
                EditorGUIUtility.TrIconContent("TransformTool", "Move, Rotate or Scale selected objects."),
                EditorGUIUtility.TrTextContent("Editor tool"),

                EditorGUIUtility.IconContent("MoveTool On"),
                EditorGUIUtility.IconContent("RotateTool On"),
                EditorGUIUtility.IconContent("ScaleTool On"),
                EditorGUIUtility.IconContent("RectTool On"),
                EditorGUIUtility.IconContent("TransformTool On"),
                EditorGUIUtility.TrTextContent("Editor tool")
            };

            public static readonly string[] toolControlNames = new string[k_ToolbarButtonCount]
            {
                "ToolbarPersistentToolsPan",
                "ToolbarPersistentToolsTranslate",
                "ToolbarPersistentToolsRotate",
                "ToolbarPersistentToolsScale",
                "ToolbarPersistentToolsRect",
                "ToolbarPersistentToolsTransform",
                "ToolbarPersistentToolsCustom"
            };

            public static readonly GUIContent[] s_ViewToolIcons = new GUIContent[k_ViewToolCount * 2]
            {
                EditorGUIUtility.TrIconContent("ViewToolOrbit", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolMove", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolZoom", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolOrbit", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolOrbit", "Orbit the Scene view."),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolMove On", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolZoom On", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On", k_ViewTooltip),
                EditorGUIUtility.TrIconContent("ViewToolOrbit On", k_ViewTooltip)
            };

            public static readonly int viewToolOffset = s_ViewToolIcons.Length / 2;
        }

        public static GUIContent[] s_ShownToolIcons = new GUIContent[k_ToolbarButtonCount];
        public static bool[] s_ShownToolEnabled = new bool[k_ToolbarButtonCount];

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal class ReusableArrayPool<T>
        {
            Dictionary<int, T[]> m_Pool = new Dictionary<int, T[]>();
            int m_MaxEntries = 8;

            public int maxEntries
            {
                get { return m_MaxEntries; }
                set { m_MaxEntries = value; }
            }

            public T[] Get(int count)
            {
                T[] res;
                if (m_Pool.TryGetValue(count, out res))
                    return res;
                if (m_Pool.Count > m_MaxEntries)
                    m_Pool.Clear();
                m_Pool.Add(count, res = new T[count]);
                return res;
            }
        }

        static readonly List<EditorTool> s_ToolList = new List<EditorTool>();
        static readonly List<EditorTool> s_EditorToolModes = new List<EditorTool>(8);
        public static readonly StyleRect s_ButtonRect = EditorResources.GetStyle("AppToolbar-Button").GetRect(StyleCatalogKeyword.size, StyleRect.Size(22, 22));

        internal static Rect GetToolbarEntryRect(Rect pos)
        {
            return new Rect(pos.x, 4, pos.width, s_ButtonRect.height);
        }

        internal static void DoBuiltinToolSettings()
        {
            EditorGUI.BeginChangeCheck();
            Vector2 width = Styles.buttonRight.CalcSize(Styles.s_PivotRotation[1]);
            GUILayout.BeginHorizontal();

            GUI.SetNextControlName("ToolbarToolPivotPositionButton");
            Tools.pivotMode = (PivotMode)EditorGUILayout.CycleButton(
                (int)Tools.pivotMode,
                Styles.s_PivotIcons,
                Styles.buttonLeft,
                GUILayout.Width(width.x));

            using (new EditorGUI.DisabledScope(Tools.current == Tool.Scale && Selection.transforms.Length < 2))
            {
                GUI.SetNextControlName("ToolbarToolPivotOrientationButton");

                PivotRotation tempPivot = (PivotRotation)EditorGUILayout.CycleButton(
                    (int)Tools.pivotRotation,
                    Styles.s_PivotRotation,
                    Styles.buttonRight,
                    GUILayout.Width(width.x));

                if (Tools.pivotRotation != tempPivot)
                {
                    Tools.pivotRotation = tempPivot;
                    if (tempPivot == PivotRotation.Global)
                        Tools.ResetGlobalHandleRotation();
                }
            }

            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                Tools.RepaintAllToolViews();
        }

        internal static void DoContextualToolbarOverlay()
        {
            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.MinWidth(210), GUILayout.Height(30));

            EditorToolManager.GetComponentToolsForSharedTracker(s_EditorToolModes);

            if (s_EditorToolModes.Count > 0)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.EditorToolbar(s_EditorToolModes);

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var inspector in InspectorWindow.GetInspectors())
                    {
                        foreach (var editor in inspector.tracker.activeEditors)
                            editor.Repaint();
                    }
                }
            }
            else
            {
                var fontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = FontStyle.Italic;
                GUILayout.Label(Styles.noToolsAvailable, EditorStyles.centeredGreyMiniLabel);
                EditorStyles.label.fontStyle = fontStyle;
            }
            GUILayout.EndHorizontal();
        }

        internal static void DoEditorToolMenu()
        {
            var toolHistoryMenu = new GenericMenu()
            {
                allowDuplicateNames = true
            };

            var foundTool = false;

            // Recent history
            if (EditorToolManager.GetLastCustomTool() != null)
            {
                foundTool = true;
                toolHistoryMenu.AddDisabledItem(Styles.recentTools);
                EditorToolManager.GetToolHistory(s_ToolList, true);

                for (var i = 0; i < Math.Min(k_MaxToolHistory, s_ToolList.Count); i++)
                {
                    var tool = s_ToolList[i];

                    if (EditorToolUtility.IsCustomEditorTool(tool.GetType()))
                        continue;

                    var name = EditorToolUtility.GetToolName(tool.GetType());

                    if (tool.IsAvailable())
                        toolHistoryMenu.AddItem(new GUIContent(name), false, () => { ToolManager.SetActiveTool(tool); });
                    else
                        toolHistoryMenu.AddDisabledItem(new GUIContent(name));
                }

                toolHistoryMenu.AddSeparator("");
            }

            EditorToolManager.GetComponentToolsForSharedTracker(s_ToolList);

            // Current selection
            if (s_ToolList.Any())
            {
                foundTool = true;
                toolHistoryMenu.AddDisabledItem(Styles.selectionTools);

                for (var i = 0; i < s_ToolList.Count; i++)
                {
                    var tool = s_ToolList[i];

                    if (!EditorToolUtility.IsCustomEditorTool(tool.GetType()))
                        continue;

                    var path = new GUIContent(EditorToolUtility.GetToolMenuPath(tool));

                    if (tool.IsAvailable())
                        toolHistoryMenu.AddItem(path, false, () => { EditorToolManager.activeTool = tool; });
                    else
                        toolHistoryMenu.AddDisabledItem(path);
                }

                toolHistoryMenu.AddSeparator("");
            }

            var global = EditorToolUtility.GetCustomEditorToolsForType(null);

            foreach (var tool in global)
            {
                if (tool.targetContext != null && tool.targetContext != ToolManager.activeContextType)
                    continue;

                if (!foundTool)
                {
                    foundTool = true;
                    toolHistoryMenu.AddDisabledItem(Styles.availableTools);
                }

                toolHistoryMenu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolMenuPath(tool.editor)),
                    false,
                    () => { ToolManager.SetActiveTool(tool.editor); });
            }

            if (!foundTool)
            {
                toolHistoryMenu.AddDisabledItem(Styles.noToolsAvailable);
            }

            toolHistoryMenu.ShowAsContext();
        }

        internal static void DoToolContextMenu()
        {
            var menu = new GenericMenu();
            foreach (var ctx in EditorToolUtility.availableToolContexts)
            {
                menu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolName(ctx)),
                    ToolManager.activeContextType == ctx,
                    () => { ToolManager.SetActiveContext(ctx); });
            }
            menu.ShowAsContext();
        }

        internal static Rect DoToolContextButton(Rect rect)
        {
            var icon = EditorToolUtility.GetIcon(ToolManager.activeContextType);
            rect.x += rect.width;
            rect.width = Styles.dropdown.CalcSize(icon).x;
            if (EditorGUI.DropdownButton(rect, icon, FocusType.Passive, Styles.dropdown))
                DoToolContextMenu();
            return rect;
        }

        internal static void DoBuiltinToolbar(Rect rect)
        {
            EditorGUI.BeginChangeCheck();

            int selectedIndex = (int)(Tools.viewToolActive ? Tool.View : Tools.current);

            EditorTool lastCustomTool = EditorToolManager.GetLastCustomTool();

            // Set View & Custom entries manually
            s_ShownToolEnabled[0] = true;
            s_ShownToolIcons[0] = Styles.s_ViewToolIcons[(int)Tools.viewTool + (selectedIndex == 0 ? Styles.viewToolOffset : 0)];

            s_ShownToolEnabled[(int)Tool.Custom] = true;
            s_ShownToolIcons[(int)Tool.Custom] = EditorToolUtility.GetToolbarIcon(lastCustomTool);

            // Get enabled state for each builtin tool (or whatever the active context resolves to)
            // Currently builtin tools always use the default icon and tooltip
            for (int i = (int)Tool.Move; i < (int)Tool.Custom; i++)
            {
                s_ShownToolIcons[i] = Styles.toolIcons[i - 1 + (i == selectedIndex ? k_TransformToolCount : 0)];
                s_ShownToolIcons[i].tooltip = Styles.toolIcons[i - 1].tooltip;
                var tool = EditorToolUtility.GetEditorToolWithEnum((Tool)i);
                s_ShownToolEnabled[i] = tool != null && tool.IsAvailable();
            }

            selectedIndex = GUI.Toolbar(rect, selectedIndex, s_ShownToolIcons, Styles.toolControlNames, Styles.command, GUI.ToolbarButtonSize.FitToContents, s_ShownToolEnabled);

            if (EditorGUI.EndChangeCheck())
            {
                var evt = Event.current;

                switch (selectedIndex)
                {
                    case (int)Tool.Custom:
                    {
                        if (EditorToolManager.GetLastCustomTool() == null
                            || evt.button == 1
                            || (evt.button == 0 && evt.modifiers == EventModifiers.Alt))
                            DoEditorToolMenu();
                        else
                            goto default;
                        break;
                    }

                    default:
                    {
                        Tools.current = (Tool)selectedIndex;
                        Tools.ResetGlobalHandleRotation();
                        break;
                    }
                }
            }
        }
    }
}
