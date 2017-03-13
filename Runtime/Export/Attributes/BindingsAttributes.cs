// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Bindings
{
    internal interface IBindingsAttribute
    {
    }

    internal interface IBindingsNameProviderAttribute : IBindingsAttribute
    {
        string Name { get; set; }
    }

    internal interface IBindingsHeaderProviderAttribute : IBindingsAttribute
    {
        string Header { get; set; }
    }

    internal interface IBindingsIsThreadSafeProviderAttribute : IBindingsAttribute
    {
        bool IsThreadSafe { get; set; }
    }

    internal interface IBindingsIsFreeFunctionProviderAttribute : IBindingsAttribute
    {
        bool IsFreeFunction { get; set; }
    }

    internal interface IBindingsGenerateMarshallingTypeAttribute : IBindingsNameProviderAttribute
    {
        NativeStructGenerateOption GenerateMarshallingType { get; set; }
    }

    internal abstract class NativeMemberAttribute : Attribute, IBindingsHeaderProviderAttribute, IBindingsNameProviderAttribute
    {
        public string Name { get; set; }
        public string Header { get; set; }

        protected NativeMemberAttribute()
        {
        }

        protected NativeMemberAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }

        protected NativeMemberAttribute(string name, string header)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            Name = name;
            Header = header;
        }
    }

    // This is a set of attributes used to override conventional behaviour in the bindings generator.
    // Please refer to bindings generator documentation.


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    internal class NativeConditionalAttribute : Attribute, IBindingsAttribute
    {
        public string Condition { get; set; }
        public bool Enabled { get; set; }

        public NativeConditionalAttribute()
        {
        }

        public NativeConditionalAttribute(string condition)
        {
            Condition = condition;
            Enabled = true;
        }

        public NativeConditionalAttribute(bool enabled)
        {
            Enabled = enabled;
        }

        public NativeConditionalAttribute(string condition, bool enabled) : this(condition)
        {
            Enabled = enabled;
        }
    }


    [AttributeUsage(AttributeTargets.Enum)]
    internal class NativeEnumAttribute : NativeMemberAttribute
    {
        public bool GenerateNativeType { get; set; }

        public NativeEnumAttribute()
        {
            GenerateNativeType = false;
        }

        public NativeEnumAttribute(string name) : base(name)
        {
            GenerateNativeType = false;
        }

        public NativeEnumAttribute(string name, string header) : base(name, header)
        {
            GenerateNativeType = false;
        }

        public NativeEnumAttribute(bool generateNativeType)
        {
            GenerateNativeType = generateNativeType;
        }

        public NativeEnumAttribute(string name, bool generateNativeType) : base(name)
        {
            GenerateNativeType = generateNativeType;
        }

        public NativeEnumAttribute(string name, string header, bool generateNativeType) : base(name, header)
        {
            GenerateNativeType = generateNativeType;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class NativeGetterAttribute : Attribute, IBindingsNameProviderAttribute
    {
        public string Name { get; set; }

        public NativeGetterAttribute()
        {
        }

        public NativeGetterAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    internal class NativeIncludeAttribute : Attribute, IBindingsHeaderProviderAttribute
    {
        public string Header { get; set; }

        public NativeIncludeAttribute()
        {
        }

        public NativeIncludeAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            Header = header;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class NativeMethodAttribute : Attribute, IBindingsNameProviderAttribute, IBindingsIsThreadSafeProviderAttribute, IBindingsIsFreeFunctionProviderAttribute
    {
        public string Name { get; set; }
        public bool IsThreadSafe { get; set; }
        public bool IsFreeFunction { get; set; }

        public NativeMethodAttribute()
        {
        }

        public NativeMethodAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }

        public NativeMethodAttribute(string name, bool isFreeFunction) : this(name)
        {
            IsFreeFunction = isFreeFunction;
        }

        public NativeMethodAttribute(string name, bool isFreeFunction, bool isThreadSafe) : this(name, isFreeFunction)
        {
            IsThreadSafe = isThreadSafe;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Field)]
    internal class NativeNameAttribute : Attribute, IBindingsNameProviderAttribute
    {
        public string Name { get; set; }

        public NativeNameAttribute()
        {
        }

        public NativeNameAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NativeParameterAttribute : Attribute, IBindingsAttribute
    {
        public bool Unmarshalled { get; set; }

        public NativeParameterAttribute()
        {
        }

        public NativeParameterAttribute(bool unmarshalled)
        {
            Unmarshalled = unmarshalled;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NotNullAttribute : Attribute, IBindingsAttribute
    {
        public NotNullAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class NativePropertyAttribute : Attribute, IBindingsNameProviderAttribute, IBindingsIsThreadSafeProviderAttribute
    {
        public string Name { get; set; }
        public bool IsThreadSafe { get; set; }

        public NativePropertyAttribute()
        {
        }

        public NativePropertyAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }

        public NativePropertyAttribute(bool isThreadSafe)
        {
            IsThreadSafe = isThreadSafe;
        }

        public NativePropertyAttribute(string name, bool isThreadSafe) : this(name)
        {
            IsThreadSafe = isThreadSafe;
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class NativeSetterAttribute : Attribute, IBindingsNameProviderAttribute
    {
        public string Name { get; set; }

        public NativeSetterAttribute()
        {
        }

        public NativeSetterAttribute(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name == "") throw new ArgumentException("name cannot be empty", "name");

            Name = name;
        }
    }


    internal enum NativeStructGenerateOption
    {
        Default,
        UseCustomStruct,
        ForceGenerate
    }

    [AttributeUsage(AttributeTargets.Struct)]
    internal class NativeStructAttribute : NativeMemberAttribute, IBindingsGenerateMarshallingTypeAttribute
    {
        public NativeStructGenerateOption GenerateMarshallingType { get; set; }

        public NativeStructAttribute()
        {
            GenerateMarshallingType = NativeStructGenerateOption.Default;
        }

        public NativeStructAttribute(NativeStructGenerateOption generateMarshallingType)
        {
            GenerateMarshallingType = generateMarshallingType;
        }

        public NativeStructAttribute(string name) : base(name)
        {
            GenerateMarshallingType = NativeStructGenerateOption.Default;
        }

        public NativeStructAttribute(string name, string header) : base(name, header)
        {
            GenerateMarshallingType = NativeStructGenerateOption.Default;
        }

        public NativeStructAttribute(string name, NativeStructGenerateOption generateMarshallingType) : base(name)
        {
            GenerateMarshallingType = generateMarshallingType;
        }

        public NativeStructAttribute(string name, string header, NativeStructGenerateOption generateMarshallingType) : base(name, header)
        {
            GenerateMarshallingType = generateMarshallingType;
        }

        public string IntermediateScriptingStructName { get; set; }
    }


    [AttributeUsage(AttributeTargets.Class)]
    internal class NativeTypeAttribute : NativeMemberAttribute, IBindingsGenerateMarshallingTypeAttribute
    {
        public NativeStructGenerateOption GenerateMarshallingType { get; set; }

        public NativeTypeAttribute()
        {
            GenerateMarshallingType = NativeStructGenerateOption.UseCustomStruct;
        }

        public NativeTypeAttribute(NativeStructGenerateOption generateMarshallingType)
        {
            GenerateMarshallingType = generateMarshallingType;
        }

        public NativeTypeAttribute(string name) : base(name)
        {
            GenerateMarshallingType = NativeStructGenerateOption.UseCustomStruct;
        }

        public NativeTypeAttribute(string name, string header) : base(name, header)
        {
            GenerateMarshallingType = NativeStructGenerateOption.UseCustomStruct;
        }

        public NativeTypeAttribute(string name, NativeStructGenerateOption generateMarshallingType) : base(name)
        {
            GenerateMarshallingType = generateMarshallingType;
        }

        public NativeTypeAttribute(string name, string header, NativeStructGenerateOption generateMarshallingType) : base(name, header)
        {
            GenerateMarshallingType = generateMarshallingType;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class Unmarshalled : NativeParameterAttribute
    {
        public Unmarshalled()
        {
            Unmarshalled = true;
        }
    }

    internal enum InvocationTargetKind
    {
        Pointer,
        NonPointer
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    internal class NativeInstanceAttribute : Attribute, IBindingsAttribute
    {
        public string Accessor { get; set; }

        public InvocationTargetKind InvocationTargetKind { get; set; }

        public NativeInstanceAttribute()
        {
            InvocationTargetKind = InvocationTargetKind.NonPointer;
        }

        internal NativeInstanceAttribute(string accessor)
        {
            Accessor = accessor;
            InvocationTargetKind = InvocationTargetKind.NonPointer;
        }

        public NativeInstanceAttribute(InvocationTargetKind kind)
        {
            InvocationTargetKind = kind;
        }

        public NativeInstanceAttribute(string accessor, InvocationTargetKind kind)
        {
            Accessor = accessor;
            InvocationTargetKind = kind;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal class FreeFunction : NativeMethodAttribute
    {
        public FreeFunction()
        {
            IsFreeFunction = true;
        }

        public FreeFunction(string name) : base(name, true)
        {
        }

        public FreeFunction(string name, bool isThreadSafe) : base(name, true, isThreadSafe)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    internal class ThrowsAttribute : Attribute, IBindingsAttribute
    {
        public ThrowsAttribute()
        {
        }
    }
}
