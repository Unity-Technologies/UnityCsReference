// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Playables
{
    internal struct DataPlayable<TPayload> : IPlayable, IEquatable<DataPlayable<TPayload>>
        where TPayload : struct
    {
        private PlayableHandle m_Handle;

        static readonly DataPlayable<TPayload> m_NullPlayable = new DataPlayable<TPayload>(PlayableHandle.Null);
        public static DataPlayable<TPayload> Null { get { return m_NullPlayable; } }

        public static DataPlayable<TPayload> Create(PlayableGraph graph, int inputCount = 0)
        {
            return Create(graph, default, inputCount);
        }

        public static DataPlayable<TPayload> Create(PlayableGraph graph, TPayload payload, int inputCount = 0)
        {
            var handle = CreateHandle(graph, payload, inputCount);
            return new DataPlayable<TPayload>(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, TPayload payload, int inputCount)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!DataPlayableBindings.CreateHandleInternal(graph, ref handle))
                return PlayableHandle.Null;

            handle.SetInputCount(inputCount);

            handle.SetScriptInstance(payload);

            return handle;
        }

        internal DataPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (typeof(TPayload) != handle.GetPlayableType())
                    throw new InvalidCastException($"Incompatible handle: Trying to assign a playable data of type `{ handle.GetPlayableType() }` that is not compatible with the Payload of type `{ typeof(TPayload) }`.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public TPayload GetPayload()
        {
            return m_Handle.GetPayload<TPayload>();
        }

        public void SetPayload(TPayload payload)
        {
            m_Handle.SetPayload(payload);
        }

        public static implicit operator Playable(DataPlayable<TPayload> playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator DataPlayable<TPayload>(Playable playable)
        {
            return new DataPlayable<TPayload>(playable.GetHandle());
        }

        public bool Equals(DataPlayable<TPayload> other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
