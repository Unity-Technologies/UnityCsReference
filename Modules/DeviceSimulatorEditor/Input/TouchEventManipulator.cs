// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal enum MousePhase { Start, Move, End }

    internal enum SimulatorTouchPhase
    {
        None,
        Began,
        Moved,
        Ended,
        Canceled,
        Stationary
    }

    internal class TouchEventManipulator : MouseManipulator
    {
        private bool m_TouchFromMouseActive;
        private int m_ScreenWidth;
        private int m_ScreenHeight;
        private ScreenSimulation m_ScreenSimulation;
        private InputManagerBackend m_InputManagerBackend;
        public Vector2 PointerPosition { private set; get; } = new Vector2(-1, -1);
        public bool IsPointerInsideDeviceScreen { private set; get; }

        public Matrix4x4 PreviewImageRendererSpaceToScreenSpace { get; set; }

        public TouchEventManipulator()
        {
            activators.Add(new ManipulatorActivationFilter() {button = MouseButton.LeftMouse});

            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
            var so = new SerializedObject(playerSettings);
            var activeInputHandler = so.FindProperty("activeInputHandler");
            // 0 -> Input Manager, 1 -> Input System, 2 -> Both
            if (activeInputHandler.intValue == 0 || activeInputHandler.intValue == 2)
                m_InputManagerBackend = new InputManagerBackend();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            SendMouseEvent(evt, MousePhase.Start);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            SendMouseEvent(evt, MousePhase.Move);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            SendMouseEvent(evt, MousePhase.End);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            SendMouseEvent(evt, MousePhase.End);
        }

        private void SendMouseEvent(IMouseEvent evt, MousePhase phase)
        {
            if (!activators.Any(filter => filter.Matches(evt)))
                return;

            var position = PreviewImageRendererSpaceToScreenSpace.MultiplyPoint(evt.localMousePosition);
            TouchFromMouse(position, phase);
        }

        public void InitTouchInput(int screenWidth, int screenHeight, ScreenSimulation screenSimulation)
        {
            m_ScreenWidth = screenWidth;
            m_ScreenHeight = screenHeight;
            m_ScreenSimulation = screenSimulation;
            CancelAllTouches();
        }

        public void TouchFromMouse(Vector2 position, MousePhase mousePhase)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            // Clamping position inside the device screen. UI element that sends input events also includes the device border and we don't want to register inputs there.
            IsPointerInsideDeviceScreen = true;
            if (position.x < 0)
            {
                position.x = 0;
                IsPointerInsideDeviceScreen = false;
            }
            else if (position.x > m_ScreenWidth)
            {
                position.x = m_ScreenWidth;
                IsPointerInsideDeviceScreen = false;
            }
            if (position.y < 0)
            {
                position.y = 0;
                IsPointerInsideDeviceScreen = false;
            }
            else if (position.y > m_ScreenHeight)
            {
                position.y = m_ScreenHeight;
                IsPointerInsideDeviceScreen = false;
            }

            PointerPosition = ScreenPixelToTouchCoordinate(position);

            if (!m_TouchFromMouseActive && mousePhase != MousePhase.Start)
                return;

            var phase = SimulatorTouchPhase.None;

            if (!IsPointerInsideDeviceScreen)
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        return;
                    case MousePhase.Move:
                    case MousePhase.End:
                        phase = SimulatorTouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }
            else
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        phase = SimulatorTouchPhase.Began;
                        m_TouchFromMouseActive = true;
                        break;
                    case MousePhase.Move:
                        phase = SimulatorTouchPhase.Moved;
                        break;
                    case MousePhase.End:
                        phase = SimulatorTouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }

            m_InputManagerBackend?.Touch(0, PointerPosition, phase);
        }

        /// <summary>
        /// Converting from screen pixel to coordinates that are returned by input. Input coordinates change depending on:
        /// current resolution, full screen or not (insets), and orientation.
        /// </summary>
        /// <param name="position">Pixel position in portrait orientation, with origin at the top left corner</param>
        /// <returns>Position dependent on current resolution, insets and orientation, with origin at the bottom left of the rendered rect in the current orientation.</returns>
        private Vector2 ScreenPixelToTouchCoordinate(Vector2 position)
        {
            // First calculating which pixel is being touched inside the pixel rect where game is rendered in portrait orientation, due to insets this might not be full screen
            var renderedAreaPortraitWidth = m_ScreenWidth - m_ScreenSimulation.Insets.x - m_ScreenSimulation.Insets.z;
            var renderedAreaPortraitHeight = m_ScreenHeight - m_ScreenSimulation.Insets.y - m_ScreenSimulation.Insets.w;

            var touchedPixelPortraitX = position.x - m_ScreenSimulation.Insets.x;
            var touchedPixelPortraitY = position.y - m_ScreenSimulation.Insets.y;

            // Converting touch so that no matter the orientation origin would be at the bottom left corner
            float touchedPixelX = 0;
            float touchedPixelY = 0;
            switch (m_ScreenSimulation.orientation)
            {
                case ScreenOrientation.Portrait:
                    touchedPixelX = touchedPixelPortraitX;
                    touchedPixelY = renderedAreaPortraitHeight - touchedPixelPortraitY;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    touchedPixelX = renderedAreaPortraitWidth - touchedPixelPortraitX;
                    touchedPixelY = touchedPixelPortraitY;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    touchedPixelX = touchedPixelPortraitY;
                    touchedPixelY = touchedPixelPortraitX;
                    break;
                case ScreenOrientation.LandscapeRight:
                    touchedPixelX = renderedAreaPortraitHeight - touchedPixelPortraitY;
                    touchedPixelY = renderedAreaPortraitWidth - touchedPixelPortraitX;
                    break;
            }

            // Scaling in case rendering resolution does not match screen pixels
            float scaleX;
            float scaleY;
            if (m_ScreenSimulation.IsRenderingLandscape)
            {
                scaleX = m_ScreenSimulation.Width / renderedAreaPortraitHeight;
                scaleY = m_ScreenSimulation.Height / renderedAreaPortraitWidth;
            }
            else
            {
                scaleX = m_ScreenSimulation.Width / renderedAreaPortraitWidth;
                scaleY = m_ScreenSimulation.Height / renderedAreaPortraitHeight;
            }

            return new Vector2(touchedPixelX * scaleX, touchedPixelY * scaleY);
        }

        public void CancelAllTouches()
        {
            if (m_TouchFromMouseActive)
            {
                m_TouchFromMouseActive = false;
                m_InputManagerBackend?.Touch(0, Vector2.zero, SimulatorTouchPhase.Canceled);
            }
        }

        public void Dispose()
        {
            CancelAllTouches();
            m_InputManagerBackend?.Dispose();
        }
    }
}
