// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;
using TextGenerator = UnityEngine.TextCore.Text.TextGenerator;
using TextUtilities = UnityEngine.UIElements.TextUtilities;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the title of an <see cref="IHasTitle"/> model using an <see cref="EditableLabel"/> to allow editing.
    /// </summary>
    class EditableTitlePart : GraphElementPart
    {
        public static readonly string ussClassName = "ge-editable-title-part";
        public static readonly string titleLabelName = "title";

        protected static readonly CustomStyleProperty<float> k_LodMinTextSize = new CustomStyleProperty<float>("--lod-min-text-size");
        protected static readonly CustomStyleProperty<float> k_WantedTextSize = new CustomStyleProperty<float>("--wanted-text-size");

        static UnityEngine.TextCore.Text.TextGenerationSettings s_TextGenerationSettings = new();

        /// <summary>
        /// Creates a new instance of the <see cref="EditableTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
        /// <param name="useEllipsis">Whether to use ellipsis when the text is too large.</param>
        /// <param name="setWidth">Whether to set the width of the element to its text width at 100%.</param>
        /// <returns>A new instance of <see cref="EditableTitlePart"/>.</returns>
        public static EditableTitlePart Create(string name, Model model, ModelView ownerElement, string parentClassName, bool multiline = false, bool useEllipsis = false, bool setWidth = true)
        {
            if (model is IHasTitle)
            {
                return new EditableTitlePart(name, model, ownerElement, parentClassName, multiline, useEllipsis, setWidth);
            }

            return null;
        }

        static float GetTextWidthWithFontSize(TextElement element, float fontSize)
        {
            var style = element.computedStyle;

            s_TextGenerationSettings.textSettings = TextUtilities.GetTextSettingsFrom(element);
            if ( ! s_TextGenerationSettings.textSettings )
                return 0;

            s_TextGenerationSettings.fontAsset = TextUtilities.GetFontAsset(element);
            if ( ! s_TextGenerationSettings.fontAsset )
                return 0;

            s_TextGenerationSettings.material = s_TextGenerationSettings.fontAsset.material;
            s_TextGenerationSettings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            s_TextGenerationSettings.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);
            s_TextGenerationSettings.wordWrap = style.whiteSpace == WhiteSpace.Normal;
            s_TextGenerationSettings.wordWrappingRatio = 0.4f;
            s_TextGenerationSettings.richText = element.enableRichText;
            s_TextGenerationSettings.overflowMode = TextOverflowMode.Overflow;
            s_TextGenerationSettings.characterSpacing = style.letterSpacing.value;
            s_TextGenerationSettings.wordSpacing = style.wordSpacing.value;
            s_TextGenerationSettings.paragraphSpacing = style.unityParagraphSpacing.value;

            s_TextGenerationSettings.inverseYAxis = true;

            s_TextGenerationSettings.text = element.text;
            s_TextGenerationSettings.screenRect = new Rect(0, 0, 32000, 32000);
            s_TextGenerationSettings.fontSize = fontSize;

            var size = TextGenerator.GetPreferredValues(s_TextGenerationSettings, TextHandle.layoutTextInfo);

            return size.x;
        }

        bool m_Multiline;
        bool m_UseEllipsis;
        bool m_SetWidth;

        string m_PreviousTitle;

        /// <summary>
        /// The current zoom level of the graph.
        /// </summary>
        protected float m_CurrentZoom;

        /// <summary>
        /// The root element of the part.
        /// </summary>
        protected VisualElement TitleContainer { get; set; }

        /// <summary>
        /// The title visual element that can be either a <see cref="Label"/> or an <see cref="EditableLabel"/>.
        /// </summary>
        public VisualElement TitleLabel { get; protected set; }

        /// <summary>
        /// The minimum readable size of the text, lod will try to make the text at least this size.
        /// </summary>
        public float LodMinTextSize { get; protected internal set; } = 12;

        /// <summary>
        /// The wanted text size at 100% zoom.
        /// </summary>
        public float WantedTextSize { get; protected set; }

        /// <inheritdoc />
        public override VisualElement Root => TitleContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
        /// <param name="useEllipsis">Whether to use ellipsis when the text is too large.</param>
        /// <param name="setWidth">Whether to leave the width of the element automated.</param>
        protected EditableTitlePart(string name, Model model, ModelView ownerElement, string parentClassName, bool multiline, bool useEllipsis, bool setWidth)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Multiline = multiline;
            m_UseEllipsis = useEllipsis;
            m_SetWidth = setWidth;
        }

        protected virtual bool HasEditableLabel => (m_Model as GraphElementModel).IsRenamable();

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasTitle)
            {
                TitleContainer = new VisualElement { name = PartName };
                TitleContainer.AddToClassList(ussClassName);
                TitleContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                CreateTitleLabel();

                container.Add(TitleContainer);

                TitleContainer.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }
        }

        protected void CreateTitleLabel()
        {
            if (HasEditableLabel)
            {
                TitleLabel = new EditableLabel { name = titleLabelName, EditActionName = "Rename", multiline = m_Multiline};
                TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
            }
            else
            {
                TitleLabel = new Label { name = titleLabelName };
            }

            if (m_UseEllipsis)
            {
                VisualElement labelContainer = new VisualElement();
                labelContainer.AddToClassList(ussClassName.WithUssElement("label-container"));
                labelContainer.Add(TitleLabel);
                TitleContainer.Add(labelContainer);
            }
            else
            {
                TitleContainer.Add(TitleLabel);
            }

            TitleLabel.AddToClassList(ussClassName.WithUssElement("title"));
            TitleLabel.AddToClassList(m_ParentClassName.WithUssElement("title"));
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (TitleLabel == null)
                return;

            bool labelTypeChanged = false;
            if ((TitleLabel is EditableLabel && !HasEditableLabel) ||
                (TitleLabel is Label && HasEditableLabel))

            {
                TitleContainer.Remove(m_UseEllipsis ? TitleLabel.parent : TitleLabel);
                CreateTitleLabel();
                labelTypeChanged = true;
            }

            var value = (m_Model as IHasTitle)?.DisplayTitle ?? String.Empty;

            if (value == m_PreviousTitle && !labelTypeChanged)
                return;

            m_PreviousTitle = value;
            if (TitleLabel is EditableLabel editableLabel)
                editableLabel.SetValueWithoutNotify(value);
            else if (TitleLabel is Label label)
                label.text = value;
            SetupWidthFromOriginalSize();

            if (labelTypeChanged)
                SetupLod();
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            TitleContainer.AddStylesheet_Internal("EditableTitlePart.uss");
        }

        /// <summary>
        /// Manage the custom styles used for this part.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            bool changed = false;
            if (e.customStyle.TryGetValue(k_LodMinTextSize, out var value) && value != LodMinTextSize)
            {
                LodMinTextSize = value;
                changed = true;
            }

            if (e.customStyle.TryGetValue(k_WantedTextSize, out value) && value != WantedTextSize)
            {
                WantedTextSize = value;
                changed = true;
            }

            if (changed)
            {
                SetupWidthFromOriginalSize();
                SetupLod();
            }
        }

        protected void OnRename(ChangeEvent<string> e)
        {
            m_OwnerElement.RootView.Dispatch(new RenameElementCommand(m_Model as IRenamable, e.newValue));
        }

        /// <summary>
        /// Place the focus on the TextField, if any.
        /// </summary>
        public void BeginEditing()
        {
            if( TitleLabel is EditableLabel editableLabel)
                editableLabel.BeginEditing();
        }

        /// <inheritdoc />
        public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            m_CurrentZoom = zoom;

            if (float.IsFinite(TitleLabel.layout.width))
                SetupLod();
            else
                Root.schedule.Execute(SetupLod).ExecuteLater(0);
        }

        void SetupLod()
        {
            if (WantedTextSize != 0 && m_CurrentZoom != 0)
            {
                TextElement te = null;
                if (TitleLabel is EditableLabel editableLabel)
                    te = editableLabel.MandatoryQ<Label>();
                else if (TitleLabel is Label label)
                    te = label;

                if (!string.IsNullOrEmpty(te?.text))
                {
                    float inverseZoom = 1 / m_CurrentZoom;

                    if (inverseZoom * LodMinTextSize > WantedTextSize)
                    {
                        TitleLabel.style.fontSize = LodMinTextSize * inverseZoom;
                    }
                    else
                    {
                        if (TitleLabel.style.fontSize.value != WantedTextSize)
                        {
                            TitleLabel.style.fontSize = WantedTextSize;
                        }
                    }
                }
                else
                {
                    TitleLabel.style.fontSize = WantedTextSize;
                }
            }
        }

        void SetupWidthFromOriginalSize()
        {
            TextElement te = null;
            if (TitleLabel is EditableLabel editableLabel)
                te = editableLabel.MandatoryQ<Label>();
            else if (TitleLabel is Label label)
                te = label;

            if (!m_SetWidth)
            {
                TitleLabel.parent.style.flexGrow = 1;
                return;
            }

            // postpone the execution because the first UpdateFromModel is called way to early for the te to be setup and WantedTextSize to be defined.
            te?.schedule.Execute(
                () =>
                {
                    if (WantedTextSize == 0)
                        return;

                    if (!string.IsNullOrEmpty(te.text))
                        TitleLabel.parent.style.width = te != null ? new StyleLength(8.0f + Mathf.Ceil(GetTextWidthWithFontSize(te, WantedTextSize))) : new StyleLength(StyleKeyword.Null);
                    else
                        TitleLabel.parent.style.width = StyleKeyword.Null;
                }).ExecuteLater(0);
        }
    }
}
