// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal readonly struct BindingTarget
    {
        public readonly VisualElement element;
        public readonly BindingId bindingId;

        public BindingTarget(VisualElement element, BindingId bindingId)
        {
            this.element = element;
            this.bindingId = bindingId;
        }
    }
}
