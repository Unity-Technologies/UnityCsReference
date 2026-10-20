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
        public readonly struct SystemData : IEquatable<SystemData>
        {
            readonly SystemHandle m_System;
            readonly FixedString128Bytes m_Name;

            public SystemHandle System => m_System;

            [ExcludeFromBurstCompatTesting("Returns managed string")]
            public string Name => m_Name.ToString();

            public SystemData(SystemTypeIndex systemType, in SystemHandle systemHandle)
            {
                m_System = systemHandle;
                m_Name = default;
                m_Name.Append(TypeManager.GetSystemName(systemType));
            }

            public bool Equals(SystemData other)
            {
                return m_System.Equals(other.m_System);
            }

            [ExcludeFromBurstCompatTesting("Takes managed object")]
            public override bool Equals(object obj)
            {
                return obj is SystemData systemData ? Equals(systemData) : false;
            }

            public override int GetHashCode()
            {
                return m_System.GetHashCode();
            }

            public static bool operator ==(SystemData lhs, SystemData rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(SystemData lhs, SystemData rhs)
            {
                return !lhs.Equals(rhs);
            }
        }
    }
}
