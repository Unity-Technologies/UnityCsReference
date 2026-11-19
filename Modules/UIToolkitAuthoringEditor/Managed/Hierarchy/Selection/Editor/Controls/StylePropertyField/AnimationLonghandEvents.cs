// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Raised by <see cref="StyleAnimationListView"/> after a user edit updates its backing longhand lists, so a
/// host that drives the control imperatively (the UI Builder) can write the change back. The authoring host
/// ignores it and instead relies on the <c>[CreateProperty]</c> two-way binding. <see cref="structural"/> is
/// true for add/remove (row count changed), false for an in-place field edit.
/// </summary>
internal class AnimationLonghandListChangedEvent : EventBase<AnimationLonghandListChangedEvent>
{
    public StyleAnimationListView.AnimationChangeType changeType;
    public bool structural;

    // True when removing the last row fell back to the keep-last default: the host should delete every longhand
    // rather than write the default values the control now shows.
    public bool cleared;
}
