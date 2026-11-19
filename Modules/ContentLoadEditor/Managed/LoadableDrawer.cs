// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Loading;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(Loadable<>))]
    internal sealed class LoadableDrawer : PropertyDrawer
    {
        const string k_LoadableObjectIdPropertyName = "m_LoadableObjectId";
        [NoAutoStaticsCleanup] // immutable reflection ctor-signature array of framework types (LoadableObjectId by-ref); no user-code refs, safe to persist
        static readonly Type[] k_LoadableObjectIdByRefCtorSignature = { typeof(LoadableObjectId).MakeByRefType() };

        // m_LoadableObjectType is derived from the PropertyDrawer's fieldInfo, cached for performance.
        Type m_LoadableObjectType;

        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableObjectIdProp = property.FindPropertyRelative(k_LoadableObjectIdPropertyName);
            m_LoadableObjectType ??= FindLoadableObjectType(fieldInfo);
            var loadableObjectIdField = new LoadableObjectIdField(preferredLabel, m_LoadableObjectType);

            // loadableObjectIdProp is null when the managed reference value is null ([SerializeReference] field).
            // Binding is skipped in that case; PropertyField will re-invoke the drawer once a value is assigned.
            if (loadableObjectIdProp != null)
            {
                loadableObjectIdField.BindProperty(loadableObjectIdProp);
            }
            else if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // The managed reference is null so there is no sub-property to bind to.
                // When the user assigns a value, create the Loadable<T> instance, write it
                // as the managed reference value, and bind the field to the new sub-property.
                var managedRefProp = property.Copy();
                loadableObjectIdField.RegisterValueChangedCallback(evt =>
                {
                    try
                    {
                        if (!TryAssignManagedReferenceLoadable(managedRefProp, m_LoadableObjectType, evt.newValue))
                        {
                            loadableObjectIdField.SetValueWithoutNotify(evt.previousValue);
                        }
                        else
                        {
                            var loadableObjectIdProp = managedRefProp.FindPropertyRelative(k_LoadableObjectIdPropertyName);
                            if (loadableObjectIdProp != null)
                                loadableObjectIdField.BindProperty(loadableObjectIdProp);
                        }
                    }
                    catch (Exception e)
                    {
                        loadableObjectIdField.SetValueWithoutNotify(evt.previousValue);
                        Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}", null), e.InnerException?.Message ?? e.Message));
                    }
                });
            }

            loadableObjectIdField.TrackPropertyValue(property, prop =>
            {
                var subProp = prop.FindPropertyRelative(k_LoadableObjectIdPropertyName);
                if (subProp != null)
                    loadableObjectIdField.BindProperty(subProp);
            });

            PropertyField.ConfigureFieldStyles<LoadableObjectIdField, LoadableObjectId>(loadableObjectIdField);

            return loadableObjectIdField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var loadableObjectIdProp = property.FindPropertyRelative(k_LoadableObjectIdPropertyName);
            m_LoadableObjectType ??= FindLoadableObjectType(fieldInfo);

            // loadableObjectIdProp is null when the managed reference value is null ([SerializeReference] field).
            if (loadableObjectIdProp != null)
            {
                EditorGUI.BeginProperty(position, label, loadableObjectIdProp);
                EditorGUI.BeginChangeCheck();
                var newObj = LoadableObjectIdEditorUtility.DrawLoadableObjectIdField(position, loadableObjectIdProp, label, m_LoadableObjectType);

                if (EditorGUI.EndChangeCheck())
                    LoadableObjectIdEditorUtility.ApplyLoadableObjectIdChange(loadableObjectIdProp, newObj);
                EditorGUI.EndProperty();
            }
            else if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Managed reference is null — draw a loadable object field (with striped
                // background) so the user can make an initial selection and instantiate
                // the Loadable<T>.
                label = EditorGUI.BeginProperty(position, label, property);
                EditorGUI.BeginChangeCheck();
                var newObj = LoadableObjectIdEditorUtility.DrawLoadableObjectIdField(position, null, label, m_LoadableObjectType);
                if (EditorGUI.EndChangeCheck() && newObj != null)
                {
                    try
                    {
                        var newRef = LoadableObjectIdEditorUtility.CreateLoadableObjectId(newObj);
                        TryAssignManagedReferenceLoadable(property, m_LoadableObjectType, newRef);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}", null), e.InnerException?.Message ?? e.Message));
                    }
                }
                EditorGUI.EndProperty();
            }
        }

        /// <summary>
        /// Validates the given LoadableObjectId, creates a Loadable&lt;T&gt; instance,
        /// writes it as the managed reference value, and applies modified properties.
        /// </summary>
        /// <returns>True if the assignment succeeded, false if the id was invalid.</returns>
        private static bool TryAssignManagedReferenceLoadable(SerializedProperty property, Type loadableObjectType, LoadableObjectId id)
        {
            if (!id.IsValid)
            {
                Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableObjectId.", null));
                return false;
            }

            SetManagedReferenceLoadable(property, loadableObjectType, id);
            property.serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static void SetManagedReferenceLoadable(SerializedProperty property, Type loadableObjectType, LoadableObjectId id)
        {
            var concreteLoadableType = typeof(Loadable<>).MakeGenericType(loadableObjectType);
            // Activator.CreateInstance cannot bind to constructors with in/ref/out
            // parameters. Loadable<T>'s constructor takes `in LoadableObjectId`, so
            // we must locate the constructor explicitly using MakeByRefType().
            var ctor = concreteLoadableType.GetConstructor(k_LoadableObjectIdByRefCtorSignature);
            if (ctor == null)
                throw new InvalidOperationException($"{concreteLoadableType} does not have a constructor that accepts LoadableObjectId.");
            property.managedReferenceValue = ctor.Invoke(new object[] { id });
        }

        private static bool IsLoadableType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Loadable<>);
        }

        // We need to step into array and list types until the Loadable<T> is reached. 'fieldInfo' is the
        // innermost _named_ field on the property path, so a dictionary entry is already the
        // Loadable<T> (".value" is a named field), while an array or list element is not.
        // Example table:
        //   Declared field 'f'                 Property path                         fieldInfo.FieldType
        //   --------------------------------   -----------------------------------   -------------------
        //   Loadable<T>                        f                                     Loadable<T>
        //   Loadable<T>[]                      f.Array.data[0]                       Loadable<T>[]
        //   List<Loadable<T>>                  f.Array.data[0]                       List<Loadable<T>>
        //   Dictionary<K, Loadable<T>>         f.Array.data[0].value                 Loadable<T>
        //   Dictionary<K, List<Loadable<T>>>   f.Array.data[0].value.Array.data[1]   List<Loadable<T>>
        private static Type FindLoadableObjectType(FieldInfo fieldInfo)
        {
            var type = fieldInfo?.FieldType;

            while (type != null && !IsLoadableType(type))
            {
                if (type.IsArray)
                    type = type.GetElementType();
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    type = type.GetGenericArguments()[0];
                else
                    break;
            }

            return IsLoadableType(type) ? type.GetGenericArguments()[0] : typeof(UnityEngine.Object);
        }
    }
}
