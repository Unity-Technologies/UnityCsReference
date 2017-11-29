// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
    [VisibleToOtherModules]
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
    [VisibleToOtherModules]
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
    [VisibleToOtherModules]
    internal class GenerateManagedProxyAttribute : Attribute
    {
        public GenerateManagedProxyAttribute()
        {
        }

        public GenerateManagedProxyAttribute(string nativeType)
        {
            NativeType = nativeType;
        }

        public string NativeType { get; set; }
    }

    // NOTE(rb): This is a temporary attribute that UnityBindingsParser will
    // generate for all custom methods and properties in the transition process
    // to new bindings generator. It will be removed when migration is complete.
    [VisibleToOtherModules]
    internal class GeneratedByOldBindingsGeneratorAttribute : System.Attribute
    {
    }
}

namespace UnityEngine
{
    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class AssetFileNameExtensionAttribute : Attribute
    {
        // the extension that should be given to assets of the decorated type when created from contexts that have limited information about the type
        public string preferredExtension { get; }
        // other extensions that assets of the decorated type might be given in special contexts
        public IEnumerable<string> otherExtensions { get; }

        public AssetFileNameExtensionAttribute(string preferredExtension, params string[] otherExtensions)
        {
            this.preferredExtension = preferredExtension;
            this.otherExtensions = otherExtensions;
        }
    }

    [VisibleToOtherModules]
    internal class ThreadAndSerializationSafeAttribute : System.Attribute
    {
        public ThreadAndSerializationSafeAttribute()
        {
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Struct)]
    [VisibleToOtherModules]
    internal class IL2CPPStructAlignmentAttribute : System.Attribute
    {
        public int Align;
        public IL2CPPStructAlignmentAttribute()
        {
            Align = 1;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    [VisibleToOtherModules]
    internal class WritableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    [VisibleToOtherModules]
    internal class RejectDragAndDropMaterial : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    [VisibleToOtherModules]
    internal class UnityEngineModuleAssembly : Attribute
    {
    }
}
