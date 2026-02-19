// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using UnityEditorInternal;

using UnityEditor.Animations.AnimationWindow.TimelineFoundation;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class CanvasManager : ICanvas
    {
        private CanvasOverlayManager m_CanvasOverlayManager;
        private AnimationWindowState m_State;

        public CanvasManager(CanvasOverlayManager canvasOverlayManager, AnimationWindowState state)
        {
            m_CanvasOverlayManager = canvasOverlayManager;
            m_State = state;
        }

        public DiscreteTime WorldPixelToTime(float worldPixel, bool ignoreSnapToFrame = false)
        {
            float localPixel = m_CanvasOverlayManager.WorldToLocal(new Vector2(worldPixel, 0f)).x;
            return new DiscreteTime(m_State.PixelToTime(localPixel,
                ignoreSnapToFrame ? AnimationWindowState.SnapMode.SnapToFrame : AnimationWindowState.SnapMode.Disabled));
        }

        public float TimeToWorldPixel(DiscreteTime time)
        {
            float localPixel = m_State.TimeToPixel((float)time);
            return m_CanvasOverlayManager.LocalToWorld(new Vector2(localPixel, 0f)).x;
        }

        public float DurationToPixelWidth(DiscreteTime duration)
        {
            return m_State.TimeToPixel((float)duration) - m_State.TimeToPixel(0f);
        }

        public DiscreteTime PixelWidthToDuration(float width)
        {
            return new DiscreteTime(m_State.PixelToTime(m_State.zeroTimePixel + width) - m_State.PixelToTime(m_State.zeroTimePixel));
        }

        public string ToTimeString(DiscreteTime time)
        {
            var timeFormat = m_State.timeFormat;
            return timeFormat.ToTimeString((double)time, new FrameRate((uint)m_State.frameRate), timeFormat != TimeFormat.Seconds ? "f0" : "f4");
        }
    }
}
