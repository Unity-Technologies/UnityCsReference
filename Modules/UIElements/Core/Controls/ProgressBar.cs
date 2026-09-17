// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Abstract base class for the ProgressBar.
    /// </summary>
    [UxmlElement]
    public abstract partial class AbstractProgressBar : BindableElement, INotifyValueChanged<float>
    {
        internal static readonly BindingId titleProperty = nameof(title);
        internal static readonly BindingId lowValueProperty = nameof(lowValue);
        internal static readonly BindingId highValueProperty = nameof(highValue);
        internal static readonly BindingId valueProperty = nameof(value);

        /// <summary>
        /// USS Class Name used to style the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string ussClassName = "unity-progress-bar";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS Class Name used to style the container of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";
        internal static readonly UniqueStyleString containerUssClassNameUnique = new(containerUssClassName);

        /// <summary>
        /// USS Class Name used to style the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName + "__title";
        internal static readonly UniqueStyleString titleUssClassNameUnique = new(titleUssClassName);

        /// <summary>
        /// USS Class Name used to style the container of the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName + "__title-container";
        internal static readonly UniqueStyleString titleContainerUssClassNameUnique = new(titleContainerUssClassName);

        /// <summary>
        /// USS Class Name used to style the progress bar of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string progressUssClassName = ussClassName + "__progress";
        internal static readonly UniqueStyleString progressUssClassNameUnique = new(progressUssClassName);

        /// <summary>
        /// USS Class Name used to style the background of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string backgroundUssClassName = ussClassName + "__background";
        internal static readonly UniqueStyleString backgroundUssClassNameUnique = new(backgroundUssClassName);

        readonly VisualElement m_Background;
        readonly VisualElement m_Progress;
        readonly Label m_Title;
        float m_LowValue;
        float m_HighValue = 100f;

        /// <summary>
        /// Sets the minimum value of the ProgressBar.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public float lowValue
        {
            get => m_LowValue;
            set
            {
                var previous = lowValue;
                m_LowValue = value;
                SetProgress(m_Value);

                if (!Mathf.Approximately(previous, lowValue))
                    NotifyPropertyChanged(lowValueProperty);
            }
        }

        /// <summary>
        /// Sets the maximum value of the ProgressBar.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public float highValue
        {
            get => m_HighValue;
            set
            {
                var previous = highValue;

                m_HighValue = value;
                SetProgress(m_Value);

                if (!Mathf.Approximately(previous, highValue))
                    NotifyPropertyChanged(highValueProperty);
            }
        }

        // We can not have a UxmlAttribute on a virtual property so need to do this. 
        [UxmlAttribute("value"), UxmlAttributeBindingPath("value")]
        internal float valueOverride { get => value; set => SetValueWithoutNotify(value); }

        /// <summary>
        /// Sets the title of the ProgressBar that displays in the center of the control.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public string title
        {
            get => m_Title.text;
            set
            {
                var previous = title;
                m_Title.text = value;

                if (string.CompareOrdinal(previous, title) != 0)
                    NotifyPropertyChanged(titleProperty);
            }
        }

        /// <undoc/>
        public AbstractProgressBar()
        {
            AddToClassList(ussClassNameUnique);

            var container = new VisualElement() { name = ussClassName };

            m_Background = new VisualElement();
            m_Background.AddToClassList(backgroundUssClassNameUnique);
            container.Add(m_Background);

            m_Progress = new VisualElement();
            m_Progress.AddToClassList(progressUssClassNameUnique);
            m_Background.Add(m_Progress);

            var titleContainer = new VisualElement();
            titleContainer.AddToClassList(titleContainerUssClassNameUnique);
            m_Background.Add(titleContainer);

            m_Title = new Label();
            m_Title.AddToClassList(titleUssClassNameUnique);
            titleContainer.Add(m_Title);

            container.AddToClassList(containerUssClassNameUnique);
            hierarchy.Add(container);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            SetProgress(value);
        }

        float m_Value;

        /// <summary>
        /// Sets the progress value. The value is clamped between lowValue and highValue.
        /// </summary>
        /// <remarks>
        /// The progress percentage is calculated as @@100 * (value - lowValue) / (highValue - lowValue)@@.
        /// </remarks>
        /// <remarks>
        /// If the value has changed, dispatches an <see cref="ChangeEvent{T}"/> of type float.
        /// </remarks>
        [CreateProperty]
        public virtual float value
        {
            get { return m_Value; }
            set
            {
                if (!EqualityComparer<float>.Default.Equals(m_Value, value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<float> evt = ChangeEvent<float>.GetPooled(m_Value, value))
                        {
                            evt.elementTarget = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                            NotifyPropertyChanged(valueProperty);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the progress value.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetValueWithoutNotify(float newValue)
        {
            m_Value = newValue;
            SetProgress(value);
        }

        void SetProgress(float p)
        {
            float right;
            if (p < lowValue)
            {
                right = lowValue;
            }
            else if (p > highValue)
            {
                right = highValue;
            }
            else
            {
                right = p;
            }

            right = CalculateOppositeProgressWidth(right);
            if (right >= 0)
            {
                m_Progress.style.right = right;
            }
        }

        const float k_MinVisibleProgress = 0.0f;
        const float k_AcceptedWidthEpsilon = 0.1f;

        /// <summary>
        /// Returns the opposite of the progress width to set as the "right" style. Ex: progress bar at 75% will return 25%.
        /// </summary>
        float CalculateOppositeProgressWidth(float width)
        {
            if (m_Background == null || m_Progress == null)
            {
                return 0f;
            }

            if (float.IsNaN(m_Background.layout.width))
            {
                return 0f;
            }

            var maxWidth = Mathf.Floor(m_Background.layout.width - 2);
            var progressWidth = Mathf.Max((maxWidth) * width / highValue, k_MinVisibleProgress);
            var oppositeProgressWidth = maxWidth - progressWidth;

            // If the difference between the max width and the desired right position is too small, we don't want to display the progress bar.
            m_Progress.style.width = Mathf.Abs(maxWidth - oppositeProgressWidth) < k_AcceptedWidthEpsilon ? new StyleLength(0f)
                : new StyleLength(StyleKeyword.Auto);
            return oppositeProgressWidth;
        }
    }

    /// <summary>
    /// A control that displays the progress between a lower and upper bound value. For more information, refer to [[wiki:UIE-uxml-element-ProgressBar|UXML element ProgressBar]].
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// public ProgressBar CreateProgressBar()
    /// {
    ///     var progressBar = new ProgressBar
    ///     {
    ///         title = "Progress",
    ///         lowValue = 0f,
    ///         highValue = 100f,
    ///         value = 0f
    ///     };
    ///
    ///     progressBar.schedule.Execute(() =>
    ///     {
    ///         progressBar.value += 2f;
    ///     }).Every(75).Until(() => progressBar.value >= 100f);
    ///
    ///     return progressBar;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/ProgressBar.png")]
    public partial class ProgressBar : AbstractProgressBar
    {
    }
}
