// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/LightProbeGroup.h")]
    public sealed partial class LightProbeGroup : Behaviour
    {
        [NativeName("Positions")]
        public extern Vector3[] probePositions { get; set; }
        [NativeName("Dering")]
        public extern bool dering { get; set; }
    }
}
