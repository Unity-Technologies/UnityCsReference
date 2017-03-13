// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Scripting
{
    [System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class PreserveAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
    internal class UsedByNativeCodeAttribute : Attribute
    {
        public UsedByNativeCodeAttribute()
        {
        }

        public UsedByNativeCodeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
    internal class RequiredByNativeCodeAttribute : Attribute
    {
        public RequiredByNativeCodeAttribute()
        {
        }

        public RequiredByNativeCodeAttribute(string name)
        {
            Name = name;
        }

        public RequiredByNativeCodeAttribute(bool optional)
        {
            Optional = optional;
        }

        public RequiredByNativeCodeAttribute(string name, bool optional)
        {
            Name = name;
            Optional = optional;
        }

        public string Name { get; set; }
        public bool Optional { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    internal class GenerateManagedProxyAttribute : Attribute
    {
    }
}
