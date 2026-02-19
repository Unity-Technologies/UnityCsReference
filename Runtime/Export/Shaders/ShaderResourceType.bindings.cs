// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;

namespace UnityEngine
{
namespace Shaders
{
    public enum ShaderResourceType
    {
        // ConstantBuffer<>
        ConstantBuffer,
        // StructuredBuffer<> or ByteAddressBuffer
        Buffer,
        // Buffer<>
        TypedBuffer,
        // Texture<>
        Texture,
        // Texture<> + SamplerState
        CombinedTextureSampler,
        // SamplerState
        Sampler,
        // RaytracingAccelerationStructure
        RayTracingAccelerationStructure,
        // Framebuffer input
        InputTarget,
    }

    [Flags]
    public enum ShaderResourceOptions
    {
        None = 0,

        Readable = 1 << 0,
        Writable = 1 << 1,
    }
} // namespace Shaders
} // namespace UnityEngine
