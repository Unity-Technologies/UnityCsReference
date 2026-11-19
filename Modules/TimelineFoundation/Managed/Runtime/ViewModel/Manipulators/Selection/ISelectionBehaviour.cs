// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal interface ISelectionBehaviour
    {
        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal enum Location { Start, End, None }
        void ClearSelection(ISequenceViewModel vm);
        void ToggleSelection(ISequenceViewModel vm, IEnumerable<UniqueID> ids);
        void Select(ISequenceViewModel vm, IEnumerable<UniqueID> ids);
        void Select(ISequenceViewModel vm, UniqueID id, Location location = Location.None);
        void Deselect(ISequenceViewModel vm, IEnumerable<UniqueID> ids);
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class SelectionBehaviourExtensions
    {
        public static void ToggleSelection(this ISelectionBehaviour behaviour, ISequenceViewModel vm, UniqueID id)
            => behaviour.ToggleSelection(vm, new[] { id });

        public static void Select(this ISelectionBehaviour behaviour, ISequenceViewModel vm, UniqueID id)
            => behaviour.Select(vm, new[] { id });

        public static void Deselect(this ISelectionBehaviour behaviour, ISequenceViewModel vm, UniqueID id)
            => behaviour.Deselect(vm, new[] { id });
    }
}
