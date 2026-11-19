// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animations
{
    /// <summary>
    /// Core runtime functions.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static partial class Core
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AlignUp(int size, int align)
        {
            return (size + align - 1) & -align;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint AlignUp(nint address, int align)
        {
            return (address + align - 1) & -align;
        }

        internal static unsafe bool EnsureCapacity(ref byte* data, int length, ref int capacity, int requiredCapacity, int alignment, Allocator allocator)
        {
            int newCapacity = math.max(capacity, 1); // avoid infinite loop
            for (int i = 0; i < 32 && newCapacity < requiredCapacity; ++i)
                newCapacity *= 2;

            if (newCapacity == capacity)
                return false;

            byte* newData = (byte*) UnsafeUtility.Malloc(newCapacity, alignment, allocator);
            UnsafeUtility.MemCpy(newData, data, length);
            UnsafeUtility.Free(data, allocator);
            data = newData;
            capacity = newCapacity;

            return true;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void SetDataInSample<T>(ref BlobArray<float> samples, int offset, T data)
            where T : unmanaged
        {
            *(T*)((float*)samples.GetUnsafePtr() + offset) = data;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref T GetDataInSample<T>(ref BlobArray<float> samples, int offset)
            where T : unmanaged
        {
            return ref *(T*)((float*)samples.GetUnsafePtr() + offset);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref T GetDataInSample<T>(float* samples, int offset)
            where T : unmanaged
        {
            return ref *(T*)(samples + offset);
        }

        /// <summary>
        /// Copies the channel values of a source stream to a destination stream, using a rig remap table.
        /// </summary>
        /// <remarks>
        /// The function copies the translation, rotation, scale, float, integer, and entity ID channels that the remap table lists.
        /// It applies the offsets of the remap table to the translation and rotation values.
        /// Translation and rotation channels whose offset uses <see cref="RigRemapSpace.LocalToCharacter"/> space are
        /// read and written in that space. All other channels are read and written in
        /// <see cref="RigRemapSpace.LocalToParent"/> space.
        /// Channels that the remap table doesn't list keep their current value in the destination stream.
        ///
        /// The function doesn't copy sockets, because sockets belong to the rig, not to the stream. To map sockets, use
        /// <see cref="RigRemapTable.SocketMappings"/>.
        /// </remarks>
        /// <param name="remapTable">Remap table from the rig of the source stream to the rig of the destination stream.</param>
        /// <param name="destinationStream">Stream that receives the remapped values. It must be a created stream.</param>
        /// <param name="sourceStream">Stream that provides the values. It must be a created stream.</param>
        public static void Remap(
            ref RigRemapTable remapTable,
            ref AnimationStream destinationStream,
            ref AnimationStream sourceStream
        )
        {
            destinationStream.ValidateIsNotNull();
            sourceStream.ValidateIsNotNull();

            for (int i = 0, count = remapTable.LocalToParentTRCount.x; i < count; i++)
            {
                ref var mapping = ref remapTable.TranslationMappings[i];
                var value = sourceStream.GetLocalToParentTranslation(mapping.SourceIndex);

                if (mapping.OffsetIndex > 0)
                {
                    var translationOffset = remapTable.TranslationOffsets[mapping.OffsetIndex];
                    value = math.mul(translationOffset.Rotation, math.mad(value, translationOffset.Scale, translationOffset.Translation));
                }

                destinationStream.SetLocalToParentTranslation(mapping.DestinationIndex, value);
            }


            for (int i = 0, count = remapTable.LocalToParentTRCount.y; i < count; i++)
            {
                ref var mapping = ref remapTable.RotationMappings[i];
                var value = sourceStream.GetLocalToParentRotation(mapping.SourceIndex);

                if (mapping.OffsetIndex > 0)
                {
                    var rotationOffset = remapTable.RotationOffsets[mapping.OffsetIndex];
                    value = math.mul(math.mul(rotationOffset.PreRotation, value), rotationOffset.PostRotation);
                }

                destinationStream.SetLocalToParentRotation(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remapTable.ScaleMappings.Length; i++)
            {
                ref var mapping = ref remapTable.ScaleMappings[i];
                var value = sourceStream.GetLocalToParentScale(mapping.SourceIndex);
                destinationStream.SetLocalToParentScale(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remapTable.FloatMappings.Length; i++)
            {
                ref var mapping = ref remapTable.FloatMappings[i];
                var value = sourceStream.GetFloat(mapping.SourceIndex);
                destinationStream.SetFloat(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remapTable.IntMappings.Length; i++)
            {
                ref var mapping = ref remapTable.IntMappings[i];
                var value = sourceStream.GetInt(mapping.SourceIndex);
                destinationStream.SetInt(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remapTable.EntityIdMappings.Length; i++)
            {
                ref var mapping = ref remapTable.EntityIdMappings[i];
                var value = sourceStream.GetEntityId(mapping.SourceIndex);
                destinationStream.SetEntityId(mapping.DestinationIndex, value);
            }

            for (int i = 0; i < remapTable.SortedLocalToCharacterTREntries.Length; i++)
            {
                ref var localToCharacterTREntry = ref remapTable.SortedLocalToCharacterTREntries[i];
                if (localToCharacterTREntry.x != -1 && localToCharacterTREntry.y != -1)
                {
                    ref var translationMapping = ref remapTable.TranslationMappings[localToCharacterTREntry.x];
                    ref var rotationMapping = ref remapTable.RotationMappings[localToCharacterTREntry.y];

                    ValidateAreEqual(translationMapping.DestinationIndex, rotationMapping.DestinationIndex);

                    float3 tValue;
                    quaternion rValue;
                    if (translationMapping.SourceIndex == rotationMapping.SourceIndex)
                    {
                        sourceStream.GetLocalToCharacterTR(translationMapping.SourceIndex, out tValue, out rValue);
                    }
                    else
                    {
                        tValue = sourceStream.GetLocalToCharacterTranslation(translationMapping.SourceIndex);
                        rValue = sourceStream.GetLocalToCharacterRotation(rotationMapping.SourceIndex);
                    }

                    if (translationMapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.TranslationOffsets[translationMapping.OffsetIndex];
                        tValue = math.mul(offset.Rotation, math.mad(tValue, offset.Scale, offset.Translation));
                    }

                    if (rotationMapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.RotationOffsets[rotationMapping.OffsetIndex];
                        rValue = math.mul(math.mul(offset.PreRotation, rValue), offset.PostRotation);
                    }

                    destinationStream.SetLocalToCharacterTR(translationMapping.DestinationIndex, tValue, rValue);
                }
                else if (localToCharacterTREntry.y == -1)
                {
                    ref var mapping = ref remapTable.TranslationMappings[localToCharacterTREntry.x];
                    var value = sourceStream.GetLocalToCharacterTranslation(mapping.SourceIndex);

                    if (mapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.TranslationOffsets[mapping.OffsetIndex];
                        value = math.mul(offset.Rotation, math.mad(value, offset.Scale, offset.Translation));
                    }

                    destinationStream.SetLocalToCharacterTranslation(mapping.DestinationIndex, value);
                }
                else
                {
                    ref var mapping = ref remapTable.RotationMappings[localToCharacterTREntry.y];
                    var value = sourceStream.GetLocalToCharacterRotation(mapping.SourceIndex);

                    if (mapping.OffsetIndex > 0)
                    {
                        var offset = remapTable.RotationOffsets[mapping.OffsetIndex];
                        value = math.mul(math.mul(offset.PreRotation, value), offset.PostRotation);
                    }

                    destinationStream.SetLocalToCharacterRotation(mapping.DestinationIndex, value);
                }
            }
        }

        static float WeightForIndex(ref BlobArray<float> thresholdArray, int length, int index, float blend)
        {
            if (blend >= thresholdArray[index])
            {
                if (index + 1 == length)
                {
                    return 1.0f;
                }
                else if (thresholdArray[index + 1] < blend)
                {
                    return 0.0f;
                }
                else
                {
                    if (thresholdArray[index] - thresholdArray[index + 1] != 0)
                    {
                        return (blend - thresholdArray[index + 1]) / (thresholdArray[index] - thresholdArray[index + 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
            else
            {
                if (index == 0)
                {
                    return 1.0f;
                }
                else if (thresholdArray[index - 1] > blend)
                {
                    return 0.0f;
                }
                else
                {
                    if ((thresholdArray[index] - thresholdArray[index - 1]) != 0)
                    {
                        return (blend - thresholdArray[index - 1]) / (thresholdArray[index] - thresholdArray[index - 1]);
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
            }
        }

        internal static int AdjustLastFrameValue(int beforeLastValue, int atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? (int)math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        internal static float AdjustLastFrameValue(float beforeLastValue, float atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        internal static float3 AdjustLastFrameValue(float3 beforeLastValue, float3 atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        internal static float4 AdjustLastFrameValue(float4 beforeLastValue, float4 atDurationValue, float lastFrameError)
        {
            return lastFrameError < 1.0f ? math.lerp(beforeLastValue, atDurationValue, 1.0f / (1.0f - lastFrameError)) : atDurationValue;
        }

        /// <summary>
        /// Compute matrices in LocalToCharacter space with the specified offset. For example, if you want to compute
        /// the LocalToWorld matrices of the rig, pass in the LocalToWorld matrix of the root as the offset parameter.
        /// </summary>
        /// <param name="stream">Animation stream</param>
        /// <param name="offset">An extra offset which applies to all matrices</param>
        /// <param name="outMatrices">Resulting matrix computation. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        public static void ComputeLocalToCharacter(
            ref AnimationStream stream,
            AffineTransform offset,
            NativeArray<AffineTransform> outMatrices
        )
        {
            if (!stream.IsCreated || stream.BoneCount == 0)
                return;

            int count = stream.BoneCount;

            ValidateBufferLengthsAreEqual(outMatrices.Length, count);

            outMatrices[0] = math.mul(offset, stream.GetLocalToParentMatrix(0));
            for (int i = 1; i != count; ++i)
            {
                var pIdx = stream.Rig.Skeleton.ParentIndexes[i];
                outMatrices[i] = math.mul(outMatrices[pIdx], stream.GetLocalToParentMatrix(i));
            }
        }

        /// <summary>
        /// Compute two sets of matrices in LocalToCharacter space with specified offsets. For example, if you want to compute
        /// the LocalToWorld matrices of the rig, pass in the LocalToWorld matrix of the root as the offset parameter. This method
        /// is used to conveniently compute the LocalToCharacter and LocalToWorld matrices of an AnimationStream in one pass.
        /// </summary>
        /// <param name="stream">Animation stream</param>
        /// <param name="offset1">An extra offset which applies to all matrices in outMatrices1</param>
        /// <param name="outMatrices1">Resulting matrix computation using offset1. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        /// <param name="offset2">An extra offset which applies to all matrices in outMatrices2</param>
        /// <param name="outMatrices2">Resulting matrix computation using offset2. This array must be preallocated with the same bone count specified by the RigDefinition.</param>
        public static void ComputeLocalToCharacter(
            ref AnimationStream stream,
            AffineTransform offset1,
            NativeArray<AffineTransform> outMatrices1,
            AffineTransform offset2,
            NativeArray<AffineTransform> outMatrices2
        )
        {
            int count = stream.BoneCount;

            ValidateBufferLengthsAreEqual(outMatrices1.Length, count);
            ValidateBufferLengthsAreEqual(outMatrices2.Length, count);

            var mat = stream.GetLocalToParentMatrix(0);
            outMatrices1[0] = math.mul(offset1, mat);
            outMatrices2[0] = math.mul(offset2, mat);

            for (int i = 1; i != count; ++i)
            {
                var pIdx = stream.Rig.Skeleton.ParentIndexes[i];
                mat = stream.GetLocalToParentMatrix(i);

                outMatrices1[i] = math.mul(outMatrices1[pIdx], mat);
                outMatrices2[i] = math.mul(outMatrices2[pIdx], mat);
            }
        }

        /// <summary>
        /// Computes the linear velocity of each bone in LocalToParent space, from two consecutive streams of the same rig.
        /// </summary>
        /// <remarks>
        /// The velocity of a bone is the change of its `LocalToParent` translation between the two streams, divided by
        /// the elapsed time. Element j of the output belongs to the bone with index j.
        /// </remarks>
        /// <param name="input">The stream for the current update.</param>
        /// <param name="previousInput">The stream for the previous update. It must use the same rig as <paramref name="input"/>.</param>
        /// <param name="deltaTime">Time between the two streams, in seconds. It must be greater than zero.</param>
        /// <param name="outVelocities">Receives the velocity of each bone, in units per second. The length must be equal to the bone count of the rig.</param>
        public static void ComputeLocalToParentLinearVelocities(
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(previousInput);

            int translationCount = input.BoneCount;

            ValidateBufferLengthsAreEqual(translationCount, outVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);

            ValidateIsFinite(inverseDeltaTime);

            for (int j = 0; j < translationCount; ++j)
            {
                float3 from = previousInput.GetLocalToParentTranslation(j);
                float3 to = input.GetLocalToParentTranslation(j);

                float3 deltaTrans = to - from;
                outVelocities[j] = deltaTrans * inverseDeltaTime;
            }
        }

        /// <summary>
        /// Computes the angular velocity of each bone in LocalToParent space, from two consecutive streams of the same rig.
        /// </summary>
        /// <remarks>
        /// For each bone, the function computes the rotation that goes from the previous `LocalToParent` rotation to the
        /// current one, converts it to Euler angles with `math.Euler`, and divides the angles by the elapsed time. The
        /// result has one component per Euler axis. A bone whose rotation didn't change has a velocity of zero.
        /// </remarks>
        /// <param name="input">The stream for the current update.</param>
        /// <param name="previousInput">The stream for the previous update. It must use the same rig as <paramref name="input"/>.</param>
        /// <param name="deltaTime">Time between the two streams, in seconds. It must be greater than zero.</param>
        /// <param name="outAngularVelocities">Receives the angular velocity of each bone, in radians per second. The length must be equal to the bone count of the rig.</param>
        public static void ComputeLocalToParentAngularVelocities(
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outAngularVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(previousInput);

            int rotationCount = input.BoneCount;

            ValidateBufferLengthsAreEqual(rotationCount, outAngularVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);

            ValidateIsFinite(inverseDeltaTime);

            for (int j = 0; j < rotationCount; ++j)
            {
                quaternion from = previousInput.GetLocalToParentRotation(j);
                quaternion to = input.GetLocalToParentRotation(j);

                if (!to.Equals(from))
                    outAngularVelocities[j] = math.Euler(math.mul(math.inverse(from), to)) * inverseDeltaTime;
                else
                    outAngularVelocities[j] = float3.zero;
            }
        }

        /// <summary>
        /// Computes the linear velocity of each bone in world space, from two consecutive streams of the same rig.
        /// </summary>
        /// <remarks>
        /// The velocity of a bone is the change of its world position between the two streams, divided by the elapsed
        /// time. The world position of a bone is its `LocalToCharacter` translation transformed by the `LocalToWorld`
        /// matrix.
        /// </remarks>
        /// <param name="localToWorld">The `LocalToWorld` matrix of the character at the time of <paramref name="input"/>.</param>
        /// <param name="previousLocalToWorld">The `LocalToWorld` matrix of the character at the time of <paramref name="previousInput"/>.</param>
        /// <param name="input">The stream for the current update.</param>
        /// <param name="previousInput">The stream for the previous update. It must use the same rig as <paramref name="input"/>.</param>
        /// <param name="deltaTime">The time between the two streams, in seconds. It must not be zero.</param>
        /// <param name="outVelocities">Receives the velocity of each bone, in units per second. The length must be equal to the bone count of the rig.</param>
        public static void ComputeLocalToWorldLinearVelocities(
            float4x4 localToWorld,
            float4x4 previousLocalToWorld,
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(previousInput);

            int translationCount = input.BoneCount;

            ValidateBufferLengthsAreEqual(translationCount, outVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);
            ValidateIsFinite(inverseDeltaTime);
            for (int j = 0; j < translationCount; ++j)
            {
                float3 from = math.transform(previousLocalToWorld, previousInput.GetLocalToCharacterTranslation(j));
                float3 to = math.transform(localToWorld, input.GetLocalToCharacterTranslation(j));

                float3 deltaTrans = to - from;
                outVelocities[j] = deltaTrans * inverseDeltaTime;
            }
        }

        /// <summary>
        /// Computes the angular velocity of each bone in world space, from two consecutive streams of the same rig.
        /// </summary>
        /// <remarks>
        /// The world rotation of a bone is its `LocalToCharacter` rotation combined with the rotation of the
        /// `LocalToWorld` matrix. For each bone, the function computes the rotation that goes from the previous world
        /// rotation to the current one, converts it to Euler angles with `math.Euler`, and divides the angles by the
        /// elapsed time. The function ignores the translation of the matrices.
        /// </remarks>
        /// <param name="localToWorld">The `LocalToWorld` matrix of the character at the time of <paramref name="input"/>.</param>
        /// <param name="previousLocalToWorld">The `LocalToWorld` matrix of the character at the time of <paramref name="previousInput"/>.</param>
        /// <param name="input">The stream for the current update.</param>
        /// <param name="previousInput">The stream for the previous update. It must use the same rig as <paramref name="input"/>.</param>
        /// <param name="deltaTime">Time between the two streams, in seconds. It must be greater than zero.</param>
        /// <param name="outAngularVelocities">Receives the angular velocity of each bone, in radians per second. The length must be equal to the bone count of the rig.</param>
        public static void ComputeLocalToWorldAngularVelocities(
            float4x4 localToWorld,
            float4x4 previousLocalToWorld,
            ref AnimationStream input,
            ref AnimationStream previousInput,
            float deltaTime,
            NativeArray<float3> outAngularVelocities
        )
        {
            input.ValidateIsNotNull();
            input.ValidateRigEquality(previousInput);

            int rotationCount = input.BoneCount;

            ValidateBufferLengthsAreEqual(rotationCount, outAngularVelocities.Length);

            float inverseDeltaTime = math.rcp(deltaTime);
            ValidateIsFinite(inverseDeltaTime);

            var previousLocalToWorldRot = new quaternion(previousLocalToWorld);
            var localToWorldRot = new quaternion(localToWorld);

            for (int j = 0; j < rotationCount; ++j)
            {
                quaternion from = math.mul(previousLocalToWorldRot, previousInput.GetLocalToCharacterRotation(j));
                quaternion to = math.mul(localToWorldRot, input.GetLocalToCharacterRotation(j));

                outAngularVelocities[j] = math.Euler(math.mul(math.inverse(from), to)) * inverseDeltaTime;
            }
        }
    }
}
