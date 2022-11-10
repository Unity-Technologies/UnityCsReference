// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderType.h")]
    internal struct ShaderTypeInternal : IInternalType<ShaderTypeInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal UInt32 m_Kind; // Enum declaration in ShaderType.h
        internal UInt32 m_Flags; // Enum declaration in ShaderType.h
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_IncludeListHandle;
        internal FoundryHandle m_v0;
        internal UInt32 m_v1;
        internal UInt32 m_v2;
        internal FoundryHandle m_ParentBlockHandle;

        internal static extern ShaderTypeInternal Invalid();

        internal extern bool IsValid { [NativeMethod("IsValid")] get; }
        internal extern bool IsVoid { [NativeMethod("IsVoid")] get; }
        internal extern bool IsScalar { [NativeMethod("IsScalar")] get; }
        internal extern bool IsVector { [NativeMethod("IsVector")] get; }
        internal extern bool IsMatrix { [NativeMethod("IsMatrix")] get; }
        internal extern bool IsStruct { [NativeMethod("IsStruct")] get; }
        internal extern bool IsTexture { [NativeMethod("IsTexture")] get; }
        internal extern bool IsSamplerState { [NativeMethod("IsSamplerState")] get; }
        internal extern bool IsArray { [NativeMethod("IsArray")] get; }
        internal extern bool IsVectorOrScalar { [NativeMethod("IsVectorOrScalar")] get; }
        internal extern bool IsPlaceholder { [NativeMethod("IsPlaceholder")] get; }
        internal extern bool IsDeclaredExternally();

        internal extern int VectorDimension { get; }
        internal extern int MatrixColumns { get; }
        internal extern int MatrixRows { get; }
        internal extern int ArrayElements { get; }
        internal extern FoundryHandle GetArrayElementTypeHandle();
        internal extern FoundryHandle GetScalarTypeHandle();

        internal IEnumerable<StructField> StructFields(ShaderContainer container)
        {
            if (IsStruct)
            {
                var list = new FixedHandleListInternal(m_v0);
                return list.Select<StructField>(container, (handle) => (new StructField(container, handle)));
            }
            return Enumerable.Empty<StructField>();
        }

        internal IEnumerable<ShaderAttribute> Attributes(ShaderContainer container)
        {
            var list = new FixedHandleListInternal(m_AttributeListHandle);
            return list.Select<ShaderAttribute>(container, (handle) => (new ShaderAttribute(container, handle)));
        }

        internal IEnumerable<IncludeDescriptor> Includes(ShaderContainer container)
        {
            var list = new FixedHandleListInternal(m_IncludeListHandle);
            return list.Select<IncludeDescriptor>(container, (handle) => (new IncludeDescriptor(container, handle)));
        }

        internal extern static FoundryHandle Void(ShaderContainer container);
        internal extern static FoundryHandle Scalar(ShaderContainer container, string name, FoundryHandle includeListHandle);
        internal extern static FoundryHandle Vector(ShaderContainer container, FoundryHandle scalarTypeHandle, int dimension);
        internal extern static FoundryHandle Matrix(ShaderContainer container, FoundryHandle scalarTypeHandle, int rows, int cols);
        internal extern static FoundryHandle Texture(ShaderContainer container, string name);
        internal extern static FoundryHandle SamplerState(ShaderContainer container, string name);
        internal extern static FoundryHandle Array(ShaderContainer container, FoundryHandle elementTypeHandle, int elementCount);
        internal extern static FoundryHandle Struct(ShaderContainer container, FoundryHandle typeHandle, string name, FoundryHandle fieldListHandle, FoundryHandle attributeListHandle, FoundryHandle includeListHandle,  FoundryHandle parentBlockHandle, bool declaredExternally);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

        // IInternalType
        ShaderTypeInternal IInternalType<ShaderTypeInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderType : IEquatable<ShaderType>, IPublicType<ShaderType>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderTypeInternal type;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderType IPublicType<ShaderType>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderType(container, handle);

        // public API
        public ShaderContainer Container => container;
        public static ShaderType Invalid => new ShaderType(null, FoundryHandle.Invalid());

        // Exists means it has been allocated (it can be referenced, even if not valid yet)
        public bool Exists => (container != null) && handle.IsValid;

        // Valid means the value is fully built (description is complete) and it is inspectable
        public bool IsValid => Exists && type.IsValid;

        public string Name => container?.GetString(type.m_NameHandle) ?? string.Empty;

        public bool IsVoid => type.IsVoid;
        public bool IsScalar => type.IsScalar;
        public bool IsVector => type.IsVector;
        public bool IsMatrix => type.IsMatrix;
        public bool IsStruct => type.IsStruct;
        public bool IsTexture => type.IsTexture;
        public bool IsSamplerState => type.IsSamplerState;
        public bool IsArray => type.IsArray;
        public bool IsVectorOrScalar => type.IsVectorOrScalar;
        public bool IsPlaceholder => type.IsPlaceholder;
        public bool IsDeclaredExternally => type.IsDeclaredExternally();

        public int VectorDimension => type.IsVector ? type.VectorDimension : (type.IsScalar ? 1 : 0);
        public int MatrixColumns => type.IsMatrix ? type.MatrixColumns : 0;
        public int MatrixRows => type.IsMatrix ? type.MatrixRows : 0;
        public int ArrayElements => type.IsArray ? type.ArrayElements : 0;
        public ShaderType ArrayElementType => (container != null && IsArray) ? new ShaderType(container, type.GetArrayElementTypeHandle()) : ShaderType.Invalid;
        public ShaderType ScalarType => (container != null) ? new ShaderType(container, type.GetScalarTypeHandle()) : ShaderType.Invalid;
        public IEnumerable<StructField> StructFields => type.StructFields(container);
        public IEnumerable<ShaderAttribute> Attributes => type.Attributes(container);
        public IEnumerable<IncludeDescriptor> Includes => type.Includes(container);
        // Not valid until the parent block is finished being built.
        public Block ParentBlock => new Block(container, type.m_ParentBlockHandle);

        public override int GetHashCode() => (container, handle).GetHashCode();

        // Equals and operator == check reference equality - an indication that the two objects are literally the same.
        // ValueEquals does a deep compare of the internal details of both objects, and determines if the two objects
        // are equivalent.  Reference equality implies value equality, but not vice versa.
        public override bool Equals(object obj) => obj is ShaderType other && this.Equals(other);
        public bool Equals(ShaderType other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public static bool operator==(ShaderType lhs, ShaderType rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderType lhs, ShaderType rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderType other)
        {
            return ShaderTypeInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        internal ShaderType(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = (container ? handle : FoundryHandle.Invalid());
            ShaderContainer.Get(container, handle, out type);
        }

        public static ShaderType Void(ShaderContainer container)
        {
            if (container != null)
            {
                var handle = ShaderTypeInternal.Void(container);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Scalar(ShaderContainer container, string name, List<ShaderAttribute> attributes = null)
        {
            if ((container != null) && (name != null))
            {
                var handle = ShaderTypeInternal.Scalar(container, name, FoundryHandle.Invalid());
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Vector(ShaderContainer container, ShaderType scalarType, int dimension)
        {
            if ((container != null) && (container == scalarType.container) && scalarType.IsScalar)
            {
                if ((dimension >= 1) && (dimension <= 4))
                {
                    var handle = ShaderTypeInternal.Vector(container, scalarType.handle, dimension);
                    if (handle.IsValid)
                        return new ShaderType(container, handle);
                }
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Matrix(ShaderContainer container, ShaderType scalarType, int rows, int cols)
        {
            if ((container != null) && (container == scalarType.container) && scalarType.IsScalar)
            {
                if ((rows >= 1) && (rows <= 4) && (cols >= 1) && (cols <= 4))
                {
                    var handle = ShaderTypeInternal.Matrix(container, scalarType.handle, rows, cols);
                    if (handle.IsValid)
                        return new ShaderType(container, handle);
                }
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Texture(ShaderContainer container, string name)
        {
            if ((container != null) && (name != null))
            {
                var handle = ShaderTypeInternal.Texture(container, name);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType SamplerState(ShaderContainer container, string name)
        {
            if ((container != null) && (name != null))
            {
                var handle = ShaderTypeInternal.SamplerState(container, name);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Array(ShaderContainer container, ShaderType elementType, int elementCount)
        {
            if ((container != null) && elementType.IsValid && elementCount >= 0)
            {
                var handle = ShaderTypeInternal.Array(container, elementType.handle, elementCount);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        // use Builder ?
        internal static ShaderType Struct(ShaderContainer container, FoundryHandle typeHandle, string name, FoundryHandle parentBlockHandle, List<StructField> fields, List<ShaderAttribute> attributes = null, List<string> includes = null, bool declaredExternally = false)
        {
            if ((container != null) && !string.IsNullOrEmpty(name))
            {
                bool success = true;
                FoundryHandle fieldListHandle = FixedHandleListInternal.Build(container, fields, (f) => (f.handle));

                var attributeList = FixedHandleListInternal.Build(container, attributes, (a) => (a.handle));
                FoundryHandle includeList = FoundryHandle.Invalid();
                if ((includes != null) && (includes.Count > 0))
                {
                    var includeHandles = new List<FoundryHandle>();
                    foreach (string path in includes)
                    {
                        var builder = new IncludeDescriptor.Builder(container, path);
                        FoundryHandle includeHandle = builder.Build().handle;
                        if (includeHandle.IsValid)
                            includeHandles.Add(includeHandle);
                    }
                    includeList = FixedHandleListInternal.Build(container, includeHandles);
                }
                if (success)
                {
                    var handle = ShaderTypeInternal.Struct(container, typeHandle, name, fieldListHandle, attributeList, includeList, parentBlockHandle, declaredExternally);
                    if (handle.IsValid)
                        return new ShaderType(container, handle);
                }
            }
            return ShaderType.Invalid;
        }

        public class StructBuilder
        {
            ShaderContainer container;
            readonly internal FoundryHandle typeHandle;

            Block.Builder parentBlock;

            string name;
            List<StructField> fields;
            List<ShaderAttribute> attributes;
            List<string> includes;
            bool declaredExternally;
            bool finalized = false;

            // create struct in global namespace
            public StructBuilder(ShaderContainer container, string name) : this(container, name, null) {}

            // create struct in block
            public StructBuilder(Block.Builder blockBuilder, string name) : this(blockBuilder.Container, name, blockBuilder) {}

            internal StructBuilder(ShaderContainer container, string name, Block.Builder blockBuilder)
            {
                this.container = container;
                this.name = name;
                this.typeHandle = container.Create<ShaderTypeInternal>();
                this.parentBlock = blockBuilder;
            }

            public void AddField(ShaderType type, string name)
            {
                var fieldBuilder = new StructField.Builder(container, name, type);
                AddField(fieldBuilder.Build());
            }

            public void AddField(StructField field)
            {
                if (fields == null)
                    fields = new List<StructField>();
                fields.Add(field);
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                if (attributes == null)
                    attributes = new List<ShaderAttribute>();
                attributes.Add(attribute);
            }

            public void AddInclude(string path)
            {
                if (includes == null)
                    includes = new List<string>();
                includes.Add(path);
            }

            public void DeclaredExternally()
            {
                declaredExternally = true;
            }

            public ShaderType Build()
            {
                if (finalized)
                    return new ShaderType(container, typeHandle);
                finalized = true;

                FoundryHandle parentBlockHandle = parentBlock?.blockHandle ?? FoundryHandle.Invalid();
                var structType = ShaderType.Struct(container, typeHandle, name, parentBlockHandle, fields, attributes, includes, declaredExternally);

                if (parentBlock != null)
                {
                    // register with parent block
                    parentBlock.AddType(structType);
                }

                return structType;
            }
        }
    }
}
