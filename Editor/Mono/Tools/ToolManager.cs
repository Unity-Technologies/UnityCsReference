// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public static class ToolManager
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

            var ctx = context != null ? context : typeof(GameObjectToolContext);

            if (EditorToolUtility.IsComponentEditor(ctx))
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

            if (!EditorToolUtility.IsComponentEditor(context))
                return true;

            return EditorToolManager.GetComponentContext(context, toolOwner: contextOwner, true) != null;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool CanSetActiveContext<T>() where T : EditorToolContext
            => CanSetActiveContext(typeof(T));
        
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

        public static event Action activeToolChanging;
        
        internal static event Action<Type> activeToolChangingForOwner;

        public static event Action activeToolChanged;
        internal static event Action<Type> activeToolChangedForOwner;

        public static event Action activeContextChanging;
        internal static event Action<Type> activeContextChangingForOwner;

        public static event Action activeContextChanged;
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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var overlay = toolOwnerWindow.overlayCanvas?.overlays.FirstOrDefault(x => x is TransformToolsOverlayToolBar);
#pragma warning restore UA2001
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
            if (EditorToolUtility.GetToolOwnerDefinition(toolsOwnerType, out var toolOwnerDef))
                SetActiveContext(toolOwnerDef.defaultContext, toolsOwnerType);
            else
                SetActiveContext(typeof(GameObjectToolContext), toolsOwnerType);
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

            ctxIdx++;
            if (active is GameObjectToolContext)
            {
                // GO ctx is always idx 0 in sorted context association list, therefore if it's active, we can immediately cycle into idx 1 context.
                SetActiveContext(sortedAvailableCtxs[ctxIdx].editor, toolsOwnerType);
                return;
            }

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
                if (EditorToolUtility.GetToolOwnerDefinition(toolsOwnerType, out var toolOwnerDef))
                    SetActiveContext(toolOwnerDef.defaultContext, toolsOwnerType);
                else
                    SetActiveContext(typeof(GameObjectToolContext), toolsOwnerType);
            }
        }
    }
}
