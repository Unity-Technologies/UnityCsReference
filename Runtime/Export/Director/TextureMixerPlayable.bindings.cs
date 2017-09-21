// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.Playables
{
    [NativeHeader("Runtime/Export/Director/TextureMixerPlayable.bindings.h")]
    [NativeHeader("Runtime/Graphics/Director/TextureMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("TextureMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct TextureMixerPlayable : IPlayable, IEquatable<TextureMixerPlayable>
    {
        PlayableHandle m_Handle;

        public static TextureMixerPlayable Create(PlayableGraph graph)
        {
            var handle = CreateHandle(graph);
            return new TextureMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateTextureMixerPlayableInternal(ref graph, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal TextureMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<TextureMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an TextureMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(TextureMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator TextureMixerPlayable(Playable playable)
        {
            return new TextureMixerPlayable(playable.GetHandle());
        }

        public bool Equals(TextureMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        extern private static bool CreateTextureMixerPlayableInternal(ref PlayableGraph graph, ref PlayableHandle handle);

    }
}
