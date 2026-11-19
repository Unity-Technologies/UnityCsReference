// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>Transform space in which position and rotation are read from.</summary>
    /// <remarks>Remapping in LocalToCharacter space has a performance cost, use sparingly.</remarks>
    public enum RigRemapSpace : byte
    {
        /// <summary>LocalToParent space</summary>
        LocalToParent = 0,
        /// <summary>LocalToCharacter space</summary>
        LocalToCharacter   = 1
    }

    /// <summary>
    /// Single blob asset reference containing both input and output remap table
    /// </summary>
    internal partial struct DualRigRemapTable
    {
        /// <summary>
        /// Remap table allowing to remap the stream in input nodes for some animation graphs, from the target rig
        /// to the source rig, coming from the graph/state machine.
        /// </summary>
        public RigRemapTable InputRemapTable;

        /// <summary>
        /// Remap table allowing to remap the output of the graph/state machine to the target rig.
        /// </summary>
        public RigRemapTable OutputRemapTable;
    }

    /// <summary>Runtime Rig Remap Table.</summary>
    public partial struct RigRemapTable
    {
        /// <summary>Entry in RigRemapTable.</summary>
        /// <seealso cref="Animations.RigRemapTable"/>
        public struct RemapEntry
        {
            /// <summary>Zero-based channel index in source rig.</summary>
            public int SourceIndex;
            /// <summary>Zero-based channel index in destination rig.</summary>
            public int DestinationIndex;
            /// <summary>Zero-based offset index to offset.</summary>
            /// <remarks>Index zero is reserved for identity offset and should be used whenever there is no offset required in remap entry.</remarks>
            public int OffsetIndex;
        }

        /// <summary>Offset for translation channel in RigRemapTable.</summary>
        /// <remarks>Offset is applied to the channel value like so: "Rotation * (value * Scale + Translation)"</remarks>
        /// <seealso cref="Animations.RigRemapTable"/>
        public struct TranslationOffset
        {
            /// <summary>Translation offset.</summary>
            public float3 Translation;
            /// <summary>Scale offset.</summary>
            public float Scale;
            /// <summary>Rotation offset.</summary>
            public quaternion Rotation;
            /// <summary>Transform space value is read from.</summary>
            public RigRemapSpace Space;
        }

        /// <summary>Offset for rotation channel in RigRemapTable.</summary>
        /// <remarks>Offset is applied to the channel value like so: "PreRotation * value * PostRotation"</remarks>
        /// <seealso cref="Animations.RigRemapTable"/>
        public struct RotationOffset
        {
            /// <summary>Pre-rotation offset.</summary>
            public quaternion PreRotation;
            /// <summary>Post-rotation offset.</summary>
            public quaternion PostRotation;
            /// <summary>Transform space value is read from.</summary>
            public RigRemapSpace Space;
        }

        /// <summary>Translation rig remap entries.</summary>
        public BlobArray<RemapEntry>        TranslationMappings;
        /// <summary>Rotation rig remap entries.</summary>
        public BlobArray<RemapEntry>        RotationMappings;
        /// <summary>Scale rig remap entries.</summary>
        public BlobArray<RemapEntry>        ScaleMappings;
        /// <summary>Float rig remap entries.</summary>
        public BlobArray<RemapEntry>        FloatMappings;
        /// <summary>Integer rig remap entries.</summary>
        public BlobArray<RemapEntry>        IntMappings;
        /// <summary>Entity ID rig remap entries.</summary>
        public BlobArray<RemapEntry>        EntityIdMappings;
        /// <summary>Socket rig remap entries.</summary>
        public BlobArray<RemapEntry>        SocketMappings;

        /// <summary>Sorted indices of translation and rotation channels that read in LocalToCharacter space.</summary>
        public BlobArray<int2>                 SortedLocalToCharacterTREntries; // x = TranslationMappingIndex, y = RotationMappingIndex
        /// <summary>Number of translation and rotation remap entries that read in LocalToCharacter space.</summary>
        public int2                            LocalToParentTRCount;       // x = TranslationCount, y = RotationCount

        /// <summary>Offsets referenced by translation entries</summary>
        public BlobArray<TranslationOffset> TranslationOffsets;
        /// <summary>Offsets referenced by rotation entries</summary>
        public BlobArray<RotationOffset>    RotationOffsets;
    }
}
