// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.EditorTools
{
    public interface ISupportsEditorTools : ISupportsOverlays
    {
        public Camera handlesCamera { get; }
    }

    static class EditorToolUtility
    {
        static readonly Regex k_NewLine = new Regex(@"\r|\n", RegexOptions.Compiled | RegexOptions.Multiline);
        static readonly Regex k_TrailingForwardSlashOrWhiteSpace = new Regex(@"[/|\s]*\Z", RegexOptions.Compiled);

        static Dictionary<Type, EditorToolCache> s_ToolCache = new();
        static Dictionary<Type, EditorToolCache> s_ContextCache = new();
        static Dictionary<Type, List<EditorTypeAssociation>> s_EditorToolAssociations;
        static Dictionary<Type, List<EditorTypeAssociation>> s_EditorToolContextAssociations;

        static Dictionary<Type, GUIContent> s_ToolbarIcons = new();
        static Dictionary<Type, ToolOwnerDefinition> s_ToolOwnerDefinitions = new();

        internal readonly struct ToolOwnerDefinition
        {
            public Type toolOwnerType { get; }
            public Type defaultContext { get; }

            public ToolOwnerDefinition(Type toolOwnerType, EditorToolOwnerAttribute attribute)
            {
                this.toolOwnerType = toolOwnerType;
                defaultContext = attribute.defaultContext ?? typeof(GameObjectToolContext);
            }
        }

        static EditorToolCache GetContextCache(Type toolOwner)
        {
            if (!IsRegisteredToolOwner(toolOwner))
                return null;

            if (s_ContextCache == null)
                s_ContextCache = new();

            if (!s_ContextCache.TryGetValue(toolOwner, out var contextCache))
            {
                contextCache = new EditorToolCache(typeof(EditorToolContextAttribute), toolOwner);
                s_ContextCache.Add(toolOwner, contextCache);
            }

            return contextCache;
        }

        static EditorToolCache GetToolCache(Type toolOwner)
        {
            if (!IsRegisteredToolOwner(toolOwner))
                return null;

            if (s_ToolCache == null)
                s_ToolCache = new();

            if (!s_ToolCache.TryGetValue(toolOwner, out var toolCache))
            {
                toolCache = new EditorToolCache(typeof(EditorToolAttribute), toolOwner);
                s_ToolCache.Add(toolOwner, toolCache);
            }

            return toolCache;
        }

        internal static bool GetToolOwnerDefinition(Type toolOwner, out ToolOwnerDefinition toolOwnerDefinition)
        {
            toolOwnerDefinition = default;

            if (s_ToolOwnerDefinitions == null || s_ToolOwnerDefinitions.Count == 0)
                InitializeToolOwnerDefinitions();

            if (s_ToolOwnerDefinitions == null)
                return false;

            return s_ToolOwnerDefinitions.TryGetValue(toolOwner, out toolOwnerDefinition);
        }

        internal static bool IsRegisteredToolOwner(Type type)
        {
            return GetToolOwnerDefinition(type, out _);
        }

        static bool CreateDefinitionForOwner(Type toolOwner, out ToolOwnerDefinition toolOwnerDefinition)
        {
            toolOwnerDefinition = default;
            var attribs = toolOwner.GetCustomAttributes(typeof(EditorToolOwnerAttribute), false);
            for (int i = 0; i < attribs.Length; ++i)
            {
                if (attribs[i] is EditorToolOwnerAttribute toolOwnerAttrib)
                {
                    toolOwnerDefinition = new ToolOwnerDefinition(toolOwner, toolOwnerAttrib);
                    return true;
                }
            }

            return false;
        }

        [InitializeOnLoadMethod]
        static void InitializeEditorTypeAssociations()
        {
            s_EditorToolAssociations = new Dictionary<Type, List<EditorTypeAssociation>>();
            s_EditorToolContextAssociations = new Dictionary<Type, List<EditorTypeAssociation>>();

            // Initialize tool owner definitions
            if (s_ToolOwnerDefinitions == null || s_ToolOwnerDefinitions.Count == 0)
                InitializeToolOwnerDefinitions();

            // Initialize associations lists for each owner
            foreach (var ownerType in s_ToolOwnerDefinitions.Keys)
            {
                s_EditorToolAssociations[ownerType] = new List<EditorTypeAssociation>();
                s_EditorToolContextAssociations[ownerType] = new List<EditorTypeAssociation>();
            }

            // Process and distribute associations for each owner in a single pass
            BuildToolAssociations(s_EditorToolAssociations);
            BuildToolContextAssociations(s_EditorToolContextAssociations);
        }

        static void InitializeToolOwnerDefinitions()
        {
            if (s_ToolOwnerDefinitions == null)
                s_ToolOwnerDefinitions = new();
            else
                s_ToolOwnerDefinitions.Clear();

            var typesWithAttrib = TypeCache.GetTypesWithAttribute<EditorToolOwnerAttribute>();
            foreach (var toolOwnerType in typesWithAttrib)
            {
                if (toolOwnerType != typeof(SceneView))
                {
                    if (!typeof(EditorWindow).IsAssignableFrom(toolOwnerType) || toolOwnerType.IsAbstract)
                    {
                        Debug.LogError($"Tool owner type {toolOwnerType} must be assignable to EditorWindow and must not be abstract.");
                        continue;
                    }

                    var ownerAttributes = toolOwnerType.GetCustomAttributes(typeof(EditorToolOwnerAttribute), true);
                    var attributeFound = false;
                    var attributeInvalid = false;
                    foreach (var ownerAttribute in ownerAttributes)
                    {
                        if (ownerAttribute is EditorToolOwnerAttribute attrib)
                        {
                            attributeFound = true;
                            if (attrib.defaultContext == null ||
                                attrib.defaultContext.IsAbstract ||
                                attrib.defaultContext == typeof(GameObjectToolContext) ||
                                !typeof(EditorToolContext).IsAssignableFrom(attrib.defaultContext))
                            {
                                Debug.LogError($"Tool owner type {toolOwnerType} has an invalid type set as the defaultContext in its EditorToolsOwner attribute." +
                                               "The defaultContext must be a non-abstract EditorToolContext, not GameObjectToolContext, and not null.");
                                attributeInvalid = true;
                            }
                            // check that the default context targets this owner
                            else if (!IsDefaultContextTargetingOwner(attrib.defaultContext, toolOwnerType))
                            {
                                Debug.LogError($"Tool owner type {toolOwnerType} has a defaultContext ({attrib.defaultContext}) " +
                                               $"that does not target it. The defaultContext must have an [EditorToolContext] attribute " +
                                               $"with targetOwner set to {toolOwnerType}.");
                                attributeInvalid = true;
                            }
                            break;
                        }
                    }

                    if (!attributeFound || attributeInvalid)
                        continue;
                }

                if (CreateDefinitionForOwner(toolOwnerType, out var ownerDef))
                    s_ToolOwnerDefinitions.Add(toolOwnerType, ownerDef);
                else
                    Debug.LogError($"Could not create tool owner definition for {toolOwnerType}." );
            }
        }

        static void BuildToolContextAssociations(Dictionary<Type, List<EditorTypeAssociation>> ownerLists)
        {
            // Walk TypeCache for EditorToolContext
            var allContextTypes = TypeCache.GetTypesWithAttribute<EditorToolContextAttribute>();
            foreach (var contextType in allContextTypes)
            {
                // Skip abstract types
                if (contextType.IsAbstract)
                    continue;

                // Get the context's target owner
                var attrs = contextType.GetCustomAttributes(typeof(EditorToolContextAttribute), false);
                var contextAttrib = attrs.Length > 0 ? (EditorToolContextAttribute)attrs[0] : null;
                var contextTargetOwner = contextAttrib?.targetToolOwner;

                // Determine which owner this context belongs to
                Type ownerForContext;
                if (contextTargetOwner == null || contextTargetOwner == typeof(SceneView))
                    ownerForContext = typeof(SceneView);
                else
                    ownerForContext = contextTargetOwner;

                // Add to the appropriate owner's list (if owner exists)
                if (ownerLists.TryGetValue(ownerForContext, out var list))
                {
                    var association = new EditorTypeAssociation(contextType, typeof(EditorToolContextAttribute));
                    list.Add(association);
                }
            }
        }

        static void BuildToolAssociations(Dictionary<Type, List<EditorTypeAssociation>> ownerLists)
        {
            // Walk TypeCache for EditorTool
            var allToolTypes = TypeCache.GetTypesWithAttribute<EditorToolAttribute>();
            foreach (var toolType in allToolTypes)
            {
                // Skip abstract types
                if (toolType.IsAbstract)
                    continue;

                var toolAttribs = toolType.GetCustomAttributes(typeof(EditorToolAttribute), false);
                var toolAttrib = toolAttribs.Length > 0 ? (EditorToolAttribute)toolAttribs[0] : null;
                if (toolAttrib == null)
                    continue;

                // Determine which owner this tool belongs to
                Type ownerForTool;
                if (toolAttrib.targetContext != null)
                {
                    // If tool targets a specific context, look at that context's target owner
                    var ctxAttrs = toolAttrib.targetContext.GetCustomAttributes(typeof(EditorToolContextAttribute), false);
                    var contextTargetOwner = ctxAttrs.Length > 0 ? ((EditorToolContextAttribute)ctxAttrs[0]).targetToolOwner : null;

                    // Component tools targeting non-SceneView contexts are invalid
                    if (toolAttrib.targetType != null && contextTargetOwner != null && contextTargetOwner != typeof(SceneView))
                    {
                        Debug.LogError($"{toolType} is declared as a component EditorTool (EditorTool attribute specifies targetType {toolAttrib.targetType})" +
                        $"but targets a tool context {toolAttrib.targetContext}, which is registered for a non-SceneView tool owner {contextTargetOwner}. " +
                        "This component EditorTool will be ignored as component tools are only available in the Scene View window. " +
                            "To prevent this, please change the tool context's owner target type to null or SceneView " +
                            "(alternatively, change the tool from component to global by setting its target to null).");
                        continue;
                    }

                    // Determine owner based on context's target
                    if (contextTargetOwner == null || contextTargetOwner == typeof(SceneView))
                        ownerForTool = typeof(SceneView);
                    else
                        ownerForTool = contextTargetOwner;
                }
                else
                    ownerForTool = typeof(SceneView);

                // Add to the appropriate owner's list (if owner exists)
                if (ownerLists.TryGetValue(ownerForTool, out var list))
                {
                    var association = new EditorTypeAssociation(toolType, typeof(EditorToolAttribute));
                    list.Add(association);
                }
            }
        }

        internal static List<EditorTypeAssociation> GetEditorToolAssociations(Type ownerType)
        {
            if (s_EditorToolAssociations == null)
                InitializeEditorTypeAssociations();

            if (s_EditorToolAssociations.TryGetValue(ownerType, out var associations))
                return associations;

            return null;
        }

        internal static List<EditorTypeAssociation> GetEditorToolContextAssociations(Type ownerType)
        {
            if (s_EditorToolContextAssociations == null)
                InitializeEditorTypeAssociations();

            if (s_EditorToolContextAssociations.TryGetValue(ownerType, out var associations))
                return associations;

            return null;
        }

        internal static IEnumerable<ToolOwnerDefinition> allToolOwnerDefinitions
        {
            get
            {
                if (s_ToolOwnerDefinitions == null || s_ToolOwnerDefinitions.Count == 0)
                    InitializeToolOwnerDefinitions();

                return s_ToolOwnerDefinitions.Values;
            }
        }

        internal static IEnumerable<EditorTypeAssociation> availableGlobalToolContexts
        {
            get => GetContextCache(typeof(SceneView)).GetEditorsForTargetType(null);
        }

        internal static IEnumerable<EditorTypeAssociation> GetAvailableGlobalToolContexts(Type toolOwner)
        {
            var cache = GetContextCache(toolOwner);
            return cache != null ? cache.GetEditorsForTargetType(null) : Array.Empty<EditorTypeAssociation>();
        }

        internal static IEnumerable<EditorTypeAssociation> registeredToolContexts
        {
            get => GetContextCache(typeof(SceneView)).availableEditorTypeAssociations;
        }

        internal static IEnumerable<EditorTypeAssociation> GetRegisteredToolContexts(Type toolOwner)
        {
            var cache = GetContextCache(toolOwner);
            return cache != null ? cache.availableEditorTypeAssociations : Array.Empty<EditorTypeAssociation>();
        }

        internal static IEnumerable<EditorTypeAssociation> availableEditorTools
        {
            get => GetToolCache(typeof(SceneView)).availableEditorTypeAssociations;
        }

        internal static IEnumerable<EditorTypeAssociation> GetAvailableEditorTools(Type toolOwner)
        {
            var cache = GetToolCache(toolOwner);
            return cache != null ? cache.availableEditorTypeAssociations : Array.Empty<EditorTypeAssociation>();
        }

        internal static int GetToolContextsInProject(Type toolOwnerType)
        {
            var cache = GetContextCache(toolOwnerType);
            return cache != null ? cache.Count : 0;
        }

        internal class SortedContextDataCache
        {
            List<EditorTypeAssociation> m_SortedGlobalContextAssociations = new();
            List<EditorTypeAssociation> m_SortedAvailableCompContextAssoc = new();
            List<EditorTypeAssociation> m_SortedUnavailableCompContextAssoc = new();
            readonly List<EditorTypeAssociation> m_SortedAllAvailableContextAssoc = new();

            EditorToolManager.EditorToolState m_EditorToolState;

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

            public SortedContextDataCache(EditorToolManager.EditorToolState editorToolState)
            {
                m_EditorToolState = editorToolState;
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
                var globalContexts = new List<EditorTypeAssociation>(GetAvailableGlobalToolContexts(m_EditorToolState.stateToolOwnerType));

                globalContexts.Sort(sortComp);

                // Move GO context to front of globals
                for (int i = globalContexts.Count - 1; i >= 0; --i)
                {
                    if (globalContexts[i].editor == m_EditorToolState.defaultToolContextType)
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
                foreach (var assoc in GetRegisteredToolContexts(m_EditorToolState.stateToolOwnerType))
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

        internal static SortedContextDataCache sortedContextsDataCache => EditorToolManager.instance.defaultState.sortedContextsDataCache;

        // Caution: Returns all types without filtering for EditorToolContext
        internal static IEnumerable<EditorTypeAssociation> GetCustomEditorToolsForType(Type type, Type toolOwner)
        {
            return GetToolCache(toolOwner).GetEditorsForTargetType(type);
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
                var toolAttribute = tool.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault();
                if (toolAttribute is ToolAttribute attrib && !string.IsNullOrEmpty(attrib.displayName))
                {
                    string path = SanitizeToolPath(attrib.displayName);
                    if (!string.IsNullOrEmpty(path))
                        return L10n.Tr(path);
                }
            }
            else if (typeof(EditorToolContext).IsAssignableFrom(tool))
            {
                var editorToolAttribute = tool.GetCustomAttributes(typeof(EditorToolContextAttribute), false).FirstOrDefault();
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

            return (EditorToolAttribute)type.GetCustomAttributes(typeof(EditorToolAttribute), false).FirstOrDefault();
        }

        internal static int GetNonBuiltinToolCount(Type toolOwner)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var globalEditorTools = GetCustomEditorToolsForType(null, toolOwner).Where(t => EditorToolManager.additionalContextToolTypesCache.TrueForAll(tc => tc != t.editor));
#pragma warning restore UA2001
#pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return globalEditorTools.Count();
#pragma warning restore UA2005
        }

        internal static bool IsComponentEditor(Type type)
        {
            if (type.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() is ToolAttribute attrib)
                return attrib.targetType != null;
            return false;
        }

        public static void InstantiateComponentContexts(List<ComponentEditor> editors, Type toolOwner)
        {
            GetContextCache(toolOwner).InstantiateEditors(null, editors);
        }

        public static void InstantiateComponentTools(EditorToolContext ctx, List<ComponentEditor> editors, Type toolOwner)
        {
            GetToolCache(toolOwner).InstantiateEditors(ctx, editors);
        }

        // Get an EditorTool instance for type of tool enum. This will return an instance of NoneTool if the active
        // context does not resolve to a valid tool.

        internal static EditorTool GetEditorToolWithEnum(Tool type, EditorToolContext ctx = null)
        {
            return GetEditorToolWithEnum(type, typeof(SceneView), ctx);
        }

        internal static EditorTool GetEditorToolWithEnum(Tool type, Type toolOwner, EditorToolContext ctx = null)
        {
            EditorTool DoResolveTool(EditorToolContext context, EditorToolManager.EditorToolState ownerState)
            {
                var resolved = context.ResolveTool(type);
                if (resolved == null)
                    return ownerState.GetSingleton<NoneTool>();

                // Tool types can resolve to either global or instance tools
                if (IsComponentTool(resolved, toolOwner))
                {
                    var instance = EditorToolManager.GetComponentTool(resolved, toolOwner, true);
                    if (instance == null)
                    {
                        Debug.LogError($"{context} resolved Tool.{type} to a Component tool of type `{resolved}`, but " +
                                       $"no component matching the target type is in the active selection. The active tool " +
                                       $"context will be set to the default.");
                        ownerState.activeToolContext = ownerState.GetSingleton(ownerState.defaultToolContextType) as EditorToolContext;
                        return (EditorTool)ownerState.GetSingleton(ownerState.activeToolContext.ResolveTool(type));
                    }

                    if (!instance.IsAvailable() || instance.isHidden)
                    {
                        Debug.LogError($"{context} resolved Tool.{type} to a Component tool of type `{resolved}`, but " +
                                       $"the matching component tool is not Available or is Hidden for the active selection. The active tool " +
                                       $"context will be set to the default.");
                        ownerState.activeToolContext = ownerState.GetSingleton(ownerState.defaultToolContextType) as EditorToolContext;
                        return (EditorTool)ownerState.GetSingleton(ownerState.activeToolContext.ResolveTool(type));
                    }

                    return instance;
                }

                // EditorToolContext.ResolveTool does type validation, so a fast cast is safe here.
                return (EditorTool)ownerState.GetSingleton(resolved);
            }

            var context = (ctx == null ? EditorToolManager.GetActiveToolContext(toolOwner) : ctx);
            var ownerState = EditorToolManager.instance.GetOrCreateStateForType(toolOwner);
            if (ownerState != null)
            {
                switch (type)
                {
                    case Tool.View:
                        if (toolOwner == null || toolOwner == typeof(SceneView))
                            return (EditorTool)ownerState.GetSingleton(typeof(ViewModeTool));

                        var resolvedTool = DoResolveTool(context, ownerState);
                        if (resolvedTool == null || resolvedTool is NoneTool)
                            return (EditorTool)ownerState.GetSingleton(typeof(ViewModeTool));

                        return resolvedTool;
                    case Tool.Custom:
                        return ownerState.lastCustomTool;
                    case Tool.None:
                        return ownerState.GetSingleton<NoneTool>();
                    default:
                        return DoResolveTool(context, ownerState);
                }
            }

            return null;
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

        internal static bool IsManipulationToolType(Type toolType)
        {
            return toolType == typeof(MoveTool)
                   || toolType == typeof(RotateTool)
                   || toolType == typeof(ScaleTool)
                   || toolType == typeof(RectTool)
                   || toolType == typeof(TransformTool);
        }

        internal static bool IsContextTargetOwnerMatchingGivenOwner(Type contextTargetOwner, Type toolOwner)
        {
            // For SV specifically, it matches if target owner is null or it's Scene View
            if (toolOwner == typeof(SceneView) && (contextTargetOwner == null || contextTargetOwner == typeof(SceneView)))
                return true;

            // For non-SV, it matches only if target owner is set to required type
            if (toolOwner != typeof(SceneView) && contextTargetOwner == toolOwner)
                return true;

            return false;
        }

        // Checks whether the default context declared by an [EditorToolOwner] attribute has a valid [EditorToolContext] attribute that targets the given owner
        static bool IsDefaultContextTargetingOwner(Type defaultContextType, Type toolOwnerType)
        {
            var contextAttributes = defaultContextType.GetCustomAttributes(typeof(EditorToolContextAttribute), false);
            if (contextAttributes.Length > 0 && contextAttributes[0] is EditorToolContextAttribute ctxAttrib)
                return IsContextTargetOwnerMatchingGivenOwner(ctxAttrib.targetToolOwner, toolOwnerType);

            return false;
        }

        internal static bool IsManipulationTool(EditorTool tool)
        {
            var toolEnum = GetEnumWithEditorTool(tool);
            return IsManipulationTool(toolEnum);
        }

        // In the current context, is this tool considered a built-in tool?
        // Built-in tools are the first category of tools in the toolbar, and are always available while their parent
        // context is active.
        internal static bool IsBuiltinOverride(EditorTool tool)
        {
            return IsBuiltinOverride(tool, EditorToolManager.activeToolContext);
        }

        internal static bool IsBuiltinOverride(EditorTool tool, EditorToolContext ctx)
        {
            if (tool == null)
                return false;
            if (IsManipulationTool(GetEnumWithEditorTool(tool, ctx)))
                return true;
            var type = tool.GetType();
            foreach(var extra in ctx.GetAdditionalToolTypes())
                if (type == extra)
                    return true;
            return false;
        }

        internal static bool IsComponentTool(Type type)
        {
            return GetToolCache(typeof(SceneView)).GetTargetType(type) != null;
        }

        internal static bool IsComponentTool(Type type, Type toolOwner)
        {
            return GetToolCache(toolOwner).GetTargetType(type) != null;
        }

        internal static bool IsGlobalTool(EditorTool tool, Type toolOwner)
        {
            if (GetEnumWithEditorTool(tool) == Tool.Custom)
            {
                var type = tool.GetType();
                var ownerState = EditorToolManager.instance.GetOrCreateStateForType(toolOwner);
                return ownerState != null
                       && IsComponentTool(type, toolOwner)   // Component tool?
                       && !IsManipulationTool(GetEnumWithEditorTool(tool, ownerState.GetSingleton(ownerState.defaultToolContextType) as EditorToolContext)) // Built-in tool?
                       && !IsBuiltinOverride(tool, ownerState.activeToolContext) // Built-in tool override?
                       && EditorToolManager.additionalContextToolTypesCache.Exists(t => t == type); // Additional/Extra tool?
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

        internal static EditorTypeAssociation GetMetaData(Type toolType) => GetToolCache(typeof(SceneView)).GetMetaData(toolType);

        internal static EditorTypeAssociation GetMetaData(Type toolType, Type toolOwner) => GetToolCache(toolOwner).GetMetaData(toolType);

        internal static List<EditorTypeAssociation> GetEditorsForVariant(EditorTypeAssociation type,  Type toolOwner) => GetToolCache(toolOwner).GetEditorsForVariant(type);

        internal static Type GetToolOwnerFromFocusedWindow()
        {
            var toolsOwner = typeof(SceneView);
            var focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null &&
                focusedWindow.GetType() != typeof(SceneView) &&
                IsRegisteredToolOwner(focusedWindow.GetType()))
            {
                toolsOwner = focusedWindow.GetType();
            }

            return toolsOwner;
        }

        internal static bool IsCustomEditorTool(EditorTool tool, Type toolOwner)
        {
            return IsComponentTool(tool != null ? tool.GetType() : null, toolOwner);
        }

        internal static bool IsCustomToolContext(EditorToolContext context)
        {
            return context != null && context.GetType() != typeof(GameObjectToolContext);
        }

        internal static Type ResolveToolOwnerType(Type cachedType, string typeName)
        {
            if (cachedType == null && !String.IsNullOrEmpty(typeName))
                cachedType = Type.GetType(typeName);

            if (cachedType == null)
                cachedType = typeof(SceneView);

            return cachedType;
        }

        internal static void OrderAvailableTools(List<ToolEntry> tools)
        {
            tools.Sort((a, b) =>
            {
                // Sort by scope first
                var result = a.scope.CompareTo(b.scope);
                if (result != 0)
                    return result;

                // For tool groups, ensure CreationToolGroups appears first
                if (a.scope == ToolEntry.Scope.Grouped && b.scope == ToolEntry.Scope.Grouped)
                {
                    if (a.group == typeof(CreationToolsGroup) && b.group != typeof(CreationToolsGroup))
                        return -1;
                    if (a.group != typeof(CreationToolsGroup) && b.group == typeof(CreationToolsGroup))
                        return 1;
                }

                // Ensure tools of same group stay adjacent
                result = String.Compare(a.group == null ? string.Empty : a.group.Name,
                                        b.group == null ? string.Empty : b.group.Name, StringComparison.Ordinal);
                if (result != 0)
                    return result;

                // Ensure tools targeting same components stay adjacent
                result = String.Compare(a.targetBehaviour == null ? string.Empty : a.targetBehaviour.Name,
                                        b.targetBehaviour == null ? string.Empty : b.targetBehaviour.Name, StringComparison.Ordinal);
                if (result != 0)
                    return result;

                // Sort by priority next
                result = a.priority.CompareTo(b.priority);
                if (result != 0)
                    return result;

                // Finally by hash code
                return a.GetHashCode().CompareTo(b.GetHashCode());
            });
        }

        internal static VisualElement CreateEditorToolsIMGUIContainer(EditorWindow window, Action onGUIHandler)
        {
            var container = new IMGUIContainer()
            {
                onGUIHandler = onGUIHandler,
                name = "EditorToolsIMGUIContainer",
                pickingMode = PickingMode.Position,
                viewDataKey = window.name,
                renderHints = RenderHints.ClipWithScissors,
                requireMeasureFunction = false
            };

            UIElementsEditorUtility.AddDefaultEditorStyleSheets(container);
            container.style.overflow = Overflow.Hidden;
            container.style.flexGrow = 1;

            return container;
        }
    }
}
