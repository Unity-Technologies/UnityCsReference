// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class SerializedObjectBindEvent : EventBase<SerializedObjectBindEvent>, IPropagatableEvent
    {
        private SerializedObject m_BindObject;
        public SerializedObject bindObject
        {
            get
            {
                return m_BindObject;
            }
        }

        protected override void Init()
        {
            base.Init();
            this.flags = EventFlags.Cancellable; // Also makes it not propagatable.
            m_BindObject = null;
        }

        public static SerializedObjectBindEvent GetPooled(SerializedObject obj)
        {
            SerializedObjectBindEvent e = GetPooled();
            e.m_BindObject = obj;
            return e;
        }
    }

    internal class SerializedPropertyBindEvent : EventBase<SerializedPropertyBindEvent>, IPropagatableEvent
    {
        private SerializedProperty m_BindProperty;
        public SerializedProperty bindProperty
        {
            get
            {
                return m_BindProperty;
            }
        }

        protected override void Init()
        {
            base.Init();
            this.flags = EventFlags.Cancellable; // Also makes it not propagatable.
            m_BindProperty = null;
        }

        public static SerializedPropertyBindEvent GetPooled(SerializedProperty obj)
        {
            SerializedPropertyBindEvent e = GetPooled();
            e.m_BindProperty = obj;
            return e;
        }
    }

    public static class BindingExtensions
    {
        // visual element style changes wrt its property state
        internal static readonly string k_PrefabOverrideClassName = "unity-prefab-override";

        public static void Bind(this VisualElement element, SerializedObject obj)
        {
            Bind(element, new SerializedObjectUpdateWrapper(obj), null);
        }

        public static void Unbind(this VisualElement element)
        {
            RemoveBinding(element);

            for (int i = 0; i < element.shadow.childCount; ++i)
            {
                Unbind(element.shadow[i]);
            }
        }

        public static SerializedProperty BindProperty(this IBindable field, SerializedObject obj)
        {
            return BindPropertyWithParent(field, new SerializedObjectUpdateWrapper(obj), null);
        }

        public static void BindProperty(this IBindable field, SerializedProperty property)
        {
            DoBindProperty(field, new SerializedObjectUpdateWrapper(property.serializedObject), property);
        }

        private static void DoBindProperty(IBindable field, SerializedObjectUpdateWrapper obj, SerializedProperty property)
        {
            var fieldElement = field as VisualElement;
            if (property == null || fieldElement == null)
            {
                // Object is null or property was not found, we have to make sure we delete any previous binding
                RemoveBinding(fieldElement);
                return;
            }

            // This covers the case where a field is being manually bound to a property
            field.bindingPath = property.propertyPath;

            if (property != null && fieldElement != null)
            {
                using (var evt = SerializedPropertyBindEvent.GetPooled(property))
                {
                    if (SendBindingEvent(evt, fieldElement))
                    {
                        return;
                    }
                }
            }

            CreateBindingObjectForProperty(fieldElement, obj, property);
        }

        private static void Bind(VisualElement element, SerializedObjectUpdateWrapper objWrapper, SerializedProperty parentProperty)
        {
            IBindable field = element as IBindable;

            using (var evt = SerializedObjectBindEvent.GetPooled(objWrapper.obj))
            {
                if (SendBindingEvent(evt, element))
                {
                    return;
                }
            }

            if (field != null)
            {
                if (!string.IsNullOrEmpty(field.bindingPath))
                {
                    var foundProperty = BindPropertyWithParent(field, objWrapper, parentProperty);
                    if (foundProperty != null)
                    {
                        parentProperty = foundProperty;
                    }
                }
            }

            for (int i = 0; i < element.shadow.childCount; ++i)
            {
                Bind(element.shadow[i], objWrapper, parentProperty);
            }
        }

        private static SerializedProperty BindPropertyWithParent(IBindable field, SerializedObjectUpdateWrapper objWrapper, SerializedProperty parentProperty)
        {
            var property = parentProperty?.FindPropertyRelative(field.bindingPath);

            if (property == null)
            {
                property = objWrapper.obj?.FindProperty(field.bindingPath);
            }

            DoBindProperty(field, objWrapper, property);

            return property;
        }

        private static bool SendBindingEvent<TEventType>(TEventType evt, VisualElement target) where TEventType : EventBase<TEventType>, new()
        {
            evt.target = target;
            evt.propagationPhase = PropagationPhase.AtTarget;
            target.HandleEvent(evt);
            return evt.isPropagationStopped;
        }

        private static void RemoveBinding(VisualElement element)
        {
            IBindable field = element as IBindable;
            if (element == null || !field.IsBound())
            {
                return;
            }
            if (field != null)
            {
                field.binding?.Release();
                field.binding = null;
            }
        }

        /// Property getters
        private static int GetIntPropertyValue(SerializedProperty p) { return p.intValue; }
        private static bool GetBoolPropertyValue(SerializedProperty p) { return p.boolValue; }
        private static float GetFloatPropertyValue(SerializedProperty p) { return p.floatValue; }
        private static double GetDoublePropertyValue(SerializedProperty p) { return p.doubleValue; }
        private static string GetStringPropertyValue(SerializedProperty p) { return p.stringValue; }
        private static Color GetColorPropertyValue(SerializedProperty p) { return p.colorValue; }
        private static UnityEngine.Object GetObjectRefPropertyValue(SerializedProperty p) {return p.objectReferenceValue; }
        private static int GetLayerMaskPropertyValue(SerializedProperty p) {return p.intValue; }
        private static Vector2 GetVector2PropertyValue(SerializedProperty p) { return p.vector2Value; }
        private static Vector3 GetVector3PropertyValue(SerializedProperty p) { return p.vector3Value; }
        private static Vector4 GetVector4PropertyValue(SerializedProperty p) { return p.vector4Value; }
        private static Vector2Int GetVector2IntPropertyValue(SerializedProperty p) { return p.vector2IntValue; }
        private static Vector3Int GetVector3IntPropertyValue(SerializedProperty p) { return p.vector3IntValue; }
        private static Rect GetRectPropertyValue(SerializedProperty p) { return p.rectValue; }
        private static RectInt GetRectIntPropertyValue(SerializedProperty p) { return p.rectIntValue; }
        private static AnimationCurve GetAnimationCurvePropertyValue(SerializedProperty p) { return p.animationCurveValue; }
        private static Bounds GetBoundsPropertyValue(SerializedProperty p) { return p.boundsValue; }
        private static BoundsInt GetBoundsIntPropertyValue(SerializedProperty p) { return p.boundsIntValue; }
        private static Gradient GetGradientPropertyValue(SerializedProperty p) { return p.gradientValue; }
        private static Quaternion GetQuaternionPropertyValue(SerializedProperty p) { return p.quaternionValue; }
        private static char GetCharacterPropertyValue(SerializedProperty p) { return (char)p.intValue; }


        // Basic conversions
        private static float GetDoublePropertyValueAsFloat(SerializedProperty p) { return (float)p.doubleValue; }
        private static double GetFloatPropertyValueAsDouble(SerializedProperty p) { return (double)p.floatValue; }
        private static string GetCharacterPropertyValueAsString(SerializedProperty p) { return new string((char)p.intValue, 1); }

        //this one is a bit more tricky
        private static string GetEnumPropertyValueAsString(SerializedProperty p) { return p.enumDisplayNames[p.enumValueIndex]; }


        /// Property setters
        private static void SetIntPropertyValue(SerializedProperty p, int v) { p.intValue = v; }
        private static void SetBoolPropertyValue(SerializedProperty p, bool v) { p.boolValue = v; }
        private static void SetFloatPropertyValue(SerializedProperty p, float v) { p.floatValue = v; }
        private static void SetDoublePropertyValue(SerializedProperty p, double v) { p.doubleValue = v; }
        private static void SetStringPropertyValue(SerializedProperty p, string v) { p.stringValue = v; }
        private static void SetColorPropertyValue(SerializedProperty p, Color v) { p.colorValue = v; }
        private static void SetObjectRefPropertyValue(SerializedProperty p, UnityEngine.Object v) { p.objectReferenceValue = v; }
        private static void SetLayerMaskPropertyValue(SerializedProperty p, int v) { p.intValue = v; }
        private static void SetVector2PropertyValue(SerializedProperty p, Vector2 v) { p.vector2Value = v; }
        private static void SetVector3PropertyValue(SerializedProperty p, Vector3 v) { p.vector3Value = v; }
        private static void SetVector4PropertyValue(SerializedProperty p, Vector4 v) { p.vector4Value = v; }
        private static void SetVector2IntPropertyValue(SerializedProperty p, Vector2Int v) { p.vector2IntValue = v; }
        private static void SetVector3IntPropertyValue(SerializedProperty p, Vector3Int v) { p.vector3IntValue = v; }
        private static void SetRectPropertyValue(SerializedProperty p, Rect v) { p.rectValue = v; }
        private static void SetRectIntPropertyValue(SerializedProperty p, RectInt v) { p.rectIntValue = v; }
        private static void SetAnimationCurvePropertyValue(SerializedProperty p, AnimationCurve v) { p.animationCurveValue = v; }
        private static void SetBoundsPropertyValue(SerializedProperty p, Bounds v) { p.boundsValue = v; }
        private static void SetBoundsIntPropertyValue(SerializedProperty p, BoundsInt v) { p.boundsIntValue = v; }
        private static void SetGradientPropertyValue(SerializedProperty p, Gradient v) {p.gradientValue = v; }
        private static void SetQuaternionPropertyValue(SerializedProperty p, Quaternion v) { p.quaternionValue = v; }
        private static void SetCharacterPropertyValue(SerializedProperty p, char v) { p.intValue = v; }

        // Conversions
        private static void SetDoublePropertyValueFromFloat(SerializedProperty p, float v) { p.doubleValue = v; }
        private static void SetFloatPropertyValueFromDouble(SerializedProperty p, double v) { p.floatValue = (float)v; }
        private static void SetCharacterPropertyValueFromString(SerializedProperty p, string v)
        {
            if (v.Length > 0)
            {
                p.intValue = v[0];
            }
        }

        //this one is a bit more tricky
        private static void SetEnumPropertyValueFromString(SerializedProperty p, string v) { p.enumValueIndex = FindStringIndex(p.enumDisplayNames, v); }

        // A No Linq implementation to avoid allocations
        private static int FindStringIndex(string[] values, string v)
        {
            for (var i = 0; i < values.Length; ++i)
            {
                if (values[i] == v)
                    return i;
            }

            return -1;
        }

        // Equality comparers
        internal static bool ValueEquals<TValue>(TValue value, SerializedProperty p, Func<SerializedProperty, TValue> propertyReadFunc)
        {
            var propVal = propertyReadFunc(p);
            return EqualityComparer<TValue>.Default.Equals(value, propVal);
        }

        internal static bool ValueEquals(string value, SerializedProperty p, Func<SerializedProperty, string> propertyReadFunc)
        {
            if (p.propertyType == SerializedPropertyType.Enum)
                return p.enumDisplayNames[p.enumValueIndex] == value;
            else
                return p.ValueEquals(value);
        }

        internal static bool ValueEquals(AnimationCurve value, SerializedProperty p, Func<SerializedProperty, AnimationCurve> propertyReadFunc)
        {
            return p.ValueEquals(value);
        }

        internal static bool ValueEquals(Gradient value, SerializedProperty p, Func<SerializedProperty, Gradient> propertyReadFunc)
        {
            return p.ValueEquals(value);
        }

        internal static bool SlowEnumValueEquals(string value, SerializedProperty p, Func<SerializedProperty, string> propertyReadFunc)
        {
            var propVal = propertyReadFunc(p);
            return EqualityComparer<string>.Default.Equals(value, propVal);
        }

        private static void DefaultBind<TValue>(VisualElement element, SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop,
            Func<SerializedProperty, TValue> propertyReadFunc, Action<SerializedProperty, TValue> propertyWriteFunc,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> valueComparerFunc)
        {
            var field = element as INotifyValueChanged<TValue>;

            if (field != null)
            {
                SerializedObjectBinding<TValue>.CreateBind(field, objWrapper, prop, propertyReadFunc,
                    propertyWriteFunc, valueComparerFunc);
            }
            else
            {
                Debug.LogWarning(string.Format("Field type {0} is not compatible with {2} property \"{1}\"",
                    element.GetType().FullName, prop.propertyPath, prop.type));
            }
        }

        private static void EnumBind(PopupField<string> popup, SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop)
        {
            SerializedEnumBinding.CreateBind(popup, objWrapper, prop);
        }

        private static void CreateBindingObjectForProperty(VisualElement element,  SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop)
        {
            if (element is Foldout)
            {
                var foldout = element as Foldout;
                SerializedObjectBinding<bool>.CreateBind(
                    foldout, objWrapper, prop,
                    p => p.isExpanded,
                    (p, v) => p.isExpanded = v,
                    ValueEquals<bool>);

                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    DefaultBind(element, objWrapper, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Boolean:
                    DefaultBind(element, objWrapper, prop, GetBoolPropertyValue, SetBoolPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Float:
                    if (prop.type == "float")
                    {
                        if (element is INotifyValueChanged<double>)
                        {
                            DefaultBind(element, objWrapper, prop, GetFloatPropertyValueAsDouble, SetFloatPropertyValueFromDouble, ValueEquals);
                        }
                        else
                        {
                            DefaultBind(element, objWrapper, prop, GetFloatPropertyValue, SetFloatPropertyValue, ValueEquals);
                        }
                    }
                    else
                    {
                        if (element is INotifyValueChanged<float>)
                        {
                            DefaultBind(element, objWrapper, prop, GetDoublePropertyValueAsFloat, SetDoublePropertyValueFromFloat, ValueEquals);
                        }
                        else
                        {
                            DefaultBind(element, objWrapper, prop, GetDoublePropertyValue, SetDoublePropertyValue, ValueEquals);
                        }
                    }

                    break;
                case SerializedPropertyType.String:
                    DefaultBind(element, objWrapper, prop, GetStringPropertyValue, SetStringPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Color:
                    DefaultBind(element, objWrapper, prop, GetColorPropertyValue, SetColorPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.ObjectReference:
                    DefaultBind(element, objWrapper, prop, GetObjectRefPropertyValue, SetObjectRefPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.LayerMask:
                    DefaultBind(element, objWrapper, prop, GetLayerMaskPropertyValue, SetLayerMaskPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Enum:
                    if (element is PopupField<string>)
                    {
                        EnumBind((PopupField<string>)element, objWrapper, prop);
                    }
                    else
                    {
                        DefaultBind(element, objWrapper, prop, GetEnumPropertyValueAsString, SetEnumPropertyValueFromString, SlowEnumValueEquals);
                    }

                    break;
                case SerializedPropertyType.Vector2:
                    DefaultBind(element, objWrapper, prop, GetVector2PropertyValue, SetVector2PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector3:
                    DefaultBind(element, objWrapper, prop, GetVector3PropertyValue, SetVector3PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector4:
                    DefaultBind(element, objWrapper, prop, GetVector4PropertyValue, SetVector4PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Rect:
                    DefaultBind(element, objWrapper, prop, GetRectPropertyValue, SetRectPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.ArraySize:
                    DefaultBind(element, objWrapper, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.AnimationCurve:
                    DefaultBind(element, objWrapper, prop, GetAnimationCurvePropertyValue, SetAnimationCurvePropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Bounds:
                    DefaultBind(element, objWrapper, prop, GetBoundsPropertyValue, SetBoundsPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Gradient:
                    DefaultBind(element, objWrapper, prop, GetGradientPropertyValue, SetGradientPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Quaternion:
                    DefaultBind(element, objWrapper, prop, GetQuaternionPropertyValue, SetQuaternionPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    DefaultBind(element, objWrapper, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector2Int:
                    DefaultBind(element, objWrapper, prop, GetVector2IntPropertyValue, SetVector2IntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector3Int:
                    DefaultBind(element, objWrapper, prop, GetVector3IntPropertyValue, SetVector3IntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.RectInt:
                    DefaultBind(element, objWrapper, prop, GetRectIntPropertyValue, SetRectIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.BoundsInt:
                    DefaultBind(element, objWrapper, prop, GetBoundsIntPropertyValue, SetBoundsIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Character:
                    if (element is INotifyValueChanged<string>)
                    {
                        DefaultBind(element, objWrapper, prop, GetCharacterPropertyValueAsString, SetCharacterPropertyValueFromString, ValueEquals);
                    }
                    else
                    {
                        DefaultBind(element, objWrapper, prop, GetCharacterPropertyValue, SetCharacterPropertyValue, ValueEquals);
                    }
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

        private class SerializedObjectUpdateWrapper
        {
            SerializedObjectChangeTracker tracker;
            public UInt64 LastRevision {get; private set; }
            public SerializedObject obj {get; private set; }

            public SerializedObjectUpdateWrapper(SerializedObject so)
            {
                tracker = new SerializedObjectChangeTracker(so);
                obj = so;
            }

            private bool wasUpdated {get; set; }

            public void UpdateRevision()
            {
                tracker.UpdateTrackedVersion();
                LastRevision = tracker.CurrentRevision;
            }

            public bool IsValid()
            {
                if (obj == null)
                    return false;

                return obj.isValid;
            }

            public void UpdateIfNecessary()
            {
                if (!wasUpdated)
                {
                    obj.UpdateIfRequiredOrScript();
                    obj.UpdateExpandedState();
                    UpdateRevision();
                    wasUpdated = true;
                }
            }

            public void ResetUpdate()
            {
                wasUpdated = false;
            }
        }

        private abstract class SerializedObjectBindingBase : IBinding
        {
            public SerializedObjectUpdateWrapper boundObject;
            public string boundPropertyPath;
            public SerializedProperty boundProperty;

            protected bool isReleased { get; set; }
            protected bool isUpdating { get; set; }
            public abstract void Update();
            public abstract void Release();

            public void PreUpdate()
            {
                boundObject.UpdateIfNecessary();
            }

            public virtual void ResetUpdate()
            {
                if (boundObject != null)
                {
                    boundObject.ResetUpdate();
                }
            }

            protected static void UpdateElementStyle(VisualElement element, SerializedProperty prop)
            {
                if (prop.serializedObject.targetObjects.Length == 1 && prop.isInstantiatedPrefab && prop.prefabOverride)
                    element.AddToClassList(BindingExtensions.k_PrefabOverrideClassName);
                else
                    element.RemoveFromClassList(BindingExtensions.k_PrefabOverrideClassName);
            }

            protected bool IsPropertyValid()
            {
                if (boundProperty != null)
                {
                    return boundProperty.isValid;
                }
                return false;
            }
        }

        private abstract class SerializedObjectBindingToBaseField<TValue, TField> : SerializedObjectBindingBase where TField : class, INotifyValueChanged<TValue>
        {
            private TField m_Field;

            protected TField field
            {
                get { return m_Field; }
                set
                {
                    m_Field?.RemoveOnValueChanged(FieldValueChanged);

                    VisualElement ve = m_Field as VisualElement;
                    if (ve != null)
                    {
                        ve.UnregisterCallback<AttachToPanelEvent>(OnFieldAttached);
                        ve.UnregisterCallback<DetachFromPanelEvent>(OnFieldDetached);
                    }

                    m_Field = value;
                    UpdateFieldIsAttached();
                    if (m_Field != null)
                    {
                        m_Field.OnValueChanged(FieldValueChanged);

                        ve = m_Field as VisualElement;
                        if (ve != null)
                        {
                            ve.RegisterCallback<AttachToPanelEvent>(OnFieldAttached);
                            ve.RegisterCallback<DetachFromPanelEvent>(OnFieldDetached);
                        }
                        FieldBinding = this;
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
                    var bindable = m_Field as IBindable;
                    if (bindable != null)
                    {
                        var previousBinding = bindable.binding;
                        bindable.binding = value;
                        if (previousBinding != this)
                        {
                            previousBinding?.Release();
                        }
                        (m_Field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);
                    }
                }
            }

            protected bool isFieldAttached {get; private set; }

            private void FieldValueChanged(ChangeEvent<TValue> evt)
            {
                if (isReleased || isUpdating)
                    return;

                var bindable = evt.target as IBindable;
                var binding = bindable?.binding;

                if (binding == this && boundProperty != null && boundObject.IsValid())
                {
                    if (!isFieldAttached)
                    {
                        //we don't update when field is not attached to a panel
                        //but we don't kill binding either
                        return;
                    }

                    UpdateLastFieldValue();

                    if (IsPropertyValid())
                    {
                        if (SyncFieldValueToProperty())
                        {
                            boundObject.UpdateRevision(); //we make sure to Poll the ChangeTracker here
                            boundObject.ResetUpdate();
                        }
                        UpdateElementStyle(field as VisualElement, boundProperty);
                        return;
                    }
                }

                // Something was wrong
                Release();
            }

            private UInt64 lastUpdatedRevision = 0xFFFFFFFFFFFFFFFF;

            public void ResetCachedValues()
            {
                lastUpdatedRevision = 0xFFFFFFFFFFFFFFFF;
                UpdateLastFieldValue();
                UpdateFieldIsAttached();
            }

            public override void Update()
            {
                if (isReleased)
                    return;
                try
                {
                    ResetUpdate();
                    isUpdating = true;

                    if (FieldBinding == this && boundObject.IsValid() && IsPropertyValid())
                    {
                        if (lastUpdatedRevision == boundObject.LastRevision)
                        {
                            //nothing to do
                            return;
                        }

                        lastUpdatedRevision = boundObject.LastRevision;
                        SyncPropertyToField(field, boundProperty);
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
                VisualElement ve = m_Field as VisualElement;

                if (ve != null)
                {
                    bool attached =  ve.panel != null;

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
                    if (!isFieldAttached)
                    {
                        isFieldAttached = true;
                        ResetCachedValues();
                    }
                }
            }

            // Read the value from the ui field and save it.
            protected abstract void UpdateLastFieldValue();

            protected abstract bool SyncFieldValueToProperty();
            protected abstract void SyncPropertyToField(TField c, SerializedProperty p);
        }

        private class SerializedObjectBinding<TValue> : SerializedObjectBindingToBaseField<TValue, INotifyValueChanged<TValue>>
        {
            private Func<SerializedProperty, TValue> propGetValue;
            private Action<SerializedProperty, TValue> propSetValue;
            private Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues;

            public static ObjectPool<SerializedObjectBinding<TValue>> s_Pool =
                new ObjectPool<SerializedObjectBinding<TValue>>(32);

            //we need to keep a copy of the last value since some fields will allocate when getting the value
            private TValue lastFieldValue;

            public static void CreateBind(INotifyValueChanged<TValue> field,
                SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property,
                Func<SerializedProperty, TValue> propGetValue,
                Action<SerializedProperty, TValue> propSetValue,
                Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
            {
                var newBinding = s_Pool.Get();
                newBinding.isReleased = false;
                newBinding.SetBinding(field, objWrapper, property, propGetValue, propSetValue, propCompareValues);
            }

            private void SetBinding(INotifyValueChanged<TValue> c,
                SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property,
                Func<SerializedProperty, TValue> getValue,
                Action<SerializedProperty, TValue> setValue,
                Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
            {
                this.field = c;
                property.unsafeMode = true;
                this.boundPropertyPath = property.propertyPath;
                this.boundObject = objWrapper;
                this.boundProperty = property;
                this.propGetValue = getValue;
                this.propSetValue = setValue;
                this.propCompareValues = compareValues;
                this.lastFieldValue = c.value;

                Update();
            }

            protected override void SyncPropertyToField(INotifyValueChanged<TValue> c, SerializedProperty p)
            {
                if (!propCompareValues(lastFieldValue, p, propGetValue))
                {
                    lastFieldValue = propGetValue(p);
                    c.value = lastFieldValue;
                }
            }

            protected override void UpdateLastFieldValue()
            {
                lastFieldValue = field.value;
            }

            protected override bool SyncFieldValueToProperty()
            {
                if (!propCompareValues(lastFieldValue, boundProperty, propGetValue))
                {
                    propSetValue(boundProperty, lastFieldValue);
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
                    FieldBinding = null;
                }

                boundObject = null;
                boundProperty = null;
                field = null;
                propGetValue = null;
                propSetValue = null;
                propCompareValues = null;
                lastFieldValue = default(TValue);
                isReleased = true;
                s_Pool.Release(this);
            }
        }

        // specific enum version that binds on the index property of the PopupField<string>
        private class SerializedEnumBinding : SerializedObjectBindingToBaseField<string, PopupField<string>>
        {
            public static ObjectPool<SerializedEnumBinding> s_Pool =
                new ObjectPool<SerializedEnumBinding>(32);

            //we need to keep a copy of the last value since some fields will allocate when getting the value
            private int lastFieldValueIndex;

            private List<string> originalChoices;
            private int originalIndex;

            public static void CreateBind(PopupField<string> field,  SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property)
            {
                var newBinding = s_Pool.Get();
                newBinding.isReleased = false;
                newBinding.SetBinding(field, objWrapper, property);
            }

            private void SetBinding(PopupField<string> c, SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property)
            {
                this.field = c;
                property.unsafeMode = true;
                this.boundPropertyPath = property.propertyPath;
                this.boundObject = objWrapper;
                this.boundProperty = property;
                this.originalChoices = field.choices;
                this.originalIndex = field.index;
                this.field.choices = property.enumLocalizedDisplayNames.ToList();
                this.lastFieldValueIndex = c.index;

                Update();
            }

            protected override void SyncPropertyToField(PopupField<string> c, SerializedProperty p)
            {
                int propValueIndex = p.enumValueIndex;
                if (propValueIndex != lastFieldValueIndex)
                {
                    lastFieldValueIndex = propValueIndex;
                    c.index = propValueIndex;
                }
            }

            protected override void UpdateLastFieldValue()
            {
                lastFieldValueIndex = field.index;
            }

            protected override bool SyncFieldValueToProperty()
            {
                if (lastFieldValueIndex != boundProperty.enumValueIndex)
                {
                    boundProperty.enumValueIndex = lastFieldValueIndex;
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

                boundObject = null;
                boundProperty = null;
                field = null;
                lastFieldValueIndex = -1;
                isReleased = true;
                s_Pool.Release(this);
            }
        }
    }
}
