// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    // Required information for animation recording.
    internal interface IAnimationRecordingState
    {
        GameObject activeGameObject { get; }
        GameObject activeRootGameObject { get; }
        AnimationClip activeAnimationClip { get; }
        int currentFrame { get; }

        bool addZeroFrame { get; }

        bool DiscardModification(PropertyModification modification);
        void SaveCurve(AnimationWindowCurve curve);
        void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride);
    }
}
