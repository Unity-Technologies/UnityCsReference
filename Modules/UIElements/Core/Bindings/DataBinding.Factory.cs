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
            private string dataSourcePathString;

            [SerializeField, HideInInspector, DataSourceDrawer]
            [Tooltip(k_DataSourceTooltip)]
            private Object dataSource;

            [UxmlAttribute("data-source-type")]
            [SerializeField, HideInInspector, DataSourceTypeDrawer(typeof(object))]
            [Tooltip(k_DataSourceTooltip)]
            private string dataSourceTypeString;

            [SerializeField, HideInInspector, BindingModeDrawer]
            [Tooltip(k_BindingModeTooltip)]
            private BindingMode bindingMode;

            [UxmlAttribute("source-to-ui-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = false)]
            [Tooltip(k_SourceToUiConvertersTooltip)]
            private string sourceToUiConvertersString;

            [UxmlAttribute("ui-to-source-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = true)]
            [Tooltip(k_UiToSourceConvertersTooltip)]
            private string uiToSourceConvertersString;
            #pragma warning restore 649

            public override object CreateInstance() => new DataBinding();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);
                var e = (DataBinding) obj;
                e.dataSourcePathString = dataSourcePathString;
                e.dataSource = dataSource ? dataSource : null;
                e.dataSourceTypeString = dataSourceTypeString;
                e.bindingMode = bindingMode;
                e.uiToSourceConvertersString = uiToSourceConvertersString;
                e.sourceToUiConvertersString = sourceToUiConvertersString;
            }
        }
    }
}
