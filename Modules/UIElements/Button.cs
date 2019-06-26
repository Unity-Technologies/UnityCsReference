// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public class Button : TextElement
    {
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> {}

        public new class UxmlTraits : TextElement.UxmlTraits {}

        public new static readonly string ussClassName = "unity-button";
        private Clickable m_Clickable;

        public Clickable clickable
        {
            get
            {
                return m_Clickable;
            }
            set
            {
                if (m_Clickable != null && m_Clickable.target == this)
                {
                    this.RemoveManipulator(m_Clickable);
                }

                m_Clickable = value;

                if (m_Clickable != null)
                {
                    this.AddManipulator(m_Clickable);
                }
            }
        }

        public event Action onClick
        {
            add
            {
                if (m_Clickable == null)
                {
                    clickable = new Clickable(value);
                }
                else
                {
                    m_Clickable.clicked += value;
                }
            }
            remove
            {
                if (m_Clickable != null)
                {
                    m_Clickable.clicked -= value;
                }
            }
        }

        public Button() : this(null)
        {
        }

        public Button(System.Action clickEvent)
        {
            AddToClassList(ussClassName);

            // Click-once behaviour
            clickable = new Clickable(clickEvent);
        }

        private static readonly string NonEmptyString = " ";
        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight,
            MeasureMode heightMode)
        {
            var textToMeasure = text;
            if (string.IsNullOrEmpty(textToMeasure))
            {
                textToMeasure = NonEmptyString;
            }
            return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
        }
    }
}
