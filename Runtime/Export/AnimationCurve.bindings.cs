// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine
{
    public enum WeightedMode
    {
        None = 0,
        In = 1 << 0,
        Out = 1 << 1,
        Both = In | Out
    }

    // A single keyframe that can be injected into an animation curve.
    [RequiredByNativeCode]
    public struct Keyframe
    {
        float m_Time;
        float m_Value;
        float m_InTangent;
        float m_OutTangent;

        int m_TangentMode;

        int m_WeightedMode;

        float m_InWeight;
        float m_OutWeight;

        // Create a keyframe.
        public Keyframe(float time, float value)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = 0;
            m_OutTangent = 0;
            m_WeightedMode = 0;
            m_InWeight = 0f;
            m_OutWeight = 0f;
            m_TangentMode = 0;
        }

        // Create a keyframe.
        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
            m_WeightedMode = 0;
            m_InWeight = 0f;
            m_OutWeight = 0f;
            m_TangentMode = 0;
        }

        // Create a keyframe.
        public Keyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
            m_WeightedMode = (int)WeightedMode.Both;
            m_InWeight = inWeight;
            m_OutWeight = outWeight;
            m_TangentMode = 0;
        }

        // The time of the keyframe.
        public float time { get { return m_Time; } set { m_Time = value; }  }

        // The value of the curve at keyframe.
        public float value { get { return m_Value; } set { m_Value = value; }  }

        // Describes the tangent when approaching this point from the previous point in the curve.
        public float inTangent { get { return m_InTangent; } set { m_InTangent = value; }  }

        // Describes the tangent when leaving this point towards the next point in the curve.
        public float outTangent { get { return m_OutTangent; } set { m_OutTangent = value; }  }

        // Describes the weight when approaching this point from the previous point in the curve.
        public float inWeight { get { return m_InWeight; } set { m_InWeight = value; }  }

        // Describes the weight when leaving this point towards the next point in the curve.
        public float outWeight { get { return m_OutWeight; } set { m_OutWeight = value; }  }

        public WeightedMode weightedMode { get { return (WeightedMode)m_WeightedMode; } set { m_WeightedMode = (int)value; } }

        // The tangent mode of the keyframe.
        // This is used only in the editor and will always return 0 in the player.
        [Obsolete("Use AnimationUtility.SetLeftTangentMode, AnimationUtility.SetRightTangentMode, AnimationUtility.GetLeftTangentMode or AnimationUtility.GetRightTangentMode instead.")]
        public int tangentMode { get { return tangentModeInternal; } set { tangentModeInternal = value; } }

        internal int tangentModeInternal
        {
            get
            {
                return m_TangentMode;
            }
            set
            {
                m_TangentMode = value;
            }
        }
    }

    // Determines how time is treated outside of the keyframed range of an [[AnimationClip]] or [[AnimationCurve]].
    public enum WrapMode
    {
        // When time reaches the end of the animation clip, the clip will automatically stop playing and time will be reset to beginning of the clip.
        Once = 1,

        // When time reaches the end of the animation clip, time will continue at the beginning.
        Loop = 2,

        // When time reaches the end of the animation clip, time will ping pong back between beginning and end.
        PingPong = 4,

        // Reads the default repeat mode set higher up.
        Default = 0,

        // Plays back the animation. When it reaches the end, it will keep playing the last frame and never stop playing.
        ClampForever = 8,

        //*undocumented*
        Clamp = 1
    }


