// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal class EndedTouch
    {
        public Vector2 position;
        public double endTime;
        public int tapCount;

        public EndedTouch(Vector2 position, double endTime, int tapCount)
        {
            this.position = position;
            this.endTime = endTime;
            this.tapCount = tapCount;
        }
    }

    internal class InputManagerBackend
    {
        public float tapTimeout = .5f;
        public float tapDistance = 10f;

        // iOS and Android have different behaviors, iOS will keep Touch.deltaTime unchanged while finger is stationary while Android resets to 0
        public bool resetDeltaTimeWhenStationary;

        private int m_LastEventFrame = -1;
        private double m_LastEventTime;
        private List<EndedTouch> m_EndedTouches = new List<EndedTouch>();
        private Touch m_NextTouch;
        private Vector2 m_StartPosition;
        private bool m_TouchInProgress;

        public InputManagerBackend()
        {
            EditorApplication.update += TouchStationary;
        }

        private void TouchStationary()
        {
            if (m_TouchInProgress && m_LastEventFrame != Time.frameCount)
            {
                m_NextTouch.phase = UnityEngine.TouchPhase.Stationary;
                if (resetDeltaTimeWhenStationary)
                {
                    m_NextTouch.deltaTime = 0;
                    m_LastEventTime = EditorApplication.timeSinceStartup;
                }

                Input.SimulateTouch(m_NextTouch);
            }
        }

        public void Touch(int id, Vector2 position, TouchPhase phase)
        {
            m_LastEventFrame = Time.frameCount;

            var newPhase = ToLegacy(phase);
            m_NextTouch.position = position;
            m_NextTouch.phase = newPhase;
            m_NextTouch.fingerId = id;
            m_NextTouch.deltaTime = (float)(EditorApplication.timeSinceStartup - m_LastEventTime);
            m_LastEventTime = EditorApplication.timeSinceStartup;

            if (newPhase == UnityEngine.TouchPhase.Began)
            {
                m_StartPosition = position;
                m_TouchInProgress = true;
                m_NextTouch.tapCount = GetTapCount(m_NextTouch.position);
                m_NextTouch.deltaTime = 0;
            }
            else if (m_NextTouch.phase == UnityEngine.TouchPhase.Ended || m_NextTouch.phase == UnityEngine.TouchPhase.Canceled)
            {
                m_TouchInProgress = false;
                if (m_NextTouch.phase == UnityEngine.TouchPhase.Ended)
                    m_EndedTouches.Add(new EndedTouch(m_NextTouch.position, EditorApplication.timeSinceStartup, m_NextTouch.tapCount));
            }
            m_NextTouch.rawPosition = m_StartPosition;

            Input.SimulateTouch(m_NextTouch);
        }

        private int GetTapCount(Vector2 position)
        {
            var foundTime = false;
            for (var i = m_EndedTouches.Count - 1; i >= 0; i--)
            {
                var endedTouch = m_EndedTouches[i];
                if (tapTimeout > EditorApplication.timeSinceStartup - endedTouch.endTime)
                {
                    foundTime = true;
                    if (Vector2.Distance(position, endedTouch.position) < tapDistance)
                        return endedTouch.tapCount + 1;
                }
            }
            if (!foundTime)
                m_EndedTouches.Clear();
            return 1;
        }

        private static UnityEngine.TouchPhase ToLegacy(TouchPhase original)
        {
            switch (original)
            {
                case TouchPhase.Began:
                    return UnityEngine.TouchPhase.Began;
                case TouchPhase.Moved:
                    return UnityEngine.TouchPhase.Moved;
                case TouchPhase.Ended:
                    return UnityEngine.TouchPhase.Ended;
                case TouchPhase.Canceled:
                    return UnityEngine.TouchPhase.Canceled;
                case TouchPhase.Stationary:
                    return UnityEngine.TouchPhase.Stationary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(original), original, "None is not a supported phase with legacy input system");
            }
        }

        public void Dispose()
        {
            EditorApplication.update -= TouchStationary;
        }
    }
}
