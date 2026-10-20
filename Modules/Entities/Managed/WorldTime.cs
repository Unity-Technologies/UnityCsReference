// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Core;

namespace Unity.Entities
{
    internal struct WorldTime : IComponentData
    {
        public TimeData Time;
    }

    internal struct WorldTimeQueue : IBufferElementData
    {
        public TimeData Time;
    }
}
