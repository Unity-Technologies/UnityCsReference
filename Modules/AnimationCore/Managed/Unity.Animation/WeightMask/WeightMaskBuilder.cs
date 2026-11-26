// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


namespace Unity.Animations
{
    /// <summary>
    /// Helper to create a <see cref="WeightMask"/> blob asset reference.
    /// </summary>
    [GenerateTestsForBurstCompatibility, BurstCompile]
    public partial struct WeightMaskBuilder : IDisposable
    {

        const int k_DefaultCapacity = 16;
        const int k_BitsPerField = 32;
        const int k_BitShiftPerField = 5;

        NativeHashMap<StringHash, TransformWeight> m_Transforms;
        NativeHashMap<StringHash, float> m_Translations;
        NativeHashMap<StringHash, float> m_Rotations;
        NativeHashMap<StringHash, float> m_Scales;
        NativeHashMap<StringHash, float> m_Floats;
        NativeHashMap<StringHash, float> m_Ints;
        NativeHashMap<StringHash, float> m_EntityIds;

        float m_DefaultTransformWeight;
        float m_DefaultGenericWeight;
        [MarshalAs(UnmanagedType.U1)]
        bool m_IsCreated;

        /// <summary>
        /// Default weight value to use when the weight mask asset does not contain a specific transform binding part of a rig definition.
        /// </summary>
        public float DefaultTransformWeight
        {
            get => m_DefaultTransformWeight;
            set => m_DefaultTransformWeight = math.saturate(value);
        }
        /// <summary>
        /// Default weight value to use when the weight mask asset does not contain a specific generic binding part of rig definition.
        /// </summary>
        public float DefaultGenericWeight
        {
            get => m_DefaultGenericWeight;
            set => m_DefaultGenericWeight = math.saturate(value);
        }

        /// <summary>
        /// Returns true if the WeightMaskBuilder has been properly created.
        /// </summary>
        public bool IsCreated => m_IsCreated;

