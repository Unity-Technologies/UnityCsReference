// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    static internal class CurveBindingUtility
    {
        // Retrieve current value.  If bindings are available and value is animated, use bindings to get value.
        // Otherwise, evaluate AnimationWindowCurve at current time.
        public static object GetCurrentValue(AnimationWindowState state, AnimationWindowCurve curve)
        {
            if (state.previewing && curve.rootGameObject != null)
            {
                return AnimationWindowUtility.GetCurrentValue(curve.rootGameObject, curve.binding);
            }
            else
            {
                return curve.Evaluate(state.currentTime - curve.timeOffset);
            }
        }

        // Retrieve Current Value.  Use specified bindings to do so.
        public static object GetCurrentValue(GameObject rootGameObject, EditorCurveBinding curveBinding)
        {
            if (rootGameObject != null)
            {
                return AnimationWindowUtility.GetCurrentValue(rootGameObject, curveBinding);
            }
            else
            {
                if (curveBinding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return 0.0f;
                }
            }
        }
    }
}
