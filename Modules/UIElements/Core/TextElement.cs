// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;

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
    /// Use this as the super class if you are declaring a custom VisualElement that displays text. For example, <see cref="Button"/> or <see cref="Label"/> use this as their base class. For more information, refer to [[wiki:UIE-uxml-element-TextElement|UXML element TextElement]].
    /// </summary>
    public partial class TextElement : BindableElement, ITextElement, INotifyValueChanged<string>
    {
        internal static readonly BindingId displayTooltipWhenElidedProperty = nameof(displayTooltipWhenElided);
        internal static readonly BindingId emojiFallbackSupportProperty = nameof(emojiFallbackSupport);
        internal static readonly BindingId enableRichTextProperty = nameof(enableRichText);
        internal static readonly BindingId isElidedProperty = nameof(isElided);
        internal static readonly BindingId parseEscapeSequencesProperty = nameof(parseEscapeSequences);
        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId valueProperty = nameof(value);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(text), "text"),
                    new(nameof(enableRichText), "enable-rich-text"),
                    new(nameof(emojiFallbackSupport), "emoji-fallback-support"),
                    new(nameof(parseEscapeSequences), "parse-escape-sequences"),
                    new(nameof(isSelectable), "selectable", null, "selectable"),
                    new(nameof(doubleClickSelectsWord), "double-click-selects-word", null,"selectWordByDoubleClick", "select-word-by-double-click"),
                    new(nameof(tripleClickSelectsLine), "triple-click-selects-line", null, "selectLineByTripleClick", "select-line-by-triple-click"),
                    new(nameof(displayTooltipWhenElided), "display-tooltip-when-elided"),
                });
            }

            #pragma warning disable 649
            [SerializeField, MultilineTextField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField] bool enableRichText;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags enableRichText_UxmlAttributeFlags;
            [SerializeField] bool emojiFallbackSupport;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags emojiFallbackSupport_UxmlAttributeFlags;
            [SerializeField] bool parseEscapeSequences;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags parseEscapeSequences_UxmlAttributeFlags;
            [FormerlySerializedAs("selectable")]
            [SerializeField, UxmlAttribute("selectable")] bool isSelectable;
            [FormerlySerializedAs("selectable_UxmlAttributeFlags")]
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags isSelectable_UxmlAttributeFlags;
            [FormerlySerializedAs("selectWordByDoubleClick")]
            [SerializeField, UxmlAttribute("double-click-selects-word", "select-word-by-double-click")] bool doubleClickSelectsWord;
            [FormerlySerializedAs("selectWordByDoubleClick_UxmlAttributeFlags")]
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags doubleClickSelectsWord_UxmlAttributeFlags;
            [FormerlySerializedAs("selectLineByTripleClick")]
            [SerializeField, UxmlAttribute("triple-click-selects-line", "select-line-by-triple-click")] bool tripleClickSelectsLine;
            [FormerlySerializedAs("selectLineByTripleClick_UxmlAttributeFlags")]
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags tripleClickSelectsLine_UxmlAttributeFlags;
            [SerializeField] bool displayTooltipWhenElided;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags displayTooltipWhenElided_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new TextElement();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TextElement)obj;
                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.text = text;
                if (ShouldWriteAttributeValue(enableRichText_UxmlAttributeFlags))
                    e.enableRichText = enableRichText;
                if (ShouldWriteAttributeValue(emojiFallbackSupport_UxmlAttributeFlags))
                    e.emojiFallbackSupport = emojiFallbackSupport;
                if (ShouldWriteAttributeValue(parseEscapeSequences_UxmlAttributeFlags))
                    e.parseEscapeSequences = parseEscapeSequences;
                if (ShouldWriteAttributeValue(isSelectable_UxmlAttributeFlags))
                    e.isSelectable = isSelectable;
                if (ShouldWriteAttributeValue(doubleClickSelectsWord_UxmlAttributeFlags))
                    e.doubleClickSelectsWord = doubleClickSelectsWord;
                if (ShouldWriteAttributeValue(tripleClickSelectsLine_UxmlAttributeFlags))
                    e.tripleClickSelectsLine = tripleClickSelectsLine;
                if (ShouldWriteAttributeValue(displayTooltipWhenElided_UxmlAttributeFlags))
                    e.displayTooltipWhenElided = displayTooltipWhenElided;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="TextElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<TextElement, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_EnableRichText = new UxmlBoolAttributeDescription { name = "enable-rich-text", defaultValue = true };
            UxmlBoolAttributeDescription m_EmojiFallbackSupport = new UxmlBoolAttributeDescription { name = "emoji-fallback-support", defaultValue = true };
            UxmlBoolAttributeDescription m_ParseEscapeSequences = new UxmlBoolAttributeDescription { name = "parse-escape-sequences" };
            UxmlBoolAttributeDescription m_Selectable = new UxmlBoolAttributeDescription { name = "selectable" };
            UxmlBoolAttributeDescription m_SelectWordByDoubleClick = new UxmlBoolAttributeDescription { name = "select-word-by-double-click" };
            UxmlBoolAttributeDescription m_SelectLineByTripleClick = new UxmlBoolAttributeDescription { name = "select-line-by-triple-click" };
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
                textElement.emojiFallbackSupport = m_EmojiFallbackSupport.GetValueFromBag(bag, cc);
                textElement.isSelectable = m_Selectable.GetValueFromBag(bag, cc);
                textElement.parseEscapeSequences = m_ParseEscapeSequences.GetValueFromBag(bag, cc);
                textElement.selection.doubleClickSelectsWord = m_SelectWordByDoubleClick.GetValueFromBag(bag, cc);
                textElement.selection.tripleClickSelectsLine = m_SelectLineByTripleClick.GetValueFromBag(bag, cc);
                textElement.displayTooltipWhenElided = m_DisplayTooltipWhenElided.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-text-element";

        /// <summary>
        /// USS class name of selectable text elements.
        /// </summary>
        public static readonly string selectableUssClassName = ussClassName + "__selectable";

        /// <summary>
        /// Initializes and returns an instance of TextElement.
        /// </summary>
        public TextElement()
        {
            requireMeasureFunction = true;

            // We don't want the TextElement to be sequentially focusable through tab navigation by default.
            tabIndex = -1;

            uitkTextHandle = new UITKTextHandle(this);

            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
            edition.GetDefaultValueType = GetDefaultValueType;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        string GetDefaultValueType() { return ""; }

        internal UITKTextHandle uitkTextHandle { get; set; }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateVisibleText();
        }

        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            (attachEvent.destinationPanel as BaseVisualElementPanel)?.liveReloadSystem.RegisterTextElement(this);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            uitkTextHandle.RemoveTextInfoFromPermanentCache();
            uitkTextHandle.RemoveTextInfoFromTemporaryCache();
            (detachEvent.originPanel as BaseVisualElementPanel)?.liveReloadSystem.UnregisterTextElement(this);
        }

        private string m_Text = String.Empty;

        /// <summary>
        /// The text to be displayed.
        /// </summary>
        /// <remarks>
        /// Changing this value will implicitly invoke the <see cref="INotifyValueChanged{T}.value"/> setter, which will raise a <see cref="ChangeEvent{T}"/> of type string.
        /// </remarks>
        [CreateProperty]
        public virtual string text
        {
            get => ((INotifyValueChanged<string>) this).value;
            set => ((INotifyValueChanged<string>) this).value = value;
        }

        bool m_EnableRichText = true;

        /// <summary>
        /// When false, rich text tags will not be parsed.
        /// </summary>
        [CreateProperty]
        public bool enableRichText
        {
            get => m_EnableRichText;
            set
            {
                if (m_EnableRichText == value) return;
                m_EnableRichText = value;
                MarkDirtyRepaint();
                NotifyPropertyChanged(enableRichTextProperty);
            }
        }

        bool m_EmojiFallbackSupport = true;

        /// <summary>
        /// Specifies the order in which the system should look for Emoji characters when rendering text.
        /// If this setting is enabled, the global Emoji Fallback list will be searched first for characters defined as
        /// Emoji in the Unicode 14.0 standard.
        /// </summary>
        [CreateProperty]
        public bool emojiFallbackSupport
        {
            get => m_EmojiFallbackSupport;
            set
            {
                if (m_EmojiFallbackSupport == value) return;
                m_EmojiFallbackSupport = value;
                MarkDirtyRepaint();
                NotifyPropertyChanged(emojiFallbackSupportProperty);
            }
        }

        bool m_ParseEscapeSequences;

        /// <summary>
        /// Determines how escape sequences are displayed.
        /// When set to <c>true</c>, escape sequences (such as `\n`, `\t`)
        /// are parsed and transformed into their corresponding characters. For example,
        /// '\n' will insert a new line.
        /// When set to <c>false</c>, escape sequences are displayed as raw text
        /// (for example, `\n` is shown as the characters '\' followed by 'n').
        /// </summary>
        [CreateProperty]
        public bool parseEscapeSequences
        {
            get => m_ParseEscapeSequences;
            set
            {
                if (m_ParseEscapeSequences == value) return;

                m_ParseEscapeSequences = value;
                MarkDirtyRepaint();
                NotifyPropertyChanged(parseEscapeSequencesProperty);
            }
        }

        private bool m_DisplayTooltipWhenElided = true;

        /// <summary>
        /// When true, a tooltip displays the full version of elided text, and also if a tooltip had been previously
        /// provided, it will be overwritten.
        /// </summary>
        [CreateProperty]
        public bool displayTooltipWhenElided
        {
            get => m_DisplayTooltipWhenElided;
            set
            {
                if (m_DisplayTooltipWhenElided != value)
                {
                    m_DisplayTooltipWhenElided = value;
                    UpdateVisibleText();
                    MarkDirtyRepaint();
                    NotifyPropertyChanged(displayTooltipWhenElidedProperty);
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
        [CreateProperty(ReadOnly = true)]
        public bool isElided { get; private set; }

        internal static readonly string k_EllipsisText = @"..."; // Some web standards seem to suggest "\u2026" (horizontal ellipsis Unicode character)
        internal string elidedText;

        private bool m_WasElided;

        // Used in tests
        internal void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            UpdateVisibleText();

            if (TextUtilities.IsFontAssigned(this))
            {
                if (TextUtilities.IsAdvancedTextEnabledForElement(this))
                {
                    bool isSuccess = false;
                    var textInfo = uitkTextHandle.UpdateNative(ref isSuccess);
                    if (isSuccess)
                    {
                        mgc.DrawNativeText(textInfo, contentRect.min);

                        if (selection.HasSelection() && selectingManipulator.HasFocus())
                            DrawNativeHighlighting(mgc);
                        else if (!edition.isReadOnly && selection.isSelectable && selectingManipulator.RevealCursor())
                            DrawCaret(mgc);
                    }
                }
                else
                {
                    mgc.meshGenerator.textJobSystem.GenerateText(mgc, this);
                }
            }
        }

        internal void OnGenerateTextOver(MeshGenerationContext mgc)
        {
            if (selection.HasSelection() && selectingManipulator.HasFocus())
                DrawHighlighting(mgc);
            else if (!edition.isReadOnly && selection.isSelectable && selectingManipulator.RevealCursor())
                DrawCaret(mgc);

            if (ShouldElide() && uitkTextHandle.TextLibraryCanElide())
                isElided = uitkTextHandle.IsElided();

            UpdateTooltip();
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
                    truncatedText = (mid - 1 <= 0 ? "" : drawText.Substring(0, mid - 1)) + ellipsisText +
                        (drawTextMax - (mid - 1) <= 0 ? "" : drawText.Substring(drawTextMax - (mid - 1)));

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
                                return drawText.Substring(0, Mathf.Max(prevFitMid - 1, 0)) + ellipsisText + drawText.Substring(drawTextMax - Mathf.Max(prevFitMid - 1, 0));
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
            var shouldElide = ShouldElide();
            if (shouldElide && uitkTextHandle.TextLibraryCanElide())
            {
                //nothing to do, the text generation will elide the text and we will update the isElided after in OnGenerateVisualContent
            }
            else if (shouldElide)
            {
                elidedText = ElideText(text, k_EllipsisText, contentRect.width, computedStyle.unityTextOverflowPosition);
                isElided = shouldElide && !string.Equals(elidedText, text, StringComparison.Ordinal);
            }
            else
            {
                isElided = false;
            }
        }

        private bool ShouldElide()
        {
            return computedStyle.textOverflow == TextOverflow.Ellipsis && computedStyle.overflow == OverflowInternal.Hidden;
        }

        internal bool hasFocus => elementPanel != null && elementPanel.focusController?.GetLeafFocusedElement() == this;
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
            return TextUtilities.MeasureVisualElementTextSize(this, new RenderedText(textToMeasure), width, widthMode, height, heightMode);
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return TextUtilities.MeasureVisualElementTextSize(this, renderedText, desiredWidth, widthMode, desiredHeight, heightMode);
        }

        //INotifyValueChange
        string INotifyValueChanged<string>.value
        {
            get => m_Text ?? String.Empty;

            set
            {
                if (m_Text != value)
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(this.text, value))
                        {
                            evt.elementTarget = this;
                            ((INotifyValueChanged<string>) this).SetValueWithoutNotify(value);
                            SendEvent(evt);
                            NotifyPropertyChanged(valueProperty);
                            // We fire the property changed for text here because text simply assigns the value.
                            NotifyPropertyChanged(textProperty);
                        }
                    }
                    else
                    {
                        ((INotifyValueChanged<string>) this).SetValueWithoutNotify(value);
                    }
                }
            }
        }

        [CreateProperty]
        private string value
        {
            get => ((INotifyValueChanged<string>) this).value;
            set => ((INotifyValueChanged<string>) this).value = value;
        }

        void INotifyValueChanged<string>.SetValueWithoutNotify(string newValue)
        {
            newValue = ((ITextEdition)this).CullString(newValue);
            if (m_Text != newValue)
            {
                SetRenderedText(newValue);
                m_Text = newValue;

                //No need to dirty the layout if the element's size is not affected by the text change
                if (computedStyle.height.IsAuto() || computedStyle.height.IsNone() || (computedStyle.width.IsAuto() || computedStyle.width.IsNone())  )
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                else
                    IncrementVersion(VersionChangeType.Repaint);

                if (!string.IsNullOrEmpty(viewDataKey))
                    SaveViewData();
            }

            // Always sync the manipulator if it exists even if the element is read-only or disabled. See issue UUM-8802
            if (editingManipulator != null)
                editingManipulator.editingUtilities.text = newValue;
        }
    }
}
