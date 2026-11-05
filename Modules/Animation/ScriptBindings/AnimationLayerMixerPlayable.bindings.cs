// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    ///<summary>An implementation of <see cref="IPlayable" /> that controls an animation layer mixer.</summary>
    ///<remarks>NOTE: You can use <see cref="PlayableExtensions" /> methods with AnimationLayerMixerPlayable objects.</remarks>
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationLayerMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationLayerMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AnimationLayerMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct AnimationLayerMixerPlayable : IPlayable, IEquatable<AnimationLayerMixerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimationLayerMixerPlayable m_NullPlayable = new AnimationLayerMixerPlayable(PlayableHandle.Null);
        ///<summary>Returns an invalid AnimationLayerMixerPlayable.</summary>
        public static AnimationLayerMixerPlayable Null { get { return m_NullPlayable; } }

        public static AnimationLayerMixerPlayable Create(PlayableGraph graph, int inputCount = 0)
        {
            return Create(graph, inputCount,true);
        }

        ///<summary>Creates an <see cref="AnimationLayerMixerPlayable" /> in the <see cref="PlayableGraph" />.</summary>
        ///<param name="graph">The <see cref="PlayableGraph" /> that will contain the new <see cref="AnimationLayerMixerPlayable" />.</param>
        ///<param name="inputCount">The number of layers.</param>
        ///<param name="singleLayerOptimization">This optimization automatically sets the weight of the first animation layer to 1. Set to true If your layer mixer has a single animation layer and you want to bypass unnecessary weight calculations. This optimization is automatically set to false if your layer mixer has multiple animation layers.</param>
        ///<returns>A new <see cref="AnimationLayerMixerPlayable" /> linked to the <see cref="PlayableGraph" />.</returns>
        public static AnimationLayerMixerPlayable Create(PlayableGraph graph, int inputCount ,bool singleLayerOptimization)
        {
            var handle = CreateHandle(graph, inputCount);
            var mixer = new AnimationLayerMixerPlayable(handle, singleLayerOptimization);
            return mixer;
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount = 0)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;
            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AnimationLayerMixerPlayable(PlayableHandle handle, bool singleLayerOptimization = true)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimationLayerMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationLayerMixerPlayable.");

                SetSingleLayerOptimizationInternal(ref handle, singleLayerOptimization);
            }
            m_Handle = handle;
        }

        ///<exclude />
        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        ///<exclude />
        public static implicit operator Playable(AnimationLayerMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        ///<exclude />
        public static explicit operator AnimationLayerMixerPlayable(Playable playable)
        {
            return new AnimationLayerMixerPlayable(playable.GetHandle());
        }

        ///<exclude />
        public bool Equals(AnimationLayerMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        ///<summary>Returns true if the layer is additive, false otherwise.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>True if the layer is additive, false otherwise.</returns>
        public bool IsLayerAdditive(uint layerIndex)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            return IsLayerAdditiveInternal(ref m_Handle, layerIndex);
        }

        ///<summary>Specifies whether a layer is additive or not. Additive layers blend with previous layers.</summary>
        ///<remarks>By default, layers are not additive and override the animation from previous layers.</remarks>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="value">Whether the layer is additive or not. Set to <c>true</c> for an additive blend, or <c>false</c> for a regular blend.</param>
        public void SetLayerAdditive(uint layerIndex, bool value)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            SetLayerAdditiveInternal(ref m_Handle, layerIndex, value);
        }

        ///<summary>Sets the mask for the current layer.</summary>
        ///<remarks>This function generates a layer mask from the specified AvatarMask, and applies it to the specified Layer index. If you change the AvatarMask, you need to call this function again to update the layer mask.</remarks>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="mask">The AvatarMask used to create the new LayerMask.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///using UnityEngine.Playables;
        ///using UnityEngine.Animations;
        ///
        ///public class LayerMixerPlayable : MonoBehaviour
        ///{
        ///    public AnimationClip clip1;
        ///    public AnimationClip clip2;
        ///    public Transform leftShoulder;
        ///
        ///    PlayableGraph m_Graph;
        ///    AnimationLayerMixerPlayable m_Mixer;
        ///
        ///    public float mixLevel = 0.5f;
        ///
        ///    AvatarMask mask;
        ///
        ///    public void Start()
        ///    {
        ///        Animator animator = GetComponent<Animator>();
        ///
        ///        mask = new AvatarMask();
        ///        mask.AddTransformPath(leftShoulder, true);
        ///
        ///        m_Graph = PlayableGraph.Create();
        ///        var playableOutput = AnimationPlayableOutput.Create(m_Graph, "LayerMixer", animator);
        ///        playableOutput.SetSourcePlayable(m_Mixer);
        ///
        ///        // Create two clip playables
        ///        var clipPlayable1 = AnimationClipPlayable.Create(m_Graph, clip1);
        ///        var clipPlayable2 = AnimationClipPlayable.Create(m_Graph, clip2);
        ///
        ///        // Create mixer playable
        ///        m_Mixer = AnimationLayerMixerPlayable.Create(m_Graph, 2);
        ///
        ///        // Create two layers, second is setup to override the first layer and affect only left shoulder and childs
        ///        m_Mixer.ConnectInput(0, clipPlayable1, 0, 1.0f);
        ///        m_Mixer.ConnectInput(1, clipPlayable2, 0, mixLevel);
        ///
        ///        m_Mixer.SetLayerMaskFromAvatarMask(1, mask);
        ///
        ///        m_Graph.Play();
        ///    }
        ///
        ///    public void Update()
        ///    {
        ///        m_Mixer.SetInputWeight(1, mixLevel);
        ///    }
        ///
        ///    public void OnDestroy()
        ///    {
        ///        m_Graph.Destroy();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetLayerMaskFromAvatarMask(uint layerIndex, AvatarMask mask)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException("layerIndex", String.Format("layerIndex {0} must be in the range of 0 to {1}.", layerIndex, m_Handle.GetInputCount() - 1));

            if (mask == null)
                throw new System.ArgumentNullException("mask");

            SetLayerMaskFromAvatarMaskInternal(ref m_Handle, layerIndex, mask);
        }

        // Throws InvalidOperationException when the layer mask has more transforms than the AvatarMaskConstant format allows.
        internal int GetLayerMaskByteCount(uint layerIndex)
        {
            ThrowIfLayerIndexOutOfRange(layerIndex);

            return GetLayerMaskByteCountInternal(ref m_Handle, layerIndex);
        }

        // Writes the layer mask into destination; returns false when destination is smaller than GetLayerMaskByteCount.
        internal bool TryGetLayerMask(uint layerIndex, Span<byte> destination, out AvatarMaskConstant mask)
        {
            ThrowIfLayerIndexOutOfRange(layerIndex);

            int bytesWritten;
            unsafe
            {
                fixed (byte* ptr = destination)
                    bytesWritten = WriteLayerMaskInternal(ref m_Handle, layerIndex, ptr, destination.Length);
            }

            if (bytesWritten == 0)
            {
                mask = default;
                return false;
            }

            mask = AvatarMaskConstant.FromValidatedBytes(destination.Slice(0, bytesWritten));
            return true;
        }

        internal void SetLayerMask(uint layerIndex, in AvatarMaskConstant mask)
        {
            ThrowIfLayerIndexOutOfRange(layerIndex);

            var bytes = mask.Bytes;
            unsafe
            {
                fixed (byte* ptr = bytes)
                    SetLayerMaskFromBytesInternal(ref m_Handle, layerIndex, ptr, bytes.Length);
            }
        }

        void ThrowIfLayerIndexOutOfRange(uint layerIndex)
        {
            if (layerIndex >= m_Handle.GetInputCount())
                throw new ArgumentOutOfRangeException(nameof(layerIndex), $"layerIndex {layerIndex} must be in the range of 0 to {m_Handle.GetInputCount() - 1}.");
        }

        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);

        [NativeMethod(ThrowsException = true)]
        extern private static bool IsLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerAdditiveInternal(ref PlayableHandle handle, uint layerIndex, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetSingleLayerOptimizationInternal(ref PlayableHandle handle, bool value);

        [NativeMethod(ThrowsException = true)]
        extern private static void SetLayerMaskFromAvatarMaskInternal(ref PlayableHandle handle, uint layerIndex, AvatarMask mask);

        [NativeMethod(ThrowsException = true)]
        extern private static int GetLayerMaskByteCountInternal(ref PlayableHandle handle, uint layerIndex);

        [NativeMethod(ThrowsException = true)]
        extern private static unsafe int WriteLayerMaskInternal(ref PlayableHandle handle, uint layerIndex, byte* destination, int capacity);

        [NativeMethod(ThrowsException = true)]
        extern private static unsafe void SetLayerMaskFromBytesInternal(ref PlayableHandle handle, uint layerIndex, byte* source, int size);
    }
}
