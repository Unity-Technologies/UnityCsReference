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
        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Binding.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField, HideInInspector, UxmlAttribute("data-source-path")]
            private string dataSourcePathString;

            [SerializeField, HideInInspector, DataSourceDrawer]
            private Object dataSource;

            [UxmlAttribute("data-source-type")] 
            [SerializeField, HideInInspector, DataSourceTypeDrawer(typeof(object))]
            private string dataSourceTypeString;

            [SerializeField, HideInInspector, BindingModeDrawer]
            private BindingMode bindingMode;

            [UxmlAttribute("source-to-ui-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = false)]
            private string sourceToUiConvertersString;

            [UxmlAttribute("ui-to-source-converters")]
            [SerializeField, HideInInspector, ConverterDrawer(isConverterToSource = true)]
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
