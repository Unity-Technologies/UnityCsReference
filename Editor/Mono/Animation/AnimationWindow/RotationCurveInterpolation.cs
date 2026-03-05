// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        static List<EditorCurveBinding> s_BindingsCache = new ();
        static readonly Regex s_PropertyWithSuffixRegex = new (@"(?<suffix>\.[xyz])$");
        internal static EditorCurveBinding[] ConvertRotationPropertiesToInterpolationType(ReadOnlySpan<EditorCurveBinding> selection, Mode newInterpolationMode)
        {
            if (s_BindingsCache.Capacity < selection.Length)
                s_BindingsCache.Capacity = selection.Length;

            s_BindingsCache.Clear();
            for (int i = 0; i < selection.Length; ++i)
            {
                if (GetModeFromCurveData(selection[i]) == Mode.RawQuaternions)
                {
                    // Process x, y, z rotation bindings. Drop w channel.
                    var match = s_PropertyWithSuffixRegex.Match(selection[i].propertyName);
                    if (match.Success)
                    {
                        string prefix = GetPrefixForInterpolation(newInterpolationMode);

                        var newBinding = selection[i];
                        newBinding.propertyName = prefix + match.Groups["suffix"];

                        s_BindingsCache.Add(newBinding);
                    }
                }
                else
                {
                    s_BindingsCache.Add(selection[i]);
                }
            }

            return s_BindingsCache.ToArray();
        }
    }
}
