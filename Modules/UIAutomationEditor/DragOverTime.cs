// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIAutomation
{
    class DragOverTime
    {
        public float numEventsPerSecond = 10;

        float nextEventTime;
        float startTime;
        float endTime;

        Vector2 mouseStart;
        Vector2 mouseEnd;


        public void DragAndDrop(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd, float seconds)
        {
            DragAndDrop(window, mousePositionStart, mousePositionEnd, seconds, EventModifiers.None);
        }

        public void DragAndDrop(EditorWindow window, Vector2 mousePositionStart, Vector2 mousePositionEnd, float seconds, EventModifiers modifiers)
        {
            mouseStart = mousePositionStart;
            mouseEnd = mousePositionEnd;
            mouseStart.y += 23f;
            mouseEnd.y += 23f;
            startTime = (float)EditorApplication.timeSinceStartup;
            endTime = startTime + seconds;

            EventUtility.BeginDragAndDrop(window, mouseStart);
        }

        public bool Update(EditorWindow window)
        {
            return Update(window, EventModifiers.None);
        }

        public bool Update(EditorWindow window, EventModifiers modifiers)
        {
            float curtime = (float)EditorApplication.timeSinceStartup;

            if (curtime > nextEventTime)
            {
                // Dispatch fake drag and drop events
                float frac = Mathf.Clamp01((curtime - startTime) / (endTime - startTime));

                frac = Easing.Quadratic.InOut(frac);

                Vector2 mousePosition = Vector2.Lerp(mouseStart, mouseEnd, frac);

                EventUtility.UpdateDragAndDrop(window, mousePosition);

                bool shouldContinue = frac < 1.0f;
                if (!shouldContinue)
                    EventUtility.EndDragAndDrop(window, mousePosition);

                nextEventTime = curtime + (1 / numEventsPerSecond);
                window.Repaint();
                return shouldContinue;
            }
            return true;
        }
    }
}
