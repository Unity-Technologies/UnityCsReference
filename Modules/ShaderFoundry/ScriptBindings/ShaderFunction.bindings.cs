// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderFunction.h")]
    internal struct ShaderFunctionInternal : IInternalType<ShaderFunctionInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_BodyHandle;
        internal FoundryHandle m_ReturnTypeHandle;
        internal FoundryHandle m_ParameterListHandle;
        internal FoundryHandle m_IncludeListHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_LocationHandle;
        internal FoundryHandle m_BodyLocationHandle;
        internal bool m_IsStatic;

        [NativeMethod(IsThreadSafe = true)] internal extern static ShaderFunctionInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }
        [NativeMethod(IsThreadSafe = true)] internal extern string GetName(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetBody(ShaderContainer container);

        // IInternalType
        ShaderFunctionInternal IInternalType<ShaderFunctionInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderFunction : IEquatable<ShaderFunction>, IPublicType<ShaderFunction>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderFunctionInternal function;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderFunction IPublicType<ShaderFunction>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderFunction(container, handle);

        // public API
        public ShaderContainer Container => container;
        public static ShaderFunction Invalid => new ShaderFunction(null, FoundryHandle.Invalid());

        public bool Exists => (container != null) && handle.IsValid;
        public bool IsValid => Exists && function.IsValid;
        public bool IsStatic => function.m_IsStatic;
        public string Name => function.GetName(container);
        public string Body => function.GetBody(container);
        public ShaderType ReturnType => new ShaderType(container, function.m_ReturnTypeHandle);
        public IEnumerable<FunctionParameter> Parameters =>
            ListType.Enumerate<FunctionParameter>(container, function.m_ParameterListHandle);

        public IEnumerable<IncludeDescriptor> Includes =>
            ListType.Enumerate<IncludeDescriptor>(container, function.m_IncludeListHandle);

        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, function.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, function.m_ContainingNamespaceHandle);
        public Location Location => new Location(container, function.m_LocationHandle);
        public Location BodyLocation => new Location(container, function.m_BodyLocationHandle);

        internal ShaderFunction(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out function);
        }

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ShaderFunction other && this.Equals(other);
        public bool Equals(ShaderFunction other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderFunction lhs, ShaderFunction rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderFunction lhs, ShaderFunction rhs) => !lhs.Equals(rhs);

        public class Builder : ShaderBuilder
        {
            ShaderContainer container;
            readonly internal FoundryHandle functionHandle = FoundryHandle.Invalid();
            ShaderFoundry.Block.Builder parentBlockBuilder;
            private string name;
            ShaderType returnType = ShaderType.Invalid;
            List<FunctionParameter> parameters;
            List<IncludeDescriptor> includes;
            List<ShaderAttribute> attributes;
            public Namespace containingNamespace;
            public Location location;
            public Location bodyLocation;
            public bool isStatic = false;
            bool finalized = false;

            public ShaderContainer Container => container;

            // Construct a function with void return type in the global scope
            public Builder(ShaderContainer container, string name)
                : this(container, name, container.Void, null)
            {
            }

            // Construct a function with specified return type in the global scope
            public Builder(ShaderContainer container, string name, ShaderType returnType)
                : this(container, name, returnType, null)
            {
            }

            // Construct a function with void return type in the specified block scope
            public Builder(ShaderFoundry.Block.Builder blockBuilder, string name)
                : this(blockBuilder.Container, name, blockBuilder.Container.Void, blockBuilder)
            {
            }

            // Construct a function with specified return type in the specified block scope
            public Builder(ShaderFoundry.Block.Builder blockBuilder, string name, ShaderType returnType)
                : this(blockBuilder.Container, name, returnType, blockBuilder)
            {
            }

            internal Builder(ShaderContainer container, string name, ShaderType returnType, ShaderFoundry.Block.Builder parentBlockBuilder)
            {
                this.container = container;
                this.name = name;
                this.returnType = returnType;
                functionHandle = container.Create<ShaderFunctionInternal>();
                this.parentBlockBuilder = parentBlockBuilder;
                this.containingNamespace = Namespace.Invalid;
            }

            public void AddParameter(FunctionParameter parameter)
            {
                Utilities.AddToList(ref parameters, parameter);
            }

            public void AddInput(ShaderType type, string name)
            {
                var paramBuilder = new FunctionParameter.Builder(container, name, type, true, false);
                AddParameter(paramBuilder.Build());
            }

            public void AddOutput(ShaderType type, string name)
            {
                var paramBuilder = new FunctionParameter.Builder(container, name, type, false, true);
                AddParameter(paramBuilder.Build());
            }

            public void AddInclude(IncludeDescriptor descriptor)
            {
                Utilities.AddToList(ref includes, descriptor);
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref attributes, attribute);
            }

            public ShaderFunction Build()
            {
                if (finalized)
                    return new ShaderFunction(Container, functionHandle);
                finalized = true;

                var body = ConvertToString();

                var shaderFunctionInternal = new ShaderFunctionInternal();
                shaderFunctionInternal.m_NameHandle = container.AddString(name);
                shaderFunctionInternal.m_BodyHandle = container.AddString(body);
                shaderFunctionInternal.m_ReturnTypeHandle = returnType.handle;
                shaderFunctionInternal.m_ParameterListHandle = ListType.Build(container, parameters);
                shaderFunctionInternal.m_IncludeListHandle = ListType.Build(container, includes);
                shaderFunctionInternal.m_AttributeListHandle = ListType.Build(container, attributes);
                shaderFunctionInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                shaderFunctionInternal.m_LocationHandle = location.handle;
                shaderFunctionInternal.m_BodyLocationHandle = bodyLocation.handle;
                shaderFunctionInternal.m_IsStatic = isStatic;
                container.Set(functionHandle, shaderFunctionInternal);
                var builtFunction = new ShaderFunction(container, functionHandle);

                if (parentBlockBuilder != null)
                {
                    // Register the new function with the parent block
                    parentBlockBuilder.AddFunction(builtFunction);
                }

                return builtFunction;
            }
        }
    }
}
