// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NativeDSPParameter
    {
        internal float m_Value;
        internal float* m_ValueBuffer;
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ParameterRangeAttribute : Attribute
    {
        internal float m_Min;
        internal float m_Max;

        public ParameterRangeAttribute(float min, float max)
        {
            m_Min = min;
            m_Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class ParameterDefaultAttribute : Attribute
    {
        internal float defaultValue;

        public ParameterDefaultAttribute(float defaultVal)
        {
            defaultValue = defaultVal;
        }
    }

    internal unsafe struct ParameterData<P> where P : struct, IConvertible
    {
        public float GetFloat(P parameter, uint sampleOffset)
        {
            return GetFloat(UnsafeUtility.EnumToInt<P>(parameter), sampleOffset);
        }

        public float GetFloat(int index, uint sampleOffset)
        {
            if (index >= m_ParametersCount)
                throw new ArgumentException("Undefined parameter in ParameterData.GetValue");

            if (m_Parameters[index].m_ValueBuffer != null)
            {
                if (sampleOffset >= m_ReadLength)
                    throw new ArgumentException("sampleOffset greater that the read length of the frame in ParameterData.GetValue");

                return m_Parameters[index].m_ValueBuffer[sampleOffset];
            }

            return m_Parameters[index].m_Value;
        }

        internal NativeDSPParameter* m_Parameters;
        internal uint m_ParametersCount;
        internal uint m_ReadLength;
    }
}

