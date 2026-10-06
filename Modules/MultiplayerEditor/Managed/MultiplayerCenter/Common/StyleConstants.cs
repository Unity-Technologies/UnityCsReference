// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.Center.Common
{
    /// <summary>
    /// Common style classes that can be accessed from the quickstart sections
    /// </summary>
    internal static class StyleConstants
    {
        /// <summary>
        /// Green checkmark to reflect a success
        /// Applicable to <c>VisualElement</c> to show a success state
        /// </summary>
        public const string CheckmarkClass = "checkmark-icon";

        /// <summary>
        /// The style that enables to have foldable sections in the getting started tab.
        /// Applicable to <c>Foldout</c>
        /// </summary>
        public const string OnBoardingSectionFoldout = "section-foldout";

        /// <summary>
        /// Default style for an onboarding section
        /// Apply to the top level element (Root) in Onboarding section
        /// Applicable to <c>VisualElement</c>
        /// </summary>
        public const string OnBoardingSectionClass = "onboarding-section";

        /// <summary>
        /// Default style for the title of an onboarding section
        /// Apply to the title visual element (<c>Label</c>) in Onboarding section
        /// </summary>
        public const string OnboardingSectionTitle = "onboarding-section-title";

        /// <summary>
        /// Button inside the header of a onboarding-section
        /// Applicable to <c>Button</c>
        /// </summary>
        public const string OnBoardingSectionMainButton = "onboarding-section-mainbutton";

        /// <summary>
        /// Button inside the header of a onboarding-section
        /// Applicable to <c>Label</c>
        /// </summary>
        public const string OnBoardingShortDescription = "onboarding-section-short-description";

        /// <summary>
        /// A button that opens a documentation page
        /// Applicable to <c>Button</c>
        /// </summary>
        public const string DocButtonClass = "doc-button";

        /// <summary>
        /// An element that will take all the remaining space in a flex container
        /// Applicable to <c>VisualElement</c>
        /// </summary>
        public const string FlexSpaceClass = "flex-spacer";

        /// <summary>
        /// Horizontal flex container
        /// Applicable to <c>VisualElement</c>
        /// </summary>
        public const string HorizontalContainerClass = "horizontal-container";

        /// <summary>
        /// Darker background color in dark mode, lighter in light mode
        /// Applicable to <c>VisualElement</c>
        /// </summary>
        public const string HighlightBackgroundClass = "highlight-background-color";
    }
}
