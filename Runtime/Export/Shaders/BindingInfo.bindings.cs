// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEngine
{
namespace Shaders
{
    [NativeHeader("Modules/ShaderRuntime/Public/BindingInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct BindingInfo
    {
        public extern ShaderStageFlags ShaderStages
        {
            [NativeMethod("GetStages")]
            get;
        }
        public extern ShaderResourceType ResourceType { get; }
        public extern ShaderResourceOptions ResourceOptions
        {
            [NativeMethod("GetResourceFlags")]
            get;
        }
        public uint Slot { get { return m_Slot; } }
        public extern uint Set { get; }

        private uint m_Slot;
        private uint m_Field1;
        private uint m_Field2;
    }
} // namespace Shaders
} // namespace UnityEngine
