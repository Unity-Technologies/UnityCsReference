// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    static internal class CurveBindingUtility
    {
        // Current value of the property that rootGO + curveBinding is pointing to
        public static object GetCurrentValue(AnimationWindowState state, EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
            {
                return state.controlInterface.GetObjectReferenceValue(binding);
            }
            else if (binding.isDiscreteCurve)
            {
                return state.controlInterface.GetIntValue(binding);
            }

            return state.controlInterface.GetFloatValue(binding);
        }


        // Retrieve current value.  If bindings are available and value is animated, use bindings to get value.
        // Otherwise, evaluate AnimationWindowCurve at current time.
        public static object GetCurrentValue(AnimationWindowState state, AnimationWindowCurve curve)
        {
            // UUM-66112 - state.linkedWithSequencer - Padding for issue in Timeline where muscle
            // values are not updated in the editor when previewing in the Animation Window.
            // Fallback to curve values.
            if (state.previewing && curve.rootGameObject != null && !state.linkedWithSequencer)
            {
                return GetCurrentValue(state, curve.binding);
            }
            else
            {
                return curve.Evaluate(state.currentTime);
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
                else if (curveBinding.isDiscreteCurve)
                {
                    return 0;
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
