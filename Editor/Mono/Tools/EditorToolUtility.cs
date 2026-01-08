// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Overlays;
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
        
        internal static IEnumerable<EditorTypeAssociation> availableGlobalToolContexts
        {
            get => s_ContextCache.GetEditorsForTargetType(null);
        }

        internal static IEnumerable<EditorTypeAssociation> registeredToolContexts
        {
            get => s_ContextCache.availableEditorTypeAssociations;
        }
        
        internal static IEnumerable<EditorTypeAssociation> availailableEditorTools
        {
            get => s_ToolCache.availableEditorTypeAssociations;
        }

        internal static int toolContextsInProject => s_ContextCache.Count;

        internal class SortedContextDataCache
        {
            List<EditorTypeAssociation> m_SortedGlobalContextAssociations = new();
            List<EditorTypeAssociation> m_SortedAvailableCompContextAssoc = new();
            List<EditorTypeAssociation> m_SortedUnavailableCompContextAssoc = new();
            readonly List<EditorTypeAssociation> m_SortedAllAvailableContextAssoc = new();
            
            bool m_Dirty = true;
            
            internal IReadOnlyList<EditorTypeAssociation> allAvailableContextAssociations
            {
                get
                {
                    EnsureSorted();
                    return m_SortedAllAvailableContextAssoc;
                }
            }
            
            internal IReadOnlyList<EditorTypeAssociation> globalContextAssociations
            {
                get
                {
                    EnsureSorted();
                    return m_SortedGlobalContextAssociations;
                }
            }
            
            internal IReadOnlyList<EditorTypeAssociation> availableCompContextAssociations
            {
                get
                {
                    EnsureSorted();
                    return m_SortedAvailableCompContextAssoc;
                }
            }
            
            internal IReadOnlyList<EditorTypeAssociation> unavailableCompContextAssociations
            {
                get
                {
                    EnsureSorted();
                    return m_SortedUnavailableCompContextAssoc;
                }
            }

            void EnsureSorted()
            {
                if (m_Dirty)
                {
                    SortContextAssociations();
                    m_Dirty = false;
                }
            }

            internal void SetDirty()
            {
                m_Dirty = true;
            }
            
            void SortContextAssociations()
            {
                Comparison<EditorTypeAssociation> sortComp = (a, b) =>
                {
                    // Sort by priority
                    int result = a.priority.CompareTo(b.priority);
                    if (result != 0) 
                        return result;
                    
                    // Then by name
                    result = string.Compare(GetToolName(a.editor), GetToolName(b.editor), StringComparison.Ordinal);
                    if (result != 0) 
                        return result;
                    
                    // Then by hashcode
                    return a.GetHashCode().CompareTo(b.GetHashCode());
                };
                
                // Sort global contexts
                var globalContexts = new List<EditorTypeAssociation>(availableGlobalToolContexts);
                globalContexts.Sort(sortComp);

                // Move GO context to front of globals
                for (int i = globalContexts.Count - 1; i >= 0; --i)
                {
                    if (globalContexts[i].editor == typeof(GameObjectToolContext))
                    {
                        var goAssoc = globalContexts[i];
                        globalContexts.RemoveAt(i);
                        globalContexts.Insert(0, goAssoc);
                        break;
                    }
                }
                m_SortedGlobalContextAssociations = globalContexts;

                // Collect all registered component contexts
                var allRegisteredCompContexts = new List<EditorTypeAssociation>();
                foreach (var assoc in registeredToolContexts)
                {
                    if (assoc.targetBehaviour != typeof(NullTargetKey))
                        allRegisteredCompContexts.Add(assoc);
                }

                // Split into available and unavailable component contexts
                m_SortedAvailableCompContextAssoc.Clear();
                m_SortedUnavailableCompContextAssoc.Clear();

                foreach (var compAssoc in allRegisteredCompContexts)
                {
                    bool isAvailableComp = false;
                    foreach (var compEditor in EditorToolManager.componentContexts)
                    {
                        if (compEditor.editorType == compAssoc.editor)
                        {
                            isAvailableComp = true;
                            break;
                        }
                    }

                    if (isAvailableComp)
                        m_SortedAvailableCompContextAssoc.Add(compAssoc);
                    else
                        m_SortedUnavailableCompContextAssoc.Add(compAssoc);
                }

                // Sort both lists
                m_SortedAvailableCompContextAssoc.Sort(sortComp);
                m_SortedUnavailableCompContextAssoc.Sort(sortComp);

                // Combine globals and available component contexts
                m_SortedAllAvailableContextAssoc.Clear();
                m_SortedAllAvailableContextAssoc.AddRange(m_SortedGlobalContextAssociations);
                m_SortedAllAvailableContextAssoc.AddRange(m_SortedAvailableCompContextAssoc);
            }
        }

        internal static SortedContextDataCache sortedContextsDataCache { get; } = new();

        // Caution: Returns all types without filtering for EditorToolContext
        internal static IEnumerable<EditorTypeAssociation> GetCustomEditorToolsForType(Type type)
        {
            return s_ToolCache.GetEditorsForTargetType(type);
        }

        internal static string GetToolName(Type tool)
        {
            var path = GetToolMenuPath(tool);
            return GetNameFromToolPath(path);
        }

        internal static string GetContextName(Type context, bool forTooltip)
        {
            var contextName = GetToolName(context);
            if (forTooltip)
                contextName += (context == typeof(GameObjectToolContext) ? " (Default)" : "");

            return contextName;
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
            if (typeof(EditorTool).IsAssignableFrom(tool) || typeof(EditorToolContext).IsAssignableFrom(tool))
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var toolAttribute = tool.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault();
#pragma warning restore RS0030
                if (toolAttribute is ToolAttribute attrib && !string.IsNullOrEmpty(attrib.displayName))
                {
                    string path = SanitizeToolPath(attrib.displayName);
                    if (!string.IsNullOrEmpty(path))
                        return L10n.Tr(path);
                }
            }
            else if (typeof(EditorToolContext).IsAssignableFrom(tool))
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var editorToolAttribute = tool.GetCustomAttributes(typeof(EditorToolContextAttribute), false).FirstOrDefault();
#pragma warning restore RS0030
                if (editorToolAttribute is EditorToolContextAttribute attrib && !string.IsNullOrEmpty(attrib.displayName))
                {
                    string path = SanitizeToolPath(attrib.displayName);
                    if (!string.IsNullOrEmpty(path))
                        return L10n.Tr(path);
                }
            }
            return L10n.Tr(ObjectNames.NicifyVariableName(tool.Name.Replace("ToolContext", string.Empty)));
        }

        internal static string GetToolMenuPath(EditorTool tool)
        {
            return GetToolMenuPath(tool != null ? tool.GetType() : typeof(EditorTool));
        }

        internal static EditorToolAttribute GetEditorToolAttribute(Type type)
        {
            if (type == null)
                return null;

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return (EditorToolAttribute)type.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
#pragma warning restore RS0030
        }

        internal static int GetNonBuiltinToolCount()
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var globalEditorTools = GetCustomEditorToolsForType(null).Where(t => EditorToolManager.additionalContextToolTypesCache.All(tc => tc != t.editor));
#pragma warning restore RS0030
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return globalEditorTools.Count();
#pragma warning restore RS0030
        }

        internal static bool IsComponentEditor(Type type)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (type.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() is ToolAttribute attrib)
