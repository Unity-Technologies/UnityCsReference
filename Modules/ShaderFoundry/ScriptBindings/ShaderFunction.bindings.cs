// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderFunction.h")]
    internal struct ShaderFunctionInternal : IInternalType<ShaderFunctionInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_BodyHandle;
        internal FoundryHandle m_ReturnTypeHandle;
        internal FoundryHandle m_ParameterListHandle;
        internal FoundryHandle m_ParentBlockHandle;
        internal FoundryHandle m_IncludeListHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;

        internal extern static ShaderFunctionInternal Invalid();
        internal extern bool IsValid { [NativeMethod("IsValid")] get; }
        internal extern string GetName(ShaderContainer container);
        internal extern string GetBody(ShaderContainer container);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

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

        public string Name => function.GetName(container);
        public string Body => function.GetBody(container);
        public ShaderType ReturnType => new ShaderType(container, function.m_ReturnTypeHandle);
        public IEnumerable<FunctionParameter> Parameters
        {
            get
            {
                var localContainer = container;
                var blockHandles = new FixedHandleListInternal(function.m_ParameterListHandle);
                return blockHandles.Select(localContainer, (handle) => (new FunctionParameter(localContainer, handle)));
            }
        }

        // Not valid until the parent block is finished being built.
        public Block ParentBlock => new Block(Container, function.m_ParentBlockHandle);

        public IEnumerable<IncludeDescriptor> Includes
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(function.m_IncludeListHandle);
                return list.Select<IncludeDescriptor>(localContainer, (handle) => (new IncludeDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<ShaderAttribute> Attributes => function.m_AttributeListHandle.AsListEnumerable(container, (container, handle) => new ShaderAttribute(container, handle));
        public Namespace ContainingNamespace => new Namespace(container, function.m_ContainingNamespaceHandle);

        internal ShaderFunction(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out function);
        }

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderFunction other && this.Equals(other);
        public bool Equals(ShaderFunction other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderFunction lhs, ShaderFunction rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderFunction lhs, ShaderFunction rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderFunction other)
        {
            return ShaderFunctionInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public class Builder : ShaderBuilder
        {
            ShaderContainer container;
            readonly internal FoundryHandle functionHandle = FoundryHandle.Invalid();
            ShaderFoundry.Block.Builder parentBlockBuilder;
            protected string name;
            ShaderType returnType = ShaderType.Invalid;
            List<FunctionParameter> parameters;
            List<IncludeDescriptor> includes;
            List<ShaderAttribute> attributes;
            public Namespace containingNamespace;
            bool finalized = false;

            public ShaderContainer Container => container;

            // Construct a function with void return type in the global scope
            public Builder(ShaderContainer container, string name)
                : this(container, name, container._void, null)
            {
            }

            // Construct a function with specified return type in the global scope
            public Builder(ShaderContainer container, string name, ShaderType returnType)
                : this(container, name, returnType, null)
            {
            }

            // Construct a function with void return type in the specified block scope
            public Builder(ShaderFoundry.Block.Builder blockBuilder, string name)
                : this(blockBuilder.Container, name, blockBuilder.Container._void, blockBuilder)
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
                this.containingNamespace = parentBlockBuilder?.containingNamespace ?? Namespace.Invalid;
            }

            public void AddParameter(FunctionParameter parameter)
            {
                if (parameters == null)
                    parameters = new List<FunctionParameter>();
                parameters.Add(parameter);
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
                if (includes == null)
                    includes = new List<IncludeDescriptor>();
                includes.Add(descriptor);
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                if (attributes == null)
                    attributes = new List<ShaderAttribute>();
                attributes.Add(attribute);
            }

            public ShaderFunction Build()
            {
                if (finalized)
                    return new ShaderFunction(Container, functionHandle);
                finalized = true;

                FoundryHandle parentBlockHandle = parentBlockBuilder?.blockHandle ?? FoundryHandle.Invalid();
                var body = ConvertToString();

                var shaderFunctionInternal = new ShaderFunctionInternal();
                shaderFunctionInternal.m_NameHandle = container.AddString(name);
                shaderFunctionInternal.m_BodyHandle = container.AddString(body);
                shaderFunctionInternal.m_ReturnTypeHandle = container.AddShaderType(returnType, true);
                shaderFunctionInternal.m_ParameterListHandle = FixedHandleListInternal.Build(container, parameters, (p) => (p.handle));
                shaderFunctionInternal.m_ParentBlockHandle = parentBlockHandle;
                shaderFunctionInternal.m_IncludeListHandle = FixedHandleListInternal.Build(container, includes, (i) => (i.handle));
                shaderFunctionInternal.m_AttributeListHandle = FixedHandleListInternal.Build(container, attributes, (a) => (a.handle));
                shaderFunctionInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
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
