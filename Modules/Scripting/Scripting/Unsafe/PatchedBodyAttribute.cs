// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Collections.LowLevel.Unsafe
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [VisibleToOtherModules("UnityEngine.ManagedKernelModule", "UnityEngine.MathematicsModule")]
    internal sealed class PatchedBodyAttribute : Attribute
    {
        public PatchedBodyAttribute(string fieldName)
        {
        }
    }
}
