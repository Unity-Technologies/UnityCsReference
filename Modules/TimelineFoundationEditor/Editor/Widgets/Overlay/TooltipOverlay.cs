// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class TooltipOverlay : CanvasOverlay
    {
        const string k_Style = "tooltipOverlay";
        const string k_Name = "tooltipOverlay";
        const string k_LabelName = "label";

        Label m_LabelElement;

        public string labelText
        {
            get => m_LabelElement.text;
            set => m_LabelElement.text = value;
        }

        public TooltipOverlay(string labelText = "")
        {
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);
            name = k_Name;

            CreateLabel(labelText);
        }

        void CreateLabel(string aLabelText)
        {
            m_LabelElement = new Label(aLabelText) { name = k_LabelName };
            Add(m_LabelElement);
        }
    }
}
