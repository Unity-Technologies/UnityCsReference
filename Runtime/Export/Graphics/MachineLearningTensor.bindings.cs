// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningTensor.h")]
    [NativeHeader("Runtime/Export/Graphics/MachineLearning.bindings.h")]
    public enum MachineLearningDataType
    {
        Unknown,
        Float32,
        Float16,
        UInt32,
        UInt16,
        UInt8,
        Int32,
        Int16,
        Int8,
        Float64,
        UInt64,
        Int64
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct MachineLearningTensorShape : IEquatable<MachineLearningTensorShape>
    {
        public override int GetHashCode()
        {
            return (rank, D0, D1, D2, D3, D4, D5, D6, D7).GetHashCode();
        }

        public bool Equals(MachineLearningTensorShape other)
        {
            // optimal order for early out
            return rank == other.rank
                   && D0 == other.D0
                   && D1 == other.D1
                   && D2 == other.D2
                   && D3 == other.D3
                   && D4 == other.D4
                   && D5 == other.D5
                   && D6 == other.D6
                   && D7 == other.D7;
        }

        public override bool Equals(object obj)
        {
            return obj is MachineLearningTensorShape other && Equals(other);
        }

        public static bool operator ==(MachineLearningTensorShape lhs, MachineLearningTensorShape rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MachineLearningTensorShape lhs, MachineLearningTensorShape rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct MachineLearningTensorDescriptor :  IEquatable<MachineLearningTensorDescriptor>
    {
        public override int GetHashCode()
        {
            return (dataType, shape).GetHashCode();
        }

        public bool Equals(MachineLearningTensorDescriptor other)
        {
            return dataType == other.dataType && shape.Equals(other.shape);
        }

        public override bool Equals(object obj)
        {
            return obj is MachineLearningTensorDescriptor other && Equals(other);
        }

        public static bool operator ==(MachineLearningTensorDescriptor lhs, MachineLearningTensorDescriptor rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MachineLearningTensorDescriptor lhs, MachineLearningTensorDescriptor rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

}
