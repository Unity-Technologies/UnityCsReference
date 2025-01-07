// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningContext.h")]
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningOperator.h")]
    [NativeHeader("Runtime/Graphics/MachineLearning/MachineLearningOperatorAttributes.h")]

    public partial class MachineLearningContext : IDisposable
    {
        [FreeFunction(Name = "MachineLearning_Bindings::CreateContext")]
        extern private static IntPtr CreateContext();

        [FreeFunction(Name = "MachineLearning_Bindings::DestroyContext")]
        extern private static void DestroyContext(IntPtr op);

        [FreeFunction(Name = "MachineLearning_Bindings::BuildOperatorForContext<IdentityAttributes>", HasExplicitThis = true)]
        extern internal MachineLearningOperator BuildIdentity_Internal(ReadOnlySpan<MachineLearningTensorDescriptor> inputDescriptors, ReadOnlySpan<MachineLearningTensorDescriptor> outputDescriptors,
            MachineLearningOperator.IdentityAttributes attributes);

        [FreeFunction(Name = "MachineLearning_Bindings::BuildOperatorForContext<ConvAttributes>", HasExplicitThis = true)]
        extern internal MachineLearningOperator BuildConv_Internal(ReadOnlySpan<MachineLearningTensorDescriptor> inputDescriptors, ReadOnlySpan<MachineLearningTensorDescriptor> outputDescriptors, MachineLearningOperator.ConvAttributes attributes);

        [FreeFunction(Name = "MachineLearning_Bindings::BuildOperatorForContext<ReduceAttributes>", HasExplicitThis = true)]
        extern internal MachineLearningOperator BuildReduce_Internal(ReadOnlySpan<MachineLearningTensorDescriptor> inputDescriptors, ReadOnlySpan<MachineLearningTensorDescriptor> outputDescriptors, MachineLearningOperator.ReduceAttributes attributes);

        [FreeFunction(Name = "MachineLearning_Bindings::BuildOperatorForContext<GemmAttributes>", HasExplicitThis = true)]
        extern internal MachineLearningOperator BuildGemm_Internal(ReadOnlySpan<MachineLearningTensorDescriptor> inputDescriptors, ReadOnlySpan<MachineLearningTensorDescriptor> outputDescriptors, MachineLearningOperator.GemmAttributes attributes);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityEngine.Rendering.MachineLearningContext obj) => obj.m_Ptr;
        }
    }
}
