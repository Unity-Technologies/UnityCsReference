using System;
using System.Collections.Generic;
using UnityEngine.TextCore;

namespace UnityEngine.UIElements
{
    internal interface ITextElement
    {
        string text { get; set; }
    }

    /// <summary>
    /// Abstract base class for <see cref="VisualElement"/> containing text.
    /// </summary>
    public class TextElement : BindableElement, ITextElement, INotifyValueChanged<string>
    {
        /// <summary>
        /// Instantiates a <see cref="TextElement"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<TextElement, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextElement"/>.
        /// </summary>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Enumerator to get the child elements of the <see cref="UxmlTraits"/> of <see cref="TextElement"/>.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initializer for the <see cref="UxmlTraits"/> for the <see cref="TextElement"/>.
            /// </summary>
            /// <param name="ve"><see cref="VisualElement"/> to initialize.</param>
            /// <param name="bag">Bag of attributes where to get the value from.</param>
            /// <param name="cc">Creation Context, not used.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((ITextElement)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-text-element";

        public TextElement()
        {
            requireMeasureFunction = true;
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private TextHandle m_TextHandle = TextHandle.New();

        // For automated testing purposes
        internal TextHandle textHandle { get { return m_TextHandle; } }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_TextHandle.useLegacy = e.destinationPanel.contextType == ContextType.Editor;
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateVisibleText();
        }

        [SerializeField]
        private string m_Text = String.Empty;
        public virtual string text
        {
            get { return ((INotifyValueChanged<string>) this).value; }
            set
            {
                ((INotifyValueChanged<string>) this).value = value;
            }
        }

        private bool m_DisplayTooltipWhenElided = true;

