// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    static class EditorToolUtility
    {
        static readonly Regex k_NewLine = new Regex(@"\r|\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_TrailingForwardSlashOrWhiteSpace = new Regex(@"[/|\s]*\Z", RegexOptions.Compiled);

        static EditorToolCache s_ToolCache = new EditorToolCache(typeof(EditorToolAttribute));
        static EditorToolCache s_ContextCache = new EditorToolCache(typeof(EditorToolContextAttribute));
        static Dictionary<Type, GUIContent> s_ToolbarIcons = new Dictionary<Type, GUIContent>();
        static Type[] s_AvailableToolContexts;

        // Caution: Returns all types without filtering for EditorToolContext
        internal static IEnumerable<EditorTypeAssociation> GetCustomEditorToolsForType(Type type)
        {
            return s_ToolCache.GetEditorsForTargetType(type);
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
            return L10n.Tr(ObjectNames.NicifyVariableName(tool.Name));
        }

        internal static string GetToolMenuPath(EditorTool tool)
        {
            return GetToolMenuPath(tool != null ? tool.GetType() : typeof(EditorTool));
        }

        internal static EditorToolAttribute GetEditorToolAttribute(Type type)
        {
            if (type == null)
                return null;

            return (EditorToolAttribute)type.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
        }

        internal static int GetNonBuiltinToolCount()
        {
            var globalToolsCount = GetCustomEditorToolsForType(null).Count();
            var customToolsCount = EditorToolManager.GetCustomEditorToolsCount(true);
            var totalCount = globalToolsCount + customToolsCount;

            return totalCount;
        }

        internal static bool IsComponentEditor(Type type)
        {
            if (type.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() is ToolAttribute attrib)
                return attrib.targetType != null;
            return false;
        }

        public static void InstantiateComponentContexts(List<ComponentEditor> editors)
        {
            s_ContextCache.InstantiateEditors(null, editors);
        }

        public static void InstantiateComponentTools(EditorToolContext ctx, List<ComponentEditor> editors)
        {
            s_ToolCache.InstantiateEditors(ctx, editors);
        }

        // Get an EditorTool instance for type of tool enum. This will return an instance of NoneTool if the active
        // context does not resolve to a valid tool.
        internal static EditorTool GetEditorToolWithEnum(Tool type, EditorToolContext ctx = null)
        {
            var context = (ctx == null ? EditorToolManager.activeToolContext : ctx);
            switch (type)
            {
                case Tool.View:
                    return (EditorTool)EditorToolManager.GetSingleton(typeof(ViewModeTool));
                case Tool.Custom:
                    return EditorToolManager.GetLastTool(x => GetEnumWithEditorTool(x) == Tool.Custom);
                case Tool.None:
                    return EditorToolManager.GetSingleton<NoneTool>();
                default:
                    var resolved = context.ResolveTool(type);
                    if (resolved == null)
                        goto case Tool.None;

                    // Tool types can resolve to either global or instance tools
                    if (IsCustomEditorTool(resolved))
                    {
                        var instance = EditorToolManager.GetComponentTool(resolved);
                        if (instance == null)
                        {
                            Debug.LogError($"{context} resolved Tool.{type} to a Component tool of type `{resolved}`, but " +
                                $"no component matching the target type is in the active selection. The active tool " +
                                $"context will be set to the default.");
                            EditorToolManager.activeToolContext = EditorToolManager.GetSingleton<GameObjectToolContext>();
                            return (EditorTool)EditorToolManager.GetSingleton(EditorToolManager.activeToolContext.ResolveTool(type));
                        }

                        return instance;
                    }

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
            return s_ToolCache.GetTargetType(type) != null;
        }

        internal static GUIContent GetToolbarIcon<T>(T obj) where T : IEditor
        {
            if (obj == null)
                return GetIcon(typeof(T));
            if (obj is EditorTool tool && tool.toolbarIcon != null)
                return tool.toolbarIcon;
            return GetIcon(obj.GetType());
        }

        internal static GUIContent GetIcon(Type editorToolType)
        {
            GUIContent res;

            if (s_ToolbarIcons.TryGetValue(editorToolType, out res))
                return res;

            res = new GUIContent() { tooltip = GetToolName(editorToolType) };

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
