// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.Animations
{
    /// <summary>
    /// An animated value of a rig, such as the translation of a bone or a float property, identified by a hash.
    /// </summary>
    /// <remarks>
    /// The built-in channel types, such as <see cref="LocalTranslationChannel"/> and <see cref="FloatChannel"/>,
    /// implement this interface. Add them to a <see cref="RigBuilder"/> to define the bindings of a
    /// <see cref="RigDefinition"/>. Clips find the channels of a rig that match their curves through the
    /// <see cref="IAnimationChannel.Id"/>. The built-in channel types compute the identifier from a transform path and,
    /// for properties, an attribute name.
    /// </remarks>
    /// <seealso cref="RigBuilder"/>
    /// <seealso cref="FloatChannel"/>
    public interface IAnimationChannel
    {
        /// <summary>
        /// The unique identifier of the channel.
        /// </summary>
        StringHash Id { get; set; }
    }

    /// <summary>
    /// Base interface for the animation channels.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    public interface IAnimationChannel<T> : IAnimationChannel
        where T : struct
    {
        /// <summary>
        /// Default value of the channel.
        /// </summary>
        T DefaultValue { get; set; }
    }

    /// <summary>
    /// Translation channel.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LocalTranslationChannel : IAnimationChannel<float3>, IEquatable<LocalTranslationChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <inheritdoc/>
        public float3 DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(LocalTranslationChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Create a LocalTranslationChannel from a transform path.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="defaultValue">Default translation value.</param>
        /// <returns>Returns a LocalTranslationChannel.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static LocalTranslationChannel Create(string path, float3 defaultValue) =>
            new LocalTranslationChannel
            {
                Id = new StringHash(path),
                DefaultValue = defaultValue
            };
    }

    /// <summary>
    /// Rotation channel.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LocalRotationChannel : IAnimationChannel<quaternion>, IEquatable<LocalRotationChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <inheritdoc/>
        public quaternion DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(LocalRotationChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Create a LocalRotationChannel from a transform path.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="defaultValue">Default rotation value.</param>
        /// <returns>Returns a LocalRotationChannel.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static LocalRotationChannel Create(string path, quaternion defaultValue) =>
            new LocalRotationChannel
            {
                Id = new StringHash(path),
                DefaultValue = defaultValue
            };
    }

    /// <summary>
    /// Scale channel.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LocalScaleChannel : IAnimationChannel<float3>, IEquatable<LocalScaleChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <inheritdoc/>
        public float3 DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(LocalScaleChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Create a LocalScaleChannel from a transform path.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="defaultValue">Default scale value.</param>
        /// <returns>Returns a LocalScaleChannel.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static LocalScaleChannel Create(string path, float3 defaultValue) =>
            new LocalScaleChannel
            {
                Id = new StringHash(path),
                DefaultValue = defaultValue
            };
    }

    /// <summary>
    /// Float channel.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct FloatChannel : IAnimationChannel<float>, IEquatable<FloatChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <inheritdoc/>
        public float DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(FloatChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Create a FloatChannel from a transform path, a component type and an attribute name.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="type">The type of the object to animate.</param>
        /// <param name="attributeName">The name of the property to animate on the object.</param>
        /// <param name="defaultValue">Default float value.</param>
        /// <returns>Returns a FloatChannel.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static FloatChannel Create(string path, Type type, string attributeName, float defaultValue)
        {
            return GenericBindingUtility.CreateGenericBinding(path, type, attributeName, false, out var genericBinding)
                ? new FloatChannel { Id = genericBinding, DefaultValue = defaultValue }
                : default;
        }
    }

    /// <summary>
    /// A channel that animates an integer value, such as a discrete property of a component.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct IntChannel : IAnimationChannel<int>, IEquatable<IntChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <inheritdoc/>
        public int DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(IntChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Create an IntChannel from a transform path, a component type and an attribute name.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="type">The type of the object to animate.</param>
        /// <param name="attributeName">The name of the property to animate on the object.</param>
        /// <param name="defaultValue">Default int value.</param>
        /// <returns>Returns an IntChannel.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static IntChannel Create(string path, Type type, string attributeName, int defaultValue)
        {
            return GenericBindingUtility.CreateGenericBinding(path, type, attributeName, false, out var genericBinding)
                ? new IntChannel { Id = genericBinding, DefaultValue = defaultValue }
                : default;
        }
    }

    /// <summary>
    /// An animation channel that holds an <see cref="EntityId"/>, such as a sprite or a material reference.
    /// </summary>
    /// <remarks>
    /// Use this channel to animate a property that references an object, such as a sprite or a material. Add it to a
    /// <see cref="RigBuilder"/> to define an entity ID binding in a <see cref="RigDefinition"/>. A clip drives the
    /// channel when it has a curve for an object reference with the same <see cref="IAnimationChannel.Id"/>.
    ///
    /// Two channels are equal when their `Id` is equal, whatever their `DefaultValue`.
    /// </remarks>
    /// <seealso cref="RigBuilder"/>
    /// <seealso cref="IAnimationChannel{T}"/>
    [GenerateTestsForBurstCompatibility]
    public struct EntityIdChannel : IAnimationChannel<EntityId>, IEquatable<EntityIdChannel>
    {
        /// <inheritdoc/>
        public StringHash Id { get; set; }

        /// <summary>
        /// The <see cref="EntityId"/> the channel holds when no clip animates it.
        /// </summary>
        public EntityId DefaultValue { get; set; }

        /// <inheritdoc/>
        public bool Equals(EntityIdChannel other)
        {
            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Creates an EntityIdChannel from a transform path, a component type and an attribute name.
        /// </summary>
        /// <param name="path">The transform path to the object to animate.</param>
        /// <param name="type">The type of the object to animate.</param>
        /// <param name="attributeName">The name of the property to animate on the object.</param>
        /// <param name="defaultValue">The `EntityId` that the channel holds when no clip animates it.</param>
        /// <returns>The new channel, with an identifier computed from <paramref name="path"/> and
        /// <paramref name="attributeName"/>.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static EntityIdChannel Create(string path, Type type, string attributeName, EntityId defaultValue)
        {
            return GenericBindingUtility.CreateGenericBinding(path, type, attributeName, true, out var genericBinding)
                ? new EntityIdChannel { Id = genericBinding, DefaultValue = defaultValue }
                : default;
        }
    }

    /// <summary>
    /// The representation of a transform in the animation rig.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct SkeletonNode
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public StringHash Id;

        /// <summary>
        /// Parent <see cref="SkeletonNode"/> index.
        /// </summary>
        /// <remarks>
        /// The root node is the first node of a skeleton and has a parent index of `-1`. Each other node must have a
        /// parent index that is lower than its own index, which means that you add a parent to a
        /// <see cref="RigBuilder"/> before its children. To get the index of a node from its identifier, use
        /// <see cref="RigBuilder.GetBoneIndex"/>.
        /// </remarks>
        public int        ParentIndex;

        /// <summary>
        /// Default translation values.
        /// </summary>
        public float3       LocalTranslationDefaultValue;
        /// <summary>
        /// Default rotation values.
        /// </summary>
        public quaternion   LocalRotationDefaultValue;
        /// <summary>
        /// Default scale values.
        /// </summary>
        public float3       LocalScaleDefaultValue;
    }

    /// <summary>
    /// The representation of a socket in the animation rig.
    /// </summary>
    /// <remarks>
    /// A socket is a fixed transform relative to a bone, for example the place where a character holds a weapon. Add
    /// sockets with <see cref="RigBuilder.Add(in SocketNode)"/>. The animation stream has no channel for a socket, so
    /// the socket moves only with its parent bone.
    ///
    /// The following example uses the `Unity.Animation`, `Unity.Collections`, `Unity.Entities`, and
    /// `Unity.Mathematics` namespaces. It adds a socket to the hand of an arm rig, then reads the transform of the
    /// socket in character space from an animation stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SocketExample.cs#AddAndReadSocket}]]></code>
    /// </example>
    [GenerateTestsForBurstCompatibility]
    public struct SocketNode
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public StringHash Id;

        /// <summary>
        /// Default rotation values.
        /// </summary>
        public quaternion   LocalRotationValue;
        /// <summary>
        /// Default translation values.
        /// </summary>
        public float3       LocalTranslationValue;
        /// <summary>
        /// Default scale values.
        /// </summary>
        public float3       LocalScaleValue;
        /// <summary>
        /// Parent <see cref="SkeletonNode"/> index.
        /// </summary>
        public int          BoneParentIndex;
    }

    /// <summary>
    /// The skeleton topology.
    /// </summary>
    /// <remarks>
    /// The skeleton represents a bone hierarchy with unique identifier for each skeleton bone.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public struct Skeleton
    {
        // The 2 following fields are stored as separate arrays rather than in a SkeletonNode to minimize cache pollution,
        // most of the time those 2 arrays are not used together

        /// <summary>
        /// Unique identifiers for each bone.
        /// </summary>
        /// <remarks>
        /// The index of a bone in this array is the bone index that <see cref="AnimationStream"/> methods take.
        /// </remarks>
        public BlobArray<StringHash>  Ids;

        /// <summary>
        /// Parenting information for bones. Each bone has only one parent define by the parent index in this skeleton.
        /// </summary>
        /// <remarks>
        /// A Skeleton can only have one root which should always be the first bone,
        /// the root parent index is always -1 which means that it doesn't have a parent.
        /// </remarks>
        public BlobArray<int>           ParentIndexes;

        /// <summary>
        /// Returns number of bones defined in this skeleton.
        /// </summary>
        public int BoneCount => Ids.Length;

        /// <summary>
        /// Finds the index of the bone with an identifier.
        /// </summary>
        /// <remarks>
        /// For a rig built from transforms or an Avatar, the identifier of a bone is the <see cref="StringHash"/> of
        /// its path relative to the root. For example, `FindBoneIndex(new StringHash("Hips/Spine"))` finds the bone at
        /// that path. The method searches the bones linearly. For details, refer to
        /// <see cref="BindingUtility.FindBindingIndex"/>.
        /// </remarks>
        /// <param name="id">The `StringHash` identifier of the bone to find.</param>
        /// <returns>The index of the bone in <see cref="Ids"/>. If no bone has the identifier, returns `-1`.</returns>
        public int FindBoneIndex(StringHash id) =>
            BindingUtility.FindBindingIndex(ref Ids, id);
    }

    /// <summary>
    /// The blob representation of a Rig Definition.
    /// It contains the skeleton topology, a set of animation channels with unique identifiers and
    /// a buffer of default values for each animation channel.
    /// </summary>
    /// <remarks>
    /// Build a rig definition with <see cref="RigBuilder.ToRigDefinition"/>. To get a builder with the bones and
    /// channels that clips animate, use <see cref="RigBuilder.Create(Transform, AnimationClip[], Allocator)"/>. An
    /// Avatar also stores a rig definition with a bone for each of its transforms. Read it with
    /// <see cref="AvatarExtensions.ToRigReference(Avatar)"/>.
    ///
    /// The following example uses the `Unity.Animation`, `Unity.Collections`, `Unity.Entities`, `Unity.IntegerTime`,
    /// `Unity.Mathematics`, and `UnityEngine` namespaces. It is a `MonoBehaviour` that plays a clip on the hierarchy
    /// under its GameObject with the rig definition that an Avatar stores.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipWithAvatarExample.cs#SampleClipWithAvatar}]]></code>
    /// </example>
    [GenerateTestsForBurstCompatibility]
    public partial struct RigDefinition
    {
        // Version of the blob layout that an Avatar stores in builds. Increase it when the layout changes
        internal const int BlobVersion = 2;

        /// <summary>
        /// The skeleton topology.
        /// </summary>
        public Skeleton          Skeleton;

        /// <summary>
        /// The set of animated channels with unique identifiers.
        /// </summary>
        public BindingSet        Bindings;

        /// <summary>
        /// Buffer of default values for all animated channels.
        /// </summary>
        /// <remarks>
        /// The <see cref="Unity.Animations.AnimationStream.FromDefaultValues(Unity.Entities.BlobAssetReference{Unity.Animations.RigDefinition})"/> can be used to create a read only animation stream and
        /// access this data through the AnimationStream API.
        /// </remarks>
        public BlobArray<AnimatedData>  DefaultValues;

        /// <summary>
        /// Buffer of static socket offsets relative to their bone parent.
        /// </summary>
        public BlobArray<SocketNode>  SocketValues;

        /// <summary>
        /// The index of the bone whose motion gives the root motion of a clip.
        /// </summary>
        /// <remarks>
        /// The value is an index in <see cref="Skeleton.Ids"/>. <see cref="RigBuilder.ToRigDefinition"/> sets it to the
        /// bone that you set in <see cref="RigBuilder.RootMotionBoneId"/>, or to 0, the root bone, if you
        /// don't choose one. The value is `-1` if the rig has no skeleton.
        ///
        /// <see cref="ClipSampler.SampleRootMotion(in TimerState)"/> uses this bone when the clip has no root motion
        /// curves, and <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/> removes
        /// the root motion from this bone.
        /// </remarks>
        public int RootMotionBoneIndex;
    }
}
