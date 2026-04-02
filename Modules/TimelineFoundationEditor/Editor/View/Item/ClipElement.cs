// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    class ClipElement : ItemElement, ISelectableElement
    {
        const string k_Style = "clipElement";
        const string k_LabelName = "label";
        const string k_ClipInIndicatorName = "clipInIndicator";
        const string k_Foreground = "foreground";
        const string k_Background = "background";

        static readonly TemplateResource k_Template = Internals.UIResources.TemplateFactory.Get<ClipElement>();
        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<ClipElement>();

        readonly VisualElement m_ClipInIndicator;
        readonly Label m_Label;
        static readonly string k_DefaultLabelText = "Label";

        protected VisualElement foreground { get; }
        protected VisualElement background { get; }

        protected Label label => m_Label;

        public ClipElement(ItemElementContext context) : base(context)
        {
            this.AddToTimelineClassList(k_Style);
            k_Template.CloneInto(this);
            k_Stylesheet.ApplyTo(this);

            m_Label = this.Q<Label>(k_LabelName);
            m_Label.text = k_DefaultLabelText;
            m_ClipInIndicator = this.Q<VisualElement>(k_ClipInIndicatorName);
            foreground = this.Q<VisualElement>(k_Foreground);
            background = this.Q<VisualElement>(k_Background);

            foreground.pickingMode = PickingMode.Ignore;
            background.pickingMode = PickingMode.Ignore;

            UpdateVisuals();
        }

        public override void OnItemChanged()
        {
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            m_Label.text = item.name;
            m_ClipInIndicator.visible = item.contentRange.start > DiscreteTime.Zero;
        }

        public bool supportsMultiSelect => true;

        public virtual void OnSelectionStateChanged(bool selected)
        {
            this.selected = selected;
        }
    }
}
