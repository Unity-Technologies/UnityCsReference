// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Bindings;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class SerializedObjectBindingContext
{
    public ulong lastRevision { get; private set; }
    private SerializedObject serializedObject { get; set; }

    private SerializedObjectChangeTracker changeTracker;
    private bool wasUpdated { get; set; }

    // Used in tests to ensure that we do not call `SerializedObject.UpdateIfRequiredOrScript` more than
    // once per update in normal use-cases.
    internal int updateCount { get; private set; }

    private bool m_DelayBind = false;
    private long m_BindingOperationStartTimeMs;
    private const int k_MaxBindingTimeMs = 50;
    private long m_LastFrame = long.MinValue;

    public SerializedObjectBindingContext(SerializedObject so)
    {
        this.serializedObject = so;
        this.lastRevision = so.objectVersion;
        changeTracker = new SerializedObjectChangeTracker(so);
    }

    public bool TargetsSerializedObject(SerializedObject obj)
    {
        return serializedObject == obj;
    }

    public bool IsInitialized()
    {
        if (null == serializedObject)
            return false;

        return IntPtr.Zero != serializedObject.m_NativeObjectPtr;
    }

    public uint objectVersion => IsInitialized() ? serializedObject.objectVersion : 0;

    public Object GetTargetObject()
    {
        if (IsInitialized())
            return serializedObject.targetObject;
        return null;
    }

    public SerializedProperty FindProperty(string propertyPath)
    {
        return serializedObject?.FindProperty(propertyPath);
    }

    public void Bind(VisualElement element)
    {
        element.SetProperty(FindContextPropertyKey, this);
        ContinueBinding(element, null);
    }

    public bool RequestSerializedObject(out SerializedObject so)
    {
        so = serializedObject;
        return IsInitialized();
    }

    internal void ContinueBinding(VisualElement element, SerializedProperty parentProperty)
    {
        try
        {
            m_BindingOperationStartTimeMs = Panel.TimeSinceStartupMs();
            m_DelayBind = false;
            BindTree(element, parentProperty);
        }
        finally
        {
            m_DelayBind = false;
        }
    }

    private bool ShouldDelayBind()
    {
        if (!m_DelayBind && !VisualTreeBindingsUpdater.disableBindingsThrottling)
        {
            m_DelayBind = (Panel.TimeSinceStartupMs() - m_BindingOperationStartTimeMs) > k_MaxBindingTimeMs;
        }

        return m_DelayBind;
    }

    public void Unbind(VisualElement element)
    {
        var existingContext = element.GetProperty(FindContextPropertyKey);

        if (existingContext != null)
        {
            if (existingContext != this)
            {
                return;
            }
            element.SetProperty(FindContextPropertyKey, null);
        }

        var serializedObjectBinding = element.GetBinding(BindingExtensions.s_SerializedBindingId) as SerializedObjectBindingBase;
        serializedObjectBinding?.Unbind();
        RemoveBinding(element as IBindable, false);

        var childCount = element.hierarchy.childCount;
        for (int i = 0; i < childCount; ++i)
        {
            Unbind(element.hierarchy[i]);
        }
    }

    #region Bind Tree

    private static bool SendBindingEvent<TEventType>(TEventType evt, VisualElement target)
        where TEventType : EventBase<TEventType>, new()
    {
        evt.elementTarget = target;
        EventDispatchUtilities.HandleEventAtTargetAndDefaultPhase(evt, target.elementPanel, target);
        return evt.isPropagationStopped;
    }

    internal void BindTree(VisualElement element, SerializedProperty parentProperty)
    {
        if (element.HasSelfEventInterests(SerializedObjectBindEvent.EventCategory))
        {
            using (var evt = SerializedObjectBindEvent.GetPooled(serializedObject))
            {
                if (SendBindingEvent(evt, element))
                {
                    return;
                }
            }
        }

        if (ShouldDelayBind())
        {
            // too much time spent on binding, we`ll do the rest on the next frame
            var request = DefaultSerializedObjectBindingImplementation.BindingRequest.CreateDelayBinding(this, parentProperty);
            VisualTreeBindingsUpdater.AddBindingRequest(element, request);
            return;
        }

        if (element is IBindable field)
        {
            if (!string.IsNullOrEmpty(field.bindingPath))
            {
                var foundProperty = BindPropertyRelative(field, parentProperty);
                if (foundProperty != null)
                {
                    parentProperty = foundProperty;
                }
            }
        }

        var childCount = element.hierarchy.childCount;
        for (var i = 0; i < childCount; ++i)
        {
            BindTree(element.hierarchy[i], parentProperty);
        }
    }

    private static readonly string k_EnabledOverrideSet = "EnabledSetByBindings";

    static void SyncEditableState(VisualElement fieldElement, bool shouldBeEditable)
    {
        if (fieldElement.enabledSelf != shouldBeEditable)
        {
            if (shouldBeEditable)
            {
                if (fieldElement.GetProperty(k_EnabledOverrideSet) != null)
                {
                    fieldElement.SetEnabled(true);
                }
                else
                {
                    // the field was disabled by user code, we don't want to re-enable it.
                }
            }
            else
            {
                fieldElement.SetProperty(k_EnabledOverrideSet, fieldElement.enabledSelf);
                fieldElement.SetEnabled(false);
            }
        }
    }

    internal SerializedProperty BindPropertyRelative(IBindable field, SerializedProperty parentProperty)
    {
        SerializedProperty property = null;
        var fieldElement = field as VisualElement;

        if (parentProperty != null)
        {
            var unsafeMode = parentProperty.unsafeMode;

            // We switch to unsafe mode because we don't care if the parentProperty is valid or not (using
            // [SerializeReference], you can end up with a property that doesn't exist anymore, which would throw in
            // "safe mode")
            parentProperty.unsafeMode = true;

            // If a PropertyField has a type which contains a child field with the same name we may
            // mistakenly use that instead of the correct SerializedProperty.
            // To avoid this we check if the PropertyField has already assigned a SerializedProperty
            // to the field and use that instead. (UUM-27252)
            if (!EditorGUI.HasVisibleChildFields(parentProperty) &&
                fieldElement?.GetProperty(BaseField<string>.serializedPropertyCopyName) is SerializedProperty fieldProperty)
            {
                property = fieldProperty;
            }
            else
            {
                property = parentProperty.FindPropertyRelative(field.bindingPath);
            }

            parentProperty.unsafeMode = unsafeMode;
        }

        if (property == null)
            property = serializedObject?.FindProperty(field.bindingPath);

        if (property == null || fieldElement == null)
        {
            // Object is null or property was not found, we have nothing to do here
            return property;
        }

        // Set enabled state before sending the event because element like PropertyField may stop the event
        SyncEditableState(fieldElement, property.editable);

        if (fieldElement.HasSelfEventInterests(SerializedPropertyBindEvent.EventCategory))
        {
            using (var evt = SerializedPropertyBindEvent.GetPooled(property))
            {
                if (SendBindingEvent(evt, fieldElement))
                {
                    return property;
                }
            }
        }

        // If we intend on binding, we first remove any binding that were already present
        RemoveBinding(field, true);

        CreateBindingObjectForProperty(fieldElement, property);

        return property;
    }

    private void CreateBindingObjectForProperty(VisualElement element, SerializedProperty prop)
    {
        // A bound Foldout (a PropertyField with child properties) is special.
        if (element is Foldout foldout && prop.hasChildren)
        {
            // We bind to the given propertyPath but we only bind to its 'isExpanded' state, not its value.
            SerializedIsExpandedBinding.CreateBind(foldout, this, prop);
            return;
        }
        else if (element is Label label && label.GetProperty(PropertyField.foldoutTitleBoundLabelProperty) != null)
        {
            // We bind to the given propertyPath but we only bind to its 'localizedDisplayName' state, not its value.
            // This is a feature from IMGUI where the title of a Foldout will change if one of the child
            // properties is named "Name" and its value changes.
            SerializedObjectBinding<string>.CreateBind(
                label, this, prop,
                p => p.localizedDisplayName,
                (p, v) => { },
                SerializedPropertyHelper.ValueEquals<string>);

            return;
        }
        if (element is BaseListView baseListView)
        {
            if (BindListView(baseListView, prop))
            {
                if (baseListView.headerFoldout != null)
                {
                    // The foldout will be bound as hierarchy binding continues.
                    baseListView.headerFoldout.bindingPath = prop.propertyPath;
                }

                return;
            }
        }

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                if (prop.type == "long" || prop.type == "ulong")
                {
                    if (element is INotifyValueChanged<long> || element is INotifyValueChanged<string>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetLongPropertyValue, SerializedPropertyHelper.SetLongPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<ulong>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetULongPropertyValue, SerializedPropertyHelper.SetULongPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<int>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<float>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetLongPropertyValueAsFloat, SerializedPropertyHelper.SetFloatPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                }
                else // prop.type == "int"
                {
                    if (element is INotifyValueChanged<int> || element is INotifyValueChanged<string>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<uint>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetUIntPropertyValue, SerializedPropertyHelper.SetUIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<long>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetLongPropertyValue, SerializedPropertyHelper.SetLongPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                    else if (element is INotifyValueChanged<float>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValueAsFloat, SerializedPropertyHelper.SetFloatPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                }
                break;
            case SerializedPropertyType.Boolean:
                DefaultBind(element, prop, SerializedPropertyHelper.GetBoolPropertyValue, SerializedPropertyHelper.SetBoolPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Float:
                if (prop.type == "float")
                {
                    if (element is INotifyValueChanged<double>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetFloatPropertyValueAsDouble, SerializedPropertyHelper.SetFloatPropertyValueFromDouble, SerializedPropertyHelper.ValueEquals);
                    }
                    else
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetFloatPropertyValue, SerializedPropertyHelper.SetFloatPropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                }
                else // prop.type == "double"
                {
                    if (element is INotifyValueChanged<float>)
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetDoublePropertyValueAsFloat, SerializedPropertyHelper.SetDoublePropertyValueFromFloat, SerializedPropertyHelper.ValueEquals);
                    }
                    else
                    {
                        DefaultBind(element, prop, SerializedPropertyHelper.GetDoublePropertyValue, SerializedPropertyHelper.SetDoublePropertyValue, SerializedPropertyHelper.ValueEquals);
                    }
                }

                break;
            case SerializedPropertyType.String:
                DefaultBind(element, prop, SerializedPropertyHelper.GetStringPropertyValue, SerializedPropertyHelper.SetStringPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Color:
                DefaultBind(element, prop, SerializedPropertyHelper.GetColorPropertyValue, SerializedPropertyHelper.SetColorPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.ObjectReference:
                if (element is ObjectField objectField)
                    SerializedObjectReferenceBinding.CreateBind(objectField, this, prop);
                else
                    DefaultBind(element, prop, SerializedPropertyHelper.GetObjectRefPropertyValue, SerializedPropertyHelper.SetObjectRefPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.LayerMask:
                DefaultBind(element, prop, SerializedPropertyHelper.GetLayerMaskPropertyValue, SerializedPropertyHelper.SetLayerMaskPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.RenderingLayerMask:
                DefaultBind(element, prop, SerializedPropertyHelper.GetRenderingLayerMaskPropertyValue, SerializedPropertyHelper.SetRenderingLayerMaskPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Enum:
                CreateEnumBindingObject(element, prop);
                break;
            case SerializedPropertyType.Vector2:
                DefaultBind(element, prop, SerializedPropertyHelper.GetVector2PropertyValue, SerializedPropertyHelper.SetVector2PropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Vector3:
                DefaultBind(element, prop, SerializedPropertyHelper.GetVector3PropertyValue, SerializedPropertyHelper.SetVector3PropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Vector4:
                DefaultBind(element, prop, SerializedPropertyHelper.GetVector4PropertyValue, SerializedPropertyHelper.SetVector4PropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Rect:
                DefaultBind(element, prop, SerializedPropertyHelper.GetRectPropertyValue, SerializedPropertyHelper.SetRectPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.ArraySize:
                DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.AnimationCurve:
                DefaultBind(element, prop, SerializedPropertyHelper.GetAnimationCurvePropertyValue, SerializedPropertyHelper.SetAnimationCurvePropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Bounds:
                DefaultBind(element, prop, SerializedPropertyHelper.GetBoundsPropertyValue, SerializedPropertyHelper.SetBoundsPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Gradient:
                DefaultBind(element, prop, SerializedPropertyHelper.GetGradientPropertyValue, SerializedPropertyHelper.SetGradientPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Quaternion:
                DefaultBind(element, prop, SerializedPropertyHelper.GetQuaternionPropertyValue, SerializedPropertyHelper.SetQuaternionPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.FixedBufferSize:
                DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Vector2Int:
                DefaultBind(element, prop, SerializedPropertyHelper.GetVector2IntPropertyValue, SerializedPropertyHelper.SetVector2IntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Vector3Int:
                DefaultBind(element, prop, SerializedPropertyHelper.GetVector3IntPropertyValue, SerializedPropertyHelper.SetVector3IntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.RectInt:
                DefaultBind(element, prop, SerializedPropertyHelper.GetRectIntPropertyValue, SerializedPropertyHelper.SetRectIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.BoundsInt:
                DefaultBind(element, prop, SerializedPropertyHelper.GetBoundsIntPropertyValue, SerializedPropertyHelper.SetBoundsIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.Character:
                if (element is INotifyValueChanged<string>)
                {
                    DefaultBind(element, prop, SerializedPropertyHelper.GetCharacterPropertyValueAsString, SerializedPropertyHelper.SetCharacterPropertyValueFromString, SerializedPropertyHelper.ValueEquals);
                }
                else
                {
                    DefaultBind(element, prop, SerializedPropertyHelper.GetCharacterPropertyValue, SerializedPropertyHelper.SetCharacterPropertyValue, SerializedPropertyHelper.ValueEquals);
                }
                break;
            case SerializedPropertyType.Hash128:
                DefaultBind(element, prop, SerializedPropertyHelper.GetHash128PropertyValue, SerializedPropertyHelper.SetHash128PropertyValue, SerializedPropertyHelper.ValueEquals);
                break;
            case SerializedPropertyType.ExposedReference:
                // nothing to bind here
                break;
            case SerializedPropertyType.Generic:
                if (prop.type == nameof(ToggleButtonGroupState))
                {
                    DefaultBind(element, prop, SerializedPropertyHelper.GetToggleStatePropertyValue, SerializedPropertyHelper.SetToggleStatePropertyValue, SerializedPropertyHelper.ValueEquals);
                }

                // nothing to bind here
                break;
            default:
                Debug.LogWarning(string.Format("Binding is not supported for {0} properties \"{1}\"", prop.type,
                    prop.propertyPath));
                break;
        }
    }

    private void DefaultBind<TValue>(VisualElement element, SerializedProperty prop,
        Func<SerializedProperty, TValue> propertyReadFunc, Action<SerializedProperty, TValue> propertyWriteFunc,
        Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> valueComparerFunc)
    {
        var field = element as INotifyValueChanged<TValue>;

        if (element is INotifyValueChanged<string> && typeof(TValue) != typeof(string))
        {
            //One Way Binding here with string conversions

            SerializedObjectStringConversionBinding<TValue>.CreateBind(element as INotifyValueChanged<string>, this, prop, propertyReadFunc,
                propertyWriteFunc, valueComparerFunc);
        }
        else if (field != null)
        {
            SerializedObjectBinding<TValue>.CreateBind(field, this, prop, propertyReadFunc,
                propertyWriteFunc, valueComparerFunc);
        }
        else
        {
            Debug.LogWarning(string.Format("Field type {0} is not compatible with {2} property \"{1}\"",
                element.GetType().FullName, prop.propertyPath, prop.type));
        }
    }

    private void CreateEnumBindingObject(VisualElement element, SerializedProperty prop)
    {
        if (element is PopupField<string>)
        {
            SerializedDefaultEnumBinding.CreateBind((PopupField<string>) element, this, prop);
        }
        else if (element is EnumFlagsField || element is EnumField)
        {
            SerializedManagedEnumBinding.CreateBind((BaseField<Enum>) element, this, prop);
        }
        else if (element is INotifyValueChanged<int>)
        {
            DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
        }
        else
        {
            DefaultBind(element, prop, SerializedPropertyHelper.GetEnumPropertyValueAsString, SerializedPropertyHelper.SetEnumPropertyValueFromString, SerializedPropertyHelper.SlowEnumValueEquals);
        }
    }

    private bool BindListView(BaseListView baseListView, SerializedProperty prop)
    {
        // This should be done elsewhere. That's what the SerializedPropertyBindEvent are for.
        // Problem is, ListView is in the engine assembly and can't have knowledge of SerializedObjects
        if (prop.propertyType == SerializedPropertyType.Generic)
        {
            var sizeProperty = prop.FindPropertyRelative("Array.size");

            if (sizeProperty == null)
            {
                Debug.LogWarning(string.Format("Binding ListView failed: can't find array size for property \"{0}\"",
                    prop.propertyPath));
                return false;
            }

            return BaseListViewSerializedObjectBinding.CreateBind(baseListView, this, prop);
        }

        Debug.LogWarning(string.Format("Binding ListView is not supported for {0} properties \"{1}\"", prop.type,
            prop.propertyPath));

        return false;
    }

    private void RemoveBinding(IBindable bindable, bool forceRemove)
    {
        if (null == bindable)
            return;

        var ve = bindable as VisualElement;

        var bindingBase = (SerializedObjectBindingBase) ve?.GetBinding(BindingExtensions.s_SerializedBindingId);
        if (bindingBase == null)
            return;

        if (bindingBase.isReleased)
            return;

        if (forceRemove || bindingBase.bindingContext == this)
        {
            bindingBase.Unbind();
        }
    }

    private static readonly PropertyName FindContextPropertyKey = "__UnityBindingContext";

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static SerializedObjectBindingContext GetBindingContextFromElement(VisualElement element)
    {
        if (element is IBindable && element.GetBinding(BindingExtensions.s_SerializedBindingId) is SerializedObjectBindingBase bindingBase)
        {
            return bindingBase.bindingContext;
        }

        if (element.GetProperty(FindContextPropertyKey) is SerializedObjectBindingContext context)
        {
            return context;
        }

        return null;
    }

    internal static SerializedObjectBindingContext FindBindingContext(VisualElement element, SerializedObject obj)
    {
        while (element != null)
        {
            var context = GetBindingContextFromElement(element);

            if (context != null && context.serializedObject == obj)
                return context;

            element = element.hierarchy.parent;
        }

        return null;
    }

    #endregion

    #region SerializedObject Version Update

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal void UpdateRevision()
    {
        if (IsValid())
        {
            bool hasChanges = false;

            using (k_ChangeTracking.Auto())
            {
                hasChanges = changeTracker.PollForChanges(false);
            }

            if(hasChanges)
            {
                lastRevision = serializedObject.objectVersion;
                OnSerializedObjectChanged();
            }
        }
    }

    internal bool IsValid()
    {
        if (serializedObject == null || serializedObject.m_NativeObjectPtr == IntPtr.Zero)
            return false;

        return serializedObject.isValid;
    }

    private static readonly ProfilerMarker k_SerializedObjectUpdater = new ProfilerMarker("BindingContext.UpdateSerializedObject");
    private static readonly ProfilerMarker k_ChangeTracking = new ProfilerMarker("BindingContext.ChangeTracking");
    private static readonly ProfilerMarker k_SyncVersion = new ProfilerMarker("BindingContext.SyncChangeVersion");
    private static readonly ProfilerMarker k_NotifyChanges = new ProfilerMarker("BindingContext.NotifyChanges");

    internal void UpdateIfNecessary(VisualElement element)
    {
        if (!wasUpdated && IsValid())
        {
            if (element.elementPanel?.GetUpdater(VisualTreeUpdatePhase.DataBinding) is VisualTreeDataBindingsUpdater updater && m_LastFrame != (updater as IVisualTreeUpdater).FrameCount)
            {
                using (var p = k_SerializedObjectUpdater.Auto())
                {
                    serializedObject.UpdateIfRequiredOrScript();
                }

                ++updateCount;
                UpdateRevision();
                m_LastFrame = (updater as IVisualTreeUpdater).FrameCount;
                wasUpdated = true;
            }
        }
    }

    internal void ResetUpdate()
    {
        if (wasUpdated)
        {
            wasUpdated = false;
            UpdateRevision(); //If somebody called Update() on our serializedObject, we need to revalidate properties

        }
    }

    void OnSerializedObjectChanged()
    {
        UpdateTrackedProperties();
    }


    #endregion


    #region Property Value Tracking
    class TrackedValue
    {
        public Action<object, SerializedProperty> onChangeCallback;
        public Action<object> onUpdateCallback;
        public object cookie;

        public SerializedPropertyType originalPropType;

        public TrackedValue(SerializedProperty property, Action<object, SerializedProperty> changeCB, Action<object> updateCB)
        {
            originalPropType = property.propertyType;
            onChangeCallback = changeCB;
            onUpdateCallback = updateCB;
        }

        public void InvokeUpdateCallback()
        {
            onUpdateCallback?.Invoke(cookie);
        }

        public void InvokeOnChangeCallback(SerializedProperty prop)
        {
            if (prop.propertyType == originalPropType)
            {
                onChangeCallback?.Invoke(cookie, prop);
            }
        }
    }

    class TrackedValues
    {
        // MultiValueDictionary?
        private Dictionary<int, List<TrackedValue>> m_TrackedValues = new Dictionary<int, List<TrackedValue>>();

        public void Add(SerializedProperty prop, object cookie, Action<object, SerializedProperty> onChangeCallback, Action<object> onUpdateCallback)
        {
            var hash = prop.hashCodeForPropertyPath;

            if (!m_TrackedValues.TryGetValue(hash, out var values))
            {
                values = new List<TrackedValue>();
                m_TrackedValues.Add(hash, values);
            }

            var t = new TrackedValue(prop, onChangeCallback, onUpdateCallback);
            t.cookie = cookie;
            values.Add(t);
        }

        public bool Remove(int propertyPathHash, object cookie)
        {
            bool removed = false;
            if (m_TrackedValues.TryGetValue(propertyPathHash, out var values))
            {
                for (int i = values.Count - 1; i >= 0; i--)
                {
                    var t = values[i];

                    if (ReferenceEquals(t.cookie, cookie))
                    {
                        values.RemoveAt(i);
                        removed = true;
                    }
                }

                if (values.Count == 0)
                {
                    m_TrackedValues.Remove(propertyPathHash);
                }
            }

            return removed;
        }

        public void SyncUpdateCallbacks(int hash)
        {
            if (m_TrackedValues.TryGetValue(hash, out var values))
            {
                for (int i = 0; i < values.Count; ++i)
                {
                    values[i].InvokeUpdateCallback();
                }
            }
        }

        public void NotifyValueChanged(SerializedObjectBindingContext context, SerializedProperty currentProperty)
        {
            var hash = currentProperty.hashCodeForPropertyPath;

            if (m_TrackedValues.TryGetValue(hash, out var values))
            {
                for (int i = 0; i < values.Count; ++i)
                {
                    values[i].InvokeOnChangeCallback(currentProperty);
                }
            }
            // else
            // {
            //     Debug.LogWarning("Received PropertyChange for untracked property?");
            // }
        }
    }

    /// <summary>
    /// Map of value trackers per serialized property type. WARNING: tracker may be null for some types.
    /// </summary>
    private TrackedValues m_ValueTracker = new TrackedValues();


    // Ideally we'd also be able to do this with just the property path hash value instead of requiring a resolved SerializedProperty
    public bool RegisterSerializedPropertyChangeCallback(object cookie, SerializedProperty property,
        Action<object, SerializedProperty> valueChangedCallback)
    {
        m_ValueTracker.Add(property, cookie, valueChangedCallback, null);
        changeTracker.AddPropertyTracking(property);
        return true;
    }

    public void UnregisterSerializedPropertyChangeCallback(object cookie, int propertyPathHash)
    {
        m_ValueTracker.Remove(propertyPathHash, cookie);
        changeTracker.RemovePropertyTracking(propertyPathHash);
    }

    public SerializedObjectBindingContextUpdater AddBindingUpdater(VisualElement element)
    {
        var contextUpdater = element.GetBinding(BindingExtensions.s_SerializedBindingContextUpdaterId) as SerializedObjectBindingContextUpdater;
        if (contextUpdater == null)
            return SerializedObjectBindingContextUpdater.Create(element, this);

        var bindingContext = contextUpdater.bindingContext;
        if (bindingContext == null || bindingContext.serializedObject != serializedObject)
            throw new NotSupportedException("An element can track properties on only one serializedObject at a time");

        return contextUpdater;
    }

    #endregion

    private HashSet<SerializedObjectBindingBase> m_RegisteredBindings = new HashSet<SerializedObjectBindingBase>();

    private static void DefaultOnPropertyChange(object cookie, SerializedProperty changedProp)
    {
        if (cookie is SerializedObjectBindingBase binding)
        {
            binding.OnPropertyValueChanged(changedProp);
        }
    }

    private static void OnPropertyUpdate(object cookie)
    {
        if (cookie is SerializedObjectBindingBase binding)
        {
            binding.SyncObjectVersion();
        }
    }
    internal void RegisterBindingObject(SerializedObjectBindingBase b)
    {
        if (!m_RegisteredBindings.Contains(b))
        {
            m_RegisteredBindings.Add(b);

            if (b.ResolveProperty())
            {
                m_ValueTracker.Add(b.boundProperty, b, (o, p) => DefaultOnPropertyChange(o, p), (o) => OnPropertyUpdate(o));
                changeTracker.AddPropertyTracking(b.boundProperty);
            }
        }
    }

    internal void UnregisterBindingObject(SerializedObjectBindingBase b)
    {
        if (b.boundPropertyHash != 0)
        {
            m_ValueTracker.Remove(b.boundPropertyHash, b);
            changeTracker.RemovePropertyTracking(b.boundPropertyHash);
        }

        m_RegisteredBindings.Remove(b);
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static Action PostProcessTrackedPropertyChanges;

    void UpdateTrackedProperties()
    {
        using (k_SyncVersion.Auto())
        {
            var syncPropertyPathHashes = changeTracker.GetSyncedTrackedProperties();

            for(int i = 0; i < syncPropertyPathHashes.Length;++i)
                m_ValueTracker.SyncUpdateCallbacks(syncPropertyPathHashes[i]);
        }

        if (changeTracker.HasModifiedTrackedProperties())
        {
            using (k_NotifyChanges.Auto())
            {
                try
                {
                    var changedProperties = changeTracker.GetModifiedTrackedProperties();

                    for (int i = 0; i < changedProperties.Length; ++i)
                    {
                        m_ValueTracker.NotifyValueChanged(this, changedProperties[i]);
                    }

                    PostProcessTrackedPropertyChanges?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    changeTracker.ClearModifiedTrackedProperties();
                }
            }
        }
    }
}
