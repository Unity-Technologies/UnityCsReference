// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Animations
{
    public enum BlendTreeType
    {
        Simple1D = 0 ,
        SimpleDirectional2D = 1,
        FreeformDirectional2D = 2,
        FreeformCartesian2D = 3,
        Direct = 4
    }

    [NativeType("Editor/Src/Animation/BlendTree.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ChildMotion
    {
        public Motion         motion                  { get { return m_Motion; }               set { m_Motion = value; }          }
        public float          threshold               { get { return m_Threshold; }            set { m_Threshold = value; }       }
        public Vector2        position                { get { return m_Position; }             set { m_Position = value; }        }
        public float          timeScale               { get { return m_TimeScale; }            set { m_TimeScale = value; }       }
        public float          cycleOffset             { get { return m_CycleOffset; }          set { m_CycleOffset = value; }     }
        public string         directBlendParameter    { get { return m_DirectBlendParameter; }     set { m_DirectBlendParameter = value; }}
        public bool           mirror                  { get { return m_Mirror; }               set { m_Mirror = value; }          }

        Motion        m_Motion;
        float         m_Threshold;
        Vector2       m_Position;
        float         m_TimeScale;
        float         m_CycleOffset;
        string        m_DirectBlendParameter;
        bool          m_Mirror;
    }

    [NativeHeader("Editor/Src/Animation/BlendTree.bindings.h")]
    [NativeType("Editor/Src/Animation/BlendTree.h")]
    public partial class BlendTree : Motion
    {
        public BlendTree()
        {
            Internal_Create(this);
        }

        [FreeFunction("BlendTreeBindings::Internal_Create")]
        extern private static void Internal_Create([Writable] BlendTree self);

        extern public string          blendParameter
        {
            get;
            set;
        }
        extern public string          blendParameterY
        {
            get;
            set;
        }
        extern public BlendTreeType   blendType
        {
            get;
            set;
        }

        extern public ChildMotion[] children
        {
            get;
            set;
        }

        extern internal int GetChildMotionCount();

        internal Motion GetChildMotion(int index)
        {
            if (index < 0 && index >= GetChildMotionCount())
                throw new ArgumentOutOfRangeException("index");

            return Internal_GetChildMotion(index);
        }

        [NativeMethod("GetChildMotion")]
        extern internal Motion Internal_GetChildMotion(int index);

        [NativeMethod("SetDirectBlendParameter")]
        extern internal void SetDirectBlendTreeParameter(int index, string parameter);

        [NativeMethod("GetDirectBlendParameter")]
        extern internal string GetDirectBlendTreeParameter(int index);

        extern public bool useAutomaticThresholds
        {
            get;
            set;
        }
        extern public float minThreshold
        {
            get;
            set;
        }
        extern public float maxThreshold
        {
            get;
            set;
        }

        extern internal void SortChildren();

        extern internal int    recursiveBlendParameterCount
        {
            get;
        }
        extern internal string GetRecursiveBlendParameter(int index);
        extern internal float GetRecursiveBlendParameterMin(int index);
        extern internal float GetRecursiveBlendParameterMax(int index);

        extern internal void SetInputBlendValue(string blendValueName, float value);
        extern internal float GetInputBlendValue(string blendValueName);

        [NativeMethod("GetAnimationClips")]
        extern internal AnimationClip[] GetAnimationClipsFlattened();
    }
}
