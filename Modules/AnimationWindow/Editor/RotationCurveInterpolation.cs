// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditorInternal;

namespace UnityEditor
{
    static partial class RotationCurveInterpolation
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

        // Maps all rotation euler interpolation variants to their m_LocalRotation equivalent
        // so that node IDs stay stable when the interpolation mode changes.
        private static readonly Dictionary<string, string> s_PropertyNameForHashing = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "localEulerAnglesRaw",      "m_LocalRotation"   },
            { "localEulerAnglesRaw.x",    "m_LocalRotation.x" },
            { "localEulerAnglesRaw.y",    "m_LocalRotation.y" },
            { "localEulerAnglesRaw.z",    "m_LocalRotation.z" },
            { "localEulerAnglesBaked",    "m_LocalRotation"   },
            { "localEulerAnglesBaked.x",  "m_LocalRotation.x" },
            { "localEulerAnglesBaked.y",  "m_LocalRotation.y" },
            { "localEulerAnglesBaked.z",  "m_LocalRotation.z" },
            { "localEulerAngles",         "m_LocalRotation"   },
            { "localEulerAngles.x",       "m_LocalRotation.x" },
            { "localEulerAngles.y",       "m_LocalRotation.y" },
            { "localEulerAngles.z",       "m_LocalRotation.z" },
        };

        internal static string GetPropertyNameForHashing(System.Type type, string propertyName)
        {
            if (type != typeof(UnityEngine.Transform) || !propertyName.StartsWith("localEuler"))
                return propertyName;
            return s_PropertyNameForHashing.TryGetValue(propertyName, out string canonical) ? canonical : propertyName;
        }

        static List<EditorCurveBinding> s_BindingsCache;
        const string s_PropertyWithSuffixRegex = @"(?<suffix>\.[xyz])$";
        internal static EditorCurveBinding[] ConvertRotationPropertiesToInterpolationType(ReadOnlySpan<EditorCurveBinding> selection, Mode newInterpolationMode)
        {
            s_BindingsCache ??= new List<EditorCurveBinding>(4);
            if (s_BindingsCache.Capacity < selection.Length)
                s_BindingsCache.Capacity = selection.Length;

            s_BindingsCache.Clear();
            for (int i = 0; i < selection.Length; ++i)
            {
                if (GetModeFromCurveData(selection[i]) == Mode.RawQuaternions)
                {
                    // Process x, y, z rotation bindings. Drop w channel.
                    var match = Regex.Match(selection[i].propertyName, s_PropertyWithSuffixRegex);
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
