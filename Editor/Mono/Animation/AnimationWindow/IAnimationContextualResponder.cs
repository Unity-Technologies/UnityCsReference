// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    // Required information for animation recording.
    internal interface IAnimationContextualResponder
    {
        bool IsAnimatable(SerializedProperty property);
        bool IsEditable(SerializedProperty property);

        bool KeyExists(SerializedProperty property);
        bool CandidateExists(SerializedProperty property);

        bool CurveExists(SerializedProperty property);

        bool HasAnyCandidates();
        bool HasAnyCurves();

        void AddKey(SerializedProperty property);
        void RemoveKey(SerializedProperty property);

        void RemoveCurve(SerializedProperty property);

        void AddCandidateKeys();
        void AddAnimatedKeys();

        void GoToNextKeyframe(SerializedProperty property);
        void GoToPreviousKeyframe(SerializedProperty property);
    }
}
