// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An <see cref="IAnimatableView"/> that can also display a static fill amount, used by <see cref="GraphVisualization.AccentManager{TView}"/>.
    /// </summary>
    interface IAccentAnimatableView : IAnimatableView
    {
        /// <summary>
        /// Overrides the fill amount shown on this element's accent bar.
        /// </summary>
        /// <param name="fillAmount">Fill value in the range [-100, 100]. A value of <c>0</c> hides the fill bar.</param>
        void OverrideFillAmount(float fillAmount);

        /// <summary>
        /// Removes any fill amount override and restores the displayed fill to the amount defined on the model.
        /// </summary>
        void ClearFillAmountOverride();
    }
}
