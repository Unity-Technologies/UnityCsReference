// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal enum MousePhase { Start, Move, End }

    internal class TouchEventManipulator : MouseManipulator
    {
        private bool m_TouchFromMouseActive;
        private Texture m_OverlayTexture;
        private DeviceInfo m_DeviceInfo;
        private ScreenSimulation m_ScreenSimulation;
        private readonly DeviceSimulator m_DeviceSimulator;
        private InputManagerBackend m_InputManagerBackend;
        public Vector2 pointerPosition { private set; get; } = new Vector2(-1, -1);
        public bool isPointerInsideDeviceScreen { private set; get; }

        public Matrix4x4 previewImageRendererSpaceToScreenSpace { get; set; }

        public TouchEventManipulator(DeviceSimulator deviceSimulator)
        {
            activators.Add(new ManipulatorActivationFilter() {button = MouseButton.LeftMouse});
            var playerSettings = PlayerSettings.GetSerializedObject();
            var activeInputHandler = playerSettings.FindProperty("activeInputHandler");
            // 0 -> Input Manager, 1 -> Input System, 2 -> Both
            if (activeInputHandler.intValue == 0 || activeInputHandler.intValue == 2)
                m_InputManagerBackend = new InputManagerBackend();

            m_DeviceSimulator = deviceSimulator;
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

            var position = previewImageRendererSpaceToScreenSpace.MultiplyPoint(evt.localMousePosition);
            TouchFromMouse(position, phase);
        }

        public void InitTouchInput(Texture overlayTexture, DeviceInfo deviceInfo, ScreenSimulation screenSimulation)
        {
            m_OverlayTexture = overlayTexture;
            m_DeviceInfo = deviceInfo;
            m_ScreenSimulation = screenSimulation;
            if (m_InputManagerBackend != null)
            {
                if (deviceInfo.IsAndroidDevice())
                    m_InputManagerBackend.resetDeltaTimeWhenStationary = true;
                else if (deviceInfo.IsiOSDevice())
                    m_InputManagerBackend.resetDeltaTimeWhenStationary = false;
            }
            CancelAllTouches();
        }

        public void TouchFromMouse(Vector2 position, MousePhase mousePhase)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            // Clamping position inside the device screen. UI element that sends input events also includes the device border and we don't want to register inputs there.
            isPointerInsideDeviceScreen = true;
            if (position.x < 0)
            {
                position.x = 0;
                isPointerInsideDeviceScreen = false;
            }
            else if (position.x > m_DeviceInfo.screens[0].width)
            {
                position.x = m_DeviceInfo.screens[0].width;
                isPointerInsideDeviceScreen = false;
            }
            if (position.y < 0)
            {
                position.y = 0;
                isPointerInsideDeviceScreen = false;
            }
            else if (position.y > m_DeviceInfo.screens[0].height)
            {
                position.y = m_DeviceInfo.screens[0].height;
                isPointerInsideDeviceScreen = false;
            }

            pointerPosition = ScreenPixelToTouchCoordinate(position);

            // Test if the touch is over a cutout or notch using the texture (preferred) or the cutouts from the .device (fallback)
            // Texture check uses the originalTouchPosition as it assumes the touch and texture are in portrait regardless of orientation
            // Cutouts check uses the adjusted pointerPosition as we have different cutout positions for different orientations
            if (IsTouchOnCutout(position, pointerPosition))
            {
                isPointerInsideDeviceScreen = false;
            }

            if (!m_TouchFromMouseActive && mousePhase != MousePhase.Start)
                return;

            TouchPhase phase = TouchPhase.Canceled;

            if (!isPointerInsideDeviceScreen)
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        return;
                    case MousePhase.Move:
                    case MousePhase.End:
                        phase = TouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }
            else
            {
                switch (mousePhase)
                {
                    case MousePhase.Start:
                        phase = TouchPhase.Began;
                        m_TouchFromMouseActive = true;
                        break;
                    case MousePhase.Move:
                        phase = TouchPhase.Moved;
                        break;
                    case MousePhase.End:
                        phase = TouchPhase.Ended;
                        m_TouchFromMouseActive = false;
                        break;
                }
            }

            m_DeviceSimulator.OnTouchScreenInput(new TouchEvent(0, pointerPosition, phase));
            m_InputManagerBackend?.Touch(0, pointerPosition, phase);
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
            var renderedAreaPortraitWidth = m_DeviceInfo.screens[0].width - m_ScreenSimulation.Insets.x - m_ScreenSimulation.Insets.z;
            var renderedAreaPortraitHeight = m_DeviceInfo.screens[0].height - m_ScreenSimulation.Insets.y - m_ScreenSimulation.Insets.w;

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

        private bool IsTouchOnCutout(Vector2 originalTouchPosition, Vector2 screenPixelToTouchPosition)
        {
            // If we have and can read the overlay texture (this is more accurate and follows the curves of cutouts)
            if (m_OverlayTexture is Texture2D {isReadable : true} overlayTexture)
            {
                // We use the orientation agnostic touch position, but we need to adjust the 0,0 position from bottom/left to top/left
                var adjustedTouchPositionY = m_DeviceInfo.screens[0].height - originalTouchPosition.y;

                // Calculate the device size:overlay texture ratio
                var screenWidthScale = m_OverlayTexture.width / (m_DeviceInfo.screens[0].presentation.borderSize.x + m_DeviceInfo.screens[0].presentation.borderSize.z + m_DeviceInfo.screens[0].width);
                var screenHeightScale = m_OverlayTexture.height / (m_DeviceInfo.screens[0].presentation.borderSize.y + m_DeviceInfo.screens[0].presentation.borderSize.w + m_DeviceInfo.screens[0].height);

                // Since we're orientation agnostic and always checking using portrait mode we need to take into account the x and w borders.
                var touchToTexturePositionX = (int)((originalTouchPosition.x + m_DeviceInfo.screens[0].presentation.borderSize.x) * screenWidthScale);
                var touchToTexturePositionY = (int)((adjustedTouchPositionY + m_DeviceInfo.screens[0].presentation.borderSize.w) * screenHeightScale);

                // We can consider pixels of over 80% alpha as opaque enough for a cutout
                return overlayTexture.GetPixel(touchToTexturePositionX, touchToTexturePositionY).a > 0.8;
            }

            // just check using the cutouts from the device file (always square)
            foreach (var cutout in m_ScreenSimulation.cutouts)
            {
                if (!cutout.Contains(screenPixelToTouchPosition))
                    continue;

                return true;
            }

            return false;
        }

        public void CancelAllTouches()
        {
            if (m_TouchFromMouseActive)
            {
                m_TouchFromMouseActive = false;
                m_InputManagerBackend?.Touch(0, Vector2.zero, TouchPhase.Canceled);
                m_DeviceSimulator.OnTouchScreenInput(new TouchEvent(0, Vector2.zero, TouchPhase.Canceled));
            }
        }

        public void Dispose()
        {
            CancelAllTouches();
            m_InputManagerBackend?.Dispose();
        }
    }
}
