// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class AnimEditorOverlay
    {
        public AnimationWindowState state;

        private TimeCursorManipulator m_PlayHeadCursor;

        private Rect m_Rect;
        private Rect m_ContentRect;

        public Rect rect { get { return m_Rect; } }
        public Rect contentRect { get { return m_ContentRect; } }

        public void Initialize()
        {
            if (m_PlayHeadCursor == null)
            {
                m_PlayHeadCursor = new TimeCursorManipulator(AnimationWindowStyles.playHead);

                m_PlayHeadCursor.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (evt.mousePosition.y <= (m_Rect.yMin + 20))
                            return OnStartDragPlayHead(evt);

                        return false;
                    };
                m_PlayHeadCursor.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        return OnDragPlayHead(evt);
                    };
                m_PlayHeadCursor.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        return OnEndDragPlayHead(evt);
                    };
            }
        }

        public void OnGUI(Rect rect, Rect contentRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_Rect = rect;
            m_ContentRect = contentRect;

            Initialize();

            m_PlayHeadCursor.OnGUI(m_Rect, m_Rect.xMin + TimeToPixel(state.currentTime));
        }

        public void HandleEvents()
        {
            Initialize();

            m_PlayHeadCursor.HandleEvents();
        }

        private bool OnStartDragPlayHead(Event evt)
        {
            state.controlInterface.StopPlayback();
            state.controlInterface.StartScrubTime();
            state.controlInterface.ScrubTime(MousePositionToTime(evt));
            return true;
        }

        private bool OnDragPlayHead(Event evt)
        {
            state.controlInterface.ScrubTime(MousePositionToTime(evt));
            return true;
        }

        private bool OnEndDragPlayHead(Event evt)
        {
            state.controlInterface.EndScrubTime();
            return true;
        }

        public float MousePositionToTime(Event evt)
        {
            float width = m_ContentRect.width;
            float time = Mathf.Max(((evt.mousePosition.x / width) * state.visibleTimeSpan + state.minVisibleTime), 0);
            time = state.SnapToFrame(time, AnimationWindowState.SnapMode.SnapToFrame);
            return time;
        }

        public float MousePositionToValue(Event evt)
        {
            float height = m_ContentRect.height;
            float valuePixel = height - evt.mousePosition.y;

            TimeArea timeArea = state.timeArea;

            float pixelPerValue = timeArea.m_Scale.y * -1f;
            float zeroValuePixel = timeArea.shownArea.yMin * pixelPerValue * -1f;

            float value = (valuePixel - zeroValuePixel) / pixelPerValue;

            return value;
        }

        public float TimeToPixel(float time)
        {
            return state.TimeToPixel(time);
        }

        public float ValueToPixel(float value)
        {
            TimeArea timeArea = state.timeArea;

            float pixelPerValue = timeArea.m_Scale.y * -1f;
            float zeroValuePixel = timeArea.shownArea.yMin * pixelPerValue * -1f;

            float pixelValue = value * pixelPerValue + zeroValuePixel;

            return pixelValue;
        }
    }
}
