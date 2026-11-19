// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Unmanaged;
using Object = UnityEngine.Object;

namespace UnityEngine.UIElements
{
    // Unmanaged Background: asset id inline, gradient metadata behind a refcounted
    // list-of-one so identical gradients share one heap allocation. Layout-compatible
    // with the C++ mirror in Modules/UIElements/Core/Native/Style/BackgroundTypes.h.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal struct UnmanagedBackground : IEquatable<UnmanagedBackground>
    {
        public EntityId imageEntityId;                                //  8
        public UnmanagedRefCountedList<UnmanagedBackgroundGradient> gradient; //  8 (pointer)

        public bool IsEmpty => imageEntityId == EntityId.None && gradient.IsEmpty;

        // Explicit conversions — implicit would clash with animation/comparison overloads.
        public static explicit operator UnmanagedBackground(EntityId id) =>
            new() { imageEntityId = id };

        public static explicit operator EntityId(UnmanagedBackground bg) => bg.imageEntityId;

        public UnmanagedBackgroundGradient GetGradient() =>
            gradient.Count > 0 ? gradient[0] : default;

        public void CopyFrom(UnmanagedBackground other)
        {
            imageEntityId = other.imageEntityId;
            gradient.CopyFrom(other.gradient);
        }

        // In-place populate — allocating an intermediate would orphan the refcounted list.
        public void CopyFrom(Background managed)
        {
            var obj = managed.GetSelectedImage();
            // Gradients are baked at render time (see UIRElementBuilder); style-computation
            // paths just carry the gradient metadata, leaving imageEntityId at None.
            imageEntityId = obj != null ? obj.GetEntityId() : EntityId.None;
            if (managed.gradient.IsEmpty())
            {
                gradient.Clear();
            }
            else
            {
                Span<UnmanagedBackgroundGradient> single = stackalloc UnmanagedBackgroundGradient[1];
                single[0] = managed.gradient;
                gradient.CopyFrom((ReadOnlySpan<UnmanagedBackgroundGradient>)single);
            }
        }

        // Called from generated ApplyStyleValueManaged; boxed is either a Background
        // (typed inline-style set) or a UnityEngine.Object (legacy asset assignment).
        public void CopyFromBoxed(object boxed)
        {
            if (boxed is Background bg)
            {
                CopyFrom(bg);
                return;
            }
            imageEntityId = (boxed as Object)?.GetEntityId() ?? EntityId.None;
            gradient.Clear();
        }

        // Non-allocating comparison against the managed struct; mirrors CopyFrom(Background).
        public bool ValueEquals(in Background managed)
        {
            var obj = managed.GetSelectedImage();
            var managedId = obj != null ? obj.GetEntityId() : EntityId.None;
            if (imageEntityId != managedId)
                return false;
            if (managed.gradient.IsEmpty())
                return gradient.Count == 0;
            return gradient.Count == 1 && gradient[0].Equals((UnmanagedBackgroundGradient)managed.gradient);
        }

        public void Dispose() => gradient.Clear();

        public bool Equals(UnmanagedBackground other)
        {
            return imageEntityId == other.imageEntityId && gradient.Equals(other.gradient);
        }

        public override bool Equals(object obj) => obj is UnmanagedBackground o && Equals(o);

        public override int GetHashCode()
        {
            var h = imageEntityId.GetHashCode();
            h = (h * -1521134295) + gradient.GetHashCode();
            return h;
        }

        public static bool operator ==(UnmanagedBackground a, UnmanagedBackground b) => a.Equals(b);
        public static bool operator !=(UnmanagedBackground a, UnmanagedBackground b) => !a.Equals(b);
    }
}
