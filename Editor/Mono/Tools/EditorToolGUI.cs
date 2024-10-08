// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            public static GUIStyle commandLeft;
            public static GUIStyle commandMid;
            public static GUIStyle commandRight;

            static Styles()
            {
                GUI.FindStyles(ref command, out commandLeft, out commandMid, out commandRight, "left", "mid", "right");

                var normalColor = command.normal.textColor;

                command.imagePosition = ImagePosition.ImageAbove;
                command.active.textColor = normalColor;
                command.onActive.textColor = normalColor;
                command.onNormal.textColor = normalColor;
                command.fontStyle = FontStyle.Bold;

                commandLeft.imagePosition = ImagePosition.ImageAbove;
                commandLeft.active.textColor = normalColor;
                commandLeft.onActive.textColor = normalColor;
                commandLeft.onNormal.textColor = normalColor;
                commandLeft.fontStyle = FontStyle.Bold;

                commandMid.imagePosition = ImagePosition.ImageAbove;
                commandMid.active.textColor = normalColor;
                commandMid.onActive.textColor = normalColor;
                commandMid.onNormal.textColor = normalColor;
                commandMid.fontStyle = FontStyle.Bold;

                commandRight.imagePosition = ImagePosition.ImageAbove;
                commandRight.active.textColor = normalColor;
                commandRight.onActive.textColor = normalColor;
                commandRight.onNormal.textColor = normalColor;
                commandRight.fontStyle = FontStyle.Bold;
            }
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
                target = editor.target;

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
                    ToolManager.RestorePreviousTool();
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

                    if (ReferenceEquals(tools[i], selected))
                        index = i;

                    enabled[i] = tools[i] is EditorTool tool && !tool.IsAvailable() ? false : true;
                    buttons[i] = EditorToolUtility.GetToolbarIcon(tools[i]);
                }

                EditorGUI.BeginChangeCheck();

                index = GUILayout.Toolbar(index, buttons, enabled, Styles.command, Styles.commandLeft, Styles.commandMid, Styles.commandRight);

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
        // Number of buttons present in the tools toolbar.
        internal const int k_ToolbarButtonCount = 7;

        static class Styles
        {
            public static readonly GUIContent selectionTools = EditorGUIUtility.TrTextContent("Selection");
            public static readonly GUIContent globalTools = EditorGUIUtility.TrTextContent("Global");
            public static readonly GUIContent noToolsAvailable = EditorGUIUtility.TrTextContent("No custom tools available");
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

        internal static void ShowComponentToolsContextMenu()
        {
            BuildCustomGlobalToolsContextMenu().ShowAsContext();
        }

        internal static void ShowCustomGlobalToolsContextMenu(Rect worldBound)
        {
            BuildCustomGlobalToolsContextMenu().DropDown(worldBound);
        }

        static GenericMenu BuildCustomGlobalToolsContextMenu()
        {
            var toolHistoryMenu = new GenericMenu() { allowDuplicateNames = true };
            bool foundGlobalTools = false;
            var global = EditorToolUtility.GetCustomEditorToolsForType(null);

            foreach (var tool in global)
            {
                if (tool.targetContext != null && tool.targetContext != ToolManager.activeContextType)
                    continue;

                foundGlobalTools = true;

                toolHistoryMenu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolMenuPath(tool.editor)),
                    false,
                    () => { ToolManager.SetActiveTool(tool.editor); });
            }

            if (!foundGlobalTools)
                toolHistoryMenu.AddDisabledItem(Styles.noToolsAvailable);

            return toolHistoryMenu;
        }
    }
}
