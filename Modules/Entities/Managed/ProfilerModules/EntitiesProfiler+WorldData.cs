// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Unity.Entities
{
    partial class EntitiesProfiler
    {
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct WorldData : IEquatable<WorldData>
        {
            readonly ulong m_SequenceNumber;
            readonly FixedString128Bytes m_Name;

            public ulong SequenceNumber => m_SequenceNumber;

            [ExcludeFromBurstCompatTesting("Returns managed string")]
            public string Name => m_Name.ToString();

            [ExcludeFromBurstCompatTesting("Takes managed World")]
            public WorldData(World world)
            {
                m_SequenceNumber = world.SequenceNumber;
                m_Name = world.Name.ToFixedString128();
            }

            public bool Equals(WorldData other)
            {
                return m_SequenceNumber == other.m_SequenceNumber;
            }

            [ExcludeFromBurstCompatTesting("Takes managed object")]
            public override bool Equals(object obj)
            {
                return obj is WorldData worldData ? Equals(worldData) : false;
            }

            public override int GetHashCode()
            {
                return (int)m_SequenceNumber;
            }

            public static bool operator ==(WorldData lhs, WorldData rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(WorldData lhs, WorldData rhs)
            {
                return !lhs.Equals(rhs);
            }
        }
    }
}