        /// <summary>
        /// WeightMaskBuilder constructor.
        /// </summary>
        /// <param name="allocator">The memory allocator to use.</param>
        /// <param name="defaultTransformWeight">Default transform weight value to use when the weight mask asset does not contain a specific binding part of a rig definition.</param>
        /// <param name="defaultGenericWeight">Default generic weight value to use when the weight mask asset does not contain a specific binding part of rig definition.</param>
        public WeightMaskBuilder(Allocator allocator, float defaultTransformWeight = 0f, float defaultGenericWeight = 1f)
        {
            m_Transforms = new NativeHashMap<StringHash, TransformWeight>(k_DefaultCapacity, allocator);
            m_Translations = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);
            m_Rotations = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);
            m_Scales = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);
            m_Floats = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);
            m_Ints = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);
            m_EntityIds = new NativeHashMap<StringHash, float>(k_DefaultCapacity, allocator);

            m_DefaultTransformWeight = math.saturate(defaultTransformWeight);
            m_DefaultGenericWeight = math.saturate(defaultGenericWeight);

            m_IsCreated = true;
        }

        /// <summary>
        /// Disposes the WeightMaskBuilder.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;

            m_Transforms.Dispose();
            m_Translations.Dispose();
            m_Rotations.Dispose();
            m_Scales.Dispose();
            m_Floats.Dispose();
            m_Ints.Dispose();
            m_EntityIds.Dispose();
            m_IsCreated = false;
        }

        /// <summary>
        /// Clear all WeightMaskBuilder data.
        /// </summary>
        public void Clear()
        {
            if (!IsCreated)
                return;

            m_Transforms.Clear();
            m_Translations.Clear();
            m_Rotations.Clear();
            m_Scales.Clear();
            m_Floats.Clear();
            m_Ints.Clear();
            m_EntityIds.Clear();
        }

        /// <summary>
        /// Sets the weight mask and propagation flag of a transform. A transform mask will apply the same
        /// masking value to all associated translation, rotation and scale channels. When the propageWeight parameter is true,
        /// it will also apply the same weight mask value to all children of this transform.
        /// </summary>
        /// <remarks>
        /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`,
        /// `Unity.IntegerTime`, `Unity.Mathematics`, and `UnityEngine` namespaces. It is a `MonoBehaviour` that
        /// propagates the weight of the spine to the bones below it to mask the upper body or the lower body of a
        /// character. It keeps the weight mask and its instance for the rig of the character, and plays one clip on
        /// the masked bones over another clip.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BodyMaskExample.cs#MaskUpperOrLowerBody}]]></code>
        /// </example>
        /// <param name="id">Transform channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        /// <param name="propagateWeight">When true, all children of this transform will also have the same weight mask value.</param>
        public void SetTransformMask(StringHash id, float weight, bool propagateWeight = false)
        {
            ValidateIsCreated();
            m_Transforms[id] = new TransformWeight { Value = weight, IsPropagated = propagateWeight };
        }

        /// <summary>
        /// Sets the weight mask of a translation channel.
        /// </summary>
        /// <param name="id">Translation channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetTranslationMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_Translations[id] = weight;
        }

        /// <summary>
        /// Sets the weight mask of a rotation channel.
        /// </summary>
        /// <param name="id">Translation channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetRotationMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_Rotations[id] = weight;
        }

        /// <summary>
        /// Sets the weight mask of a scale channel.
        /// </summary>
        /// <param name="id">Translation channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetScaleMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_Scales[id] = weight;
        }

        /// <summary>
        /// Sets the weight mask of a float channel.
        /// </summary>
        /// <param name="id">Float channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetFloatMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_Floats[id] = weight;
        }

        /// <summary>
        /// Sets the weight mask an int channel.
        /// </summary>
        /// <param name="id">Int channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetIntMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_Ints[id] = weight;
        }

        /// <summary>
        /// Sets the weight mask of an object channel.
        /// </summary>
        /// <param name="id">Object channel ID.</param>
        /// <param name="weight">Value of the channel weight.</param>
        public void SetEntityIdMask(StringHash id, float weight)
        {
            ValidateIsCreated();
            m_EntityIds[id] = weight;
        }

        /// <summary>
        /// Creates a WeightMask blob asset reference from the WeightMaskBuilder data.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The BlobAssetReference of a WeightMask.</returns>
        public BlobAssetReference<WeightMask> ToWeightMask(Allocator allocator)
        {
            ValidateIsCreated();

            BlobAssetReference<WeightMask> weightMaskAsset = default;
            CreateWeightMask(ref this, ref weightMaskAsset, (int)allocator);
            return weightMaskAsset;
        }

        [BurstCompile]
        static void CreateWeightMask(ref WeightMaskBuilder weightMaskBuilder, ref BlobAssetReference<WeightMask> result, int allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            try
            {
                ref var mask = ref blobBuilder.ConstructRoot<WeightMask>();

                FillTransformData(ref blobBuilder, ref mask, weightMaskBuilder.m_Transforms.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.TranslationBindings, ref mask.TranslationWeights, weightMaskBuilder.m_Translations.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.RotationBindings, ref mask.RotationWeights, weightMaskBuilder.m_Rotations.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.ScaleBindings, ref mask.ScaleWeights, weightMaskBuilder.m_Scales.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.FloatBindings, ref mask.FloatWeights, weightMaskBuilder.m_Floats.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.IntBindings, ref mask.IntWeights, weightMaskBuilder.m_Ints.AsReadOnly());
                FillBindingAndWeightData(ref blobBuilder, ref mask.EntityIdBindings, ref mask.EntityIdWeights, weightMaskBuilder.m_EntityIds.AsReadOnly());

                mask.DefaultTransformChannelWeight = math.saturate(weightMaskBuilder.DefaultTransformWeight);
                mask.DefaultGenericChannelWeight = math.saturate(weightMaskBuilder.DefaultGenericWeight);

                result = blobBuilder.CreateBlobAssetReference<WeightMask>((Allocator)allocator);
            }
            finally
            {
                blobBuilder.Dispose();
            }
        }

        static void FillTransformData(ref BlobBuilder builder, ref WeightMask mask, in NativeHashMap<StringHash, TransformWeight>.ReadOnly data)
        {
            var bindingBuilder = builder.Allocate(ref mask.TransformBindings, data.Count);
            var weightBuilder = builder.Allocate(ref mask.TransformWeights, data.Count);
            var propagatedWeightBuilder = builder.Allocate(ref mask.TransformPropagateWeights, (int)math.ceil(data.Count / (float)k_BitsPerField));

            var i = 0;
            var enumerator = data.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                bindingBuilder[i] = current.Key;
                weightBuilder[i] = math.saturate(current.Value.Value);
                propagatedWeightBuilder[i >> k_BitShiftPerField].SetBits(i % k_BitsPerField, current.Value.IsPropagated);
                i++;
            }
        }

        static void FillBindingAndWeightData(ref BlobBuilder builder, ref BlobArray<StringHash> bindings, ref BlobArray<float> weights, in NativeHashMap<StringHash, float>.ReadOnly data)
        {
            var bindingBuilder = builder.Allocate(ref bindings, data.Count);
            var weightBuilder = builder.Allocate(ref weights, data.Count);

            var i = 0;
            var enumerator = data.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                bindingBuilder[i] = current.Key;
                weightBuilder[i] = math.saturate(current.Value);
                i++;
            }
        }

        struct TransformWeight
        {
            public float Value;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsPropagated;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsCreated()
        {
            if (!m_IsCreated)
                throw new NullReferenceException("RigBuilder is not properly created.");
        }
    }
}
