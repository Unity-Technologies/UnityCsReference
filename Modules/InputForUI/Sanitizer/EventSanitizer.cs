// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Internal method to sanitize the event stream and ensure runtime behaviors.
    /// Is primarily concerns with event ordering and presence of redundant events.
    /// </summary>
    internal struct EventSanitizer
    {
        private IEventSanitizer[] _sanitizers;

        public void Reset()
        {
            _sanitizers = new IEventSanitizer[]
            {
// Removed from release builds for the time being as it currently only logs issues rather
// than ensuring any runtime behavior (https://jira.unity3d.com/browse/ISX-1388).
            };

            foreach (var sanitizer in _sanitizers)
                sanitizer.Reset();
        }

        public void BeforeProviderUpdate()
        {
            if (_sanitizers == null)
                Reset();

            foreach (var sanitizer in _sanitizers)
                sanitizer.BeforeProviderUpdate();
        }

        public void AfterProviderUpdate()
        {
            if (_sanitizers == null)
                Reset();

            foreach (var sanitizer in _sanitizers)
                sanitizer.AfterProviderUpdate();
        }

        public void Inspect(in Event ev)
        {
            if (_sanitizers == null)
                Reset();

            foreach (var sanitizer in _sanitizers)
                sanitizer.Inspect(ev);
        }

        private interface IEventSanitizer
        {
            public void Reset();
            public void BeforeProviderUpdate();
            public void AfterProviderUpdate();
            public void Inspect(in Event ev);
        }

        /// <summary>
        /// Click count sanitizer to ensure click count on button up is equal to click count on button down
        /// </summary>
        private struct ClickCountEventSanitizer : IEventSanitizer
        {
            private List<PointerEvent> _activeButtons;
            private int lastPushedIndex;

            public void Reset()
            {
                _activeButtons = new List<PointerEvent>();
                lastPushedIndex = 0;
            }

            public void BeforeProviderUpdate()
            {
            }

            public void AfterProviderUpdate()
            {
            }

            public void Inspect(in Event ev)
            {
                if (ev.type != Event.Type.PointerEvent)
                    return;

                var pointerEvent = ev.asPointerEvent;
                switch (pointerEvent.type)
                {
                    case PointerEvent.Type.ButtonPressed:
                    {
                        lastPushedIndex = _activeButtons.Count;
                        _activeButtons.Add(pointerEvent);
                        break;
                    }
                    case PointerEvent.Type.ButtonReleased:
                    {
                        var releaseEvent = pointerEvent;
                        for(var i = 0; i < _activeButtons.Count; ++i)
                        {
                            var pressEvent = _activeButtons[i];

                            if (pressEvent.eventSource != releaseEvent.eventSource || pressEvent.pointerIndex != releaseEvent.pointerIndex)
                                continue;

                            if (i == lastPushedIndex)
                            {
                                if (pressEvent.clickCount != releaseEvent.clickCount)
                                {
                                    Debug.LogWarning(
                                        $"ButtonReleased click count doesn't match ButtonPressed click count, where '{pressEvent}' and '{releaseEvent}'");
                                }
                            }
                            else if (releaseEvent.clickCount != 1)
                            {
                                Debug.LogWarning(
                                    $"ButtonReleased for not the last pressed button should have click count == 1, but got '{releaseEvent}'");
                            }
                            _activeButtons.RemoveAt(i);
                            return;
                        }

                        Debug.LogWarning($"Can't find corresponding ButtonPressed for '{ev}'");
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// DefaultEventSystem sanitizer to make sure data comes in to the DefaultEventSystem in the form and order
        /// that it is expected to.
        /// </summary>
        private class DefaultEventSystemSanitizer : IEventSanitizer
        {
            private int m_MouseEventCount;
            private int m_PenOrTouchEventCount;

            public void Reset()
            {
            }

            public void BeforeProviderUpdate()
            {
                m_MouseEventCount = 0;
                m_PenOrTouchEventCount = 0;
            }

            public void AfterProviderUpdate()
            {
                if (m_MouseEventCount > 0 && m_PenOrTouchEventCount > 0)
                {
                    Debug.LogError("PointerEvents of source Mouse and [Pen or Touch] received in the same update. This is likely an error, and Mouse events should be discarded.");
                }
            }

            public void Inspect(in Event ev)
            {
                if (ev.type == Event.Type.PointerEvent)
                {
                    var pointerEvent = ev.asPointerEvent;

                    if (pointerEvent.type == PointerEvent.Type.ButtonPressed &&
                        pointerEvent.button == PointerEvent.Button.None)
                    {
                        Debug.LogError("PointerEvent of type ButtonPressed must have button property set to a value other than None.");
                    }

                    if (pointerEvent.type == PointerEvent.Type.ButtonReleased &&
                        pointerEvent.button == PointerEvent.Button.None)
                    {
                        Debug.LogError("PointerEvent of type ButtonReleased must have button property set to a value other than None.");
                    }

                    if (pointerEvent.eventSource == EventSource.Mouse)
                    {
                        m_MouseEventCount++;

                        if (!pointerEvent.isPrimaryPointer)
                        {
                            Debug.LogError("PointerEvent of source Mouse is expected to have isPrimaryPointer set to true.");
                        }

                        if (pointerEvent.pointerIndex != 0)
                        {
                            Debug.LogError("PointerEvent of source Mouse is expected to have pointerIndex set to 0.");
                        }
                    }
                    else if (pointerEvent.eventSource == EventSource.Touch)
                    {
                        m_PenOrTouchEventCount++;

                        if (pointerEvent.button != PointerEvent.Button.None &&
                            pointerEvent.button != PointerEvent.Button.FingerInTouch)
                        {
                            Debug.LogError("PointerEvent of source Touch is expected to have button set to None or FingerInTouch.");
                        }
                    }
                    else if (pointerEvent.eventSource == EventSource.Pen)
                    {
                        m_PenOrTouchEventCount++;

                        if (pointerEvent.button != PointerEvent.Button.None &&
                            pointerEvent.button != PointerEvent.Button.PenTipInTouch &&
                            pointerEvent.button != PointerEvent.Button.PenBarrelButton &&
                            pointerEvent.button != PointerEvent.Button.PenEraserInTouch)
                        {
                            Debug.LogError("PointerEvent of source Pen is expected to have button set to None, PenTipInTouch, PenBarrelButton, or PenEraserInTouch.");
                        }
                    }
                }
            }
        }
    }
}
