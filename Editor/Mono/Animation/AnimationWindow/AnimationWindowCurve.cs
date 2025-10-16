// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.AnimationWindowBuiltin;

namespace UnityEditorInternal
{
    enum InheritanceState
    {
        None,                   // This curve does not inherit from a parent asset.
        Inherited,              // Inherited curve can be edited, but cannot be removed.
        Overridden              // This curve was inherited from a parent asset and modified.
    }

    class AnimationWindowCurve :
        IComparable<AnimationWindowCurve>,
        IEquatable<AnimationWindowCurve>
    {
        public const float timeEpsilon = 0.00001f;

        private List<AnimationWindowKeyframe> m_Keyframes = new();

        private EditorCurveBinding m_Binding;
        private int m_BindingHashCode;

        private IAnimationWindowClip m_Clip;

        private System.Type m_ValueType;

        private bool m_IsPhantom;

        public EditorCurveBinding binding { get { return m_Binding;  } }
        public bool isPPtrCurve { get { return m_Binding.isPPtrCurve; } }
        public bool isDiscreteCurve { get { return m_Binding.isDiscreteCurve; } }
        public bool isSerializeReferenceCurve { get {return m_Binding.isSerializeReferenceCurve;}}
        public bool isPhantom { get { return m_IsPhantom; } set { m_IsPhantom = value; } }
        public InheritanceState inheritanceState { get; set; }
        public string propertyName { get { return m_Binding.propertyName; } }
        public string path { get { return m_Binding.path; } }
        public System.Type type { get { return m_Binding.type; } }
        public System.Type valueType { get { return m_ValueType; } }
        public int length { get { return m_Keyframes.Count; } }

        public int depth { get { return path.Length > 0 ? path.Split('/').Length : 0; } }

        public IAnimationWindowClip clip { get { return m_Clip; } }

        public IReadOnlyList<AnimationWindowKeyframe> keyframes => m_Keyframes;

        private object defaultValue
        {
            get
            {
                if (isPPtrCurve)
                    return null;
                if (isDiscreteCurve)
                    return 0;
                return 0f;
            }
        }

        public AnimationWindowCurve(IAnimationWindowClip clip, EditorCurveBinding binding, System.Type valueType)
        {
            if(clip is AnimationWindowClip animationWindowClip)
                binding = UnityEditor.AnimationWindowBuiltin.RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(binding, animationWindowClip.animationClip);

            m_Binding = binding;
            m_BindingHashCode = binding.GetHashCode();
            m_ValueType = valueType;
            m_Clip = clip;

            m_Keyframes.Clear();
            clip.LoadKeyframes(this, m_Keyframes);
        }

        public void Reload()
        {
            m_Keyframes.Clear();
            m_Clip.LoadKeyframes(this, m_Keyframes);
        }

        public override int GetHashCode()
        {
            int clipID = (clip == null ? 0 : clip.id);
            return unchecked(clipID.GetHashCode() * 19603 ^ GetBindingHashCode());
        }

        public int GetBindingHashCode()
        {
            return m_BindingHashCode;
        }

        public int CompareTo(AnimationWindowCurve obj)
        {
            if (!path.Equals(obj.path))
            {
                return ComparePaths(obj.path);
            }

            bool sameTransformComponent = type == typeof(Transform) && obj.type == typeof(Transform);
            bool oneIsTransformComponent = (type == typeof(Transform) || obj.type == typeof(Transform));

            // We want to sort position before rotation
            if (sameTransformComponent)
            {
                string propertyGroupA = AnimationWindowUtility.GetPropertyGroupName(propertyName);
                string propertyGroupB = AnimationWindowUtility.GetPropertyGroupName(obj.propertyName);

                if (propertyGroupA.Equals("m_LocalPosition") && (propertyGroupB.Equals("m_LocalRotation") || propertyGroupB.StartsWith("localEulerAngles")))
                    return -1;
                if ((propertyGroupA.Equals("m_LocalRotation") || propertyGroupA.StartsWith("localEulerAngles")) && propertyGroupB.Equals("m_LocalPosition"))
                    return 1;
            }
            // Transform component should always come first.
            else if (oneIsTransformComponent)
            {
                if (type == typeof(Transform))
                    return -1;
                else
                    return 1;
            }

            // Sort (.r, .g, .b, .a) and (.x, .y, .z, .w)
            if (obj.type == type)
            {
                int lhsIndex = AnimationWindowUtility.GetComponentIndex(obj.propertyName);
                int rhsIndex = AnimationWindowUtility.GetComponentIndex(propertyName);
                if (lhsIndex != -1 && rhsIndex != -1 && propertyName.Substring(0, propertyName.Length - 2) == obj.propertyName.Substring(0, obj.propertyName.Length - 2))
                    return rhsIndex - lhsIndex;
            }

            return string.Compare((path + type + propertyName), obj.path + obj.type + obj.propertyName, StringComparison.Ordinal);
        }

        public bool Equals(AnimationWindowCurve other)
        {
            return CompareTo(other) == 0;
        }

        int ComparePaths(string otherPath)
        {
            var thisPath = path.Split('/');
            var objPath = otherPath.Split('/');

            int smallerLength = Math.Min(thisPath.Length, objPath.Length);
            for (int i = 0; i < smallerLength; ++i)
            {
                int compare = string.Compare(thisPath[i], objPath[i], StringComparison.Ordinal);
                if (compare == 0)
                {
                    continue;
                }

                return compare;
            }

            if (thisPath.Length < objPath.Length)
            {
                return -1;
            }

            return 1;
        }

        public AnimationWindowKeyframe FindKeyAtTime(AnimationKeyTime keyTime)
        {
            int index = GetKeyframeIndex(keyTime);
            if (index == -1)
                return null;

            return m_Keyframes[index];
        }

        public object Evaluate(float time)
        {
            if (m_Keyframes.Count == 0)
                return defaultValue;

            AnimationWindowKeyframe firstKey = m_Keyframes[0];
            if (time <= firstKey.time)
                return firstKey.value;

            AnimationWindowKeyframe lastKey = m_Keyframes[m_Keyframes.Count - 1];
            if (time >= lastKey.time)
                return lastKey.value;

            AnimationWindowKeyframe key = firstKey;
            for (int i = 1; i < m_Keyframes.Count; ++i)
            {
                AnimationWindowKeyframe nextKey = m_Keyframes[i];

                if (key.time <= time && nextKey.time > time)
                {
                    if (isPPtrCurve || isDiscreteCurve)
                    {
                        return key.value;
                    }
                    else
                    {
                        //  Create an animation curve stub and evaluate.
                        Keyframe keyframe = key.ToKeyframe();
                        Keyframe nextKeyframe = nextKey.ToKeyframe();

                        AnimationCurve animationCurve = new AnimationCurve();
                        animationCurve.keys = new Keyframe[2] { keyframe, nextKeyframe };

                        return animationCurve.Evaluate(time);
                    }
                }

                key = nextKey;
            }

            // Shouldn't happen...
            return defaultValue;
        }

        public void AddKeyframe(AnimationWindowKeyframe key, AnimationKeyTime keyTime)
        {
            // If there is already key in this time, we always want to remove it
            RemoveKeyframe(keyTime);

            m_Keyframes.Add(key);
            m_Keyframes.Sort((a, b) => a.time.CompareTo(b.time));
        }

        public void AddKeyframeFast(AnimationWindowKeyframe key) => m_Keyframes.Add(key);

        public void RemoveKeyframe(AnimationKeyTime time)
        {
            // Loop backwards so key removals don't mess up order
            for (int i = m_Keyframes.Count - 1; i >= 0; i--)
            {
                if (time.ContainsTime(m_Keyframes[i].time))
                    m_Keyframes.RemoveAt(i);
            }
        }

        public void RemoveKeyframe(AnimationWindowKeyframe keyframe)
        {
            m_Keyframes.Remove(keyframe);
        }

        public bool HasKeyframe(AnimationKeyTime time)
        {
            return GetKeyframeIndex(time) != -1;
        }

        public int GetKeyframeIndex(AnimationKeyTime time)
        {
            for (int i = 0; i < m_Keyframes.Count; i++)
            {
                if (time.ContainsTime(m_Keyframes[i].time))
                    return i;
            }
            return -1;
        }

        // Remove keys at range. Start time is exclusive and end time inclusive.
        public void RemoveKeysAtRange(float startTime, float endTime)
        {
            for (int i = m_Keyframes.Count - 1; i >= 0; i--)
            {
                if (Mathf.Approximately(endTime, m_Keyframes[i].time) ||
                    m_Keyframes[i].time > startTime && m_Keyframes[i].time < endTime)
                    m_Keyframes.RemoveAt(i);
            }
        }

        public void Clear()
        {
            m_Keyframes.Clear();
        }
    }
}
