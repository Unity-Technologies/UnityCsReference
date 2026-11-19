// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UObject = UnityEngine.Object;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.EditorTools
{
    public static partial class ToolManager
    {
        public static Type activeContextType => EditorToolManager.activeToolContext.GetType();

        public static void SetActiveContext(Type context)
        {
            SetActiveContext(context, typeof(SceneView));
        }
        
        internal static void SetActiveContext(Type context, Type contextOwner)
        {
            if (contextOwner == null)
                contextOwner = typeof(SceneView);
            
            if (context != null && (!typeof(EditorToolContext).IsAssignableFrom(context) || context.IsAbstract))
                throw new ArgumentException("Type must be assignable to EditorToolContext, and not abstract.", "context");

            var defaultContext = EditorToolUtility.GetDefaultContextType(contextOwner);
            var ctx = context != null ? context : defaultContext;

            if (!EditorToolUtility.IsContextSupported(ctx, contextOwner))
            {
                Debug.LogWarning($"Tool context {ctx} is not supported in the current stage.");
                return;
            }

            if (EditorToolUtility.IsComponentEditor(ctx) && ctx != defaultContext)
            {
                var instance = EditorToolManager.GetComponentContext(ctx, toolOwner:contextOwner, true);
                if (instance == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable " +
                        $"by the component tool of type: {context}");
                
                EditorToolManager.SetActiveToolContext(instance, contextOwner);
            }
            else
            {
                var ownerToolsState = EditorToolManager.instance.GetOrCreateStateForType(contextOwner);
                if (ownerToolsState != null)
                {
                    var ctxInstance = ownerToolsState.GetSingleton(ctx) as EditorToolContext;
                    EditorToolManager.SetActiveToolContext(ctxInstance, contextOwner);
                }
            }
        }

        public static void SetActiveContext<T>() where T : EditorToolContext
        {
            SetActiveContext(typeof(T));
        }

        internal static void SetActiveContext<T>(Type toolOwnerType) where T : EditorToolContext
        {
            SetActiveContext(typeof(T), toolOwnerType);
        }

        internal static bool CanSetActiveContext(Type context, Type contextOwner = null)
        {
            if (contextOwner == null)
                contextOwner = typeof(SceneView);

            if (context == null || !typeof(EditorToolContext).IsAssignableFrom(context) || context.IsAbstract)
                return false;

            if (!EditorToolUtility.IsContextSupported(context, contextOwner))
                return false;

            if (!EditorToolUtility.IsComponentEditor(context) || context == EditorToolUtility.GetDefaultContextType(contextOwner))
                return true;

            return EditorToolManager.GetComponentContext(context, toolOwner: contextOwner, true) != null;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool CanSetActiveContext<T>() where T : EditorToolContext
            => CanSetActiveContext(typeof(T));

        internal readonly struct StageToolContextOverride
        {
            public Type defaultContext { get; }
            public Type[] supportedContexts { get; }

            public StageToolContextOverride(Type defaultContext, Type[] supportedContexts)
            {
                this.defaultContext = defaultContext;
                this.supportedContexts = supportedContexts;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Stage tool context overrides are registered again by their owners on the next code load")]
        static Dictionary<Type, StageToolContextOverride> s_StageToolContextOverrides = new();

        /// <summary>
        /// Overrides the Scene view's default and supported tool contexts while a stage of the given type is the current stage.
        /// </summary>
        /// <param name="stageType">The stage type to override, matched exactly. Must derive from Stage, not be abstract, and not be MainStage.</param>
        /// <param name="defaultContext">The context that becomes the default in the stage.</param>
        /// <param name="supportedContexts">Contexts allowed in addition to the default. Null allows all contexts, an empty array allows only the default.</param>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void RegisterStageToolContextOverride(Type stageType, Type defaultContext, Type[] supportedContexts)
        {
            if (stageType == null || !typeof(Stage).IsAssignableFrom(stageType) || stageType.IsAbstract)
                throw new ArgumentException("Type must be assignable to Stage, and not abstract.", nameof(stageType));

            if (stageType == typeof(MainStage))
                throw new ArgumentException("The tool contexts of the main stage cannot be overridden.", nameof(stageType));

            if (defaultContext == null || !typeof(EditorToolContext).IsAssignableFrom(defaultContext) || defaultContext.IsAbstract)
                throw new ArgumentException("Type must be assignable to EditorToolContext, and not abstract.", nameof(defaultContext));

            if (supportedContexts != null)
            {
                foreach (var supportedContext in supportedContexts)
                {
                    if (supportedContext == null || !typeof(EditorToolContext).IsAssignableFrom(supportedContext) || supportedContext.IsAbstract)
                        throw new ArgumentException("Types must be assignable to EditorToolContext, and not abstract.", nameof(supportedContexts));
                }
            }

            if (s_StageToolContextOverrides.ContainsKey(stageType))
                throw new InvalidOperationException($"A tool context override is already registered for {stageType}.");

            s_StageToolContextOverrides.Add(stageType, new StageToolContextOverride(defaultContext, supportedContexts));
            RefreshStageToolContextsIfCurrent(stageType);
        }

        /// <summary>
        /// Removes the tool context override registered for the given stage type.
        /// </summary>
        /// <param name="stageType">The stage type whose override to remove.</param>
        internal static void UnregisterStageToolContextOverride(Type stageType)
        {
            if (stageType != null && s_StageToolContextOverrides.Remove(stageType))
                RefreshStageToolContextsIfCurrent(stageType);
        }

        static void RefreshStageToolContextsIfCurrent(Type stageType)
        {
            var currentStage = StageNavigationManager.instance.currentStage;
            if (currentStage != null && currentStage.GetType() == stageType)
                EditorToolManager.RefreshStageToolContexts();
        }

        internal static bool TryGetCurrentStageToolContextOverride(out StageToolContextOverride stageOverride)
        {
            stageOverride = default;

            if (s_StageToolContextOverrides.Count == 0)
                return false;

            var currentStage = StageNavigationManager.instance.currentStage;
            if (currentStage == null)
                return false;

            return s_StageToolContextOverrides.TryGetValue(currentStage.GetType(), out stageOverride);
        }
        
        internal static Type GetActiveToolType(Type toolOwnerType)
        {
            EditorTool tool = null;
            var state = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (state != null) 
                tool = state.activeTool;
            return tool != null ? tool.GetType() : null;
        }
        
        internal static Type GetActiveContextType(Type contextOwnerType)
        {
            var ctx = EditorToolManager.GetActiveToolContext(contextOwnerType);
            return ctx != null ? ctx.GetType() : null;
        }
        
        public static Type activeToolType
        {
            get
            {
                var tool = EditorToolManager.activeTool;
                return tool != null ? tool.GetType() : null;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        public static event Action activeToolChanging;

        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<Type> activeToolChangingForOwner;

        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        public static event Action activeToolChanged;
        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        internal static event Action<Type> activeToolChangedForOwner;

        [AutoStaticsCleanupOnCodeReload]
        public static event Action activeContextChanging;
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<Type> activeContextChangingForOwner;

        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        public static event Action activeContextChanged;
        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        internal static event Action<Type> activeContextChangedForOwner;

        internal static void ActiveToolWillChange()
        {
            ActiveToolWillChange(typeof(SceneView));
        }

        internal static void ActiveToolWillChange(Type toolOwnerType)
        {
            if (toolOwnerType == null)
                toolOwnerType = typeof(SceneView);

            if (toolOwnerType == typeof(SceneView))
            {
                if (activeToolChanging != null)
                    activeToolChanging();
#pragma warning disable 618
                EditorTools.ActiveToolWillChange();
#pragma warning restore 618
            }
            
            activeToolChangingForOwner?.Invoke(toolOwnerType);
        }

        internal static void ActiveToolDidChange()
        {
            ActiveToolDidChange(typeof(SceneView));
        }
        
        internal static void ActiveToolDidChange(Type toolOwnerType)
        {
            if (toolOwnerType == null)
                toolOwnerType = typeof(SceneView);
            
            if (toolOwnerType == typeof(SceneView))
            {
                if (activeToolChanged != null)
                    activeToolChanged();
#pragma warning disable 618
            EditorTools.ActiveToolDidChange();
#pragma warning restore 618
            }
         
            activeToolChangedForOwner?.Invoke(toolOwnerType);
        }

        internal static void ActiveContextWillChange()
        {
            ActiveContextWillChange(typeof(SceneView));
        }

        internal static void ActiveContextWillChange(Type contextOwnerType)
        {
            if (contextOwnerType == null)
                contextOwnerType = typeof(SceneView);

            if (contextOwnerType == typeof(SceneView))
            {
                if (activeContextChanging != null)
                    activeContextChanging();
            }
            
            activeContextChangingForOwner?.Invoke(contextOwnerType);
        }

        internal static void ActiveContextDidChange()
        {
            ActiveContextDidChange(typeof(SceneView));
        }

        internal static void ActiveContextDidChange(Type contextOwnerType)
        {
            if (contextOwnerType == null)
                contextOwnerType = typeof(SceneView);

            if (contextOwnerType == typeof(SceneView))
            {
                if (activeContextChanged != null)
                    activeContextChanged();
            }
            
            activeContextChangedForOwner?.Invoke(contextOwnerType);
        }

        public static void SetActiveTool<T>() where T : EditorTool
        {
            SetActiveTool(typeof(T));
        }

        public static void SetActiveTool(Type type)
        {
            SetActiveTool(type, typeof(SceneView));
        }

        public static void SetActiveTool(EditorTool tool)
        {
            EditorToolManager.activeTool = tool;
        }

        internal static void SetActiveTool<T>(Type toolOwnerType) where T : EditorTool
        {
            SetActiveTool(typeof(T), toolOwnerType);
        }

        internal static void SetActiveTool(Type type, Type toolOwnerType)
        {
            if (!typeof(EditorTool).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException("Type must be assignable to EditorTool, and not abstract.");

            if (EditorToolUtility.IsComponentEditor(type))
            {
                var tool = EditorToolManager.GetComponentTool(type, toolOwnerType);

                if (tool == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable " +
                                                        $"by the component tool of type: {type}");
                if (!tool.IsAvailable() || tool.isHidden)
                    throw new InvalidOperationException($"Cannot activate {type} tool because it is currently not " +
                                                        "available (either the tool's IsAvailable() method returned " +
                                                        "false or IsHidden() method returned true).");
                SetActiveTool(tool, toolOwnerType);
                return;
            }

            SetActiveTool((EditorTool)EditorToolManager.GetSingleton(type, toolOwnerType), toolOwnerType);
        }
        
        internal static void SetActiveTool(EditorTool tool, Type toolOwnerType)
        {
            EditorToolManager.SetActiveTool(tool, toolOwnerType);
        }

        public static void RestorePreviousTool()
        {
            RestorePreviousTool(typeof(SceneView));
        }
        
        internal static void RestorePreviousTool(Type toolOwnerType)
        {
            var ownerState = EditorToolManager.GetEditorToolStateForOwner(toolOwnerType);
            if (ownerState != null)
            {
                var prev = EditorToolUtility.GetEditorToolWithEnum(ownerState.previousTool, toolOwnerType);
                if (!(prev is NoneTool))
                    ownerState.activeTool = prev;
            }
        }

        public static void RestorePreviousPersistentTool() => EditorToolManager.RestorePreviousPersistentTool();

        internal static void RestorePreviousPersistentTool(Type toolOwnerType) => EditorToolManager.RestorePreviousPersistentTool(toolOwnerType);

        public static bool IsActiveTool(EditorTool tool)
        {
            return EditorToolManager.activeTool == tool;
        }

        internal static bool IsActiveTool(EditorTool tool, Type toolOwnerType)
        {
            return EditorToolManager.GetActiveTool(toolOwnerType) == tool;
        }

        // Used in tests
        internal static IEnumerable<Type> allContextsExceptGameObject
        {
            get
            {
                foreach(var ctx in EditorToolManager.componentContexts)
                    yield return ctx.editorType;

                foreach(var ctx in EditorToolUtility.availableGlobalToolContexts)
                    if (ctx.editor != typeof(GameObjectToolContext))
                        yield return ctx.editor;
            }
        }

        public static void RefreshAvailableTools()
        {
            RefreshAvailableTools(typeof(SceneView));
        }

        internal static void RefreshAvailableTools(Type toolOwnerType)
        {
            if (toolOwnerType == typeof(SceneView))
            {
                foreach (var obj in SceneView.sceneViews)
                {
                    if (!(obj is SceneView scene))
                        continue;
                    RebuildToolsOverlay(scene);
                }
            }
            else
            {
                var toolOwnerWndObjects = UObject.FindObjectsByType(toolOwnerType);
                foreach (var obj in toolOwnerWndObjects)
                {
                    if (!(obj is EditorWindow toolOwnerWindow))
                        continue;
                    RebuildToolsOverlay(toolOwnerWindow);
                }
            }

            void RebuildToolsOverlay(EditorWindow toolOwnerWindow)
            {
#pragma warning disable UAC2001 // Avoid Linq
                var overlay = toolOwnerWindow.overlayCanvas?.overlays.FirstOrDefault(x => x is TransformToolsOverlayToolBar);
#pragma warning restore UAC2001
                if (overlay != null)
                    overlay.RebuildContent();
            }
        }

        public static bool IsActiveContext(EditorToolContext context)
        {
            return EditorToolManager.activeToolContext == context;
        }

        [Shortcut("Tools/Enter Default Tool Mode", typeof(ToolShortcutContext))]
        internal static void ExitToolContext()
        {
            var toolsOwnerType = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            SetActiveContext(EditorToolUtility.GetDefaultContextType(toolsOwnerType), toolsOwnerType);
        }

        [Shortcut("Tools/Cycle Tool Modes", typeof(ToolShortcutContext), KeyCode.G)]
        internal static void CycleToolContexts()
        {
            var toolsOwnerType = EditorToolUtility.GetToolOwnerFromFocusedWindow();
            if (EditorToolUtility.GetToolContextsInProject(toolsOwnerType) < 2)
                return;

            var active = EditorToolManager.GetActiveToolContext(toolsOwnerType);
            var state = EditorToolManager.GetEditorToolStateForOwner(toolsOwnerType);
            if (state == null)
                return;
            
            var sortedAvailableCtxs = state.sortedContextsDataCache.allAvailableContextAssociations;

            var ctxIdx = 0;
            if (sortedAvailableCtxs.Count <= 1)
                return;

            // Iterate over context associations until we reach the currently active type
            while (sortedAvailableCtxs[ctxIdx].editor != active.GetType())
            {
                // If we've reached end of associations list, the active context is not registered with an attribute.
                // We'll just return the last available context in that case.
                if (++ctxIdx == sortedAvailableCtxs.Count)
                {
                    SetActiveContext(sortedAvailableCtxs[^1].editor, toolsOwnerType);
                    return;
                }
            }

            // If we can advance from the active context, that is the next context. If not, we're at the end of the list
            // and need to circle back around to the first available context.
            if (++ctxIdx < sortedAvailableCtxs.Count)
                SetActiveContext(sortedAvailableCtxs[ctxIdx].editor, toolsOwnerType);
            else
            {
                SetActiveContext(sortedAvailableCtxs[0].editor, toolsOwnerType);
            }
        }
    }
}
