// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class EnumFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class EnumFlagsFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class TagFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class ImageFieldValueDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class FixedItemHeightDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class MultilineDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class MultilineTextFieldAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class LayerDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class BindingModeDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class DataSourceDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class AdvanceTextGeneratorDecoratorAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class BindingPathDrawerAttribute : PropertyAttribute { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class ConverterDrawerAttribute : PropertyAttribute
    {
        public bool isConverterToSource;
    }
}
