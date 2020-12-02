// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal class ApplicationSimulation : ApplicationShimBase
    {
        public SystemLanguage simulatedSystemLanguage { get; set; }
        public NetworkReachability simulatedInternetReachability { get; set; }

        private readonly DeviceInfo m_DeviceInfo;
        private event Application.LowMemoryCallback m_LowMemory;

        public ApplicationSimulation(SimulatorState serializedState, DeviceInfo deviceInfo)
        {
            m_DeviceInfo = deviceInfo;
            simulatedSystemLanguage = serializedState.systemLanguage;
            simulatedInternetReachability = serializedState.networkReachability;
            Enable();
        }

        public void Enable()
        {
            ShimManager.UseShim(this);
        }

        public void Disable()
        {
            ShimManager.RemoveShim(this);
        }

        public new void Dispose()
        {
            Disable();
        }

        public void StoreSerializationStates(ref SimulatorState states)
        {
            states.systemLanguage = simulatedSystemLanguage;
            states.networkReachability = simulatedInternetReachability;
        }

        public override bool isEditor => false;
        public override RuntimePlatform platform => m_DeviceInfo.IsiOSDevice() ? RuntimePlatform.IPhonePlayer : RuntimePlatform.Android;
        public override bool isMobilePlatform => m_DeviceInfo.IsMobileDevice();
        public override bool isConsolePlatform => m_DeviceInfo.IsConsoleDevice();
        public override SystemLanguage systemLanguage => simulatedSystemLanguage;
        public override NetworkReachability internetReachability => simulatedInternetReachability;

        public override event Application.LowMemoryCallback lowMemory
        {
            add => m_LowMemory += value;
            remove => m_LowMemory -= value;
        }

        public void InvokeLowMemory()
        {
            m_LowMemory?.Invoke();
        }
    }
}
