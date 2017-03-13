// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    internal class IncrementalInitialize
    {
        public enum State
        {
            PreInitialize,  // 1. Show 'loading' UI
            Initialize,     // 2. Do heavy operation
            Initialized     // 3. Done
        }

        [SerializeField]
        State m_InitState;
        [NonSerialized]
        bool m_IncrementOnNextEvent;

        public State state
        {
            get { return m_InitState; }
        }

        public void Restart()
        {
            m_InitState = State.PreInitialize;
        }

        // Call OnEvent() in begining of your OnGUI (e.g in an EditorWindow).
        // We are checking for repaint events
        public void OnEvent()
        {
            if (m_IncrementOnNextEvent)
            {
                m_InitState++;
                m_IncrementOnNextEvent = false;
            }

            switch (m_InitState)
            {
                case State.PreInitialize:
                    if (Event.current.type == EventType.Repaint)
                    {
                        m_IncrementOnNextEvent = true;
                        // Ensure a new repaint after our first repaint (to show the result of Initialize)
                        HandleUtility.Repaint();
                    }
                    break;

                case State.Initialize:
                    // We are only in Initialize in one event
                    m_IncrementOnNextEvent = true;
                    break;
            }
        }
    }
}
