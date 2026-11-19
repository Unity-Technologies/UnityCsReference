// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    /// <summary>
    /// Use this interface to override the Animation window controls and to customize how animation is evaluated.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    interface IAnimationWindowController : IDisposable
    {
        void OnSelectionChanged();

        float time { get; set; }
        int frame { get; set; }

        bool canPlay { get; }
        bool playing { get; set; }

        bool PlaybackUpdate();

        bool canPreview { get; }
        bool previewing { get; set; }

        bool canRecord { get; }
        bool recording { get; set; }

        void ResampleAnimation();

        void ProcessCandidates();
        void ClearCandidates();

        float GetFloatValue(EditorCurveBinding binding);
        int GetIntValue(EditorCurveBinding binding);
        UnityEngine.Object GetObjectReferenceValue(EditorCurveBinding binding);
    }
}
