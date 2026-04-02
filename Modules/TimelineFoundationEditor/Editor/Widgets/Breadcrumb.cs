// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class Breadcrumb : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Breadcrumb();
        }

        const string k_Class = "breadcrumb";
        const string k_ClassSelected = k_Class + "--selected";
        const string k_ClassIcon = "icon";
        const string k_ClassTitle = "title";

        static readonly TemplateResource k_Template = UIResources.TemplateFactory.Get<Breadcrumb>();
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<Breadcrumb>();

        public Breadcrumb() { }

        public Breadcrumb(string label, Texture icon, Action onClick)
        {
            k_Template.CloneInto(this);
            style.flexDirection = FlexDirection.Row;
            name = k_Class;

            UIResources.CommonStylesheet.ApplyTo(this);
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Class);
            this.AddManipulator(new Clickable(onClick));

            var imageElement = this.Q<Image>(k_ClassIcon);
            imageElement.image = icon;
            imageElement.pickingMode = PickingMode.Ignore;
            imageElement.focusable = false;

            var title = this.Q<Label>(k_ClassTitle);
            title.text = label;
            title.pickingMode = PickingMode.Ignore;
            title.focusable = false;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                this.AddToTimelineClassList(k_ClassSelected);
            else
                this.RemoveFromTimelineClassList(k_ClassSelected);
        }
    }
}
