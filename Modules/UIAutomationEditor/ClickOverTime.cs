// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIAutomation
{
    class ClickOverTime
    {
        public float numEventsPerSecond = 10;

        float nextEventTime;
        float startTime;
        float endTime;
        float secondsBetweenClicks;

        List<Vector2> clickPositions;
        int currentClickIndex;

        public void Clicks(EditorWindow window, List<Vector2> clickPositions, float secondsBetweenClicks)
        {
            Clicks(window, clickPositions, secondsBetweenClicks, EventModifiers.None);
        }

        public void Clicks(EditorWindow window, List<Vector2> clickPositions, float secondsBetweenClicks, EventModifiers modifiers)
        {
            this.clickPositions = clickPositions;
            this.secondsBetweenClicks = secondsBetweenClicks;

            // First click immediately
            EventUtility.Click(window, clickPositions[0]);

            SetCurrentClickIndex(1);
        }

        void SetCurrentClickIndex(int clickIndex)
        {
            currentClickIndex = clickIndex;
            startTime = (float)EditorApplication.timeSinceStartup;
            endTime = startTime + secondsBetweenClicks;
            nextEventTime = 0;
        }

        public bool Update(EditorWindow window)
        {
            return Update(window, EventModifiers.None);
        }

        public bool Update(EditorWindow window, EventModifiers modifiers)
        {
            if (currentClickIndex >= clickPositions.Count)
                return false;

            float curtime = (float)EditorApplication.timeSinceStartup;

            if (curtime > nextEventTime)
            {
                // Dispatch fake drag and drop events
                float frac = Mathf.Clamp01((curtime - startTime) / (endTime - startTime));

                frac = Easing.Quadratic.InOut(frac);
                var mouseStart = clickPositions[currentClickIndex - 1];
                var mouseEnd = clickPositions[currentClickIndex];

                Vector2 mousePosition = Vector2.Lerp(mouseStart, mouseEnd, frac);

                EventUtility.UpdateMouseMove(window, mousePosition);

                if (frac >= 1f)
                {
                    SetCurrentClickIndex(currentClickIndex + 1);
                    EventUtility.Click(window, mousePosition);
                }

                nextEventTime = curtime + (1 / numEventsPerSecond);
                window.Repaint();

                bool shouldContinue = currentClickIndex < clickPositions.Count - 1;
                return shouldContinue;
            }
            return true;
        }
    }
}
