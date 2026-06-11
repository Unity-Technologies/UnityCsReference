// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    /// <summary>
    /// Use this interface to create a custom selection for the AnimationWindow.
    /// This allows to control how clips are created and managed in the AnimationWindow.
    /// Also, this gives the ability to customize how animation is authored.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    interface IAnimationWindowSelectionItem : ISelectionBinding, IDisposable
    {
        GameObject gameObject { get; }
        GameObject rootGameObject { get; }
        IAnimationWindowClip clip { get; set;  }
        IAnimationWindowController controller { get; }

        Component animationPlayer { get; }

        bool disabled { get; }

        bool canChangeClip { get; }
        bool canAddCurves { get; }
        bool canCreateClips { get;  }
        bool canSyncSceneSelection { get; }

        int GetRefreshHash();
        void Synchronize();

        IAnimationWindowClip[] GetClips();
        IAnimationWindowClip CreateNewClip();
        bool InitializeSelection();

        bool IsCompatibleWith(UnityEngine.Object selectedObject);
        EditorCurveBinding[] GetAnimatableBindings(GameObject gameObject);
        EditorCurveBinding[] GetAnimatableBindings();
        System.Type GetValueType(EditorCurveBinding binding);

        bool isImported { get; }
        bool hasUnsavedChanges { get; }
        void SaveChanges();
        void DiscardChanges();

        // Optional label shown by the Animation Window's onboarding panel when the selection has
        // no backing GameObject; null falls back to the legacy "No animatable object selected" text.
        string onboardingLabel => null;
    }
}
