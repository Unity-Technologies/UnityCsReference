// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    // This must always be in sync with DirectorUpdateMode in Runtime/Director/Core/PlayableTypes.h
    public enum DirectorUpdateMode
    {
        DSPClock = 0,
        GameTime = 1,
        UnscaledGameTime = 2,
        Manual = 3
    }

    public enum DataStreamType
    {
        Animation = 0,
        Audio = 1,
        Texture = 2,
        None = 3
    }

    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [UsedByNativeCode]
    public struct PlayableGraph
    {
        internal IntPtr m_Handle;
        internal Int32  m_Version;

        public Playable GetRootPlayable(int index)
        {
            PlayableHandle handle = GetRootPlayableInternal(index);
            return new Playable(handle);
        }

        public bool Connect<U, V>(U source, int sourceOutputPort, V destination, int destinationInputPort)
            where U : struct, IPlayable
            where V : struct, IPlayable
        {
            return ConnectInternal(source.GetHandle(), sourceOutputPort, destination.GetHandle(), destinationInputPort);
        }

        public void Disconnect<U>(U input, int inputPort)
            where U : struct, IPlayable
        {
            DisconnectInternal(input.GetHandle(), inputPort);
        }

        public void DestroyPlayable<U>(U playable)
            where U : struct, IPlayable
        {
            DestroyPlayableInternal(playable.GetHandle());
        }

        public void DestroySubgraph<U>(U playable)
            where U : struct, IPlayable
        {
            DestroySubgraphInternal(playable.GetHandle());
        }

        public void DestroyOutput<U>(U output)
            where U : struct, IPlayableOutput
        {
            DestroyOutputInternal(output.GetHandle());
        }

        public int GetOutputCountByType<T>()
            where T : struct, IPlayableOutput
        {
            return GetOutputCountByTypeInternal(typeof(T));
        }

        public PlayableOutput GetOutput(int index)
        {
            PlayableOutputHandle handle;
            if (!GetOutputInternal(index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }

        public PlayableOutput GetOutputByType<T>(int index)
            where T : struct, IPlayableOutput
        {
            PlayableOutputHandle handle;
            if (!GetOutputByTypeInternal(typeof(T), index, out handle))
                return PlayableOutput.Null;
            return new PlayableOutput(handle);
        }

        public void Evaluate()
        {
            Evaluate(0);
        }

        // Bindings methods.
        extern public static PlayableGraph Create();
        extern public void Destroy();
        extern public bool IsValid();
        extern public bool IsPlaying();
        extern public bool IsDone();
        extern public void Play();
        extern public void Stop();
        extern public void Evaluate([DefaultValue("0")] float deltaTime);
        extern public DirectorUpdateMode GetTimeUpdateMode();
        extern public void SetTimeUpdateMode(DirectorUpdateMode value);
        extern public IExposedPropertyTable GetResolver();
        extern public void SetResolver(IExposedPropertyTable value);
        extern public int GetPlayableCount();
        extern public int GetRootPlayableCount();
        extern public int GetOutputCount();

        extern internal PlayableHandle CreatePlayableHandle();
        extern internal bool CreateScriptOutputInternal(string name, out PlayableOutputHandle handle);
        extern internal PlayableHandle GetRootPlayableInternal(int index);
        extern internal void DestroyOutputInternal(PlayableOutputHandle handle);

        extern private bool GetOutputInternal(int index, out PlayableOutputHandle handle);
        extern private int GetOutputCountByTypeInternal(Type outputType);
        extern private bool GetOutputByTypeInternal(Type outputType, int index, out PlayableOutputHandle handle);
        extern private bool ConnectInternal(PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort);
        extern private void DisconnectInternal(PlayableHandle playable, int inputPort);
        extern private void DestroyPlayableInternal(PlayableHandle playable);
        extern private void DestroySubgraphInternal(PlayableHandle playable);
    }
}
