// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal class DeviceSimulatorMain : IDisposable
    {
        private ScreenSimulation m_ScreenSimulation;
        private UserInterfaceController m_UserInterface;
        private TouchEventManipulator m_TouchInput;

        public Vector2 TargetSize => new Vector2(m_ScreenSimulation.currentResolution.width, m_ScreenSimulation.currentResolution.height);

        public RenderTexture DisplayTexture
        {
            set
            {
                m_UserInterface.PreviewTexture = value.IsCreated() ? value : null;
                m_UserInterface.OverlayTexture = m_Devices[m_DeviceIndex]?.screens[0].presentation.overlay;
            }
        }

        public Vector2 MousePositionInUICoordinates =>
            m_TouchInput.IsPointerInsideDeviceScreen ? new Vector2(m_TouchInput.PointerPosition.x, m_ScreenSimulation.Height - m_TouchInput.PointerPosition.y) : Vector2.negativeInfinity;

        private DeviceInfo[] m_Devices;
        public DeviceInfo[] Devices => m_Devices;

        private int m_DeviceIndex;
        public int DeviceIndex
        {
            get => m_DeviceIndex;
            set
            {
                DeviceLoader.UnloadOverlays(m_Devices[m_DeviceIndex]);
                m_DeviceIndex = value;
                InitSimulation();
            }
        }

        public DeviceSimulatorMain(SimulatorSerializationStates serializedState, VisualElement rootVisualElement)
        {
            m_Devices = DeviceLoader.LoadDevices();
            InitDeviceIndex(serializedState);
            m_TouchInput = new TouchEventManipulator();
            m_UserInterface = new UserInterfaceController(this, rootVisualElement, m_TouchInput);
            InitSimulation();
            m_UserInterface.ApplySerializedStates(serializedState);
        }

        public void Dispose()
        {
            m_TouchInput.Dispose();
            m_ScreenSimulation.Dispose();
            DeviceLoader.UnloadOverlays(m_Devices[m_DeviceIndex]);
        }

        public void Enable()
        {
            m_ScreenSimulation.Enable();
            m_UserInterface.OnSimulationStateChanged(SimulationState.Enabled);
        }

        public void Disable()
        {
            m_ScreenSimulation.Disable();
            m_UserInterface.OnSimulationStateChanged(SimulationState.Disabled);
        }

        public void InitSimulation()
        {
            m_ScreenSimulation?.Dispose();

            var playerSettings = new SimulationPlayerSettings();
            DeviceLoader.LoadOverlay(m_Devices[m_DeviceIndex], 0);
            m_ScreenSimulation = new ScreenSimulation(m_Devices[m_DeviceIndex], playerSettings);
            m_TouchInput.InitTouchInput(m_Devices[m_DeviceIndex].screens[0].width, m_Devices[m_DeviceIndex].screens[0].height, m_ScreenSimulation);
            m_UserInterface.OnSimulationStart(m_ScreenSimulation);
        }

        private void InitDeviceIndex(SimulatorSerializationStates states)
        {
            m_DeviceIndex = 0;
            if (states == null || string.IsNullOrEmpty(states.friendlyName))
                return;

            for (int index = 0; index < m_Devices.Length; ++index)
            {
                if (m_Devices[index].friendlyName == states.friendlyName)
                {
                    m_DeviceIndex = index;
                    break;
                }
            }
        }

        public SimulatorSerializationStates SerializeSimulatorState()
        {
            var state = new SimulatorSerializationStates()
            {
                friendlyName = m_Devices[m_DeviceIndex].friendlyName
            };
            m_UserInterface.StoreSerializedStates(ref state);

            return state;
        }

        public void UpdateDeviceList()
        {
            var deviceName = m_Devices[m_DeviceIndex].friendlyName;
            DeviceLoader.UnloadOverlays(m_Devices[m_DeviceIndex]);
            m_Devices = DeviceLoader.LoadDevices();

            m_DeviceIndex = 0;
            for (int index = 0; index < m_Devices.Length; ++index)
            {
                if (m_Devices[index].friendlyName == deviceName)
                {
                    m_DeviceIndex = index;
                    break;
                }
            }

            InitSimulation();
        }

        public void HandleInputEvent()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            // The following code makes IMGUI work in-game, it's mostly copied from the GameView class.

            // MouseDown events outside game view rect are not send to scripts but MouseUp events are (see below)
            if (Event.current.rawType == EventType.MouseDown && !m_TouchInput.IsPointerInsideDeviceScreen)
                return;

            var editorMousePosition = Event.current.mousePosition;

            // If this is not set IMGUI doesn't know when you drag cursor from one element to another.
            // For example you could press the mouse on a button then drag the cursor onto a TextField, release the mouse and the button would still get pressed.
            Event.current.mousePosition = new Vector2(m_TouchInput.PointerPosition.x, m_ScreenSimulation.Height - m_TouchInput.PointerPosition.y);

            // This sends keyboard events to input systems and UI
            EditorGUIUtility.QueueGameViewInputEvent(Event.current);

            var useEvent = Event.current.rawType != EventType.MouseUp || m_TouchInput.IsPointerInsideDeviceScreen;

            if (Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.ValidateCommand)
                useEvent = false;

            if (useEvent)
                Event.current.Use();
            else
                Event.current.mousePosition = editorMousePosition;
        }
    }
}
