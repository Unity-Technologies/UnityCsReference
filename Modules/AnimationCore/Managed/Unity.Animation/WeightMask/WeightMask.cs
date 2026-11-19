// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;

namespace Unity.Animations
{
    /// <summary>
    ///  Weight Mask data, this representation can be compatible with multiple different rig definitions holding
    ///  identical bindings.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct WeightMask
    {
        // Version of the blob layout that an AvatarMask stores in builds. Increase it when the layout changes
        internal const int BlobVersion = 1;

        internal BlobArray<StringHash> TransformBindings;
        internal BlobArray<StringHash> TranslationBindings;
        internal BlobArray<StringHash> RotationBindings;
        internal BlobArray<StringHash> ScaleBindings;
        internal BlobArray<StringHash> FloatBindings;
        internal BlobArray<StringHash> IntBindings;
        internal BlobArray<StringHash> EntityIdBindings;

        internal BlobArray<float> TransformWeights;
        internal BlobArray<BitField32> TransformPropagateWeights;

        internal BlobArray<float> TranslationWeights;
        internal BlobArray<float> RotationWeights;
        internal BlobArray<float> ScaleWeights;
        internal BlobArray<float> FloatWeights;
        internal BlobArray<float> IntWeights;
        internal BlobArray<float> EntityIdWeights;

        internal float DefaultTransformChannelWeight;
        internal float DefaultGenericChannelWeight;
    }

    // A WeightMask resolved for one RigDefinition. WeightMaskSampler owns it
    [GenerateTestsForBurstCompatibility]
    internal partial struct WeightMaskInstance
    {
        internal BlobArray<WeightData> Value;

        // Address of the RigDefinition blob that the instance was built for. Only compared, never dereferenced
        internal ulong RigId;
    }
}
