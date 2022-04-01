// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    public partial class CommandBuffer
    {
        [Obsolete("CommandBuffer.CreateGPUFence has been deprecated. Use CreateGraphicsFence instead (UnityUpgradable) -> CreateAsyncGraphicsFence(*)", false)]
        public GPUFence CreateGPUFence(SynchronisationStage stage) { return new GPUFence(); }

        [Obsolete("CommandBuffer.CreateGPUFence has been deprecated. Use CreateGraphicsFence instead (UnityUpgradable) -> CreateAsyncGraphicsFence()", false)]
        public GPUFence CreateGPUFence() { return new GPUFence(); }

        [Obsolete("CommandBuffer.WaitOnGPUFence has been deprecated. Use WaitOnGraphicsFence instead (UnityUpgradable) -> WaitOnAsyncGraphicsFence(*)", false)]
        public void WaitOnGPUFence(GPUFence fence, SynchronisationStage stage) {}

        [Obsolete("CommandBuffer.WaitOnGPUFence has been deprecated. Use WaitOnGraphicsFence instead (UnityUpgradable) -> WaitOnAsyncGraphicsFence(*)", false)]
        public void WaitOnGPUFence(GPUFence fence) {}

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData(*)", false)]
        public void SetComputeBufferData(ComputeBuffer buffer, Array data)
        {
            SetBufferData(buffer, data);
        }

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData<T>(*)", false)]
        public void SetComputeBufferData<T>(ComputeBuffer buffer, List<T> data) where T : struct
        {
            SetBufferData(buffer, data);
        }

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData<T>(*)", false)]
        public void SetComputeBufferData<T>(ComputeBuffer buffer, NativeArray<T> data) where T : struct
        {
            SetBufferData(buffer, data);
        }

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData(*)", false)]
        public void SetComputeBufferData(ComputeBuffer buffer, Array data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count)
        {
            SetBufferData(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        }

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData<T>(*)", false)]
        public void SetComputeBufferData<T>(ComputeBuffer buffer, List<T> data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct
        {
            SetBufferData(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        }

        [Obsolete("CommandBuffer.SetComputeBufferData has been deprecated. Use SetBufferData instead (UnityUpgradable) -> SetBufferData<T>(*)", false)]
        public void SetComputeBufferData<T>(ComputeBuffer buffer, NativeArray<T> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct
        {
            SetBufferData(buffer, data, nativeBufferStartIndex, graphicsBufferStartIndex, count);
        }

        [Obsolete("CommandBuffer.SetComputeBufferCounterValue has been deprecated. Use SetBufferCounterValue instead (UnityUpgradable) -> SetBufferCounterValue(*)", false)]
        public void SetComputeBufferCounterValue(ComputeBuffer buffer, uint counterValue)
        {
            SetBufferCounterValue(buffer, counterValue);
        }
    }
}
