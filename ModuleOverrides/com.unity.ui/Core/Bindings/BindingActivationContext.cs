// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿namespace UnityEngine.UIElements
{
    /// <summary>
    /// Contains information passed to binding instances during registration and deregistration.
    /// </summary>
    public readonly struct BindingActivationContext
    {
        private readonly VisualElement m_TargetElement;
        private readonly BindingId m_BindingId;

        /// <summary>
        /// The target element of the binding.
        /// </summary>
        public VisualElement targetElement => m_TargetElement;

        /// <summary>
        /// The binding id being activated/deactivated.
        /// </summary>
        public BindingId bindingId => m_BindingId;

        internal BindingActivationContext(VisualElement element, BindingId property)
        {
            m_TargetElement = element;
            m_BindingId = property;
        }
    }
}
