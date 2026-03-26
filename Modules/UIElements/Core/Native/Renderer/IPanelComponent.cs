// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IPanelComponentRootElement
    {
        IPanelComponent panelComponent { get; }
    }

    /// <summary>
    /// Interface for UI panel components, <see cref="PanelRenderer"/> and <see cref="UIDocument"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    public interface IPanelComponent
    {
        /// <summary>
        /// The GameObject this panel component is attached to.
        /// </summary>
        public GameObject gameObject { get; }

        /// <summary>
        /// The panel settings used by this panel component.
        /// </summary>
        public PanelSettings panelSettings { get; set; }

        /// <summary>
        /// The VisualTreeAsset used by this panel component.
        /// </summary>
        public VisualTreeAsset visualTreeAsset { get; set; }

        /// <summary>
        /// The parent panel component if this panel component is nested; otherwise, null.
        /// </summary>
        public IPanelComponent parentUI { get; }

        /// <summary>
        /// The sorting order of the panel component.
        /// </summary>
        public float sortingOrder { get; }

        /// <summary>
        /// The mode used to determine the size of the world space panel.
        /// </summary>
        public WorldSpaceSizeMode worldSpaceSizeMode { get; set; }

        /// <summary>
        /// The fixed size of the world space panel, used when <see cref="worldSpaceSizeMode"/> is set to Fixed.
        /// </summary>
        public Vector2 worldSpaceSize { get; set; }

        /// <summary>
        /// The position (relative or absolute) of the world space panel.
        /// </summary>
        public Position position { get; set; }

        /// <summary>
        /// The reference size mode for the pivot of the world space panel.
        /// </summary>
        public PivotReferenceSize pivotReferenceSize { get; set; }

        /// <summary>
        /// The pivot point of the world space panel.
        /// </summary>
        public Pivot pivot { get; set; }

        /// <summary>
        /// Validates the panel component after the associated assets have changed.
        /// </summary>
        /// <param name="forced">Forces the validation even if the assets haven't changed.</param>
        public void PerformValidation(bool forced);

        /// <summary>
        /// Updates the panel component.
        /// </summary>
        public void PerformUpdate();

        internal int creationIndex { get; }

        internal VisualElement GetRootVisualElement();

        //Just like the one above, but does not capture the reference to VisualElement for code stripping reasons
        internal IEventHandler GetRoot();

        internal void SetComponentEnabled(bool enabled);

        internal int softPointerCaptures { get; set; }

        internal VisualElementFocusRing focusRing { get; set; }

        internal Vector3 GetPanelPosition(IEventHandler pickedElement, Ray worldRay);

        internal IRuntimePanel GetContainerPanel();

        /// <summary>
        /// Handles live reload of the visual tree asset.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void HandleLiveReload();

        internal void OnLiveReloadOptionChanged();
    }
}
