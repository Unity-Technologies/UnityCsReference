// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Widgets.Properties;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class ContextMenuButton : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new ContextMenuButton();
        }

        public event Action<ContextualMenuPopulateEvent> menuBuilding;

        public ContextMenuButton()
        {
            UIResources.CommonStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(CommonStyles.flatClickable);
            this.AddToTimelineClassList(CommonStyles.toolbarButton);

            var manipulator = new ContextualMenuManipulator(evt =>
            {
                menuBuilding?.Invoke(evt);
                evt.StopPropagation();
            });
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 1 });
            this.AddManipulator(manipulator);
        }

        public ContextMenuButton(Action<ContextualMenuPopulateEvent> builder) : this()
        {
            menuBuilding += builder;
        }
    }
}
