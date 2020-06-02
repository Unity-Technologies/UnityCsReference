// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ProgressBar : BindableElement, INotifyValueChanged<float>
    {
        public static readonly string ussClassName = "unity-progress-bar";
        public static readonly string containerUssClassName = ussClassName + "__container";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string titleContainerUssClassName = ussClassName + "__title-container";
        public static readonly string progressUssClassName = ussClassName + "__progress";
        public static readonly string backgroundUssClassName = ussClassName + "__background";

        public new class UxmlFactory : UxmlFactory<ProgressBar, UxmlTraits> {}

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

        public string title
        {
            get { return this.Q<Label>(null, titleUssClassName).text; }
            set { this.Q<Label>(null, titleUssClassName).text = value; }
        }

        internal float lowValue { get; private set; }
        internal float highValue { get; private set; } = 100f;

        public ProgressBar()
        {
            var tpl = EditorGUIUtility.Load("UIPackageResources/UXML/ProgressBar.uxml") as VisualTreeAsset;
            AddToClassList(ussClassName);
            var container = tpl.Instantiate();
            container.AddToClassList(containerUssClassName);
            hierarchy.Add(container);

            m_Background = container.Q(null, backgroundUssClassName);
            m_Progress = container.Q(null, progressUssClassName);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            SetProgress(value);
        }

        [SerializeField]
        private float m_Value { get; set; }
        public virtual float value
        {
            get
            {
                return m_Value;
            }
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

        const float minVisibleProgress = 1.0f;

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
            return maxWidth - Mathf.Max((maxWidth) * width / highValue, minVisibleProgress);
        }
    }
}
