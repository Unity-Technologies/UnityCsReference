// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Bindings
{
    internal class SerializedObjectBindingContext
    {
        public UInt64 lastRevision {get; private set; }
        public SerializedObject serializedObject {get; private set; }

        private bool wasUpdated {get; set; }

        private bool m_DelayBind = false;
        private long m_BindingOperationStartTimeMs;
        private const int k_MaxBindingTimeMs = 50;

        public SerializedObjectBindingContext(SerializedObject so)
        {
            this.serializedObject = so;
            this.lastRevision = so.objectVersion;
        }

        public void Bind(VisualElement element)
        {
            element.SetProperty(FindContextPropertyKey, this);
            ContinueBinding(element, null);
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
            evt.target = target;
            target.HandleEventAtTargetAndDefaultPhase(evt);
            return evt.isPropagationStopped;
        }

        internal void BindTree(VisualElement element, SerializedProperty parentProperty)
        {
            if (element.HasEventCallbacksOrDefaultActions(SerializedObjectBindEvent.EventCategory))
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
            var unsafeMode = parentProperty?.unsafeMode ?? false;

            // We switch to unsafe mode because we don't care if the parentProperty is valid or not (using
            // [SerializeReference], you can end up with a property that doesn't exist anymore, which would throw in
            // "safe mode")
            if (null != parentProperty)
                parentProperty.unsafeMode = true;

            var property = parentProperty?.FindPropertyRelative(field.bindingPath);

            if (null != parentProperty)
                parentProperty.unsafeMode = unsafeMode;

            if (property == null)
                property = serializedObject?.FindProperty(field.bindingPath);

            if (property == null || field is not VisualElement fieldElement)
            {
                // Object is null or property was not found, we have nothing to do here
                return property;
            }

            // Set enabled state before sending the event because element like PropertyField may stop the event
            SyncEditableState(fieldElement, property.editable);

            if (fieldElement.HasEventCallbacksOrDefaultActions(SerializedPropertyBindEvent.EventCategory))
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
            if (element is Foldout foldout)
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
                    (p, v) => {},
                    SerializedPropertyHelper.ValueEquals<string>);

                return;
            }
            if (element is ListView listView)
            {
                BindListView(listView, prop);

                if (listView.headerFoldout != null)
                {
                    // The foldout will be bound as hierarchy binding continues.
                    listView.headerFoldout.bindingPath = prop.propertyPath;
                }

                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (prop.type == "long" || prop.type == "ulong")
                    {
                        if (element is INotifyValueChanged<long> || element is  INotifyValueChanged<string>)
                        {
                            DefaultBind(element,  prop, SerializedPropertyHelper.GetLongPropertyValue, SerializedPropertyHelper.SetLongPropertyValue, SerializedPropertyHelper.ValueEquals);
                        }
                        else if (element is INotifyValueChanged<ulong>)
                        {
                            DefaultBind(element,  prop, SerializedPropertyHelper.GetULongPropertyValue, SerializedPropertyHelper.SetULongPropertyValue, SerializedPropertyHelper.ValueEquals);
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
                        if (element is INotifyValueChanged<int> || element is  INotifyValueChanged<string>)
                        {
                            DefaultBind(element, prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                        }
                        else if (element is INotifyValueChanged<uint>)
                        {
                            DefaultBind(element, prop, SerializedPropertyHelper.GetUIntPropertyValue, SerializedPropertyHelper.SetUIntPropertyValue, SerializedPropertyHelper.ValueEquals);
                        }
                        else if (element is INotifyValueChanged<long>)
                        {
                            DefaultBind(element,  prop, SerializedPropertyHelper.GetLongPropertyValue, SerializedPropertyHelper.SetLongPropertyValue, SerializedPropertyHelper.ValueEquals);
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
                    else  // prop.type == "double"
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
                    DefaultBind(element, prop, SerializedPropertyHelper.GetObjectRefPropertyValue, SerializedPropertyHelper.SetObjectRefPropertyValue, SerializedPropertyHelper.ValueEquals);
                    break;
                case SerializedPropertyType.LayerMask:
                    DefaultBind(element, prop, SerializedPropertyHelper.GetLayerMaskPropertyValue, SerializedPropertyHelper.SetLayerMaskPropertyValue, SerializedPropertyHelper.ValueEquals);
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
                case SerializedPropertyType.Generic:
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
                SerializedDefaultEnumBinding.CreateBind((PopupField<string>)element, this, prop);
            }
            else if (element is EnumFlagsField || element is EnumField)
            {
                SerializedManagedEnumBinding.CreateBind((BaseField<Enum>)element, this, prop);
            }
            else if (element is INotifyValueChanged<int>)
            {
                DefaultBind(element,  prop, SerializedPropertyHelper.GetIntPropertyValue, SerializedPropertyHelper.SetIntPropertyValue, SerializedPropertyHelper.ValueEquals);
            }
            else
            {
                DefaultBind(element,  prop, SerializedPropertyHelper.GetEnumPropertyValueAsString, SerializedPropertyHelper.SetEnumPropertyValueFromString, SerializedPropertyHelper.SlowEnumValueEquals);
            }
        }

        private bool BindListView(ListView listView, SerializedProperty prop)
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
                ListViewSerializedObjectBinding.CreateBind(listView, this, prop);

                return true;
            }
            else
            {
                Debug.LogWarning(string.Format("Binding ListView is not supported for {0} properties \"{1}\"", prop.type,
                    prop.propertyPath));
            }
            return false;
        }

        private void RemoveBinding(IBindable bindable, bool forceRemove)
        {
            if (bindable == null || !bindable.IsBound())
            {
                return;
            }

            if (bindable.binding is SerializedObjectBindingBase bindingBase)
            {
                if (forceRemove || bindingBase.bindingContext == this)
                {
                    bindable.binding?.Release();
                    bindable.binding = null;
                }
            }
        }

        private static readonly PropertyName FindContextPropertyKey = "__UnityBindingContext";

        internal static SerializedObjectBindingContext GetBindingContextFromElement(VisualElement element)
        {
            if (element is IBindable bindable)
            {
                if (bindable.binding is SerializedObjectBindingBase bindingBase)
                {
                    return bindingBase.bindingContext;
                }
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

        internal void UpdateRevision()
        {
            var previousRevision = lastRevision;
            if (IsValid())
            {
                lastRevision = serializedObject.objectVersion;

                if (previousRevision != lastRevision)
                {
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

        internal void UpdateIfNecessary()
        {
            if (!wasUpdated)
            {
                if (IsValid())
                {
                    serializedObject.UpdateIfRequiredOrScript();

                    UpdateRevision();
                }

                wasUpdated = true;
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
            public uint contentHash;
            public Action<object, SerializedProperty> onChangeCallback;
            public Action<object, SerializedProperty> onUpdateCallback;
            public object cookie;

            public SerializedPropertyType originalPropType;
            public int propertyHash;

            public TrackedValue(SerializedProperty property, Action<object, SerializedProperty> changeCB, Action<object, SerializedProperty> updateCB)
            {
                contentHash = property.contentHash;
                originalPropType = property.propertyType;
                propertyHash = property.hashCodeForPropertyPath;
                onChangeCallback = changeCB;
                onUpdateCallback = updateCB;
            }

            public bool Update(SerializedObjectBindingContext context, SerializedProperty  currentProp)
            {
                if (currentProp.propertyType != originalPropType)
                {
                    return false;
                }

                onUpdateCallback?.Invoke(cookie, currentProp);

                var newContentHash = currentProp.contentHash;

                if (contentHash != newContentHash)
                {
                    contentHash = newContentHash;
                    onChangeCallback(cookie, currentProp);
                }

                return true;
            }
        }

        class TrackedValues
        {
            // MultiValueDictionary?
            private Dictionary<int, List<TrackedValue> > m_TrackedValues = new Dictionary<int, List<TrackedValue>>();

            public TrackedValues()
            {
            }

            public void Add(SerializedProperty prop, object cookie, Action<object, SerializedProperty> onChangeCallback, Action<object, SerializedProperty> onUpdateCallback)
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

            public void Remove(SerializedProperty prop, object cookie)
            {
                var hash = prop.hashCodeForPropertyPath;

                Remove(hash, cookie);
            }

            public void Remove(int propertyPathHash, object cookie)
            {
                if (m_TrackedValues.TryGetValue(propertyPathHash, out var values))
                {
                    for (int i = values.Count - 1; i >= 0; i--)
                    {
                        var t = values[i];

                        if (ReferenceEquals(t.cookie, cookie))
                        {
                            values.RemoveAt(i);
                        }
                    }

                    if (values.Count == 0)
                    {
                        m_TrackedValues.Remove(propertyPathHash);
                    }
                }
            }

            public void Update(SerializedObjectBindingContext context, SerializedProperty currentProperty)
            {
                var hash = currentProperty.hashCodeForPropertyPath;

                if (m_TrackedValues.TryGetValue(hash, out var values))
                {
                    for (int i = 0; i < values.Count; ++i)
                    {
                        values[i].Update(context, currentProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Map of value trackers per serialized property type. WARNING: tracker may be null for some types.
        /// Check <see cref="GetOrCreateTrackedValues"/> for reference.
        /// </summary>
        private TrackedValues m_ValueTracker = new TrackedValues();

        public bool RegisterSerializedPropertyChangeCallback(object cookie, SerializedProperty property,
            Action<object, SerializedProperty> valueChangedCallback)
        {
            m_ValueTracker.Add(property, cookie, valueChangedCallback, null);
            return true;
        }

        public void UnregisterSerializedPropertyChangeCallback(object cookie, SerializedProperty property)
        {
            m_ValueTracker.Remove(property, cookie);
        }

        public SerializedObjectBindingContextUpdater AddBindingUpdater(VisualElement element)
        {
            var b = VisualTreeBindingsUpdater.GetAdditionalBinding(element);

            var contextUpdater = b as SerializedObjectBindingContextUpdater;

            if (b == null)
            {
                contextUpdater = SerializedObjectBindingContextUpdater.Create(element, this);
                VisualTreeBindingsUpdater.SetAdditionalBinding(element, contextUpdater);
            }
            else
            {
                if (contextUpdater == null || contextUpdater.bindingContext != this)
                {
                    throw new NotSupportedException("An element can track properties on only one serializedObject at a time");
                }
            }

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

        private static void OnPropertyUpdate(object cookie, SerializedProperty changedProp)
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
                    m_ValueTracker.Add(b.boundProperty, b, (o, p) => DefaultOnPropertyChange(o,p), (o, p) => OnPropertyUpdate(o,p));
                }
            }
        }

        internal void UnregisterBindingObject(SerializedObjectBindingBase b)
        {
            if (b.boundPropertyHash != 0)
            {
                m_ValueTracker.Remove(b.boundPropertyHash, b);
            }

            m_RegisteredBindings.Remove(b);
        }

        HashSet<long> visited = new HashSet<long>();
        void UpdateTrackedProperties()
        {
            // Iterating over the entire object, as gathering valid property names hashes is faster than querying
            // each saved SerializedProperty.isValid
            var iterator = serializedObject.GetIterator();
            iterator.unsafeMode = true;

            visited.Clear();
            {
                bool visitChild = true;
                while (iterator.Next(visitChild))
                {
                    visitChild = true;

                    switch (iterator.propertyType)
                    {
                        case SerializedPropertyType.ManagedReference:
                        {
                            // Managed reference objects can form a cyclical graph, so need to track visited objects
                            long refId = iterator.managedReferenceId;

                            if (!visited.Add(refId))
                            {
                                visitChild = false;
                            }

                            break;
                        }
                        case SerializedPropertyType.String:
                            visitChild = false;
                            break;
                        default:
                            break;
                    }

                    m_ValueTracker.Update(this, iterator);
                }
            }

            visited.Clear();
        }
    }


    internal class DefaultSerializedObjectBindingImplementation : ISerializedObjectBindingImplementation
    {
        public void Bind(VisualElement element, SerializedObject obj)
        {
            element.SetProperty(BindingExtensions.s_DataSourceProperty, obj);
            if (element.panel != null || element is EditorElement || element is InspectorElement)
            {
                var context = FindOrCreateBindingContext(element, obj);
                context.Bind(element);
                return;
            }

            //wait later
            CreateBindingRequest(element, obj);
        }

        public void Unbind(VisualElement element)
        {
            element?.SetProperty(BindingExtensions.s_DataSourceProperty, null);
            UnbindTree(element);
        }

        private void UnbindTree(VisualElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var context = SerializedObjectBindingContext.GetBindingContextFromElement(element);
            RemoveBindingRequest(element);
            VisualTreeBindingsUpdater.ClearAdditionalBinding(element);

            if (context != null)
            {
                context.Unbind(element);
            }
            else
            {
                var childCount = element.hierarchy.childCount;

                for (int i = 0; i < childCount; ++i)
                {
                    UnbindTree(element.hierarchy[i]);
                }
            }
        }

        static Dictionary<object, object> GetTemporaryCache(VisualElement element)
        {
            var updater = element.elementPanel?.GetUpdater(VisualTreeUpdatePhase.Bindings) as VisualTreeBindingsUpdater;
            return updater?.temporaryObjectCache;
        }

        static SerializedObjectBindingContext FindOrCreateBindingContext(VisualElement element, SerializedObject obj)
        {
            var context = SerializedObjectBindingContext.FindBindingContext(element, obj);

            if (context == null || context.serializedObject != obj)
            {
                //we couldn't find the context, let's try to find it elsewhere
                var cookie = GetTemporaryCache(element);

                if (cookie != null && obj != null)
                {
                    if (cookie.TryGetValue(obj, out var c))
                    {
                        context = c as SerializedObjectBindingContext;
                    }
                }

                if (context == null)
                {
                    context = new SerializedObjectBindingContext(obj);

                    if (obj != null)
                    {
                        cookie?.Add(obj, context);
                    }
                }
            }

            return context;
        }

        public SerializedProperty BindProperty(IBindable field, SerializedObject obj)
        {
            var property = obj?.FindProperty(field.bindingPath);

            if (property != null)
            {
                var element = field as VisualElement;

                if (element != null)
                {
                    if (element.panel == null)
                    {
                        // wait until element is attached
                        CreateBindingRequest(element, property.serializedObject);
                    }
                    else
                    {
                        var context = FindOrCreateBindingContext(element, obj);
                        context.Bind(element);
                    }
                }
            }

            return property;
        }

        public void BindProperty(IBindable field, SerializedProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            field.bindingPath = property.propertyPath;

            var element = field as VisualElement;

            if (element != null)
            {
                if (element.panel == null)
                {
                    // wait until element is attached
                    CreateBindingRequest(element, property.serializedObject);
                }
                else
                {
                    var context = FindOrCreateBindingContext(element, property.serializedObject);
                    context.Bind(element);
                }
            }
        }

        public void TrackPropertyValue(VisualElement element, SerializedProperty property, Action<SerializedProperty> callback)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (element != null)
            {
                var request = BindingRequest.Create(BindingRequest.RequestType.TrackProperty, property.serializedObject);
                request.parentProperty = property.Copy();
                request.obj = property.serializedObject;

                if (callback != null)
                {
                    request.callback = (e, p) => callback(p);
                }
                else
                {
                    request.callback = SendPropertyChangeCallback;
                }

                if (element.panel == null)
                {
                    // wait until element is attached
                    VisualTreeBindingsUpdater.AddBindingRequest(element, request);
                }
                else
                {
                    request.Bind(element);
                    request.Release();
                }
            }
        }

        private static void SendPropertyChangeCallback(object cookie, SerializedProperty prop)
        {
            if (cookie is VisualElement element)
            {
                using (SerializedPropertyChangeEvent evt = SerializedPropertyChangeEvent.GetPooled())
                {
                    evt.changedProperty = prop;
                    evt.target = element;
                    element.SendEvent(evt);
                }
            }
        }

        private static void SendObjectChangeCallback(object cookie, SerializedObject obj)
        {
            if (cookie is VisualElement element)
            {
                using (SerializedObjectChangeEvent evt = SerializedObjectChangeEvent.GetPooled(obj))
                {
                    evt.target = element;
                    element.SendEvent(evt);
                }
            }
        }

        public void TrackSerializedObjectValue(VisualElement element, SerializedObject serializedObject, Action<SerializedObject> callback)
        {
            if (serializedObject == null)
            {
                throw new ArgumentNullException(nameof(serializedObject));
            }

            if (element != null)
            {
                var request = BindingRequest.Create(BindingRequest.RequestType.TrackObject, serializedObject);

                request.obj = serializedObject;

                if (callback != null)
                {
                    request.objectChangedCallback = (e, o) => callback(o);
                }
                else
                {
                    request.objectChangedCallback = (o, obj) => SendObjectChangeCallback(o, obj);
                }

                if (element.panel == null)
                {
                    // wait until element is attached
                    VisualTreeBindingsUpdater.AddBindingRequest(element, request);
                }
                else
                {
                    request.Bind(element);
                    request.Release();
                }
            }
        }

        // visual element style changes wrt its property state

        void ISerializedObjectBindingImplementation.Bind(VisualElement element, object objWrapper, SerializedProperty parentProperty)
        {
            var context = objWrapper as SerializedObjectBindingContext;

            if (context == null)
            {
                if (element.panel == null)
                {
                    CreateBindingRequest(element, parentProperty.serializedObject);
                    return;
                }
                context = FindOrCreateBindingContext(element, parentProperty.serializedObject);
            }

            context.BindTree(element, parentProperty);
        }

        private static void CreateBindingRequest(VisualElement element, SerializedObject obj)
        {
            var request = BindingRequest.Create(BindingRequest.RequestType.Bind, obj);
            VisualTreeBindingsUpdater.AddBindingRequest(element, request);
        }

        private static void RemoveBindingRequest(VisualElement element)
        {
            VisualTreeBindingsUpdater.ClearBindingRequests(element);
        }

        public void HandleStyleUpdate(VisualElement element)
        {
            var bindable = element as IBindable;
            var binding = bindable?.binding as SerializedObjectBindingBase;
            if (binding?.boundProperty == null)
                return;

            BindingsStyleHelpers.UpdateElementStyle(element, binding.boundProperty);
        }

        internal class BindingRequest : IBindingRequest
        {
            public static ObjectPool<BindingRequest> s_Pool =
                new ObjectPool<BindingRequest>(() => new BindingRequest(), 32);

            public enum RequestType
            {
                Bind,
                DelayBind,
                TrackProperty,
                TrackObject
            }

            public SerializedObject obj;
            public SerializedObjectBindingContext context;
            public SerializedProperty parentProperty;
            public Action<object, SerializedProperty> callback;
            public Action<object, SerializedObject> objectChangedCallback;
            public RequestType requestType;

            public static BindingRequest Create(RequestType reqType, SerializedObject obj)
            {
                var req = s_Pool.Get();
                req.requestType = reqType;
                req.obj = obj;
                return req;
            }

            public static BindingRequest CreateDelayBinding(SerializedObjectBindingContext context, SerializedProperty parentProperty)
            {
                var req = s_Pool.Get();
                req.requestType = RequestType.DelayBind;
                req.context = context;
                req.parentProperty = parentProperty;
                return req;
            }

            public void Bind(VisualElement element)
            {
                context ??= FindOrCreateBindingContext(element, obj);

                switch (requestType)
                {
                    case RequestType.Bind:
                        context.Bind(element);
                        break;
                    case RequestType.DelayBind:
                        if (context.IsValid())  // Sometimes our serializedObject might have vanished, after a domain reload
                        {
                            context.ContinueBinding(element, parentProperty);
                        }
                        break;
                    case RequestType.TrackProperty:
                    {
                        var contextUpdater = context.AddBindingUpdater(element);
                        contextUpdater.AddTracking(parentProperty);
                        context.RegisterSerializedPropertyChangeCallback(element, parentProperty,
                            callback as Action<object, SerializedProperty>);
                    }
                    break;
                    case RequestType.TrackObject:
                    {
                        var contextUpdater = context.AddBindingUpdater(element);
                        contextUpdater.registeredCallbacks += objectChangedCallback;
                    }
                    break;
                    default:
                        break;
                }
            }

            public void Release()
            {
                obj = null;
                context = null;
                parentProperty = null;
                callback = null;
                objectChangedCallback = null;
                requestType = RequestType.Bind;
                s_Pool.Release(this);
            }
        }
    }

    //TODO: Pool this


    internal abstract class SerializedObjectBindingBase : IBinding
    {
        public SerializedObjectBindingContext bindingContext
        {
            get => m_BindingContext;
            private set
            {
                if (m_BindingContext != null)
                {
                    m_BindingContext.UnregisterBindingObject(this);
                }

                m_BindingContext = value;

                if (m_BindingContext != null)
                {
                    m_BindingContext.RegisterBindingObject(this);
                    SyncObjectVersion();
                }
                else
                {
                    boundProperty = null;
                }
            }
        }

        public string boundPropertyPath;
        public int boundPropertyHash;
        private SerializedProperty m_BoundProperty;

        public SerializedProperty boundProperty
        {
            get => m_BoundProperty;
            set
            {
                m_BoundProperty = value;

                if (m_BoundProperty != null)
                {
                    boundPropertyPath = m_BoundProperty.propertyPath;
                    boundPropertyHash = m_BoundProperty.hashCodeForPropertyPath;
                }
                else
                {
                    boundPropertyPath = null;
                    boundPropertyHash = 0;
                }
            }
        }

        protected bool isReleased { get; set; }
        protected bool isUpdating { get; set; }
        public abstract void Update();
        public abstract void Release();

        protected void SetContext(SerializedObjectBindingContext context, SerializedProperty prop)
        {
            if (context != null)
            {
                boundProperty = prop;
            }

            bindingContext = context;
        }

        protected void ResetContext()
        {
            bindingContext = null;
        }

        public void PreUpdate()
        {
            bindingContext?.UpdateIfNecessary();
        }

        public virtual void ResetUpdate()
        {
            bindingContext?.ResetUpdate();
        }

        protected IBindable m_Field;
        private SerializedObjectBindingContext m_BindingContext;

        private bool m_TooltipWasSet;
        private bool m_IsFieldAttached;

        EventCallback<AttachToPanelEvent> m_OnFieldAttachedToPanel;
        EventCallback<DetachFromPanelEvent> m_OnFieldDetachedFromPanel;

        protected SerializedObjectBindingBase()
        {
            m_OnFieldAttachedToPanel = OnFieldAttached;
            m_OnFieldDetachedFromPanel = OnFieldDetached;
        }

        protected IBindable boundElement
        {
            get { return m_Field; }
            set
            {
                if (m_Field is VisualElement ve)
                {
                    ve.UnregisterCallback(m_OnFieldAttachedToPanel);
                    ve.UnregisterCallback(m_OnFieldDetachedFromPanel);
                    if (m_TooltipWasSet)
                        ve.tooltip = null;
                    m_TooltipWasSet = false;
                }

                FieldBinding = null;

                m_Field = value;
                FieldBinding = this;
                UpdateFieldIsAttached();
                if (m_Field != null)
                {
                    ve = m_Field as VisualElement;
                    if (ve != null)
                    {
                        ve.RegisterCallback(m_OnFieldAttachedToPanel);
                        ve.RegisterCallback(m_OnFieldDetachedFromPanel);

                        if (string.IsNullOrEmpty(ve.tooltip))
                        {
                            ve.tooltip = boundProperty?.tooltip;
                            m_TooltipWasSet = true;
                        }
                    }
                }
            }
        }

        protected IBinding FieldBinding
        {
            get
            {
                var bindable = m_Field as IBindable;
                return bindable?.binding;
            }
            set
            {
                if (m_Field != null)
                {
                    var previousBinding = m_Field.binding;
                    m_Field.binding = value;
                    if (previousBinding != this)
                    {
                        previousBinding?.Release();
                    }
                    (m_Field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);
                }
            }
        }

        protected bool isFieldAttached
        {
            get => m_IsFieldAttached;
            private set
            {
                if (m_IsFieldAttached != value)
                {
                    m_IsFieldAttached = value;

                    if (m_IsFieldAttached)
                    {
                        m_BindingContext?.RegisterBindingObject(this);
                        if (boundProperty != null)
                        {
                            //we make sure the property value is applied
                            OnPropertyValueChanged(boundProperty);
                        }
                    }
                    else
                    {
                        m_BindingContext?.UnregisterBindingObject(this);
                    }
                }

            }
        }

        private void OnFieldAttached(AttachToPanelEvent evt)
        {
            isFieldAttached = true;
            ResetCachedValues();
        }

        private void OnFieldDetached(DetachFromPanelEvent evt)
        {
            isFieldAttached = false;
        }

        protected void UpdateFieldIsAttached()
        {
            if (m_Field is VisualElement ve)
            {
                bool attached = ve.panel != null;

                if (isFieldAttached != attached)
                {
                    isFieldAttached = attached;
                    if (attached)
                    {
                        ResetCachedValues();
                    }
                }
            }
            else
            {
                //we're not dealing with VisualElement
                if (m_Field != null && !isFieldAttached)
                {
                    isFieldAttached = true;
                    ResetCachedValues();
                }
                else
                {
                    isFieldAttached = false;
                }
            }
        }

        protected abstract void ResetCachedValues();

        public virtual void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
        {
        }

        public bool ResolveProperty()
        {

            boundProperty = (bindingContext != null && bindingContext.IsValid()) ? bindingContext.serializedObject.FindProperty(boundPropertyPath) : null;
            return boundProperty != null;
        }

        private ulong m_LastSyncedVersion = 0;

        public void SyncObjectVersion()
        {
            if (m_BindingContext != null)
            {
                m_LastSyncedVersion = m_BindingContext.lastRevision;
            }
        }

        public bool IsSynced()
        {
            return m_BindingContext != null && m_LastSyncedVersion == m_BindingContext.lastRevision;
        }
    }

    internal sealed class SerializedObjectBindingContextUpdater : SerializedObjectBindingBase
    {
        public static ObjectPool<SerializedObjectBindingContextUpdater> s_Pool =
            new ObjectPool<SerializedObjectBindingContextUpdater>(() => new SerializedObjectBindingContextUpdater(), 32);

        private VisualElement owner;

        private UInt64 lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;

        public event Action<object, SerializedObject> registeredCallbacks;

        public static SerializedObjectBindingContextUpdater Create(VisualElement owner, SerializedObjectBindingContext context)
        {
            var b = s_Pool.Get();

            b.isReleased = false;
            b.SetContext(context, null);
            b.owner = owner;
            b.lastTrackedObjectRevision = context.lastRevision;
            return b;
        }

        public SerializedObjectBindingContextUpdater()
        {
            trackedProperties = new List<SerializedProperty>();
        }

        private List<SerializedProperty> trackedProperties { get; }

        public void AddTracking(SerializedProperty prop)
        {
            trackedProperties.Add(prop);
        }

        public override void Update()
        {
            if (isReleased)
                return;


            if (bindingContext != null)
            {
                ResetUpdate();

                if (lastTrackedObjectRevision != bindingContext.lastRevision)
                {
                    lastTrackedObjectRevision = bindingContext.lastRevision;

                    registeredCallbacks?.Invoke(owner, bindingContext.serializedObject);
                    return;
                }
            }
        }

        public override void Release()
        {
            if (isReleased)
                return;

            if (owner != null && bindingContext != null)
            {
                foreach (var prop in trackedProperties)
                {
                    bindingContext.UnregisterSerializedPropertyChangeCallback(owner, prop);
                }
            }

            trackedProperties.Clear();
            owner = null;

            ResetContext();

            registeredCallbacks = null;
            ResetCachedValues();
            isReleased = true;
            s_Pool.Release(this);
        }

        protected override void ResetCachedValues()
        {
            lastTrackedObjectRevision = 0xFFFFFFFFFFFFFFFF;
        }
    }

    abstract class SerializedObjectBindingToBaseField<TValue, TField> : SerializedObjectBindingBase where TField : class, INotifyValueChanged<TValue>
    {
        EventCallback<ChangeEvent<TValue>> m_FieldValueChanged;

        protected TField field
        {
            get { return m_Field as TField; }
            set
            {
                var ve = field as VisualElement;
                ve?.UnregisterCallback(m_FieldValueChanged, TrickleDown.TrickleDown);
                boundElement = value as IBindable;
                ve = field as VisualElement;
                ve?.RegisterCallback(m_FieldValueChanged, TrickleDown.TrickleDown);
            }
        }

        protected SerializedObjectBindingToBaseField()
        {
            m_FieldValueChanged = FieldValueChanged;
        }

        private void FieldValueChanged(ChangeEvent<TValue> evt)
        {
            if (isReleased || isUpdating)
                return;

            if (evt.target != m_Field)
                return;

            try
            {
                var bindable = evt.target as IBindable;
                var binding = bindable?.binding;

                if (binding == this && ResolveProperty())
                {
                    if (!isFieldAttached)
                    {
                        //we don't update when field is not attached to a panel
                        //but we don't kill binding either
                        return;
                    }

                    UpdateLastFieldValue();

                    if (SyncFieldValueToProperty())
                    {
                        bindingContext.UpdateRevision(); //we make sure to Poll the ChangeTracker here
                        bindingContext.ResetUpdate();
                    }

                    BindingsStyleHelpers.UpdateElementStyle(field as VisualElement, boundProperty);
                    return;
                }
            }
            catch
            {
                //this can happen when serializedObject has been disposed of
            }

            // Something was wrong
            Release();
        }

        protected override void ResetCachedValues()
        {
            UpdateLastFieldValue();
            UpdateFieldIsAttached();

            if (field is ObjectField objectField)
            {
                objectField.SetProperty(ObjectField.serializedPropertyKey, boundProperty);
                objectField.UpdateDisplay();
            }
        }

        public override void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
        {
            if (isReleased)
                return;
            try
            {
                isUpdating = true;
                var veField = field as VisualElement;
                var bindable = field as IBindable;

                if (bindable.binding == this)
                {
                    SyncPropertyToField(field, currentPropertyIterator);
                    BindingsStyleHelpers.UpdateElementStyle(veField, currentPropertyIterator);
                    return;
                }
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }
            // We unbind here
            Release();
        }

        public override void Update()
        {
            if (isReleased)
                return;
            try
            {
                ResetUpdate();

                if (!IsSynced())
                    return;

                isUpdating = true;

                if (FieldBinding == this)
                {
                    var veField = field as VisualElement;

                    // Value might not have changed but prefab state could have been reverted, so we need to
                    // at least update the prefab override visual if necessary. Happens when user reverts a
                    // field where the value is the same as the prefab registered value. Case 1276154.
                    BindingsStyleHelpers.UpdatePrefabStateStyle(veField, boundProperty);

                    if (EditorApplication.isPlaying && SerializedObject.GetLivePropertyFeatureGlobalState() && boundProperty.isLiveModified)
                        BindingsStyleHelpers.UpdateLivePropertyStateStyle(veField, boundProperty);

                    return;
                }
            }
            catch (ArgumentNullException)
            {
                //this can happen when serializedObject has been disposed of
            }
            finally
            {
                isUpdating = false;
            }

            // Something failed, we unbind here
            Release();
        }

        // Read the value from the ui field and save it.
        protected abstract void UpdateLastFieldValue();

        protected abstract bool SyncFieldValueToProperty();
        protected abstract void SyncPropertyToField(TField c, SerializedProperty p);
    }

    abstract class SerializedObjectBindingPropertyToBaseField<TProperty, TValue> : SerializedObjectBindingToBaseField<TValue, INotifyValueChanged<TValue>>
    {
        protected Func<SerializedProperty, TProperty> propGetValue;
        protected Action<SerializedProperty, TProperty> propSetValue;
        protected Func<TProperty, SerializedProperty, Func<SerializedProperty, TProperty>, bool> propCompareValues;

        //we need to keep a copy of the last value since some fields will allocate when getting the value
        protected TProperty lastFieldValue;

        protected override void SyncPropertyToField(INotifyValueChanged<TValue> c, SerializedProperty p)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            lastFieldValue = propGetValue(p);
            AssignValueToField(lastFieldValue);
        }

        protected override bool SyncFieldValueToProperty()
        {
            if (!propCompareValues(lastFieldValue, boundProperty, propGetValue))
            {
                propSetValue(boundProperty, lastFieldValue);
                boundProperty.m_SerializedObject.ApplyModifiedProperties();

                // Force the field to update its display as its label is dependent on having an up to date SerializedProperty. (UUM-27629)
                if (field is ObjectField objectField)
                {
                    objectField.UpdateDisplay();
                }

                return true;
            }
            return false;
        }

        protected abstract void AssignValueToField(TProperty lastValue);

        public override void Release()
        {
            if (isReleased)
                return;

            if (FieldBinding == this)
            {
                FieldBinding = null;

                if (field is BaseField<TValue> bf)
                {
                    BindingsStyleHelpers.UnregisterRightClickMenu(bf);
                }else if (field is Foldout foldout)
                {
                    BindingsStyleHelpers.UnregisterRightClickMenu(foldout);
                }
            }

            ResetContext();
            field = null;
            propGetValue = null;
            propSetValue = null;
            propCompareValues = null;
            ResetCachedValues();
            isReleased = true;
        }
    }
    class SerializedObjectBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, TValue>
    {
        public static ObjectPool<SerializedObjectBinding<TValue>> s_Pool =
            new ObjectPool<SerializedObjectBinding<TValue>>(() => new SerializedObjectBinding<TValue>(), 32);

        public static void CreateBind(INotifyValueChanged<TValue> field,
            SerializedObjectBindingContext context,
            SerializedProperty property,
            Func<SerializedProperty, TValue> propGetValue,
            Action<SerializedProperty, TValue> propSetValue,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
        {
            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            newBinding.SetBinding(field, context, property, propGetValue, propSetValue, propCompareValues);
        }

        private void SetBinding(INotifyValueChanged<TValue> c,
            SerializedObjectBindingContext context,
            SerializedProperty property,
            Func<SerializedProperty, TValue> getValue,
            Action<SerializedProperty, TValue> setValue,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
        {
            property.unsafeMode = true;

            this.propGetValue = getValue;
            this.propSetValue = setValue;
            this.propCompareValues = compareValues;

            SetContext(context, property);

            var originalValue = this.lastFieldValue = c.value;

            if (c is BaseField<TValue> bf)
            {
                BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
            }
            else if (c is Foldout foldout)
            {
                BindingsStyleHelpers.RegisterRightClickMenu(foldout, property);
            }

            this.field = c;

            if (compareValues(originalValue, property, getValue)) //the value hasn't changed, but we want the binding to send an event no matter what
            {
                if (this.field is VisualElement handler)
                {
                    using (ChangeEvent<TValue> evt = ChangeEvent<TValue>.GetPooled(originalValue, originalValue))
                    {
                        evt.target = handler;
                        handler.SendEvent(evt);
                    }
                }
            }
        }

        public override void Release()
        {
            base.Release();
            s_Pool.Release(this);
        }

        protected override void UpdateLastFieldValue()
        {
            if (field == null)
            {
                return;
            }

            lastFieldValue = field.value;
        }

        protected override void AssignValueToField(TValue lastValue)
        {
            if (field == null)
            {
                return;
            }

            if (field is BaseField<TValue> baseField)
            {
                baseField.SetValueWithoutValidation(lastValue);
            }
            else
            {
                field.value = lastValue;
            }
        }
    }

    class SerializedIsExpandedBinding : SerializedObjectBindingPropertyToBaseField<bool, bool>
    {
        static readonly ObjectPool<SerializedIsExpandedBinding> s_Pool =
            new ObjectPool<SerializedIsExpandedBinding>(() => new SerializedIsExpandedBinding(), 32);

        const string serializedBindingId = "--unity-serialized-is-expanded-bindings";

        readonly Clickable m_ClickedWithAlt;

        public SerializedIsExpandedBinding()
        {
            m_ClickedWithAlt = new Clickable(OnClickWithAlt);
            m_ClickedWithAlt.activators.Clear();
            m_ClickedWithAlt.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
        }

        public static void CreateBind(Foldout field, SerializedObjectBindingContext context, SerializedProperty property)
        {
            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            field?.SetProperty(serializedBindingId, newBinding);
            newBinding.SetBinding(field, context, property);
            newBinding.AddClickedManipulator();
        }

        protected void SetBinding(Foldout foldout, SerializedObjectBindingContext context, SerializedProperty property)
        {
            property.unsafeMode = true;
            propGetValue = GetValue;
            propSetValue = SetValue;
            propCompareValues = SerializedPropertyHelper.ValueEquals<bool>;

            SetContext(context, property);
            var originalValue = this.lastFieldValue = foldout.value;
            BindingsStyleHelpers.RegisterRightClickMenu(foldout, property);
            field = foldout;

            if (propCompareValues(originalValue, property, propGetValue)) //the value hasn't changed, but we want the binding to send an event no matter what
            {
                using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(originalValue, originalValue))
                {
                    evt.target = foldout;
                    foldout.SendEvent(evt);
                }
            }
        }

        public override void Release()
        {
            if (isReleased)
                return;

            base.Release();
            RemoveClickedManipulator();
            s_Pool.Release(this);
        }

        protected override void UpdateLastFieldValue()
        {
            if (field is Foldout foldout)
                lastFieldValue = foldout.value;
        }

        protected override void AssignValueToField(bool lastValue)
        {
            if (field is Foldout foldout)
                foldout.value = lastValue;
        }

        static bool GetValue(SerializedProperty property) => property.isExpanded;
        static void SetValue(SerializedProperty property, bool value) => property.isExpanded = value;

        void AddClickedManipulator() => ((Foldout)field).toggle.AddManipulator(m_ClickedWithAlt);
        void RemoveClickedManipulator() => ((Foldout)field).RemoveManipulator(m_ClickedWithAlt);

        void OnClickWithAlt()
        {
            EditorGUI.SetExpandedRecurse(boundProperty, !boundProperty.isExpanded);

            // Force all visible field to update
            foreach (var f in ((Foldout)field).Query<Foldout>().Build())
            {
                if (f.GetProperty(serializedBindingId) is SerializedIsExpandedBinding binding)
                    binding.OnPropertyValueChanged(binding.boundProperty);
            }
        }
    }

    // specific enum version that binds on the index property of the BaseField<Enum>
    class SerializedManagedEnumBinding : SerializedObjectBindingToBaseField<Enum, BaseField<Enum>>
    {
        public static ObjectPool<SerializedManagedEnumBinding> s_Pool =
            new ObjectPool<SerializedManagedEnumBinding>(() => new SerializedManagedEnumBinding(), 32);

        //we need to keep a copy of the last value since some fields will allocate when getting the value
        private int lastEnumValue;
        private Type managedType;

        public static void CreateBind(BaseField<Enum> field,  SerializedObjectBindingContext context,
            SerializedProperty property)
        {
            Type managedType;
            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out managedType);

            if (managedType == null)
            {
                Debug.LogWarning(
                    $"{field.GetType().FullName} is not compatible with property \"{property.propertyPath}\". " +
                    "Make sure you're binding to a managed enum type");
                return;
            }

            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            newBinding.SetBinding(field, context, property, managedType);
        }

        private void SetBinding(BaseField<Enum> c, SerializedObjectBindingContext context,
            SerializedProperty property, Type manageType)
        {
            this.managedType = manageType;
            property.unsafeMode = true;

            SetContext(context, property);

            int enumValueAsInt = property.intValue;

            Enum value = GetEnumFromSerializedFromInt(manageType, ref enumValueAsInt);

            if (c is EnumField)
                (c as EnumField).Init(value);
            else if (c is EnumFlagsField)
                (c as EnumFlagsField).Init(value);
            else
            {
                throw new InvalidOperationException(c.GetType() + " cannot be bound to a enum");
            }

            lastEnumValue = enumValueAsInt;

            var previousValue = c.value;

            c.value = value;

            BindingsStyleHelpers.RegisterRightClickMenu(c, property);

            // Make sure to write this property only after setting a first value into the field
            // This avoid any null checks in regular update methods
            this.field = c;

            if (!EqualityComparer<Enum>.Default.Equals(previousValue, c.value))
            {
                if (c is VisualElement handler)
                {
                    using (ChangeEvent<Enum> evt = ChangeEvent<Enum>.GetPooled(previousValue, previousValue))
                    {
                        evt.target = handler;
                        handler.SendEvent(evt);
                    }
                }
            }
        }

        static Enum GetEnumFromSerializedFromInt(Type managedType, ref int enumValueAsInt)
        {
            var enumData = EnumDataUtility.GetCachedEnumData(managedType);

            if (enumData.flags)
                return EnumDataUtility.IntToEnumFlags(managedType, enumValueAsInt);

            int valueIndex = Array.IndexOf(enumData.flagValues, enumValueAsInt);
            if (valueIndex != -1)
                return enumData.values[valueIndex];

            // For binding, return the minimal default value if enumValueAsInt is smaller than the smallest enum value,
            // especially if no default enum is defined
            if (enumData.flagValues.Length != 0)
            {
                var minIntValue = enumData.flagValues[0];
                var minIntValueIndex = 0;
                for (int i = 1; i < enumData.flagValues.Length; i++)
                {
                    if (enumData.flagValues[i] < minIntValue)
                    {
                        minIntValueIndex = i;
                        minIntValue = enumData.flagValues[i];
                    }
                }

                if (enumValueAsInt <= minIntValue || (enumValueAsInt == 0 && minIntValue < 0))
                {
                    enumValueAsInt = minIntValue;
                    return enumData.values[minIntValueIndex];
                }
            }

            Debug.LogWarning("Error: invalid enum value " + enumValueAsInt + " for type " + managedType);
            return null;
        }

        protected override void SyncPropertyToField(BaseField<Enum> c, SerializedProperty p)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            int enumValueAsInt = p.intValue;
            field.value = GetEnumFromSerializedFromInt(managedType, ref enumValueAsInt);
            lastEnumValue = enumValueAsInt;
        }

        protected override void UpdateLastFieldValue()
        {
            if (field == null || managedType == null)
            {
                lastEnumValue = 0;
                return;
            }

            var enumData = EnumDataUtility.GetCachedEnumData(managedType);

            Enum fieldValue = field?.value;

            if (enumData.flags)
                lastEnumValue = EnumDataUtility.EnumFlagsToInt(enumData, fieldValue);
            else
            {
                int valueIndex = Array.IndexOf(enumData.values, fieldValue);

                if (valueIndex != -1)
                    lastEnumValue = enumData.flagValues[valueIndex];
                else
                {
                    lastEnumValue = 0;
                    if (field != null)
                        Debug.LogWarning("Error: invalid enum value " + fieldValue + " for type " + managedType);
                }
            }
        }

        protected override bool SyncFieldValueToProperty()
        {
            if (lastEnumValue == boundProperty.intValue)
                return false;

            // When the value is a negative we need to convert it or it will be clamped.
            var underlyingType = managedType.GetEnumUnderlyingType();
            if (lastEnumValue < 0 && (underlyingType == typeof(uint) || underlyingType == typeof(ushort) || underlyingType == typeof(byte)))
            {
                boundProperty.longValue = (uint)lastEnumValue;
            }
            else
            {
                boundProperty.intValue = lastEnumValue;
            }
            boundProperty.m_SerializedObject.ApplyModifiedProperties();
            return true;
        }

        public override void Release()
        {
            if (isReleased)
                return;

            if (FieldBinding == this)
            {
                // Make sure to nullify the field to unbind before reverting the enum value
                var saveField = field;
                BindingsStyleHelpers.UnregisterRightClickMenu(saveField);

                field = null;
                saveField.value = null;
                FieldBinding = null;
            }

            ResetContext();

            field = null;
            managedType = null;
            isReleased = true;

            ResetCachedValues();
            s_Pool.Release(this);
        }
    }

    // specific enum version that binds on the index property of the PopupField<string>
    class SerializedDefaultEnumBinding : SerializedObjectBindingToBaseField<string, PopupField<string>>
    {
        public static ObjectPool<SerializedDefaultEnumBinding> s_Pool =
            new ObjectPool<SerializedDefaultEnumBinding>(() => new SerializedDefaultEnumBinding(), 32);

        private const int kDefaultValueIndex = -1;

        //we need to keep a copy of the last value since some fields will allocate when getting the value
        private int lastFieldValueIndex;

        private List<string> originalChoices;
        private List<int> displayIndexToEnumIndex;
        private List<int> enumIndexToDisplayIndex;
        private int originalIndex;

        public static void CreateBind(PopupField<string> field,  SerializedObjectBindingContext context,
            SerializedProperty property)
        {
            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            newBinding.SetBinding(field, context, property);
        }

        private void SetBinding(PopupField<string> c, SerializedObjectBindingContext context,
            SerializedProperty property)
        {
            property.unsafeMode = true;
            SetContext(context, property);

            this.originalChoices = c.choices;
            this.originalIndex = c.index;

            // We need to keep bidirectional lists of indices to translate between Popup choice index and
            // SerializedProperty enumValueIndex because the Popup choices might be displayed in another language
            // (using property.enumLocalizedDisplayNames), or in another display order (using enumData.displayNames).
            // We need to build the bidirectional lists when we assign field.choices, using any of the above options.

            if (displayIndexToEnumIndex == null)
                displayIndexToEnumIndex = new List<int>();
            else
                displayIndexToEnumIndex.Clear();

            if (enumIndexToDisplayIndex == null)
                enumIndexToDisplayIndex = new List<int>();
            else
                enumIndexToDisplayIndex.Clear();

            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var enumType);
            if (enumType != null)
            {
                var enumData = EnumDataUtility.GetCachedEnumData(enumType, UnityEngine.EnumDataUtility.CachedType.ExcludeObsolete);
                var enumDataOld = EnumDataUtility.GetCachedEnumData(enumType, UnityEngine.EnumDataUtility.CachedType.IncludeAllObsolete);
                c.choices = new List<string>(enumData.displayNames);

                var sortedEnumNames = EditorGUI.EnumNamesCache.GetEnumNames(property);
                // Build a name to value lookup. We need this to check for duplicate values.
                var nameValueDict = UnityEngine.Pool.DictionaryPool<string, int>.Get();
                for (int i = 0; i < enumDataOld.names.Length; ++i)
                {
                    nameValueDict[enumDataOld.names[i]] = enumDataOld.flagValues[i];
                }

                foreach (var enumName in enumData.names) 
                    displayIndexToEnumIndex.Add(Array.IndexOf(sortedEnumNames, enumName));
                foreach (var sortedEnumName in sortedEnumNames)
                    enumIndexToDisplayIndex.Add(Array.IndexOf(enumData.names, sortedEnumName));

                 // We need to map the display index to the first occurrence of the value in the serialized property enum names.
                // The serialized property lacks information about obsolete enum values, so it always maps to the first occurrence of the value,
                // regardless of its obsolescence and visibility.
                // Additionally, we must handle obsolete values that are not displayed, as the enumValueIndex encompasses all values,
                // including those marked as obsolete but not visible. (UUM-36836, UUM-31162)
                var firstOccurrenceIndexToValueDict = UnityEngine.Pool.DictionaryPool<int, int>.Get();
                for (int i = 0; i < sortedEnumNames.Length; ++i)
                {
                    var value = nameValueDict[sortedEnumNames[i]];

                    var displayIndex = Array.IndexOf(enumData.names, sortedEnumNames[i]);
                    if (displayIndex != -1)
                    {
                        // If we have already encountered this value then we need to use the first index as the serialized property will always map to this one.
                        if (firstOccurrenceIndexToValueDict.TryGetValue(value, out var firstEnumIndex))
                        {
                            displayIndexToEnumIndex[displayIndex] = firstEnumIndex;
                            enumIndexToDisplayIndex[firstEnumIndex] = displayIndex;
                        }
                        else
                        {
                            firstOccurrenceIndexToValueDict[value] = i;
                            displayIndexToEnumIndex[displayIndex] = i;
                        }

                        enumIndexToDisplayIndex[i] = displayIndex;
                    }
                    else
                    {
                        firstOccurrenceIndexToValueDict[value] = i;
                    }
                }

                UnityEngine.Pool.DictionaryPool<string, int>.Release(nameValueDict);
                UnityEngine.Pool.DictionaryPool<int, int>.Release(firstOccurrenceIndexToValueDict);
            }
            else
            {
                c.choices = new List<string>(property.enumLocalizedDisplayNames);

                for (int i = 0; i < c.choices.Count; i++)
                {
                    displayIndexToEnumIndex.Add(i);
                    enumIndexToDisplayIndex.Add(i);
                }
            }


            var originalValue = this.lastFieldValueIndex = c.index;

            BindingsStyleHelpers.RegisterRightClickMenu(c, property);

            this.field = c;

            if (originalValue == c.index) //the value hasn't changed, but we want the binding to send an event no matter what
            {
                if (c is VisualElement handler)
                {
                    using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(c.value, c.value))
                    {
                        evt.target = handler;
                        handler.SendEvent(evt);
                    }
                }
            }
        }

        protected override void SyncPropertyToField(PopupField<string> c, SerializedProperty p)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            int propValueIndex = p.enumValueIndex;
            if (propValueIndex >= 0 && propValueIndex < enumIndexToDisplayIndex.Count)
            {
                c.index = lastFieldValueIndex = enumIndexToDisplayIndex[propValueIndex];
            }
            else
            {
                c.index = lastFieldValueIndex = kDefaultValueIndex;
            }
        }

        protected override void UpdateLastFieldValue()
        {
            if (field == null)
            {
                lastFieldValueIndex  = Int32.MinValue;
            }
            else
            {
                lastFieldValueIndex = field.index;
            }
        }

        protected override bool SyncFieldValueToProperty()
        {
            if (lastFieldValueIndex >= 0 && lastFieldValueIndex < displayIndexToEnumIndex.Count
                && boundProperty.enumValueIndex != displayIndexToEnumIndex[lastFieldValueIndex])
            {
                boundProperty.enumValueIndex = displayIndexToEnumIndex[lastFieldValueIndex];
                boundProperty.m_SerializedObject.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        public override void Release()
        {
            if (isReleased)
                return;

            if (FieldBinding == this)
            {
                //we set the popup values to the original ones
                try
                {
                    var previousField = field;
                    BindingsStyleHelpers.UnregisterRightClickMenu(previousField);

                    field = null;
                    previousField.choices = originalChoices;
                    previousField.index = originalIndex;
                }
                catch (ArgumentException)
                {
                    //we did our best
                }

                FieldBinding = null;
            }


            ResetContext();
            field = null;
            lastFieldValueIndex = kDefaultValueIndex;
            isReleased = true;

            ResetCachedValues();
            s_Pool.Release(this);
        }
    }


    //One-way binding
    class SerializedObjectStringConversionBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, string>
    {
        public static ObjectPool<SerializedObjectStringConversionBinding<TValue>> s_Pool =
            new ObjectPool<SerializedObjectStringConversionBinding<TValue>>(() => new SerializedObjectStringConversionBinding<TValue>(), 32);

        public static void CreateBind(INotifyValueChanged<string> field,
            SerializedObjectBindingContext context,
            SerializedProperty property,
            Func<SerializedProperty, TValue> propGetValue,
            Action<SerializedProperty, TValue> propSetValue,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
        {
            var newBinding = s_Pool.Get();
            newBinding.isReleased = false;
            newBinding.SetBinding(field, context, property, propGetValue, propSetValue, propCompareValues);
        }

        private void SetBinding(INotifyValueChanged<string> c,
            SerializedObjectBindingContext context,
            SerializedProperty property,
            Func<SerializedProperty, TValue> getValue,
            Action<SerializedProperty, TValue> setValue,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
        {
            property.unsafeMode = true;

            this.propGetValue = getValue;
            this.propSetValue = setValue;
            this.propCompareValues = compareValues;

            SetContext(context, property);

            this.field = c;

            if (c is BaseField<TValue> bf)
            {
                BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
            }

            var previousFieldValue = field.value;

            // In this subclass implementation the lastFieldValue is in fact the propertyValue assigned to the field.
            // this is made to compare TValues instead of strings
            UpdateLastFieldValue();
            AssignValueToField(lastFieldValue);

            if (previousFieldValue == field.value) //the value hasn't changed, but we want the binding to send an event no matter what
            {
                if (this.field is VisualElement handler)
                {
                    using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previousFieldValue, previousFieldValue))
                    {
                        evt.target = handler;
                        handler.SendEvent(evt);
                    }
                }
            }
        }

        protected override void UpdateLastFieldValue()
        {
            if (field == null)
            {
                lastFieldValue = default(TValue);
                return;
            }

            lastFieldValue = propGetValue(boundProperty);
        }

        public override void Release()
        {
            base.Release();
            s_Pool.Release(this);
        }

        protected override void AssignValueToField(TValue lastValue)
        {
            if (field == null)
            {
                return;
            }

            field.value = $"{lastFieldValue}";
        }
    }
}
