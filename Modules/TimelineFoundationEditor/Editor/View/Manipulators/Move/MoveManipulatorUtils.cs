// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    static partial class MoveManipulatorUtils
    {
        [AutoStaticsCleanupOnCodeReload] // scratch buffer for panel picking; retains the last pick's VisualElements, which must not outlive a code reload
        static readonly List<VisualElement> s_Picks = new List<VisualElement>();

        public static ItemElement FindItemFromTarget(VisualElement target, Vector2 mousePosition)
        {
            var itemElement = PickElement<ItemElement>(target, mousePosition);
            return itemElement is ISelectableElement
                || itemElement?.GetFirstOfType<ISelectableElement>() != null
                    ? itemElement
                    : null;
        }

        public static T PickElement<T>(VisualElement target, Vector2 mousePosition) where T : VisualElement
        {
            target.panel.PickAll(mousePosition, s_Picks);
            foreach (VisualElement element in s_Picks)
                if (element is T specificElement)
                    return specificElement;
            return null;
        }
    }
}
