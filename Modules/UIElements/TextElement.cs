// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal interface ITextElement
    {
        string text { get; set; }
    }

    public class TextElement : VisualElement, ITextElement
    {
        public new class UxmlFactory : UxmlFactory<TextElement, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
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

        internal const string k_TextElementClass = "textElement";
        public TextElement()
        {
            requireMeasureFunction = true;
            AddToClassList(k_TextElementClass);
        }

        [SerializeField]
        private string m_Text;
        public virtual string text
        {
            get { return m_Text ?? String.Empty; }
            set
            {
                if (m_Text == value)
                    return;

                m_Text = value;
                IncrementVersion(VersionChangeType.Layout);

                if (!string.IsNullOrEmpty(persistenceKey))
                    SavePersistentData();
            }
        }

        protected override void DoRepaint(IStylePainter painter)
        {
            var stylePainter = (IStylePainterInternal)painter;
            stylePainter.DrawText(text);
        }

        public Vector2 MeasureTextSize(string textToMeasure, float width, MeasureMode widthMode, float height,
            MeasureMode heightMode)
        {
            return MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode);
        }

        internal static Vector2 MeasureVisualElementTextSize(VisualElement ve, string textToMeasure, float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            // TODO: This scaling parameter should depend on the real scaling of the text (dpi scaling * world scaling)
            //       because depending of its value, the glyphs may align on different pixels which can change the
            //       measure. The resulting measure should then be divided by this scaling to obtain the local measure.
            float scaling = 1;

            Font usedFont = ve.style.font;
            if (textToMeasure == null || usedFont == null)
                return new Vector2(measuredWidth, measuredHeight);

            if (widthMode == MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = TextStylePainterParameters.GetDefault(ve, textToMeasure);
                textParams.text = textToMeasure;
                textParams.font = usedFont;
                textParams.wordWrapWidth = 0.0f;
                textParams.wordWrap = false;
                textParams.richText = true;

                measuredWidth = TextNative.ComputeTextWidth(textParams.GetTextNativeSettings(scaling));

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
                var textParams = TextStylePainterParameters.GetDefault(ve, textToMeasure);
                textParams.text = textToMeasure;
                textParams.font = usedFont;
                textParams.wordWrapWidth = measuredWidth;
                textParams.richText = true;

                measuredHeight = TextNative.ComputeTextHeight(textParams.GetTextNativeSettings(scaling));

                if (heightMode == MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            return new Vector2(measuredWidth, measuredHeight);
        }

        protected internal override Vector2 DoMeasure(float width, MeasureMode widthMode, float height,
            MeasureMode heightMode)
        {
            return MeasureTextSize(text, width, widthMode, height, heightMode);
        }
    }
}
