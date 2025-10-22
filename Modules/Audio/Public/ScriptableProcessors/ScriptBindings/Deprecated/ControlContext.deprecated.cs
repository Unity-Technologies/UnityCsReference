// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    public partial struct ControlContext
    {
        /// <undoc/>
        [Obsolete("ControlContext.ProcessorUpdateSetting has been deprecated. Use ProcessorInstance.UpdateSetting instead. (UnityUpgradable) -> ProcessorInstance/UpdateSetting", true)]
        public struct ProcessorUpdateSetting
        {
        }

        /// <undoc/>
        [Obsolete("ControlContext.ProcessorCreationParameters has been deprecated. Use ProcessorInstance.CreationParameters instead. (UnityUpgradable) -> ProcessorInstance/CreationParameters", true)]
        public struct ProcessorCreationParameters
        {
        }

        /// <undoc/>
        [Obsolete("ControlContext.GetAvailableData has been deprecated. Use ControlContext.SendMessage instead.", true)]
        public ProcessorInstance.AvailableData GetAvailableData(ProcessorInstance processorInstance)
        {
            throw new NotImplementedException();
        }

        /// <undoc/>
        [Obsolete("ControlContext.SendData has been deprecated. Use ControlContext.SendMessage instead.", true)]
        public void SendData<T>(ProcessorInstance processorInstance, in T data) where T : unmanaged
        {
            throw new NotImplementedException();
        }
    }
}
