// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    internal enum DataType : ushort
    {
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN DataType.h
        Unknown = 0,

        // Special types
        Array = 1,
        String,
        Linker,

        StartStaticSized,
        // Static sized types
        ShaderAttributeParam = StartStaticSized,
        ShaderAttribute,
        CommandDescriptor,
        DefineDescriptor,
        IncludeDescriptor,
        KeywordDescriptor,
        PragmaDescriptor,
        TagDescriptor,
        FunctionParameter,
        ShaderFunction,
        StructField,
        ShaderType,
        BlockVariable,
        Block,
        PassIdentifier,
        CustomizationPoint,
        TemplatePass,
        Template,
        TemplateInstance,
        ShaderDependency,
        ShaderCustomEditor,
        PackageRequirement,
        CopyRule,
        LinkOverride,
        LinkAccessor,
        LinkElement,
        BlockSequenceElement,
        CustomizationPointImplementation,
        StageDescription,
        // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN DataType.h -- ALSO ADD THE TYPE MAPPING TO Initialize() BELOW
    };
}
