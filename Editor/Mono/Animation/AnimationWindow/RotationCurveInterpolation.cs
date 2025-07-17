// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;

namespace UnityEditor
{
    partial class RotationCurveInterpolation
    {
        public enum Mode { Baked, NonBaked, RawQuaternions, RawEuler, Undefined }

        public static Mode GetModeFromCurveData(EditorCurveBinding data)
        {
            if (AnimationWindowUtility.IsTransformType(data.type) && data.propertyName.StartsWith("localEulerAngles"))
            {
                if (data.propertyName.StartsWith("localEulerAnglesBaked"))
                    return Mode.Baked;
                else if (data.propertyName.StartsWith("localEulerAnglesRaw"))
                    return Mode.RawEuler;
                else
                    return Mode.NonBaked;
            }
            else if (AnimationWindowUtility.IsTransformType(data.type) && data.propertyName.StartsWith("m_LocalRotation"))
                return Mode.RawQuaternions;

            return Mode.Undefined;
        }

        public static string GetPrefixForInterpolation(Mode newInterpolationMode)
        {
            if (newInterpolationMode == Mode.Baked)
                return "localEulerAnglesBaked";
            else if (newInterpolationMode == Mode.NonBaked)
                return "localEulerAngles";
            else if (newInterpolationMode == Mode.RawEuler)
                return "localEulerAnglesRaw";
            else if (newInterpolationMode == Mode.RawQuaternions)
                return "m_LocalRotation";
            else
                return null;
        }

        internal static EditorCurveBinding[] ConvertRotationPropertiesToInterpolationType(ReadOnlySpan<EditorCurveBinding> selection, Mode newInterpolationMode)
        {
            if (selection.Length != 4)
                return selection.ToArray();

            if (GetModeFromCurveData(selection[0]) == Mode.RawQuaternions)
            {
                EditorCurveBinding[] newCurves = new EditorCurveBinding[3];
                newCurves[0] = selection[0];
                newCurves[1] = selection[1];
                newCurves[2] = selection[2];

                string prefix = GetPrefixForInterpolation(newInterpolationMode);
                newCurves[0].propertyName = prefix + ".x";
                newCurves[1].propertyName = prefix + ".y";
                newCurves[2].propertyName = prefix + ".z";

                return newCurves;
            }
            else
                return selection.ToArray();
        }
    }
}
