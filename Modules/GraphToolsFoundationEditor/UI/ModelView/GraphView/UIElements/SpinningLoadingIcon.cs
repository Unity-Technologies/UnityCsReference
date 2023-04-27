// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class SpinningLoadingIcon : VisualElement
    {
        public static readonly string ussClassName = "ge-spinning-loading-icon";
        public static readonly string iconElementName = "icon";
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(iconElementName);
        public static readonly string rotateIconUssClassName = ussClassName.WithUssModifier("rotate");

        public SpinningLoadingIcon()
        {
            this.AddStylesheet_Internal("SpinningLoadingIcon.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = iconElementName };
            icon.AddToClassList(iconElementUssClassName);
            Add(icon);

            RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            schedule.Execute(() => AddToClassList(rotateIconUssClassName)).StartingIn(100);
        }

        void OnTransitionEnd(TransitionEndEvent evt)
        {
            RemoveFromClassList(rotateIconUssClassName);
            schedule.Execute(() => AddToClassList(rotateIconUssClassName));
        }
    }
}
