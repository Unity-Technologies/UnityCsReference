// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    // match layout of DrawSettings on C++ side
    public unsafe struct DrawingSettings : IEquatable<DrawingSettings>
    {
        const int kMaxShaderPasses = 16;

        // externals bind to this to avoid recompile
        // as precompiled assemblies inline the const
        public static readonly int maxShaderPasses = kMaxShaderPasses;

        SortingSettings m_SortingSettings;

        // can't make fixed types private, because then the compiler generates different code which BindinsgGenerator does not handle yet.
        internal fixed int shaderPassNames[kMaxShaderPasses];

        PerObjectData m_PerObjectData;

        DrawRendererFlags m_Flags;

#pragma warning disable 414
        int m_OverrideShaderID;
        int m_OverrideShaderPassIndex;
        int m_OverrideMaterialInstanceId;
        int m_OverrideMaterialPassIndex;
        int m_fallbackMaterialInstanceId;
        int m_MainLightIndex;
        int m_UseSrpBatcher; // only needed to match native struct
#pragma warning restore 414

        public DrawingSettings(ShaderTagId shaderPassName, SortingSettings sortingSettings)
        {
            m_SortingSettings = sortingSettings;
            m_PerObjectData = PerObjectData.None;
            m_Flags = DrawRendererFlags.EnableInstancing;

            m_OverrideShaderID = 0;
            m_OverrideShaderPassIndex = 0;
            m_OverrideMaterialInstanceId = 0;
            m_OverrideMaterialPassIndex = 0;
            m_fallbackMaterialInstanceId = 0;
            m_MainLightIndex = -1;

            fixed(int* p = shaderPassNames)
            {
                p[0] = shaderPassName.id;
                for (int i = 1; i < maxShaderPasses; i++)
                {
                    p[i] = -1;
                }
            }

            m_UseSrpBatcher = 0;
        }

        public SortingSettings sortingSettings
        {
            get { return m_SortingSettings; }
            set { m_SortingSettings = value; }
        }

        public PerObjectData perObjectData
        {
            get { return m_PerObjectData; }
            set { m_PerObjectData = value; }
        }

        public bool enableDynamicBatching
        {
            get { return (m_Flags & DrawRendererFlags.EnableDynamicBatching) != 0; }
            set
            {
                if (value)
                    m_Flags |= DrawRendererFlags.EnableDynamicBatching;
                else
                    m_Flags &= ~DrawRendererFlags.EnableDynamicBatching;
            }
        }

        public bool enableInstancing
        {
            get { return (m_Flags & DrawRendererFlags.EnableInstancing) != 0; }
            set
            {
                if (value)
                    m_Flags |= DrawRendererFlags.EnableInstancing;
                else
                    m_Flags &= ~DrawRendererFlags.EnableInstancing;
            }
        }

        public Material overrideMaterial
        {
            get { return m_OverrideMaterialInstanceId != 0 ? Object.FindObjectFromInstanceID(m_OverrideMaterialInstanceId) as Material : null; }
            set { m_OverrideMaterialInstanceId = value?.GetInstanceID() ?? 0; }
        }

        public Shader overrideShader
        {
            get { return m_OverrideShaderID != 0 ? Object.FindObjectFromInstanceID(m_OverrideShaderID) as Shader : null; }
            set { m_OverrideShaderID = value?.GetInstanceID() ?? 0; }
        }

        public int overrideMaterialPassIndex
        {
            get { return m_OverrideMaterialPassIndex; }
            set { m_OverrideMaterialPassIndex = value; }
        }

        public int overrideShaderPassIndex
        {
            get { return m_OverrideShaderPassIndex; }
            set { m_OverrideShaderPassIndex = value; }
        }

        public Material fallbackMaterial
        {
            get { return m_fallbackMaterialInstanceId != 0 ? Object.FindObjectFromInstanceID(m_fallbackMaterialInstanceId) as Material : null; }
            set { m_fallbackMaterialInstanceId = value?.GetInstanceID() ?? 0; }
        }

        public int mainLightIndex
        {
            get { return m_MainLightIndex; }
            set { m_MainLightIndex = value; }
        }

        public ShaderTagId GetShaderPassName(int index)
        {
            if (index >= maxShaderPasses || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index should range from 0 to DrawSettings.maxShaderPasses ({maxShaderPasses}), was {index}");

            fixed(int* p = shaderPassNames)
            {
                return new ShaderTagId { id = p[index] };
            }
        }

        public void SetShaderPassName(int index, ShaderTagId shaderPassName)
        {
            if (index >= maxShaderPasses || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index should range from 0 to DrawSettings.maxShaderPasses ({maxShaderPasses}), was {index}");

            fixed(int* p = shaderPassNames)
            {
                p[index] = shaderPassName.id;
            }
        }

        public bool Equals(DrawingSettings other)
        {
            for (var i = 0; i < maxShaderPasses; i++)
            {
                if (!GetShaderPassName(i).Equals(other.GetShaderPassName(i)))
                    return false;
            }

            return m_SortingSettings.Equals(other.m_SortingSettings)
                && m_PerObjectData == other.m_PerObjectData
                && m_Flags == other.m_Flags
                && m_OverrideMaterialInstanceId == other.m_OverrideMaterialInstanceId
                && m_OverrideMaterialPassIndex == other.m_OverrideMaterialPassIndex
                && m_fallbackMaterialInstanceId == other.m_fallbackMaterialInstanceId
                && m_UseSrpBatcher == other.m_UseSrpBatcher;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DrawingSettings && Equals((DrawingSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_SortingSettings.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_PerObjectData;
                hashCode = (hashCode * 397) ^ (int)m_Flags;
                hashCode = (hashCode * 397) ^ m_OverrideMaterialInstanceId;
                hashCode = (hashCode * 397) ^ m_OverrideMaterialPassIndex;
                hashCode = (hashCode * 397) ^ m_fallbackMaterialInstanceId;
                hashCode = (hashCode * 397) ^ m_UseSrpBatcher;
                return hashCode;
            }
        }

        public static bool operator==(DrawingSettings left, DrawingSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(DrawingSettings left, DrawingSettings right)
        {
            return !left.Equals(right);
        }
    }
}