#pragma warning disable 414

    // A collection of curves form an [[AnimationClip]].
    [NativeHeader("Runtime/Math/AnimationCurve.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [ThreadAndSerializationSafe]
    public class AnimationCurve : IEquatable<AnimationCurve>
    {
        internal IntPtr m_Ptr;

        [FreeFunction("AnimationCurveBindings::Internal_Destroy", IsThreadSafe = true)]
        extern static private void Internal_Destroy(IntPtr ptr);

        [FreeFunction("AnimationCurveBindings::Internal_Create", IsThreadSafe = true)]
        extern static private IntPtr Internal_Create(Keyframe[] keys);

        [FreeFunction("AnimationCurveBindings::Internal_Equals", HasExplicitThis = true)]
        extern private bool Internal_Equals(IntPtr other);

        ~AnimationCurve()
        {
            Internal_Destroy(m_Ptr);
        }

        // Evaluate the curve at /time/.
        [ThreadSafe]
        extern public float Evaluate(float time);

        //  All keys defined in the animation curve.
        public Keyframe[] keys { get { return GetKeys(); } set { SetKeys(value); } }

        // Add a new key to the curve.
        [FreeFunction("AnimationCurveBindings::AddKeySmoothTangents", HasExplicitThis = true, IsThreadSafe = true)]
        extern public int AddKey(float time, float value);

        // Add a new key to the curve.
        public int AddKey(Keyframe key) { return AddKey_Internal(key); }

        [NativeMethod("AddKey", IsThreadSafe = true)]
        extern private int AddKey_Internal(Keyframe key);

        // Removes the keyframe at /index/ and inserts key.
        [NativeThrows]
        [FreeFunction("AnimationCurveBindings::MoveKey", HasExplicitThis = true, IsThreadSafe = true)]
        extern public int MoveKey(int index, Keyframe key);

        // Removes a key
        [NativeThrows]
        [FreeFunction("AnimationCurveBindings::RemoveKey", HasExplicitThis = true, IsThreadSafe = true)]
        extern public void RemoveKey(int index);

        // Retrieves the key at index (RO)
        public Keyframe this[int index]
        {
            get { return GetKey(index); }
        }

        // The number of keys in the curve (RO)
        extern public int length
        {
            [NativeMethod("GetKeyCount", IsThreadSafe = true)]
            get;
        }

        // Replace all keyframes with the /keys/ array.
        [FreeFunction("AnimationCurveBindings::SetKeys", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void SetKeys(Keyframe[] keys);

        [NativeThrows]
        [FreeFunction("AnimationCurveBindings::GetKey", HasExplicitThis = true, IsThreadSafe = true)]
        extern private Keyframe GetKey(int index);

        [FreeFunction("AnimationCurveBindings::GetKeys", HasExplicitThis = true, IsThreadSafe = true)]
        extern private Keyframe[] GetKeys();

        // Smooth the in and out tangents of the keyframe at /index/.
        [NativeThrows]
        [FreeFunction("AnimationCurveBindings::SmoothTangents", HasExplicitThis = true, IsThreadSafe = true)]
        extern public void SmoothTangents(int index, float weight);

        // A constant line at /value/ starting at /timeStart/ and ending at /timeEnd/
        public static AnimationCurve Constant(float timeStart, float timeEnd, float value)
        {
            return Linear(timeStart, value, timeEnd, value);
        }

        // A straight Line starting at /timeStart/, /valueStart/ and ending at /timeEnd/, /valueEnd/
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            if (timeStart == timeEnd)
            {
                Keyframe key = new Keyframe(timeStart, valueStart);
                return new AnimationCurve(new Keyframe[] {key});
            }

            float tangent = (valueEnd - valueStart) / (timeEnd - timeStart);
            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, tangent), new Keyframe(timeEnd, valueEnd, tangent, 0.0F) };
            return new AnimationCurve(keys);
        }

        // An ease-in and out curve starting at /timeStart/, /valueStart/ and ending at /timeEnd/, /valueEnd/.
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            if (timeStart == timeEnd)
            {
                Keyframe key = new Keyframe(timeStart, valueStart);
                return new AnimationCurve(new Keyframe[] {key});
            }

            Keyframe[] keys = { new Keyframe(timeStart, valueStart, 0.0F, 0.0F), new Keyframe(timeEnd, valueEnd, 0.0F, 0.0F) };
            return new AnimationCurve(keys);
        }

        // The behaviour of the animation before the first keyframe
        extern public WrapMode preWrapMode
        {
            [NativeMethod("GetPreInfinity", IsThreadSafe = true)]
            get;
            [NativeMethod("SetPreInfinity", IsThreadSafe = true)]
            set;
        }

        // The behaviour of the animation after the last keyframe
        extern public WrapMode postWrapMode
        {
            [NativeMethod("GetPostInfinity", IsThreadSafe = true)]
            get;
            [NativeMethod("SetPostInfinity", IsThreadSafe = true)]
            set;
        }

        // Creates an animation curve from arbitrary number of keyframes.
        public AnimationCurve(params Keyframe[] keys) { m_Ptr = Internal_Create(keys); }

        // *undocumented*

        // Creates an empty animation curve
        [RequiredByNativeCode]
        public AnimationCurve()  { m_Ptr = Internal_Create(null); }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o))
            {
                return false;
            }

            if (ReferenceEquals(this, o))
            {
                return true;
            }

            if (o.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((AnimationCurve)o);
        }

        public bool Equals(AnimationCurve other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (m_Ptr.Equals(other.m_Ptr))
            {
                return true;
            }

            return Internal_Equals(other.m_Ptr);
        }

        public override int GetHashCode()
        {
            return m_Ptr.GetHashCode();
        }
    }

#pragma warning restore 414
}
