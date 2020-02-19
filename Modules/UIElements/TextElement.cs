// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface ITextElement
    {
        string text { get; set; }
    }

    public class TextElement : BindableElement, ITextElement, INotifyValueChanged<string>
    {
        public new class UxmlFactory : UxmlFactory<TextElement, UxmlTraits> {}
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((ITextElement)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        public static readonly string ussClassName = "unity-text-element";

        public TextElement()
        {
            requireMeasureFunction = true;
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private TextHandle m_TextHandle = TextHandle.New();

        // For automated testing purposes
        internal TextHandle textHandle { get { return m_TextHandle; } }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_TextHandle.useLegacy = e.destinationPanel.contextType == ContextType.Editor;
        }

        [SerializeField]
        private string m_Text;
        public virtual string text
        {
            get { return ((INotifyValueChanged<string>) this).value; }
            set
            {
                ((INotifyValueChanged<string>) this).value = value;
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mgc.Text(MeshGenerationContextUtils.TextParams.MakeStyleBased(this, this.text), m_TextHandle, this.scaledPixelsPerPoint);
        }

        public Vector2 MeasureTextSize(string textToMeasure, float width, MeasureMode widthMode, float height,
            MeasureMode heightMode)
        {
            return MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode, m_TextHandle);
        }

        internal static Vector2 MeasureVisualElementTextSize(VisualElement ve, string textToMeasure, float width, MeasureMode widthMode, float height, MeasureMode heightMode, TextHandle textHandle)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            Font usedFont = ve.computedStyle.unityFont.value;
            if (textToMeasure == null || usedFont == null)
                return new Vector2(measuredWidth, measuredHeight);

            var elementScaling = ve.ComputeGlobalScale();

            float scaling = (elementScaling.x + elementScaling.y) * 0.5f * ve.scaledPixelsPerPoint;

            if (scaling <= 0)
                return Vector2.zero;

            if (widthMode == MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = GetTextSettings(ve, textToMeasure);
                textParams.wordWrap = false;
                textParams.richText = false;

                //we make sure to round up as yoga could decide to round down and text would start wrapping
                measuredWidth = Mathf.Ceil(textHandle.ComputeTextWidth(textParams, scaling));

                if (widthMode == MeasureMode.AtMost)
                {
                    measuredWidth = Mathf.Min(measuredWidth, width);
                }
            }

            if (heightMode == MeasureMode.Exactly)
            {
                measuredHeight = height;
            }
            else
            {
                var textParams = GetTextSettings(ve, textToMeasure);
                textParams.wordWrapWidth = measuredWidth;
                textParams.richText = false;

                measuredHeight = Mathf.Ceil(textHandle.ComputeTextHeight(textParams, scaling));

                if (heightMode == MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            return new Vector2(measuredWidth, measuredHeight);
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return MeasureTextSize(text, desiredWidth, widthMode, desiredHeight, heightMode);
        }

        private static MeshGenerationContextUtils.TextParams GetTextSettings(VisualElement ve, string text)
        {
            ComputedStyle style = ve.computedStyle;
            return new MeshGenerationContextUtils.TextParams
            {
                rect = ve.contentRect,
                text = text,
                font = style.unityFont.value,
                fontSize = (int)style.fontSize.value.value,
                fontStyle = style.unityFontStyleAndWeight.value,
                fontColor = style.color.value,
                anchor = style.unityTextAlign.value,
                wordWrap = style.whiteSpace.value == WhiteSpace.Normal,
                wordWrapWidth = style.whiteSpace.value == WhiteSpace.Normal ? ve.contentRect.width : 0.0f,
                richText = true
            };
        }

        //INotifyValueChange
        string INotifyValueChanged<string>.value
        {
            get
            {
                return m_Text ?? String.Empty;
            }

            set
            {
                if (m_Text != value)
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(this.text, value))
                        {
                            evt.target = this;
                            ((INotifyValueChanged<string>) this).SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        ((INotifyValueChanged<string>) this).SetValueWithoutNotify(value);
                    }
                }
            }
        }

        void INotifyValueChanged<string>.SetValueWithoutNotify(string newValue)
        {
            if (m_Text != newValue)
            {
                m_Text = newValue;
                IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);

                if (!string.IsNullOrEmpty(viewDataKey))
                    SaveViewData();
            }
        }
    }
}
