// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Animations.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        /// <summary>
        /// Blends two input streams using a blending weight value and stream weight masks. The result
        /// of the operation is stored in the output stream.
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation of input1 and input2.</param>
        /// <param name="input1">The stream to blend.</param>
        /// <param name="input2">The stream to blend.</param>
        /// <param name="weight">The blending weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void Blend(
            ref AnimationStream output,
            in AnimationStream input1,
            in AnimationStream input2,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input1);
            output.ValidateRigEquality(input2);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var input1Data = input1.InterpolatedDataChunksReadOnly;
            var input2Data = input2.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.lerp(input1Data[i], input2Data[i], wMasks[wIdx] * weight);
            }

            var input1IntData = input1.DiscreteDataChunksReadOnly;
            var input2IntData = input2.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(input1IntData[i], input2IntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var input1Rot = input1.RotationDataChunksReadOnly;
            var input2Rot = input2.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = input1Rot[i].Nlerp(input2Rot[i], wMasks[wIdx] * weight);
            }

            output.OrMasks(input1, input2);
        }

        /// <summary>
        /// Blends two input streams using a blending weight value and stores the result in the output stream.
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation of input1 and input2.</param>
        /// <param name="input1">The stream to blend.</param>
        /// <param name="input2">The stream to blend.</param>
        /// <param name="weight">The blending weight.</param>
        [BurstCompile]
        static internal void Blend(
            ref AnimationStream output,
            in AnimationStream input1,
            in AnimationStream input2,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input1);
            output.ValidateRigEquality(input2);

            // Blend 4 wide lerp non rotation data
            var input1Data = input1.InterpolatedDataChunksReadOnly;
            var input2Data = input2.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.lerp(input1Data[i], input2Data[i], weight);
            }

            var outputIntData = output.DiscreteDataChunks;
            if (weight > 0.5f)
                input2.DiscreteDataChunksReadOnly.CopyTo(outputIntData);
            else
                input1.DiscreteDataChunksReadOnly.CopyTo(outputIntData);

            // Blend 4-wide rotations
            var inputRot1 = input1.RotationDataChunksReadOnly;
            var inputRot2 = input2.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = inputRot1[i].Nlerp(inputRot2[i], weight);
            }

            output.OrMasks(input1, input2);
        }

        /// <summary>
        /// In place blending of output and input streams using a blending weight value and stream weight masks. This is
        /// the equivalent of calling Core.Blend(ref output, output, input, weight, mask).
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="input">The stream to blend to with the output stream.</param>
        /// <param name="weight">The blending weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void Blend(
            ref AnimationStream output,
            in AnimationStream input,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var inputData = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.lerp(outputData[i], inputData[i], wMasks[wIdx] * weight);
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(outputIntData[i], inputIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var inputRot = input.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = outputRot[i].Nlerp(inputRot[i], wMasks[wIdx] * weight);
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// In place blending of output and input streams using a blending weight value and stream weight masks. This is
        /// the equivalent of calling Core.Blend(ref output, output, input, weight, mask).
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation, in local space.</param>
        /// <param name="input">The stream to blend to with the output stream, in local space.</param>
        /// <param name="weight">The blending weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static unsafe internal void BlendCharacterSpace(
            ref AnimationStream output,
            ref AnimationStream input,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var inputData = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.lerp(outputData[i], inputData[i], wMasks[wIdx] * weight);
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(outputIntData[i], inputIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            var wRotationMasks = mask.m_RotationMasks.m_Ptr;
            input.ConvertRotationsToCharacterSpace();
            {
                // Blend root
                var w = wRotationMasks[0] * weight;
                output.SetLocalToParentRotation(0,
                    math.nlerp(output.GetLocalToParentRotation(0), input.GetLocalToParentRotation(0), w));
                // Input gets progressively set to the global space rotations of A
                input.SetLocalToParentRotation(0, output.GetLocalToParentRotation(0));
            }
            for (int i = 1; i < input.RotationCount; ++i)
            {
                var w = wRotationMasks[i] * weight;
                var parentGlobal = input.GetLocalToParentRotation(input.Rig.Skeleton.ParentIndexes[i]);

                var newLocal = math.nlerp(output.GetLocalToParentRotation(i),
                    math.mul(math.conjugate(parentGlobal), input.GetLocalToParentRotation(i)), w);
                output.SetLocalToParentRotation(i, newLocal);
                input.SetLocalToParentRotation(i, math.mul(parentGlobal, newLocal));
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// In place blending of output and input streams using a blending weight value. This is
        /// the equivalent of calling Core.Blend(ref output, output, input, weight).
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="input">The stream to blend with the output stream.</param>
        /// <param name="weight">The blending weight value.</param>
        [BurstCompile]
        static internal void Blend(
            ref AnimationStream output,
            in AnimationStream input,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);

            // Blend 4-wide lerp non rotation data
            var inputData  = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.lerp(outputData[i], inputData[i], weight);
            }

            if (weight > 0.5f)
            {
                var outputIntData = output.DiscreteDataChunks;
                input.DiscreteDataChunksReadOnly.CopyTo(outputIntData);
            }

            // Blend 4-wide rotations
            var inputRot  = input.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = outputRot[i].Nlerp(inputRot[i], weight);
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// Blends an input stream with an additive stream using a blend weight value and stream weight masks.
        /// The result is stored in the output stream.
        /// </summary>
        /// <remarks>
        /// The effective weight of a channel is <paramref name="weight"/> multiplied by the weight of that channel in
        /// <paramref name="mask"/>. If the effective weight of a discrete channel of <paramref name="output"/> is greater
        /// than 0.5, the channel takes the value from <paramref name="additive"/>.
        /// Otherwise, the channel takes the value from <paramref name="input"/>.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The blend weight value.</param>
        /// <param name="mask">The stream weight mask.</param>
        [BurstCompile]
        static internal void BlendAdditive(
            ref AnimationStream output,
            in AnimationStream input,
            in AnimationStream additive,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);
            output.ValidateRigEquality(additive);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var inputData = input.InterpolatedDataChunksReadOnly;
            var addData = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.mad(addData[i], wMasks[wIdx] * weight, inputData[i]);
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var additiveIntData = additive.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(inputIntData[i], additiveIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var inputRot = input.RotationDataChunksReadOnly;
            var additiveRot = additive.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = inputRot[i].Mul(additiveRot[i].QuatWeight(wMasks[wIdx] * weight));
            }

            output.OrMasks(input, additive);
        }

        /// <summary>
        /// Blends an input stream with an additive stream using a blend weight value.
        /// The result is stored in the output stream.
        /// </summary>
        /// <remarks>
        /// If <paramref name="weight"/> is greater than 0.5, each discrete channel of <paramref name="output"/> takes the
        /// value from <paramref name="additive"/>.
        /// Otherwise, the channel takes the value from <paramref name="input"/>.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The blend weight value.</param>
        [BurstCompile]
        static internal void BlendAdditive(
            ref AnimationStream output,
            in AnimationStream input,
            in AnimationStream additive,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);
            output.ValidateRigEquality(additive);

            // Blend 4-wide lerp non rotation data
            var inputData = input.InterpolatedDataChunksReadOnly;
            var addData = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.mad(addData[i], weight, inputData[i]);
            }

            var outputIntData = output.DiscreteDataChunks;
            input.DiscreteDataChunksReadOnly.CopyTo(outputIntData);
            if (weight > 0.5f)
            {
                var addIntData = additive.DiscreteDataChunksReadOnly;
                for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
                {
                    outputIntData[i] = addIntData[i];
                }
            }

            // Blend 4-wide rotations
            var inputRot = input.RotationDataChunksReadOnly;
            var addRot = additive.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = inputRot[i].Mul(addRot[i].QuatWeight(weight));
            }

            output.OrMasks(input, additive);
        }

        /// <summary>
        /// In place blending of the output stream with an additive stream using a blend weight value and stream weight masks.
        /// This is the equivalent of calling Core.BlendAdditive(ref output, output, input, weight, mask).
        /// </summary>
        /// <remarks>
        /// The effective weight of a channel is <paramref name="weight"/> multiplied by the weight of that channel in
        /// <paramref name="mask"/>. If the effective weight of a discrete channel of <paramref name="output"/> is greater
        /// than 0.5, the channel takes the value from <paramref name="additive"/>.
        /// Otherwise, the channel doesn't change.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The blend weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void BlendAdditive(
            ref AnimationStream output,
            in AnimationStream additive,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(additive);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var addData = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.mad(addData[i], wMasks[wIdx] * weight, outputData[i]);
            }

            var additiveIntData = additive.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select( outputIntData[i], additiveIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var additiveRot = additive.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = outputRot[i].Mul(additiveRot[i].QuatWeight(wMasks[wIdx] * weight));
            }

            output.OrMasks(additive);
        }

        /// <summary>
        /// In place blending of the output stream with an additive stream using a blend weight value and stream weight masks.
        /// </summary>
        /// <remarks>
        /// The effective weight of a channel is <paramref name="weight"/> multiplied by the weight of that channel in
        /// <paramref name="mask"/>. If the effective weight of a discrete channel of <paramref name="output"/> is greater
        /// than 0.5, the channel takes the value from <paramref name="additive"/>.
        /// Otherwise, the channel doesn't change. Character space doesn't affect this selection.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation, in character space.</param>
        /// <param name="scratchpad">Animation stream used to store temporary results</param>
        /// <param name="additive">The additive stream to blend, in local space.</param>
        /// <param name="weight">The blend weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void BlendAdditiveCharacterSpace(
            ref AnimationStream output,
            ref AnimationStream scratchpad,
            ref AnimationStream additive,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(additive);
            output.ValidateRigEquality(scratchpad);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Translation, scale and scalars are not affected by character space: blend them normally

            // Blend 4-wide lerp non rotation data
            var addData = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = math.mad(addData[i], wMasks[wIdx] * weight, outputData[i]);
            }

            var additiveIntData = additive.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select( outputIntData[i], additiveIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Pre-multiply weights in the additive stream so that it is working in local space as the user expects
            {
                var additiveRotLocalSpace = additive.RotationDataChunks;
                for (int i = 0, count = additive.RotationDataChunkCount; i < count; ++i, ++wIdx)
                {
                    additiveRotLocalSpace[i] = additiveRotLocalSpace[i].QuatWeight(wMasks[wIdx] * weight);
                }
            }

            // First convert the additive stream to character space
            scratchpad.ResetToDefaultValues();
            additive.Add(scratchpad, additive);

            additive.ConvertRotationsToCharacterSpace();
            scratchpad.ConvertRotationsToCharacterSpace();

            // Blend 4-wide rotations
            var additiveRot = additive.RotationDataChunksReadOnly;
            var scratchpadRot = scratchpad.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                // characterSpaceDelta is multiplied to the left of outputRot[i] to apply a rotation in character space, and not to the right (which is a rotation in local space)
                var characterSpaceDelta = additiveRot[i].Mul(scratchpadRot[i].Conjugate());
                outputRot[i] = characterSpaceDelta.Mul(outputRot[i]);
            }

            output.OrMasks(additive);
        }

        /// <summary>
        /// In place blending of the output stream with an additive stream using a blend weight value and stream weight masks.
        /// </summary>
        /// <remarks>
        /// If <paramref name="weight"/> is greater than 0.5, each discrete channel of <paramref name="output"/> takes the
        /// value from <paramref name="additive"/>.
        /// Otherwise, the channel doesn't change. Character space doesn't affect this selection.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation, in character space.</param>
        /// <param name="scratchpad">Animation stream used to store temporary results</param>
        /// <param name="additive">The additive stream to blend, in local space.</param>
        /// <param name="weight">The blend weight value.</param>
        [BurstCompile]
        static internal void BlendAdditiveCharacterSpace(
            ref AnimationStream output,
            ref AnimationStream scratchpad,
            ref AnimationStream additive,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(additive);
            output.ValidateRigEquality(scratchpad);

            // Translation, scale and scalars are not affected by character space: blend them normally

            // Blend 4-wide lerp non rotation data
            var addData = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.mad(addData[i], weight, outputData[i]);
            }

            var outputIntData = output.DiscreteDataChunks;
            if (weight > 0.5f)
            {
                var addIntData = additive.DiscreteDataChunksReadOnly;
                for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
                {
                    outputIntData[i] = addIntData[i];
                }
            }

            {
                var additiveRotLocalSpace = additive.RotationDataChunks;
                for (int i = 0, count = additive.RotationDataChunkCount; i < count; ++i)
                {
                    additiveRotLocalSpace[i] = additiveRotLocalSpace[i].NormalizeSafe();
                }
            }

            // First convert the additive stream to character space
            scratchpad.ResetToDefaultValues();
            additive.Add(scratchpad, additive);

            additive.ConvertRotationsToCharacterSpace();
            scratchpad.ConvertRotationsToCharacterSpace();

            // Blend 4-wide rotations
            var additiveRot = additive.RotationDataChunksReadOnly;
            var scratchpadRot = scratchpad.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                // characterSpaceDelta is multiplied to the left of outputRot[i] to apply a rotation in character space, and not to the right (which is a rotation in local space)
                var characterSpaceDelta = additiveRot[i].Mul(scratchpadRot[i].Conjugate());
                outputRot[i] = (characterSpaceDelta.QuatWeight(weight)).Mul(outputRot[i]);
            }

            output.OrMasks(additive);
        }

        /// <summary>
        /// In place blending of the output stream with an additive stream using a blend weight value.
        /// This is the equivalent of calling Core.BlendAdditive(ref output, output, input, weight).
        /// </summary>
        /// <remarks>
        /// If <paramref name="weight"/> is greater than 0.5, each discrete channel of <paramref name="output"/> takes the
        /// value from <paramref name="additive"/>.
        /// Otherwise, the channel doesn't change.
        /// </remarks>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The blend weight value.</param>
        [BurstCompile]
        static internal void BlendAdditive(
            ref AnimationStream output,
            in AnimationStream additive,
            float weight
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(additive);

            // Blend 4-wide lerp non rotation data
            var addData  = additive.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = math.mad(addData[i], weight, outputData[i]);
            }

            if (weight > 0.5f)
            {
                var addIntData = additive.DiscreteDataChunksReadOnly;
                var outputIntData = output.DiscreteDataChunks;
                for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
                {
                    outputIntData[i] = addIntData[i];
                }
            }

            // Blend 4-wide rotations
            var addRot  = additive.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = outputRot[i].Mul(addRot[i].QuatWeight(weight));
            }

            output.OrMasks(additive);
        }

        /// <summary>
        /// Adds two input animation streams together and returns the result in the output stream.
        /// </summary>
        /// <param name="output">The resulting stream after the operation.</param>
        /// <param name="input1">The stream to add.</param>
        /// <param name="input2">The stream to add.</param>
        [BurstCompile]
        static internal void Add(
            ref AnimationStream output,
            in AnimationStream input1,
            in AnimationStream input2
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input1);
            output.ValidateRigEquality(input2);

            var input1Data = input1.InterpolatedDataChunksReadOnly;
            var input2Data = input2.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = input1Data[i] + input2Data[i];
            }

            var inputInt1Data = input1.DiscreteDataChunksReadOnly;
            var inputInt2Data = input2.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = inputInt1Data[i] + inputInt2Data[i];
            }

            // 4-wide add
            var input1Rot = input1.RotationDataChunksReadOnly;
            var input2Rot = input2.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = input1Rot[i].Mul(input2Rot[i]);
            }

            output.OrMasks(input1, input2);
        }

        /// <summary>
        /// In-place addition of two streams. Is equivalent to calling Core.Add(ref output, output, input)
        /// The output stream will hold the result.
        /// </summary>
        /// <param name="output">The resulting stream after the blending operation.</param>
        /// <param name="input">The stream to add.</param>
        [BurstCompile]
        static internal void Add(
            ref AnimationStream output,
            in AnimationStream input
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);

            var inputData = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] += inputData[i];
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] += inputIntData[i];
            }

            // 4-wide add
            var inputRot = input.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = outputRot[i].Mul(inputRot[i]);
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// Inverse input stream and store result in output stream.
        /// </summary>
        /// <param name="output">The resulting output stream.</param>
        /// <param name="input">The stream to inverse.</param>
        [BurstCompile]
        static internal void Inverse(
            ref AnimationStream output,
            in AnimationStream input
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);

            // 4-wide inverse
            var inputData = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = -inputData[i];
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = -inputIntData[i];
            }

            // Inverse 4-wide rotations
            var inputRot = input.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = inputRot[i].Conjugate();
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// In-place inverse of a stream.
        /// </summary>
        /// <param name="stream">Stream to inverse.</param>
        [BurstCompile]
        static internal void Inverse(ref AnimationStream stream)
        {
            stream.ValidateIsNotNull();

            // 4-wide inverse
            var outputData = stream.InterpolatedDataChunks;
            for (int i = 0, count = stream.InterpolatedDataChunkCount; i < count; ++i)
            {
                outputData[i] = -outputData[i];
            }

            var outputIntData = stream.DiscreteDataChunks;
            for (int i = 0, count = stream.DiscreteDataChunkCount; i < count; ++i)
            {
                outputIntData[i] = -outputIntData[i];
            }

            // Inverse 4-wide rotations
            var outputRot = stream.RotationDataChunks;
            for (int i = 0, count = stream.RotationDataChunkCount; i < count; ++i)
            {
                outputRot[i] = outputRot[i].Conjugate();
            }
        }

        /// <summary>
        /// Weight and input stream and store the result in the output stream.
        /// </summary>
        /// <param name="output">The weighted stream.</param>
        /// <param name="input">The stream to weight.</param>
        /// <param name="weight">The weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void Weight(
            ref AnimationStream output,
            in AnimationStream input,
            float weight,
            in AnimationStreamMask mask
        )
        {
            output.ValidateIsNotNull();
            output.ValidateRigEquality(input);
            output.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var inputData = input.InterpolatedDataChunksReadOnly;
            var outputData = output.InterpolatedDataChunks;
            for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputData[i] = inputData[i] * wMasks[wIdx] * weight;
            }

            var inputIntData = input.DiscreteDataChunksReadOnly;
            var outputIntData = output.DiscreteDataChunks;
            for (int i = 0, count = output.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputIntData[i] = math.select(int4.zero, inputIntData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var inputRot = input.RotationDataChunksReadOnly;
            var outputRot = output.RotationDataChunks;
            for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                outputRot[i] = inputRot[i].QuatWeight(wMasks[wIdx] * weight);
            }

            output.OrMasks(input);
        }

        /// <summary>
        /// Weights values of a stream based on provided weight masks.
        /// Equivaluate to calling Core.Weight(ref stream, stream, weights).
        /// </summary>
        /// <param name="stream">The stream to weight.</param>
        /// <param name="weight">The weight value.</param>
        /// <param name="mask">The stream weight masks.</param>
        [BurstCompile]
        static internal void Weight(
            ref AnimationStream stream,
            float weight,
            in AnimationStreamMask mask
        )
        {
            stream.ValidateIsNotNull();
            stream.ValidateCompatibility(mask);

            var wIdx = 0;
            var wMasks = mask.WeightDataChunksReadOnly;

            // Blend 4-wide lerp non rotation data
            var data = stream.InterpolatedDataChunks;
            for (int i = 0, count = stream.InterpolatedDataChunkCount; i < count; ++i, ++wIdx)
            {
                data[i] = data[i] * wMasks[wIdx] * weight;
            }

            var intData = stream.DiscreteDataChunks;
            for (int i = 0, count = stream.DiscreteDataChunkCount; i < count; ++i, ++wIdx)
            {
                intData[i] = math.select(int4.zero, intData[i], wMasks[wIdx] * weight > 0.5f);
            }

            // Blend 4-wide rotations
            var rotData = stream.RotationDataChunks;
            for (int i = 0, count = stream.RotationDataChunkCount; i < count; ++i, ++wIdx)
            {
                rotData[i] = rotData[i].QuatWeight(wMasks[wIdx] * weight);
            }
        }

        /// <summary>
        /// Mixes several weighted animation streams into one output stream.
        /// </summary>
        /// <remarks>
        /// The mixer adds the continuous channels of each stream, multiplied by the weight of the stream. It takes the
        /// discrete channels, such as integers and entity IDs, from the stream with the greatest weight. Use it, for
        /// example, to mix streams with the weights that a blend space computes.
        ///
        /// To mix streams, follow these steps:
        ///
        ///1. Declare a `PoseMixer`. The default value is ready to use.
        ///1. Call <see cref="Begin(ref AnimationStream)"/> with the output stream. The method resets the output
        ///   stream to zero.
        ///1. Call <see cref="Add(ref AnimationStream, in AnimationStream, float)"/> once for each stream and its
        ///   weight.
        ///1. Call <see cref="End(ref AnimationStream, in AnimationStream)"/> to finish the mix. The output stream
        ///   isn't a valid pose until you call it.
        ///
        /// All streams must use the same rig. If safety checks are enabled, `Add` throws an `InvalidOperationException`
        /// if a stream uses another rig.
        ///
        /// The weights aren't normalized. If they sum to more than 1, the translations and scales are a weighted sum of
        /// the streams, not an average, and the mixer doesn't add a default pose. The `End` method normalizes rotations
        /// once, after the mixer adds all streams. This differs from chaining pairwise blends.
        /// </remarks>
        /// <seealso cref="AnimationStreamBlendingExtensions"/>
        /// <seealso cref="FreeformBlendSpace2DData"/>
        [BurstCompile]
        public struct PoseMixer
        {
            float m_WeightSum;
            float m_WeightMax;

            /// <summary>
            /// Starts a mix and resets the output stream.
            /// </summary>
            /// <remarks>
            /// Call this method before the first call to
            /// <see cref="Add(ref AnimationStream, in AnimationStream, float)"/>. It sets all channels of
            /// <paramref name="output"/> to zero and clears the weights that the mixer has collected, so the previous
            /// contents of the stream are lost. Finish the mix with `End`.
            /// </remarks>
            /// <param name="output">The stream that accumulates the mixed result. The method overwrites its
            /// contents.</param>
            public void Begin(ref AnimationStream output)
            {
                output.ValidateIsNotNull();

                output.ResetToZero();
                m_WeightSum = 0;
                m_WeightMax = 0;
            }

            /// <summary>
            /// Adds a weighted animation stream to the mix.
            /// </summary>
            /// <remarks>
            /// The method scales each continuous channel by <paramref name="weight"/> and adds it to the output stream.
            /// Rotations are scaled and summed the same way, and `End` normalizes them. The discrete channels, such as
            /// integers and entity IDs, come from the stream with the greatest weight.
            /// The default pose that `End` adds takes part in this choice. Its weight is 1 minus the sum of all
            /// weights. For example, with the weights 0.1, 0.2, and 0.3, the default pose has the weight 0.4, so its
            /// discrete channels are used. If several streams have the same greatest weight, the stream that you added
            /// first is used.
            /// </remarks>
            /// <param name="output">The stream that accumulates the mixed result. Use the same stream as in the call to
            /// <see cref="Begin(ref AnimationStream)"/>.</param>
            /// <param name="add">The stream to add to the mix. It must use the same rig as
            /// <paramref name="output"/>.</param>
            /// <param name="weight">The weight of the stream in the mix. The mixer doesn't normalize weights.</param>
            public void Add(ref AnimationStream output, in AnimationStream add, float weight)
            {
                output.ValidateIsNotNull();
                output.ValidateRigEquality(add);
                Add(ref output, add, weight, ref this);
            }

            /// <summary>
            /// Finishes the mix and normalizes the result.
            /// </summary>
            /// <remarks>
            /// Call this method after the last call to
            /// <see cref="Add(ref AnimationStream, in AnimationStream, float)"/>. If the weights of the added streams
            /// sum to less than 1, it adds the default pose for the remaining weight. It then normalizes the rotations.
            /// The output stream isn't a valid pose until this method has run.
            /// </remarks>
            /// <param name="output">The stream that holds the mixed result. Use the same stream as in the calls to
            /// `Begin` and `Add`.</param>
            /// <param name="defaultPoseStream">The stream that fills the remaining weight when the weights of all added
            /// streams sum to less than 1. If this is a default stream, the mixer uses the
            /// default values from the rig definition.</param>
            public void End(ref AnimationStream output, in AnimationStream defaultPoseStream)
            {
                output.ValidateIsNotNull();
                End(ref output, defaultPoseStream, ref this);
            }

            [BurstCompile]
            static void Add(ref AnimationStream output, in AnimationStream add, float weight, ref PoseMixer state)
            {
                // Add 4-wide non rotational data
                var addData = add.InterpolatedDataChunksReadOnly;
                var outputData = output.InterpolatedDataChunks;
                for (int i = 0, count = output.InterpolatedDataChunkCount; i < count; ++i)
                {
                    outputData[i] = math.mad(addData[i], weight, outputData[i]);
                }

                if (weight > state.m_WeightMax)
                {
                    state.m_WeightMax = weight;
                    var outputIntData = output.DiscreteDataChunks;
                    add.DiscreteDataChunksReadOnly.CopyTo(outputIntData);
                }

                // Add 4-wide rotations
                var addRot = add.RotationDataChunksReadOnly;
                var outputRot = output.RotationDataChunks;
                for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
                {
                    outputRot[i] = outputRot[i].Add(addRot[i] * weight);
                }

                output.OrMasks(add);

                state.m_WeightSum += weight;
            }

            [BurstCompile]
            static void End(ref AnimationStream output, in AnimationStream defaultPoseInput, ref PoseMixer state)
            {
                if (state.m_WeightSum < 1f)
                {
                    if (!defaultPoseInput.IsCreated)
                    {
                        Add(ref output, output.GetDefaultValues(), 1f - state.m_WeightSum, ref state);
                    }
                    else
                    {
                        Add(ref output, defaultPoseInput, 1f - state.m_WeightSum, ref state);
                    }
                }

                // Normalize 4-wide rotations
                var outputRot = output.RotationDataChunks;
                for (int i = 0, count = output.RotationDataChunkCount; i < count; ++i)
                {
                    outputRot[i] = outputRot[i].NormalizeSafe();
                }
            }
        }

        /// <summary>
        /// Returns true if the weight of a mixer contributes to the resulting final pose. Otherwise, returns false.
        /// This function is used during graph queries, setting branch culling and mixer kernel operations
        /// </summary>
        /// <param name="weight">The weight of the mixer branch.</param>
        /// <returns>Returns true if the mixing branch contributes to the final pose computation, otherwise returns false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool IsMixerBranchEnabled(float weight) => math.abs(weight) > 0.001f;
    }

    /// <summary>
    /// Extension methods that blend, add, weight, invert, and subtract animation streams.
    /// </summary>
    /// <remarks>
    /// These methods let you call the stream operations on the stream that receives the result, for example
    /// `stream.Blend(input, weight)`. Each method writes its result into the stream that it's called on. All the
    /// streams that you pass to a method must use the same rig. Overloads that take an
    /// <see cref="AnimationStreamMask"/> scale the weight of each channel by the mask.
    ///
    /// The following example uses the `Unity.Animations` and `Unity.Collections` namespaces. It computes the weights of
    /// walk, jog, and run streams for a speed, then blends the streams into one output stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlendSpace1DExample.cs#BlendLocomotion}]]></code>
    /// </example>
    /// <seealso cref="BlendSpaceUtility"/>
    /// <seealso cref="AnimationStream"/>
    public static class AnimationStreamBlendingExtensions
    {
        /// <summary>
        /// Blends an input stream with the current stream using a blending value.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="weight">The weight of the input stream in the blend.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(this ref AnimationStream stream, in AnimationStream input, float weight) =>
            Core.Blend(ref stream, input, weight);

        /// <summary>
        /// Blends an input stream with the current stream using a blending value and weight masks.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="weight">The weight of the input stream in the blend.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(this ref AnimationStream stream, in AnimationStream input, float weight, in AnimationStreamMask mask) =>
            Core.Blend(ref stream, input, weight, mask);

        /// <summary>
        /// Blends two input streams using a blending weight value and stores the result in the animation stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input1">The first stream to blend.</param>
        /// <param name="input2">The second stream to blend.</param>
        /// <param name="weight">The weight of <paramref name="input2"/> in the blend.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(this ref AnimationStream stream, in AnimationStream input1, in AnimationStream input2, float weight) =>
            Core.Blend(ref stream, input1, input2, weight);

        /// <summary>
        /// Blends two input streams using a blending weight value and weight masks.
        /// The result is stored in the animation stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input1">The first stream to blend.</param>
        /// <param name="input2">The second stream to blend.</param>
        /// <param name="weight">The weight of <paramref name="input2"/> in the blend.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Blend(this ref AnimationStream stream, in AnimationStream input1, in AnimationStream input2, float weight, in AnimationStreamMask mask) =>
            Core.Blend(ref stream, input1, input2, weight, mask);

        /// <summary>
        /// Blend stream with an additive stream using a blend weight value.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The weight of the additive stream in the blend.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditive(this ref AnimationStream stream, in AnimationStream additive, float weight) =>
            Core.BlendAdditive(ref stream, additive, weight);

        /// <summary>
        /// Blend stream with an additive stream using a blend weight value and stream weight mask.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The weight of the additive stream in the blend.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditive(this ref AnimationStream stream, in AnimationStream additive, float weight, in AnimationStreamMask mask) =>
            Core.BlendAdditive(ref stream, additive, weight, mask);

        /// <summary>
        /// Blends an input stream with an additive stream using a blend weight value.
        /// The result is stored in the animation stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The weight of the additive stream in the blend.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditive(this ref AnimationStream stream, in AnimationStream input, in AnimationStream additive, float weight) =>
            Core.BlendAdditive(ref stream, input, additive, weight);

        /// <summary>
        /// Blends an input stream with an additive stream using a blend weight value and stream weight masks.
        /// The result is stored in the animation stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input">The stream to blend.</param>
        /// <param name="additive">The additive stream to blend.</param>
        /// <param name="weight">The weight of the additive stream in the blend.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditive(this ref AnimationStream stream, in AnimationStream input, in AnimationStream additive, float weight, in AnimationStreamMask mask) =>
            Core.BlendAdditive(ref stream, input, additive, weight, mask);

        /// <summary>
        /// Blends an additive stream into the stream and applies its rotations in character space.
        /// </summary>
        /// <remarks>
        /// <see cref="BlendAdditive(ref AnimationStream, in AnimationStream, float)"/> applies the additive rotation of
        /// each bone in the local space of that bone. This method applies it in character space instead. Use this
        /// method, for example, for an upper body aim layer.
        ///
        /// The method composes the additive rotations with the default pose of the rig and converts the result to
        /// character space. The rotations are relative to the default pose, not to the pose in the stream. The method
        /// then applies the difference before the rotation of each bone in the stream, not after it.
        /// Translations, scales, floats, integers, and entity IDs blend as they do in `BlendAdditive`.
        ///
        /// The scratchpad is a second stream on the same rig. The method overwrites it with intermediate values. The
        /// method also overwrites the channels of the additive stream, so the stream no longer holds the pose that you
        /// passed in. Copy it before the call if you need the original pose again.
        /// </remarks>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="scratchpad">A stream on the same rig that stores temporary results. The method overwrites its
        /// contents.</param>
        /// <param name="additive">The additive stream to blend, in local space. The method modifies it.</param>
        /// <param name="weight">The weight of the additive stream in the blend.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the streams don't
        /// use the same rig.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditiveCharacterSpace(this ref AnimationStream stream, ref AnimationStream scratchpad, ref AnimationStream additive, float weight) =>
            Core.BlendAdditiveCharacterSpace(ref stream, ref scratchpad, ref additive, weight);

        /// <summary>
        /// Blends the stream with an additive stream in character space, with a blend weight value and stream weight
        /// masks.
        /// </summary>
        /// <remarks>
        /// This overload scales the blend of each channel by the weight of that channel in <paramref name="mask"/>,
        /// multiplied by <paramref name="weight"/>, so you can blend only some bones. Discrete channels come from the
        /// additive stream when that combined weight is greater than 0.5. In all other respects it works like
        /// <see cref="BlendAdditiveCharacterSpace(ref AnimationStream, ref AnimationStream, ref AnimationStream, float)"/>:
        /// it applies the additive rotations in character space, overwrites the scratchpad stream, and modifies the
        /// additive stream.
        /// </remarks>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="scratchpad">A stream on the same rig that stores temporary results. The method overwrites its
        /// contents.</param>
        /// <param name="additive">The additive stream to blend, in local space. The method modifies it.</param>
        /// <param name="weight">The weight of the additive stream in the blend. Each channel also scales it by its
        /// weight in <paramref name="mask"/>.</param>
        /// <param name="mask">The mask that scales the weight of each channel, so that you can blend only some
        /// bones.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if safety checks are enabled and the streams don't
        /// use the same rig, or if <paramref name="mask"/> wasn't created for that rig.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlendAdditiveCharacterSpace(this ref AnimationStream stream, ref AnimationStream scratchpad, ref AnimationStream additive, float weight, in AnimationStreamMask mask) =>
            Core.BlendAdditiveCharacterSpace(ref stream, ref scratchpad, ref additive, weight, mask);

        /// <summary>
        /// Inverses the stream.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Inverse(this ref AnimationStream stream) =>
            Core.Inverse(ref stream);

        /// <summary>
        /// Inverses the input stream and stores the result in the stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input">The stream to inverse.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Inverse(this ref AnimationStream stream, in AnimationStream input) =>
            Core.Inverse(ref stream, input);

        /// <summary>
        /// Weights the stream using a stream weight mask.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="weight">The weight to apply to the stream. The mask scales it for each channel.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Weight(this ref AnimationStream stream, float weight, in AnimationStreamMask mask) =>
            Core.Weight(ref stream, weight, mask);

        /// <summary>
        /// Weights the input stream using a stream weight mask and stores the result in the stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input">The stream to weight.</param>
        /// <param name="weight">The weight to apply to the stream. The mask scales it for each channel.</param>
        /// <param name="mask">The mask that scales the weight of each channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Weight(this ref AnimationStream stream, in AnimationStream input, float weight, in AnimationStreamMask mask) =>
            Core.Weight(ref stream, input, weight, mask);

        /// <summary>
        /// Adds stream values to the current stream.
        /// </summary>
        /// <param name="stream">The stream to modify. The method stores the result in this stream.</param>
        /// <param name="add">The stream to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this ref AnimationStream stream, in AnimationStream add) =>
            Core.Add(ref stream, add);

        /// <summary>
        /// Adds two input stream and stores the result in the stream.
        /// </summary>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input1">The first stream to add.</param>
        /// <param name="input2">The second stream to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this ref AnimationStream stream, in AnimationStream input1, in AnimationStream input2) =>
            Core.Add(ref stream, input1, input2);

        /// <summary>
        /// Computes the difference between input and a stream to subtract (delta = subtract^-1 * input) and
        /// stores the result in the stream.
        /// </summary>
        /// <remarks>
        /// Don't pass the stream itself as <paramref name="input"/> or <paramref name="subtract"/>. The method uses the
        /// stream for intermediate operations.
        /// </remarks>
        /// <param name="stream">The stream that receives the result. The method overwrites its previous
        /// contents.</param>
        /// <param name="input">The input stream.</param>
        /// <param name="subtract">The stream to subtract.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Delta(this ref AnimationStream stream, in AnimationStream input, in AnimationStream subtract)
        {
            unsafe
            {
                // Validate that input animation streams do not overlap with the stream.
                Core.ValidateIsTrue(stream.GetUnsafePtr() != input.GetUnsafePtr());
                Core.ValidateIsTrue(stream.GetUnsafePtr() != subtract.GetUnsafePtr());
            }

            Core.Inverse(ref stream, subtract);
            Core.Add(ref stream, input);
        }
    }
}
