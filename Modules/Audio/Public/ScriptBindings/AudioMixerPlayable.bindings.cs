// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Audio
{
    ///<summary>An implementation of <see cref="IPlayable" /> that controls an audio mixer.</summary>
    ///<remarks>NOTE: You can use <see cref="PlayableExtensions" /> methods with AudioMixerPlayable objects.</remarks>
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Audio/Public/Director/AudioMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AudioMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct AudioMixerPlayable : IPlayable, IEquatable<AudioMixerPlayable>
    {
        PlayableHandle m_Handle;

        ///<summary>Creates an <see cref="AudioMixerPlayable" /> in the <see cref="PlayableGraph" />.</summary>
        ///<param name="graph">The <see cref="PlayableGraph" /> that will contain the new <see cref="AudioMixerPlayable" />.</param>
        ///<param name="inputCount">The number of inputs that the mixer will update.</param>
        ///<param name="normalizeInputVolumes">Obsolete. This parameter has no effect and will be removed.</param>
        ///<returns>Returns a new <see cref="AudioMixerPlayable" /> linked to the <see cref="PlayableGraph" />.</returns>
        public static AudioMixerPlayable Create(PlayableGraph graph, int inputCount = 0, bool normalizeInputVolumes = false)
        {
            var handle = CreateHandle(graph, inputCount, normalizeInputVolumes);
            return new AudioMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount, bool normalizeInputVolumes)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateAudioMixerPlayableInternal(ref graph, normalizeInputVolumes, ref handle))
                return PlayableHandle.Null;

            handle.SetInputCount(inputCount);
            return handle;
        }

        internal AudioMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AudioMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AudioMixerPlayable.");
            }

            m_Handle = handle;
        }

        ///<exclude />
        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        ///<exclude />
        public static implicit operator Playable(AudioMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        ///<exclude />
        public static explicit operator AudioMixerPlayable(Playable playable)
        {
            return new AudioMixerPlayable(playable.GetHandle());
        }

        ///<exclude />
        public bool Equals(AudioMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }


        [NativeMethod(ThrowsException = true)]
        extern private static bool CreateAudioMixerPlayableInternal(ref PlayableGraph graph, bool normalizeInputVolumes, ref PlayableHandle handle);

    }
}
