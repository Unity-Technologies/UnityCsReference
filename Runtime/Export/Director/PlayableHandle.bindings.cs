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
    [UsedByNativeCode]
    public struct PlayableHandle
    {
        internal IntPtr m_Handle;
        internal Int32 m_Version;

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

        internal bool IsPlayableOfType<T>()
        {
            return GetPlayableType() == typeof(T);
        }

        public static PlayableHandle Null
        {
            get { return new PlayableHandle() { m_Version = 10 }; }
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
            if (!(p is PlayableHandle))
                return false;
            return CompareVersion(this, (PlayableHandle)p);
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

        // Bindings methods.
        extern internal bool IsValid();
        extern internal Type GetPlayableType();
        extern internal void SetScriptInstance(object scriptInstance);
        extern internal bool CanChangeInputs();
        extern internal bool CanSetWeights();
        extern internal bool CanDestroy();
        extern internal PlayState GetPlayState();
        extern internal void Play();
        extern internal void Pause();
        extern internal double GetSpeed();
        extern internal void SetSpeed(double value);
        extern internal double GetTime();
        extern internal void SetTime(double value);
        extern internal bool IsDone();
        extern internal void SetDone(bool value);
        extern internal double GetDuration();
        extern internal void SetDuration(double value);
        extern internal bool GetPropagateSetTime();
        extern internal void SetPropagateSetTime(bool value);
        extern internal PlayableGraph GetGraph();
        extern internal int GetInputCount();
        extern internal void SetInputCount(int value);
        extern internal int GetOutputCount();
        extern internal void SetOutputCount(int value);
        extern internal void SetInputWeight(PlayableHandle input, float weight);
        extern internal void SetDelay(double delay);
        extern internal double GetDelay();
        extern internal bool IsDelayed();
        extern internal double GetPreviousTime();

        extern private object GetScriptInstance();
        extern private PlayableHandle GetInputHandle(int index);
        extern private PlayableHandle GetOutputHandle(int index);
        extern private void SetInputWeightFromIndex(int index, float weight);
        extern private float GetInputWeightFromIndex(int index);
    }
}
