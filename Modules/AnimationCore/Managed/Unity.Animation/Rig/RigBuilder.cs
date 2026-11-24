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
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace Unity.Animations
{
    /// <summary>
    /// An intermediate data representing the rig.
    /// </summary>
    /// <remarks>
    /// If you don't use the Skeleton but your own custom rig representation,
    /// as long as you have a method to convert this representation to a RigBuilder,
    /// we can build a RigDefinition from it.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.Mathematics`,
    /// and `UnityEngine` namespaces. It builds a rig definition with three bones and one float channel.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BuildRigExample.cs#BuildRigDefinition}]]></code>
    /// </example>
    /// <seealso cref="RigDefinition"/>
    /// <seealso cref="BindingSet"/>
    [GenerateTestsForBurstCompatibility, BurstCompile]
    public partial struct RigBuilder : IDisposable
    {

        internal NativeList<SkeletonNode> m_SkeletonNodes;
        internal NativeList<LocalTranslationChannel> m_TranslationChannels;
        internal NativeList<LocalRotationChannel> m_RotationChannels;
        internal NativeList<LocalScaleChannel> m_ScaleChannels;
        internal NativeList<FloatChannel> m_FloatChannels;
        internal NativeList<IntChannel> m_IntChannels;
        internal NativeList<EntityIdChannel> m_EntityIdChannels;
        internal NativeList<SocketNode> m_SocketNodes;

        internal NativeReference<StringHash> m_RootMotionBone;

        [MarshalAs(UnmanagedType.U1)]
        bool m_IsCreated;

        /// <summary>
        /// RigBuilder constructor.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        public RigBuilder(Allocator allocator)
        {
            m_SkeletonNodes = new NativeList<SkeletonNode>(allocator);
            m_TranslationChannels = new NativeList<LocalTranslationChannel>(allocator);
            m_RotationChannels = new NativeList<LocalRotationChannel>(allocator);
            m_ScaleChannels = new NativeList<LocalScaleChannel>(allocator);
            m_FloatChannels = new NativeList<FloatChannel>(allocator);
            m_IntChannels = new NativeList<IntChannel>(allocator);
            m_EntityIdChannels = new NativeList<EntityIdChannel>(allocator);
            m_SocketNodes = new NativeList<SocketNode>(allocator);
            m_RootMotionBone = new NativeReference<StringHash>(allocator);

            m_IsCreated = true;
        }

        /// <summary>
        /// Returns true if the RigBuilder has been properly created.
        /// </summary>
        /// <remarks>The default constructor doesn't create the builder correctly. Use
        /// <see cref="RigBuilder(Allocator)"/> instead.</remarks>
        public readonly bool IsCreated => m_IsCreated;

        /// <summary>
        /// The identifier of the bone whose motion gives the root motion of a clip.
        /// </summary>
        /// <remarks>
        /// By default, the value isn't set: it is the default `StringHash`, and the root motion bone is the root bone of
        /// the skeleton. Set it to choose another bone, for example a hips bone under a root that clips don't animate.
        /// The identifier of a bone is the <see cref="StringHash"/> of its path relative to the root of the hierarchy,
        /// for example `rigBuilder.RootMotionBoneId = "Skeleton/Hips"`. Set the default `StringHash` to go back to the
        /// root bone. <see cref="Clear"/> also resets the value to the default.
        ///
        /// <see cref="ToRigDefinition(Allocator)"/> stores the index of the bone in
        /// <see cref="RigDefinition.RootMotionBoneIndex"/>. When a clip has no root motion curves,
        /// <see cref="ClipSampler.SampleRootMotion(in TimerState)"/> returns the motion of this bone in character
        /// space, which includes the motion of its parent bones.
        /// <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/> removes the root
        /// motion from this bone in the pose.
        ///
        /// Add the bone before you set this value. If safety checks are enabled, setting it throws an
        /// `ArgumentException` when no bone of the skeleton has this identifier, and `ToRigDefinition` throws one when
        /// you removed the bone afterward.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and you set an identifier that no bone
        /// of the skeleton has.</exception>
        public StringHash RootMotionBoneId
        {
            get => m_RootMotionBone.Value;
            readonly set
            {
                ValidateIsCreated();
                ValidateRootMotionBone(value);

                // The reference points to memory that all copies of the builder share, so write through a local copy.
                var rootMotionBone = m_RootMotionBone;
                rootMotionBone.Value = value;
            }
        }

        /// <summary>
        /// Adds a SkeletonNode to the RigBuilder.
        /// </summary>
        /// <remarks>
        /// The index of the node is the number of nodes in the builder before the call, and it becomes the bone index
        /// in the <see cref="RigDefinition"/>. Add the root first, with a <see cref="SkeletonNode.ParentIndex"/> of
        /// `-1`. Add each other node after its parent, so that its `ParentIndex` is lower than its own index. To get
        /// the index of a parent from its identifier, use <see cref="GetBoneIndex"/>.
        ///
        /// Each bone has a translation, rotation, and scale channel with the identifier of its node, so the identifier
        /// must differ from the identifiers of the other nodes and of the translation, rotation, and scale channels.
        /// </remarks>
        /// <param name="node">The skeleton node to add.</param>
        /// <exception cref="ArgumentException">The <see cref="SkeletonNode.ParentIndex"/> of the node isn't valid, or
        /// the builder already has a node or a translation, rotation, or scale channel with the identifier of the node.
        /// The method checks this only when safety checks are enabled.</exception>
        public void Add(in SkeletonNode node)
        {
            ValidateIsCreated();
            ValidateSkeletonNode(node, m_SkeletonNodes.Length);
            m_SkeletonNodes.Add(node);
        }

        /// <summary>
        /// Finds the index of the skeleton node that has an identifier.
        /// </summary>
        /// <remarks>
        /// The index is the bone index in the <see cref="RigDefinition"/> that <see cref="ToRigDefinition"/> creates.
        /// Use it to set the <see cref="SkeletonNode.ParentIndex"/> of a node from the identifier of its parent.
        /// The method searches the nodes linearly, like <see cref="Skeleton.FindBoneIndex"/> does on a rig definition.
        /// </remarks>
        /// <param name="id">The <see cref="SkeletonNode.Id"/> of the node to find.</param>
        /// <returns>The index of the node. If no node has the identifier, returns `-1`.</returns>
        public int GetBoneIndex(StringHash id)
        {
            ValidateIsCreated();
            return FindIndex(m_SkeletonNodes, id);
        }

        /// <summary>
        /// Adds a LocalTranslationChannel to the Rigbuilder.
        /// </summary>
        /// <param name="channel">The LocalTranslationChannel to add.</param>
        /// <exception cref="ArgumentException">The builder already has a bone or a translation channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in LocalTranslationChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            ValidateUniqueId(m_TranslationChannels, channel.Id, -1);
            m_TranslationChannels.Add(channel);
        }

        /// <summary>
        /// Adds a LocalRotationChannel to the RigBuilder/
        /// </summary>
        /// <param name="channel">The LocalRotationChannel to add.</param>
        /// <exception cref="ArgumentException">The builder already has a bone or a rotation channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in LocalRotationChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            ValidateUniqueId(m_RotationChannels, channel.Id, -1);
            m_RotationChannels.Add(channel);
        }

        /// <summary>
        /// Adds a LocalScaleChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The LocalScaleChannel to add.</param>
        /// <exception cref="ArgumentException">The builder already has a bone or a scale channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in LocalScaleChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            ValidateUniqueId(m_ScaleChannels, channel.Id, -1);
            m_ScaleChannels.Add(channel);
        }

        /// <summary>
        /// Adds a FloatChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The FloatChannel to add.</param>
        /// <exception cref="ArgumentException">The builder already has a float channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in FloatChannel channel)
        {
            ValidateIsCreated();
            ValidateUniqueId(m_FloatChannels, channel.Id, -1);
            m_FloatChannels.Add(channel);
        }

        /// <summary>
        /// Adds an IntChannel to the RigBuilder.
        /// </summary>
        /// <param name="channel">The IntChannel to add.</param>
        /// <exception cref="ArgumentException">The builder already has a integer channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in IntChannel channel)
        {
            ValidateIsCreated();
            ValidateUniqueId(m_IntChannels, channel.Id, -1);
            m_IntChannels.Add(channel);
        }

        /// <summary>
        /// Adds an EntityIdChannel to the RigBuilder.
        /// </summary>
        /// <remarks>
        /// The channel becomes an entity ID binding of the <see cref="RigDefinition"/> that
        /// <see cref="ToRigDefinition(Unity.Collections.Allocator)"/> creates. A clip drives the channel when it has a
        /// curve that animates an object reference and whose combined path and property hash equals
        /// <see cref="EntityIdChannel.Id"/>. Call this method on a builder that you created with
        /// <see cref="RigBuilder(Allocator)"/>. Otherwise, if safety checks are enabled, the method throws a
        /// `NullReferenceException`.
        /// </remarks>
        /// <param name="channel">The channel to add to the rig that the builder creates.</param>
        /// <exception cref="ArgumentException">The builder already has a entity ID channel with this
        /// identifier. The method checks this only when safety checks are enabled.</exception>
        public void Add(in EntityIdChannel channel)
        {
            ValidateIsCreated();
            ValidateUniqueId(m_EntityIdChannels, channel.Id, -1);
            m_EntityIdChannels.Add(channel);
        }

        /// <summary>
        /// Adds a SocketNode to the RigBuilder.
        /// </summary>
        /// <param name="socketNode">The SocketNode to add.</param>
        /// <exception cref="ArgumentException">The builder already has a socket with the identifier of the socket. The
        /// method checks this only when safety checks are enabled.</exception>
        public void Add(in SocketNode socketNode)
        {
            ValidateIsCreated();
            ValidateUniqueSocketId(socketNode.Id);
            m_SocketNodes.Add(socketNode);
        }

        /// <summary>
        /// Gets the skeleton node that has an identifier.
        /// </summary>
        /// <remarks>
        /// Use this method to read the default values of a bone. To change them, use
        /// <see cref="TrySetBoneDefaultTranslation"/>, <see cref="TrySetBoneDefaultRotation"/>, and
        /// <see cref="TrySetBoneDefaultScale"/>.
        /// </remarks>
        /// <param name="id">The <see cref="SkeletonNode.Id"/> of the node to get.</param>
        /// <param name="node">When the method returns, the node that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a node with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out SkeletonNode node)
        {
            ValidateIsCreated();
            var index = FindIndex(m_SkeletonNodes, id);
            node = index >= 0 ? m_SkeletonNodes[index] : default;
            return index >= 0;
        }

        /// <summary>
        /// Gets the skeleton node at a bone index.
        /// </summary>
        /// <remarks>
        /// The bone index of a node is its index in the builder, which becomes its bone index in the
        /// <see cref="RigDefinition"/>. To get the bone index of a node from its identifier, use
        /// <see cref="GetBoneIndex"/>.
        /// </remarks>
        /// <param name="boneIndex">The bone index of the node to get.</param>
        /// <param name="node">When the method returns, the node at the bone index, or the default value if the index
        /// is out of range.</param>
        /// <returns>True if the builder has a node at the bone index. Otherwise, false.</returns>
        public bool TryGet(int boneIndex, out SkeletonNode node)
        {
            ValidateIsCreated();
            var isValid = (uint)boneIndex < (uint)m_SkeletonNodes.Length;
            node = isValid ? m_SkeletonNodes[boneIndex] : default;
            return isValid;
        }

        /// <summary>
        /// Gets the translation channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalTranslationChannel)"/>. To
        /// read the default translation of a bone, use <see cref="TryGet(StringHash, out SkeletonNode)"/>. If several
        /// channels have the identifier, the method gets the first one, which is the one that
        /// <see cref="ToRigDefinition"/> uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out LocalTranslationChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_TranslationChannels, id, out channel);
        }

        /// <summary>
        /// Gets the rotation channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalRotationChannel)"/>. To read
        /// the default rotation of a bone, use <see cref="TryGet(StringHash, out SkeletonNode)"/>. If several channels
        /// have the identifier, the method gets the first one, which is the one that <see cref="ToRigDefinition"/>
        /// uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out LocalRotationChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_RotationChannels, id, out channel);
        }

        /// <summary>
        /// Gets the scale channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalScaleChannel)"/>. To read the
        /// default scale of a bone, use <see cref="TryGet(StringHash, out SkeletonNode)"/>. If several channels have
        /// the identifier, the method gets the first one, which is the one that <see cref="ToRigDefinition"/> uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out LocalScaleChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_ScaleChannels, id, out channel);
        }

        /// <summary>
        /// Gets the float channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// If several channels have the identifier, the method gets the first one, which is the one that
        /// <see cref="ToRigDefinition"/> uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out FloatChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_FloatChannels, id, out channel);
        }

        /// <summary>
        /// Gets the integer channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// If several channels have the identifier, the method gets the first one, which is the one that
        /// <see cref="ToRigDefinition"/> uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out IntChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_IntChannels, id, out channel);
        }

        /// <summary>
        /// Gets the entity ID channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// If several channels have the identifier, the method gets the first one, which is the one that
        /// <see cref="ToRigDefinition"/> uses.
        /// </remarks>
        /// <param name="id">The identifier of the channel to get.</param>
        /// <param name="channel">When the method returns, the channel that has the identifier, or the default value if
        /// there's none.</param>
        /// <returns>True if the builder has a channel with the identifier. Otherwise, false.</returns>
        public bool TryGet(StringHash id, out EntityIdChannel channel)
        {
            ValidateIsCreated();
            return TryGet(m_EntityIdChannels, id, out channel);
        }

        /// <summary>
        /// Replaces the skeleton node that has the same identifier as a node.
        /// </summary>
        /// <remarks>
        /// The method keeps the index of the node, so the <see cref="SkeletonNode.ParentIndex"/> of the other nodes
        /// stays valid. The new node must follow the same rules as in <see cref="Add(in SkeletonNode)"/>: its parent
        /// has a lower index, and only the node at index 0 is a root. To only change the default values of a bone, use
        /// <see cref="TrySetBoneDefaultTranslation"/>, <see cref="TrySetBoneDefaultRotation"/>, and
        /// <see cref="TrySetBoneDefaultScale"/>.
        /// </remarks>
        /// <param name="node">The node that replaces the node with the same <see cref="SkeletonNode.Id"/>.</param>
        /// <returns>True if the builder had a node with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        /// <exception cref="ArgumentException">The <see cref="SkeletonNode.ParentIndex"/> of the node isn't valid at
        /// the index of the replaced node, or another node or a translation, rotation, or scale channel has the
        /// identifier of the node. The method checks this only when safety checks are enabled.</exception>
        public bool Replace(in SkeletonNode node)
        {
            ValidateIsCreated();
            var index = FindIndex(m_SkeletonNodes, node.Id);
            if (index < 0)
                return false;

            ValidateSkeletonNode(node, index);
            m_SkeletonNodes[index] = node;
            return true;
        }

        /// <summary>
        /// Replaces the translation channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel. The method doesn't change the bones. To change the default
        /// translation of a bone, use <see cref="TrySetBoneDefaultTranslation"/>.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        /// <exception cref="ArgumentException">A bone has the identifier of the channel. The method checks this only
        /// when safety checks are enabled.</exception>
        public bool Replace(in LocalTranslationChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            return Replace(m_TranslationChannels, channel);
        }

        /// <summary>
        /// Replaces the rotation channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel. The method doesn't change the bones. To change the default
        /// rotation of a bone, use <see cref="TrySetBoneDefaultRotation"/>.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        /// <exception cref="ArgumentException">A bone has the identifier of the channel. The method checks this only
        /// when safety checks are enabled.</exception>
        public bool Replace(in LocalRotationChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            return Replace(m_RotationChannels, channel);
        }

        /// <summary>
        /// Replaces the scale channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel. The method doesn't change the bones. To change the default
        /// scale of a bone, use <see cref="TrySetBoneDefaultScale"/>.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        /// <exception cref="ArgumentException">A bone has the identifier of the channel. The method checks this only
        /// when safety checks are enabled.</exception>
        public bool Replace(in LocalScaleChannel channel)
        {
            ValidateIsCreated();
            ValidateIsNotBoneId(channel.Id);
            return Replace(m_ScaleChannels, channel);
        }

        /// <summary>
        /// Replaces the float channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool Replace(in FloatChannel channel)
        {
            ValidateIsCreated();
            return Replace(m_FloatChannels, channel);
        }

        /// <summary>
        /// Replaces the integer channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool Replace(in IntChannel channel)
        {
            ValidateIsCreated();
            return Replace(m_IntChannels, channel);
        }

        /// <summary>
        /// Replaces the entity ID channel that has the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// Use this method to change a channel.
        /// </remarks>
        /// <param name="channel">The channel that replaces the channel with the same identifier.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool Replace(in EntityIdChannel channel)
        {
            ValidateIsCreated();
            return Replace(m_EntityIdChannels, channel);
        }

        /// <summary>
        /// Removes the skeleton node that has the same identifier as a node.
        /// </summary>
        /// <remarks>
        /// The method only compares the <see cref="SkeletonNode.Id"/>. It also removes the descendants of the node and
        /// the sockets attached to the removed nodes.
        ///
        /// The method decreases the index of each node after a removed node, and updates the
        /// <see cref="SkeletonNode.ParentIndex"/> of the nodes and the <see cref="SocketNode.BoneParentIndex"/> of the
        /// sockets to match.
        /// </remarks>
        /// <param name="node">A node with the identifier of the node to remove.</param>
        /// <returns>True if the builder had a node with the identifier. Otherwise, false.</returns>
        public bool Remove(in SkeletonNode node)
        {
            ValidateIsCreated();
            var index = FindIndex(m_SkeletonNodes, node.Id);
            if (index < 0)
                return false;

            var nodeCount = m_SkeletonNodes.Length;
            var removed = new NativeArray<bool>(nodeCount, Allocator.Temp);
            removed[index] = true;

            // Parents come before their children, so one pass finds all the descendants
            for (int i = index + 1; i < nodeCount; ++i)
            {
                var parentIndex = m_SkeletonNodes[i].ParentIndex;
                removed[i] = parentIndex >= 0 && parentIndex < i && removed[parentIndex];
            }

            RemoveSkeletonNodes(removed);
            removed.Dispose();
            return true;
        }

        /// <summary>
        /// Removes the translation channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like
        /// <see cref="LocalTranslationChannel.Equals(LocalTranslationChannel)"/>. It doesn't change the bones, which
        /// always have a translation channel. To remove a bone, use <see cref="Remove(in SkeletonNode)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in LocalTranslationChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_TranslationChannels, channel.Id);
        }

        /// <summary>
        /// Removes the rotation channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like
        /// <see cref="LocalRotationChannel.Equals(LocalRotationChannel)"/>. It doesn't change the bones, which always
        /// have a rotation channel. To remove a bone, use <see cref="Remove(in SkeletonNode)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in LocalRotationChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_RotationChannels, channel.Id);
        }

        /// <summary>
        /// Removes the scale channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like <see cref="LocalScaleChannel.Equals(LocalScaleChannel)"/>.
        /// It doesn't change the bones, which always have a scale channel. To remove a bone, use
        /// <see cref="Remove(in SkeletonNode)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in LocalScaleChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_ScaleChannels, channel.Id);
        }

        /// <summary>
        /// Removes the float channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like <see cref="FloatChannel.Equals(FloatChannel)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in FloatChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_FloatChannels, channel.Id);
        }

        /// <summary>
        /// Removes the integer channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like <see cref="IntChannel.Equals(IntChannel)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in IntChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_IntChannels, channel.Id);
        }

        /// <summary>
        /// Removes the entity ID channels that have the same identifier as a channel.
        /// </summary>
        /// <remarks>
        /// The method only compares the identifiers, like <see cref="EntityIdChannel.Equals(EntityIdChannel)"/>.
        /// </remarks>
        /// <param name="channel">A channel with the identifier of the channels to remove.</param>
        /// <returns>True if the builder had a channel with the identifier. Otherwise, false.</returns>
        public bool Remove(in EntityIdChannel channel)
        {
            ValidateIsCreated();
            return Remove(m_EntityIdChannels, channel.Id);
        }

        /// <summary>
        /// Removes the sockets that have the same identifier as a socket.
        /// </summary>
        /// <remarks>
        /// The method only compares the <see cref="SocketNode.Id"/>. It decreases by one the index of each socket
        /// after a removed socket.
        /// </remarks>
        /// <param name="socketNode">A socket with the identifier of the sockets to remove.</param>
        /// <returns>True if the builder had a socket with the identifier. Otherwise, false.</returns>
        public bool Remove(in SocketNode socketNode)
        {
            ValidateIsCreated();
            var removed = false;
            for (int i = m_SocketNodes.Length - 1; i >= 0; --i)
            {
                if (m_SocketNodes[i].Id.Equals(socketNode.Id))
                {
                    m_SocketNodes.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        /// <summary>
        /// Sets the default translation of the bone at a bone index.
        /// </summary>
        /// <remarks>
        /// The default translation is the <see cref="SkeletonNode.LocalTranslationDefaultValue"/> of the node. To get
        /// the bone index of a node from its identifier, use <see cref="GetBoneIndex"/>.
        /// </remarks>
        /// <param name="boneIndex">The bone index of the node.</param>
        /// <param name="translation">The default translation, relative to the parent bone.</param>
        /// <returns>True if the builder has a node at the bone index. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool TrySetBoneDefaultTranslation(int boneIndex, float3 translation)
        {
            ValidateIsCreated();
            if ((uint)boneIndex >= (uint)m_SkeletonNodes.Length)
                return false;

            m_SkeletonNodes.ElementAt(boneIndex).LocalTranslationDefaultValue = translation;
            return true;
        }

        /// <summary>
        /// Sets the default rotation of the bone at a bone index.
        /// </summary>
        /// <remarks>
        /// The default rotation is the <see cref="SkeletonNode.LocalRotationDefaultValue"/> of the node. To get the
        /// bone index of a node from its identifier, use <see cref="GetBoneIndex"/>.
        /// </remarks>
        /// <param name="boneIndex">The bone index of the node.</param>
        /// <param name="rotation">The default rotation, relative to the parent bone.</param>
        /// <returns>True if the builder has a node at the bone index. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool TrySetBoneDefaultRotation(int boneIndex, quaternion rotation)
        {
            ValidateIsCreated();
            if ((uint)boneIndex >= (uint)m_SkeletonNodes.Length)
                return false;

            m_SkeletonNodes.ElementAt(boneIndex).LocalRotationDefaultValue = rotation;
            return true;
        }

        /// <summary>
        /// Sets the default scale of the bone at a bone index.
        /// </summary>
        /// <remarks>
        /// The default scale is the <see cref="SkeletonNode.LocalScaleDefaultValue"/> of the node. To get the bone
        /// index of a node from its identifier, use <see cref="GetBoneIndex"/>.
        /// </remarks>
        /// <param name="boneIndex">The bone index of the node.</param>
        /// <param name="scale">The default scale, relative to the parent bone.</param>
        /// <returns>True if the builder has a node at the bone index. Otherwise, false, and the builder doesn't
        /// change.</returns>
        public bool TrySetBoneDefaultScale(int boneIndex, float3 scale)
        {
            ValidateIsCreated();
            if ((uint)boneIndex >= (uint)m_SkeletonNodes.Length)
                return false;

            m_SkeletonNodes.ElementAt(boneIndex).LocalScaleDefaultValue = scale;
            return true;
        }

        /// <summary>
        /// Sets the default value of the translation channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalTranslationChannel)"/>.
        /// To set the default translation of a bone, use <see cref="TrySetBoneDefaultTranslation"/>.
        /// </remarks>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="translation">The default translation of the channel.</param>
        /// <returns>True if the builder has a translation channel with the identifier. Otherwise, false, and the
        /// builder doesn't change.</returns>
        public bool TrySetDefaultTranslation(StringHash id, float3 translation)
        {
            ValidateIsCreated();
            var index = FindIndex(m_TranslationChannels, id);
            if (index < 0)
                return false;

            m_TranslationChannels.ElementAt(index).DefaultValue = translation;
            return true;
        }

        /// <summary>
        /// Sets the default value of the rotation channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalRotationChannel)"/>.
        /// To set the default rotation of a bone, use <see cref="TrySetBoneDefaultRotation"/>.
        /// </remarks>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="rotation">The default rotation of the channel.</param>
        /// <returns>True if the builder has a rotation channel with the identifier. Otherwise, false, and the builder
        /// doesn't change.</returns>
        public bool TrySetDefaultRotation(StringHash id, quaternion rotation)
        {
            ValidateIsCreated();
            var index = FindIndex(m_RotationChannels, id);
            if (index < 0)
                return false;

            m_RotationChannels.ElementAt(index).DefaultValue = rotation;
            return true;
        }

        /// <summary>
        /// Sets the default value of the scale channel that has an identifier.
        /// </summary>
        /// <remarks>
        /// The method only searches the channels that you add with <see cref="Add(in LocalScaleChannel)"/>.
        /// To set the default scale of a bone, use <see cref="TrySetBoneDefaultScale"/>.
        /// </remarks>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="scale">The default scale of the channel.</param>
        /// <returns>True if the builder has a scale channel with the identifier. Otherwise, false, and the builder
        /// doesn't change.</returns>
        public bool TrySetDefaultScale(StringHash id, float3 scale)
        {
            ValidateIsCreated();
            var index = FindIndex(m_ScaleChannels, id);
            if (index < 0)
                return false;

            m_ScaleChannels.ElementAt(index).DefaultValue = scale;
            return true;
        }

        /// <summary>
        /// Sets the default value of the float channel that has an identifier.
        /// </summary>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="value">The default value of the channel.</param>
        /// <returns>True if the builder has a float channel with the identifier. Otherwise, false, and the builder
        /// doesn't change.</returns>
        public bool TrySetDefaultFloat(StringHash id, float value)
        {
            ValidateIsCreated();
            var index = FindIndex(m_FloatChannels, id);
            if (index < 0)
                return false;

            m_FloatChannels.ElementAt(index).DefaultValue = value;
            return true;
        }

        /// <summary>
        /// Sets the default value of the integer channel that has an identifier.
        /// </summary>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="value">The default value of the channel.</param>
        /// <returns>True if the builder has a integer channel with the identifier. Otherwise, false, and the builder
        /// doesn't change.</returns>
        public bool TrySetDefaultInt(StringHash id, int value)
        {
            ValidateIsCreated();
            var index = FindIndex(m_IntChannels, id);
            if (index < 0)
                return false;

            m_IntChannels.ElementAt(index).DefaultValue = value;
            return true;
        }

        /// <summary>
        /// Sets the default value of the entity ID channel that has an identifier.
        /// </summary>
        /// <param name="id">The identifier of the channel.</param>
        /// <param name="value">The default value of the channel.</param>
        /// <returns>True if the builder has a entity ID channel with the identifier. Otherwise, false, and the builder
        /// doesn't change.</returns>
        public bool TrySetDefaultEntityId(StringHash id, EntityId value)
        {
            ValidateIsCreated();
            var index = FindIndex(m_EntityIdChannels, id);
            if (index < 0)
                return false;

            m_EntityIdChannels.ElementAt(index).DefaultValue = value;
            return true;
        }

        /// <summary>
        /// Clear all RigBuilder channels.
        /// </summary>
        /// <remarks>
        /// This method also resets the root motion bone that you set in <see cref="RootMotionBoneId"/>.
        /// </remarks>
        public readonly void Clear()
        {
            if (!IsCreated)
                return;

            // The containers point to memory that all copies of the builder share, so clear them through local copies.
            var rootMotionBone = m_RootMotionBone;
            var skeletonNodes = m_SkeletonNodes;
            var translationChannels = m_TranslationChannels;
            var rotationChannels = m_RotationChannels;
            var scaleChannels = m_ScaleChannels;
            var floatChannels = m_FloatChannels;
            var entityIdChannels = m_EntityIdChannels;
            var intChannels = m_IntChannels;
            var socketNodes = m_SocketNodes;

            rootMotionBone.Value = default;
            skeletonNodes.Clear();
            translationChannels.Clear();
            rotationChannels.Clear();
            scaleChannels.Clear();
            floatChannels.Clear();
            entityIdChannels.Clear();
            intChannels.Clear();
            socketNodes.Clear();
        }

        /// <summary>
        /// Disposes the RigBuilder.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;

            m_SkeletonNodes.Dispose();
            m_TranslationChannels.Dispose();
            m_RotationChannels.Dispose();
            m_ScaleChannels.Dispose();
            m_FloatChannels.Dispose();
            m_IntChannels.Dispose();
            m_EntityIdChannels.Dispose();
            m_SocketNodes.Dispose();
            m_RootMotionBone.Dispose();

            m_IsCreated = false;
        }

        /// <summary>
        /// Creates a RigDefinition from the current RigBuilder data.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration
        /// </param>
        /// <returns>The BlobAssetReference of a RigDefinition.</returns>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and no bone of the skeleton has
        /// the identifier that you set in <see cref="RootMotionBoneId"/>.</exception>
        public BlobAssetReference<RigDefinition> ToRigDefinition(Allocator allocator)
        {
            ValidateIsCreated();
            ValidateSkeletonData();
            ValidateRootMotionBone(m_RootMotionBone.Value);

            BlobAssetReference<RigDefinition> rigDefinitionAsset = default;
            CreateRigDefinition(ref this, ref rigDefinitionAsset, (int)allocator);
            return rigDefinitionAsset;
        }

        [BurstCompile]
        static void CreateRigDefinition(ref RigBuilder rigBuilderData, ref BlobAssetReference<RigDefinition> result, int allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            try
            {
                using var translations = new NativeList<LocalTranslationChannel>(Allocator.Temp);
                using var rotations = new NativeList<LocalRotationChannel>(Allocator.Temp);
                using var scales = new NativeList<LocalScaleChannel>(Allocator.Temp);

                GetAnimationChannels(rigBuilderData.m_SkeletonNodes, translations, rotations, scales);
                GetAnimationChannels(rigBuilderData.m_TranslationChannels, translations);
                GetAnimationChannels(rigBuilderData.m_RotationChannels, rotations);
                GetAnimationChannels(rigBuilderData.m_ScaleChannels, scales);

                ref var rig = ref blobBuilder.ConstructRoot<RigDefinition>();

                InitializeSkeletonNodes(ref blobBuilder, rigBuilderData.m_SkeletonNodes, ref rig.Skeleton.ParentIndexes, ref rig.Skeleton.Ids);

                InitializeBindings(ref blobBuilder, translations, ref rig.Bindings.TranslationBindings);
                InitializeBindings(ref blobBuilder, scales, ref rig.Bindings.ScaleBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_FloatChannels, ref rig.Bindings.FloatBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_IntChannels, ref rig.Bindings.IntBindings);
                InitializeBindings(ref blobBuilder, rotations, ref rig.Bindings.RotationBindings);
                InitializeBindings(ref blobBuilder, rigBuilderData.m_EntityIdChannels, ref rig.Bindings.EntityIdBindings);

                rig.SetupBindingSet(
                    translations.Length,
                    rotations.Length,
                    scales.Length,
                    rigBuilderData.m_FloatChannels.Length,
                    rigBuilderData.m_IntChannels.Length,
                    rigBuilderData.m_EntityIdChannels.Length);

                var arrayBuilder = blobBuilder.Allocate(ref rig.DefaultValues, rig.Bindings.StreamSize);

                InitializeDefaultValues<LocalTranslationChannel, float3>(rig.Bindings.TranslationSamplesOffset, ref arrayBuilder, translations);
                InitializeDefaultValues<LocalScaleChannel, float3>(rig.Bindings.ScaleSamplesOffset, ref arrayBuilder, scales);
                InitializeDefaultValues<FloatChannel, float>(rig.Bindings.FloatSamplesOffset, ref arrayBuilder, rigBuilderData.m_FloatChannels);
                InitializeDefaultValues<IntChannel, int>(rig.Bindings.IntSamplesOffset, ref arrayBuilder, rigBuilderData.m_IntChannels);
                InitializeDefaultValues<EntityIdChannel, EntityId>(rig.Bindings.EntityIdSamplesOffset, ref arrayBuilder, rigBuilderData.m_EntityIdChannels);
                InitializeDefaultRotationValues(rig.Bindings.RotationSamplesOffset, rig.Bindings.RotationDataChunkCount, ref arrayBuilder, rotations);

                InitializeSocketValues(ref blobBuilder, rigBuilderData.m_SocketNodes, ref rig.SocketValues);

                if (rigBuilderData.m_SkeletonNodes.IsEmpty)
                    rig.RootMotionBoneIndex = -1;
                else if (StringHash.IsNullOrEmpty(rigBuilderData.m_RootMotionBone.Value))
                    rig.RootMotionBoneIndex = 0;
                else
                    rig.RootMotionBoneIndex = FindSkeletonNodeIndex(rigBuilderData.m_SkeletonNodes, rigBuilderData.m_RootMotionBone.Value);

                result = blobBuilder.CreateBlobAssetReference<RigDefinition>((Allocator)allocator);
            }
            finally
            {
                blobBuilder.Dispose();
            }
        }

        static void GetAnimationChannels(NativeList<SkeletonNode> skeletonNodes, NativeList<LocalTranslationChannel> translationChannels, NativeList<LocalRotationChannel> rotationChannels, NativeList<LocalScaleChannel> scaleChannels)
        {
            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                var localTranslationChannel = new LocalTranslationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalTranslationDefaultValue };
                if (!translationChannels.Contains(localTranslationChannel))
                    translationChannels.Add(localTranslationChannel);

                var localRotationChannel = new LocalRotationChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalRotationDefaultValue };
                if (!rotationChannels.Contains(localRotationChannel))
                    rotationChannels.Add(localRotationChannel);

                var localScaleChannel = new LocalScaleChannel { Id = skeletonNodes[i].Id, DefaultValue = skeletonNodes[i].LocalScaleDefaultValue };
                if (!scaleChannels.Contains(localScaleChannel))
                    scaleChannels.Add(localScaleChannel);
            }
        }

        static void GetAnimationChannels<T>(NativeList<T> animationChannels, NativeList<T> filteredAnimationChannels) where T : unmanaged, IEquatable<T>
        {
            for (var i = 0; i != animationChannels.Length; i++)
            {
                var channel = animationChannels[i];
                if (!filteredAnimationChannels.Contains(channel))
                    filteredAnimationChannels.Add(channel);
            }
        }

        static int FindSkeletonNodeIndex(NativeList<SkeletonNode> skeletonNodes, StringHash id)
        {
            for (var i = 0; i < skeletonNodes.Length; i++)
            {
                if (skeletonNodes[i].Id == id)
                    return i;
            }

            return -1;
        }

        static bool ContainsChannel<T>(NativeList<T> channels, StringHash id) where T : unmanaged, IAnimationChannel
        {
            for (var i = 0; i < channels.Length; i++)
            {
                if (channels[i].Id == id)
                    return true;
            }

            return false;
        }

        static void InitializeSkeletonNodes(ref BlobBuilder blobBuilder, NativeList<SkeletonNode> skeletonNodes, ref BlobArray<int> parentIndices, ref BlobArray<StringHash> skeletonIds)
        {
            if (!skeletonNodes.IsCreated || skeletonNodes.Length == 0)
                return;

            var parentIndicesBuilder = blobBuilder.Allocate(ref parentIndices, skeletonNodes.Length);
            var skeletonIdsBuilder = blobBuilder.Allocate(ref skeletonIds, skeletonNodes.Length);
            for (int i = 0; i < skeletonNodes.Length; i++)
            {
                parentIndicesBuilder[i] = skeletonNodes[i].ParentIndex;
                skeletonIdsBuilder[i] = skeletonNodes[i].Id;
            }
        }

        static void InitializeBindings<T>(ref BlobBuilder blobBuilder, NativeList<T> animationChannels, ref BlobArray<StringHash> bindings)
            where T : unmanaged, IAnimationChannel
        {
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref bindings, animationChannels.Length);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                arrayBuilder[i] = animationChannels[i].Id;
            }
        }

        static unsafe void InitializeDefaultValues<TChannel, TData>(int index, ref BlobBuilderArray<AnimatedData> arrayBuilder, NativeList<TChannel> animationChannels)
            where TChannel : unmanaged, IAnimationChannel<TData>
            where TData : unmanaged
        {
            if (!animationChannels.IsCreated || animationChannels.Length == 0)
                return;

            TData* data = (TData*)((float*)arrayBuilder.GetUnsafePtr() + index);
            for (int i = 0; i < animationChannels.Length; i++)
            {
                data[i] = animationChannels[i].DefaultValue;
            }
        }

        static unsafe void InitializeDefaultRotationValues(int index, int rotationDataChunkCount, ref BlobBuilderArray<AnimatedData> arrayBuilder, NativeList<LocalRotationChannel> rotationChannels)
        {
            if (rotationChannels.IsEmpty)
                return;

            var combinedRotationChannelCount = 0;
            if (rotationChannels.IsCreated)
                combinedRotationChannelCount += rotationChannels.Length;

            using var combinedRotationChannels = new NativeList<quaternion>(combinedRotationChannelCount, Allocator.Temp);
            if (rotationChannels.IsCreated)
            {
                for (var i = 0; i < rotationChannels.Length; i++)
                {
                    combinedRotationChannels.Add(rotationChannels[i].DefaultValue);
                }
            }

            // Fill as SOA 4-wide quaternions
            quaternion4* data = (quaternion4*)((float*)arrayBuilder.GetUnsafePtr() + index);
            int length = combinedRotationChannels.Length >> 2;
            for (int i = 0; i < length; ++i)
            {
                int idx = i << 2;
                data[i] = new quaternion4(
                    math.normalizesafe(combinedRotationChannels[idx + 0]),
                    math.normalizesafe(combinedRotationChannels[idx + 1]),
                    math.normalizesafe(combinedRotationChannels[idx + 2]),
                    math.normalizesafe(combinedRotationChannels[idx + 3])
                );
            }

            // Fill remaining rotations
            for (int i = length << 2; i < combinedRotationChannels.Length; ++i)
            {
                int chunkIdx = i >> 2;
                int subIdx = i & 0x3; // equivalent to % 4;

                quaternion q = math.normalizesafe(combinedRotationChannels[i]);
                ref quaternion4 q4 = ref UnsafeUtility.AsRef<quaternion4>(data + chunkIdx);

                q4.x[subIdx] = q.value.x;
                q4.y[subIdx] = q.value.y;
                q4.z[subIdx] = q.value.z;
                q4.w[subIdx] = q.value.w;
            }

            // Fill the empty quaternions in the last chunk with identity value, to not break blend code.
            var rotationChannelsInMemoryCount = rotationDataChunkCount * 4;
            for(var i = combinedRotationChannels.Length; i < rotationChannelsInMemoryCount ; ++i)
            {
                var chunkIdx = i >> 2;
                var subIdx = i & 0x3;

                ref quaternion4 q4 = ref UnsafeUtility.AsRef<quaternion4>(data + chunkIdx);

                q4.x[subIdx] = 0.0f;
                q4.y[subIdx] = 0.0f;
                q4.z[subIdx] = 0.0f;
                q4.w[subIdx] = 1.0f;
            }
        }

        static unsafe void InitializeSocketValues(ref BlobBuilder blobBuilder, NativeList<SocketNode> socketNodes, ref BlobArray<SocketNode> socketValues)
        {
            if (!socketNodes.IsCreated || socketNodes.Length == 0)
                return;

            var arrayBuilder = blobBuilder.Allocate(ref socketValues, socketNodes.Length);
            UnsafeUtility.MemCpy(arrayBuilder.GetUnsafePtr(), socketNodes.GetUnsafePtr(), UnsafeUtility.SizeOf<SocketNode>() * socketNodes.Length);
        }

        static int FindIndex(NativeList<SkeletonNode> nodes, StringHash id)
        {
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (nodes[i].Id.Equals(id))
                    return i;
            }

            return -1;
        }

        static int FindIndex<T>(NativeList<T> channels, StringHash id) where T : unmanaged, IAnimationChannel
        {
            for (int i = 0; i < channels.Length; ++i)
            {
                if (channels[i].Id.Equals(id))
                    return i;
            }

            return -1;
        }

        static bool TryGet<T>(NativeList<T> channels, StringHash id, out T channel) where T : unmanaged, IAnimationChannel
        {
            var index = FindIndex(channels, id);
            channel = index >= 0 ? channels[index] : default;
            return index >= 0;
        }

        static bool Replace<T>(NativeList<T> channels, in T channel) where T : unmanaged, IAnimationChannel
        {
            var index = FindIndex(channels, channel.Id);
            if (index < 0)
                return false;

            ValidateUniqueId(channels, channel.Id, index);

            channels[index] = channel;
            return true;
        }

        static bool Remove<T>(NativeList<T> channels, StringHash id) where T : unmanaged, IAnimationChannel
        {
            var removed = false;
            for (int i = channels.Length - 1; i >= 0; --i)
            {
                if (channels[i].Id.Equals(id))
                {
                    channels.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        void RemoveSkeletonNodes(NativeArray<bool> removed)
        {
            var nodeCount = m_SkeletonNodes.Length;
            var newIndices = new NativeArray<int>(nodeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var keptCount = 0;
            for (int i = 0; i < nodeCount; ++i)
                newIndices[i] = removed[i] ? -1 : keptCount++;

            for (int i = 0; i < nodeCount; ++i)
            {
                if (removed[i])
                    continue;

                var skeletonNode = m_SkeletonNodes[i];
                if (skeletonNode.ParentIndex >= 0 && skeletonNode.ParentIndex < nodeCount)
                    skeletonNode.ParentIndex = newIndices[skeletonNode.ParentIndex];
                m_SkeletonNodes[newIndices[i]] = skeletonNode;
            }

            m_SkeletonNodes.Resize(keptCount, NativeArrayOptions.UninitializedMemory);

            for (int i = m_SocketNodes.Length - 1; i >= 0; --i)
            {
                var boneIndex = m_SocketNodes[i].BoneParentIndex;
                if (boneIndex < 0 || boneIndex >= nodeCount)
                    continue;

                if (removed[boneIndex])
                    m_SocketNodes.RemoveAt(i);
                else
                    m_SocketNodes.ElementAt(i).BoneParentIndex = newIndices[boneIndex];
            }

            newIndices.Dispose();
        }

        static bool ContainsId(NativeList<SkeletonNode> nodes, StringHash id, int ignoredIndex)
        {
            for (int i = 0; i < nodes.Length; ++i)
            {
                if (i != ignoredIndex && nodes[i].Id.Equals(id))
                    return true;
            }

            return false;
        }

        static bool ContainsId<T>(NativeList<T> channels, StringHash id, int ignoredIndex) where T : unmanaged, IAnimationChannel
        {
            for (int i = 0; i < channels.Length; ++i)
            {
                if (i != ignoredIndex && channels[i].Id.Equals(id))
                    return true;
            }

            return false;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateSkeletonNode(in SkeletonNode node, int index)
        {
            if (index == 0 && node.ParentIndex != -1)
            {
                throw new ArgumentException(
                    $"SkeletonNode[0] has an invalid ParentIndex '{node.ParentIndex}': The first SkeletonNode is the root, so its ParentIndex must be -1.");
            }

            if (index > 0 && (node.ParentIndex < 0 || node.ParentIndex >= index))
            {
                throw new ArgumentException(
                    $"SkeletonNode[{index}] has an invalid ParentIndex '{node.ParentIndex}': The parent must be a SkeletonNode with a lower index, between 0 and {index - 1}.");
            }

            if (ContainsId(m_SkeletonNodes, node.Id, index))
                throw new ArgumentException($"SkeletonNode[{index}] has the same Id as another SkeletonNode.");

            if (ContainsId(m_TranslationChannels, node.Id, -1) || ContainsId(m_RotationChannels, node.Id, -1) || ContainsId(m_ScaleChannels, node.Id, -1))
                throw new ArgumentException($"SkeletonNode[{index}] has the same Id as a translation, rotation, or scale channel. A bone already has these channels.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsNotBoneId(StringHash id)
        {
            if (ContainsId(m_SkeletonNodes, id, -1))
                throw new ArgumentException("A SkeletonNode has the Id of the channel. A bone already has a translation, rotation, and scale channel.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateUniqueId<T>(NativeList<T> channels, StringHash id, int index) where T : unmanaged, IAnimationChannel
        {
            if (ContainsId(channels, id, index))
                throw new ArgumentException("The RigBuilder already has a channel of this type with the same Id.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateUniqueSocketId(StringHash id)
        {
            for (int i = 0; i < m_SocketNodes.Length; ++i)
            {
                if (m_SocketNodes[i].Id.Equals(id))
                    throw new ArgumentException($"SocketNode[{i}] already has the same Id.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateSkeletonData()
        {
            if (m_SkeletonNodes.IsEmpty)
                return;

            var skeletonNodeCount = m_SkeletonNodes.Length;
            if (skeletonNodeCount > 0 && m_SkeletonNodes[0].ParentIndex != -1)
                throw new ArgumentException("First skeleton node is expected to be the root node.");

            for (int i = 1; i < skeletonNodeCount; ++i)
            {
                ref readonly var skeletonNode = ref m_SkeletonNodes.ElementAt(i);

                if (skeletonNode.ParentIndex == -1)
                {
                    throw new ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex: You can have only one root node in your skeleton."
                    );
                }

                if (skeletonNode.ParentIndex == i)
                {
                    throw new ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNode.ParentIndex}': The SkeletonNode parent cannot be itself."
                    );
                }

                if (skeletonNode.ParentIndex < 0 || skeletonNode.ParentIndex >= skeletonNodeCount)
                {
                    throw new ArgumentOutOfRangeException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNode.ParentIndex}': value must be between 0 and {skeletonNodeCount - 1}."
                    );
                }

                if (skeletonNode.ParentIndex > i)
                {
                    throw new ArgumentException(
                        $"SkeletonNode[{i}] has an invalid ParentIndex '{skeletonNode.ParentIndex}': You must add the parent of a SkeletonNode before the SkeletonNode."
                    );
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateRootMotionBone(StringHash id)
        {
            if (StringHash.IsNullOrEmpty(id))
                return;

            if (FindSkeletonNodeIndex(m_SkeletonNodes, id) != -1)
                return;

            if (ContainsChannel(m_TranslationChannels, id) ||
                ContainsChannel(m_RotationChannels, id) ||
                ContainsChannel(m_ScaleChannels, id) ||
                ContainsChannel(m_FloatChannels, id) ||
                ContainsChannel(m_IntChannels, id) ||
                ContainsChannel(m_EntityIdChannels, id))
            {
                throw new ArgumentException(
                    $"The root motion bone '{id.Id}' is a channel, not a bone. The root motion bone must be a bone of the skeleton.");
            }

            throw new ArgumentException(
                $"The root motion bone '{id.Id}' isn't in the rig. No bone of the skeleton has this identifier.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsCreated()
        {
            if (!m_IsCreated)
                throw new NullReferenceException("RigBuilder is not properly created.");
        }
    }
}
