// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    /// <summary>
    /// Use this interface to provide a custom responder to the Animation Window selection change.
    /// This allow any custom component to provide its own [IAnimationWindowSelectionItem] to the AnimationWindow
    /// and control how animation is authored and evaluated.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    interface IAnimationWindowResponder
    {
        bool OnSelectionChange(AnimationWindow window, UnityEngine.Object selectedObject, out IAnimationWindowSelectionItem newSelection);
    }
}
