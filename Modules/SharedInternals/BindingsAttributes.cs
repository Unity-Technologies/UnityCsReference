// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Bindings
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    [VisibleToOtherModules]
    class VisibleToOtherModulesAttribute : Attribute
    {
        // This attributes controls visibility of internal types and members to other modules.
        // See https://confluence.hq.unity3d.com/display/DEV/Modular+UnityEngine+managed+assemblies+setup for details.
        public VisibleToOtherModulesAttribute()
        {
        }

        public VisibleToOtherModulesAttribute(params string[] modules)
        {
        }
    }

    interface IBindingsAttribute
    {
    }

    interface IBindingsNameProviderAttribute : IBindingsAttribute
    {
        string Name { get; set; }
    }

    interface IBindingsHeaderProviderAttribute : IBindingsAttribute
    {
        string Header { get; set; }
    }

    interface IBindingsIsThreadSafeProviderAttribute : IBindingsAttribute
    {
        bool IsThreadSafe { get; set; }
    }

    interface IBindingsIsFreeFunctionProviderAttribute : IBindingsAttribute
    {
        bool IsFreeFunction { get; set; }
        bool HasExplicitThis { get; set; }
    }

    interface IBindingsThrowsProviderAttribute : IBindingsAttribute
    {
        bool ThrowsException { get; set; }
    }

    interface IBindingsGenerateMarshallingTypeAttribute : IBindingsAttribute
    {
        CodegenOptions CodegenOptions { get; set; }
    }

    interface IBindingsWritableSelfProviderAttribute : IBindingsAttribute
    {
        bool WritableSelf { get; set; }
    }

    // This is a set of attributes used to override conventional behaviour in the bindings generator.
    // Please refer to bindings generator documentation.


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    [VisibleToOtherModules]
    class NativeConditionalAttribute : Attribute, IBindingsAttribute
    {
        public string Condition { get; set; }
        public string StubReturnStatement { get; set; }
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

        public NativeConditionalAttribute(string condition, string stubReturnStatement, bool enabled) : this(condition, stubReturnStatement)
        {
            Enabled = enabled;
        }

        public NativeConditionalAttribute(string condition, string stubReturnStatement) : this(condition)
        {
            StubReturnStatement = stubReturnStatement;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true)]
    [VisibleToOtherModules]
    class NativeHeaderAttribute : Attribute, IBindingsHeaderProviderAttribute
    {
        public string Header { get; set; }

        public NativeHeaderAttribute()
        {
        }

        public NativeHeaderAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            Header = header;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [VisibleToOtherModules]
    class NativeNameAttribute : Attribute, IBindingsNameProviderAttribute
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

    [AttributeUsage(AttributeTargets.Method)]
    [VisibleToOtherModules]
    sealed class NativeWritableSelfAttribute : Attribute, IBindingsWritableSelfProviderAttribute
    {
        public bool WritableSelf { get; set; }

        public NativeWritableSelfAttribute()
        {
            WritableSelf = true;
        }

        public NativeWritableSelfAttribute(bool writable)
        {
            WritableSelf = writable;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    [VisibleToOtherModules]
    class NativeMethodAttribute : Attribute, IBindingsNameProviderAttribute, IBindingsIsThreadSafeProviderAttribute, IBindingsIsFreeFunctionProviderAttribute, IBindingsThrowsProviderAttribute
    {
        public string Name { get; set; }
        public bool IsThreadSafe { get; set; }
        public bool IsFreeFunction { get; set; }
        public bool ThrowsException { get; set; }
        public bool HasExplicitThis { get; set; }
        public bool WritableSelf { get; set; }

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

        public NativeMethodAttribute(string name, bool isFreeFunction, bool isThreadSafe, bool throws) : this(name, isFreeFunction, isThreadSafe)
        {
            ThrowsException = throws;
        }
    }

    [VisibleToOtherModules]
    enum TargetType
    {
        Function,
        Field
    }

    [AttributeUsage(AttributeTargets.Property)]
    [VisibleToOtherModules]
    class NativePropertyAttribute : NativeMethodAttribute
    {
        public TargetType TargetType { get; set; }

        public NativePropertyAttribute()
        {
        }

        public NativePropertyAttribute(string name) : base(name)
        {
        }

        public NativePropertyAttribute(string name, TargetType targetType) : base(name)
        {
            TargetType = targetType;
        }

        public NativePropertyAttribute(string name, bool isFree, TargetType targetType) : base(name, isFree)
        {
            TargetType = targetType;
        }

        public NativePropertyAttribute(string name, bool isFree, TargetType targetType, bool isThreadSafe) : base(name, isFree, isThreadSafe)
        {
            TargetType = targetType;
        }
    }

    [VisibleToOtherModules]
    enum CodegenOptions
    {
        Auto,
        Custom,
        Force
    }

    [AttributeUsage(AttributeTargets.Class)]
    [VisibleToOtherModules]
    class NativeAsStructAttribute : Attribute, IBindingsAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    [VisibleToOtherModules]
    class NativeTypeAttribute : Attribute, IBindingsHeaderProviderAttribute, IBindingsGenerateMarshallingTypeAttribute
    {
        public string Header { get; set; }

        public string IntermediateScriptingStructName { get; set; }

        public CodegenOptions CodegenOptions { get; set; }

        public NativeTypeAttribute()
        {
            CodegenOptions = CodegenOptions.Auto;
        }

        public NativeTypeAttribute(CodegenOptions codegenOptions)
        {
            CodegenOptions = codegenOptions;
        }

        public NativeTypeAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            CodegenOptions = CodegenOptions.Auto;
            Header = header;
        }

        public NativeTypeAttribute(string header, CodegenOptions codegenOptions) : this(header)
        {
            CodegenOptions = codegenOptions;
        }

        public NativeTypeAttribute(CodegenOptions codegenOptions, string intermediateStructName) : this(codegenOptions)
        {
            IntermediateScriptingStructName = intermediateStructName;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    [VisibleToOtherModules]
    class NotNullAttribute : Attribute, IBindingsAttribute
    {
        public NotNullAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    [VisibleToOtherModules]
    class UnmarshalledAttribute : Attribute, IBindingsAttribute
    {
        public UnmarshalledAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    [VisibleToOtherModules]
    class FreeFunctionAttribute : NativeMethodAttribute
    {
        public FreeFunctionAttribute()
        {
            IsFreeFunction = true;
        }

        public FreeFunctionAttribute(string name) : base(name, true)
        {
        }

        public FreeFunctionAttribute(string name, bool isThreadSafe) : base(name, true, isThreadSafe)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    [VisibleToOtherModules]
    class ThreadSafeAttribute : NativeMethodAttribute
    {
        public ThreadSafeAttribute()
        {
            IsThreadSafe = true;
        }
    }

    [VisibleToOtherModules]
    enum StaticAccessorType
    {
        Dot,
        Arrow,
        DoubleColon
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    [VisibleToOtherModules]
    class StaticAccessorAttribute : Attribute, IBindingsAttribute
    {
        public string Name { get; set; }
        public StaticAccessorType Type { get; set; }

        public StaticAccessorAttribute()
        {
        }

        [VisibleToOtherModules]
        internal StaticAccessorAttribute(string name)
        {
            Name = name;
        }

        public StaticAccessorAttribute(StaticAccessorType type)
        {
            Type = type;
        }

        public StaticAccessorAttribute(string name, StaticAccessorType type)
        {
            Name = name;
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    [VisibleToOtherModules]
    class NativeThrowsAttribute : Attribute, IBindingsThrowsProviderAttribute
    {
        public bool ThrowsException { get; set; }

        public NativeThrowsAttribute()
        {
            ThrowsException = true;
        }

        public NativeThrowsAttribute(bool throwsException)
        {
            ThrowsException = throwsException;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    [VisibleToOtherModules]
    class IgnoreAttribute : Attribute, IBindingsAttribute
    {
        public bool DoesNotContributeToSize { get; set; }
    }
}
