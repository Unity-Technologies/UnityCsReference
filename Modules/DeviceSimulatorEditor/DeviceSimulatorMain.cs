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
        private SystemInfoSimulation m_SystemInfoSimulation;
        private readonly DeviceSimulator m_DeviceSimulator;
        private readonly ApplicationSimulation m_ApplicationSimulation;
        private readonly UserInterfaceController m_UserInterface;
        private readonly PluginController m_PluginController;
        private readonly TouchEventManipulator m_TouchInput;

        public UserInterfaceController userInterface => m_UserInterface;
        public PlayModeView playModeView { get; }

        public Vector2 targetSize => new Vector2(m_ScreenSimulation.currentResolution.width, m_ScreenSimulation.currentResolution.height);

        public RenderTexture displayTexture
        {
            set
            {
                m_UserInterface.DeviceView.PreviewTexture = value.IsCreated() ? value : null;
            }
        }

        private DeviceInfoAsset[] m_Devices;
        public DeviceInfoAsset[] devices => m_Devices;
        public DeviceInfoAsset currentDevice => m_Devices[m_DeviceIndex];
        public Vector2 mousePositionInUICoordinates =>
            m_TouchInput.isPointerInsideDeviceScreen ? new Vector2(m_TouchInput.pointerPosition.x, m_ScreenSimulation.Height - m_TouchInput.pointerPosition.y) : Vector2.negativeInfinity;

        private int m_DeviceIndex;
        public int deviceIndex
        {
            get => m_DeviceIndex;
            set
            {
                m_DeviceIndex = value;
                InitSimulation();
            }
        }

        private int m_ScreenIndex;
        public ScreenData currentScreen => currentDevice.deviceInfo.screens[m_ScreenIndex];

        public DeviceSimulatorMain(SimulatorState serializedState, VisualElement rootVisualElement, PlayModeView view)
        {
            playModeView = view;
            if (serializedState == null)
                serializedState = new SimulatorState();
            m_Devices = DeviceLoader.LoadDevices();
            InitDeviceIndex(serializedState);
            m_ApplicationSimulation = new ApplicationSimulation(serializedState);
            m_DeviceSimulator = new DeviceSimulator {applicationSimulation = m_ApplicationSimulation};
            m_PluginController = new PluginController(serializedState, m_DeviceSimulator);
            m_TouchInput = new TouchEventManipulator(m_DeviceSimulator);
            m_UserInterface = new UserInterfaceController(this, rootVisualElement, serializedState, m_PluginController, m_TouchInput);
            m_UserInterface.OnScreenToggled += HandleScreenChange;
            InitSimulation();
        }

        public void Dispose()
        {
            m_TouchInput.Dispose();
            m_ScreenSimulation.Dispose();
            m_ApplicationSimulation.Dispose();
            m_SystemInfoSimulation.Dispose();
            m_PluginController.Dispose();
        }

        public void Enable()
        {
            m_ScreenSimulation.Enable();
            m_ApplicationSimulation.Enable();
            m_SystemInfoSimulation.Enable();
            m_UserInterface.OnSimulationStateChanged(SimulationState.Enabled);
        }

        public void Disable()
        {
            m_ScreenSimulation.Disable();
            m_ApplicationSimulation.Disable();
            m_SystemInfoSimulation.Disable();
            m_UserInterface.OnSimulationStateChanged(SimulationState.Disabled);
        }

        public void InitSimulation()
        {
            m_ScreenSimulation?.Dispose();
            m_SystemInfoSimulation?.Dispose();

            if (currentDevice.deviceInfo.screens.Length > 1)
            {
                m_UserInterface.SetScreenToggleVisibility(true);
                m_UserInterface.SetScreenToggleName(string.IsNullOrEmpty(currentScreen.presentation.name) ? $"Screen {m_ScreenIndex + 1}" : currentScreen.presentation.name);
            }
            else
            {
                m_UserInterface.SetScreenToggleVisibility(false);
                m_ScreenIndex = 0;
            }

            var playerSettings = new SimulationPlayerSettings();
            m_ScreenSimulation = new ScreenSimulation(currentDevice.deviceInfo, playerSettings, m_ScreenIndex);
            m_SystemInfoSimulation = new SystemInfoSimulation(currentDevice, playerSettings);
            m_TouchInput.SetDevice(m_ScreenSimulation, currentDevice.deviceInfo.IsAndroidDevice());

            m_UserInterface.OnSimulationStart(m_ScreenSimulation);
            InitScreenUI();

            m_ApplicationSimulation.OnSimulationStart(currentDevice.deviceInfo);
        }

        private void InitDeviceIndex(SimulatorState serializedState)
        {
            m_DeviceIndex = 0;
            if (string.IsNullOrEmpty(serializedState.friendlyName))
                return;

            for (int index = 0; index < m_Devices.Length; ++index)
            {
                if (m_Devices[index].deviceInfo.friendlyName == serializedState.friendlyName)
                {
                    m_DeviceIndex = index;
                    break;
                }
            }

            if (currentDevice.deviceInfo.screens.Length > serializedState.screenIndex)
                m_ScreenIndex = serializedState.screenIndex;

            PlayModeAnalytics.SimulatorSelectDeviceEvent(currentDevice.deviceInfo.friendlyName);
        }

        private void InitScreenUI()
        {
            var overlayTexture = DeviceLoader.LoadOverlay(currentDevice, m_ScreenIndex);
            m_TouchInput.SetScreen(overlayTexture, currentScreen);
            m_UserInterface.DeviceView.OverlayTexture = overlayTexture;
            m_UserInterface.DeviceView.SetDevice(currentScreen.width, currentScreen.height, currentScreen.presentation.borderSize);
            m_UserInterface.DeviceView.ScreenOrientation = m_ScreenSimulation.orientation;
            m_UserInterface.DeviceView.ScreenInsets = m_ScreenSimulation.Insets;
            m_UserInterface.DeviceView.SafeArea = m_ScreenSimulation.ScreenSpaceSafeArea;
        }

        public SimulatorState SerializeSimulatorState()
        {
            var serializedState = new SimulatorState()
            {
                friendlyName = currentDevice.deviceInfo.friendlyName,
                screenIndex = m_ScreenIndex
            };
            m_UserInterface.StoreSerializedStates(ref serializedState);
            m_PluginController.StoreSerializationStates(ref serializedState);
            m_ApplicationSimulation.StoreSerializationStates(ref serializedState);

            return serializedState;
        }

        public void UpdateDeviceList()
        {
            var deviceName = currentDevice.deviceInfo.friendlyName;
            m_Devices = DeviceLoader.LoadDevices();

            m_DeviceIndex = 0;
            for (int index = 0; index < m_Devices.Length; ++index)
            {
                if (m_Devices[index].deviceInfo.friendlyName == deviceName)
                {
                    m_DeviceIndex = index;
                    break;
                }
            }

            InitSimulation();
        }

        public void HandleScreenChange()
        {
            m_ScreenIndex++;
            m_ScreenIndex %= currentDevice.deviceInfo.screens.Length;
            m_ScreenSimulation.ChangeScreen(m_ScreenIndex);
            InitScreenUI();

            m_UserInterface.SetScreenToggleName(string.IsNullOrEmpty(currentScreen.presentation.name) ? $"Screen {m_ScreenIndex + 1}" : currentScreen.presentation.name);
        }

        public void HandleInputEvent()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            // The following code makes IMGUI work in-game, it's mostly copied from the GameView class.

            // MouseDown events outside game view rect are not send to scripts but MouseUp events are (see below)
            if (Event.current.rawType == EventType.MouseDown && !m_TouchInput.isPointerInsideDeviceScreen)
                return;

            var editorMousePosition = Event.current.mousePosition;

            // If this is not set IMGUI doesn't know when you drag cursor from one element to another.
            // For example you could press the mouse on a button then drag the cursor onto a TextField, release the mouse and the button would still get pressed.
            Event.current.mousePosition = new Vector2(m_TouchInput.pointerPosition.x, m_ScreenSimulation.Height - m_TouchInput.pointerPosition.y);

            // This sends keyboard events to input systems and UI
            EditorGUIUtility.QueueGameViewInputEvent(Event.current);

            var useEvent = Event.current.rawType != EventType.MouseUp || m_TouchInput.isPointerInsideDeviceScreen;

            if (Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.ValidateCommand)
                useEvent = false;

            if (useEvent)
                Event.current.Use();
            else
                Event.current.mousePosition = editorMousePosition;
        }
    }
}
