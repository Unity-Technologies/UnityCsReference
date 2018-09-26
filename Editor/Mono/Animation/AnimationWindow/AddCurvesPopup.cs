// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AddCurvesPopup : EditorWindow
    {
        const float k_WindowPadding = 3;
        const float k_SpaceForSlider = 16;

        const float k_WindowMaxWidth = 450;
        const float k_WindowMinWidth = 240;
        const float k_WindowFixedHeight = 250;

        internal static AnimationWindowState s_State;

        private static AddCurvesPopup s_AddCurvesPopup;
        private static long s_LastClosedTime;
        private static AddCurvesPopupHierarchy s_Hierarchy;

        public delegate void OnNewCurveAdded(AddCurvesPopupPropertyNode node);

        private static OnNewCurveAdded NewCurveAddedCallback;

        Vector2 GetWindowSize()
        {
            float contentWidth = s_Hierarchy.GetContentWidth();
            float width = Mathf.Clamp(contentWidth + k_SpaceForSlider + k_WindowPadding, k_WindowMinWidth, k_WindowMaxWidth);
            return new Vector2(width, k_WindowFixedHeight);
        }

        void Init(Rect buttonRect)
        {
            s_Hierarchy = new AddCurvesPopupHierarchy();
            s_Hierarchy.InitIfNeeded(this, new Rect(0, 0, k_WindowMinWidth, k_WindowFixedHeight));

            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            ShowAsDropDown(buttonRect, GetWindowSize(), new[] { PopupLocation.Right });
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_AddCurvesPopup = null;
            s_Hierarchy = null;
        }

        internal static void AddNewCurve(AddCurvesPopupPropertyNode node)
        {
            AnimationWindowUtility.CreateDefaultCurves(s_State, node.curveBindings);
            if (NewCurveAddedCallback != null)
                NewCurveAddedCallback(node);
        }

        internal static bool ShowAtPosition(Rect buttonRect, AnimationWindowState state, OnNewCurveAdded newCurveCallback)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_AddCurvesPopup == null)
                    s_AddCurvesPopup = ScriptableObject.CreateInstance<AddCurvesPopup>();

                NewCurveAddedCallback = newCurveCallback;
                s_State = state;
                s_AddCurvesPopup.Init(buttonRect);
                return true;
            }
            return false;
        }

        internal void OnGUI()
        {
            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            Vector2 windowSize = GetWindowSize();

            Rect rect = new Rect(1, 1, windowSize.x - k_WindowPadding, windowSize.y - k_WindowPadding);
            GUI.Box(new Rect(0, 0, windowSize.x, windowSize.y), GUIContent.none, "grey_border");
            s_Hierarchy.OnGUI(rect, this);
        }
    }
}
