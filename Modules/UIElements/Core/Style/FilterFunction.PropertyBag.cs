// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements.Layout;
using static UnityEngine.UIElements.StyleSheets.Dimension;

namespace UnityEngine.UIElements
{
    internal partial struct FilterFunction
    {
        internal class PropertyBag : ContainerPropertyBag<FilterFunction>
        {
            class ParametersProperty : Property<FilterFunction, FixedBuffer4<FilterParameter>>
            {
                public override string Name { get; } = nameof(parameters);
                public override bool IsReadOnly { get; } = false;
                public override FixedBuffer4<FilterParameter> GetValue(ref FilterFunction container) => container.parameters;
                public override void SetValue(ref FilterFunction container, FixedBuffer4<FilterParameter> value) => container.parameters = value;
            }

            class FilterFunctionDefinitionProperty : Property<FilterFunction, FilterFunctionDefinition>
            {
                public override string Name { get; } = nameof(customDefinition);
                public override bool IsReadOnly { get; } = false;
                public override FilterFunctionDefinition GetValue(ref FilterFunction container) => container.customDefinition;
                public override void SetValue(ref FilterFunction container, FilterFunctionDefinition value) => container.customDefinition = value;
            }

            public PropertyBag()
            {
                AddProperty(new ParametersProperty());
                AddProperty(new FilterFunctionDefinitionProperty());
            }
        }
    }
}