#pragma warning restore RS0030
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
                    return EditorToolManager.lastCustomTool;
                case Tool.None:
                    return EditorToolManager.GetSingleton<NoneTool>();
                default:
                    var resolved = context.ResolveTool(type);
                    if (resolved == null)
                        goto case Tool.None;

                    // Tool types can resolve to either global or instance tools
                    if (IsComponentTool(resolved))
                    {
                        var instance = EditorToolManager.GetComponentTool(resolved, true);
                        if (instance == null)
                        {
                            Debug.LogError($"{context} resolved Tool.{type} to a Component tool of type `{resolved}`, but " +
                                $"no component matching the target type is in the active selection. The active tool " +
                                $"context will be set to the default.");
                            EditorToolManager.activeToolContext = EditorToolManager.GetSingleton<GameObjectToolContext>();
                            return (EditorTool)EditorToolManager.GetSingleton(EditorToolManager.activeToolContext.ResolveTool(type));
                        }
                        else if (!instance.IsAvailable() || instance.isHidden)
                        {
                            Debug.LogError($"{context} resolved Tool.{type} to a Component tool of type `{resolved}`, but " +
                                           $"the matching component tool is not Available or is Hidden for the active selection. The active tool " +
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

        static Tool GetToolTypeInContext(EditorToolContext ctx, Type type)
        {
            for (int i = (int)Tool.Move; i < (int)Tool.Custom; i++)
            {
                if (ctx.ResolveTool((Tool)i) == type)
                    return (Tool)i;
            }

            return Tool.Custom;
        }

        internal static Tool GetEnumWithEditorTool(EditorTool tool, EditorToolContext ctx = null)
        {
            if (tool == null || tool is NoneTool)
                return Tool.None;

            if (tool is ViewModeTool)
                return Tool.View;

            var type = tool.GetType();

            return GetToolTypeInContext(ctx != null ? ctx : EditorToolManager.activeToolContext, type);
        }

        internal static bool IsManipulationTool(Tool tool)
        {
            return tool == Tool.Move
                || tool == Tool.Rotate
                || tool == Tool.Scale
                || tool == Tool.Rect
                || tool == Tool.Transform;
        }

        // In the current context, is this tool considered a built-in tool?
        // Built-in tools are the first category of tools in the toolbar, and are always available while their parent
        // context is active.
        internal static bool IsBuiltinOverride(EditorTool tool)
        {
            if (tool == null)
                return false;
            if (IsManipulationTool(GetEnumWithEditorTool(tool)))
                return true;
            var type = tool.GetType();
            foreach(var extra in EditorToolManager.activeToolContext.GetAdditionalToolTypes())
                if (type == extra)
                    return true;
            return false;
        }

        internal static bool IsComponentTool(Type type)
        {
            return s_ToolCache.GetTargetType(type) != null;
        }

        internal static bool IsGlobalTool(EditorTool tool)
        {
            if(GetEnumWithEditorTool(tool) == Tool.Custom)
            {
                var type = tool.GetType();
                return !IsComponentTool(type)   // Component tool?
                    && !IsManipulationTool(GetEnumWithEditorTool(tool, EditorToolManager.GetSingleton<GameObjectToolContext>())) // Built-in tool?
                    && !IsBuiltinOverride(tool) // Built-in tool override?
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    && EditorToolManager.additionalContextToolTypesCache.Any(t => t == type); // Additional/Extra tool?
#pragma warning restore RS0030
            }

            return false;
        }

        internal static GUIContent GetToolbarIcon<T>(T obj) where T : IEditor
        {
            if (obj == null)
                return GetIcon(typeof(T));
            if (obj is EditorTool tool && tool.toolbarIcon != null)
                return tool.toolbarIcon;
            return GetIcon(obj.GetType());
        }

        internal static GUIContent GetIcon(Type editorToolType, bool forceReload = false)
        {
            GUIContent res;

            if (forceReload)
                s_ToolbarIcons.Remove(editorToolType);

            if (s_ToolbarIcons.TryGetValue(editorToolType, out res))
                return res;

            res = new GUIContent() { tooltip = GetToolName(editorToolType) };

            // First check if the tool as an icon attribute
            var iconPath = EditorGUIUtility.GetIconPathFromAttribute(editorToolType);
            if(!string.IsNullOrEmpty(iconPath) && (res.image = EditorGUIUtility.IconContent(iconPath, false).image))
                goto ReturnToolbarIcon;

            // Second check for the tool type itself
            if(( res.image = EditorGUIUtility.FindTexture(editorToolType) ) != null)
                goto ReturnToolbarIcon;

            // And finally fall back to the significant letters of the tool's typename
            res.text = OverlayUtilities.GetSignificantLettersForIcon(editorToolType.Name);

        ReturnToolbarIcon:
            if (string.IsNullOrEmpty(res.tooltip))
                res.tooltip = ObjectNames.NicifyVariableName(editorToolType.Name);

            s_ToolbarIcons.Add(editorToolType, res);

            return res;
        }
        
        internal static GUIContent GetContextIcon(Type editorToolContextType, out bool isFallbackIcon)
        {
            GUIContent icon;
            isFallbackIcon = false;
            icon = GetIcon(editorToolContextType, true);
            if (icon.image == null)
            {
                icon.image = EditorGUIUtility.IconContent("ToolContext").image;
                isFallbackIcon = true;
            }

            return icon;
        }

        internal static EditorTypeAssociation GetMetaData(Type toolType) => s_ToolCache.GetMetaData(toolType);

        internal static List<EditorTypeAssociation> GetEditorsForVariant(EditorTypeAssociation type) => s_ToolCache.GetEditorsForVariant(type);
    }
}
