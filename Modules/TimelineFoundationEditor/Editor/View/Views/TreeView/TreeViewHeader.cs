// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    [UxmlElement]
    internal sealed partial class TreeViewHeader : VisualElement
    {
        const string k_ControlsHeader = "controlsHeader";
        const string k_ContentsHeader = "contentsHeader";

        static readonly TemplateResource k_Template = Internals.UIResources.TemplateFactory.Get<TreeViewHeader>();

        public VisualElement ControlsHeader { get; }
        public VisualElement ContentsHeader { get; }

        public TreeViewHeader()
        {
            k_Template.CloneInto(this);

            style.flexDirection = FlexDirection.Row;
            ControlsHeader = this.Q(k_ControlsHeader);
            ContentsHeader = this.Q(k_ContentsHeader);
        }

        public float GetControlsWidth() => ControlsHeader.localBound.width;
        public float GetContentsWidth() => ContentsHeader.localBound.width;
        public bool GetControlsVisibility() => ControlsHeader.resolvedStyle.display == DisplayStyle.Flex;

        public void SetControlsVisibility(bool visibilityState)
        {
            ControlsHeader.style.display = visibilityState ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetControlsWidth(float width)
        {
            ControlsHeader.style.width = width;
        }
    }
}
