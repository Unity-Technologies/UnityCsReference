// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Accessibility
{
    public partial class AccessibilityNode
    {
        /// <summary>
        /// Deprecated. Use <see cref="AccessibilityNode.invoked"/> instead.
        /// </summary>
        /// <remarks>
        /// This event has been renamed to <see cref="AccessibilityNode.invoked"/> to avoid confusion with
        /// <see cref="AccessibilityState.Selected"/>.
        /// </remarks>
        [Obsolete("AccessibilityNode.selected has been renamed to AccessibilityNode.invoked to avoid confusion with " +
            "AccessibilityState.Selected. (UnityUpgradable) -> invoked", false)]
        public event Func<bool> selected
        {
            add => invoked += value;
            remove => invoked -= value;
        }
    }
}
