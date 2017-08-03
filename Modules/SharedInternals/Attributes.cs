// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Scripting
{
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

    // NOTE(rb): This is a temporary attribute that UnityBindingsParser will
    // generate for all custom methods and properties in the transition process
    // to new bindings generator. It will be removed when migration is complete.
    internal class GeneratedByOldBindingsGeneratorAttribute : System.Attribute
    {
    }
}

namespace UnityEngine
{
    internal class ThreadAndSerializationSafeAttribute : System.Attribute
    {
        public ThreadAndSerializationSafeAttribute()
        {
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Struct)]
    internal class IL2CPPStructAlignmentAttribute : System.Attribute
    {
        public int Align;
        public IL2CPPStructAlignmentAttribute()
        {
            Align = 1;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal class WritableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class RejectDragAndDropMaterial : Attribute
    {
    }
}
