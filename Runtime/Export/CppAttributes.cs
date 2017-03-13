// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    //TodoBC: make all these internal next time we do a breaking release
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    internal class CppIncludeAttribute : Attribute
    {
        public CppIncludeAttribute(string header) {}
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    internal class CppDefineAttribute : Attribute
    {
        public CppDefineAttribute(string symbol, string value) {}
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false)]
    internal class CppBodyAttribute : Attribute
    {
        public CppBodyAttribute(string body) {}
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class CppInvokeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class CppPropertyBodyAttribute : Attribute
    {
        public CppPropertyBodyAttribute(string getterBody, string setterBody) {}
        public CppPropertyBodyAttribute(string getterBody) {}
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class CppPropertyAttribute : Attribute
    {
        public CppPropertyAttribute(string getter, string setter) {}
        public CppPropertyAttribute(string getter) {}
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false)]
    public class ThreadSafeAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false)]
    public class ConstructorSafeAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal class WritableAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Assembly)]
    [RequiredByNativeCode]
    public class AssemblyIsEditorAssembly : Attribute
    {}
}
