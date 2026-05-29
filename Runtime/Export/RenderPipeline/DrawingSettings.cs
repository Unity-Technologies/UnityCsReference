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
        EntityId m_OverrideShaderID;
        int m_OverrideShaderPassIndex;
        EntityId m_OverrideMaterialEntityId;
        int m_OverrideMaterialPassIndex;
        EntityId m_fallbackMaterialEntityId;
        int m_MainLightIndex;
        int m_UseSrpBatcher; // only needed to match native struct
        int m_LodCrossFadeStencilMask;
#pragma warning restore 414

        public DrawingSettings(ShaderTagId shaderPassName, SortingSettings sortingSettings)
        {
            m_SortingSettings = sortingSettings;
            m_PerObjectData = PerObjectData.None;
            m_Flags = DrawRendererFlags.EnableInstancing;

            m_OverrideShaderID = EntityId.None;
            m_OverrideShaderPassIndex = 0;
            m_OverrideMaterialEntityId = EntityId.None;
            m_OverrideMaterialPassIndex = 0;
            m_fallbackMaterialEntityId = EntityId.None;
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
            m_LodCrossFadeStencilMask = 0;
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

        [System.Obsolete("enableDynamicBatching is obsolete.", true)]
        public bool enableDynamicBatching
        {
            get => false;
            set { }
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
            get { return m_OverrideMaterialEntityId != EntityId.None ? Object.FindObjectFromInstanceID(m_OverrideMaterialEntityId) as Material : null; }
            set { m_OverrideMaterialEntityId = value?.GetEntityId() ?? EntityId.None; }
        }

        public Shader overrideShader
        {
            get { return m_OverrideShaderID != EntityId.None ? Object.FindObjectFromInstanceID(m_OverrideShaderID) as Shader : null; }
            set { m_OverrideShaderID = value?.GetEntityId() ?? EntityId.None; }
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
            get { return m_fallbackMaterialEntityId != EntityId.None ? Object.FindObjectFromInstanceID(m_fallbackMaterialEntityId) as Material : null; }
            set { m_fallbackMaterialEntityId = value?.GetEntityId() ?? EntityId.None; }
        }

        public int mainLightIndex
        {
            get { return m_MainLightIndex; }
            set { m_MainLightIndex = value; }
        }

        public int lodCrossFadeStencilMask
        {
            get { return m_LodCrossFadeStencilMask; }
            set { m_LodCrossFadeStencilMask = value; }
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
                && m_OverrideMaterialEntityId == other.m_OverrideMaterialEntityId
                && m_OverrideMaterialPassIndex == other.m_OverrideMaterialPassIndex
                && m_fallbackMaterialEntityId == other.m_fallbackMaterialEntityId
                && m_UseSrpBatcher == other.m_UseSrpBatcher
                && m_LodCrossFadeStencilMask == other.m_LodCrossFadeStencilMask;
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
                hashCode = (hashCode * 397) ^ m_OverrideMaterialEntityId.GetHashCode();
                hashCode = (hashCode * 397) ^ m_OverrideMaterialPassIndex;
                hashCode = (hashCode * 397) ^ m_fallbackMaterialEntityId.GetHashCode();
                hashCode = (hashCode * 397) ^ m_UseSrpBatcher;
                hashCode = (hashCode * 397) ^ m_LodCrossFadeStencilMask;
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
