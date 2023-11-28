// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    class EnumFieldValueDecoratorAttribute : PropertyAttribute { }
    class EnumFlagsFieldValueDecoratorAttribute : PropertyAttribute { }
    class TagFieldValueDecoratorAttribute : PropertyAttribute { }
    class ImageFieldValueDecoratorAttribute : PropertyAttribute { }
    class FixedItemHeightDecoratorAttribute : PropertyAttribute { }
    class MultilineDecoratorAttribute : PropertyAttribute { }
    class MultilineTextFieldAttribute : PropertyAttribute { }
    class LayerDecoratorAttribute : PropertyAttribute { }
    class BindingModeDrawerAttribute : PropertyAttribute { }
    class DataSourceDrawerAttribute : PropertyAttribute { }
    class BindingPathDrawerAttribute : PropertyAttribute { }

    class DataSourceTypeDrawerAttribute : UxmlTypeReferenceAttribute
    {
        public DataSourceTypeDrawerAttribute(Type baseType)
            : base(baseType) { }
    }

    class ConverterDrawerAttribute : PropertyAttribute
    {
        public bool isConverterToSource;
    }
}
