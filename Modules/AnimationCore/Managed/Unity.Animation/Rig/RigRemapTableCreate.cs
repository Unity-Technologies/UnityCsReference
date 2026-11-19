// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    public partial struct RigRemapTable
    {
        static bool DefaultMatchBindings(StringHash srcId, StringHash destId) => srcId.Equals(destId);

        static int CompareRigRemapEntry(int lhsIndex, RigRemapSpace lhsSpace, int rhsIndex, RigRemapSpace rhsSpace)
        {
            var spaceCompare = math.select(math.select(1, -1, lhsSpace < rhsSpace), 0, lhsSpace == rhsSpace);
            return spaceCompare != 0 ? spaceCompare : lhsIndex.CompareTo(rhsIndex);
        }

        struct RigRemapTranslationEntryComparer : IComparer<RemapEntry>
        {
            public NativeList<TranslationOffset> TranslationOffets;

            public int Compare(RemapEntry x, RemapEntry y) => CompareRigRemapEntry(
                x.DestinationIndex, x.OffsetIndex > 0 ? TranslationOffets[x.OffsetIndex].Space : RigRemapSpace.LocalToParent,
                y.DestinationIndex, y.OffsetIndex > 0 ? TranslationOffets[y.OffsetIndex].Space : RigRemapSpace.LocalToParent
            );
        }

        struct RigRemapRotationEntryComparer : IComparer<RemapEntry>
        {
            public NativeList<RotationOffset> RotationOffets;

            public int Compare(RemapEntry x, RemapEntry y) => CompareRigRemapEntry(
                x.DestinationIndex, x.OffsetIndex > 0 ? RotationOffets[x.OffsetIndex].Space : RigRemapSpace.LocalToParent,
                y.DestinationIndex, y.OffsetIndex > 0 ? RotationOffets[y.OffsetIndex].Space : RigRemapSpace.LocalToParent);
        }

        /// <summary>
        /// Channel filter to specify which types of channels should be used for matching.
        /// </summary>
        [System.Flags]
        public enum ChannelFilter : byte
        {
            /// <summary>Does not match any channels, so the remap table has no mappings.</summary>
            None           = 0,
            /// <summary>Matches the translation channels of the source and destination rigs.</summary>
            Translation    = 1 << 0,
            /// <summary>Matches the rotation channels of the source and destination rigs.</summary>
            Rotation       = 1 << 1,
            /// <summary>Matches the scale channels of the source and destination rigs.</summary>
            Scale          = 1 << 2,
            /// <summary>Matches the float channels of the source and destination rigs.</summary>
            Float          = 1 << 3,
            /// <summary>Matches the integer channels of the source and destination rigs.</summary>
            Int            = 1 << 4,
            /// <summary>Matches the sockets of the source and destination rigs.</summary>
            Socket         = 1 << 5,
            /// <summary>Matches the entity ID channels of the source and destination rigs.</summary>
            EntityId       = 1 << 6,
            /// <summary>Matches the translation, rotation, scale, float, integer, socket, and entity ID channels.</summary>
            All         = Translation | Rotation | Scale | Float | Int | Socket | EntityId,
        }

        /// <summary>
        /// Offset overrides for translation and rotation channels in RigRemapTable.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct OffsetOverrides : System.IDisposable
        {
            internal NativeList<StringHash>           m_TranslationIds;
            internal NativeList<StringHash>           m_RotationIds;
            internal NativeList<TranslationOffset> m_TranslationOffsets;
            internal NativeList<RotationOffset>    m_RotationOffsets;

            internal int LocalToCharacterTranslationCount { get; private set; }
            internal int LocalToCharacterRotationCount { get; private set; }

            /// <summary>
            /// Creates an empty set of offset overrides.
            /// </summary>
            /// <remarks>
            /// Add overrides with
            /// <see cref="AddTranslationOffsetOverride(StringHash, RigRemapTable.TranslationOffset)"/> and
            /// <see cref="AddRotationOffsetOverride(StringHash, RigRemapTable.RotationOffset)"/>, then pass the set to
            /// <see cref="RigRemapTable.Create(BlobAssetReference{RigDefinition}, BlobAssetReference{RigDefinition}, Allocator, RigRemapTable.ChannelFilter, RigRemapTable.OffsetOverrides, System.Func{StringHash, StringHash, bool})"/>.
            /// Call <see cref="Dispose()"/> to release the memory of the set.
            /// </remarks>
            /// <param name="capacity">The number of translation overrides and the number of rotation overrides that the
            /// set can hold before it must grow.</param>
            /// <param name="allocator">The allocator to use for the memory of the set. Call <see cref="Dispose()"/> to
            /// release the memory.</param>
            public OffsetOverrides(int capacity, Allocator allocator)
            {
                m_TranslationIds = new NativeList<StringHash>(capacity, allocator);
                m_RotationIds = new NativeList<StringHash>(capacity, allocator);
                m_TranslationOffsets = new NativeList<TranslationOffset>(capacity, allocator);
                m_RotationOffsets = new NativeList<RotationOffset>(capacity, allocator);

                m_TranslationOffsets.Add(default);
                m_RotationOffsets.Add(default);
                LocalToCharacterTranslationCount = 0;
                LocalToCharacterRotationCount = 0;
            }

            /// <summary>
            /// Checks if the <see cref="OffsetOverrides"/> has been created
            /// </summary>
            /// <returns>True if the <see cref="OffsetOverrides"/> has been created</returns>
            public bool IsCreated => m_TranslationIds.IsCreated || m_RotationIds.IsCreated;
            /// <summary>
            /// Checks if there are translation offset overrides.
            /// </summary>
            public bool HasTranslationOffsetOverrides => m_TranslationIds.IsCreated ? m_TranslationIds.Length > 0 : false;
            /// <summary>
            /// Checks if there are rotation offset overrides.
            /// </summary>
            public bool HasRotationOffsetOverrides => m_RotationIds.IsCreated ? m_RotationIds.Length > 0 : false;

            /// <summary>
            /// Adds a translation offset override for a given binding.
            /// </summary>
            /// <param name="binding"><see cref="StringHash"/> binding</param>
            /// <param name="offset">Offset</param>
            public void AddTranslationOffsetOverride(StringHash binding, TranslationOffset offset)
            {
                m_TranslationIds.Add(binding);
                m_TranslationOffsets.Add(offset);

                if (offset.Space == RigRemapSpace.LocalToCharacter)
                    LocalToCharacterTranslationCount++;
            }

            /// <summary>
            /// Adds a rotation offset override for a given binding.
            /// </summary>
            /// <param name="binding"><see cref="StringHash"/> binding</param>
            /// <param name="offset">Offset</param>
            public void AddRotationOffsetOverride(StringHash binding, RotationOffset offset)
            {
                m_RotationIds.Add(binding);
                m_RotationOffsets.Add(offset);

                if (offset.Space == RigRemapSpace.LocalToCharacter)
                    LocalToCharacterRotationCount++;
            }

            /// <summary>
            ///  Dispose pattern implementation
            /// </summary>
            public void Dispose()
            {
                m_TranslationIds.Dispose();
                m_RotationIds.Dispose();
                m_TranslationOffsets.Dispose();
                m_RotationOffsets.Dispose();
            }
        }

        /// <summary>
        /// Given source and destination BlobAssetReference{RigDefinition}, this function creates a remap table based on matching bindings.
        /// </summary>
        /// <param name="srcRig">The source BlobAssetReference{RigDefinition} to remap from.</param>
        /// <param name="dstRig">The destination BlobAssetReference{RigDefinition} to remap to.</param>
        /// <param name="allocator">The type of memory to allocate. Unless the asset has a very short life span, use <see cref="Allocator.Persistent"/>.</param>
        /// <param name="filter">Optional parameter to filter matches based on channel type. By default, all types of channels are used for matching.</param>
        /// <param name="offsetOverrides">Optional parameter to specify which translation or rotation channels should be matched with offsets in a given space (LocalToParent vs. LocalToCharacter). By default, LocalToParent mapping is performed.</param>
        /// <param name="matchBindingsFunc">The functor used to match bindings from the source BlobAssetReference{RigDefinition} destination BlobAssetReference{RigDefinition}.</param>
        /// <returns>Returns the BlobAssetReference of the RigRemapTable.</returns>
        public static BlobAssetReference<RigRemapTable> Create(
            BlobAssetReference<RigDefinition> srcRig,
            BlobAssetReference<RigDefinition> dstRig,
            Allocator allocator,
            ChannelFilter filter = ChannelFilter.All,
            OffsetOverrides offsetOverrides = default,
            Func<StringHash, StringHash, bool> matchBindingsFunc = null
        )
        {
            Core.ValidateArgumentIsCreated(srcRig);
            Core.ValidateArgumentIsCreated(dstRig);

            if (matchBindingsFunc == null)
            {
                matchBindingsFunc = DefaultMatchBindings;
            }

            ref var srcBindings = ref srcRig.Value.Bindings;
            ref var dstBindings = ref dstRig.Value.Bindings;

            using var translationMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var rotationMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var scaleMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var floatMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var intMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var entityIdMatches = new NativeList<RemapEntry>(Allocator.Temp);
            using var socketMatches = new NativeList<RemapEntry>(Allocator.Temp);

            bool hasTranslationChannels = (filter & ChannelFilter.Translation) != ChannelFilter.None;
            bool hasRotationChannels = (filter & ChannelFilter.Rotation) != ChannelFilter.None;
            bool hasSocketChannels = (filter & ChannelFilter.Socket) != ChannelFilter.None;

            if (hasTranslationChannels)
                GatherMatchingBindings(translationMatches, ref srcBindings.TranslationBindings, ref dstBindings.TranslationBindings, matchBindingsFunc);
            if (hasRotationChannels)
                GatherMatchingBindings(rotationMatches, ref srcBindings.RotationBindings, ref dstBindings.RotationBindings, matchBindingsFunc);
            if ((filter & ChannelFilter.Scale) != ChannelFilter.None)
                GatherMatchingBindings(scaleMatches, ref srcBindings.ScaleBindings, ref dstBindings.ScaleBindings, matchBindingsFunc);
            if ((filter & ChannelFilter.Float) != ChannelFilter.None)
                GatherMatchingBindings(floatMatches, ref srcBindings.FloatBindings, ref dstBindings.FloatBindings, matchBindingsFunc);
            if ((filter & ChannelFilter.Int) != ChannelFilter.None)
                GatherMatchingBindings(intMatches, ref srcBindings.IntBindings, ref dstBindings.IntBindings, matchBindingsFunc);
            if ((filter & ChannelFilter.EntityId) != ChannelFilter.None)
                GatherMatchingBindings(entityIdMatches, ref srcBindings.EntityIdBindings, ref dstBindings.EntityIdBindings, matchBindingsFunc);
            if (hasSocketChannels)
                GatherMatchingBindings(socketMatches, ref srcRig.Value.SocketValues, ref dstRig.Value.SocketValues);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var rigRemapTable = ref blobBuilder.ConstructRoot<RigRemapTable>();

            int localToCharacterTranslationCount = 0;
            int localToCharacterRotationCount = 0;
            if (offsetOverrides.IsCreated)
            {
                if (hasTranslationChannels && offsetOverrides.HasTranslationOffsetOverrides)
                {
                    UpdateBindingOffsets(ref dstBindings.TranslationBindings, offsetOverrides.m_TranslationIds, translationMatches);
                    localToCharacterTranslationCount = offsetOverrides.LocalToCharacterTranslationCount;
                }
                else
                    localToCharacterTranslationCount = 0;

                if (hasRotationChannels && offsetOverrides.HasRotationOffsetOverrides)
                {
                    UpdateBindingOffsets(ref dstBindings.RotationBindings, offsetOverrides.m_RotationIds, rotationMatches);
                    localToCharacterRotationCount = offsetOverrides.LocalToCharacterRotationCount;
                }
                else
                    localToCharacterRotationCount = 0;

                var sortedLocalToCharacterTREntries = new NativeList<int2>(Allocator.Temp);
                ComputeSortedLocalToCharacterTREntries(
                    sortedLocalToCharacterTREntries,
                    translationMatches,
                    rotationMatches,
                    offsetOverrides.m_TranslationOffsets,
                    offsetOverrides.m_RotationOffsets,
                    localToCharacterTranslationCount,
                    localToCharacterRotationCount
                );

                if (hasTranslationChannels)
                    FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_TranslationOffsets, ref rigRemapTable.TranslationOffsets);
                if (hasRotationChannels)
                    FillRigMapperBlobBuffer(ref blobBuilder, offsetOverrides.m_RotationOffsets, ref rigRemapTable.RotationOffsets);

                FillRigMapperBlobBuffer(ref blobBuilder, sortedLocalToCharacterTREntries, ref rigRemapTable.SortedLocalToCharacterTREntries);
                sortedLocalToCharacterTREntries.Dispose();
            }

            FillRigMapperBlobBuffer(ref blobBuilder, translationMatches, ref rigRemapTable.TranslationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, rotationMatches, ref rigRemapTable.RotationMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, scaleMatches, ref rigRemapTable.ScaleMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, floatMatches, ref rigRemapTable.FloatMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, intMatches, ref rigRemapTable.IntMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, entityIdMatches, ref rigRemapTable.EntityIdMappings);
            FillRigMapperBlobBuffer(ref blobBuilder, socketMatches, ref rigRemapTable.SocketMappings);
            rigRemapTable.LocalToParentTRCount = math.int2(
                translationMatches.Length - localToCharacterTranslationCount,
                rotationMatches.Length - localToCharacterRotationCount
            );

            var rigRemapTableAsset = blobBuilder.CreateBlobAssetReference<RigRemapTable>(allocator);

            blobBuilder.Dispose();

            return rigRemapTableAsset;
        }

        static void GatherMatchingBindings(
            NativeList<RemapEntry> matches,
            ref BlobArray<StringHash> srcBindings,
            ref BlobArray<StringHash> dstBindings,
            Func<StringHash, StringHash, bool> matchBindingsFunc)
        {
            if (srcBindings.Length == 0 || dstBindings.Length == 0)
                return;

            matches.Capacity = math.max(srcBindings.Length, dstBindings.Length);
            for (int i = 0; i != srcBindings.Length; i++)
            {
                for (var j = 0; j < dstBindings.Length; ++j)
                {
                    if (matchBindingsFunc(srcBindings[i], dstBindings[j]))
                    {
                        matches.AddNoResize(new RemapEntry { SourceIndex = i, DestinationIndex = j, OffsetIndex = -1 });
                        break;
                    }
                }
            }
        }

        static void GatherMatchingBindings(NativeList<RemapEntry> matches, ref BlobArray<SocketNode> srcSockets, ref BlobArray<SocketNode> dstSockets)
        {
            if (srcSockets.Length == 0 || dstSockets.Length == 0)
                return;

            matches.Capacity = math.max(srcSockets.Length, dstSockets.Length);
            for (int i = 0; i != srcSockets.Length; i++)
            {
                var dstIdx = BindingUtility.FindSocketIndex(ref dstSockets, srcSockets[i].Id);
                if (dstIdx != -1)
                {
                    matches.AddNoResize(new RemapEntry { SourceIndex = i, DestinationIndex = dstIdx, OffsetIndex = -1 });
                }
            }
        }

        static void UpdateBindingOffsets(ref BlobArray<StringHash> dstBindings, NativeList<StringHash> bindingOverride, NativeList<RemapEntry> remapEntries)
        {
            for (int i = 0; i < bindingOverride.Length; ++i)
            {
                var dstIdx = BindingUtility.FindBindingIndex(ref dstBindings, bindingOverride[i]);
                if (dstIdx != -1)
                {
                    for (int j = 0; j < remapEntries.Length; ++j)
                    {
                        if (dstIdx == remapEntries[j].DestinationIndex)
                        {
                            var entry = remapEntries[j];
                            entry.OffsetIndex = i + 1;
                            remapEntries[j] = entry;
                            break;
                        }
                    }
                }
            }
        }

        internal static void ComputeSortedLocalToCharacterTREntries(
            NativeList<int2> sortedLocalToCharacterTREntries,
            NativeList<RemapEntry> translationMatches,
            NativeList<RemapEntry> rotationMatches,
            NativeList<TranslationOffset> translationOffsets,
            NativeList<RotationOffset> rotationOffsets,
            int localToCharacterTranslationCount,
            int localToCharacterRotationCount
        )
        {
            Core.ValidateArgumentIsCreated(translationMatches);
            Core.ValidateArgumentIsCreated(rotationMatches);
            Core.ValidateArgumentIsCreated(translationOffsets);
            Core.ValidateArgumentIsCreated(rotationOffsets);

            int totalCount = localToCharacterTranslationCount + localToCharacterRotationCount;
            if (totalCount == 0)
                return;

            sortedLocalToCharacterTREntries.Capacity = totalCount;

            // Sort both Translation/Rotation matches so that LocalToCharacter space mappings are at the end of the lists and by increasing destination index
            if (localToCharacterTranslationCount > 0)
                translationMatches.Sort(new RigRemapTranslationEntryComparer { TranslationOffets = translationOffsets });
            if (localToCharacterRotationCount > 0)
                rotationMatches.Sort(new RigRemapRotationEntryComparer { RotationOffets = rotationOffsets });

            // Compute an ordered list of LocalToWorld entries by sorting T/R operations based on destination index.
            // If both T/R operations target the same destination index then one entry is added. Otherwise,
            // the operation with the smallest destination index is added first.

            int tEntryIdx = translationMatches.Length - localToCharacterTranslationCount;
            int rEntryIdx = rotationMatches.Length - localToCharacterRotationCount;

            while (tEntryIdx != translationMatches.Length || rEntryIdx != rotationMatches.Length)
            {
                // Get destination index for T/R operation, this is set to -1 if we are at the end of the entries for a list
                int tDstIdx = (tEntryIdx == translationMatches.Length) ? -1 : translationMatches[tEntryIdx].DestinationIndex;
                int rDstIdx = (rEntryIdx == rotationMatches.Length) ? -1 : rotationMatches[rEntryIdx].DestinationIndex;

                if (tDstIdx != -1 && rDstIdx == -1)
                {
                    // We only have a valid translation operation
                    sortedLocalToCharacterTREntries.Add(math.int2(tEntryIdx, -1));
                }
                else if (tDstIdx == -1 && rDstIdx != -1)
                {
                    // We only have a valid rotation operation
                    sortedLocalToCharacterTREntries.Add(math.int2(-1, rEntryIdx));
                }
                else
                {
                    // If both T/R are valid, chose which operations should come first when
                    // destination indices don't match.
                    if (tDstIdx == rDstIdx)
                        sortedLocalToCharacterTREntries.Add(math.int2(tEntryIdx, rEntryIdx));
                    else if (tDstIdx < rDstIdx)
                    {
                        sortedLocalToCharacterTREntries.Add(math.int2(tEntryIdx, -1));
                        sortedLocalToCharacterTREntries.Add(math.int2(-1, rEntryIdx));
                    }
                    else
                    {
                        sortedLocalToCharacterTREntries.Add(math.int2(-1, rEntryIdx));
                        sortedLocalToCharacterTREntries.Add(math.int2(tEntryIdx, -1));
                    }
                }

                if (tEntryIdx != translationMatches.Length)
                    tEntryIdx++;
                if (rEntryIdx != rotationMatches.Length)
                    rEntryIdx++;
            }
        }


        /// <summary>
        /// Remaps a transform channel index associated to the RigRemapTable source rig definition
        /// to a transform channel index associated to the RigRemapTable target rig definition.
        /// </summary>
        /// <param name="remapTable">RigRemapTable blob asset reference.</param>
        /// <param name="sourceIndex">Source index.</param>
        /// <returns>Returns the remapped index or -1 if source index is not found in RigRemapTable.</returns>
        public static int RemapTransformChannel(BlobAssetReference<RigRemapTable> remapTable, int sourceIndex)
        {
            int destinationIndex = RemapChannelIndex(sourceIndex, ref remapTable.Value.TranslationMappings);
            if (destinationIndex == -1)
                destinationIndex = RemapChannelIndex(sourceIndex, ref remapTable.Value.RotationMappings);
            if (destinationIndex == -1)
                destinationIndex = RemapChannelIndex(sourceIndex, ref remapTable.Value.ScaleMappings);

            return destinationIndex;
        }

        /// <summary>
        /// Remaps a socket channel index associated to the RigRemapTable source rig definition
        /// to a socket channel index associated to the RigRemapTable target rig definition.
        /// </summary>
        /// <param name="remapTable">RigRemapTable blob asset reference.</param>
        /// <param name="sourceIndex">Source index.</param>
        /// <returns>Returns the remapped index or -1 if source index is not found in RigRemapTable.</returns>
        public static int RemapSocketChannel(BlobAssetReference<RigRemapTable> remapTable, int sourceIndex) =>
            RemapChannelIndex(sourceIndex, ref remapTable.Value.SocketMappings);

        /// <summary>
        /// Remaps a float channel index associated to the RigRemapTable source rig definition
        /// to a float channel index associated to the RigRemapTable target rig definition.
        /// </summary>
        /// <param name="remapTable">RigRemapTable blob asset reference.</param>
        /// <param name="sourceIndex">Source index.</param>
        /// <returns>Returns the remapped index or -1 if source index is not found in RigRemapTable.</returns>
        public static int RemapFloatChannel(BlobAssetReference<RigRemapTable> remapTable, int sourceIndex) =>
            RemapChannelIndex(sourceIndex, ref remapTable.Value.FloatMappings);

        /// <summary>
        /// Remaps an integer channel index associated to the RigRemapTable source rig definition
        /// to an integer channel index associated to the RigRemapTable target rig definition.
        /// </summary>
        /// <param name="remapTable">RigRemapTable blob asset reference.</param>
        /// <param name="sourceIndex">Source index.</param>
        /// <returns>Returns the remapped index or -1 if source index is not found in RigRemapTable.</returns>
        public static int RemapIntChannel(BlobAssetReference<RigRemapTable> remapTable, int sourceIndex) =>
            RemapChannelIndex(sourceIndex, ref remapTable.Value.IntMappings);

        /// <summary>
        /// Remaps an entity ID channel index associated to the RigRemapTable source rig definition
        /// to an entity ID channel index associated to the RigRemapTable target rig definition.
        /// </summary>
        /// <param name="remapTable">RigRemapTable blob asset reference.</param>
        /// <param name="sourceIndex">Source index.</param>
        /// <returns>Returns the remapped index or -1 if source index is not found in RigRemapTable.</returns>
        public static int RemapEntityIdChannel(BlobAssetReference<RigRemapTable> remapTable, int sourceIndex) =>
            RemapChannelIndex(sourceIndex, ref remapTable.Value.EntityIdMappings);

        static int RemapChannelIndex(int sourceIndex, ref BlobArray<RemapEntry> mappings)
        {
            for (int i = 0; i < mappings.Length; ++i)
            {
                if (mappings[i].SourceIndex == sourceIndex)
                    return mappings[i].DestinationIndex;
            }

            return -1;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal static unsafe void FillRigMapperBlobBuffer<T>(ref BlobBuilder blobBuilder, NativeList<T> data, ref BlobArray<T> blobBuffer)
            where T : unmanaged
        {
            if (!data.IsCreated || data.Length == 0)
                return;

            var builderArray = blobBuilder.Allocate(ref blobBuffer, data.Length);
            UnsafeUtility.MemCpy(
                builderArray.GetUnsafePtr(),
                data.GetUnsafeReadOnlyPtr(),
                UnsafeUtility.SizeOf<T>() * data.Length
            );
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal static unsafe void FillRigMapperBlobBuffer<T>(ref BlobBuilder blobBuilder, ref BlobArray<T> data, ref BlobArray<T> blobBuffer)
            where T : unmanaged
        {
            if (data.Length == 0)
                return;

            var builderArray = blobBuilder.Allocate(ref blobBuffer, data.Length);
            UnsafeUtility.MemCpy(
                builderArray.GetUnsafePtr(),
                data.GetUnsafePtr(),
                UnsafeUtility.SizeOf<T>() * data.Length
            );
        }
    }

    internal partial struct DualRigRemapTable
    {
        /// <summary>
        /// Given an input and output BlobAssetReference{RigRemapTable}, this function creates a dual rig remap table
        /// with remaps table that can remap from a source to a destination and vice versa.
        /// </summary>
        /// <param name="inputRemapTable">Input Remap Table. Remaps a destination rig to a source rig</param>
        /// <param name="outputRemapTable">Output Remap Table. Remaps a source rig to a destination rig</param>
        /// <param name="allocator">Allocator for DualRigRemableTable BlobAssetReference. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns the BlobAssetReference of the DualRigRemapTable.</returns>
        internal static BlobAssetReference<DualRigRemapTable> Create(
            BlobAssetReference<RigRemapTable> inputRemapTable,
            BlobAssetReference<RigRemapTable> outputRemapTable,
            Allocator allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var dualRigRemapTable = ref blobBuilder.ConstructRoot<DualRigRemapTable>();

            CopyRemapTable(ref inputRemapTable.Value, ref dualRigRemapTable.InputRemapTable, ref blobBuilder);
            CopyRemapTable(ref outputRemapTable.Value, ref dualRigRemapTable.OutputRemapTable, ref blobBuilder);

            var blobAssetReference = blobBuilder.CreateBlobAssetReference<DualRigRemapTable>(allocator);
            blobBuilder.Dispose();

            return blobAssetReference;
        }

        static void CopyRemapTable(ref RigRemapTable sourceRemapTable, ref RigRemapTable targetRemapTable,
            ref BlobBuilder blobBuilder)
        {
            blobBuilder.CopyBlobArray(ref sourceRemapTable.TranslationMappings, ref targetRemapTable.TranslationMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.RotationMappings, ref targetRemapTable.RotationMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.ScaleMappings, ref targetRemapTable.ScaleMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.FloatMappings, ref targetRemapTable.FloatMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.IntMappings, ref targetRemapTable.IntMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.EntityIdMappings, ref targetRemapTable.EntityIdMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.SocketMappings, ref targetRemapTable.SocketMappings);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.SortedLocalToCharacterTREntries, ref targetRemapTable.SortedLocalToCharacterTREntries);
            targetRemapTable.LocalToParentTRCount = sourceRemapTable.LocalToParentTRCount;
            blobBuilder.CopyBlobArray(ref sourceRemapTable.TranslationOffsets, ref targetRemapTable.TranslationOffsets);
            blobBuilder.CopyBlobArray(ref sourceRemapTable.RotationOffsets, ref targetRemapTable.RotationOffsets);
        }
    }
}
