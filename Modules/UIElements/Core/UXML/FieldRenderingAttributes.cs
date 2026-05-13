// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    abstract class PropertyAttributeWithDisplayName : PropertyAttribute
    {
        public string displayName;
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class EnumFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class EnumFlagsFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class TagFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class ImageFieldValueDecoratorAttribute : PropertyAttributeWithDisplayName { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class FixedItemHeightDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class SelectableTextElementAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class MultilineDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class MultilineTextFieldAttribute() : PropertyAttributeWithDisplayName { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class LayerDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class BindingModeDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class DataSourceDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class BindingPathDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class ConverterDrawerAttribute : PropertyAttribute
    {
        public bool isConverterToSource;
    }
}
