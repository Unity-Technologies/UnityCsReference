// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animations
{
    /// <summary>
    /// Blob asset produced from the animation set bindings resolved by mecanim's
    /// <c>CreateAnimationSetBindings()</c>. For each entry received from mecanim, it stores the
    /// mapping between:
    ///   - <see cref="MappedBinding.ClipIndex"/>: the per-type slot the binding occupies in the
    ///     mecanim <c>ValueArray</c> that the playable graph fills at evaluation time (this is
    ///     <c>BoundIndex.index</c>, not the flat position in the AnimationSetBindings arrays).
    ///     For example, entries under <see cref="TranslationBindingMap"/> use
    ///     <c>ValueArray.m_PositionValues[ClipIndex]</c>.
    ///   - <see cref="MappedBinding.RigIndex"/>: the position of the binding in the corresponding
    ///     type-specific <see cref="BindingSet"/> array of the <see cref="RigDefinition"/>
    ///     (TranslationBindings, RotationBindings, ScaleBindings, FloatBindings, IntBindings,
    ///     EntityIdBindings).
    /// The layout intentionally mirrors `ClipInstance` so downstream code that already
    /// consumes ClipInstance's per-type binding spans can be reused with minimal changes.
    /// </summary>
    struct PlayableBindings
    {
        public readonly struct MappedBinding
        {
            public readonly int ClipIndex;
            public readonly int RigIndex;

            internal MappedBinding(int clip, int rig)
            {
                ClipIndex = clip;
                RigIndex = rig;
            }
        }

        internal struct BindingInfo
        {
            public int ScaleOffset;
            public int FloatOffset;
            public int IntOffset;
            public int EntityIdOffset;
            public int RotationOffset;
        }

        internal BlobArray<MappedBinding> m_BindingMap;
        internal BindingInfo              m_BindingInfo;

        public NativeArray<MappedBinding>.ReadOnly TranslationBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>(m_BindingMap.GetUnsafePtr(), m_BindingInfo.ScaleOffset, Allocator.None, true).AsReadOnly(); } }
        }

        public NativeArray<MappedBinding>.ReadOnly ScaleBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.ScaleOffset, m_BindingInfo.FloatOffset - m_BindingInfo.ScaleOffset, Allocator.None, true).AsReadOnly(); } }
        }

        public NativeArray<MappedBinding>.ReadOnly FloatBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.FloatOffset, m_BindingInfo.IntOffset - m_BindingInfo.FloatOffset, Allocator.None, true).AsReadOnly(); } }
        }

        public NativeArray<MappedBinding>.ReadOnly IntBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.IntOffset, m_BindingInfo.EntityIdOffset - m_BindingInfo.IntOffset, Allocator.None, true).AsReadOnly(); } }
        }

        public NativeArray<MappedBinding>.ReadOnly EntityIdBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.EntityIdOffset, m_BindingInfo.RotationOffset - m_BindingInfo.EntityIdOffset, Allocator.None, true).AsReadOnly(); } }
        }

        public NativeArray<MappedBinding>.ReadOnly RotationBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.RotationOffset, m_BindingMap.Length - m_BindingInfo.RotationOffset, Allocator.None, true).AsReadOnly(); } }
        }
    }
}
