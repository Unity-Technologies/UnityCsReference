// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal struct MouseEventArgs
    {
        readonly EventModifiers m_Modifiers;

        public Vector2 mousePosition { get; private set; }
        public int clickCount { get; private set; }

        // Is Shift held down? (RO)
        public bool shift
        {
            get { return (m_Modifiers & EventModifiers.Shift) != 0; }
        }

        public MouseEventArgs(Vector2 pos, int clickCount, EventModifiers modifiers)
            : this()
        {
            mousePosition = pos;
            this.clickCount = clickCount;
            m_Modifiers = modifiers;
        }
    }

    internal struct KeyboardEventArgs
    {
        readonly EventModifiers m_Modifiers;

        public char character { get; private set; }
        public KeyCode keyCode { get; private set; }

        // Is Shift held down? (RO)
        public bool shift
        {
            get { return (m_Modifiers & EventModifiers.Shift) != 0; }
        }

        // Is Alt held down? (RO)
        public bool alt
        {
            get { return (m_Modifiers & EventModifiers.Alt) != 0; }
        }

        public KeyboardEventArgs(char character, KeyCode keyCode, EventModifiers modifiers)
            : this()
        {
            this.character = character;
            this.keyCode = keyCode;
            m_Modifiers = modifiers;
        }

        // TODO: we need to pull Event out of KeyboardEventArgs... just not now.
        public Event ToEvent()
        {
            return new Event()
            {
                character = this.character,
                keyCode = this.keyCode,
                modifiers = m_Modifiers,
                type = EventType.KeyDown
            };
        }
    }
}
