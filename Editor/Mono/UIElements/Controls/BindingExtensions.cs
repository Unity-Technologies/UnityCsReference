// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class DefaultSerializedObjectBindingImplementation : ISerializedObjectBindingImplementation
    {
        public void Bind(VisualElement element, SerializedObject obj)
        {
            Bind(element, new SerializedObjectUpdateWrapper(obj), null);
        }

        public void Unbind(VisualElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            RemoveBinding(element as IBindable);

            var childCount = element.hierarchy.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                Unbind(element.hierarchy[i]);
            }
        }

        public SerializedProperty BindProperty(IBindable field, SerializedObject obj)
        {
            var property = obj?.FindProperty(field.bindingPath);

            if (property != null)
            {
                Bind(field as VisualElement, new SerializedObjectUpdateWrapper(obj), null);
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
            Bind(field as VisualElement, new SerializedObjectUpdateWrapper(property.serializedObject), null);
        }

        // visual element style changes wrt its property state
        private static void DoBindProperty(IBindable field, SerializedObjectUpdateWrapper obj, SerializedProperty property)
        {
            var fieldElement = field as VisualElement;
            if (property == null || fieldElement == null)
            {
                // Object is null or property was not found, we have nothing to do here
                return;
            }

            //we first remove any binding that were already present
            RemoveBinding(field);

            // This covers the case where a field is being manually bound to a property
            field.bindingPath = property.propertyPath;

            using (var evt = SerializedPropertyBindEvent.GetPooled(property))
            {
                if (SendBindingEvent(evt, fieldElement))
                {
                    return;
                }
            }

            CreateBindingObjectForProperty(fieldElement, obj, property);
        }

        void ISerializedObjectBindingImplementation.Bind(VisualElement element, object objWrapper, SerializedProperty parentProperty)
        {
            Bind(element, objWrapper as SerializedObjectUpdateWrapper, parentProperty);
        }

        private void Bind(VisualElement element, SerializedObjectUpdateWrapper objWrapper, SerializedProperty parentProperty)
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

            var childCount = element.hierarchy.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                Bind(element.hierarchy[i], objWrapper, parentProperty);
            }
        }

        private SerializedProperty BindPropertyWithParent(IBindable field, SerializedObjectUpdateWrapper objWrapper, SerializedProperty parentProperty)
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
            target.HandleEventAtTargetPhase(evt);
            return evt.isPropagationStopped;
        }

        private static void RemoveBinding(IBindable bindable)
        {
            if (bindable == null || !bindable.IsBound())
            {
                return;
            }

            bindable.binding?.Release();
            bindable.binding = null;
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
        private static float GetIntPropertyValueAsFloat(SerializedProperty p) { return p.intValue; }

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

            if (element is INotifyValueChanged<string> && typeof(TValue) != typeof(string))
            {
                //One Way Binding here with string conversions

                SerializedObjectStringConversionBinding<TValue>.CreateBind(element as INotifyValueChanged<string>, objWrapper, prop, propertyReadFunc,
                    propertyWriteFunc, valueComparerFunc);
            }
            else if (field != null)
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

        public void HandleStyleUpdate(VisualElement element)
        {
            var bindable = element as IBindable;
            var binding = bindable?.binding as SerializedObjectBindingBase;
            if (binding == null || binding.boundProperty == null)
                return;

            SerializedObjectBindingBase.UpdateElementStyle(element, binding.boundProperty);
        }

        private static void CreateEnumBindingObject(VisualElement element, SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop)
        {
            if (element is PopupField<string>)
            {
                SerializedDefaultEnumBinding.CreateBind((PopupField<string>)element, objWrapper, prop);
            }
            else if (element is EnumFlagsField || element is EnumField)
            {
                SerializedManagedEnumBinding.CreateBind((BaseField<Enum>)element, objWrapper, prop);
            }
            else if (element is INotifyValueChanged<int>)
            {
                DefaultBind(element, objWrapper, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
            }
            else
            {
                DefaultBind(element, objWrapper, prop, GetEnumPropertyValueAsString, SetEnumPropertyValueFromString, SlowEnumValueEquals);
            }
        }

        private static bool BindListView(ListView listView, SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop)
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
                ListViewSerializedObjectBinding.CreateBind(listView, objWrapper, prop);

                return true;
            }
            else
            {
                Debug.LogWarning(string.Format("Binding ListView is not supported for {0} properties \"{1}\"", prop.type,
                    prop.propertyPath));
            }
            return false;
        }

        private static void CreateBindingObjectForProperty(VisualElement element, SerializedObjectUpdateWrapper objWrapper, SerializedProperty prop)
        {
            // A bound Foldout (a PropertyField with child properties) is special.
            if (element is Foldout)
            {
                // We bind to the given propertyPath but we only bind to its 'isExpanded' state, not its value.
                var foldout = element as Foldout;
                SerializedObjectBinding<bool>.CreateBind(
                    foldout, objWrapper, prop,
                    p => p.isExpanded,
                    (p, v) => p.isExpanded = v,
                    ValueEquals<bool>);

                return;
            }
            else if (element is Label && element.GetProperty(PropertyField.foldoutTitleBoundLabelProperty) != null)
            {
                // We bind to the given propertyPath but we only bind to its 'localizedDisplayName' state, not its value.
                // This is a feature from IMGUI where the title of a Foldout will change if one of the child
                // properties is named "Name" and its value changes.
                var label = element as Label;
                SerializedObjectBinding<string>.CreateBind(
                    label, objWrapper, prop,
                    p => p.localizedDisplayName,
                    (p, v) => {},
                    ValueEquals<string>);

                return;
            }
            if (element is ListView)
            {
                BindListView(element as ListView, objWrapper, prop);
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (element is INotifyValueChanged<int> || element is  INotifyValueChanged<string>)
                    {
                        DefaultBind(element, objWrapper, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    }
                    else if (element is INotifyValueChanged<float>)
                    {
                        DefaultBind(element, objWrapper, prop, GetIntPropertyValueAsFloat, SetFloatPropertyValue, ValueEquals);
                    }

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
                    CreateEnumBindingObject(element, objWrapper, prop);
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

        internal class SerializedObjectUpdateWrapper
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
                if (obj == null || obj.m_NativeObjectPtr == IntPtr.Zero)
                    return false;

                return obj.isValid;
            }

            public void UpdateIfNecessary()
            {
                if (!wasUpdated)
                {
                    if (IsValid())
                    {
                        obj.UpdateIfRequiredOrScript();
                        obj.UpdateExpandedState();
                        UpdateRevision();
                    }

                    wasUpdated = true;
                }
            }

            public void ResetUpdate()
            {
                wasUpdated = false;
            }
        }

        internal abstract class SerializedObjectBindingBase : IBinding
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
                if (boundObject != null)
                {
                    boundObject.UpdateIfNecessary();
                }
            }

            public virtual void ResetUpdate()
            {
                if (boundObject != null)
                {
                    boundObject.ResetUpdate();
                }
            }

            private static InspectorElement FindPrefabOverrideBarCompatibleParent(VisualElement field)
            {
                // For now we only support these blue prefab override bars within an InspectorElement.
                return field.GetFirstAncestorOfType<InspectorElement>();
            }

            private static void UpdatePrefabOverrideBarStyle(VisualElement blueBar)
            {
                var element = blueBar.userData as VisualElement;

                var container = FindPrefabOverrideBarCompatibleParent(element);
                if (container == null)
                    return;

                // Move the bar to where the control is in the container.
                var top = element.worldBound.y - container.worldBound.y;
                if (float.IsNaN(top)) // If this is run before the container has been layed out.
                    return;

                var elementHeight = element.resolvedStyle.height;

                // This is needed so if you have 2 overridden fields their blue
                // bars touch (and it looks like one long bar). They normally wouldn't
                // because most fields have a small margin.
                var bottomOffset = element.resolvedStyle.marginBottom;

                blueBar.style.top = top;
                blueBar.style.height = elementHeight + bottomOffset;
                blueBar.style.left = 0.0f;
            }

            private static void UpdatePrefabOverrideBarStyleEvent(GeometryChangedEvent evt)
            {
                var container = evt.target as InspectorElement;
                if (container == null)
                    return;

                var barContainer = container.Q(BindingExtensions.prefabOverrideBarContainerName);
                if (barContainer == null)
                    return;

                foreach (var bar in barContainer.Children())
                    UpdatePrefabOverrideBarStyle(bar);
            }

            internal static void UpdateElementStyle(VisualElement element, SerializedProperty prop)
            {
                if (element == null)
                    return;

                if (element is Foldout)
                {
                    // We only want to apply override styles onto the Foldout header, not the entire contents.
                    element = element.Q(className: Foldout.toggleUssClassName);
                }
                else if (element.ClassListContains(BaseCompositeField<int, IntegerField, int>.ussClassName)
                         || element is BoundsField || element is BoundsIntField)
                {
                    // The problem with compound fields is that they are bound at the parent level using
                    // their parent value data type. For example, a Vector3Field is bound to the parent
                    // SerializedProperty which uses the Vector3 data type. However, animation overrides
                    // are not stored on the parent SerializedProperty but on the component child
                    // SerializedProperties. So even though we're bound to the parent property, we still
                    // have to dive inside and example the child SerializedProperties (ie. x, y, z, height)
                    // and override the animation styles individually.

                    var compositeField = element;

                    // The element we style in the main pass is going to be just the label.
                    element = element.Q(className: BaseField<int>.labelUssClassName);

                    // Go through the inputs and find any that match the names of the child PropertyFields.
                    var propCopy = prop.Copy();
                    var endProperty = propCopy.GetEndProperty();
                    propCopy.NextVisible(true); // Expand the first child.
                    do
                    {
                        if (SerializedProperty.EqualContents(propCopy, endProperty))
                            break;

                        var subInputName = "unity-" + propCopy.name + "-input";
                        var subInput = compositeField.Q(subInputName);
                        if (subInput == null)
                            continue;

                        UpdateElementStyle(subInput, propCopy);
                    }
                    while (propCopy.NextVisible(false)); // Never expand children.
                }

                // It's possible for there to be no label in a compound field, for example. So, nothing to style.
                if (element == null)
                    return;

                bool handlePrefabState = false;

                try
                {
                    // This can throw if the serialized object changes type under our feet
                    handlePrefabState = prop.serializedObject.targetObjects.Length == 1 &&
                        prop.isInstantiatedPrefab &&
                        prop.prefabOverride;
                }
                catch (Exception)
                {
                    return;
                }

                // Handle prefab state.
                if (handlePrefabState)
                {
                    if (!element.ClassListContains(BindingExtensions.prefabOverrideUssClassName))
                    {
                        var container = FindPrefabOverrideBarCompatibleParent(element);
                        var barContainer = container?.prefabOverrideBlueBarsContainer;

                        element.AddToClassList(BindingExtensions.prefabOverrideUssClassName);

                        if (container != null && barContainer != null)
                        {
                            // Ideally, this blue bar would be a child of the field and just move
                            // outside the field in absolute offsets to hug the side of the field's
                            // container. However, right now we need to have overflow:hidden on
                            // fields because of case 1105567 (the inputs can grow beyond the field).
                            // Therefore, we have to add the blue bars as children of the container
                            // and move them down beside their respective field.

                            var prefabOverrideBar = new VisualElement();
                            prefabOverrideBar.name = BindingExtensions.prefabOverrideBarName;
                            prefabOverrideBar.userData = element;
                            prefabOverrideBar.AddToClassList(BindingExtensions.prefabOverrideBarUssClassName);
                            barContainer.Add(prefabOverrideBar);

                            element.SetProperty(BindingExtensions.prefabOverrideBarName, prefabOverrideBar);

                            // We need to try and set the bar style right away, even if the container
                            // didn't compute its layout yet. This is for when the override is done after
                            // everything has been layed out.
                            UpdatePrefabOverrideBarStyle(prefabOverrideBar);

                            // We intentionally re-register this event on the container per element and
                            // never unregister.
                            container.RegisterCallback<GeometryChangedEvent>(UpdatePrefabOverrideBarStyleEvent);
                        }
                    }
                }
                else if (element.ClassListContains(BindingExtensions.prefabOverrideUssClassName))
                {
                    element.RemoveFromClassList(BindingExtensions.prefabOverrideUssClassName);

                    var container = FindPrefabOverrideBarCompatibleParent(element);
                    var barContainer = container?.prefabOverrideBlueBarsContainer;

                    if (container != null && barContainer != null)
                    {
                        var prefabOverrideBar = element.GetProperty(BindingExtensions.prefabOverrideBarName) as VisualElement;
                        if (prefabOverrideBar != null)
                            prefabOverrideBar.RemoveFromHierarchy();
                    }
                }

                // Handle animated state.

                // Since we handle compound fields above, the element here will always be a single field
                // (or not a field at all). This means we can perform a faster query and search for
                // a single element.
                var inputElement = element.Q(className: BaseField<int>.inputUssClassName);
                if (inputElement == null)
                {
                    return;
                }

                bool animated = AnimationMode.IsPropertyAnimated(prop.serializedObject.targetObject, prop.propertyPath);
                bool candidate = AnimationMode.IsPropertyCandidate(prop.serializedObject.targetObject, prop.propertyPath);
                bool recording = AnimationMode.InAnimationRecording();

                inputElement.EnableInClassList(BindingExtensions.animationRecordedUssClassName, animated && recording);
                inputElement.EnableInClassList(BindingExtensions.animationCandidateUssClassName, animated && !recording && candidate);
                inputElement.EnableInClassList(BindingExtensions.animationAnimatedUssClassName, animated && !recording && !candidate);
            }

            protected bool IsPropertyValid()
            {
                if (boundProperty != null)
                {
                    return boundProperty.isValid;
                }
                return false;
            }

            protected IBindable m_Field;

            protected IBindable boundElement
            {
                get { return m_Field; }
                set
                {
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

            protected bool isFieldAttached { get; private set; }

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
                }
            }

            protected abstract void ResetCachedValues();
        }

        private abstract class SerializedObjectBindingToBaseField<TValue, TField> : SerializedObjectBindingBase where TField : class, INotifyValueChanged<TValue>
        {
            protected TField field
            {
                get { return m_Field as TField; }
                set
                {
                    field?.UnregisterValueChangedCallback(FieldValueChanged);
                    boundElement = value as IBindable;

                    if (field != null)
                    {
                        field.RegisterValueChangedCallback(FieldValueChanged);
                    }
                }
            }


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

            protected override void ResetCachedValues()
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
                        UpdateElementStyle(field as VisualElement, boundProperty);
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

            // Read the value from the ui field and save it.
            protected abstract void UpdateLastFieldValue();

            protected abstract bool SyncFieldValueToProperty();
            protected abstract void SyncPropertyToField(TField c, SerializedProperty p);
        }

        private abstract class SerializedObjectBindingPropertyToBaseField<TProperty, TValue> : SerializedObjectBindingToBaseField<TValue, INotifyValueChanged<TValue>>
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

                if (!propCompareValues(lastFieldValue, p, propGetValue))
                {
                    lastFieldValue = propGetValue(p);
                    AssignValueToField(lastFieldValue);
                }
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

            protected abstract void AssignValueToField(TProperty lastValue);

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
                ResetCachedValues();
                isReleased = true;
            }
        }
        private class SerializedObjectBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, TValue>
        {
            public static ObjectPool<SerializedObjectBinding<TValue>> s_Pool =
                new ObjectPool<SerializedObjectBinding<TValue>>(32);

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

                field.value = lastValue;
            }
        }
        // specific enum version that binds on the index property of the PopupField<string>
        private class SerializedManagedEnumBinding : SerializedObjectBindingToBaseField<Enum, BaseField<Enum>>
        {
            public static ObjectPool<SerializedManagedEnumBinding> s_Pool =
                new ObjectPool<SerializedManagedEnumBinding>(32);

            //we need to keep a copy of the last value since some fields will allocate when getting the value
            private int lastEnumValue;
            private Type managedType;

            public static void CreateBind(BaseField<Enum> field,  SerializedObjectUpdateWrapper objWrapper,
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
                newBinding.SetBinding(field, objWrapper, property, managedType);
            }

            private void SetBinding(BaseField<Enum> c, SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property, Type manageType)
            {
                this.managedType = manageType;
                property.unsafeMode = true;
                this.boundPropertyPath = property.propertyPath;
                this.boundObject = objWrapper;
                this.boundProperty = property;

                int enumValueAsInt = property.intValue;

                Enum value = GetEnumFromSerializedFromInt(manageType, enumValueAsInt);

                if (c is EnumField)
                    (c as EnumField).Init(value);
                else if (c is EnumFlagsField)
                    (c as EnumFlagsField).Init(value);
                else
                {
                    throw new InvalidOperationException(c.GetType() + " cannot be bound to a enum");
                }

                lastEnumValue = enumValueAsInt;
                c.value = value;

                // Make sure to write this property only after setting a first value into the field
                // This avoid any null checks in regular update methods
                this.field = c;

                Update();
            }

            static Enum GetEnumFromSerializedFromInt(Type managedType, int enumValueAsInt)
            {
                var enumData = EnumDataUtility.GetCachedEnumData(managedType);

                if (enumData.flags)
                    return EnumDataUtility.IntToEnumFlags(managedType, enumValueAsInt);
                else
                {
                    int valueIndex = Array.IndexOf(enumData.flagValues, enumValueAsInt);

                    if (valueIndex != -1)
                        return enumData.values[valueIndex];
                    else
                    {
                        Debug.LogWarning("Error: invalid enum value " + enumValueAsInt + " for type " + managedType);
                        return null;
                    }
                }
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
                if (enumValueAsInt != lastEnumValue)
                {
                    field.value = GetEnumFromSerializedFromInt(managedType, enumValueAsInt);
                    lastEnumValue = enumValueAsInt;
                }
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
                if (lastEnumValue != boundProperty.intValue)
                {
                    boundProperty.intValue = lastEnumValue;
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
                    // Make sure to nullify the field to unbind before reverting the enum value
                    var saveField = field;
                    field = null;
                    saveField.value = null;
                    FieldBinding = null;
                }

                boundObject = null;
                boundProperty = null;
                field = null;
                managedType = null;
                isReleased = true;

                ResetCachedValues();
                s_Pool.Release(this);
            }
        }

        // specific enum version that binds on the index property of the PopupField<string>
        private class SerializedDefaultEnumBinding : SerializedObjectBindingToBaseField<string, PopupField<string>>
        {
            public static ObjectPool<SerializedDefaultEnumBinding> s_Pool =
                new ObjectPool<SerializedDefaultEnumBinding>(32);

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
                if (p == null)
                {
                    throw new ArgumentNullException(nameof(p));
                }
                if (c == null)
                {
                    throw new ArgumentNullException(nameof(c));
                }

                int propValueIndex = p.enumValueIndex;
                if (propValueIndex != lastFieldValueIndex)
                {
                    lastFieldValueIndex = propValueIndex;
                    c.index = propValueIndex;
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

                ResetCachedValues();
                s_Pool.Release(this);
            }
        }


        //One-way binding
        private class SerializedObjectStringConversionBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, string>
        {
            public static ObjectPool<SerializedObjectStringConversionBinding<TValue>> s_Pool =
                new ObjectPool<SerializedObjectStringConversionBinding<TValue>>(32);

            public static void CreateBind(INotifyValueChanged<string> field,
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

            private void SetBinding(INotifyValueChanged<string> c,
                SerializedObjectUpdateWrapper objWrapper,
                SerializedProperty property,
                Func<SerializedProperty, TValue> getValue,
                Action<SerializedProperty, TValue> setValue,
                Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
            {
                property.unsafeMode = true;
                this.boundPropertyPath = property.propertyPath;
                this.boundObject = objWrapper;
                this.boundProperty = property;
                this.propGetValue = getValue;
                this.propSetValue = setValue;
                this.propCompareValues = compareValues;
                this.field = c;

                // In this subclass implementation the lastFieldValue is in fact the propertyValue assigned to the field.
                // this is made to compare TValues instead of strings
                UpdateLastFieldValue();
                AssignValueToField(lastFieldValue);
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
}
