// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Bindings
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Event, Inherited = false)]
    [VisibleToOtherModules]
    class VisibleToOtherModulesAttribute : Attribute
    {
        // This attributes controls visibility of internal types and members to other modules.
        // See https://internaldocs.unity.com/editor_and_runtime_development_guide/DevelopmentProcess/authoring-changes/modules/#managed-code-in-modules for details.
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

    // This is a set of attributes used to override conventional behaviour in the bindings generator.
    // Please refer to bindings generator documentation.


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
    [VisibleToOtherModules]
    class NativeConditionalAttribute : Attribute, IBindingsAttribute
    {
        /// <summary>
        /// Native conditional define
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Custom value to return when the condition is not met.
        /// </summary>
        public string StubReturnStatement { get; set; }

        public NativeConditionalAttribute()
        {
        }

        public NativeConditionalAttribute(string condition)
        {
            Condition = condition;
        }

        public NativeConditionalAttribute(string condition, string stubReturnStatement) : this(condition)
        {
            StubReturnStatement = stubReturnStatement;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true)]
    [VisibleToOtherModules]
    class NativeHeaderAttribute : Attribute, IBindingsAttribute
    {
        public string Header { get; }

        public NativeHeaderAttribute(string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (header == "") throw new ArgumentException("header cannot be empty", "header");

            Header = header;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [VisibleToOtherModules]
    sealed class NativeNameAttribute : Attribute, IBindingsAttribute
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

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    [VisibleToOtherModules]
    class NativeMethodAttribute : Attribute, IBindingsAttribute
    {
        public string Name { get; set; }
        public bool IsThreadSafe { get; set; }
        public bool IsFreeFunction { get; set; }
        public bool ThrowsException { get; set; }
        public bool HasExplicitThis { get; set; }

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

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
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
    sealed class NativeTypeAttribute : Attribute, IBindingsAttribute
    {
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

        public NativeTypeAttribute(CodegenOptions codegenOptions, string intermediateStructName) : this(codegenOptions)
        {
            IntermediateScriptingStructName = intermediateStructName;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    [VisibleToOtherModules]
    class NotNullAttribute : Attribute, IBindingsAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [VisibleToOtherModules]
    [Obsolete("This attribute is not supported - consider using blittable types or supported marshaling - or if native code requires a ScriptingObjectPtr use [UnityMarshalAs(NativeType.ScriptingObjectPtr)]", error: true)]
    /// <summary>
    /// This attribute is no longer supported.  For GC safety types will be marshalled in some way
    /// If possible rely on the supported marshaling, or blittable types
    /// If you wish to pass a managed object to native code as a ScriptingObjectPtr mark the parameter, type, or field as [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
    /// If native code needs a GCHandle use GCHandle marshalling [UnityMarshalAs(NativeType.GCHandle, GCHandleOptions = /* See below docs for GCHandleOptions below */)]
    /// See https://internaldocs.unity.com/version/neutron/main/index.html or #devs-bindings for more information
    /// </summary>
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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [VisibleToOtherModules]
    sealed class ThreadSafeAttribute : Attribute, IBindingsAttribute
    {
    }

    [VisibleToOtherModules]
    enum StaticAccessorType
    {
        Dot,
        Arrow,
        DoubleColon,
        ArrowWithDefaultReturnIfNull
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

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    [VisibleToOtherModules]
    class NativeThrowsAttribute : Attribute, IBindingsAttribute
    {
    }

    /// <summary>
    /// Ignore a field for marshaling - the field will not be marshaled to native code
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [VisibleToOtherModules]
    class IgnoreAttribute : Attribute, IBindingsAttribute
    {
        /// <summary>
        /// Used to ignore this field for size calculations.
        /// This is used to handle union types because the bindings generator does not support explicit layout with overlapping fields
        /// In general you should not marshal unions because this can lead to undefined behavior.
        /// </summary>
        public bool DoesNotContributeToSize { get; set; }
    }

    [VisibleToOtherModules]
    enum PreventExecutionSeverity
    {
        PreventExecution_Error,
        PreventExecution_ManagedException,
        PreventExecution_Warning
    }


    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    sealed class PreventExecutionInStateAttribute : Attribute
    {
        public object singleFlagValue { get; set; }
        public PreventExecutionSeverity severity { get; set; }
        public string howToFix { get; set; }

        public PreventExecutionInStateAttribute(object systemAndFlags, PreventExecutionSeverity reportSeverity, string howToString = "")
        {
            singleFlagValue = systemAndFlags;
            severity = reportSeverity;
            howToFix = howToString;
        }
    }

    /// <summary>
    ///  Use this attribute on a class if there is a need to be able to make Read-only instances.
    ///  Any Setters will check if the HideFlags.NotEditable is set to true, and if so, an exception will be thrown to prevent data modification.
    ///  Only works on classes that can create an instance (not static or abstract classes) and that contain Setters.
    /// </summary>
    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class PreventReadOnlyInstanceModificationAttribute : Attribute
    {
        public PreventReadOnlyInstanceModificationAttribute()
        {
        }
    }


    [VisibleToOtherModules]
    interface IBindingsMarshalAsSpan
    {
        bool IsReadOnly { get; }
        string SizeParameter { get; }
    }

    [VisibleToOtherModules]
    internal enum NativeType
    {
        /// <summary>
        /// A CustomMarshaller must be specified
        /// </summary>
        Custom,
        /// <summary>
        /// Marshal the reference as a ScriptingObjectPtr (ScritptingArrayPtr for arrays)
        /// </summary>
        ScriptingObjectPtr,

        /// <summary>
        /// Pass a GCHandle to native code, native code is responsible for freeing the GC handle
        /// GCHandleOptions must be specified
        /// </summary>
        GCHandle,

        /// <summary>
        /// Marshal as if this where a different type (Currently only implemented for arrays/lists of UnityEngine.Object types)
        /// </summary>
        MarshalAsType,
    }

    [VisibleToOtherModules]
    internal enum GCHandleOptions
    {
        Strong  = 0,
        Weak    = 1,   
        Pinned  = 2,
    }

    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Method)]
    class UnityMarshalThisAsAttribute : UnityMarshalAsAttribute
    {
        public UnityMarshalThisAsAttribute(NativeType nativeType) : base(nativeType)
        {
        }
    }


    /// <summary>
    /// Applies Unity specific marshaling to a type/field/parameter/return.  This will override the default marshaling behavior.
    /// </summary>
    [VisibleToOtherModules]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    class UnityMarshalAsAttribute : Attribute, IBindingsAttribute
    {
        /// <summary>
        /// Specifies the native type to marshal as
        /// </summary>
        public NativeType NativeType { get; }

        /// <summary>
        /// When NativeType is NativeType.Custom, this specifies the custom marshaller class to use
        /// </summary>
        public Type CustomMarshaller { get; set; }

        /// <summary>
        /// When NativeType is NativeType.MarshalAsType, marshal the type as if it was this type
        /// This is currently only implemented for arrays/lists of UnityEngine.Object types and is indented to preserve previous marshaling behavior
        /// </summary>
        public Type MarshalAsType { get; set; }

        /// <summary>
        /// When NativeType is NativeType.GCHandle, specifies the GCHandleOptions to use
        /// </summary>
        public GCHandleOptions GCHandleOptions { get; set; }

        public UnityMarshalAsAttribute(NativeType nativeType)
        {
            NativeType = nativeType;
        }
    }

    /// <summary>
    /// Causes a Debugger.Launch() when the target is marshhalled
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [VisibleToOtherModules]
    internal class BindingsGeneratorLaunchDebuggerAttribute : Attribute
    {
        public BindingsGeneratorLaunchDebuggerAttribute()
        {
        }
    }

    /// <summary>
    // Prevents the bindings generator from generating bindings method this is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [VisibleToOtherModules]
    internal class BindingsGeneratorIgnoreAttribute : Attribute
    {
        public BindingsGeneratorIgnoreAttribute()
        {
        }
    }
}
