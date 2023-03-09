// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    // This class stores static data relating IInternalType and IPublicType objects.
    // This is used to handle generics in order to interface with the native container.
    class DataTypeStatic
    {
        internal readonly struct DataTypeInfo
        {
            public readonly DataType dataType;
            public readonly Type publicType;
            public readonly Type internalType;
            public readonly int internalTypeSizeInBytes;

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

            public static DataTypeInfo Invalid => new DataTypeInfo(DataType.Unknown, null, null, 0);
        }

        DataTypeInfo Invalid = DataTypeInfo.Invalid;
        Dictionary<Type, DataTypeInfo> internalTypeMap = new Dictionary<Type, DataTypeInfo>();
        Dictionary<Type, DataTypeInfo> publicTypeMap = new Dictionary<Type, DataTypeInfo>();
        Dictionary<DataType, DataTypeInfo> dataTypeToInfo = new Dictionary<DataType, DataTypeInfo>();
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
            m_Instance.Register<DefineDescriptorInternal, DefineDescriptor>(DataType.DefineDescriptor);
            m_Instance.Register<IncludeDescriptorInternal, IncludeDescriptor>(DataType.IncludeDescriptor);
            m_Instance.Register<KeywordDescriptorInternal, KeywordDescriptor>(DataType.KeywordDescriptor);
            m_Instance.Register<PragmaDescriptorInternal, PragmaDescriptor>(DataType.PragmaDescriptor);
            m_Instance.Register<TagDescriptorInternal, TagDescriptor>(DataType.TagDescriptor);
            m_Instance.Register<FunctionParameterInternal, FunctionParameter>(DataType.FunctionParameter);
            m_Instance.Register<ShaderFunctionInternal, ShaderFunction>(DataType.ShaderFunction);
            m_Instance.Register<StructFieldInternal, StructField>(DataType.StructField);
            m_Instance.Register<ShaderTypeInternal, ShaderType>(DataType.ShaderType);
            m_Instance.Register<BlockVariableInternal, BlockVariable>(DataType.BlockVariable);
            m_Instance.Register<BlockInternal, Block>(DataType.Block);
            m_Instance.Register<CustomizationPointInternal, CustomizationPoint>(DataType.CustomizationPoint);
            m_Instance.Register<TemplatePassInternal, TemplatePass>(DataType.TemplatePass);
            m_Instance.Register<TemplateInternal, Template>(DataType.Template);
            m_Instance.Register<TemplateInstanceInternal, TemplateInstance>(DataType.TemplateInstance);
            m_Instance.Register<ShaderDependencyInternal, ShaderDependency>(DataType.ShaderDependency);
            m_Instance.Register<ShaderCustomEditorInternal, ShaderCustomEditor>(DataType.ShaderCustomEditor);
            m_Instance.Register<PackageRequirementInternal, PackageRequirement>(DataType.PackageRequirement);
            m_Instance.Register<CopyRuleInternal, CopyRule>(DataType.CopyRule);
            m_Instance.Register<BlockLinkOverrideInternal, BlockLinkOverride>(DataType.LinkOverride);
            m_Instance.Register<BlockLinkOverrideInternal.LinkAccessorInternal, BlockLinkOverride.LinkAccessor>(DataType.LinkAccessor);
            m_Instance.Register<BlockLinkOverrideInternal.LinkElementInternal, BlockLinkOverride.LinkElement>(DataType.LinkElement);
            m_Instance.Register<BlockSequenceElementInternal, BlockSequenceElement>(DataType.BlockSequenceElement);
            m_Instance.Register<CustomizationPointImplementationInternal, CustomizationPointImplementation>(DataType.CustomizationPointImplementation);
            m_Instance.Register<StageDescriptionInternal, StageDescription>(DataType.StageDescription);
            m_Instance.Register<BlockShaderInterfaceInternal, BlockShaderInterface>(DataType.BlockShaderInterface);
            m_Instance.Register<BlockShaderInternal, BlockShader>(DataType.BlockShader);
            m_Instance.Register<RegisterTemplatesWithInterfaceInternal, RegisterTemplatesWithInterface>(DataType.RegisterTemplatesWithInterface);
            m_Instance.Register<InterfaceRegistrationStatementInternal, InterfaceRegistrationStatement>(DataType.InterfaceRegistrationStatement);
        }

        void Register<InternalType, PublicType>(DataType dataType)
            where InternalType : struct, IInternalType<InternalType>
            where PublicType : struct, IPublicType<PublicType>
        {
            var iType = typeof(InternalType);
            var pType = typeof(PublicType);
            var info = new DataTypeInfo(dataType, iType, pType);
            m_Instance.internalTypeMap[iType] = info;
            m_Instance.publicTypeMap[pType] = info;
            m_Instance.dataTypeToInfo[dataType] = info;
        }

        static internal DataTypeInfo GetInfoFromDataType(DataType type)
        {
            if (!Instance.dataTypeToInfo.TryGetValue(type, out var info))
                return Instance.Invalid;
            return info;
        }

        static internal DataTypeInfo GetInfoFromInternalType<T>() where T : struct, IInternalType<T>
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

        static internal DataTypeInfo GetInfoFromPublicType<T>() where T : struct, IPublicType<T>
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
