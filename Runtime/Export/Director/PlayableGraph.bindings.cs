// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
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

    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Export/Director/PlayableGraph.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableGraph
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

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

        public static PlayableGraph Create()
        {
            return Create(null);
        }

        // Bindings methods.
        extern public static PlayableGraph Create(string name);

        [FreeFunction("PlayableGraphBindings::Destroy", HasExplicitThis = true, ThrowsException = true)]
        extern public void Destroy();

        extern public bool IsValid();

        [FreeFunction("PlayableGraphBindings::IsPlaying", HasExplicitThis = true, ThrowsException = true)]
        extern public bool IsPlaying();

        [FreeFunction("PlayableGraphBindings::IsDone", HasExplicitThis = true, ThrowsException = true)]
        extern public bool IsDone();

        [FreeFunction("PlayableGraphBindings::Play", HasExplicitThis = true, ThrowsException = true)]
        extern public void Play();

        [FreeFunction("PlayableGraphBindings::Stop", HasExplicitThis = true, ThrowsException = true)]
        extern public void Stop();

        [FreeFunction("PlayableGraphBindings::Evaluate", HasExplicitThis = true, ThrowsException = true)]
        extern public void Evaluate([DefaultValue("0")] float deltaTime);

        [FreeFunction("PlayableGraphBindings::GetTimeUpdateMode", HasExplicitThis = true, ThrowsException = true)]
        extern public DirectorUpdateMode GetTimeUpdateMode();

        [FreeFunction("PlayableGraphBindings::SetTimeUpdateMode", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetTimeUpdateMode(DirectorUpdateMode value);

        [FreeFunction("PlayableGraphBindings::GetResolver", HasExplicitThis = true, ThrowsException = true)]
        extern public IExposedPropertyTable GetResolver();

        [FreeFunction("PlayableGraphBindings::SetResolver", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetResolver(IExposedPropertyTable value);

        [FreeFunction("PlayableGraphBindings::GetPlayableCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetPlayableCount();

        [FreeFunction("PlayableGraphBindings::GetRootPlayableCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetRootPlayableCount();

        [FreeFunction("PlayableGraphBindings::GetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern public int GetOutputCount();

        [FreeFunction("PlayableGraphBindings::CreatePlayableHandle", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle CreatePlayableHandle();

        [FreeFunction("PlayableGraphBindings::CreateScriptOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CreateScriptOutputInternal(string name, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::GetRootPlayableInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle GetRootPlayableInternal(int index);

        [FreeFunction("PlayableGraphBindings::DestroyOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern internal void DestroyOutputInternal(PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::GetOutputInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool GetOutputInternal(int index, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::GetOutputCountByTypeInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetOutputCountByTypeInternal(Type outputType);

        [FreeFunction("PlayableGraphBindings::GetOutputByTypeInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool GetOutputByTypeInternal(Type outputType, int index, out PlayableOutputHandle handle);

        [FreeFunction("PlayableGraphBindings::ConnectInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private bool ConnectInternal(PlayableHandle source, int sourceOutputPort, PlayableHandle destination, int destinationInputPort);

        [FreeFunction("PlayableGraphBindings::DisconnectInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DisconnectInternal(PlayableHandle playable, int inputPort);

        [FreeFunction("PlayableGraphBindings::DestroyPlayableInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DestroyPlayableInternal(PlayableHandle playable);

        [FreeFunction("PlayableGraphBindings::DestroySubgraphInternal", HasExplicitThis = true, ThrowsException = true)]
        extern private void DestroySubgraphInternal(PlayableHandle playable);

        [FreeFunction("PlayableGraphBindings::GetEditorName", HasExplicitThis = true, ThrowsException = true)]
        extern public string GetEditorName();
    }
}
