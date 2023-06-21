// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Burst
{
    [RequireAttributeUsages]
    [AttributeUsage(AttributeTargets.Method)]
    public class BurstAuthorizedExternalMethodAttribute : Attribute
    {
        // Attribute used to signify that an external method may be called from a Burst-compiled cctor
    }
}
