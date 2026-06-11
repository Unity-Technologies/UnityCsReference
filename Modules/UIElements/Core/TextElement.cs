// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Collections;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
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
    [UxmlElement]
    public partial class TextElement : BindableElement, ITextElement, INotifyValueChanged<string>
    {
        internal static readonly BindingId displayTooltipWhenElidedProperty = nameof(displayTooltipWhenElided);
        internal static readonly BindingId emojiFallbackSupportProperty = nameof(emojiFallbackSupport);
        internal static readonly BindingId enableRichTextProperty = nameof(enableRichText);
        internal static readonly BindingId isElidedProperty = nameof(isElided);
        internal static readonly BindingId parseEscapeSequencesProperty = nameof(parseEscapeSequences);
        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId valueProperty = nameof(value);

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-text-element";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of selectable text elements.
        /// </summary>
        public static readonly string selectableUssClassName = ussClassName + "__selectable";
        internal static readonly UniqueStyleString selectableUssClassNameUnique = new(selectableUssClassName);

        /// <summary>
        /// Initializes and returns an instance of TextElement.
        /// </summary>
        public TextElement()
        {
            requireMeasureFunction = true;

            // We don't want the TextElement to be sequentially focusable through tab navigation by default.
            tabIndex = -1;

            uitkTextHandle = new UITKTextHandle(this);

            AddToClassList(ussClassNameUnique);

            generateVisualContent += OnGenerateVisualContent;
            edition.GetDefaultValueType = GetDefaultValueType;
        }

        string GetDefaultValueType() { return string.Empty; }

        /// <summary>
        /// Callback fired after UI Toolkit has generated the vertex data for a
        /// <see cref="TextElement"/> and before the geometry is sent to the renderer.
        /// Use it to inspect or modify each glyph’s quad (position, tint, UVs, etc.) to
        /// implement custom per‑glyph effects.
        /// </summary>
        public Action<GlyphsEnumerable> PostProcessTextVertices { get; set; }
        internal UITKTextHandle uitkTextHandle { get; set; }

        // From SelectingManipulator.HandleEventBubbleUp, EditingManipulator.HandleEventBubbleUp
        [EventInterest(typeof(ContextualMenuPopulateEvent), typeof(KeyDownEvent), typeof(KeyUpEvent),
            typeof(ValidateCommandEvent), typeof(ExecuteCommandEvent),
            typeof(FocusEvent), typeof(BlurEvent), typeof(FocusInEvent), typeof(FocusOutEvent),
            typeof(PointerDownEvent), typeof(PointerUpEvent), typeof(PointerMoveEvent),
            typeof(NavigationMoveEvent), typeof(NavigationSubmitEvent), typeof(NavigationCancelEvent), typeof(IMEEvent),
            typeof(GeometryChangedEvent), typeof(AttachToPanelEvent), typeof(DetachFromPanelEvent)
        )]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt.target == this)
            {
                switch (evt)
                {
                    case GeometryChangedEvent:
                        UpdateVisibleText();
                        return;

                    case AttachToPanelEvent ape:
                        OnAttachToPanel(ape);
                        return;
                    case DetachFromPanelEvent dpe:
                        OnDetachFromPanel(dpe);
                        return;
                }
            }

            if (selection.isSelectable)
            {
                EditionHandleEvent(evt);
            }
        }


        void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            // All panels should account for TextElement  LiveReload in the Editor
            // And otherwise we only register them if ATG is effectively used
            (attachEvent.destinationPanel as BaseVisualElementPanel)?.textElementRegistry.Value.Add(this);

            if (m_Text != null && m_Text.Length > 0)
                m_TextBuffer.CopyFrom(m_Text);

            uitkTextHandle.ReleaseResourcesIfPossible();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            uitkTextHandle.RemoveFromPermanentCache();
            uitkTextHandle.RemoveFromTemporaryCache();
            var textRegistry = (detachEvent.originPanel as BaseVisualElementPanel)?.textElementRegistry;
            if (textRegistry != null && textRegistry.IsValueCreated)
                textRegistry.Value.Remove(this);
            m_TextBuffer.Dispose();
            uitkTextHandle.ReleaseResourcesIfPossible();
        }

        private string m_Text = String.Empty;
        NativeTextBuffer m_TextBuffer;
        bool m_IsTextBufferDirty;

        internal ref NativeTextBuffer textBuffer => ref m_TextBuffer;

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

        /// <summary>
        /// Sets the text content without allocating a managed string.
        /// </summary>
        /// <remarks>
        /// Unlike assigning to <see cref="text"/>, this method writes directly into an internal
        /// native buffer and defers string materialization until <see cref="text"/> is read.
        /// A <see cref="ChangeEvent{T}"/> of type <c>string</c> is raised only when listeners
        /// for value-change events exist in the element's hierarchy; when no such listeners are
        /// present, the method is fully allocation-free.
        /// </remarks>
        /// <param name="text">The character span to set as the element's text content.</param>
        public void SetText(ReadOnlySpan<char> text)
        {
            if (panel == null)
            {
                Debug.LogWarning("TextElement.SetText() called while the element is not attached to a panel. Falling back to string allocation.");
                this.text = new string(text);
                return;
            }

            int length = text.Length;
            int maxLen = edition.maxLength;
            if (maxLen >= 0 && length > maxLen)
                length = maxLen;

            if (IsBufferEqualTo(text, length))
                return;

            m_TextBuffer.CopyFrom(text, length);

            ApplyBufferChange(length);
        }

        /// <summary>
        /// Sets the text content from a character array slice without allocating a managed string.
        /// </summary>
        /// <param name="sourceText">The source character array.</param>
        /// <param name="start">The starting index in the array.</param>
        /// <param name="length">The number of characters to copy.</param>
        public void SetText(char[] sourceText, int start, int length)
        {
            if (sourceText == null)
                throw new ArgumentNullException(nameof(sourceText));
            if ((uint)start > (uint)sourceText.Length || (uint)length > (uint)(sourceText.Length - start))
                throw new ArgumentOutOfRangeException();
            SetText(new ReadOnlySpan<char>(sourceText, start, length));
        }

        /// <summary>
        /// Sets the text content from a <see cref="StringBuilder"/> without allocating a managed string.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> whose contents to copy.</param>
        public void SetText(StringBuilder sb)
        {
            if (panel == null)
            {
                Debug.LogWarning("TextElement.SetText() called while the element is not attached to a panel. Falling back to string allocation.");
                this.text = sb?.ToString() ?? string.Empty;
                return;
            }

            if (sb == null || sb.Length == 0)
            {
                SetText(ReadOnlySpan<char>.Empty);
                return;
            }

            int length = sb.Length;
            int maxLen = edition.maxLength;
            if (maxLen >= 0 && length > maxLen)
                length = maxLen;

            if (IsBufferEqualTo(sb, length))
                return;

            m_TextBuffer.EnsureCapacity(length);
            for (int i = 0; i < length; i++)
                m_TextBuffer[i] = sb[i];

            ApplyBufferChange(length);
        }

        /// <summary>
        /// Formats a float value directly into the text buffer without allocating a managed string.
        /// </summary>
        /// <param name="value">The float value to display.</param>
        /// <param name="format">An optional standard or custom numeric format string.</param>
        public void SetText(float value, string format = null)
        {
            Span<char> buffer = stackalloc char[64];
            if (value.TryFormat(buffer, out int charsWritten, format.AsSpan()))
                SetText((ReadOnlySpan<char>)buffer.Slice(0, charsWritten));
            else
                text = value.ToString(format);
        }

        /// <summary>
        /// Formats an integer value directly into the text buffer without allocating a managed string.
        /// </summary>
        /// <param name="value">The integer value to display.</param>
        public void SetText(int value)
        {
            Span<char> buffer = stackalloc char[12];
            if (value.TryFormat(buffer, out int charsWritten))
                SetText((ReadOnlySpan<char>)buffer.Slice(0, charsWritten));
            else
                text = value.ToString();
        }

        bool IsBufferEqualTo(ReadOnlySpan<char> span, int length)
        {
            if (!m_TextBuffer.isCreated)
                return length == 0 && m_TextBuffer.length == 0;
            if (m_TextBuffer.length != length)
                return false;
            for (int i = 0; i < length; i++)
                if (m_TextBuffer[i] != span[i])
                    return false;
            return true;
        }

        bool IsBufferEqualTo(StringBuilder sb, int length)
        {
            if (!m_TextBuffer.isCreated)
                return length == 0 && m_TextBuffer.length == 0;
            if (m_TextBuffer.length != length)
                return false;
            for (int i = 0; i < length; i++)
                if (m_TextBuffer[i] != sb[i])
                    return false;
            return true;
        }

        void ApplyBufferChange(int newLength)
        {
            string previousText = m_Text;

            m_TextBuffer.length = newLength;
            m_IsTextBufferDirty = true;
            m_Text = null;
            isElided = false;

            if (AnySizeAutoOrNone(ref computedStyle))
                IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
            else
                IncrementVersion(VersionChangeType.Repaint);

            if (!string.IsNullOrEmpty(viewDataKey))
                SaveViewData();

            if (panel != null && HasParentEventInterests(EventCategory.ChangeValue))
            {
                m_Text = m_TextBuffer.Materialize();
                SetRenderedText(m_Text);
                m_IsTextBufferDirty = false;
                using (var evt = ChangeEvent<string>.GetPooled(previousText ?? string.Empty, m_Text))
                {
                    evt.elementTarget = this;
                    SendEvent(evt);
                }
            }

            NotifyPropertyChanged(valueProperty);
            NotifyPropertyChanged(textProperty);

            if (editingManipulator != null)
                editingManipulator.editingUtilities.text = this.text;
        }
        
        [MultilineTextField(displayName = "Text")]
        [UxmlAttribute("text"), UxmlAttributeBindingPath(nameof(text))]
        internal string textUXML
        {
            get => text;
            set => text = value;
        }

        bool m_EnableRichText = true;

        /// <summary>
        /// When false, rich text tags will not be parsed.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
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
        [UxmlAttribute]
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
        [UxmlAttribute]
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

        [CreateProperty]
        [SelectableTextElement]
        [UxmlAttribute("selectable")]
        internal bool isSelectable
        {
            get => selection.isSelectable;
            set => selection.isSelectable = value;
        }

        [CreateProperty]
        [UxmlAttribute(obsoleteNames = new string[]{"selectWordByDoubleClick", "select-word-by-double-click"})]
        internal bool doubleClickSelectsWord
        {
            get => selection.doubleClickSelectsWord;
            set => selection.doubleClickSelectsWord = value;
        }

        [CreateProperty]
        [UxmlAttribute(obsoleteNames = new string[] { "selectLineByTripleClick", "select-line-by-triple-click" })]
        internal bool tripleClickSelectsLine
        {
            get => selection.tripleClickSelectsLine;
            set => selection.tripleClickSelectsLine = value;
        }

        private bool m_DisplayTooltipWhenElided = true;

        /// <summary>
        /// When true, a tooltip displays the full version of elided text, and also if a tooltip had been previously
        /// provided, it will be overwritten.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
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
        internal static void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (mgc.visualElement is TextElement element)
            {
                element.UpdateVisibleText();

                if (TextUtilities.IsFontAssigned(element))
                {
                    element.uitkTextHandle.ReleaseResourcesIfPossible();
                    mgc.meshGenerator.textJobSystem.GenerateText(mgc, element);
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

        internal void OnGenerateTextOverNative(MeshGenerationContext mgc)
        {
            if (selection.HasSelection() && selectingManipulator.HasFocus())
                DrawNativeHighlighting(mgc);
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
            var size = MeasureTextSize(drawText, float.NaN, MeasureMode.Undefined, float.NaN, MeasureMode.Undefined);
            if (size.x <= (width + extraWidth) || string.IsNullOrEmpty(ellipsisText))
                return drawText;

            var minText = drawText.Length > 1 ? ellipsisText : drawText;
            var minSize = MeasureTextSize(minText, float.NaN, MeasureMode.Undefined, float.NaN, MeasureMode.Undefined);
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

                size = MeasureTextSize(truncatedText, float.NaN, MeasureMode.Undefined,
                    float.NaN, MeasureMode.Undefined);

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
            return TextUtilities.MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode);
        }

        /// <summary>
        /// Computes the size needed to display a text string based on element style values such as font, font-size, word-wrap, and so on.
        /// </summary>
        /// <param name="textToMeasure">The text to measure.</param>
        /// <param name="width">Suggested width. Can be zero.</param>
        /// <param name="widthMode">Width restrictions.</param>
        /// <param name="height">Suggested height.</param>
        /// <param name="heightMode">Height restrictions.</param>
        /// <param name="fontsize">Optional parameter that override the fontSize that would be applied on the visualElement.</param>
        /// <returns>The horizontal and vertical size needed to display the text string.</returns>
        public Vector2 MeasureTextSize(string textToMeasure, float width, MeasureMode widthMode, float height, MeasureMode heightMode, float? fontsize = null)
        {
            return TextUtilities.MeasureVisualElementTextSize(this, textToMeasure, width, widthMode, height, heightMode, fontsize);
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            if (TextUtilities.IsAdvancedTextEnabledForElement(this))
            {
                return TextUtilities.MeasureVisualElementTextSize(this, null, desiredWidth, widthMode, desiredHeight, heightMode);
            }
            else
            {
                return TextUtilities.MeasureVisualElementTextSize(this, renderedText, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }

        //INotifyValueChange
        string INotifyValueChanged<string>.value
        {
            get
            {
                if (m_IsTextBufferDirty)
                {
                    m_Text = m_TextBuffer.Materialize();
                    m_IsTextBufferDirty = false;
                }
                return m_Text ?? string.Empty;
            }

            set
            {
                if (m_IsTextBufferDirty)
                {
                    m_Text = m_TextBuffer.Materialize();
                    m_IsTextBufferDirty = false;
                }
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

        static internal bool AnySizeAutoOrNone(ref ComputedStyle computedStyle)
        {
            return computedStyle.height.IsAuto() || computedStyle.height.IsNone() || computedStyle.width.IsAuto() || computedStyle.width.IsNone();
        }

        void INotifyValueChanged<string>.SetValueWithoutNotify(string newValue)
        {
            newValue = ((ITextEdition)this).CullString(newValue);
            if (m_IsTextBufferDirty)
            {
                m_Text = m_TextBuffer.Materialize();
                m_IsTextBufferDirty = false;
            }
            if (m_Text != newValue)
            {
                SetRenderedText(newValue);
                m_Text = newValue;
                isElided = false;
                if (panel != null)
                    m_TextBuffer.CopyFrom(newValue);

                //No need to dirty the layout if the element's size is not affected by the text change
                if (AnySizeAutoOrNone(ref computedStyle))
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                else
                    IncrementVersion(VersionChangeType.Repaint);

                if (!string.IsNullOrEmpty(viewDataKey))
                    SaveViewData();
            }

            // Always sync the manipulator if it exists even if the element is read-only or disabled. See issue UUM-8802
            if (editingManipulator != null)
                editingManipulator.editingUtilities.text = newValue;
            else if (uitkTextHandle.IsCachedPermanentATG)
                TextEditingService.SetText(uitkTextHandle.textGenerationInfo, newValue);
        }

        /// <summary>
        /// Marks that the <see cref="TextElement"/> forces a layout and repaint.
        /// </summary>
        /// <remarks>
        /// Call this method if you modify assets that influence text generation at runtime,
        /// such as a <see cref="FontAsset"/>.
        /// </remarks>
        public void MarkDirtyText()
        {
            IncrementVersion(VersionChangeType.Repaint | VersionChangeType.Layout);
            uitkTextHandle.SetDirty();
        }

        internal FontAsset cachedFontAsset { get; private set; }
        internal void RefreshCachedFontAsset()
        {
            cachedFontAsset = TextUtilities.GetFontAssetFromStyle_MainThreadOnly(this);
        }
    }
}
