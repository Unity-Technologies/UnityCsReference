using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// ProgressBar control using UIElements. Supports binding to float and int values.
    /// </summary>
    [MovedFrom(true, "UnityEditor.UIElements", "UnityEditor.UIElementsModule")]
    public class ProgressBar : BindableElement, INotifyValueChanged<float>
    {
        /// <summary>
        /// Uss Class Name used to style the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string ussClassName = "unity-progress-bar";
        /// <summary>
        /// Uss Class Name used to style the container of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";
        /// <summary>
        /// Uss Class Name used to style the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName + "__title";
        /// <summary>
        /// Uss Class Name used to style the container of the title of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName + "__title-container";
        /// <summary>
        /// Uss Class Name used to style the progress bar of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string progressUssClassName = ussClassName + "__progress";
        /// <summary>
        /// Uss Class Name used to style the background of the <see cref="ProgressBar"/>.
        /// </summary>
        public static readonly string backgroundUssClassName = ussClassName + "__background";

        public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = 100 };
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription() { name = "title" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as ProgressBar;
                bar.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                bar.highValue = m_HighValue.GetValueFromBag(bag, cc);
                var title = m_Title.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(title))
                {
                    bar.title = title;
                }
            }
        }

        readonly VisualElement m_Background;
        readonly VisualElement m_Progress;
        readonly Label m_Title;

        /// <summary>
        /// Sets the title of the ProgressBar which will be displayed in the center of the control.
        /// </summary>
        public string title
        {
            get => m_Title.text;
            set => m_Title.text = value;
        }

        internal float lowValue { get; private set; }
        internal float highValue { get; private set; } = 100f;

        public ProgressBar()
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
        /// Bindable float value that can be bound to int and float properties. Setting this will change the current displayed progress of the ProgressBar.
        /// </summary>
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
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

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
}
