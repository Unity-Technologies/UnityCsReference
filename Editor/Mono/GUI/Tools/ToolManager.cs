// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public static class ToolManager
    {
        public static Type activeContextType => EditorToolManager.activeToolContext.GetType();

        public static void SetActiveContext(Type context)
        {
            if (context != null && (!typeof(EditorToolContext).IsAssignableFrom(context) || context.IsAbstract))
                throw new ArgumentException("Type must be assignable to EditorToolContext, and not abstract.", "context");

            var ctx = context != null ? context : typeof(GameObjectToolContext);

            if (EditorToolUtility.IsComponentEditor(ctx))
            {
                var instance = EditorToolManager.GetComponentContext(ctx, true);
                if (instance == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable " +
                        $"by the component tool of type: {context}");
                EditorToolManager.activeToolContext = instance;
            }
            else
            {
                EditorToolManager.activeToolContext = EditorToolManager.GetSingleton(ctx) as EditorToolContext;
            }
        }

        public static void SetActiveContext<T>() where T : EditorToolContext
        {
            SetActiveContext(typeof(T));
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

        public static event Action activeToolChanged;

        public static event Action activeContextChanging;

        public static event Action activeContextChanged;

        internal static void ActiveToolWillChange()
        {
            if (activeToolChanging != null)
                activeToolChanging();
#pragma warning disable 618
            EditorTools.ActiveToolWillChange();
#pragma warning restore 618
        }

        internal static void ActiveToolDidChange()
        {
            if (activeToolChanged != null)
                activeToolChanged();
#pragma warning disable 618
            EditorTools.ActiveToolDidChange();
#pragma warning restore 618
        }

        internal static void ActiveContextWillChange()
        {
            if (activeContextChanging != null)
                activeContextChanging();
        }

        internal static void ActiveContextDidChange()
        {
            if (activeContextChanged != null)
                activeContextChanged();
        }

        public static void SetActiveTool<T>() where T : EditorTool
        {
            SetActiveTool(typeof(T));
        }

        public static void SetActiveTool(Type type)
        {
            if (!typeof(EditorTool).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException("Type must be assignable to EditorTool, and not abstract.");

            if (EditorToolUtility.IsComponentEditor(type))
            {
                var tool = EditorToolManager.GetComponentTool(type);

                if (tool == null)
                    throw new InvalidOperationException("The current selection does not contain any objects editable " +
                        $"by the component tool of type: {type}");
                SetActiveTool(tool);
                return;
            }

            SetActiveTool((EditorTool)EditorToolManager.GetSingleton(type));
        }

        public static void SetActiveTool(EditorTool tool)
        {
            EditorToolManager.activeTool = tool;
        }

        public static void RestorePreviousTool()
        {
            var prev = EditorToolUtility.GetEditorToolWithEnum(EditorToolManager.previousTool);
            if (!(prev is NoneTool))
                EditorToolManager.activeTool = prev;
        }

        public static void RestorePreviousPersistentTool() => EditorToolManager.RestorePreviousPersistentTool();

        public static bool IsActiveTool(EditorTool tool)
        {
            return EditorToolManager.activeTool == tool;
        }

        public static bool IsActiveContext(EditorToolContext context)
        {
            return EditorToolManager.activeToolContext == context;
        }

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

        internal static Type GetLastContextType()
        {
            var lastContext = EditorToolManager.lastCustomContext;
            if (lastContext != null && lastContext != typeof(GameObjectToolContext))
                return lastContext;

            return allContextsExceptGameObject.FirstOrDefault();
        }

        [Shortcut("Tools/Enter GameObject Mode", typeof(ToolShortcutContext))]
        internal static void ExitToolContext()
        {
            SetActiveContext<GameObjectToolContext>();
        }

        [Shortcut("Tools/Cycle Tool Modes", typeof(ToolShortcutContext))]
        internal static void CycleToolContexts()
        {
            if (EditorToolUtility.toolContextsInProject < 2)
                return;

            var active = EditorToolManager.activeToolContext;

            if (active is GameObjectToolContext && EditorToolManager.lastCustomContext != null)
            {
                var instance = allContextsExceptGameObject.FirstOrDefault(x => x == EditorToolManager.lastCustomContext);

                if (instance != null)
                {
                    SetActiveContext(instance);
                    return;
                }
            }

            using var all = allContextsExceptGameObject.GetEnumerator();

            if (!all.MoveNext())
                return;

            // Select the next available context after the active
            while (all.Current != active.GetType())
            {
                // The active context is not registered with an attribute. We'll just return the last available context
                // in that case.
                if (!all.MoveNext())
                {
                    SetActiveContext(all.Current);
                    return;
                }
            }

            // If we can advance from the active context, that is the next context. If not, we're at the end of the list
            // and need to circle back around to the first available context.
            if (all.MoveNext())
                SetActiveContext(all.Current);
            else
                SetActiveContext(allContextsExceptGameObject.First());
        }
    }
}
