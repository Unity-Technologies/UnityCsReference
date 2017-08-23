// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    delegate EventPropagation ShortcutDelegate();

    internal
    class ShortcutHandler : Manipulator
    {
        readonly Dictionary<Event, ShortcutDelegate> m_Dictionary;

        public ShortcutHandler(Dictionary<Event, ShortcutDelegate> dictionary)
        {
            m_Dictionary = dictionary;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Dictionary.ContainsKey(evt.imguiEvent))
            {
                EventPropagation result = m_Dictionary[evt.imguiEvent]();
                if (result == EventPropagation.Stop)
                {
                    evt.StopPropagation();
                }
            }
        }
    }
}
