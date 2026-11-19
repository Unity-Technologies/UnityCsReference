// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animations
{
    /// <summary>
    /// ChannelMask is a helpe used by <see cref="AnimationStream.FrameMask"/> and <see cref="AnimationStream.PassMask"/> to inspect which channels have been animated
    /// in a given animation stream.
    /// </summary>
    /// <remarks>This struct references a memory block in the ECS component store, it's life cycle is limited to a local scope.</remarks>
    public readonly ref struct ChannelMask
    {
        internal readonly UnsafeBitArray.ReadOnly  m_Masks;
        readonly BlobAssetReference<RigDefinition> m_Rig;

        internal ChannelMask(UnsafeBitArray.ReadOnly masks, BlobAssetReference<RigDefinition> rigDefinition)
        {
            m_Masks = masks;
            m_Rig = rigDefinition;
        }

        /// <summary>
        /// Returns number of translation channels.
        /// </summary>
        public readonly int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.TranslationBindings.Length;
        }

        /// <summary>
        /// Returns number of rotation channels.
        /// </summary>
        public readonly int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.RotationBindings.Length;
        }

        /// <summary>
        /// Returns number of scale channels.
        /// </summary>
        public readonly int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.ScaleBindings.Length;
        }

        /// <summary>
        /// Returns number of float channels.
        /// </summary>
        public readonly int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.FloatBindings.Length;
        }

        /// <summary>
        /// Returns number of int channels.
        /// </summary>
        public readonly int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.IntBindings.Length;
        }

        /// <summary>
        /// Returns number of entity ID channels.
        /// </summary>
        public readonly int EntityIdCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.EntityIdBindings.Length;
        }

        readonly ref RigDefinition Rig
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Rig.Value;
        }

        /// <summary>
        /// Returns the number of channel bits that are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CountChannelBits() => m_Masks.CountBits(0, Rig.Bindings.BindingCount);

        /// <summary>
        /// Returns true if any channel bits are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAny() => m_Masks.TestAny(0, Rig.Bindings.BindingCount);

        /// <summary>
        /// Returns true if all channel bits are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAll() => m_Masks.TestAll(0, Rig.Bindings.BindingCount);

        /// <summary>
        /// Returns true if no channel bits are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasNone() => m_Masks.TestNone(0, Rig.Bindings.BindingCount);

        /// <summary>
        /// Returns true if the channel bit at specified position is set.
        /// </summary>
        /// <param name="index">Channel index in ChannelMask.</param>
        /// <returns>Returns the channel bit state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, Rig.Bindings.BindingCount);
            return m_Masks.IsSet(index);
        }

        /// <summary>
        /// Returns true if the specified translation channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Translation channel index.</param>
        /// <returns>The state of the translation channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsTranslationSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, TranslationCount);
            return m_Masks.IsSet(Rig.Bindings.TranslationBindingIndex + index);
        }

        /// <summary>
        /// Returns true if the specified rotation channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Rotation channel index.</param>
        /// <returns>The state of the rotation channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsRotationSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, RotationCount);
            return m_Masks.IsSet(Rig.Bindings.RotationBindingIndex + index);
        }

        /// <summary>
        /// Returns true if the specified scale channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Scale channel index.</param>
        /// <returns>The state of the scale channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsScaleSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, ScaleCount);
            return m_Masks.IsSet(Rig.Bindings.ScaleBindingIndex + index);
        }

        /// <summary>
        /// Returns true if the specified float channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Float channel index.</param>
        /// <returns>The state of the float channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsFloatSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, FloatCount);
            return m_Masks.IsSet(Rig.Bindings.FloatBindingIndex + index);
        }

        /// <summary>
        /// Returns true if the specified integer channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Int channel index.</param>
        /// <returns>The state of the integer channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsIntSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, IntCount);
            return m_Masks.IsSet(Rig.Bindings.IntBindingIndex + index);
        }

        /// <summary>
        /// Returns true if the specified entity ID channel bit is set, indicating this channel value has been animated.
        /// </summary>
        /// <param name="index">Entity ID channel index.</param>
        /// <returns>The state of the entity ID channel bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsEntityIdSet(int index)
        {
            Core.ValidateBufferIndexBounds(index, EntityIdCount);
            return m_Masks.IsSet(Rig.Bindings.EntityIdBindingIndex + index);
        }
    }
}
