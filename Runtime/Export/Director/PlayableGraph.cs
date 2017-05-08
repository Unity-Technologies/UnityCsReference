// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Playables
{
    public partial struct PlayableGraph
    {
        public bool Connect<U, V>(U input, int inputPort, V output, int outputPort)
            where U : struct, IPlayable
            where V : struct, IPlayable
        {
            return ConnectInternal(ref this, input.GetHandle(), inputPort, output.GetHandle(), outputPort);
        }

        public void Disconnect<U>(U input, int inputPort)
            where U : struct, IPlayable
        {
            DisconnectInternal(ref this, input.GetHandle(), inputPort);
        }

        public void DestroyPlayable<U>(U playable)
            where U : struct, IPlayable
        {
            DestroyPlayableInternal(ref this, playable.GetHandle());
        }

        public void DestroySubgraph<U>(U playable)
            where U : struct, IPlayable
        {
            DestroySubgraphInternal(ref this, playable.GetHandle());
        }

        public void DestroyOutput<U>(U output)
            where U : struct, IPlayableOutput
        {
            DestroyOutputInternal(ref this, output.GetHandle());
        }

        public int GetOutputCount()
        {
            return GetOutputCountInternal(ref this);
        }

        public int GetOutputCountByType<T>()
            where T : struct, IPlayableOutput
        {
            return GetOutputCountByTypeInternal(ref this, typeof(T));
        }

        public PlayableOutput GetOutput(int index)
        {
            PlayableOutputHandle handle;
            if (!GetOutputInternal(ref this, index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }

        public PlayableOutput GetOutputByType<T>(int index)
            where T : struct, IPlayableOutput
        {
            PlayableOutputHandle handle;
            if (!GetOutputByTypeInternal(ref this, typeof(T), index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }
    }
}
