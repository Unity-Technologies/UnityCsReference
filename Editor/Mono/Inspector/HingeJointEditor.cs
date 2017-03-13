// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint))]
    [CanEditMultipleObjects]
    internal class HingeJointEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            DrawDefaultInspector();

            string err = "";
            var limits = ((HingeJoint)target).limits;
            if (limits.min < -180 || limits.min > 180)
                err += "Min Limit needs to be within [-180,180].";
            if (limits.max < -180 || limits.max > 180)
                err += (String.IsNullOrEmpty(err) ? "" : "\n") + "Max Limit needs to be within [-180,180].";
            if (limits.max < limits.min)
                err += (String.IsNullOrEmpty(err) ? "" : "\n") + "Max Limit needs to be larger or equal to the Min Limit.";
            if (!String.IsNullOrEmpty(err))
                EditorGUILayout.HelpBox(err, MessageType.Warning);
        }
    }
}
