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

        public static void EditorToolbarForTarget(UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            EditorToolContext.GetCustomEditorToolsForTarget(target, s_CustomEditorTools, true);
            EditorToolbar<EditorTool>(s_CustomEditorTools);
            s_CustomEditorTools.Clear();
        }

        public static void EditorToolbarForTarget(GUIContent content, UObject target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            GUILayout.BeginHorizontal();
            PrefixLabel(content);
            EditorToolContext.GetCustomEditorToolsForTarget(target, s_CustomEditorTools, true);
            EditorToolbar<EditorTool>(s_CustomEditorTools);
            GUILayout.EndHorizontal();
            s_CustomEditorTools.Clear();
        }

        public static void EditorToolbar(params EditorTool[] tools)
        {
            EditorToolbar<EditorTool>(tools);
        }

        public static void EditorToolbar<T>(IList<T> tools) where T : EditorTool
        {
            EditorTool selected;

            if (EditorToolbar(EditorToolContext.activeTool, tools, out selected))
            {
                if (selected == EditorToolContext.activeTool)
                    EditorToolContext.RestorePreviousTool();
                else
                    EditorToolContext.activeTool = selected;
            }
        }

        internal static bool EditorToolbar<T>(EditorTool selected, IList<T> tools, out EditorTool clicked) where T : EditorTool
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

                if (tools[i] == selected)
                    index = i;

                enabled[i] = tools[i].IsAvailable();
                buttons[i] = tools[i].toolbarIcon ?? GUIContent.none;
            }

            EditorGUI.BeginChangeCheck();

            index = GUILayout.Toolbar(index, buttons, enabled, "Command");

            if (EditorGUI.EndChangeCheck())
            {
                clicked = tools[index];
                return true;
            }
            return false;
        }
    }

    static class EditorToolGUI
    {
        const int k_MaxToolHistory = 6;

        static class Styles
        {
            public static GUIContent recentTools = EditorGUIUtility.TrTextContent("Recent");
            public static GUIContent selectionTools = EditorGUIUtility.TrTextContent("Selection");
            public static GUIContent availableTools = EditorGUIUtility.TrTextContent("Available");
            public static GUIContent noToolsAvailable = EditorGUIUtility.TrTextContent("No custom tools available");
        }

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

        static GUIContent[] s_PivotIcons = new GUIContent[]
        {
            EditorGUIUtility.TrTextContentWithIcon("Center", "Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.", "ToolHandleCenter"),
            EditorGUIUtility.TrTextContentWithIcon("Pivot", "Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.", "ToolHandlePivot"),
        };

        static GUIContent[] s_PivotRotation = new GUIContent[]
        {
            EditorGUIUtility.TrTextContentWithIcon("Local", "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.", "ToolHandleLocal"),
            EditorGUIUtility.TrTextContentWithIcon("Global", "Toggle Tool Handle Rotation\n\nTool handles are in global rotation.", "ToolHandleGlobal")
        };

        static readonly List<EditorTool> s_EditorToolModes = new List<EditorTool>(8);
        public static readonly StyleRect s_ButtonRect = EditorResources.GetStyle("AppToolbar-Button").GetRect(StyleCatalogKeyword.size, StyleRect.Size(22, 22));

        internal static Rect GetThinArea(Rect pos)
        {
            return new Rect(pos.x, 4, pos.width, s_ButtonRect.height);
        }

        internal static Rect GetThickArea(Rect pos)
        {
            return new Rect(pos.x, 4, pos.width, s_ButtonRect.height);
        }

        internal static void DoBuiltinToolSettings(Rect rect)
        {
            DoBuiltinToolSettings(rect, "ButtonLeft", "ButtonRight");
        }

        internal static void DoBuiltinToolSettings(Rect rect, GUIStyle buttonLeftStyle, GUIStyle buttonRightStyle)
        {
            GUI.SetNextControlName("ToolbarToolPivotPositionButton");
            Tools.pivotMode = (PivotMode)EditorGUI.CycleButton(new Rect(rect.x, rect.y, rect.width / 2, rect.height), (int)Tools.pivotMode, s_PivotIcons, buttonLeftStyle);
            if (Tools.current == Tool.Scale && Selection.transforms.Length < 2)
                GUI.enabled = false;
            GUI.SetNextControlName("ToolbarToolPivotOrientationButton");
            PivotRotation tempPivot = (PivotRotation)EditorGUI.CycleButton(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), (int)Tools.pivotRotation, s_PivotRotation, buttonRightStyle);
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

        static internal void DoContextualToolbarOverlay(UnityEngine.Object target, SceneView sceneView)
        {
            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.MinWidth(210), GUILayout.Height(30));

            EditorToolContext.GetCustomEditorTools(s_EditorToolModes, false);

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

        internal static void DoToolContextMenu()
        {
            var toolHistoryMenu = new GenericMenu()
            {
                allowDuplicateNames = true
            };

            var foundTool = false;

            // Recent history
            if (EditorToolContext.GetLastCustomTool() != null)
            {
                foundTool = true;
                toolHistoryMenu.AddDisabledItem(Styles.recentTools);
                EditorToolContext.GetToolHistory(s_ToolList, true);

                for (var i = 0; i < Math.Min(k_MaxToolHistory, s_ToolList.Count); i++)
                {
                    var tool = s_ToolList[i];

                    if (EditorToolUtility.IsCustomEditorTool(tool.GetType()))
                        continue;

                    var name = EditorToolUtility.GetToolName(tool.GetType());

                    if (tool.IsAvailable())
                        toolHistoryMenu.AddItem(new GUIContent(name), false, () => { EditorToolContext.activeTool = tool; });
                    else
                        toolHistoryMenu.AddDisabledItem(new GUIContent(name));
                }

                toolHistoryMenu.AddSeparator("");
            }

            EditorToolContext.GetCustomEditorTools(s_ToolList, false);

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
                        toolHistoryMenu.AddItem(path, false, () => { EditorToolContext.activeTool = tool; });
                    else
                        toolHistoryMenu.AddDisabledItem(path);
                }

                toolHistoryMenu.AddSeparator("");
            }

            var global = EditorToolUtility.GetCustomEditorToolsForType(null);

            if (global.Any())
            {
                foundTool = true;
                toolHistoryMenu.AddDisabledItem(Styles.availableTools);

                foreach (var toolType in global)
                {
                    toolHistoryMenu.AddItem(
                        new GUIContent(EditorToolUtility.GetToolMenuPath(toolType)),
                        false,
                        () => { EditorTools.EditorTools.SetActiveTool(toolType); });
                }
            }

            if (!foundTool)
            {
                toolHistoryMenu.AddDisabledItem(Styles.noToolsAvailable);
            }

            toolHistoryMenu.ShowAsContext();
        }
    }
}
