// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    public enum WindZoneMode
    {
        Directional,
        Spherical
    }

    [NativeHeader("Modules/Wind/Public/Wind.h")]
    public class WindZone : Component
    {
        extern public WindZoneMode mode {get; set; }
        extern public float radius {get; set; }
        extern public float windMain {get; set; }
        extern public float windTurbulence {get; set; }
        extern public float windPulseMagnitude  {get; set; }
        extern public float windPulseFrequency {get; set; }
    }
}
