// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    public abstract class DeviceSimulatorPlugin
    {
        public DeviceSimulator deviceSimulator { get; internal set; }
        public abstract string title { get; }

        public virtual void OnCreate()
        {
        }

        public virtual void OnDestroy()
        {
        }

        public virtual VisualElement OnCreateUI()
        {
            return null;
        }
    }
}
