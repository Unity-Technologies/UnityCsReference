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
    [NativeHeader("Modules/ShaderFoundry/Public/FunctionParameter.h")]
    internal struct FunctionParameterInternal
    {
        // these enums must match the declarations in FunctionParameter.h
        internal enum Flags
        {
            kFlagsInput = 1 << 0,
            kFlagsOutput = 1 << 1,
        };

        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_TypeHandle;
        internal UInt32 m_Flags;

        // TODO no need to make this extern, can duplicate it here
        internal static extern FunctionParameterInternal Invalid();

        internal bool IsValid => (m_NameHandle.IsValid && (m_Flags != 0));

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);
    }

    [FoundryAPI]
    internal readonly struct FunctionParameter : IEquatable<FunctionParameter>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly FunctionParameterInternal param;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null) && handle.IsValid && (param.IsValid);
        public string Name => container?.GetString(param.m_NameHandle) ?? string.Empty;
        public ShaderType Type => new ShaderType(container, param.m_TypeHandle);
        public bool IsInput => ((param.m_Flags & (UInt32)FunctionParameterInternal.Flags.kFlagsInput) != 0);
        public bool IsOutput => ((param.m_Flags & (UInt32)FunctionParameterInternal.Flags.kFlagsOutput) != 0);
        internal UInt32 Flags => param.m_Flags;

        // private
        internal FunctionParameter(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.param = container?.GetFunctionParameter(handle) ?? FunctionParameterInternal.Invalid();
        }

        public static FunctionParameter Invalid => new FunctionParameter(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is FunctionParameter other && this.Equals(other);
        public bool Equals(FunctionParameter other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(FunctionParameter lhs, FunctionParameter rhs) => lhs.Equals(rhs);
        public static bool operator!=(FunctionParameter lhs, FunctionParameter rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in FunctionParameter other)
        {
            return FunctionParameterInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            internal ShaderType type;
            internal UInt32 flags;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, ShaderType type, bool input, bool output)
            {
                this.container = container;
                this.name = name;
                this.type = type;
                this.flags = (input ? (UInt32)FunctionParameterInternal.Flags.kFlagsInput : 0) | (output ? (UInt32)FunctionParameterInternal.Flags.kFlagsOutput : 0);
            }

            internal Builder(ShaderContainer container, string name, ShaderType type, UInt32 flags)
            {
                this.container = container;
                this.name = name;
                this.type = type;
                this.flags = flags;
            }

            public FunctionParameter Build()
            {
                var functionParamInternal = new FunctionParameterInternal();
                functionParamInternal.m_NameHandle = container.AddString(name);
                functionParamInternal.m_TypeHandle = type.handle;
                functionParamInternal.m_Flags = flags;
                FoundryHandle returnHandle = container.AddFunctionParameter(functionParamInternal);
                return new FunctionParameter(Container, returnHandle);
            }
        }
    }
}
