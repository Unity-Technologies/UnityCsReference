// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part that displays a progress bar. Progress value from the model is interpreted as a percentage.
    /// </summary>
    [UnityRestricted]
    internal class ProgressBarPart : BaseModelViewPart
    {
        const float k_DefaultDebugAlpha = 1f;
        public static readonly string ussClassName = "ge-node-progress-bar";
        public static readonly string progressBarName = "progress";

        static readonly CustomStyleProperty<float> k_DebugBackgroundAlphaProperty = new("--debug-background-alpha");
        float m_DebugBackgroundAlpha = k_DefaultDebugAlpha;

        VisualElement m_Root;
        VisualElement m_Progress;

        float m_CurrentValue;
        bool m_Displayed;

        /// <summary>
        /// Creates a new instance of the <see cref="ProgressBarPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        /// <returns>The created part.</returns>
        public static ProgressBarPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            return new ProgressBarPart(name, model, ownerElement, parentClassName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBarPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model associated with the part.</param>
        /// <param name="ownerElement">The owner element of the part.</param>
        /// <param name="parentClassName">The parent class name of the part.</param>
        protected ProgressBarPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName, pickingMode = PickingMode.Ignore };
            m_Root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Progress = new VisualElement { name = progressBarName };
            m_Progress.AddToClassList(ussClassName.WithUssElement(progressBarName));
            m_Progress.AddToClassList(m_ParentClassName.WithUssElement(progressBarName));
            m_Root.Add(m_Progress);

            SetProgressPercent(0, false);

            container.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            m_Root.AddPackageStylesheet("NodeProgressBarPart.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is IHasProgress hasProgress)
            {
                var value = hasProgress.Progress;
                var shouldDisplay = value >= 0;

                if (Math.Abs(value - m_CurrentValue) > 0.005 || shouldDisplay != m_Displayed)
                {
                    SetProgressPercent(value, shouldDisplay);
                }
            }
        }

        /// <summary>
        /// Sets the progress.
        /// </summary>
        /// <param name="value">The progress value, in percent.</param>
        /// <param name="shouldDisplay">Whether the progress should be displayed.</param>
        /// <param name="progressActive">Whether the progress is active. Set to true to set the background alpha even if the actual progress is not displayed.</param>
        protected void SetProgressPercent(float value, bool shouldDisplay, bool progressActive = false)
        {
            m_Progress.tooltip = $"Progress : {value:P}";
            m_Progress.style.width = new StyleLength(Length.Percent(value * 100));
            m_Progress.visible = shouldDisplay;

            var color = GetColor();

            if (color != Color.clear)
            {
                color.a = shouldDisplay || progressActive ? m_DebugBackgroundAlpha : 1f;
                m_Root.style.backgroundColor = color;
            }

            m_CurrentValue = value;
            m_Displayed = shouldDisplay;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            m_DebugBackgroundAlpha = evt.customStyle.TryGetValue(k_DebugBackgroundAlphaProperty, out float alpha) ? alpha : k_DefaultDebugAlpha;
        }

        internal void SetColor(Color color)
        {
            var currentColor = GetColor();
            currentColor.a = color.a;
            if (currentColor != color) // only update if the color changed, ignoring the current alpha
            {
                m_Progress.style.backgroundColor = color;
                m_Root.style.backgroundColor = color;
            }
        }

        internal Color GetColor() => m_Root == null ? Color.clear : m_Root.resolvedStyle.backgroundColor;
    }
}
