// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.AnimationWindowBuiltin
{
    sealed class AnimationClipBatchOperations : IDisposable
    {
        AnimationClip m_Clip;

        public AnimationClipBatchOperations(AnimationClip clip)
        {
            m_Clip = clip;
        }

        public void SetObjectReferenceCurve(EditorCurveBinding binding, ObjectReferenceKeyframe[] curve)
        {
            AnimationUtility.SetObjectReferenceCurveNoSync(m_Clip, binding, curve);
        }

        public void SetEditorCurve(EditorCurveBinding binding, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurveNoSync(m_Clip, binding, curve);
        }

        public void Dispose()
        {
            AnimationUtility.SyncEditorCurves(m_Clip);
        }
    }
}
