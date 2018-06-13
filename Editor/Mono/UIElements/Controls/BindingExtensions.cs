// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.UIElements
{
    public static class BindingExtensions
    {
        public static void Bind(this VisualElement element, SerializedObject obj)
        {
            IBindable field = element as IBindable;

            if (field != null)
            {
                if (!string.IsNullOrEmpty(field.bindingPath))
                {
                    obj = field.BindProperty(obj);
                }
            }

            for (int i = 0; i < element.shadow.childCount; ++i)
            {
                Bind(element.shadow[i], obj);
            }
        }

        public static void Unbind(this VisualElement element)
        {
            RemoveBinding(element);

            for (int i = 0; i < element.shadow.childCount; ++i)
            {
                Unbind(element.shadow[i]);
            }
        }

        public static SerializedObject BindProperty(this IBindable field, SerializedObject obj)
        {
            var property = obj?.FindProperty(field.bindingPath);

            if (property != null)
            {
                obj = property.serializedObject;
                Bind(field as VisualElement, property);
                return obj;
            }

            //object is null or property was not found, we have to make sure we delete any previous binding
            RemoveBinding(field as VisualElement);
            return obj;
        }

        public static void BindProperty(this IBindable field, SerializedProperty property)
        {
            if (property != null)
            {
                field.bindingPath = property.propertyPath;
                Bind(field as VisualElement, property);
            }
        }

        internal static readonly string
            k_PrefabOverrideClassName = "prefab-override"; // visual element style changes wrt its property state


        private static void RemoveBinding(VisualElement element)
        {
            IBindable field = element as IBindable;

            SerializedObjectBindingBase info = field?.binding as SerializedObjectBindingBase;

            if (info != null)
            {
                info.Release();
            }
            else
            {
                if (field != null)
                    field.binding = null;
                element.IncrementVersion(VersionChangeType.Bindings);
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
        private static LayerMask GetLayerMaskPropertyValue(SerializedProperty p) {return p.intValue; }
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
        private static void SetLayerMaskPropertyValue(SerializedProperty p, LayerMask v) { p.intValue = v; }
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

        private static void DefaultBind<TValue>(VisualElement element, SerializedProperty prop,
            Func<SerializedProperty, TValue> propertyReadFunc, Action<SerializedProperty, TValue> propertyWriteFunc,
            Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> valueComparerFunc)
        {
            var field = element as INotifyValueChanged<TValue>;

            if (field != null)
            {
                SerializedObjectBinding<TValue>.CreateBind(field, prop, propertyReadFunc,
                    propertyWriteFunc, valueComparerFunc);
            }
            else
            {
                Debug.LogWarning(string.Format("Field type {0} is not compatible with {2} property \"{1}\"",
                        element.GetType().FullName, prop.propertyPath, prop.type));
            }
        }

        private static void EnumBind(PopupField<string> popup, SerializedProperty prop)
        {
            //TODO: set
            SerializedEnumBinding.CreateBind(popup, prop);
        }

        private static void Bind(VisualElement element, SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    DefaultBind(element, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Boolean:
                    DefaultBind(element, prop, GetBoolPropertyValue, SetBoolPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Float:
                    if (prop.type == "float")
                    {
                        if (element is INotifyValueChanged<double>)
                        {
                            DefaultBind(element, prop, GetFloatPropertyValueAsDouble, SetFloatPropertyValueFromDouble, ValueEquals);
                        }
                        else
                        {
                            DefaultBind(element, prop, GetFloatPropertyValue, SetFloatPropertyValue, ValueEquals);
                        }
                    }
                    else
                    {
                        if (element is INotifyValueChanged<float>)
                        {
                            DefaultBind(element, prop, GetDoublePropertyValueAsFloat, SetDoublePropertyValueFromFloat, ValueEquals);
                        }
                        else
                        {
                            DefaultBind(element, prop, GetDoublePropertyValue, SetDoublePropertyValue, ValueEquals);
                        }
                    }

                    break;
                case SerializedPropertyType.String:
                    DefaultBind(element, prop, GetStringPropertyValue, SetStringPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Color:
                    DefaultBind(element, prop, GetColorPropertyValue, SetColorPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.ObjectReference:
                    DefaultBind(element, prop, GetObjectRefPropertyValue, SetObjectRefPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.LayerMask:
                    DefaultBind(element, prop, GetLayerMaskPropertyValue, SetLayerMaskPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Enum:
                    if (element is PopupField<string>)
                    {
                        EnumBind((PopupField<string>)element, prop);
                    }
                    else
                    {
                        DefaultBind(element, prop, GetEnumPropertyValueAsString, SetEnumPropertyValueFromString, SlowEnumValueEquals);
                    }

                    break;
                case SerializedPropertyType.Vector2:
                    DefaultBind(element, prop, GetVector2PropertyValue, SetVector2PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector3:
                    DefaultBind(element, prop, GetVector3PropertyValue, SetVector3PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector4:
                    DefaultBind(element, prop, GetVector4PropertyValue, SetVector4PropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Rect:
                    DefaultBind(element, prop, GetRectPropertyValue, SetRectPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.ArraySize:
                    DefaultBind(element, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.AnimationCurve:
                    DefaultBind(element, prop, GetAnimationCurvePropertyValue, SetAnimationCurvePropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Bounds:
                    DefaultBind(element, prop, GetBoundsPropertyValue, SetBoundsPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Gradient:
                    DefaultBind(element, prop, GetGradientPropertyValue, SetGradientPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Quaternion:
                    DefaultBind(element, prop, GetQuaternionPropertyValue, SetQuaternionPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    DefaultBind(element, prop, GetIntPropertyValue, SetIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector2Int:
                    DefaultBind(element, prop, GetVector2IntPropertyValue, SetVector2IntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Vector3Int:
                    DefaultBind(element, prop, GetVector3IntPropertyValue, SetVector3IntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.RectInt:
                    DefaultBind(element, prop, GetRectIntPropertyValue, SetRectIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.BoundsInt:
                    DefaultBind(element, prop, GetBoundsIntPropertyValue, SetBoundsIntPropertyValue, ValueEquals);
                    break;
                case SerializedPropertyType.Character:
                    if (element is INotifyValueChanged<string>)
                    {
                        DefaultBind(element, prop, GetCharacterPropertyValueAsString, SetCharacterPropertyValueFromString, ValueEquals);
                    }
                    else
                    {
                        DefaultBind(element, prop, GetCharacterPropertyValue, SetCharacterPropertyValue, ValueEquals);
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

        private abstract class SerializedObjectBindingBase : IBinding
        {
            public SerializedProperty boundProperty;
            public abstract void Update();
            public abstract void Release();

            protected static void UpdateElementStyle(VisualElement element, SerializedProperty prop)
            {
                if (prop.serializedObject.targetObjects.Length == 1 && prop.isInstantiatedPrefab && prop.prefabOverride)
                    element.AddToClassList(BindingExtensions.k_PrefabOverrideClassName);
                else
                    element.RemoveFromClassList(BindingExtensions.k_PrefabOverrideClassName);
            }
        }

        private class SerializedObjectBinding<TValue> : SerializedObjectBindingBase
        {
            private INotifyValueChanged<TValue> field;

            private Func<SerializedProperty, TValue> propGetValue;
            private Action<SerializedProperty, TValue> propSetValue;
            private Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues;

            public static ObjectPool<SerializedObjectBinding<TValue>> s_Pool =
                new ObjectPool<SerializedObjectBinding<TValue>>(32);

            //we need to keep a copy of the last value since some fields will allocate when getting the value
            private TValue lastFieldValue;

            public static void CreateBind(INotifyValueChanged<TValue> field,
                SerializedProperty property,
                Func<SerializedProperty, TValue> propGetValue,
                Action<SerializedProperty, TValue> propSetValue,
                Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
            {
                IBindable bindable = field as IBindable;

                if (bindable?.binding != null)
                {
                    (bindable.binding as SerializedObjectBindingBase)?.Release();
                }

                var newBinding = s_Pool.Get();
                newBinding.SetBinding(field, property, propGetValue, propSetValue, propCompareValues);

                if (bindable != null)
                {
                    bindable.binding = newBinding;
                    (field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);
                }
            }

            private void SetBinding(INotifyValueChanged<TValue> c,
                SerializedProperty property,
                Func<SerializedProperty, TValue> getValue,
                Action<SerializedProperty, TValue> setValue,
                Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
            {
                this.field = c;
                this.boundProperty = property;
                this.propGetValue = getValue;
                this.propSetValue = setValue;
                this.propCompareValues = compareValues;
                this.lastFieldValue = c.value;
                field.OnValueChanged(OnValueChanged);

                Update();
            }

            private void SyncPropertyToField(INotifyValueChanged<TValue> c, SerializedProperty p)
            {
                if (!propCompareValues(lastFieldValue, p, propGetValue))
                {
                    lastFieldValue = propGetValue(p);
                    c.value = lastFieldValue;
                }
            }

            private static void OnValueChanged(ChangeEvent<TValue> evt)
            {
                var bindable = evt.target as IBindable;
                var binding = bindable?.binding as SerializedObjectBinding<TValue>;

                if (binding != null)
                {
                    binding.SyncFieldValueToProperty();
                }
            }

            private void SyncFieldValueToProperty()
            {
                if (boundProperty != null)
                {
                    boundProperty.m_SerializedObject.UpdateIfRequiredOrScript();

                    lastFieldValue = field.value;
                    if (!propCompareValues(lastFieldValue, boundProperty, propGetValue))
                    {
                        propSetValue(boundProperty, lastFieldValue);
                        boundProperty.m_SerializedObject.ApplyModifiedProperties();
                    }

                    UpdateElementStyle(field as VisualElement, boundProperty);
                }
            }

            public override void Update()
            {
                if (boundProperty != null)
                {
                    boundProperty.m_SerializedObject.UpdateIfRequiredOrScript();
                    SyncPropertyToField(field, boundProperty);
                }
                else
                {
                    //we unbind here
                    Release();
                }
            }

            public override void Release()
            {
                field.RemoveOnValueChanged(OnValueChanged);

                IBindable bindable = field as IBindable;

                if (bindable != null)
                {
                    if (bindable.binding == this)
                    {
                        bindable.binding = null;
                    }

                    (field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);

                    boundProperty = null;
                    field = null;
                    propGetValue = null;
                    propSetValue = null;
                    propCompareValues = null;
                    lastFieldValue = default(TValue);
                    s_Pool.Release(this);
                }
            }
        }

        // specific enum version that binds on the index property of the PopupField<string>
        private class SerializedEnumBinding : SerializedObjectBindingBase
        {
            private PopupField<string> field;

            public static ObjectPool<SerializedEnumBinding> s_Pool =
                new ObjectPool<SerializedEnumBinding>(32);

            //we need to keep a copy of the last value since some fields will allocate when getting the value
            private int lastFieldValueIndex;

            public static void CreateBind(PopupField<string> field,
                SerializedProperty property)
            {
                if (field?.binding != null)
                {
                    (field.binding as SerializedObjectBindingBase)?.Release();
                }

                var newBinding = s_Pool.Get();
                newBinding.SetBinding(field, property);

                if (field != null)
                {
                    field.binding = newBinding;
                    field.IncrementVersion(VersionChangeType.Bindings);
                }
            }

            private void SetBinding(PopupField<string> c,
                SerializedProperty property)
            {
                this.field = c;
                this.boundProperty = property;
                this.field.choices = property.enumLocalizedDisplayNames.ToList();
                this.lastFieldValueIndex = c.index;
                field.OnValueChanged(OnValueChanged);

                Update();
            }

            private void SyncPropertyToField(PopupField<string> c, SerializedProperty p)
            {
                int propValueIndex = p.enumValueIndex;
                if (propValueIndex != lastFieldValueIndex)
                {
                    lastFieldValueIndex = propValueIndex;
                    c.index = propValueIndex;
                }
            }

            private static void OnValueChanged(ChangeEvent<string> evt)
            {
                var bindable = evt.target as IBindable;
                var binding = bindable?.binding as SerializedEnumBinding;

                binding?.SyncFieldValueToProperty();
            }

            private void SyncFieldValueToProperty()
            {
                if (boundProperty != null)
                {
                    boundProperty.m_SerializedObject.UpdateIfRequiredOrScript();

                    lastFieldValueIndex = field.index;
                    if (lastFieldValueIndex != boundProperty.enumValueIndex)
                    {
                        boundProperty.enumValueIndex = lastFieldValueIndex;
                        boundProperty.m_SerializedObject.ApplyModifiedProperties();
                    }

                    UpdateElementStyle(field as VisualElement, boundProperty);
                }
            }

            public override void Update()
            {
                if (boundProperty != null)
                {
                    boundProperty.m_SerializedObject.UpdateIfRequiredOrScript();
                    SyncPropertyToField(field, boundProperty);
                }
                else
                {
                    //we unbind here
                    Release();
                }
            }

            public override void Release()
            {
                field.RemoveOnValueChanged(OnValueChanged);

                IBindable bindable = field as IBindable;

                if (bindable == null)
                    return;

                if (bindable.binding == this)
                {
                    bindable.binding = null;
                }

                (field as VisualElement)?.IncrementVersion(VersionChangeType.Bindings);

                boundProperty = null;
                field = null;
                lastFieldValueIndex = -1;
                s_Pool.Release(this);
            }
        }
    }
}
