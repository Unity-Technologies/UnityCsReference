// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditorInternal.VR;

namespace UnityEditor.XR
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class VRDeviceSettingsTransitionTargetGroupAttribute : Attribute
    {
        BuildTargetGroup m_BuildTargetGroup;

        public BuildTargetGroup TargetGroup { get { return m_BuildTargetGroup;} }
        public VRDeviceSettingsTransitionTargetGroupAttribute(BuildTargetGroup targetGroup)
        {
            m_BuildTargetGroup = targetGroup;
        }
    }
}
