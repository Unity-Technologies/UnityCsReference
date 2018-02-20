// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Assertions;

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
    }

    public partial class ShaderUtil
    {
        public static ShaderData GetShaderData(Shader shader)
        {
            return new ShaderData(shader);
        }

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
