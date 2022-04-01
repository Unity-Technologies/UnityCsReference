// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    // This is a temporary attribute to flag the parts of the API that should be public
    // but are currently flagged internal while under development
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    internal class FoundryAPIAttribute : Attribute
    {
        public FoundryAPIAttribute() {}
    }
}
