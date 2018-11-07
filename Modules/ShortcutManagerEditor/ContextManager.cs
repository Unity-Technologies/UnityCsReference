// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShortcutManagement
{
    interface IContextManager
    {
        void SetFocusedWindow(EditorWindow window);
        void RegisterToolContext(IShortcutToolContext context);
        void DeregisterToolContext(IShortcutToolContext context);
        bool HasAnyPriorityContext();
        bool HasPriorityContextOfType(Type type);
        bool HasActiveContextOfType(Type type);
        bool playModeContextIsActive { get; }
        object GetContextInstanceOfType(Type type);
    }

    class ContextManager : IContextManager
    {
        internal class GlobalContext {}

        public static readonly GlobalContext globalContext = new GlobalContext();
        public static readonly Type globalContextType = typeof(GlobalContext);

        WeakReference m_FocusedWindow = new WeakReference(null);

        List<IShortcutToolContext> m_PriorityContexts = new List<IShortcutToolContext>();

        List<IShortcutToolContext> m_ToolContexts = new List<IShortcutToolContext>();

        public int activeContextCount => 1 + ((focusedWindow != null) ? 1 : 0) + m_PriorityContexts.Count(c => c.active) + m_ToolContexts.Count(c => c.active);

        public bool playModeContextIsActive => focusedWindow is GameView && EditorApplication.isPlaying;

        private EditorWindow focusedWindow
        {
            get
            {
                if (m_FocusedWindow.Target != null && m_FocusedWindow.IsAlive)
                    return (EditorWindow)m_FocusedWindow.Target;
                return null;
            }
        }

        public void SetFocusedWindow(EditorWindow window)
        {
            m_FocusedWindow.Target = window;
        }

        static bool IsPriorityContext(IShortcutToolContext context)
        {
            return Attribute.GetCustomAttribute(context.GetType(), typeof(PriorityContextAttribute)) != null;
        }

        void RegisterPriorityContext(IShortcutToolContext context)
        {
            if (!m_PriorityContexts.Contains(context))
            {
                m_PriorityContexts.Add(context);
            }
        }

        void DeregisterPriorityContext(IShortcutToolContext context)
        {
            m_PriorityContexts.Remove(context);
        }

        public void RegisterToolContext(IShortcutToolContext context)
        {
            if (context == null)
                return;

            if (IsPriorityContext(context))
                RegisterPriorityContext(context);
            else
            {
                if (!m_ToolContexts.Contains(context))
                    m_ToolContexts.Add(context);
            }
        }

        public void DeregisterToolContext(IShortcutToolContext context)
        {
            if (context == null)
                return;

            if (IsPriorityContext(context))
                DeregisterPriorityContext(context);
            else
            {
                if (m_ToolContexts.Contains(context))
                    m_ToolContexts.Remove(context);
            }
        }

        public bool HasAnyPriorityContext()
        {
            return m_PriorityContexts.Count > 0;
        }

        internal bool HasToolContextOfType(Type type)
        {
            return GetToolContextOfType(type) != null;
        }

        public bool HasPriorityContextOfType(Type type)
        {
            return GetPriorityContextOfType(type) != null;
        }

        public bool HasActiveContextOfType(Type type)
        {
            return GetContextInstanceOfType(type) != null;
        }

        internal object GetToolContextOfType(Type type)
        {
            foreach (var toolContext in m_ToolContexts)
            {
                if (toolContext.active && type.IsInstanceOfType(toolContext))
                    return toolContext;
            }

            return null;
        }

        internal object GetPriorityContextOfType(Type type)
        {
            foreach (var priorityContext in m_PriorityContexts)
            {
                if (priorityContext.active && type.IsInstanceOfType(priorityContext))
                {
                    return priorityContext;
                }
            }

            return null;
        }

        public object GetContextInstanceOfType(Type type)
        {
            if (type == globalContextType)
                return globalContext;
            if (m_FocusedWindow != null && m_FocusedWindow.IsAlive && type.IsInstanceOfType(m_FocusedWindow.Target))
                return m_FocusedWindow.Target;

            object priorityContextType = GetPriorityContextOfType(type);
            if (priorityContextType != null)
            {
                return priorityContextType;
            }

            object toolContextType = GetToolContextOfType(type);
            if (toolContextType != null)
            {
                return toolContextType;
            }

            return null;
        }
    }
}
