// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AnimationWindowCurve : IComparable<AnimationWindowCurve>
    {
        public const float timeEpsilon = 0.00001f;

        public List<AnimationWindowKeyframe> m_Keyframes;

        private EditorCurveBinding m_Binding;
        private int m_BindingHashCode;

        private AnimationClip m_Clip;
        private AnimationWindowSelectionItem m_SelectionBinding;

        private System.Type m_ValueType;

        public EditorCurveBinding binding { get { return m_Binding;  } }
        public bool isPPtrCurve { get { return m_Binding.isPPtrCurve; } }
        public bool isDiscreteCurve { get { return m_Binding.isDiscreteCurve; } }
        public bool isPhantom { get { return m_Binding.isPhantom; } }
        public string propertyName { get { return m_Binding.propertyName; } }
        public string path { get { return m_Binding.path; } }
        public System.Type type { get { return m_Binding.type; } }
        public System.Type valueType { get { return m_ValueType; } }
        public int length { get { return m_Keyframes.Count; } }

        public int depth { get { return path.Length > 0 ? path.Split('/').Length : 0; } }

        public AnimationClip clip { get { return m_Clip; } }

        public GameObject rootGameObject { get { return m_SelectionBinding != null ? m_SelectionBinding.rootGameObject : null; } }
        public ScriptableObject scriptableObject { get { return m_SelectionBinding != null ? m_SelectionBinding.scriptableObject : null; } }
        public float timeOffset { get { return m_SelectionBinding != null ? m_SelectionBinding.timeOffset : 0.0f; } }
        public bool clipIsEditable { get { return m_SelectionBinding != null ? m_SelectionBinding.clipIsEditable : true; } }
        public bool animationIsEditable { get { return m_SelectionBinding != null ? m_SelectionBinding.animationIsEditable : true; } }
        public int selectionID { get { return m_SelectionBinding != null ? m_SelectionBinding.id : 0; } }

        public AnimationWindowSelectionItem selectionBinding { get { return m_SelectionBinding; } set { m_SelectionBinding = value; } }

        public AnimationWindowCurve(AnimationClip clip, EditorCurveBinding binding, System.Type valueType)
        {
            binding = RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(binding, clip);

            m_Binding = binding;
            m_BindingHashCode = binding.GetHashCode();
            m_ValueType = valueType;
            m_Clip = clip;

            LoadKeyframes(clip);
        }

        public void LoadKeyframes(AnimationClip clip)
        {
            m_Keyframes = new List<AnimationWindowKeyframe>();

            if (!m_Binding.isPPtrCurve)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                for (int i = 0; curve != null && i < curve.length; i++)
                    m_Keyframes.Add(new AnimationWindowKeyframe(this, curve[i]));
            }
            else
            {
                ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (int i = 0; curve != null && i < curve.Length; i++)
                    m_Keyframes.Add(new AnimationWindowKeyframe(this, curve[i]));
            }
        }

        public override int GetHashCode()
        {
            int clipID = (clip == null ? 0 : clip.GetInstanceID());
            return unchecked(selectionID * 92821 ^ clipID * 19603 ^ GetBindingHashCode());
        }

        public int GetBindingHashCode()
        {
            return m_BindingHashCode;
        }

        public int CompareTo(AnimationWindowCurve obj)
        {
            bool pathEquals = path.Equals(obj.path);
            bool typeEquals = obj.type == type;

            if (!pathEquals && depth != obj.depth)
            {
                int minLength = Math.Min(path.Length, obj.path.Length);
                int commonLength = 0;
                int index = 0;
                for (; index < minLength; ++index)
                {
                    if (path[index] != obj.path[index])
                        break;

                    if (path[index] == '/')
                        commonLength = index + 1;
                }

                if (index == minLength)
                    commonLength = minLength;

                string subPath1 = path.Substring(commonLength);
                string subPath2 = obj.path.Substring(commonLength);

                if (String.IsNullOrEmpty(subPath1))
                    return -1;
                else if (String.IsNullOrEmpty(subPath2))
                    return 1;

                Regex r = new Regex(@"^[^\/]*\/");

                Match match1 = r.Match(subPath1);
                string next1 = match1.Success ? match1.Value.Substring(0, match1.Value.Length - 1) : subPath1;

                Match match2 = r.Match(subPath2);
                string next2 = match2.Success ? match2.Value.Substring(0, match2.Value.Length - 1) : subPath2;

                return next1.CompareTo(next2);
            }

            bool sameTransformComponent = type == typeof(Transform) && obj.type == typeof(Transform) && pathEquals;
            bool oneIsTransformComponent = (type == typeof(Transform) || obj.type == typeof(Transform)) && pathEquals;

            // We want to sort position before rotation
            if (sameTransformComponent)
            {
                string propertyGroupA = AnimationWindowUtility.GetNicePropertyGroupDisplayName(typeof(Transform), AnimationWindowUtility.GetPropertyGroupName(propertyName));
                string propertyGroupB = AnimationWindowUtility.GetNicePropertyGroupDisplayName(typeof(Transform), AnimationWindowUtility.GetPropertyGroupName(obj.propertyName));

                if (propertyGroupA.Contains("Position") && propertyGroupB.Contains("Rotation"))
                    return -1;
                if (propertyGroupA.Contains("Rotation") && propertyGroupB.Contains("Position"))
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
            if (pathEquals && typeEquals)
            {
                int lhsIndex = AnimationWindowUtility.GetComponentIndex(obj.propertyName);
                int rhsIndex = AnimationWindowUtility.GetComponentIndex(propertyName);
                if (lhsIndex != -1 && rhsIndex != -1 && propertyName.Substring(0, propertyName.Length - 2) == obj.propertyName.Substring(0, obj.propertyName.Length - 2))
                    return rhsIndex - lhsIndex;
            }

            return (path + type + propertyName).CompareTo(obj.path + obj.type + obj.propertyName);
        }

        public AnimationCurve ToAnimationCurve()
        {
            int length = m_Keyframes.Count;
            AnimationCurve animationCurve = new AnimationCurve();
            List<Keyframe> keys = new List<Keyframe>();

            float lastFrameTime = float.MinValue;

            for (int i = 0; i < length; i++)
            {
                // Make sure we don't get two keyframes in an exactly the same time. We just ignore those.
                if (Mathf.Abs(m_Keyframes[i].time - lastFrameTime) > AnimationWindowCurve.timeEpsilon)
                {
                    Keyframe newKeyframe = new Keyframe(m_Keyframes[i].time, (float)m_Keyframes[i].value, m_Keyframes[i].m_InTangent, m_Keyframes[i].m_OutTangent);
                    newKeyframe.tangentMode = m_Keyframes[i].m_TangentMode;
                    keys.Add(newKeyframe);
                    lastFrameTime = m_Keyframes[i].time;
                }
            }

            animationCurve.keys = keys.ToArray();
            return animationCurve;
        }

        public ObjectReferenceKeyframe[] ToObjectCurve()
        {
            int length = m_Keyframes.Count;
            List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();

            float lastFrameTime = float.MinValue;

            for (int i = 0; i < length; i++)
            {
                // Make sure we don't get two keyframes in an exactly the same time. We just ignore those.
                if (Mathf.Abs(m_Keyframes[i].time - lastFrameTime) > AnimationWindowCurve.timeEpsilon)
                {
                    ObjectReferenceKeyframe newKeyframe = new ObjectReferenceKeyframe();
                    newKeyframe.time = m_Keyframes[i].time;
                    newKeyframe.value = (UnityEngine.Object)m_Keyframes[i].value;
                    lastFrameTime = newKeyframe.time;
                    keys.Add(newKeyframe);
                }
            }

            return keys.ToArray();
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
                return isPPtrCurve ? null : (object)0f;

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

                if (key.time < time && nextKey.time >= time)
                {
                    if (isPPtrCurve)
                    {
                        return key.value;
                    }
                    else
                    {
                        //  Create an animation curve stub and evaluate.
                        Keyframe keyframe = new Keyframe(key.time, (float)key.value, key.m_InTangent, key.m_OutTangent);
                        Keyframe nextKeyframe = new Keyframe(nextKey.time, (float)nextKey.value, nextKey.m_InTangent, nextKey.m_OutTangent);

                        AnimationCurve animationCurve = new AnimationCurve();
                        animationCurve.keys = new Keyframe[2] { keyframe, nextKeyframe };

                        return animationCurve.Evaluate(time);
                    }
                }

                key = nextKey;
            }

            // Shouldn't happen...
            return isPPtrCurve ? null : (object)0f;
        }

        public void AddKeyframe(AnimationWindowKeyframe key, AnimationKeyTime keyTime)
        {
            // If there is already key in this time, we always want to remove it
            RemoveKeyframe(keyTime);

            m_Keyframes.Add(key);
            m_Keyframes.Sort((a, b) => a.time.CompareTo(b.time));
        }

        public void RemoveKeyframe(AnimationKeyTime time)
        {
            // Loop backwards so key removals don't mess up order
            for (int i = m_Keyframes.Count - 1; i >= 0; i--)
            {
                if (time.ContainsTime(m_Keyframes[i].time))
                    m_Keyframes.RemoveAt(i);
            }
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
    }
}