        /// <summary>
        /// When true, a tooltip displays the full version of elided text.
        /// </summary>
        public bool displayTooltipWhenElided
        {
            get { return m_DisplayTooltipWhenElided; }
            set
            {
                if (m_DisplayTooltipWhenElided != value)
                {
                    m_DisplayTooltipWhenElided = value;
                    UpdateVisibleText();
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Returns true if text is elided, false otherwise.
        /// </summary>
        /// <remarks>
        /// Text is elided when the element that contains it is not large enough to display the full text, and has the following style property settings.
        ///
        /// overflow: Overflow.Hidden
        /// whiteSpace: WhiteSpace.NoWrap
        /// textOverflow: TextOverflow.Ellipsis
        /// textOverflowPosition: TextOverflowPosition.<End | Start | Middle>
        ///
        /// The text Element hides elided text, and displays an ellipsis ('...') to indicate that there is hidden overflow content.
        /// </remarks>
        public bool isElided { get; private set; }

        internal static readonly string k_EllipsisText = @"..."; // Some web standards seem to suggest "\u2026" (horizontal ellipsis Unicode character)

        private bool m_WasElided;
        private bool m_UpdateTextParams = true;
        private MeshGenerationContextUtils.TextParams m_TextParams;
        private int m_PreviousTextParamsHashCode = Int32.MaxValue;

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            UpdateVisibleText();

            mgc.Text(m_TextParams, m_TextHandle, this.scaledPixelsPerPoint);

            m_UpdateTextParams = true;
        }

        internal string ElideText(string drawText, string ellipsisText, float width, TextOverflowPosition textOverflowPosition)
        {
            // Try full size first
            var size = MeasureTextSize(drawText, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            if (size.x <= width || string.IsNullOrEmpty(ellipsisText))
                return drawText;

            var minText = drawText.Length > 1 ? ellipsisText : drawText;
            var minSize = MeasureTextSize(minText, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            if (minSize.x >= width)
                return minText;

            // Text will need to be truncated somehow
            var drawTextMax = drawText.Length - 1;
            var prevFitMid = -1;
            var truncatedText = drawText;

            // Don't assume that k_EllipsisText takes as much space as any other string of the same length;
            // we will start by removing one character at a time
            var min = textOverflowPosition == TextOverflowPosition.Start ? 1 : 0;
            var max = (textOverflowPosition == TextOverflowPosition.Start ||
                textOverflowPosition == TextOverflowPosition.Middle) ? drawTextMax : drawTextMax - 1;
            var mid = (min + max) / 2;

            while (min <= max)
            {
                if (textOverflowPosition == TextOverflowPosition.Start)
                    truncatedText = ellipsisText + drawText.Substring(mid, drawTextMax - (mid - 1));
                else if (textOverflowPosition == TextOverflowPosition.End)
                    truncatedText = drawText.Substring(0, mid) + ellipsisText;
                else if (textOverflowPosition == TextOverflowPosition.Middle)
                    truncatedText = drawText.Substring(0, mid - 1) + ellipsisText +
                        drawText.Substring(drawTextMax - (mid - 1));

                size = MeasureTextSize(truncatedText, 0, MeasureMode.Undefined,
                    0, MeasureMode.Undefined);

                if (Math.Abs(size.x - width) < Mathf.Epsilon)
                    return truncatedText;

                if (textOverflowPosition == TextOverflowPosition.Start)
                {
                    if (size.x > width)
                    {
                        if (prevFitMid == mid - 1)
                            return ellipsisText + drawText.Substring(prevFitMid, drawTextMax - (prevFitMid - 1));
                        min = mid + 1;
                    }
                    else
                    {
                        max = mid - 1;
                        prevFitMid = mid;
                    }
                }
                else if (textOverflowPosition == TextOverflowPosition.End || textOverflowPosition == TextOverflowPosition.Middle)
                {
                    if (size.x > width)
                    {
                        if (prevFitMid == mid - 1)
                            if (textOverflowPosition == TextOverflowPosition.End)
                                return drawText.Substring(0, prevFitMid) + ellipsisText;
                            else
                                return drawText.Substring(0, prevFitMid - 1) + ellipsisText + drawText.Substring(drawTextMax - (prevFitMid - 1));
                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                        prevFitMid = mid;
                    }
                }

                mid = (min + max) / 2;
            }

            return truncatedText;
        }

        private void UpdateTooltip()
        {
            // We set the tooltip text if text gets truncated
            bool needsTooltip = displayTooltipWhenElided && isElided;

            if (needsTooltip)
            {
                if (!m_WasElided)
                {
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = this.text;
                    m_WasElided = true;
                }
            }
            else if (m_WasElided)
            {
                if (tooltip == this.text)
                    tooltip = null;
                m_WasElided = false;
            }
        }

        private void UpdateVisibleText()
        {
            var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(this, text);
            var textParamsHashCode = textParams.GetHashCode();
            if (m_UpdateTextParams || textParamsHashCode != m_PreviousTextParamsHashCode)
            {
                m_TextParams = textParams;
                if (m_TextParams.textOverflowMode == TextOverflowMode.Ellipsis)
                    m_TextParams.text = ElideText(m_TextParams.text, k_EllipsisText, m_TextParams.rect.width,
                        m_TextParams.textOverflowPosition);

                isElided = m_TextParams.textOverflowMode == TextOverflowMode.Ellipsis && m_TextParams.text != text;
                m_PreviousTextParamsHashCode = textParamsHashCode;
                m_UpdateTextParams = false;
                UpdateTooltip();
            }
        }

        /// <summary>
        /// Computes the size needed to display a text string based on element style values such as font, font-size, word-wrap, and so on.
        /// </summary>
        /// <param name="textToMeasure">The text to measure.</param>
        /// <param name="width">Suggested width. Can be zero.</param>
        /// <param name="widthMode">Width restrictions.</param>
        /// <param name="height">Suggested height.</param>
        /// <param name="heightMode">Height restrictions.</param>
        /// <returns>The horizontal and vertical size needed to display the text string.</returns>
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
            if (elementScaling.x + elementScaling.y <= 0 || ve.scaledPixelsPerPoint <= 0)
                return Vector2.zero;

            float pixelsPerPoint = ve.scaledPixelsPerPoint;
            float pixelOffset = 0.02f;
            float pointOffset = pixelOffset / pixelsPerPoint;

            if (widthMode == MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = GetTextSettings(ve, textToMeasure);
                textParams.wordWrap = false;
                textParams.richText = false;

                // Case 1215962: round up as yoga could decide to round down and text would start wrapping
                measuredWidth = textHandle.ComputeTextWidth(textParams, pixelsPerPoint);
                measuredWidth = measuredWidth < pointOffset ? 0 : AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint, pixelOffset);

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

                measuredHeight = textHandle.ComputeTextHeight(textParams, pixelsPerPoint);
                measuredHeight = measuredHeight < pointOffset ? 0 : AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint, pixelOffset);

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
            var style = ve.computedStyle;
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
                richText = true,
                textOverflowMode = MeshGenerationContextUtils.TextParams.GetTextOverflowMode(style),
                textOverflowPosition = style.unityTextOverflowPosition.value
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
