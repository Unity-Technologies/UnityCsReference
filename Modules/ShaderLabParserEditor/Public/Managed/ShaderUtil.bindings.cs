// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderApiReflection;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/ShaderLabParserEditor/Public/Managed/ShaderUtil.bindings.h")]
    public partial class ShaderUtil
    {
        [NativeClass("ShaderLabParserEditor::ShaderUtilExtensions::MarshalledShaderInterfaceObjects")]
        [NativeHeader("Modules/ShaderLabParserEditor/Public/Managed/ShaderUtil.bindings.h")]
        internal struct MarshalledShaderInterfaceObjects
        {
            public Shader m_Shader;
            public ShaderInterfaceReflection m_Reflection;
            public TextAsset m_SplicedText;
        }

        [FreeFunction("ShaderLabParserEditor::ShaderUtilExtensions::CreateShaderInterfaceAsset")]
        extern static MarshalledShaderInterfaceObjects CreateShaderInterfaceAsset(
            AssetImportContext context,
            string source,
            bool compileInitialShaderVariants);
    }
}
