// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ShadowDrawingSettings : IEquatable<ShadowDrawingSettings>
    {
        CullingResults m_CullingResults;
        int m_LightIndex;
        int m_UseRenderingLayerMaskTest;
        ShadowSplitData m_SplitData;
        ShadowObjectsFilter m_ObjectsFilter;

        public CullingResults cullingResults
        {
            get { return m_CullingResults; }
            set
            {
                m_CullingResults.Validate();
                m_CullingResults = value;
            }
        }

        public int lightIndex
        {
            get { return m_LightIndex; }
            set { m_LightIndex = value; }
        }

        public bool useRenderingLayerMaskTest
        {
            get { return m_UseRenderingLayerMaskTest != 0; }
            set { m_UseRenderingLayerMaskTest = value ? 1 : 0; }
        }

        public ShadowSplitData splitData
        {
            get { return m_SplitData; }
            set { m_SplitData = value; }
        }

        public ShadowObjectsFilter objectsFilter
        {
            get { return m_ObjectsFilter; }
            set { m_ObjectsFilter = value; }
        }

        public ShadowDrawingSettings(CullingResults cullingResults, int lightIndex)
        {
            m_CullingResults = cullingResults;
            m_LightIndex = lightIndex;
            m_UseRenderingLayerMaskTest = 0;
            m_SplitData = default(ShadowSplitData);
            m_SplitData.shadowCascadeBlendCullingFactor = 1f;
            m_ObjectsFilter = ShadowObjectsFilter.AllObjects;
        }

        public bool Equals(ShadowDrawingSettings other)
        {
            return m_CullingResults.Equals(other.m_CullingResults) && m_LightIndex == other.m_LightIndex && m_SplitData.Equals(other.m_SplitData) && m_UseRenderingLayerMaskTest.Equals(other.m_UseRenderingLayerMaskTest) && m_ObjectsFilter.Equals(other.m_ObjectsFilter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ShadowDrawingSettings && Equals((ShadowDrawingSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_CullingResults.GetHashCode();
                hashCode = (hashCode * 397) ^ m_LightIndex;
                hashCode = (hashCode * 397) ^ m_UseRenderingLayerMaskTest;
                hashCode = (hashCode * 397) ^ m_SplitData.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_ObjectsFilter;
                return hashCode;
            }
        }

        public static bool operator==(ShadowDrawingSettings left, ShadowDrawingSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ShadowDrawingSettings left, ShadowDrawingSettings right)
        {
            return !left.Equals(right);
        }
    }
}
