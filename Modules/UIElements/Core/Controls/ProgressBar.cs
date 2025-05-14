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
    public abstract class AbstractProgressBar : BindableElement, INotifyValueChanged<float>
    {
        internal static readonly BindingId titleProperty = nameof(title);
        internal static readonly BindingId lowValueProperty = nameof(lowValue);
        internal static readonly BindingId highValueProperty = nameof(highValue);
        internal static readonly BindingId valueProperty = nameof(value);

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

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(lowValue), "low-value"),
                    new(nameof(highValue), "high-value"),
                    new(nameof(value), "value"),
                    new(nameof(title), "title"),
                });
            }

            #pragma warning disable 649
            [SerializeField] float lowValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags lowValue_UxmlAttributeFlags;
            [SerializeField] float highValue;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags highValue_UxmlAttributeFlags;
            [SerializeField] float value;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags value_UxmlAttributeFlags;
            [SerializeField] string title;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags title_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (AbstractProgressBar)obj;
                if (ShouldWriteAttributeValue(lowValue_UxmlAttributeFlags))
                    e.lowValue = lowValue;
                if (ShouldWriteAttributeValue(highValue_UxmlAttributeFlags))
                    e.highValue = highValue;
                if (ShouldWriteAttributeValue(value_UxmlAttributeFlags))
                    e.value = value;
                if (ShouldWriteAttributeValue(title_UxmlAttributeFlags))
                    e.title = title;
            }
        }

        /// <undoc/>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_LowValue = new UxmlFloatAttributeDescription { name = "low-value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_HighValue = new UxmlFloatAttributeDescription { name = "high-value", defaultValue = 100 };
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription { name = "value", defaultValue = 0 };
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription() { name = "title", defaultValue = string.Empty };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as AbstractProgressBar;
                bar.lowValue = m_LowValue.GetValueFromBag(bag, cc);
                bar.highValue = m_HighValue.GetValueFromBag(bag, cc);
                bar.value = m_Value.GetValueFromBag(bag, cc);
                bar.title = m_Title.GetValueFromBag(bag, cc);
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
    public class ProgressBar : AbstractProgressBar
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : AbstractProgressBar.UxmlSerializedData
        {
            public override object CreateInstance() => new ProgressBar();
        }

        /// <undoc/>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> {}
    }
}
