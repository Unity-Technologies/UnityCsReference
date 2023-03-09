// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    // This type does not exist in in our refrence profle so we can add it here.
    // The CoreCLR JIT will respect this attribute on methods as it only checks
    // for the attribute by name.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

#nullable enable
        public Type[]? CallConvs;
        public string? EntryPoint;
#nullable disable
    }
}
