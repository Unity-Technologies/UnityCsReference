// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.ShaderFoundry
{
    // This class stores static data relating IInternalType and IPublicType objects.
    // This is used to handle generics in order to interface with the native container.
    class DataTypeStatic
    {
        internal interface IDataTypeInfo
        {
            public DataType dataType { get; }
            public Type publicType { get; }
            public Type internalType { get; }
            public int internalTypeSizeInBytes { get; }

            public IPublicType ConstructFromHandle(ShaderContainer container, FoundryHandle handle);
        }

        internal readonly struct DataTypeInfoInvalid : IDataTypeInfo
        {
            DataType IDataTypeInfo.dataType => DataType.Unknown;

            Type IDataTypeInfo.publicType => null;

            Type IDataTypeInfo.internalType => null;

            int IDataTypeInfo.internalTypeSizeInBytes => 0;
            public IPublicType ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => null;
        }

        internal readonly struct DataTypeInfo<T> : IDataTypeInfo where T : struct, IPublicType<T>
        {

            public readonly DataType dataType;
            public readonly Type publicType;
            public readonly Type internalType;
            public readonly int internalTypeSizeInBytes;

            DataType IDataTypeInfo.dataType => dataType;

            Type IDataTypeInfo.publicType => publicType;

            Type IDataTypeInfo.internalType => internalType;

            int IDataTypeInfo.internalTypeSizeInBytes => internalTypeSizeInBytes;

            public DataTypeInfo(DataType dataType, Type internalType, Type publicType)
                : this(dataType, internalType, publicType, System.Runtime.InteropServices.Marshal.SizeOf(internalType))
            {
            }

            private DataTypeInfo(DataType dataType, Type internalType, Type publicType, int internalTypeSizeInBytes)
            {
                this.dataType = dataType;
                this.publicType = publicType;
                this.internalType = internalType;
                this.internalTypeSizeInBytes = internalTypeSizeInBytes;
            }

            public IPublicType ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            {
                return PublicTypeStatic<T>.ConstructFromHandle(container, handle);
            }
        }

        IDataTypeInfo Invalid = new DataTypeInfoInvalid();
        Dictionary<Type, IDataTypeInfo> internalTypeMap = new Dictionary<Type, IDataTypeInfo>();
        Dictionary<Type, IDataTypeInfo> publicTypeMap = new Dictionary<Type, IDataTypeInfo>();
        Dictionary<DataType, IDataTypeInfo> dataTypeToInfo = new Dictionary<DataType, IDataTypeInfo>();
        [NoAutoStaticsCleanup] // this is constructed from internal types only
        static DataTypeStatic m_Instance = null;

        static DataTypeStatic Instance
        {
            get
            {
                if (m_Instance == null)
                    Initialize();
                return m_Instance;
            }
        }

        static void Initialize()
        {
            // The type relations must be explicitly recorded.
            m_Instance = new DataTypeStatic();
            m_Instance.Register<ShaderAttributeParameterInternal, ShaderAttributeParameter>(DataType.ShaderAttributeParameter);
            m_Instance.Register<ShaderAttributeInternal, ShaderAttribute>(DataType.ShaderAttribute);
            m_Instance.Register<RenderStateDescriptorInternal, RenderStateDescriptor>(DataType.RenderStateDescriptor);
            m_Instance.Register<RenderStateNamedValueInternal, RenderStateNamedValue>(DataType.RenderStateNamedValue);
            m_Instance.Register<RenderStatePropertyInternal, RenderStateProperty>(DataType.RenderStateProperty);
            m_Instance.Register<RenderStateTargetSpecifierInternal, RenderStateTargetSpecifier>(DataType.RenderStateTargetSpecifier);
            m_Instance.Register<DefineDescriptorInternal, DefineDescriptor>(DataType.DefineDescriptor);
            m_Instance.Register<IncludeDescriptorInternal, IncludeDescriptor>(DataType.IncludeDescriptor);
            m_Instance.Register<KeywordDescriptorInternal, KeywordDescriptor>(DataType.KeywordDescriptor);
            m_Instance.Register<PragmaDescriptorInternal, PragmaDescriptor>(DataType.PragmaDescriptor);
            m_Instance.Register<TagDescriptorInternal, TagDescriptor>(DataType.TagDescriptor);
            m_Instance.Register<FunctionParameterInternal, FunctionParameter>(DataType.FunctionParameter);
            m_Instance.Register<ShaderFunctionInternal, ShaderFunction>(DataType.ShaderFunction);
            m_Instance.Register<StructFieldInternal, StructField>(DataType.StructField);
            m_Instance.Register<ShaderTypeInternal, ShaderType>(DataType.ShaderType);
            m_Instance.Register<BlockInternal, Block>(DataType.Block);
            m_Instance.Register<CustomAttributeDefinitionInternal, CustomAttributeDefinition>(DataType.CustomAttributeDefinition);
            m_Instance.Register<ConstructorSignatureParameterInternal, ConstructorSignatureParameter>(DataType.ConstructorSignatureParameter);
            m_Instance.Register<ConstructorSignatureInternal, ConstructorSignature>(DataType.ConstructorSignature);
            m_Instance.Register<CustomizationPointInternal, CustomizationPoint>(DataType.CustomizationPoint);
            m_Instance.Register<TemplatePassInternal, TemplatePass>(DataType.TemplatePass);
            m_Instance.Register<TemplateInternal, Template>(DataType.Template);
            m_Instance.Register<ShaderDependencyInternal, ShaderDependency>(DataType.ShaderDependency);
            m_Instance.Register<ShaderCustomEditorInternal, ShaderCustomEditor>(DataType.ShaderCustomEditor);
            m_Instance.Register<PackageRequirementInternal, PackageRequirement>(DataType.PackageRequirement);
            m_Instance.Register<CopyRuleInternal, CopyRule>(DataType.CopyRule);
            m_Instance.Register<BlockLinkOverrideInternal, BlockLinkOverride>(DataType.LinkOverride);
            m_Instance.Register<BlockLinkOverrideInternal.LinkAccessorInternal, BlockLinkOverride.LinkAccessor>(DataType.LinkAccessor);
            m_Instance.Register<BlockLinkOverrideInternal.LinkElementInternal, BlockLinkOverride.LinkElement>(DataType.LinkElement);
            m_Instance.Register<BlockSequenceInternal, BlockSequence>(DataType.BlockSequence);
            m_Instance.Register<BlockSequenceElementInternal, BlockSequenceElement>(DataType.BlockSequenceElement);
            m_Instance.Register<CustomizationPointImplementationInternal, CustomizationPointImplementation>(DataType.CustomizationPointImplementation);
            m_Instance.Register<StageDescriptionInternal, StageDescription>(DataType.StageDescription);
            m_Instance.Register<BlockShaderInterfaceInternal, BlockShaderInterface>(DataType.BlockShaderInterface);
            m_Instance.Register<BlockShaderInternal, BlockShader>(DataType.BlockShader);
            m_Instance.Register<RegisterTemplatesWithInterfaceInternal, RegisterTemplatesWithInterface>(DataType.RegisterTemplatesWithInterface);
            m_Instance.Register<InterfaceRegistrationStatementInternal, InterfaceRegistrationStatement>(DataType.InterfaceRegistrationStatement);
            m_Instance.Register<NamespaceInternal, Namespace>(DataType.Namespace);
            m_Instance.Register<BooleanLiteralInternal, BooleanLiteral>(DataType.Boolean);
            m_Instance.Register<FloatLiteralInternal, FloatLiteral>(DataType.Float);
            m_Instance.Register<IntegerLiteralInternal, IntegerLiteral>(DataType.Integer);
            m_Instance.Register<EnumLiteralInternal, EnumLiteral>(DataType.EnumLiteral);
            m_Instance.Register<StringLiteralInternal, StringLiteral>(DataType.String);
            m_Instance.Register<EmptyStringInternal, StringLiteral>(DataType.EmptyString);
            m_Instance.Register<LocationInternal, Location>(DataType.Location);
            m_Instance.Register<ListTypeInternal, ListType>(DataType.Array);
        }

        void Register<InternalType, PublicType>(DataType dataType)
            where InternalType : struct, IInternalType<InternalType>
            where PublicType : struct, IPublicType<PublicType>
        {
            var iType = typeof(InternalType);
            var pType = typeof(PublicType);
            var info = new DataTypeInfo<PublicType>(dataType, iType, pType);
            Register(info);
        }

        void Register(IDataTypeInfo info)
        {
            internalTypeMap[info.internalType] = info;
            publicTypeMap[info.publicType] = info;
            dataTypeToInfo[info.dataType] = info;
        }

        static internal IDataTypeInfo GetInfoFromDataType(DataType type)
        {
            if (!Instance.dataTypeToInfo.TryGetValue(type, out var info))
                return Instance.Invalid;
            return info;
        }

        static internal IDataTypeInfo GetInfoFromInternalType<T>() where T : struct, IInternalType<T>
        {
            if (!Instance.internalTypeMap.TryGetValue(typeof(T), out var info))
                return Instance.Invalid;
            return info;
        }

        static internal DataType GetDataTypeFromInternalType<T>() where T : struct, IInternalType<T>
        {
            if (!Instance.internalTypeMap.TryGetValue(typeof(T), out var info))
                return DataType.Unknown;
            return info.dataType;
        }

        static internal IDataTypeInfo GetInfoFromPublicType<T>() where T : struct, IPublicType<T>
        {
            if (!Instance.publicTypeMap.TryGetValue(typeof(T), out var info))
                return Instance.Invalid;
            return info;
        }

        static internal DataType GetDataTypeFromPublicType<T>() where T : struct, IPublicType<T>
        {
            if (!Instance.publicTypeMap.TryGetValue(typeof(T), out var info))
                return DataType.Unknown;
            return info.dataType;
        }
    }
}
