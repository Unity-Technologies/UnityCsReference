// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Animations
{
    internal struct PoseStore
    {
        public float Value;
    }

    internal struct PoseMaskStore
    {
        public float Value;
    }

    internal struct CachedPoseInfo
    {
        public ulong PhaseTagHash;
        public int Priority;
        public int PoseIndex;
    }

    internal struct CachedPoseWeight
    {
        public float Value;
    }

    internal struct CachedPoseHandle
    {
        internal readonly int m_PoseIndex;
        public CachedPoseHandle(int poseIndex)
        {
            m_PoseIndex = poseIndex;
        }
    }
}
