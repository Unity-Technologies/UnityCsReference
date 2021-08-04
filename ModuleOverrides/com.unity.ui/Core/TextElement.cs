// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal interface ITextElement
    {
        string text { get; set; }
    }

    /// <summary>
    /// Base class for a <see cref="VisualElement"/> that displays text.
    /// </summary>
    /// <summary>
    /// Use this as the super class if you are declaring a custom VisualElement that displays text. For example, <see cref="Button"/> or <see cref="Label"/> use this as their base class.
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
            UxmlBoolAttributeDescription m_EnableRichText = new UxmlBoolAttributeDescription { name = "enable-rich-text", defaultValue = true };

            UxmlBoolAttributeDescription m_DisplayTooltipWhenElided = new UxmlBoolAttributeDescription { name = "display-tooltip-when-elided" };

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

                var textElement = (TextElement)ve;
                textElement.text = m_Text.GetValueFromBag(bag, cc);
                textElement.enableRichText = m_EnableRichText.GetValueFromBag(bag, cc);
                textElement.displayTooltipWhenElided = m_DisplayTooltipWhenElided.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-text-element";

        /// <summary>
        /// Initializes and returns an instance of TextElement.
        /// </summary>
        public TextElement()
        {
            requireMeasureFunction = true;
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private ITextHandle m_TextHandle;

        // For automated testing purposes
        internal ITextHandle textHandle
        {
            get { return m_TextHandle; }
            set { m_TextHandle = value; }
        }

        internal static int maxTextVertices = MeshBuilder.s_MaxTextMeshVertices;

        public override void HandleEvent(EventBase evt)
        {
            if (evt.eventTypeId == AttachToPanelEvent.TypeId() && evt is AttachToPanelEvent attachEvent)
            {
                textHandle = TextCoreHandle.New();
                (attachEvent.destinationPanel as BaseVisualElementPanel)?.OnTextElementAdded(this);
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId() && evt is DetachFromPanelEvent detachEvent)
            {
                (detachEvent.originPanel as BaseVisualElementPanel)?.OnTextElementRemoved(this);
            }
            base.HandleEvent(evt);
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateVisibleText();
        }

        [SerializeField]
        private string m_Text = String.Empty;

        /// <summary>
        /// The text to be displayed.
        /// </summary>
        /// <remarks>
        /// Changing this value will implicitly invoke the <see cref="INotifyValueChanged{T}.value"/> setter, which will raise a <see cref="ChangeEvent{T}"/> of type string.
        /// </remarks>
        public virtual string text
        {
            get { return ((INotifyValueChanged<string>) this).value; }
            set
            {
                ((INotifyValueChanged<string>) this).value = value;
            }
        }

        private bool m_EnableRichText = true;

        /// <summary>
        /// When false, rich text tags will not be parsed.
        /// </summary>
        public bool enableRichText
        {
            get { return m_EnableRichText; }
            set
            {
                if (m_EnableRichText == value) return;
                m_EnableRichText = value;
                MarkDirtyRepaint();
            }
        }

        private bool m_DisplayTooltipWhenElided = true;

        /// <summary>
        /// When true, a tooltip displays the full version of elided text, and also if a tooltip had been previously
        /// provided, it will be overwritten.
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
            // Allow the text to partially overlap the right-padding area before showing ellipses for no good reason.
            // This is required as the content rect may be different than the measured text rect after pixel alignment.
            // See cases 1268016 and 1291452.
            float paddingRight = resolvedStyle.paddingRight;
            if (float.IsNaN(paddingRight))
                paddingRight = 0.0f; // Just in case the style isn't fully resolved yet
            float extraWidth = Mathf.Clamp(paddingRight, 1.0f / scaledPixelsPerPoint, 1.0f);

            // Try full size first
            var size = MeasureTextSize(drawText, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            if (size.x <= (width + extraWidth) || string.IsNullOrEmpty(ellipsisText))
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

                if (Math.Abs(size.x - width) < UIRUtility.k_Epsilon)
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
                // The elided text may have changed, but comparing to see if it really changed would be
                // heavier than just assigning and getting it done so let's just do it.
                tooltip = this.text;
                m_WasElided = true;
            }
            else if (m_WasElided)
            {
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
                var shouldElide = ShouldElide();
                if (shouldElide)
                    m_TextParams.text = ElideText(m_TextParams.text, k_EllipsisText, m_TextParams.rect.width, m_TextParams.textOverflowPosition);

                isElided = shouldElide && m_TextParams.text != text;
                m_PreviousTextParamsHashCode = textParamsHashCode;
                m_UpdateTextParams = false;
                UpdateTooltip();
            }
        }

        private bool ShouldElide()
        {
            return computedStyle.textOverflow == TextOverflow.Ellipsis && computedStyle.overflow == OverflowInternal.Hidden &&
                computedStyle.whiteSpace == WhiteSpace.NoWrap;
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
            return TextUtilities.MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode, m_TextHandle);
        }

        /// <summary>
        /// DO NOT USE MeasureVisualElementTextSize, use TextUtilities.MeasureVisualElementTextSize instead. This method is only there for backward compatibility reason and will soon be stripped.
        /// </summary>
        internal static Vector2 MeasureVisualElementTextSize(VisualElement ve, string textToMeasure, float width,
            MeasureMode widthMode, float height, MeasureMode heightMode, TextHandle textHandle)
        {
            return TextUtilities.MeasureVisualElementTextSize(ve, textToMeasure, width, widthMode, height, heightMode,
                textHandle.textHandle);
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return MeasureTextSize(text, desiredWidth, widthMode, desiredHeight, heightMode);
        }

        // Used in tests
        internal int VerticesCount(string text)
        {
            var textParams = m_TextParams;
            textParams.text = text;
            return textHandle.VerticesCount(textParams, scaledPixelsPerPoint);
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
