// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Playables
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameData
    {
        [Flags]
        internal enum Flags
        {
            Evaluate    = 1,
            SeekOccured = 2,
            Loop        = 4,
            Hold        = 8,
            EffectivePlayStateDelayed = 16,
            EffectivePlayStatePlaying = 32
        }

        public enum EvaluationType
        {
            Evaluate,
            Playback
        }

        internal ulong m_FrameID;
        internal double m_DeltaTime;
        internal float m_Weight;
        internal float m_EffectiveWeight;
        internal double m_EffectiveParentDelay;
        internal float m_EffectiveParentSpeed;
        internal float m_EffectiveSpeed;
        internal Flags m_Flags;
        internal PlayableOutput m_Output;

        private bool HasFlags(Flags flag)        { return (m_Flags & flag) == flag; }

        public ulong frameId                    { get { return m_FrameID; } }
        public float deltaTime                  { get { return (float)m_DeltaTime; } }
        public float weight                     { get { return m_Weight; } }
        public float effectiveWeight            { get { return m_EffectiveWeight; } }
        public double effectiveParentDelay      { get { return m_EffectiveParentDelay; } }
        public float effectiveParentSpeed       { get { return m_EffectiveParentSpeed; } }
        public float effectiveSpeed             { get { return m_EffectiveSpeed; } }
        public EvaluationType evaluationType    { get { return HasFlags(Flags.Evaluate) ? EvaluationType.Evaluate : EvaluationType.Playback; } }
        public bool seekOccurred                { get { return HasFlags(Flags.SeekOccured); } }
        public bool timeLooped                  { get { return HasFlags(Flags.Loop); } }
        public bool timeHeld                    { get { return HasFlags(Flags.Hold); } }
        public PlayableOutput output            { get { return m_Output; } }

        public PlayState effectivePlayState
        {
            get
            {
                if (HasFlags(Flags.EffectivePlayStateDelayed))
                    return PlayState.Delayed;
                if (HasFlags(Flags.EffectivePlayStatePlaying))
                    return PlayState.Playing;
                return PlayState.Paused;
            }
        }
    }
}
