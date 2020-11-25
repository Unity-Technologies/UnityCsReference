// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    public enum TouchPhase
    {
        Began,
        Moved,
        Ended,
        Canceled,
        Stationary
    }

    public struct TouchEvent
    {
        internal TouchEvent(int touchId, Vector2 position, TouchPhase phase)
        {
            this.touchId = touchId;
            this.position = position;
            this.phase = phase;
        }

        public int touchId { get; }
        public Vector2 position { get; }
        public TouchPhase phase { get; }
    }

    public class DeviceSimulator
    {
        internal DeviceSimulator()
        {
        }

        public event Action<TouchEvent> touchScreenInput;

        internal void OnTouchScreenInput(TouchEvent touchEvent)
        {
            var handlers = touchScreenInput?.GetInvocationList();
            if (handlers == null)
                return;

            foreach (Action<TouchEvent> handler in handlers)
            {
                try
                {
                    handler.Invoke(touchEvent);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
