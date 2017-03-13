// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEvent = UnityEngine.Event;

// We are putting this in the Editor folder for now since on SpriteEditorWindow et al. are using it
namespace UnityEngine.U2D.Interface
{
    internal interface IEvent
    {
        EventType type { get; }
        string commandName { get; }
        bool control { get; }
        bool alt { get; }
        bool shift { get; }
        KeyCode keyCode { get; }
        Vector2 mousePosition { get; }
        int button { get; }
        EventModifiers modifiers { get; }
        EventType GetTypeForControl(int id);

        void Use();
    }

    internal class Event : IEvent
    {
        UnityEvent m_Event;

        public Event()
        {
            m_Event = UnityEvent.current;
        }

        public EventType type
        {
            get { return m_Event.type; }
        }

        public string commandName
        {
            get { return m_Event.commandName; }
        }

        public bool control
        {
            get { return m_Event.control; }
        }

        public bool alt
        {
            get { return m_Event.alt; }
        }

        public bool shift
        {
            get { return m_Event.shift; }
        }

        public KeyCode keyCode
        {
            get { return m_Event.keyCode; }
        }

        public Vector2 mousePosition
        {
            get { return m_Event.mousePosition; }
        }

        public int button
        {
            get { return m_Event.button; }
        }

        public void Use()
        {
            m_Event.Use();
        }

        public EventModifiers modifiers
        {
            get { return m_Event.modifiers; }
        }

        public EventType GetTypeForControl(int id)
        {
            return m_Event.GetTypeForControl(id);
        }
    }

    internal interface IEventSystem
    {
        IEvent current { get; }
    }

    internal class EventSystem : IEventSystem
    {
        public IEvent current
        {
            get { return new Event(); }
        }
    }
}
