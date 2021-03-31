// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    /// <summary>
    /// An Editor instance and EditorTool type.
    /// </summary>
    class CustomEditorTool
    {
        public Editor owner;
        public List<Editor> additionalEditors;
        public Type editorToolType;

        public CustomEditorTool(Editor owner, Type tool)
        {
            this.owner = owner;
            this.editorToolType = tool;
        }

        public UObject[] targets
        {
            get
            {
                if (additionalEditors == null)
                    return owner.targets;
                List<UObject> objects = new List<UObject>(owner.targets);
                foreach (var editor in additionalEditors)
                    objects.AddRange(editor.targets);
                return objects.ToArray();
            }
        }
    }

    static class EditorToolUtility
    {
        static readonly Regex k_NewLine = new Regex(@"\r|\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_TrailingForwardSlashOrWhiteSpace = new Regex(@"[/|\s]*\Z", RegexOptions.Compiled);

        struct CustomEditorToolAssociation
        {
            public Type targetBehaviour;
            public Type editorTool;
        }

        static CustomEditorToolAssociation[] s_CustomEditorTools;
        static Dictionary<Type, List<Type>> s_CustomEditorToolsTypeAssociations = new Dictionary<Type, List<Type>>();
        static Dictionary<Type, GUIContent> s_ToolbarIcons = new Dictionary<Type, GUIContent>();
        static HashSet<Type> s_TrackerSelectedTypes = new HashSet<Type>();
        static Type[] s_AvailableToolContexts;

        static CustomEditorToolAssociation[] customEditorTools
        {
            get
            {
                if (s_CustomEditorTools == null)
                {
                    Type[] editorTools = TypeCache.GetTypesWithAttribute<EditorToolAttribute>()
                        .Where(x => !x.IsAbstract)
                        .ToArray();

                    int len = editorTools.Length;

                    s_CustomEditorTools = new CustomEditorToolAssociation[len];

                    for (int i = 0; i < len; i++)
                    {
                        var customToolAttribute = (EditorToolAttribute)editorTools[i].GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();

                        s_CustomEditorTools[i] = new CustomEditorToolAssociation()
                        {
                            editorTool = editorTools[i],
                            targetBehaviour = customToolAttribute.targetType
                        };
                    }
                }

                return s_CustomEditorTools;
            }
        }

        internal static List<Type> GetCustomEditorToolsForType(Type type)
        {
            List<Type> res;

            if (type == null)
            {
                res = new List<Type>();
                foreach (var tool in customEditorTools)
                    if (!IsBuiltinTool(tool.editorTool) && tool.targetBehaviour == null)
                        res.Add(tool.editorTool);
                return res;
            }

            if (s_CustomEditorToolsTypeAssociations.TryGetValue(type, out res))
                return res;

            s_CustomEditorToolsTypeAssociations.Add(type, res = new List<Type>());

            for (int i = 0, c = customEditorTools.Length; i < c; i++)
            {
                if (customEditorTools[i].targetBehaviour != null
                    && (customEditorTools[i].targetBehaviour.IsAssignableFrom(type)
                        || type.IsAssignableFrom(customEditorTools[i].targetBehaviour)))
                    res.Add(customEditorTools[i].editorTool);
            }

            return res;
        }

        internal static Type[] availableToolContexts
        {
            get
            {
                if (s_AvailableToolContexts == null)
                {
                    s_AvailableToolContexts = TypeCache.GetTypesWithAttribute<EditorToolContextAttribute>()
                        .Where(x => typeof(EditorToolContext).IsAssignableFrom(x) && !x.IsAbstract)
                        .ToArray();

                    // Move the default tool context to the top of the list
                    int idx = Array.IndexOf(s_AvailableToolContexts, typeof(GameObjectToolContext));

                    if (idx > 0)
                    {
                        var tmp = s_AvailableToolContexts[0];
                        s_AvailableToolContexts[0] = s_AvailableToolContexts[idx];
                        s_AvailableToolContexts[idx] = tmp;
                    }
                }

                return s_AvailableToolContexts;
            }
        }

        internal static string GetToolName(Type tool)
        {
            var path = GetToolMenuPath(tool);
            return GetNameFromToolPath(path);
        }

        internal static string GetNameFromToolPath(string path)
        {
            var index = path.LastIndexOf("/", StringComparison.Ordinal);
            if (index < 0)
                return path;
            return path.Substring(index + 1, path.Length - (index + 1));
        }

        internal static string SanitizeToolPath(string path)
        {
            path = k_TrailingForwardSlashOrWhiteSpace.Replace(path, string.Empty);
            return k_NewLine.Replace(path, " ").Trim();
        }

        internal static string GetToolMenuPath(Type tool)
        {
            if (typeof(EditorTool).IsAssignableFrom(tool))
            {
                var editorToolAttribute = tool.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
                if (editorToolAttribute is EditorToolAttribute attrib && !string.IsNullOrEmpty(attrib.displayName))
                {
                    string path = SanitizeToolPath(attrib.displayName);
                    if (!string.IsNullOrEmpty(path))
                        return L10n.Tr(path);
                }
            }
            else if (typeof(EditorToolContext).IsAssignableFrom(tool))
            {
                var toolContextAttribute =
                    tool.GetCustomAttributes(typeof(EditorToolContextAttribute), false).FirstOrDefault();

                if (toolContextAttribute is EditorToolContextAttribute ctxAttrib &&
                    !string.IsNullOrEmpty(ctxAttrib.title))
                {
                    string path = SanitizeToolPath(ctxAttrib.title);
                    if (!string.IsNullOrEmpty(path))
                        return L10n.Tr(path);
                }
            }

            return L10n.Tr(ObjectNames.NicifyVariableName(tool.Name));
        }

        internal static string GetToolMenuPath(EditorTool tool)
        {
            return GetToolMenuPath(tool != null ? tool.GetType() : typeof(EditorTool));
        }

        static EditorToolAttribute GetEditorToolAttribute(EditorTool tool)
        {
            if (tool == null)
                return null;
            return GetEditorToolAttribute(tool.GetType());
        }

        internal static EditorToolAttribute GetEditorToolAttribute(Type type)
        {
            if (type == null)
                return null;

            return (EditorToolAttribute)type.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
        }

        internal static int GetNonBuiltinToolCount()
        {
            var globalToolsCount = GetCustomEditorToolsForType(null).Count;
            var customToolsCount = EditorToolManager.GetCustomEditorToolsCount(true);
            var totalCount = globalToolsCount + customToolsCount;

            return totalCount;
        }

        internal static Type GetCustomEditorToolTargetType(EditorTool tool)
        {
            var attr = GetEditorToolAttribute(tool);
            if (attr == null)
                return null;
            return attr.targetType;
        }

        internal static void GetEditorToolsForTracker(ActiveEditorTracker tracker, List<CustomEditorTool> tools)
        {
            tools.Clear();
            s_TrackerSelectedTypes.Clear();
            var editors = tracker.activeEditors;

            for (int i = 0, c = editors.Length; i < c; i++)
            {
                var editor = editors[i];

                if (editor == null || editor.target == null)
                    continue;

                var targetType = editor.target.GetType();

                // Some components can be added to a GameObject multiple times. Prevent them from creating multiple tools.
                if (s_TrackerSelectedTypes.Add(targetType))
                {
                    var eligibleToolTypes = GetCustomEditorToolsForType(targetType);

                    foreach (var type in eligibleToolTypes)
                        tools.Add(new CustomEditorTool(editor, type));
                }
                else
                {
                    foreach (var tool in tools)
                    {
                        if (tool.owner.target.GetType() == targetType)
                        {
                            if (tool.additionalEditors == null)
                                tool.additionalEditors = new List<Editor>() { editor };
                            else
                                tool.additionalEditors.Add(editor);
                        }
                    }
                }
            }

            s_TrackerSelectedTypes.Clear();
        }

        // Get an EditorTool instance for type of tool enum. This will return an instance of NoneTool if the active
        // context does not resolve to a valid tool.
        internal static EditorTool GetEditorToolWithEnum(Tool type, EditorToolContext ctx = null)
        {
            switch (type)
            {
                case Tool.View:
                    return (EditorTool)EditorToolManager.GetSingleton(typeof(ViewModeTool));
                case Tool.Custom:
                    return EditorToolManager.GetLastTool(x => GetEnumWithEditorTool(x) == Tool.Custom);
                case Tool.None:
                    return EditorToolManager.GetSingleton<NoneTool>();
                default:
                    var resolved = (ctx == null ? EditorToolManager.activeToolContext : ctx).ResolveTool(type);
                    if (resolved == null)
                        goto case Tool.None;
                    // EditorToolContext.ResolveTool does type validation, so a fast cast is safe here.
                    return (EditorTool)EditorToolManager.GetSingleton(resolved);
            }
        }

        internal static Tool GetEnumWithEditorTool(EditorTool tool, EditorToolContext ctx = null)
        {
            if (tool == null || tool is NoneTool)
                return Tool.None;

            if (tool is ViewModeTool)
                return Tool.View;

            var type = tool.GetType();

            for (int i = (int)Tool.Move; i < (int)Tool.Custom; i++)
            {
                if ((ctx == null ? EditorToolManager.activeToolContext : ctx).ResolveTool((Tool)i) == type)
                    return (Tool)i;
            }

            return Tool.Custom;
        }

        static bool IsBuiltinTool(Type type)
        {
            return type == typeof(ViewModeTool) ||
                type == typeof(TransformTool) ||
                type == typeof(MoveTool) ||
                type == typeof(RotateTool) ||
                type == typeof(ScaleTool) ||
                type == typeof(RectTool);
        }

        internal static bool IsManipulationTool(Tool tool)
        {
            return tool == Tool.Move
                || tool == Tool.Rotate
                || tool == Tool.Scale
                || tool == Tool.Rect
                || tool == Tool.Transform;
        }

        internal static bool IsCustomEditorTool(Type type)
        {
            for (int i = 0, c = customEditorTools.Length; i < c; i++)
                if (customEditorTools[i].editorTool == type)
                    return customEditorTools[i].targetBehaviour != null;
            return false;
        }

        internal static GUIContent GetToolbarIcon<T>(T obj) where T : EditorTool
        {
            if (obj == null)
                return GetIcon(typeof(T));
            if (obj.toolbarIcon != null)
                return obj.toolbarIcon;
            return GetIcon(obj.GetType());
        }

        internal static GUIContent GetIcon(Type editorToolType)
        {
            GUIContent res;

            if (s_ToolbarIcons.TryGetValue(editorToolType, out res))
                return res;

            res = new GUIContent() { tooltip = GetToolName(editorToolType) };

            EditorToolContextAttribute ctxAttribute = editorToolType.GetCustomAttributes(typeof(EditorToolContextAttribute), false).FirstOrDefault() as EditorToolContextAttribute;

            if (ctxAttribute != null && !string.IsNullOrEmpty(ctxAttribute.tooltip))
                res.tooltip = ctxAttribute.tooltip;

            // First check for the tool type itself
            if ((res.image = EditorGUIUtility.FindTexture(editorToolType)) != null)
                goto ReturnToolbarIcon;

            // If it's a custom editor tool, try to get an icon for the tool's target type
            var attrib = GetEditorToolAttribute(editorToolType);
            if (attrib?.targetType != null && (res.image = AssetPreview.GetMiniTypeThumbnailFromType(attrib.targetType)) != null)
                goto ReturnToolbarIcon;

            // And finally fall back to the default Custom Tool icon
            res.image = EditorGUIUtility.LoadIconRequired("CustomTool");

        ReturnToolbarIcon:
            if (string.IsNullOrEmpty(res.tooltip))
                res.tooltip = ObjectNames.NicifyVariableName(editorToolType.Name);
            s_ToolbarIcons.Add(editorToolType, res);

            return res;
        }
    }
}
