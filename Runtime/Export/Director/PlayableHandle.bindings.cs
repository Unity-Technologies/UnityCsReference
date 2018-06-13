// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Object = UnityEngine.Object;


namespace UnityEngine.Playables
{
    public enum PlayState
    {
        Paused = 0,
        Playing = 1,
        Delayed = 2
    }

    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Export/Director/PlayableHandle.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableHandle : IEquatable<PlayableHandle>
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

        internal T GetObject<T>()
            where T : class, IPlayableBehaviour
        {
            if (!IsValid())
                return null;

            var playable = GetScriptInstance();
            if (playable == null)
                return null;

            return (T)playable;
        }

        [VisibleToOtherModules]
        internal bool IsPlayableOfType<T>()
        {
            return GetPlayableType() == typeof(T);
        }

        static readonly PlayableHandle m_Null = new PlayableHandle();
        public static PlayableHandle Null
        {
            get { return m_Null; }
        }

        internal Playable GetInput(int inputPort)
        {
            return new Playable(GetInputHandle(inputPort));
        }

        internal Playable GetOutput(int outputPort)
        {
            return new Playable(GetOutputHandle(outputPort));
        }

        internal bool SetInputWeight(int inputIndex, float weight)
        {
            if (CheckInputBounds(inputIndex))
            {
                SetInputWeightFromIndex(inputIndex, weight);
                return true;
            }
            return false;
        }

        internal float GetInputWeight(int inputIndex)
        {
            if (CheckInputBounds(inputIndex))
            {
                return GetInputWeightFromIndex(inputIndex);
            }
            return 0.0f;
        }

        internal void Destroy()
        {
            GetGraph().DestroyPlayable(new Playable(this));
        }

        public static bool operator==(PlayableHandle x, PlayableHandle y) { return CompareVersion(x, y); }
        public static bool operator!=(PlayableHandle x, PlayableHandle y) { return !CompareVersion(x, y); }

        public override bool Equals(object p)
        {
            return p is PlayableHandle && Equals((PlayableHandle)p);
        }

        public bool Equals(PlayableHandle other)
        {
            return CompareVersion(this, other);
        }

        public override int GetHashCode() { return m_Handle.GetHashCode() ^ m_Version.GetHashCode(); }

        static internal bool CompareVersion(PlayableHandle lhs, PlayableHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }

        internal bool CheckInputBounds(int inputIndex)
        {
            return CheckInputBounds(inputIndex, false);
        }

        internal bool CheckInputBounds(int inputIndex, bool acceptAny)
        {
            if (inputIndex == -1 && acceptAny)
                return true;

            if (inputIndex < 0)
            {
                throw new IndexOutOfRangeException("Index must be greater than 0");
            }

            if (GetInputCount() <= inputIndex)
            {
                throw new IndexOutOfRangeException("inputIndex " + inputIndex +  " is greater than the number of available inputs (" + GetInputCount() + ").");
            }

            return true;
        }

        [VisibleToOtherModules]
        extern internal bool IsNull();

        [VisibleToOtherModules]
        extern internal bool IsValid();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPlayableType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetPlayableType();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetJobType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetJobType();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetScriptInstance", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetScriptInstance(object scriptInstance);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanChangeInputs", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanChangeInputs();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanSetWeights", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanSetWeights();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::CanDestroy", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool CanDestroy();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPlayState", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayState GetPlayState();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::Play", HasExplicitThis = true, ThrowsException = true)]
        extern internal void Play();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::Pause", HasExplicitThis = true, ThrowsException = true)]
        extern internal void Pause();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetSpeed", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetSpeed();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetSpeed", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetSpeed(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTime(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::IsDone", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool IsDone();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDone", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDone(bool value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetDuration", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetDuration();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDuration", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDuration(double value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPropagateSetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool GetPropagateSetTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetPropagateSetTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetPropagateSetTime(bool value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetGraph", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableGraph GetGraph();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetInputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetInputCount();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetInputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetInputCount(int value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetOutputCount();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetOutputCount", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetOutputCount(int value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetInputWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetInputWeight(PlayableHandle input, float weight);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetDelay", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetDelay(double delay);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetDelay", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetDelay();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::IsDelayed", HasExplicitThis = true, ThrowsException = true)]
        extern internal bool IsDelayed();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetPreviousTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal double GetPreviousTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetLeadTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetLeadTime(float value);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetLeadTime", HasExplicitThis = true, ThrowsException = true)]
        extern internal float GetLeadTime();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTraversalMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableTraversalMode GetTraversalMode();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTraversalMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTraversalMode(PlayableTraversalMode mode);

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetAdditionalPayload", HasExplicitThis = true, ThrowsException = true)]
        extern internal IntPtr GetAdditionalPayload();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::GetTimeWrapMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal DirectorWrapMode GetTimeWrapMode();

        [VisibleToOtherModules]
        [FreeFunction("PlayableHandleBindings::SetTimeWrapMode", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetTimeWrapMode(DirectorWrapMode mode);

        [FreeFunction("PlayableHandleBindings::GetScriptInstance", HasExplicitThis = true, ThrowsException = true)]
        extern private object GetScriptInstance();

        [FreeFunction("PlayableHandleBindings::GetInputHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private PlayableHandle GetInputHandle(int index);

        [FreeFunction("PlayableHandleBindings::GetOutputHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private PlayableHandle GetOutputHandle(int index);

        [FreeFunction("PlayableHandleBindings::SetInputWeightFromIndex", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetInputWeightFromIndex(int index, float weight);

        [FreeFunction("PlayableHandleBindings::GetInputWeightFromIndex", HasExplicitThis = true, ThrowsException = true)]
        extern private float GetInputWeightFromIndex(int index);
    }
}
