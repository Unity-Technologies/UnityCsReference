// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    interface IUserConditionView
    {
        void Initialize(Condition condition, IConditionView view);

        /// <summary>
        /// Whether the built-in field for the condition's value is displayed.
        /// </summary>
        public bool DisplayValueField { get; }

        /// <summary>
        /// Whether the built-in title label is displayed.
        /// </summary>
        public bool DisplayTitleLabel { get; }

        /// <summary>
        /// Called once, after the condition's built-in UI is fully constructed.
        /// </summary>
        /// <remarks>
        /// This is the recommended entry point for allocating custom UI and adding it to
        /// <see cref="IConditionView.Root"/>.
        /// </remarks>
        public void OnViewBuilt();

        /// <summary>
        /// Called when the <see cref="VisualElement"/> for the <see cref="Condition"/> receives an
        /// <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <remarks>
        /// This can fire multiple times during a condition view's lifetime.
        /// </remarks>
        public void OnViewAttached();

        /// <summary>
        /// Called when the <see cref="VisualElement"/> for the <see cref="Condition"/> receives a
        /// <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <remarks>
        /// Like <see cref="OnViewAttached"/>, this can fire multiple times during a condition view's lifetime, and
        /// a matching <see cref="OnViewAttached"/> may follow.
        /// </remarks>
        public void OnViewDetached();
    }
}
