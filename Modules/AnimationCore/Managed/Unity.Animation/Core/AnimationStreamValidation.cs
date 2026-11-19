// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;

using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    public partial struct AnimationStream
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateIsNotNull()
        {
            if (!IsCreated)
                throw new System.NullReferenceException("AnimationStream is invalid, it contains a null rig definition.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateIsWritable()
        {
            if (IsReadOnly)
                throw new System.InvalidOperationException("AnimationStream is read only.");

            CheckWrite();
        }

        // Not called by ValidateIsNotNull, because setters call it too and a stream can be write-only in a job
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void CheckRead()
        {
            ValidateIsNotNull();
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void CheckWrite()
        {
            ValidateIsNotNull();
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateNotAlreadyDisposed()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForSkeleton(int index)
        {
            if ((uint)index >= BoneCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Skeleton index '{index}' is out of range of '{BoneCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForTranslation(int index)
        {
            if ((uint)index >= TranslationCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Translation index '{index}' is out of range of '{TranslationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForRotation(int index)
        {
            if ((uint)index >= RotationCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Rotation index '{index}' is out of range of '{RotationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForScale(int index)
        {
            if ((uint)index >= ScaleCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Scale index '{index}' is out of range of '{ScaleCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForFloat(int index)
        {
            if ((uint)index >= FloatCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Float index '{index}' is out of range of '{FloatCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForInt(int index)
        {
            if ((uint)index >= IntCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Int index '{index}' is out of range of '{IntCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForSockets(int index)
        {
            if ((uint)index >= SocketCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Socket index '{index}' is out of range of '{SocketCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForEntityId(int index)
        {
            if ((uint)index >= EntityIdCount)
                throw new System.IndexOutOfRangeException($"AnimationStream: Int index '{index}' is out of range of '{EntityIdCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new System.NotFiniteNumberException($"AnimationStream: float is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsFinite(in float3 value)
        {
            if (!math.all(math.isfinite(value)))
                throw new System.NotFiniteNumberException($"AnimationStream: float3 is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsFinite(in quaternion value)
        {
            if (!math.all(math.isfinite(value.value)))
                throw new System.NotFiniteNumberException($"AnimationStream: quaternion is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateRigEquality(in AnimationStream other)
        {
            other.ValidateIsNotNull();
            other.CheckRead();
            if (RigId != other.RigId)
                throw new System.InvalidOperationException("AnimationStream rigs do not match. The streams must be created from the same RigDefinition.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateCompatibility(in AnimationStream other)
        {
            other.ValidateIsNotNull();
            other.CheckRead();

            // Validate data layout compatibility
            if (TranslationCount != other.TranslationCount ||
                RotationCount != other.RotationCount ||
                ScaleCount != other.ScaleCount ||
                FloatCount != other.FloatCount ||
                IntCount != other.IntCount ||
                EntityIdCount != other.EntityIdCount ||
                Rig.Bindings.StreamSize != other.Rig.Bindings.StreamSize)
                throw new System.InvalidOperationException("AnimationStream data layouts are not compatible.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateCompatibility(BlobAssetReference<ClipInstance> clipInstance)
        {
            if (RigId != clipInstance.Value.RigId)
                throw new System.InvalidOperationException("The ClipSampler was created for another RigDefinition than the one of this AnimationStream.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateCompatibility(in AnimationStreamMask mask)
        {
            mask.ValidateIsNotNull();
            if (RigId != mask.RigId)
                throw new System.InvalidOperationException("AnimationStreamMask was created for another RigDefinition than the one of this AnimationStream.");
        }

        // Address of the RigDefinition blob, which identifies the rig
        internal readonly unsafe ulong RigId => (ulong)m_Rig.GetUnsafePtr();
    }
}
