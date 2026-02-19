// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderType.h")]
    internal struct ShaderTypeInternal : IInternalType<ShaderTypeInternal>
    {
        // TODO @ SHADERS SHADERS-277: When managed interfaces for type creation are refactored
        // consider remapping the bindings of any internal enum types so they are fully public and
        // scoped for use by more than one type interface (see enum remappings for KeywordDescriptor).
        internal enum AccessMode
        {
            kNone,
            kRead,
            kWrite,
            kReadWrite,
        };

        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN ShaderType.h
        internal enum Kind
        {
            Invalid,
            Void,
            Scalar,
            Vector,
            Matrix,
            Texture,
            SamplerState,
            Struct,
            Array,
            Resource,
            Enum,
            Buffer,
        }

        internal struct CommonTypeData
        {
            internal FoundryHandle nameHandle;
            internal FoundryHandle containingNamespaceHandle;
            internal FoundryHandle attributeListHandle;
            internal FoundryHandle includeListHandle;
            internal FoundryHandle locationHandle;
        }

        internal struct ScalarTypeData
        {
            // This must match the enum declaration in ShaderType.h
            internal enum Type
            {
                kInvalid = 0,
                kBoolean,
                kInteger,
                kFloatingPoint,
            };
            internal Type type;
            internal UInt32 size;
            internal bool isSigned;
        };

        internal struct VectorTypeData
        {
            internal FoundryHandle baseTypeHandle;
            internal UInt32 dimension;
        };

        internal struct MatrixTypeData
        {
            internal FoundryHandle baseTypeHandle;
            internal UInt32 rows;
            internal UInt32 columns;
        };

        internal struct TextureTypeData
        {
            // These must match the enum declaration in ShaderType.h
            internal enum Dimension
            {
                kInvalid,
                k1D,
                k2D,
                k3D,
                kCubemap,
            };
            internal enum Array
            {
                kFalse,
                kTrue,
            };
            internal enum Multisampled
            {
                kFalse,
                kTrue,
            };
            internal FoundryHandle dataTypeHandle;
            internal Dimension dimension;
            internal Array array;
            internal AccessMode access;
            internal Multisampled multisampled;
        };

        internal struct SamplerStateTypeData
        {
            // This must match the enum declaration in ShaderType.h
            internal enum ComparisonMode
            {
                kOff,
                kOn,
            };
            internal ComparisonMode comparisonMode;
        };

        internal struct ArrayTypeData
        {
            internal FoundryHandle elementTypeHandle;
            internal UInt32 length;
        };

        internal struct StructTypeData
        {
            internal FoundryHandle fieldListHandle;
            internal FoundryHandle functionListHandle;
            internal FoundryHandle generatedIncludePathHandle;
            internal bool declaredExternally;
        }

        internal struct ResourceTypeData
        {
            // These must match the enum declaration in ShaderType.h
            internal enum ResourceKind
            {
                kInvalid,
                kTexture,
                kSampler,
                kSampledTexture,
                kBuffer,
                kUnsafe,
            };

            internal StructTypeData structData;
            internal ResourceKind kind;
            internal FoundryHandle m_Private0;
            internal FoundryHandle m_Private1;
        };

        internal struct EnumTypeData
        {
            internal FoundryHandle literalEntries;
        };

        internal struct BufferTypeData
        {
            internal enum Structured
            {
                kFalse,
                kTrue,
            };
            internal FoundryHandle internalTypeHandle;
            internal Structured structured;
            internal AccessMode access;
        }

        internal struct ScalarInitializationData
        {
            internal CommonTypeData commonData;
            internal ScalarTypeData scalarData;
        }

        internal struct VectorInitializationData
        {
            internal CommonTypeData commonData;
            internal VectorTypeData vectorData;
        }

        internal struct MatrixInitializationData
        {
            internal CommonTypeData commonData;
            internal MatrixTypeData matrixData;
        }

        internal struct TextureInitializationData
        {
            internal CommonTypeData commonData;
            internal TextureTypeData textureData;
        };

        internal struct SamplerStateInitializationData
        {
            internal CommonTypeData commonData;
            internal SamplerStateTypeData samplerStateData;
        };

        internal struct ArrayInitializationData
        {
            internal CommonTypeData commonData;
            internal ArrayTypeData arrayData;
        }

        internal struct StructInitializationData
        {
            internal CommonTypeData commonData;
            internal StructTypeData structData;
        }

        internal struct ResourceInitializationData
        {
            internal CommonTypeData commonData;
            internal ResourceTypeData resourceData;
        }

        internal struct EnumInitializationData
        {
            internal CommonTypeData commonData;
            internal EnumTypeData enumData;
        }

        internal struct BufferInitializationData
        {
            internal CommonTypeData commonData;
            internal BufferTypeData bufferData;
        }

        internal Kind m_Kind;
        internal CommonTypeData m_CommonData;
        internal UInt32 m_Private0;
        internal UInt32 m_Private1;
        internal UInt32 m_Private2;
        internal UInt32 m_Private3;
        internal UInt32 m_Private4;
        internal UInt32 m_Private5;
        internal UInt32 m_Private6;
        internal UInt32 m_Private7;
        internal UInt32 m_Private8;
        internal UInt32 m_Private9;
        internal UInt32 m_Private10;
        internal UInt32 m_Private11;
        internal UInt32 m_Private12;

        [NativeMethod(IsThreadSafe = true)] internal static extern ShaderTypeInternal Invalid();

        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }
        internal extern bool IsVoid { [NativeMethod(Name = "IsVoid", IsThreadSafe = true)] get; }
        internal extern bool IsScalar { [NativeMethod(Name = "IsScalar", IsThreadSafe = true)] get; }
        internal extern bool IsVector { [NativeMethod(Name = "IsVector", IsThreadSafe = true)] get; }
        internal extern bool IsMatrix { [NativeMethod(Name = "IsMatrix", IsThreadSafe = true)] get; }
        internal extern bool IsStruct { [NativeMethod(Name = "IsStruct", IsThreadSafe = true)] get; }
        internal extern bool IsTexture { [NativeMethod(Name = "IsTexture", IsThreadSafe = true)] get; }
        internal extern bool IsSamplerState { [NativeMethod(Name = "IsSamplerState", IsThreadSafe = true)] get; }
        internal extern bool IsArray { [NativeMethod(Name = "IsArray", IsThreadSafe = true)] get; }
        internal extern bool IsResource { [NativeMethod(Name = "IsResource", IsThreadSafe = true)] get; }
        internal extern bool IsEnum { [NativeMethod(Name = "IsEnum", IsThreadSafe = true)] get; }
        internal extern bool IsBuffer { [NativeMethod(Name = "IsBuffer", IsThreadSafe = true)] get; }

        [NativeMethod(IsThreadSafe = true)] internal extern VectorTypeData GetVectorTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern MatrixTypeData GetMatrixTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern ArrayTypeData GetArrayTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern StructTypeData GetStructTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern ResourceTypeData GetResourceTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern EnumTypeData GetEnumTypeData();
        [NativeMethod(IsThreadSafe = true)] internal extern BufferTypeData GetBufferTypeData();

        internal IEnumerable<StructField> StructFields(ShaderContainer container)
        {
            if (IsStruct || IsResource)
            {
                var structData = GetStructTypeData();
                return ListType.Enumerate<StructField>(container, structData.fieldListHandle);
            }
            return System.Array.Empty<StructField>();
        }

        internal IEnumerable<ShaderFunction> StructFunctions(ShaderContainer container)
        {
            if (IsStruct || IsResource)
            {
                var structData = GetStructTypeData();
                return ListType.Enumerate<ShaderFunction>(container, structData.functionListHandle);
            }
            return System.Array.Empty<ShaderFunction>();
        }

        internal IEnumerable<ShaderAttribute> Attributes(ShaderContainer container) =>
            ListType.Enumerate<ShaderAttribute>(container, m_CommonData.attributeListHandle);

        internal IEnumerable<IncludeDescriptor> Includes(ShaderContainer container) =>
            ListType.Enumerate<IncludeDescriptor>(container, m_CommonData.includeListHandle);

        internal ShaderType.SampledTextureTypeData? GetSampledTextureData(ShaderContainer container)
        {
            if (!IsResource)
                return null;

            var resourceData = GetResourceTypeData();
            if (resourceData.kind != ResourceTypeData.ResourceKind.kSampledTexture)
                return null;

            return new ShaderType.SampledTextureTypeData
            {
                textureType = new ShaderType(container, resourceData.m_Private0),
                samplerType = new ShaderType(container, resourceData.m_Private1)
            };
        }

        internal ShaderType? GetTextureData(ShaderContainer container)
        {
            if (!IsResource)
                return null;

            var resourceData = GetResourceTypeData();
            if (resourceData.kind != ResourceTypeData.ResourceKind.kTexture)
                return null;

            return new ShaderType(container, resourceData.m_Private0);
        }

        internal ShaderType? GetSamplerData(ShaderContainer container)
        {
            if (!IsResource)
                return null;

            var resourceData = GetResourceTypeData();
            if (resourceData.kind != ResourceTypeData.ResourceKind.kSampler)
                return null;

            return new ShaderType(container, resourceData.m_Private0);
        }

        internal ShaderType? GetBufferTypeData(ShaderContainer container)
        {
            if (!IsResource)
                return null;

            var resourceData = GetResourceTypeData();
            if (resourceData.kind != ResourceTypeData.ResourceKind.kBuffer)
                return null;

            return new ShaderType(container, resourceData.m_Private0);
        }

        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Void(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Scalar(ShaderContainer container, ScalarInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Vector(ShaderContainer container, VectorInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Matrix(ShaderContainer container, MatrixInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Texture(ShaderContainer container, TextureInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle SamplerState(ShaderContainer container, SamplerStateInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Array(ShaderContainer container, ArrayInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle BuildStruct(ShaderContainer container, FoundryHandle typeHandle, StructInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Resource(ShaderContainer container, FoundryHandle typeHandle, ResourceInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Enum(ShaderContainer container, FoundryHandle typeHandle, EnumInitializationData initData);
        [NativeMethod(IsThreadSafe = true)] internal extern static FoundryHandle Buffer(ShaderContainer container, FoundryHandle typeHandle, BufferInitializationData initData);

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

        public string Name => container?.GetString(type.m_CommonData.nameHandle) ?? string.Empty;

        public bool IsVoid => type.IsVoid;
        public bool IsScalar => type.IsScalar;
        public bool IsVector => type.IsVector;
        public bool IsMatrix => type.IsMatrix;
        public bool IsStruct => type.IsStruct;
        public bool IsTexture => type.IsTexture;
        public bool IsSamplerState => type.IsSamplerState;
        public bool IsArray => type.IsArray;
        public bool IsResource => type.IsResource;
        public bool IsEnum => type.IsEnum;
        public bool IsBuffer => type.IsBuffer;

        internal ShaderTypeInternal.ResourceTypeData.ResourceKind ResourceKind => IsResource ? type.GetResourceTypeData().kind : ShaderTypeInternal.ResourceTypeData.ResourceKind.kInvalid;
        public bool IsResourceSampledTexture => ResourceKind == ShaderTypeInternal.ResourceTypeData.ResourceKind.kSampledTexture;
        public bool IsResourceTexture => ResourceKind == ShaderTypeInternal.ResourceTypeData.ResourceKind.kTexture;
        public bool IsResourceSampler => ResourceKind == ShaderTypeInternal.ResourceTypeData.ResourceKind.kSampler;
        public bool IsResourceBuffer => ResourceKind == ShaderTypeInternal.ResourceTypeData.ResourceKind.kBuffer;

        public struct SampledTextureTypeData
        {
            public ShaderType textureType;
            public ShaderType samplerType;
        }
        public SampledTextureTypeData? SampledTextureData => type.GetSampledTextureData(container);
        public ShaderType? TextureData => type.GetTextureData(container);
        public ShaderType? SamplerData => type.GetSamplerData(container);
        public ShaderType? BufferTypeData => type.GetBufferTypeData(container);
        public IEnumerable<EnumLiteral> EnumLiterals
        {
            get
            {
                FoundryHandle literalEntriesHandle = FoundryHandle.Invalid();
                if (IsEnum)
                    literalEntriesHandle = type.GetEnumTypeData().literalEntries;
                return ListType.Enumerate<EnumLiteral>(container, literalEntriesHandle);
            }
        }

        public uint VectorDimension => type.IsVector ? type.GetVectorTypeData().dimension : (type.IsScalar ? 1u : 0u);
        public uint MatrixColumns => type.IsMatrix ? type.GetMatrixTypeData().columns : 0;
        public uint MatrixRows => type.IsMatrix ? type.GetMatrixTypeData().rows : 0;
        public uint ArrayElements => type.IsArray ? type.GetArrayTypeData().length : 0;
        public ShaderType ArrayElementType => (container != null && IsArray) ? new ShaderType(container, type.GetArrayTypeData().elementTypeHandle) : ShaderType.Invalid;
        public ShaderType VectorBaseType => (container != null && IsVector) ? new ShaderType(container, type.GetVectorTypeData().baseTypeHandle) : ShaderType.Invalid;
        public ShaderType MatrixBaseType => (container != null && IsMatrix) ? new ShaderType(container, type.GetMatrixTypeData().baseTypeHandle) : ShaderType.Invalid;
        public bool StructIsDeclaredExternally => type.IsStruct ? type.GetStructTypeData().declaredExternally : false;
        public IEnumerable<StructField> StructFields => type.StructFields(container);
        public IEnumerable<ShaderFunction> StructFunctions => type.StructFunctions(container);
        public IEnumerable<ShaderAttribute> Attributes => type.Attributes(container);
        public IEnumerable<IncludeDescriptor> Includes => type.Includes(container);
        public Namespace ContainingNamespace => new Namespace(container, type.m_CommonData.containingNamespaceHandle);
        public Location Location => new Location(container, type.m_CommonData.locationHandle);

        public override int GetHashCode() => (container, handle).GetHashCode();

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ShaderType other && this.Equals(other);
        public bool Equals(ShaderType other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public static bool operator==(ShaderType lhs, ShaderType rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderType lhs, ShaderType rhs) => !lhs.Equals(rhs);

        internal ShaderType(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = (container != null ? handle : FoundryHandle.Invalid());
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

        private static ShaderTypeInternal.ScalarTypeData BuildScalarTypeDataFromName(string name)
        {
            // We are only handling the reserved types set up by default container initialization here
            // Initialize for 32-bit float and override based on other names
            var scalarData = new ShaderTypeInternal.ScalarTypeData();
            scalarData.size = 32;
            scalarData.type = ShaderTypeInternal.ScalarTypeData.Type.kFloatingPoint;
            scalarData.isSigned = (name != "bool" && name != "uint") ? true : false;
            if (name == "bool")
                scalarData.type = ShaderTypeInternal.ScalarTypeData.Type.kBoolean;
            else if (name == "int" || name == "uint")
                scalarData.type = ShaderTypeInternal.ScalarTypeData.Type.kInteger;
            else if (name == "half")
                scalarData.size = 16;
            else if (name == "double")
                scalarData.size = 64;

            return scalarData;
        }

        public static ShaderType Scalar(ShaderContainer container, string name)
        {
            if ((container != null) && (name != null))
            {
                var initData = new ShaderTypeInternal.ScalarInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = FoundryHandle.Invalid(),
                        attributeListHandle = FoundryHandle.Invalid(),
                        includeListHandle = FoundryHandle.Invalid(),
                    },
                    scalarData = BuildScalarTypeDataFromName(name),
                };
                var handle = ShaderTypeInternal.Scalar(container, initData);
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
                    string name = String.Format("{0}{1}", scalarType.Name, dimension);
                    var initData = new ShaderTypeInternal.VectorInitializationData
                    {
                        commonData = new ShaderTypeInternal.CommonTypeData
                        {
                            nameHandle = container.AddString(name),
                            containingNamespaceHandle = FoundryHandle.Invalid(),
                            attributeListHandle = FoundryHandle.Invalid(),
                            includeListHandle = FoundryHandle.Invalid(),
                        },
                        vectorData = new ShaderTypeInternal.VectorTypeData
                        {
                            baseTypeHandle = scalarType.handle,
                            dimension = (UInt32)dimension,
                        },
                    };
                    var handle = ShaderTypeInternal.Vector(container, initData);
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
                    string name = String.Format("{0}{1}x{2}", scalarType.Name, rows, cols);
                    var initData = new ShaderTypeInternal.MatrixInitializationData
                    {
                        commonData = new ShaderTypeInternal.CommonTypeData
                        {
                            nameHandle = container.AddString(name),
                            containingNamespaceHandle = FoundryHandle.Invalid(),
                            attributeListHandle = FoundryHandle.Invalid(),
                            includeListHandle = FoundryHandle.Invalid(),
                        },
                        matrixData = new ShaderTypeInternal.MatrixTypeData
                        {
                            baseTypeHandle = scalarType.handle,
                            rows = (UInt32)rows,
                            columns = (UInt32)cols,
                        },
                    };
                    var handle = ShaderTypeInternal.Matrix(container, initData);
                    if (handle.IsValid)
                        return new ShaderType(container, handle);
                }
            }
            return ShaderType.Invalid;
        }

        private static ShaderTypeInternal.TextureTypeData BuildTextureTypeDataFromName(ShaderContainer container, string name)
        {
            // We are only handling the reserved types set up by default container initialization here
            var textureData = new ShaderTypeInternal.TextureTypeData();
            // TODO @ SHADERS: `Contains` may not be the best idea here.
            if (name.Contains("float4"))
                textureData.dataTypeHandle = container.Float4.handle;
            else if (name.Contains("half4"))
                textureData.dataTypeHandle = container.Half4.handle;
            textureData.dimension = ShaderTypeInternal.TextureTypeData.Dimension.k2D;
            textureData.array = ShaderTypeInternal.TextureTypeData.Array.kFalse;
            textureData.access = ShaderTypeInternal.AccessMode.kRead;
            textureData.multisampled = ShaderTypeInternal.TextureTypeData.Multisampled.kFalse;
            if (name.Contains("MS"))
                textureData.multisampled = ShaderTypeInternal.TextureTypeData.Multisampled.kTrue;
            if (name.Contains("Array"))
                textureData.array = ShaderTypeInternal.TextureTypeData.Array.kTrue;
            if (name.Contains("1D"))
                textureData.dimension = ShaderTypeInternal.TextureTypeData.Dimension.k1D;
            if (name.Contains("3D"))
                textureData.dimension = ShaderTypeInternal.TextureTypeData.Dimension.k3D;
            if (name.Contains("Cube"))
                textureData.dimension = ShaderTypeInternal.TextureTypeData.Dimension.kCubemap;

            return textureData;
        }

        public static ShaderType Texture(ShaderContainer container, string name)
        {
            if ((container != null) && (name != null))
            {
                var initData = new ShaderTypeInternal.TextureInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = FoundryHandle.Invalid(),
                        attributeListHandle = FoundryHandle.Invalid(),
                        includeListHandle = FoundryHandle.Invalid(),
                    },
                    textureData = BuildTextureTypeDataFromName(container, name),
                };
                var handle = ShaderTypeInternal.Texture(container, initData);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType SamplerState(ShaderContainer container, string name)
        {
            if ((container != null) && (name != null))
            {
                var initData = new ShaderTypeInternal.SamplerStateInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = FoundryHandle.Invalid(),
                        attributeListHandle = FoundryHandle.Invalid(),
                        includeListHandle = FoundryHandle.Invalid(),
                    },
                    samplerStateData = new ShaderTypeInternal.SamplerStateTypeData
                    {
                        comparisonMode = ShaderTypeInternal.SamplerStateTypeData.ComparisonMode.kOff,
                    },
                };
                var handle = ShaderTypeInternal.SamplerState(container, initData);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public static ShaderType Array(ShaderContainer container, ShaderType elementType, int elementCount)
        {
            if ((container != null) && elementType.IsValid && elementCount >= 0)
            {
                string name = String.Format("{0}[{1}]", elementType.Name, elementCount);
                var initData = new ShaderTypeInternal.ArrayInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = FoundryHandle.Invalid(),
                        attributeListHandle = FoundryHandle.Invalid(),
                        includeListHandle = FoundryHandle.Invalid(),
                    },
                    arrayData = new ShaderTypeInternal.ArrayTypeData
                    {
                        elementTypeHandle = elementType.handle,
                        length = (UInt32)elementCount,
                    },
                };
                var handle = ShaderTypeInternal.Array(container, initData);
                if (handle.IsValid)
                    return new ShaderType(container, handle);
            }
            return ShaderType.Invalid;
        }

        public class StructBuilder
        {
            ShaderContainer container;
            readonly internal FoundryHandle typeHandle;

            Block.Builder parentBlock;

            string name;
            internal List<StructField> fields;
            internal List<ShaderFunction> functions;
            List<ShaderAttribute> attributes;
            List<IncludeDescriptor> includes;
            bool declaredExternally;
            bool finalized = false;
            public Namespace containingNamespace;
            public Location location;

            // create struct in global namespace
            public StructBuilder(ShaderContainer container, string name) : this(container, name, null) { }

            // create struct in block
            public StructBuilder(Block.Builder blockBuilder, string name) : this(blockBuilder.Container, name, blockBuilder) { }

            internal StructBuilder(ShaderContainer container, string name, Block.Builder blockBuilder)
            {
                this.container = container;
                this.name = name;
                this.typeHandle = container.Create<ShaderTypeInternal>();
                this.parentBlock = blockBuilder;
                this.containingNamespace = blockBuilder?.containingNamespace ?? Namespace.Invalid;
            }

            public void AddField(ShaderType type, string name)
            {
                var fieldBuilder = new StructField.Builder(container, name, type);
                AddField(fieldBuilder.Build());
            }

            public void AddField(StructField field)
            {
                Utilities.AddToList(ref fields, field);
            }

            public void AddFunction(ShaderFunction function)
            {
                Utilities.AddToList(ref functions, function);
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref attributes, attribute);
            }

            public void AddInclude(string path)
            {
                var includeBuilder = new IncludeDescriptor.Builder(container, path);
                Utilities.AddToList(ref includes, includeBuilder.Build());
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

                var initData = new ShaderTypeInternal.StructInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = containingNamespace.handle,
                        attributeListHandle = ListType.Build(container, attributes),
                        includeListHandle = ListType.Build(container, includes),
                        locationHandle = location.handle,
                    },
                    structData = new ShaderTypeInternal.StructTypeData
                    {
                        fieldListHandle = ListType.Build(container, fields),
                        functionListHandle = ListType.Build(container, functions),
                        declaredExternally = declaredExternally,
                    },
                };

                var handle = ShaderTypeInternal.BuildStruct(container, typeHandle, initData);
                var result = new ShaderType(container, handle);
                if (handle.IsValid && parentBlock != null)
                {
                    // register with parent block
                    parentBlock.AddType(result);
                }
                return result;
            }
        }

        internal class ResourceBuilder
        {
            // Common data for type creation
            public ShaderContainer container { get; private set; }
            public string name { get; private set; }
            internal Namespace containingNamespace;
            internal List<ShaderAttribute> attributes;
            internal List<IncludeDescriptor> includes;
            // Data for the struct aspect of a resource
            readonly internal FoundryHandle structTypeHandle;
            internal List<StructField> fields = new List<StructField>();
            internal List<ShaderFunction> functions;
            public Location location;

            public ResourceBuilder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.structTypeHandle = container.Create<ShaderTypeInternal>();
                this.containingNamespace = Namespace.Invalid;
            }

            internal void AddField(ShaderType type, string name)
            {
                var fieldBuilder = new StructField.Builder(container, name, type);
                fields.Add(fieldBuilder.Build());
            }
            public void AddFunction(ShaderFunction function) => Utilities.AddToList(ref functions, function);
            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            public void AddInclude(IncludeDescriptor include) => Utilities.AddToList(ref includes, include);

            protected ShaderTypeInternal.ResourceInitializationData GetInitData()
            {
                return new ShaderTypeInternal.ResourceInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = containingNamespace.handle,
                        attributeListHandle = ListType.Build(container, attributes),
                        includeListHandle = ListType.Build(container, includes),
                        locationHandle = location.handle,
                    },
                    resourceData = new ShaderTypeInternal.ResourceTypeData
                    {
                        structData = new ShaderTypeInternal.StructTypeData
                        {
                            fieldListHandle = ListType.Build(container, fields),
                            functionListHandle = ListType.Build(container, functions),
                            declaredExternally = false,
                        },
                    }
                };
            }
        }

        public class SampledTextureBuilder : ResourceBuilder
        {
            public SampledTextureBuilder(ShaderContainer container, string name,
                ShaderType textureType, ShaderType samplerType)
                : this(container, name, textureType, "tex", samplerType, "s") {}

            public SampledTextureBuilder(ShaderContainer container, string name,
                ShaderType textureType, string textureName, ShaderType samplerType, string samplerName)
                : base(container, name)
            {
                AddField(textureType, textureName);
                AddField(samplerType, samplerName);
            }

            public ShaderType Build()
            {
                var initData = GetInitData();
                initData.resourceData.kind = ShaderTypeInternal.ResourceTypeData.ResourceKind.kSampledTexture;
                initData.resourceData.m_Private0 = fields[0].Type.handle;
                initData.resourceData.m_Private1 = fields[1].Type.handle;
                var handle = ShaderTypeInternal.Resource(container, structTypeHandle, initData);
                var result = new ShaderType(container, handle);
                return result;
            }
        }

        public class TextureBuilder : ResourceBuilder
        {
            public TextureBuilder(ShaderContainer container, string name, ShaderType textureType)
                : this(container, name, textureType, "tex") {}

            public TextureBuilder(ShaderContainer container, string name,
                ShaderType textureType, string textureName)
                : base(container, name)
            {
                AddField(textureType, textureName);
            }

            public ShaderType Build()
            {
                var initData = GetInitData();
                initData.resourceData.kind = ShaderTypeInternal.ResourceTypeData.ResourceKind.kTexture;
                initData.resourceData.m_Private0 = fields[0].Type.handle;
                var handle = ShaderTypeInternal.Resource(container, structTypeHandle, initData);
                var result = new ShaderType(container, handle);
                return result;
            }
        }

        public class SamplerBuilder : ResourceBuilder
        {
            public SamplerBuilder(ShaderContainer container, string name, ShaderType samplerType)
                : this(container, name, samplerType, "s") {}

            public SamplerBuilder(ShaderContainer container, string name,
                ShaderType samplerType, string samplerFieldName)
                : base(container, name)
            {
                AddField(samplerType, samplerFieldName);
            }

            public ShaderType Build()
            {
                var initData = GetInitData();
                initData.resourceData.kind = ShaderTypeInternal.ResourceTypeData.ResourceKind.kSampler;
                initData.resourceData.m_Private0 = fields[0].Type.handle;
                var handle = ShaderTypeInternal.Resource(container, structTypeHandle, initData);
                var result = new ShaderType(container, handle);
                return result;
            }
        }

        public class EnumBuilder
        {
            public ShaderContainer container { get; private set; }
            public string name { get; private set; }
            internal Namespace containingNamespace;
            public Location location;
            internal List<ShaderAttribute> attributes;
            internal List<IncludeDescriptor> includes;
            internal List<EnumLiteral> literalEntries;
            readonly internal FoundryHandle typeHandle;
            bool finalized = false;

            public EnumBuilder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.typeHandle = container.Create<ShaderTypeInternal>();
            }

            public void Add(string name, int value)
            {
                if (string.IsNullOrEmpty(name))
                    throw new InvalidOperationException("Invalid enum name. Names cannot be null or empty.");

                if (literalEntries != null)
                {
                    var index = literalEntries.FindIndex((e) => e.Name == name);
                    if (index != -1)
                        throw new InvalidOperationException($"Enum literal with the name {name} has already been added.");
                }

                Utilities.AddToList(ref literalEntries, CreateLiteral(name, value));
            }

            EnumLiteral CreateLiteral(string name, int value)
            {
                var internalValue = new EnumLiteralInternal()
                {
                    m_NameHandle = container.AddString(name),
                    m_Value = value,
                    m_EnumTypeHandle = typeHandle
                };
                return new EnumLiteral(container, container.Add(internalValue));
            }

            public ShaderType Build()
            {
                if (finalized)
                    return new ShaderType(container, typeHandle);
                finalized = true;
                var initData = new ShaderTypeInternal.EnumInitializationData
                {
                    commonData = new ShaderTypeInternal.CommonTypeData
                    {
                        nameHandle = container.AddString(name),
                        containingNamespaceHandle = containingNamespace.handle,
                        attributeListHandle = ListType.Build(container, attributes),
                        includeListHandle = ListType.Build(container, includes),
                        locationHandle = location.handle,
                    },
                    enumData = new ShaderTypeInternal.EnumTypeData
                    {
                        literalEntries = ListType.Build(container, literalEntries),
                    },
                };

                var handle = ShaderTypeInternal.Enum(container, typeHandle, initData);
                return new ShaderType(container, handle);
            }
        }
    }
}
