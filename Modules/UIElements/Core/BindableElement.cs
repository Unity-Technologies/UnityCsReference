// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Element that can be bound to a property. For more information, refer to [[wiki:UIE-uxml-element-BindableElement|UXML element BindableElement]].
    /// </summary>
    [UxmlElement]
    public partial class BindableElement : VisualElement, IBindable
    {
        internal const string k_BindingPathTooltip = "Default method to define a path to a serialized property. Most often used for Editor extensions and inspectors.";

        /// <summary>
        /// Binding object that will be updated.
        /// </summary>
        public IBinding binding { get; set; }

        /// <summary>
        /// Path of the target property to be bound.
        /// </summary>
        [Tooltip(k_BindingPathTooltip)]
        [UxmlAttribute("binding-path")]
        public string bindingPath { get; set; }
    }
}
