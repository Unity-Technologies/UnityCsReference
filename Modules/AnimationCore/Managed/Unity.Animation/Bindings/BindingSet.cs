// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Animations
{
    internal enum BindingChannel
    {
        Translation,
        Rotation,
        Scale,
        Float,
        Int,
        EntityId
    }

    /// <summary>
    /// Binding layout of a rig, used to find a binding by its identifier and to size the buffers of an animation stream.
    /// </summary>
    /// <remarks>
    /// A rig definition stores its binding set in <see cref="RigDefinition.Bindings"/>, and <see cref="RigBuilder"/> fills
    /// it in when it creates the <see cref="RigDefinition"/>. Read a `BindingSet` to:
    ///
    ///1. Find the index of a binding from its identifier. Pass an array of identifiers, such as
    /// <see cref="TranslationBindings"/>, to <see cref="BindingUtility.FindBindingIndex"/>.
    ///2. Find the bit of a binding in the channel masks of an <see cref="AnimationStream"/>.
    ///3. Find where the data of a binding is stored in the <see cref="AnimatedData"/> buffer of an `AnimationStream`.
    ///4. Allocate the buffer of an `AnimationStream` with <see cref="StreamSize"/> elements, or the buffer of an
    /// <see cref="AnimationStreamMask"/> with <see cref="StreamMaskSize"/> elements.
    ///
    /// A binding connects one animated channel of the rig to a unique identifier. A rig has translation, rotation, scale,
    /// float, integer, and entity ID bindings.
    ///
    /// The bindings of a rig are indexed in one sequence, in this order: translation, scale, float, integer, entity ID,
    /// and rotation. The first-binding index properties, such as
    /// <see cref="RotationBindingIndex"/> and <see cref="IntBindingIndex"/>, give the index of the first binding of each
    /// kind. The channel masks of an `AnimationStream` use one bit for each binding, with this index.
    ///
    /// A curve is one float value of a binding. A translation or scale binding has three curves, a rotation binding has
    /// four, and a float, integer, or entity ID binding has one. The curve count properties, such as
    /// <see cref="ScaleCurveCount"/> and <see cref="FloatCurveCount"/>, give the number of curves of each kind.
    ///
    /// The `AnimatedData` buffer of an `AnimationStream` has the following sections, in this order:
    ///
    ///1. Translation values, stored as one `float3` for each translation binding, starting at
    /// <see cref="TranslationSamplesOffset"/>.
    ///2. Scale values, stored as one `float3` for each scale binding, starting at <see cref="ScaleSamplesOffset"/>.
    ///3. Float values, stored as one `float` for each float binding, starting at <see cref="FloatSamplesOffset"/>. The
    /// translation, scale, and float sections together fill <see cref="InterpolatedDataChunkCount"/> `float4` chunks, so
    /// padding follows the float values.
    ///4. Integer values, stored as one `int` for each integer binding, starting at <see cref="IntSamplesOffset"/>.
    ///5. Entity ID values, stored as one `EntityId` for each entity ID binding, starting at
    /// <see cref="EntityIdSamplesOffset"/>. The integer and entity ID sections together fill
    /// <see cref="DiscreteDataChunkCount"/> `int4` chunks, so padding follows the entity ID values.
    ///6. Rotation values, stored in <see cref="quaternion4"/> chunks that each hold four rotations, starting at
    /// <see cref="RotationSamplesOffset"/>. <see cref="RotationDataChunkCount"/> gives the number of chunks.
    ///7. Channel masks, stored as two bit arrays with the pass mask first and the frame mask second, starting at
    /// <see cref="ChannelMaskOffset"/>. Each mask has one bit for each binding, rounded up to a multiple of 64 bits.
    ///
    /// Offsets and sizes count `AnimatedData` elements, which are 4 bytes each.
    ///
    /// The following example uses the `Unity.Animations` and `Unity.Entities` namespaces. It finds a float channel by
    /// its identifier, then reads and writes its value in an animation stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ReadChannelExample.cs#ReadAndWriteFloatChannel}]]></code>
    /// </example>
    /// <seealso cref="BindingUtility"/>
    /// <seealso cref="RigDefinition"/>
    /// <seealso cref="AnimationStream"/>
    [GenerateTestsForBurstCompatibility]
    public struct BindingSet
    {
        /// <summary>
        /// Identifiers of the translation bindings of the rig. The position of an identifier is the index of its
        /// binding among the translation bindings.
        /// </summary>
        public BlobArray<StringHash> TranslationBindings;
        /// <summary>
        /// Identifiers of the rotation bindings of the rig. The position of an identifier is the index of its
        /// binding among the rotation bindings.
        /// </summary>
        public BlobArray<StringHash> RotationBindings;
        /// <summary>
        /// Identifiers of the scale bindings of the rig. The position of an identifier is the index of its
        /// binding among the scale bindings.
        /// </summary>
        public BlobArray<StringHash> ScaleBindings;
        /// <summary>
        /// Identifiers of the float bindings of the rig. The position of an identifier is the index of its
        /// binding among the float bindings.
        /// </summary>
        public BlobArray<StringHash> FloatBindings;
        /// <summary>
        /// Identifiers of the integer bindings of the rig. The position of an identifier is the index of its
        /// binding among the integer bindings.
        /// </summary>
        public BlobArray<StringHash> IntBindings;
        /// <summary>
        /// Identifiers of the entity ID bindings of the rig. The position of an identifier is the index of its
        /// binding among the entity ID bindings.
        /// </summary>
        public BlobArray<StringHash> EntityIdBindings;


        internal static readonly int k_RotationDataChunkSize = UnsafeUtility.SizeOf<quaternion4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_DiscreteDataChunkSize = UnsafeUtility.SizeOf<int4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_InterpolatedDataChunkSize = UnsafeUtility.SizeOf<float4>() / UnsafeUtility.SizeOf<AnimatedData>();
        internal static readonly int k_WeightDataChunkSize = UnsafeUtility.SizeOf<float4>() / UnsafeUtility.SizeOf<WeightData>();
        internal static readonly int k_GenericEntityIdSize = UnsafeUtility.SizeOf<EntityId>() / UnsafeUtility.SizeOf<AnimatedData>();

        // Key type float count
        /// <summary>Number of curves of one translation binding: x, y, and z.</summary>
        public const int TranslationKeyFloatCount = 3;
        /// <summary>Number of curves of one rotation binding: the quaternion x, y, z, and w.</summary>
        public const int RotationKeyFloatCount = 4;
        /// <summary>Number of curves of one scale binding: x, y, and z.</summary>
        public const int ScaleKeyFloatCount = 3;
        /// <summary>Number of curves of one float binding.</summary>
        public const int FloatKeyFloatCount = 1;
        /// <summary>Number of curves of one integer binding.</summary>
        public const int IntKeyFloatCount = 1;
        /// <summary>Number of curves of one entity ID binding. The curve of a clip stores an object index.</summary>
        public const int EntityIdKeyFloatCount = 1;

        // Binding count
        /// <summary>Index of the first rotation binding among all the bindings of the rig.</summary>
        public int RotationBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Index of the first translation binding among all the bindings of the rig. The value is always 0.</summary>
        public int TranslationBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Index of the first scale binding among all the bindings of the rig.</summary>
        public int ScaleBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Index of the first float binding among all the bindings of the rig.</summary>
        public int FloatBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Index of the first integer binding among all the bindings of the rig.</summary>
        public int IntBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Index of the first entity ID binding among all the bindings of the rig.</summary>
        public int EntityIdBindingIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Total number of bindings of the rig.</summary>
        public int BindingCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        // Curve count
        /// <summary>Number of curves of all the translation bindings, three for each binding.</summary>
        public int TranslationCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of curves of all the rotation bindings, four for each binding.</summary>
        public int RotationCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of curves of all the scale bindings, three for each binding.</summary>
        public int ScaleCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of curves of all the float bindings, one for each binding.</summary>
        public int FloatCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of curves of all the integer bindings, one for each binding.</summary>
        public int IntCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of curves of all the entity ID bindings, one for each binding.</summary>
        public int EntityIdCurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Total number of curves of the rig.</summary>
        public int CurveCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        // Sample offsets
        /// <summary>Offset of the rotation data in an animation stream buffer, in AnimatedData elements.</summary>
        public int RotationSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Offset of the translation data in an animation stream buffer, in AnimatedData elements.</summary>
        public int TranslationSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Offset of the scale data in an animation stream buffer, in AnimatedData elements.</summary>
        public int ScaleSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Offset of the float data in an animation stream buffer, in AnimatedData elements.</summary>
        public int FloatSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Offset of the integer data in an animation stream buffer, in AnimatedData elements.</summary>
        public int IntSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Offset of the entity ID data in an animation stream buffer, in AnimatedData elements.</summary>
        public int EntityIdSamplesOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>
        /// Offset of the channel masks in an animation stream buffer, in AnimatedData elements. The pass mask comes first,
        /// then the frame mask. Each mask has one bit for each binding.
        /// </summary>
        public int ChannelMaskOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>Number of chunks that hold the rotation data. Each chunk holds four rotations.</summary>
        public int RotationDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of int4 chunks that hold the integer and entity ID data.</summary>
        public int DiscreteDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Number of float4 chunks that hold the translation, scale and float data.</summary>
        public int InterpolatedDataChunkCount { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>
        /// Size of the channel data of an animation stream, in AnimatedData elements. The size includes the padding that fills the chunks
        /// and excludes the channel masks.
        /// </summary>
        public int ChannelSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>
        /// Size of the buffer of an animation stream, in AnimatedData elements. The buffer holds the channel data and the
        /// channel masks.
        /// </summary>
        public int StreamSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
        /// <summary>Size of the buffer of an animation stream mask, in WeightData elements.</summary>
        public int StreamMaskSize { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }
    }

    [GenerateTestsForBurstCompatibility]
    static class RigDefinitionExtensions
    {
        public static void SetupBindingSet(this ref RigDefinition rig, int translationCount, int rotationCount, int scaleCount, int floatCount, int intCount, int objectIDCount)
        {
            Core.SetupBindingSet(ref rig.Bindings, translationCount, rotationCount, scaleCount, floatCount, intCount, objectIDCount);

            var dataCurveCount = rig.Bindings.TranslationCurveCount + rig.Bindings.ScaleCurveCount + rig.Bindings.FloatCurveCount;
            rig.Bindings.InterpolatedDataChunkCount = (int)math.ceil(dataCurveCount / (float)BindingSet.k_InterpolatedDataChunkSize);
            rig.Bindings.DiscreteDataChunkCount = (int)math.ceil((rig.Bindings.IntCurveCount + rig.Bindings.EntityIdCurveCount * BindingSet.k_GenericEntityIdSize) / (float)BindingSet.k_DiscreteDataChunkSize);
            rig.Bindings.RotationDataChunkCount = (int)math.ceil(rig.Bindings.RotationCurveCount / (float)BindingSet.k_RotationDataChunkSize);

            rig.Bindings.ChannelSize =
                rig.Bindings.InterpolatedDataChunkCount * BindingSet.k_InterpolatedDataChunkSize +
                rig.Bindings.DiscreteDataChunkCount * BindingSet.k_DiscreteDataChunkSize +
                rig.Bindings.RotationDataChunkCount * BindingSet.k_RotationDataChunkSize;

            rig.Bindings.IntSamplesOffset = rig.Bindings.TranslationSamplesOffset + rig.Bindings.InterpolatedDataChunkCount * BindingSet.k_InterpolatedDataChunkSize;
            rig.Bindings.EntityIdSamplesOffset = rig.Bindings.IntSamplesOffset + rig.Bindings.IntCurveCount;
            rig.Bindings.RotationSamplesOffset = rig.Bindings.IntSamplesOffset + rig.Bindings.DiscreteDataChunkCount * BindingSet.k_DiscreteDataChunkSize;
            rig.Bindings.ChannelMaskOffset = rig.Bindings.RotationSamplesOffset + rig.Bindings.RotationDataChunkCount * BindingSet.k_RotationDataChunkSize;

            // Mask size must be multiple of 8-bytes. see UnsafeBitArray
            // StreamSize is the number of 4-bytes needed
            const int bitsInBytes = 8;
            const int bitsIn8Bytes = 8 * bitsInBytes;
            const int bitsIn4Bytes = 4 * bitsInBytes;
            var maskSizeIn4Bytes = Core.AlignUp(rig.Bindings.BindingCount, bitsIn8Bytes) / bitsIn4Bytes;
            rig.Bindings.StreamSize = rig.Bindings.ChannelSize + (maskSizeIn4Bytes * 2);

            // Similar to the stream size and it's data chunks we make sure to align the stream mask on float4 chunks.
            // This gives the opportunity to do packed instructions when doing full operations on the weight mask. The weight mask data is also aligned
            // to the layout of the AnimationStream in order to do optimized blending operations with masks.
            rig.Bindings.StreamMaskSize = (rig.Bindings.InterpolatedDataChunkCount + rig.Bindings.DiscreteDataChunkCount + rig.Bindings.RotationDataChunkCount) * BindingSet.k_WeightDataChunkSize;
            Core.ValidateIsMultipleOf4(rig.Bindings.StreamMaskSize);
        }
    }

    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        internal static void SetupBindingSet(ref BindingSet dataLayout, int translationCount, int rotationCount, int scaleCount, int floatCount, int intCount, int objectIDCount)
        {
            dataLayout.TranslationCurveCount = translationCount * BindingSet.TranslationKeyFloatCount;
            dataLayout.RotationCurveCount = rotationCount * BindingSet.RotationKeyFloatCount;
            dataLayout.ScaleCurveCount = scaleCount * BindingSet.ScaleKeyFloatCount;
            dataLayout.FloatCurveCount = floatCount * BindingSet.FloatKeyFloatCount;
            dataLayout.IntCurveCount = intCount * BindingSet.IntKeyFloatCount;
            dataLayout.EntityIdCurveCount = objectIDCount * BindingSet.EntityIdKeyFloatCount;

            dataLayout.CurveCount =
                dataLayout.TranslationCurveCount +
                dataLayout.RotationCurveCount +
                dataLayout.ScaleCurveCount +
                dataLayout.FloatCurveCount +
                dataLayout.IntCurveCount +
                dataLayout.EntityIdCurveCount;
            dataLayout.RotationDataChunkCount = 0;
            dataLayout.DiscreteDataChunkCount = 0;
            dataLayout.InterpolatedDataChunkCount = 0;

            dataLayout.ChannelSize = dataLayout.CurveCount;
            dataLayout.StreamSize  = dataLayout.CurveCount;

            dataLayout.TranslationSamplesOffset = 0;
            dataLayout.ScaleSamplesOffset = dataLayout.TranslationSamplesOffset + dataLayout.TranslationCurveCount;
            dataLayout.FloatSamplesOffset = dataLayout.ScaleSamplesOffset + dataLayout.ScaleCurveCount;
            dataLayout.IntSamplesOffset = dataLayout.FloatSamplesOffset + dataLayout.FloatCurveCount;
            dataLayout.EntityIdSamplesOffset = dataLayout.IntSamplesOffset + dataLayout.IntCurveCount;
            dataLayout.RotationSamplesOffset = dataLayout.EntityIdSamplesOffset + dataLayout.EntityIdCurveCount;
            dataLayout.ChannelMaskOffset = 0;

            dataLayout.TranslationBindingIndex = 0;
            dataLayout.ScaleBindingIndex = dataLayout.TranslationBindingIndex + translationCount;
            dataLayout.FloatBindingIndex = dataLayout.ScaleBindingIndex + scaleCount;
            dataLayout.IntBindingIndex = dataLayout.FloatBindingIndex + floatCount;
            dataLayout.EntityIdBindingIndex = dataLayout.IntBindingIndex + intCount;
            dataLayout.RotationBindingIndex = dataLayout.EntityIdBindingIndex + objectIDCount;
            dataLayout.BindingCount = translationCount + rotationCount + scaleCount + floatCount + intCount + objectIDCount;
        }
    }
}
