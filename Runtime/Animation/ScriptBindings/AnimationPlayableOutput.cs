// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public partial struct AnimationPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        public static AnimationPlayableOutput Create(PlayableGraph graph, string name, Animator target)
        {
            PlayableOutputHandle handle;
            if (!AnimationPlayableGraphExtensions.InternalCreateAnimationOutput(ref graph, name, out handle))
                return AnimationPlayableOutput.Null;

            AnimationPlayableOutput output = new AnimationPlayableOutput(handle);
            output.SetTarget(target);

            return output;
        }

        internal AnimationPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<AnimationPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimationPlayableOutput.");
            }

            m_Handle = handle;
        }

        public static AnimationPlayableOutput Null
        {
            get { return new AnimationPlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(AnimationPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator AnimationPlayableOutput(PlayableOutput output)
        {
            return new AnimationPlayableOutput(output.GetHandle());
        }
    }
}
