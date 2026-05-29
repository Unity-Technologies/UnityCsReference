// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEngine;

namespace Unity.UIToolkit.Editor
{
    // Per-element clip wrapper. Overrides GetValueType so discrete style-enum rows
    // (e.g. Visibility) report typeof(int) when the clip has no GameObject root, instead
    // of the generic float fallback. The "discrete = int" assumption is UI-Toolkit-specific
    // and stays scoped to this module rather than baked into AnimationWindowClip itself.
    [Serializable]
    internal sealed class VisualElementAnimationWindowClip : AnimationWindowClip
    {
        public VisualElementAnimationWindowClip(AnimationClip clip)
            : base(clip)
        {
        }

        protected override Type GetValueType(EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
                return null;
            if (binding.isDiscreteCurve)
                return typeof(int);
            return typeof(float);
        }
    }
}
