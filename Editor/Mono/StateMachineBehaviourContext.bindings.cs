// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor;
using System.Runtime.InteropServices;

namespace UnityEditor.Animations
{
    [NativeHeader("Editor/Src/Animation/StateMachineBehaviourContext.h")]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    public partial class StateMachineBehaviourContext
    {
        [NativeName("m_AnimatorController")]
        public AnimatorController           animatorController;
        [NativeName("m_AnimatorObject")]
        public UnityEngine.Object           animatorObject;
        [NativeName("m_LayerIndex")]
        public int                          layerIndex;
    }
}
