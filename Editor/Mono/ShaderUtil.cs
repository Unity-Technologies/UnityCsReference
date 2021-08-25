// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using DebuggerDisplayAttribute = System.Diagnostics.DebuggerDisplayAttribute;

namespace UnityEditor
{
    public class ShaderData
    {
        public class Subshader
        {
            internal ShaderData m_Data;
            internal int m_SubshaderIndex;

            internal Subshader(ShaderData data, int subshaderIndex)
            {
                m_Data = data;
                m_SubshaderIndex = subshaderIndex;
            }

            internal Shader SourceShader { get { return m_Data.SourceShader; } }

            public int PassCount { get { return ShaderUtil.GetShaderTotalPassCount(m_Data.SourceShader, m_SubshaderIndex); } }

            public Pass GetPass(int passIndex)
            {
                if (passIndex < 0 || passIndex >= PassCount)
                {
                    Debug.LogErrorFormat("Pass index is incorrect: {0}, shader {1} has {2} passes.", passIndex, SourceShader, PassCount);
                    return null;
                }

                return new Pass(this, passIndex);
            }
        }

        public class Pass
        {
            Subshader m_Subshader;
            int m_PassIndex;

            internal Pass(Subshader subshader, int passIndex)
            {
                m_Subshader = subshader;
                m_PassIndex = passIndex;
            }

            internal Shader SourceShader { get { return m_Subshader.m_Data.SourceShader; } }
            internal int SubshaderIndex { get { return m_Subshader.m_SubshaderIndex; } }

            public string SourceCode { get { return ShaderUtil.GetShaderPassSourceCode(SourceShader, SubshaderIndex, m_PassIndex); } }
            public string Name { get { return ShaderUtil.GetShaderPassName(SourceShader, SubshaderIndex, m_PassIndex); } }

            public bool HasShaderStage(ShaderType shaderType)
            {
                return ShaderUtil.PassHasShaderStage(SourceShader, SubshaderIndex, m_PassIndex, shaderType);
            }

            internal static GraphicsTier kNoGraphicsTier = (GraphicsTier)(-1);

            public VariantCompileInfo CompileVariant(ShaderType shaderType, string[] keywords,
                ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget)
            {
                var platformKeywords = ShaderUtil.GetShaderPlatformKeywordsForBuildTarget(shaderCompilerPlatform, buildTarget, kNoGraphicsTier);
                return ShaderUtil.CompileShaderVariant(SourceShader, SubshaderIndex, m_PassIndex, shaderType, platformKeywords, keywords, shaderCompilerPlatform, buildTarget, kNoGraphicsTier);
            }

            public VariantCompileInfo CompileVariant(ShaderType shaderType, string[] keywords,
                ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, GraphicsTier tier)
            {
                var platformKeywords = ShaderUtil.GetShaderPlatformKeywordsForBuildTarget(shaderCompilerPlatform, buildTarget, tier);
                return ShaderUtil.CompileShaderVariant(SourceShader, SubshaderIndex, m_PassIndex, shaderType, platformKeywords, keywords, shaderCompilerPlatform, buildTarget, tier);
            }

            public VariantCompileInfo CompileVariant(ShaderType shaderType, string[] keywords,
                ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, BuiltinShaderDefine[] platformKeywords)
            {
                return ShaderUtil.CompileShaderVariant(SourceShader, SubshaderIndex, m_PassIndex, shaderType, platformKeywords, keywords, shaderCompilerPlatform, buildTarget, kNoGraphicsTier);
            }

            public VariantCompileInfo CompileVariant(ShaderType shaderType, string[] keywords,
                ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, BuiltinShaderDefine[] platformKeywords, GraphicsTier tier)
            {
                return ShaderUtil.CompileShaderVariant(SourceShader, SubshaderIndex, m_PassIndex, shaderType, platformKeywords, keywords, shaderCompilerPlatform, buildTarget, tier);
            }
        }

        public int ActiveSubshaderIndex { get { return ShaderUtil.GetShaderActiveSubshaderIndex(SourceShader); } }
        public int SubshaderCount { get { return ShaderUtil.GetShaderSubshaderCount(SourceShader); } }

        public Shader SourceShader { get; private set; }

        public Subshader ActiveSubshader
        {
            get
            {
                var index = ActiveSubshaderIndex;
                if (index < 0 || index >= SubshaderCount)
                    return null;

                return new Subshader(this, index);
            }
        }

        internal ShaderData(Shader sourceShader)
        {
            Assert.IsNotNull(sourceShader);
            this.SourceShader = sourceShader;
        }

        public Subshader GetSubshader(int index)
        {
            if (index < 0 || index >= SubshaderCount)
            {
                Debug.LogErrorFormat("Subshader index is incorrect: {0}, shader {1} has {2} passes.", index, SourceShader, SubshaderCount);
                return null;
            }

            return new Subshader(this, index);
        }

        //
        // Experimental reflection information and raw compiled data access.  Used for Tiny shader export.
        //
        public struct VariantCompileInfo
        {
            public bool Success;
            public ShaderMessage[] Messages;

            public byte[] ShaderData;

            public VertexAttribute[] Attributes;
            public ConstantBufferInfo[] ConstantBuffers;
            public TextureBindingInfo[] TextureBindings;
        }

        [DebuggerDisplay("cbuffer {Name} ({Size} bytes)")]
        public struct ConstantBufferInfo
        {
            public string Name;
            public int Size;
            public ConstantInfo[] Fields;
        }

        [DebuggerDisplay("{ConstantType} {Name} ({DataType} {Columns}x{Rows})")]
        public struct ConstantInfo
        {
            public string Name;
            public int Index;
            public ShaderConstantType ConstantType;
            public ShaderParamType DataType;
            public int Rows;
            public int Columns;
            public int ArraySize;

            // only relevant if ConstantType == Struct
            public int StructSize;
            public ConstantInfo[] StructFields;
        }

        [DebuggerDisplay("{Dim} {Name}")]
        public struct TextureBindingInfo
        {
            public string Name;
            public int Index;
            public int SamplerIndex;
            public bool Multisampled;
            public int ArraySize;
            public TextureDimension Dim;
        }
    }

    public partial class ShaderUtil
    {
        public static ShaderData GetShaderData(Shader shader)
        {
            return new ShaderData(shader);
        }

        // GetShaderMessageCount includes warnings, this function filters them out
        public static bool ShaderHasError(Shader shader)
        {
            FetchCachedMessages(shader);
            var errors = GetShaderMessages(shader);
            return errors.Any(x => x.severity == ShaderCompilerMessageSeverity.Error);
        }

        internal static extern bool PassHasShaderStage(Shader s, int subshaderIndex, int passIndex, ShaderType shaderType);

        internal static bool MaterialsUseInstancingShader(SerializedProperty materialsArray)
        {
            if (materialsArray.hasMultipleDifferentValues)
                return false;
            for (int i = 0; i < materialsArray.arraySize; ++i)
            {
                var material = materialsArray.GetArrayElementAtIndex(i).objectReferenceValue as Material;
                if (material != null && material.enableInstancing && material.shader != null && HasInstancing(material.shader))
                    return true;
            }
            return false;
        }
    }
}
