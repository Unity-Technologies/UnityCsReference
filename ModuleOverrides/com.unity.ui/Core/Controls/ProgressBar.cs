// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Abstract base class for the ProgressBar.
    /// </summary>
    public abstract class AbstractProgressBar : BindableElement, INotifyValueChanged<float>
    {
        internal static readonly DataBindingProperty titleProperty = nameof(title);
        internal static readonly DataBindingProperty lowValueProperty = nameof(lowValue);
        internal static readonly DataBindingProperty highValueProperty = nameof(highValue);
        internal static readonly DataBindingProperty valueProperty = nameof(value);

        /// <summary>
        /// USS Class Name used to style the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string ussClassName = "unity-progress-bar";
        /// <summary>
        /// USS Class Name used to style the container of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";
        /// <summary>
        /// USS Class Name used to style the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName + "__title";
        /// <summary>
        /// USS Class Name used to style the container of the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName + "__title-container";
        /// <summary>
        /// USS Class Name used to style the progress bar of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string progressUssClassName = ussClassName + "__progress";
        /// <summary>
        /// USS Class Name used to style the background of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string backgroundUssClassName = ussClassName + "__background";

        /// <undoc/>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = 100 };
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value", defaultValue = 0 };
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription() { name = "title" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as AbstractProgressBar;
                bar.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                bar.highValue = m_HighValue.GetValueFromBag(bag, cc);
                bar.value = m_Value.GetValueFromBag(bag, cc);
                var title = m_Title.GetValueFromBag(bag, cc);
                bar.title = (string.IsNullOrEmpty(title)) ? string.Empty : title;
            }
        }

        readonly VisualElement m_Background;
        readonly VisualElement m_Progress;
        readonly Label m_Title;
        float m_LowValue;
        float m_HighValue = 100f;

        /// <summary>
        /// Sets the title of the ProgressBar that displays in the center of the control.
        /// </summary>
        [CreateProperty]
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

        /// <summary>
        /// Sets the minimum value of the ProgressBar.
        /// </summary>
        [CreateProperty]
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

        /// <undoc/>
        public AbstractProgressBar()
        {
            AddToClassList(ussClassName);

            var container = new VisualElement() { name = ussClassName };

            m_Background = new VisualElement();
            m_Background.AddToClassList(backgroundUssClassName);
            container.Add(m_Background);

            m_Progress = new VisualElement();
            m_Progress.AddToClassList(progressUssClassName);
            m_Background.Add(m_Progress);

            var titleContainer = new VisualElement();
            titleContainer.AddToClassList(titleContainerUssClassName);
            m_Background.Add(titleContainer);

            m_Title = new Label();
            m_Title.AddToClassList(titleUssClassName);
            titleContainer.Add(m_Title);

            container.AddToClassList(containerUssClassName);
            hierarchy.Add(container);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            SetProgress(value);
        }

        float m_Value;

        /// <summary>
        /// Sets the progress value. If the value has changed, dispatches an <see cref="ChangeEvent{T}"/> of type float.
        /// </summary>
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

            right = CalculateProgressWidth(right);
            if (right >= 0)
            {
                m_Progress.style.right = right;
            }
        }

        const float k_MinVisibleProgress = 1.0f;

        float CalculateProgressWidth(float width)
        {
            if (m_Background == null || m_Progress == null)
            {
                return 0f;
            }

            if (float.IsNaN(m_Background.layout.width))
            {
                return 0f;
            }

            var maxWidth = m_Background.layout.width - 2;
            return maxWidth - Mathf.Max((maxWidth) * width / highValue, k_MinVisibleProgress);
        }
    }


    /// <summary>
    /// A control that displays the progress between a lower and upper bound value.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class ProgressBar : AbstractProgressBar
    {
        /// <undoc/>
        public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> {}
    }
}
