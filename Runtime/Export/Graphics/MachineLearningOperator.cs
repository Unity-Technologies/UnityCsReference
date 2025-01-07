// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public partial struct MachineLearningOperator
    {
    #pragma warning disable 414
        internal IntPtr m_Ptr;
    #pragma warning restore 414

        public bool IsValid => m_Ptr != IntPtr.Zero;
    }

    public static partial class MachineLearningOperatorFactory
    {
        public ref struct IdentityDescriptor
        {
            public MachineLearningTensorDescriptor X;
            public MachineLearningTensorDescriptor O;
        }

        public ref struct GemmDescriptor
        {
            public MachineLearningTensorDescriptor X;
            public MachineLearningTensorDescriptor Y;
            public MachineLearningTensorDescriptor Z;
            public MachineLearningTensorDescriptor O;
            public bool transposeX;
            public bool transposeY;
            public float alpha;
            public float beta;
            public MachineLearningOperatorType fusedActivation;
        }

        public ref struct ConvDescriptor
        {
            public MachineLearningTensorDescriptor X;
            public MachineLearningTensorDescriptor K;
            public MachineLearningTensorDescriptor B;
            public MachineLearningTensorDescriptor O;
            public int groups;
            public ReadOnlySpan<int> strides;
            public ReadOnlySpan<int> pads;
            public ReadOnlySpan<int> dilations;
            public MachineLearningOperatorType fusedActivation;
        }

        public ref struct ReduceDescriptor
        {
            public MachineLearningTensorDescriptor X;
            public MachineLearningTensorDescriptor O;
            public MachineLearningOperatorType reduceFunc;
            public ReadOnlySpan<int> axes;
        }

        public static MachineLearningOperator Identity(MachineLearningContext context, in IdentityDescriptor desc)
        {
            return Identity_Internal(context, in desc);
        }

        public static MachineLearningOperator Gemm(MachineLearningContext context, in GemmDescriptor desc)
        {
            return Gemm_Internal(context, desc);
        }

        public static MachineLearningOperator Reduce(MachineLearningContext context, in ReduceDescriptor desc)
        {
            return Reduce_Internal(context, desc);
        }

        public static MachineLearningOperator Conv(MachineLearningContext context, in ConvDescriptor desc)
        {
            return Conv_Internal(context, desc);
        }

    }


    public static partial class MachineLearningOperatorDispatcher
    {
#nullable enable
        public static void Identity(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer O)
        {
            Identity_Internal(cb, op, X, O);
        }

        public static void Gemm(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer Y, ComputeBuffer? Z, ComputeBuffer O)
        {
            Gemm_Internal(cb, op, X, Y, Z, O);
        }

        public static void Conv(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer K, ComputeBuffer? B, ComputeBuffer O)
        {
            Conv_Internal(cb, op, X, K, B, O);
        }

        public static void Reduce(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer O)
        {
            Reduce_Internal(cb, op, X, O);
        }
#nullable disable
    }
}
