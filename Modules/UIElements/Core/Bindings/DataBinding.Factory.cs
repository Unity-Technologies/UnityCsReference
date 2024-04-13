// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    // Important Note:
    // Data binding uses the UxmlSerialization system and does not support UxmlTraits/UxmlFactories.
    // <see cref="VisualElement"/>'s traits do not contain the binding attribute definition, only its uxml serialized data.
    //
    // This decision was made to avoid making the UxmlObject Traits/Factory public, just to remove them in a subsequent release.
    // They were kept internal because we knew a new way for uxml support was coming, and now it's there.
    // The only way to allow custom bindings in uxml with traits would be to expose UxmlObjectTraits, resulting in two
    // code paths to maintain. So we decided to only support UxmlObject authoring from uxml serialization data.

    [UxmlObject]
    public partial class DataBinding
    {
        internal const string k_DataSourceTooltip = "A data source is a collection of information. By default, a binding will inherit the existing data source from the hierarchy. " +
            "You can instead define another object here as the data source, or define the type of property it may be if the source is not yet available.";
        internal const string k_DataSourcePathTooltip = "The path to the value in the data source used by this binding. To see resolved bindings in the UI Builder, define a path that is compatible with the target source property.";
        internal const string k_BindingModeTooltip = "Controls how a binding is updated, which can include the direction in which data is written.";
        internal const string k_SourceToUiConvertersTooltip = "Define one or more converter groups for this binding that will be used between the data source to the target UI.";
        internal const string k_UiToSourceConvertersTooltip = "Define one or more converter groups for this binding that will be used between the target UI to the data source.";

        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Binding.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField, HideInInspector, UxmlAttribute("data-source-path")]
            [Tooltip(k_DataSourcePathTooltip)]
            string dataSourcePathString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSourcePathString_UxmlAttributeFlags;

            [SerializeField, HideInInspector, DataSourceDrawer]
            [Tooltip(k_DataSourceTooltip)]
            Object dataSource;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSource_UxmlAttributeFlags;

            [UxmlAttribute("data-source-type")]
            [SerializeField, HideInInspector, UxmlTypeReferenceAttribute(typeof(object))]
            [Tooltip(k_DataSourceTooltip)]
            string dataSourceTypeString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSourceTypeString_UxmlAttributeFlags;

            [SerializeField, HideInInspector, BindingModeDrawer]
            [Tooltip(k_BindingModeTooltip)]
            BindingMode bindingMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingMode_UxmlAttributeFlags;

            [UxmlAttribute("source-to-ui-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = false), UxmlAttributeBindingPath(nameof(uiToSourceConverters))]
            [Tooltip(k_SourceToUiConvertersTooltip)]
            string sourceToUiConvertersString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sourceToUiConvertersString_UxmlAttributeFlags;

            [UxmlAttribute("ui-to-source-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = true), UxmlAttributeBindingPath(nameof(sourceToUiConverters))]
            [Tooltip(k_UiToSourceConvertersTooltip)]
            string uiToSourceConvertersString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags uiToSourceConvertersString_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new DataBinding();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (DataBinding) obj;
                if (ShouldWriteAttributeValue(dataSourcePathString_UxmlAttributeFlags))
                    e.dataSourcePathString = dataSourcePathString;
                if (ShouldWriteAttributeValue(dataSource_UxmlAttributeFlags))
                    e.dataSource = dataSource ? dataSource : null;
                if (ShouldWriteAttributeValue(dataSourceTypeString_UxmlAttributeFlags))
                    e.dataSourceTypeString = dataSourceTypeString;
                if (ShouldWriteAttributeValue(bindingMode_UxmlAttributeFlags))
                    e.bindingMode = bindingMode;
                if (ShouldWriteAttributeValue(uiToSourceConvertersString_UxmlAttributeFlags))
                    e.uiToSourceConvertersString = uiToSourceConvertersString;
                if (ShouldWriteAttributeValue(sourceToUiConvertersString_UxmlAttributeFlags))
                    e.sourceToUiConvertersString = sourceToUiConvertersString;
            }
        }
    }
}
