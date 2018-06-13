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
        bool HasAnyActiveContextOfType(Type type);
        object GetContextInstanceOfType(Type type);
        IShortcutToolContext priorityContext { get; }
    }

    internal class ContextManager : IContextManager
    {
        internal class GlobalContext{}
        public static readonly GlobalContext globalContext = new GlobalContext();
        public static readonly Type globalContextType = typeof(GlobalContext);

        EditorWindow m_FocusedWindow;

        IShortcutToolContext m_PriorityContext;
        public IShortcutToolContext priorityContext => m_PriorityContext;

        List<IShortcutToolContext> m_ToolContexts = new List<IShortcutToolContext>();

        public int activeContextCount => 1 + (m_FocusedWindow != null ? 1 : 0) + (m_PriorityContext != null && m_PriorityContext.active ? 1 : 0) + m_ToolContexts.Count(c => c.active);

        public void SetFocusedWindow(EditorWindow window)
        {
            m_FocusedWindow = window;
        }

        public void SetPriorityContext(IShortcutToolContext context)
        {
            m_PriorityContext = context;
        }

        public void ClearPriorityContext()
        {
            m_PriorityContext = null;
        }

        public void RegisterToolContext(IShortcutToolContext context)
        {
            if (context == null)
                return;
            if (!m_ToolContexts.Contains(context))
                m_ToolContexts.Add(context);
        }

        public void DeregisterToolContext(IShortcutToolContext context)
        {
            if (context == null)
                return;
            if (m_ToolContexts.Contains(context))
                m_ToolContexts.Remove(context);
        }

        public object GetContextInstanceOfType(Type type)
        {
            if (type == globalContextType)
                return globalContext;
            if (type.IsInstanceOfType(m_FocusedWindow))
                return m_FocusedWindow;
            if (m_PriorityContext != null && m_PriorityContext.active && type.IsInstanceOfType(m_PriorityContext))
                return m_PriorityContext;
            for (int i = 0; i < m_ToolContexts.Count; i++)
            {
                if (m_ToolContexts[i].active && m_ToolContexts[i].GetType() == type)
                    return m_ToolContexts[i];
            }
            return m_ToolContexts.FirstOrDefault(c => c.active && type.IsInstanceOfType(c));
        }

        public bool HasAnyActiveContextOfType(Type type)
        {
            if (type.IsInstanceOfType(globalContext))
                return true;
            if (type.IsInstanceOfType(m_FocusedWindow))
                return true;
            if (priorityContext != null && priorityContext.active && type.IsInstanceOfType(priorityContext))
                return true;
            return m_ToolContexts.Any(c => c.active && type.IsInstanceOfType(c));
        }
    }
}
