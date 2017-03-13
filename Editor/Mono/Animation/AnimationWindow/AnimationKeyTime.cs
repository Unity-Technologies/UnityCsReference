// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    [System.Serializable]
    struct AnimationKeyTime
    {
        [SerializeField] private float  m_FrameRate;
        [SerializeField] private int    m_Frame;
        [SerializeField] private float  m_Time;

        public float    time        { get { return m_Time; } }
        public int      frame       { get { return m_Frame; } }
        public float    frameRate   { get { return m_FrameRate; } }

        /// <summary>
        /// A frame has a range of time. This is the beginning of the frame.
        /// </summary>
        public float frameFloor
        {
            get
            {
                return ((float)frame - 0.5f) / frameRate;
            }
        }

        /// <summary>
        /// A frame has a range of time. This is the end of the frame.
        /// </summary>
        public float frameCeiling
        {
            get
            {
                return ((float)frame + 0.5f) / frameRate;
            }
        }

        public static AnimationKeyTime  Time(float time, float frameRate)
        {
            AnimationKeyTime key = new AnimationKeyTime();
            key.m_Time = Mathf.Max(time, 0f);
            key.m_FrameRate = frameRate;
            key.m_Frame = UnityEngine.Mathf.RoundToInt(key.m_Time * frameRate);
            return key;
        }

        public static AnimationKeyTime Frame(int frame, float frameRate)
        {
            AnimationKeyTime key = new AnimationKeyTime();
            key.m_Frame = (frame < 0) ? 0 : frame;
            key.m_Time = key.m_Frame / frameRate;
            key.m_FrameRate = frameRate;
            return key;
        }

        // Check if a time in seconds overlaps with the frame
        public bool ContainsTime(float time)
        {
            return time >= frameFloor && time < frameCeiling;
        }

        public bool Equals(AnimationKeyTime key)
        {
            return m_Frame == key.m_Frame &&
                m_FrameRate == key.m_FrameRate &&
                Mathf.Approximately(m_Time, key.m_Time);
        }
    }
}
