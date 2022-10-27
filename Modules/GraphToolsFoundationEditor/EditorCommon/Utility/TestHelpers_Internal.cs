// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    static class TestHelpers_Internal
    {
        public static class EventCommandNames
        {
            public const string Cut = UnityEngine.EventCommandNames.Cut;
            public const string Copy = UnityEngine.EventCommandNames.Copy;
            public const string Paste = UnityEngine.EventCommandNames.Paste;
            public const string Duplicate = UnityEngine.EventCommandNames.Duplicate;
            public const string Delete = UnityEngine.EventCommandNames.Delete;
            public const string SoftDelete = UnityEngine.EventCommandNames.SoftDelete;
            public const string FrameSelected = UnityEngine.EventCommandNames.FrameSelected;
            public const string SelectAll = UnityEngine.EventCommandNames.SelectAll;
            public const string DeselectAll = UnityEngine.EventCommandNames.DeselectAll;
            public const string InvertSelection = UnityEngine.EventCommandNames.InvertSelection;
        }

        public class TimerEventSchedulerWrapper : IDisposable
        {
            readonly VisualElement m_VisualElement;

            internal TimerEventSchedulerWrapper(VisualElement visualElement)
            {
                m_VisualElement = visualElement;
                Panel.TimeSinceStartup = () => TimeSinceStartup;
            }

            public long TimeSinceStartup { get; set; }

            public void Dispose()
            {
                Panel.TimeSinceStartup = null;
            }

            public void UpdateScheduledEvents()
            {
                TimerEventScheduler s = (TimerEventScheduler)m_VisualElement.elementPanel.scheduler;
                s.UpdateScheduledEvents();
            }
        }

        public static TimerEventSchedulerWrapper CreateTimerEventSchedulerWrapper(this VisualElement graphView)
        {
            return new TimerEventSchedulerWrapper(graphView);
        }

        public static float RoundToPixelGrid(float v)
        {
            return GUIUtility.RoundToPixelGrid(v);
        }

        public static Vector2 RoundToPixelGrid(Vector2 v)
        {
            return new Vector2(GUIUtility.RoundToPixelGrid(v.x), GUIUtility.RoundToPixelGrid(v.y));
        }

        public static Rect RoundToPixelGrid(Rect r)
        {
            var min = RoundToPixelGrid(r.min);
            var max = RoundToPixelGrid(r.max);
            return new Rect(min, max - min);
        }

        public static float PixelPerPoint => GUIUtility.pixelsPerPoint;

        public static void SetDisableInputEvents(this EditorWindow window, bool value)
        {
            window.disableInputEvents = value;
        }

        public static void ClearPersistentViewData(this EditorWindow window)
        {
            window.ClearPersistentViewData();
        }

        public static void DisableViewDataPersistence(this EditorWindow window)
        {
            window.DisableViewDataPersistence();
        }

        public static void SetTimeSinceStartupCallback(Func<long> cb)
        {
            if (cb == null)
                Panel.TimeSinceStartup = null;
            else
                Panel.TimeSinceStartup = () => cb();
        }

        public static void UpdateScheduledEvents(this VisualElement ve)
        {
            var scheduler = (TimerEventScheduler)((BaseVisualElementPanel)ve.panel).scheduler;
            scheduler.UpdateScheduledEvents();
        }

        public static bool GetDisabledPseudoState(this VisualElement ve)
        {
            return (ve.pseudoStates & PseudoStates.Disabled) == PseudoStates.Disabled;
        }

        public static IEnumerable<Overlay> GetAllOverlays(this EditorWindow window)
        {
            return window.overlayCanvas.overlays;
        }

        public static VisualElement GetOverlayRoot(Overlay overlay)
        {
            return overlay.rootVisualElement;
        }
    }
}
