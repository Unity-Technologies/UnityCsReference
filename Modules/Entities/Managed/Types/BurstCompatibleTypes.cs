// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    // This exists only to be able to make generic instances of generic methods to test burst compatibility.
    internal struct BurstCompatibleEnableableComponent : IEnableableComponent
    {
        public int UnusedField;
    }

    // This exists only to be able to make generic instances of generic methods to test burst compatibility.
    internal struct BurstCompatibleComponentData : IComponentData
    {
        public int UnusedField;
    }

    // This exists only to be able to make generic instances of generic methods to test burst compatibility.
    internal struct BurstCompatibleSharedComponentData : ISharedComponentData
    {
        public int UnusedField;
    }

    // This exists only to be able to make generic instances of generic methods to test burst compatibility.
    internal struct BurstCompatibleBufferElement : IBufferElementData
    {
        public int UnusedField;
    }

    // This exists only to be able to make generic instances of generic methods to test burst compatibility.
    [DisableAutoCreation]
    partial struct BurstCompatibleSystem : ISystem
    {

    }
}
