// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningContext.h")]
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningOperator.h")]
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningOperatorAttributes.h")]

    // Keep this in sync with the C++ counterpart
    public enum MachineLearningOperatorType : UInt32
    {
        None,
        Identity,
        Gemm,
        Conv,
        ReLU,
        ReduceMax,
        ReduceMean,
        ReduceMin,
        ReduceProd,
        ReduceSum,
        ReduceSumSquare,
        ReduceL1,
        ReduceL2,
        ReduceLogSum,
        ReduceLogSumExp,
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct MachineLearningOperator : IEquatable<MachineLearningOperator>
    {

        [FreeFunction(Name = "MachineLearning_Bindings::AddInputTensorToOperator")]
        extern static internal void AddInputTensor_Internal(IntPtr self, IntPtr tensor);

        [FreeFunction(Name = "MachineLearning_Bindings::ResetInputTensorsOfOperator")]
        extern static internal void ResetInputTensors_Internal(IntPtr self);

        [FreeFunction(Name = "MachineLearning_Bindings::AddOutputTensorToOperator")]
        extern static internal void AddOutputTensor_Internal(IntPtr self, IntPtr tensor);

        [FreeFunction(Name = "MachineLearning_Bindings::ResetOutputTensorsOfOperator")]
        extern static internal void ResetOutputTensors_Internal(IntPtr self);

        [FreeFunction(Name = "MachineLearning_Bindings::DispatchOperator")]
        extern static internal  void Dispatch_Internal(IntPtr self);

        [FreeFunction(Name = "MachineLearning_Bindings::BuildOperator")]
        extern static internal bool Build_Internal(IntPtr self);

        [StructLayout(LayoutKind.Sequential)]
        internal struct IdentityAttributes
        {
            public MachineLearningOperatorType type;
        }

        private const int kMaxConvolutionRank = 3;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ConvAttributes
        {
            public MachineLearningOperatorType type;
            public fixed Int32 pads[2 * kMaxConvolutionRank]; // left right top bottom front back
            public fixed Int32 dilations[kMaxConvolutionRank];
            public fixed Int32 strides[kMaxConvolutionRank];
            public Int32 groups;
            public MachineLearningOperatorType fusedActivation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ReduceAttributes
        {
            public MachineLearningOperatorType type;
            public UInt32 axes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GemmAttributes
        {
            public MachineLearningOperatorType type;
            public Int32 transposeA;
            public Int32 transposeB;
            public float alpha;
            public float beta;
            public MachineLearningOperatorType fusedActivation;
        }

        internal static unsafe ConvAttributes ToConvAttributes(int groups, ReadOnlySpan<int> strides, ReadOnlySpan<int> pads, ReadOnlySpan<int> dilations, MachineLearningOperatorType fusedActivation)
        {
            // populate attributes
            MachineLearningOperator.ConvAttributes attrib = new MachineLearningOperator.ConvAttributes();
            attrib.type = MachineLearningOperatorType.Conv;
            attrib.groups = groups;
            for (int i = 0; i < strides.Length; i++)
            {
                attrib.strides[i] = strides[i];
            }

            for (int i = 0; i < pads.Length; i++)
            {
                attrib.pads[i] = pads[i];
            }

            for (int i = 0; i < dilations.Length; i++)
            {
                attrib.dilations[i] = dilations[i];
            }
            attrib.fusedActivation = fusedActivation;

            return attrib;
        }

        internal static ReduceAttributes ToReduceAttributes(ReadOnlySpan<int> axes, int dimensionCount, MachineLearningOperatorType reduceFunc)
        {
            var attrib = new MachineLearningOperator.ReduceAttributes();
            attrib.type = reduceFunc;
            attrib.axes = 0;
            foreach (int axis in axes)
            {
                attrib.axes |= (UInt32)1 << (axis >= 0 ? axis : axis + dimensionCount);
            }

            return attrib;
        }

        internal static GemmAttributes ToGemmAttributes(bool transposeA, bool transposeB, float alpha, float beta, MachineLearningOperatorType fusedActivation)
        {
            var attrib = new MachineLearningOperator.GemmAttributes();
            attrib.type = MachineLearningOperatorType.Gemm;
            attrib.transposeA = transposeA ? 1 : 0;
            attrib.transposeB = transposeB ? 1 : 0;
            attrib.alpha = alpha;
            attrib.beta = beta;
            attrib.fusedActivation = fusedActivation;

            return attrib;
        }

        public bool Equals(MachineLearningOperator other)
        {
            return m_Ptr.Equals(other.m_Ptr);
        }

        public override bool Equals(object obj)
        {
            return obj is MachineLearningOperator other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Ptr.GetHashCode();
        }
    }

    public static partial class MachineLearningOperatorFactory
    {
        private static MachineLearningOperator Identity_Internal(MachineLearningContext context, in IdentityDescriptor desc)
        {
            Span<MachineLearningTensorDescriptor> inputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.X };
            Span<MachineLearningTensorDescriptor> outputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.O };
            var attributes = new MachineLearningOperator.IdentityAttributes();
            attributes.type = MachineLearningOperatorType.Identity;
            return context.BuildIdentity_Internal(inputDescs, outputDescs, attributes);
        }

        private static MachineLearningOperator Gemm_Internal(MachineLearningContext context, in GemmDescriptor desc)
         {
             Span<MachineLearningTensorDescriptor> inputDescs = stackalloc MachineLearningTensorDescriptor[3] { desc.X, desc.Y, desc.Z};
             Span<MachineLearningTensorDescriptor> outputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.O };
             var attributes = MachineLearningOperator.ToGemmAttributes(desc.transposeX, desc.transposeY, desc.alpha, desc.beta, desc.fusedActivation);
             return context.BuildGemm_Internal(inputDescs, outputDescs, attributes);
         }

        private static MachineLearningOperator Conv_Internal(MachineLearningContext context, in ConvDescriptor desc)
        {
             Span<MachineLearningTensorDescriptor> inputDescs = stackalloc MachineLearningTensorDescriptor[3] { desc.X, desc.K,  desc.B};
             Span<MachineLearningTensorDescriptor> outputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.O };
             var attributes = MachineLearningOperator.ToConvAttributes(desc.groups, desc.strides, desc.pads, desc.dilations, desc.fusedActivation);
             return context.BuildConv_Internal(inputDescs, outputDescs, attributes);
        }

        private static MachineLearningOperator Reduce_Internal(MachineLearningContext context, in ReduceDescriptor desc)
        {
            Span<MachineLearningTensorDescriptor> inputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.X };
            Span<MachineLearningTensorDescriptor> outputDescs = stackalloc MachineLearningTensorDescriptor[1] { desc.O };
            var attributes = MachineLearningOperator.ToReduceAttributes(desc.axes, (int)desc.X.shape.rank, desc.reduceFunc);
            return context.BuildReduce_Internal(inputDescs, outputDescs, attributes);
        }
    }

    public static partial class MachineLearningOperatorDispatcher
    {
#nullable enable
        internal static void Identity_Internal(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer O)
        {
            Span<IntPtr> inputs = stackalloc IntPtr[1] { X.m_Ptr };
            Span<IntPtr> outputs = stackalloc IntPtr[1] { O.m_Ptr };
            RecordDispatch(cb, op, inputs, outputs);
        }

        internal static void Gemm_Internal(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer Y, ComputeBuffer? Z, ComputeBuffer O)
        {
            Span<IntPtr> inputs = stackalloc IntPtr[3] { X.m_Ptr, Y.m_Ptr, Z?.m_Ptr ?? IntPtr.Zero };
            Span<IntPtr> outputs = stackalloc IntPtr[1] { O.m_Ptr };
            RecordDispatch(cb, op, inputs, outputs);
        }

        internal static void Conv_Internal(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer K, ComputeBuffer? B, ComputeBuffer O)
        {
            Span<IntPtr> inputs = stackalloc IntPtr[3] { X.m_Ptr, K.m_Ptr, B?.m_Ptr ?? IntPtr.Zero };
            Span<IntPtr> outputs = stackalloc IntPtr[1] { O.m_Ptr };
            RecordDispatch(cb, op, inputs, outputs);
        }

        internal static void Reduce_Internal(CommandBuffer? cb, MachineLearningOperator op, ComputeBuffer X, ComputeBuffer O)
        {
            Span<IntPtr> inputs = stackalloc IntPtr[1] { X.m_Ptr };
            Span<IntPtr> outputs = stackalloc IntPtr[1] { O.m_Ptr };
            RecordDispatch(cb, op, inputs, outputs);
        }
#nullable disable

        private static void RecordDispatch(CommandBuffer cb, MachineLearningOperator op, ReadOnlySpan<IntPtr> inputs, ReadOnlySpan<IntPtr> outputs)
        {
            if (cb != null)
            {
                cb.SetMachineLearningOperatorTensors(op, inputs, outputs);
                cb.DispatchMachineLearningOperator(op);
            }
            else
            {
                MachineLearningOperator.ResetInputTensors_Internal(op.m_Ptr);
                foreach(var input in inputs)
                {
                    MachineLearningOperator.AddInputTensor_Internal(op.m_Ptr, input);
                }
                MachineLearningOperator.ResetOutputTensors_Internal(op.m_Ptr);
                foreach(var output in outputs)
                {
                    MachineLearningOperator.AddOutputTensor_Internal(op.m_Ptr, output);
                }
                MachineLearningOperator.Dispatch_Internal(op.m_Ptr);
            }
        }
    }
}
