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

        public Vector2 TargetSize => new Vector2(m_ScreenSimulation.currentResolution.width, m_ScreenSimulation.currentResolution.height);

        public RenderTexture DisplayTexture
        {
            set
            {
                m_UserInterface.PreviewTexture = value.IsCreated() ? value : null;
                m_UserInterface.OverlayTexture = m_Devices[m_DeviceIndex]?.screens[0].presentation.overlay;
            }
        }

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
            m_UserInterface = new UserInterfaceController(this, rootVisualElement);
            InitSimulation();
            m_UserInterface.ApplySerializedStates(serializedState);
        }

        public void Dispose()
        {
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
    }
}
