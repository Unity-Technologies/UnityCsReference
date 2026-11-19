// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    /// <summary>
    /// Extension methods that create a new remap table from an existing one.
    /// </summary>
    /// <remarks>
    /// A remap table maps the channels of a source rig to the channels of a destination rig. Use
    /// <see cref="Flip(ref BlobAssetReference{RigRemapTable}, Unity.Collections.Allocator)"/> when you need the opposite
    /// mapping, for example to remap from the destination rig back to the source rig.
    /// </remarks>
    [GenerateTestsForBurstCompatibility, BurstCompile]
    public static class RigRemapTableExtensions
    {
        /// <summary>
        /// Creates a new RigRemapTable where sources and destinations are flipped.
        /// </summary>
        /// <remarks>
        /// A source channel can remap to multiple target channels, but the inverse is not possible.
        /// Flipping many channels mapping unto a single source will override some mappings any may be lossy.
        /// </remarks>
        /// <param name="remapTable">BlobAssetReference of a RigRemapTable.</param>
        /// <param name="allocator">Allocator used to create the RigRemapTable blob asset reference.</param>
        /// <returns>New BlobAssetReference of a RigRemapTable with channels flipped.</returns>
        public static BlobAssetReference<RigRemapTable> Flip(this ref BlobAssetReference<RigRemapTable> remapTable, Allocator allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var newRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            using var translationMappings = new NativeList<RigRemapTable.RemapEntry>(Allocator.Temp);
            using var rotationMappings = new NativeList<RigRemapTable.RemapEntry>(Allocator.Temp);
            using var translationOffsets = new NativeList<RigRemapTable.TranslationOffset>(Allocator.Temp);
            using var rotationOffsets = new NativeList<RigRemapTable.RotationOffset>(Allocator.Temp);

            FlipRigRemapEntries(ref remapTable.Value.TranslationMappings, translationMappings);
            FlipRigRemapEntries(ref remapTable.Value.RotationMappings, rotationMappings);
            FlipTranslationOffsets(ref remapTable.Value.TranslationOffsets, translationOffsets);
            FlipRotationOffsets(ref remapTable.Value.RotationOffsets, rotationOffsets);

            FlipRigRemapEntries(ref blobBuilder, ref remapTable.Value.ScaleMappings, ref newRemapTable.ScaleMappings);
            FlipRigRemapEntries(ref blobBuilder, ref remapTable.Value.FloatMappings, ref newRemapTable.FloatMappings);
            FlipRigRemapEntries(ref blobBuilder, ref remapTable.Value.IntMappings, ref newRemapTable.IntMappings);
            FlipRigRemapEntries(ref blobBuilder, ref remapTable.Value.EntityIdMappings, ref newRemapTable.EntityIdMappings);
            FlipRigRemapEntries(ref blobBuilder, ref remapTable.Value.SocketMappings, ref newRemapTable.SocketMappings);

            // The flipped destination indices are the old source indices, so the LocalToCharacter entries are sorted again
            var localToCharacterTranslationCount = CountLocalToCharacter(translationMappings, translationOffsets);
            var localToCharacterRotationCount = CountLocalToCharacter(rotationMappings, rotationOffsets);
            using var sortedLocalToCharacterTREntries = new NativeList<int2>(Allocator.Temp);
            RigRemapTable.ComputeSortedLocalToCharacterTREntries(
                sortedLocalToCharacterTREntries,
                translationMappings,
                rotationMappings,
                translationOffsets,
                rotationOffsets,
                localToCharacterTranslationCount,
                localToCharacterRotationCount);

            RigRemapTable.FillRigMapperBlobBuffer(ref blobBuilder, translationMappings, ref newRemapTable.TranslationMappings);
            RigRemapTable.FillRigMapperBlobBuffer(ref blobBuilder, rotationMappings, ref newRemapTable.RotationMappings);
            RigRemapTable.FillRigMapperBlobBuffer(ref blobBuilder, translationOffsets, ref newRemapTable.TranslationOffsets);
            RigRemapTable.FillRigMapperBlobBuffer(ref blobBuilder, rotationOffsets, ref newRemapTable.RotationOffsets);
            RigRemapTable.FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToCharacterTREntries, ref newRemapTable.SortedLocalToCharacterTREntries);
            newRemapTable.LocalToParentTRCount = math.int2(
                translationMappings.Length - localToCharacterTranslationCount,
                rotationMappings.Length - localToCharacterRotationCount);

            var newRemapTableAsset =  blobBuilder.CreateBlobAssetReference<RigRemapTable>(allocator);

            blobBuilder.Dispose();

            return newRemapTableAsset;
        }

        [BurstCompile]
        static void FlipRigRemapEntries(ref BlobBuilder blobBuilder, ref BlobArray<RigRemapTable.RemapEntry> source, ref BlobArray<RigRemapTable.RemapEntry> destination)
        {
            if (source.Length == 0)
                return;

            var builderArray = blobBuilder.Allocate(ref destination, source.Length);
            for (int i = 0; i < source.Length; ++i)
            {
                builderArray[i] = new RigRemapTable.RemapEntry
                {
                    SourceIndex = source[i].DestinationIndex,
                    DestinationIndex = source[i].SourceIndex,
                    OffsetIndex = source[i].OffsetIndex
                };
            }
        }


        static int CountLocalToCharacter(
            NativeList<RigRemapTable.RemapEntry> mappings,
            NativeList<RigRemapTable.TranslationOffset> offsets)
        {
            var count = 0;
            for (int i = 0; i < mappings.Length; ++i)
            {
                if (mappings[i].OffsetIndex > 0 && offsets[mappings[i].OffsetIndex].Space == RigRemapSpace.LocalToCharacter)
                    ++count;
            }

            return count;
        }

        static int CountLocalToCharacter(
            NativeList<RigRemapTable.RemapEntry> mappings,
            NativeList<RigRemapTable.RotationOffset> offsets)
        {
            var count = 0;
            for (int i = 0; i < mappings.Length; ++i)
            {
                if (mappings[i].OffsetIndex > 0 && offsets[mappings[i].OffsetIndex].Space == RigRemapSpace.LocalToCharacter)
                    ++count;
            }

            return count;
        }

        static void FlipRigRemapEntries(ref BlobArray<RigRemapTable.RemapEntry> source, NativeList<RigRemapTable.RemapEntry> destination)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                destination.Add(new RigRemapTable.RemapEntry
                {
                    SourceIndex = source[i].DestinationIndex,
                    DestinationIndex = source[i].SourceIndex,
                    OffsetIndex = source[i].OffsetIndex
                });
            }
        }

        static void FlipTranslationOffsets(ref BlobArray<RigRemapTable.TranslationOffset> source, NativeList<RigRemapTable.TranslationOffset> destination)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                destination.Add(new RigRemapTable.TranslationOffset
                {
                    Translation = math.mul(source[i].Rotation, -source[i].Translation / source[i].Scale),
                    Scale = 1f / source[i].Scale,
                    Rotation = math.inverse(source[i].Rotation),
                    Space = source[i].Space
                });
            }
        }

        static void FlipRotationOffsets(ref BlobArray<RigRemapTable.RotationOffset> source, NativeList<RigRemapTable.RotationOffset> destination)
        {
            for (int i = 0; i < source.Length; ++i)
            {
                destination.Add(new RigRemapTable.RotationOffset
                {
                    PreRotation = math.inverse(source[i].PreRotation),
                    PostRotation = math.inverse(source[i].PostRotation),
                    Space = source[i].Space
                });
            }
        }
    }
}
