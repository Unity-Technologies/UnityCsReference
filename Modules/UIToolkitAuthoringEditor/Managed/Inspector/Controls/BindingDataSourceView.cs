// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// View that displays the binding data source information of a VisualElement.
/// </summary>
[UxmlElement]
internal partial class BindingDataSourceView : BindableElement
{
    /// <summary>
    /// The UXML serialized data for the BindingDataSourceView.
    /// </summary>
    [Serializable]
    public new class UxmlSerializedData : BindableElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);
        }

        public override object CreateInstance()
        {
            return new BindingDataSourceView();
        }
    }

    public const string UssClassName = "unity-binding-data-source-view";
    public const string TabButtonUssClassName = "unity-binding-data-source-tab-button";
    const string k_DataSourceObjectTabStatusIndicatorName = "DataSourceAsObjTabStatusIndicator";
    const string k_DataSourceTypeTabStatusIndicatorName = "DataSourceAsTypeTabStatusIndicator";
    static readonly string k_DataSourceLabel = L10n.Tr("Data Source");

    internal const string k_DataSourceUnityObjectPropertyId = nameof(dataSourceUnityObject);
    internal const string k_DataSourceTypePropertyId = nameof(dataSourceTypeString);
    internal const string k_DataSourcePathPropertyId = nameof(dataSourcePathString);

    const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/BindingDataSourceView.uxml";

    internal const string k_BindingWindowNotResolvedPathErrorMessage = "Path cannot be resolved";

    internal struct TestAccess
    {
        public ToggleButtonGroup dataSourceButtonStrip;
        public UxmlAttributeField dataSourceObjectField;
        public UxmlAttributeField dataSourceTypeField;
        public UxmlAttributeField dataSourcePathField;
        public AnyObjectField dataSourceObjectField_VisualInput;
        public UxmlTypeReferenceField dataSourceTypeField_VisualInput;
        public TextField dataSourcePathField_VisualInput;
        public HelpBox pathWarningBox;
        public VisualElement dataSourceObjectTabStatusIndicator;
        public VisualElement dataSourceTypeTabStatusIndicator;
    }

    // Note: They are internal only to be accessible in tests
    internal TestAccess testAccess => new()
    {
        dataSourceButtonStrip = m_DataSourceButtonStrip,
        dataSourceObjectField = m_DataSourceObjectField,
        dataSourceTypeField = m_DataSourceTypeField,
        dataSourcePathField = m_DataSourcePathField,
        dataSourceObjectField_VisualInput = m_DataSourceObjectField.Q<AnyObjectField>(),
        dataSourceTypeField_VisualInput = m_DataSourceTypeField.Q<UxmlTypeReferenceField>(),
        dataSourcePathField_VisualInput = m_DataSourcePathField.Q<TextField>(),
        pathWarningBox = m_PathWarningBox,
        dataSourceObjectTabStatusIndicator = m_DataSourceObjectTabStatusIndicator,
        dataSourceTypeTabStatusIndicator = m_DataSourceTypeTabStatusIndicator,
    };

    ToggleButtonGroup m_DataSourceButtonStrip;
    VisualElement m_DataSourceObjectTabStatusIndicator;
    VisualElement m_DataSourceTypeTabStatusIndicator;
    UxmlAttributeField m_DataSourceObjectField;
    UxmlAttributeField m_DataSourceTypeField;
    UxmlAttributeField m_DataSourcePathField;
    HelpBox m_PathWarningBox;

    IVisualElementScheduledItem m_UpdateControlsScheduledItem;

    bool m_UsesDataSourceObject;

    bool usesDataSourceObject
    {
        get => m_UsesDataSourceObject;
        set
        {
            if (m_UsesDataSourceObject == value)
                return;

            m_UsesDataSourceObject = value;
            if (m_DataSourceButtonStrip != null)
            {
                m_DataSourceButtonStrip.value = value
                    ? new ToggleButtonGroupState(0b01, 2)
                    : new ToggleButtonGroupState(0b10, 2);
                UpdateSourceVisibility();
            }
        }
    }

    public BindingDataSourceView()
    {
        AddToClassList(UssClassName);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_DataSourceButtonStrip = this.Q<ToggleButtonGroup>();
        m_DataSourceButtonStrip.AddToClassList(ToggleButtonGroup.alignedFieldUssClassName);
        m_DataSourceObjectTabStatusIndicator = m_DataSourceButtonStrip.Q<VisualElement>(k_DataSourceObjectTabStatusIndicatorName);
        m_DataSourceTypeTabStatusIndicator = m_DataSourceButtonStrip.Q<VisualElement>(k_DataSourceTypeTabStatusIndicatorName);

        // Allows buttons to be clicked even when the view is disabled
        foreach (var button in m_DataSourceButtonStrip.Query<Button>().ToList())
        {
            button.clickable.acceptClicksIfDisabled = true;
        }

        m_DataSourceButtonStrip.RegisterValueChangedCallback(evt =>
        {
            usesDataSourceObject = evt.newValue[0];
            DelayedUpdateControls();
        });

        // Data Source Object
        m_DataSourceObjectField = this.Q<UxmlAttributeField>("DataSourceObjectField");
        m_DataSourceObjectField.decorator.customFieldAffordanceDataUpdate = UpdateDataSourceFieldAffordanceData;
        m_DataSourceObjectField.Q<PropertyField>().reset += () =>
        {
            var field = m_DataSourceObjectField.Q<AnyObjectField>();

            field.label = k_DataSourceLabel;
            field?.RegisterValueChangedCallback(OnFieldValueChanged);
        };

        // Data Source Type
        m_DataSourceTypeField = this.Q<UxmlAttributeField>("DataSourceTypeField");
        m_DataSourceTypeField.decorator.customFieldAffordanceDataUpdate = UpdateDataSourceFieldAffordanceData;
        m_DataSourceTypeField.Q<PropertyField>().reset += () =>
        {
            var field = m_DataSourceTypeField.Q<UxmlTypeReferenceField>();

            field.label = k_DataSourceLabel;
            field?.RegisterValueChangedCallback(OnFieldValueChanged);
        };

        // Data Source Path
        m_DataSourcePathField = this.Q<UxmlAttributeField>("DataSourcePathField");
        m_DataSourcePathField.Q<PropertyField>().reset += () =>
        {
            var field = m_DataSourcePathField.Q<TextField>();

            field?.RegisterValueChangedCallback(OnFieldValueChanged);
        };

        m_PathWarningBox = this.Q<HelpBox>("PathWarningBox");
        UpdateSourceVisibility();
        usesDataSourceObject = true;

        m_DataSourceObjectField.Q<PropertyField>()
            ?.RegisterCallback<SerializedPropertyBindEvent>(OnDataSourceObjectFieldBound);
        m_DataSourceTypeField.Q<PropertyField>()
            ?.RegisterCallback<SerializedPropertyBindEvent>(OnDataSourceTypeFieldBound);
    }

    void OnFieldValueChanged(EventBase evt)
    {
        DelayedUpdateControls();
    }

    void UpdateDataSourceFieldAffordanceData(UxmlAttributeFieldDecorator decorator,
        in FieldAffordanceData affordanceData, VisualElement element, Binding binding, bool isInline)
    {
        // Do nothing if the property is overridden
        if (isInline || binding != null)
            return;

        // If we have an inherited data source or data source type, update the affordance to show it
        if (decorator == m_DataSourceObjectField.decorator && GetInheritedDataSourceObject() != null)
        {
            affordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Inherited;
        }
        else if (decorator == m_DataSourceTypeField.decorator && GetInheritedDataSourceType() != null)
        {
            affordanceData.sourceTypeInfo = FieldAffordanceSourceInfoType.Inherited;
        }

        DelayedUpdateControls();
    }

    void UpdateTabsIndicators()
    {
        bool showDataSourceObjectIndicator = m_DataSourceButtonStrip.value != new ToggleButtonGroupState(0b01, 2) && m_DataSourceObjectField.decorator.affordanceElement.fieldAffordanceData.sourceTypeInfo != FieldAffordanceSourceInfoType.Default;
        bool showDataSourceTypeIndicator =  m_DataSourceButtonStrip.value == new ToggleButtonGroupState(0b01, 2)  && m_DataSourceTypeField.decorator.affordanceElement.fieldAffordanceData.sourceTypeInfo != FieldAffordanceSourceInfoType.Default;

        m_DataSourceObjectTabStatusIndicator.style.visibility = showDataSourceObjectIndicator ? Visibility.Visible : Visibility.Hidden ;
        m_DataSourceTypeTabStatusIndicator.style.visibility = showDataSourceTypeIndicator? Visibility.Visible  : Visibility.Hidden ;
    }

    /// <summary>
    /// Gets the value of data source object field.
    /// </summary>
    public Object GetDataSourceObjectValue() => m_DataSourceObjectField?.Q<AnyObjectField>()?.value;

    /// <summary>
    /// Gets the value of the data source type field.
    /// </summary>
    public Type GetDataSourceTypeValue()
    {
        var fullTypeName = m_DataSourceTypeField.Q<UxmlTypeReferenceField>()?.value;

        if (!string.IsNullOrEmpty(fullTypeName))
            return Type.GetType(fullTypeName);
        return null;
    }

    /// <summary>
    /// Gets The value of the data source path field.
    /// </summary>
    public string GetDataSourcePathValue() => m_DataSourcePathField?.Q<TextField>()?.text;

    /// <summary>
    /// Gets data source inherited from the selected VisualElement.
    /// </summary>
    public object GetInheritedDataSourceObject()
    {
        var startingElement = m_DataSourceObjectField.decorator.context?.element?.parent;
        if (startingElement == null)
            return null;

        DataBindingUtility.TryGetRelativeDataSourceFromHierarchy(startingElement, out var source);
        return source;
    }

    /// <summary>
    /// Gets data source type inherited from the selected VisualElement.
    /// </summary>
    public Type GetInheritedDataSourceType()
    {
        var startingElement = m_DataSourceTypeField.decorator.context?.element?.parent;
        if (startingElement == null)
            return null;

        DataBindingUtility.TryGetRelativeDataSourceTypeFromHierarchy(startingElement, out var sourceType);
        return sourceType;
    }

    void OnDataSourceObjectFieldBound(SerializedPropertyBindEvent evt)
    {
        schedule.Execute(() =>
        {
            if (panel == null)
                return;
            UpdateDataSourceObjectField();
        });
    }

    void UpdateDataSourceObjectField()
    {
        var field = m_DataSourceObjectField.Q<AnyObjectField>();

        if (field == null)
            return;

        // Get the actual value of the data source object serialized property, which may be
        // different from the field value if it is inherited.
        var dataSourceObj = m_DataSourceObjectField.boundProperty?.boxedValue;

        // If there is no data source object set, try to get an inherited one
        if (dataSourceObj == null)
        {
            field.SetObjectWithoutNotify(GetInheritedDataSourceObject());
        }

        DelayedUpdateControls();
    }

    void OnDataSourceTypeFieldBound(SerializedPropertyBindEvent evt)
    {
        schedule.Execute(() =>
        {
            if (panel == null)
                return;
            UpdateDataSourceTypeField();
        });
    }

    void UpdateDataSourceTypeField()
    {
        var field = m_DataSourceTypeField.Q<BaseField<string>>();

        if (field == null)
            return;

        // Get the actual value of the data source type serialized property, which may be
        // different from the field value if it is inherited
        var sourceTypeValue = m_DataSourceTypeField.boundProperty?.stringValue;

        if (string.IsNullOrEmpty(sourceTypeValue))
        {
            var type = GetInheritedDataSourceType();
            if (type != null)
            {
                var fullNameWithAssembly = $"{type.FullName}, {type.Assembly.GetName().Name}";

                field.SetValueWithoutNotify(fullNameWithAssembly);
            }
        }
    }

    void DelayedUpdateControls()
    {
        if (m_UpdateControlsScheduledItem == null)
        {
            m_UpdateControlsScheduledItem = schedule.Execute(UpdateControls);
        }
        else
        {
            m_UpdateControlsScheduledItem.Pause();
            m_UpdateControlsScheduledItem.Resume();
        }
    }

    /// <summary>
    ///  Updates the state of controls.
    /// </summary>
    void UpdateControls()
    {
        UpdateCompleter();
        UpdateWarningBox();
        UpdateTabsIndicators();
    }

    void UpdateCompleter()
    {
        // TODO: WIll udpdate the conpleter of the path field when implemented
    }

    void UpdateWarningBox()
    {
        if (m_PathWarningBox == null)
            return;

        string pathWarningMessage = null;
        var sourceObject = GetDataSourceObjectValue();
        var sourceType = GetDataSourceTypeValue();
        var sourcePath = GetDataSourcePathValue();

        if (sourceObject != null)
        {
            object sourceAnyObj = sourceObject;

            if (sourceAnyObj is AnyObjectField.NonUnityObjectValue value)
                sourceAnyObj = value.data;

            if (!string.IsNullOrEmpty(sourcePath) &&
                DataBindingUtility.IsPathValid(sourceAnyObj, sourcePath).returnCode != VisitReturnCode.Ok)
            {
                pathWarningMessage = k_BindingWindowNotResolvedPathErrorMessage;
            }
        }
        else if (sourceType != null)
        {
            if (!string.IsNullOrEmpty(sourcePath))
            {
                if (DataBindingUtility.IsPathValid(sourceType, sourcePath).returnCode != VisitReturnCode.Ok)
                {
                    pathWarningMessage = k_BindingWindowNotResolvedPathErrorMessage;
                }
            }
        }

        if (!string.IsNullOrEmpty(pathWarningMessage))
        {
            m_PathWarningBox.text = pathWarningMessage;
            m_PathWarningBox.style.display = DisplayStyle.Flex;
        }
        else
        {
            m_PathWarningBox.style.display = DisplayStyle.None;
        }
    }

    void UpdateSourceVisibility()
    {
        m_DataSourceObjectField.style.display = usesDataSourceObject ? DisplayStyle.Flex : DisplayStyle.None;
        m_DataSourceTypeField.style.display = usesDataSourceObject ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
