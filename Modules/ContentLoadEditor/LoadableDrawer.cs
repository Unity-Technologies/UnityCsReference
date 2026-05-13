// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Loading;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(Loadable<>))]
    internal sealed class LoadableDrawer : PropertyDrawer
    {
        const string k_LoadableObjectIdPropertyName = "m_LoadableObjectId";
        static readonly Type[] k_LoadableObjectIdByRefCtorSignature = { typeof(LoadableObjectId).MakeByRefType() };

        // Override CreatePropertyGUI and OnGUI to support both UI tech.
        // If only CreatePropertyGUI is overriden the property drawer wouldn't work in an IMGUI context (CBD-841).
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var loadableObjectIdProp = property.FindPropertyRelative(k_LoadableObjectIdPropertyName);
            var loadableType = FindLoadableType(property);
            var loadableObjectIdField = new LoadableObjectIdField(preferredLabel, loadableType);

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
                        if (!TryAssignManagedReferenceLoadable(managedRefProp, loadableType, evt.newValue))
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
                        Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}"), e.InnerException?.Message ?? e.Message));
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
            var loadableType = FindLoadableType(property);

            // loadableObjectIdProp is null when the managed reference value is null ([SerializeReference] field).
            if (loadableObjectIdProp != null)
            {
                EditorGUI.BeginProperty(position, label, loadableObjectIdProp);
                EditorGUI.BeginChangeCheck();
                var newObj = LoadableObjectIdEditorUtility.DrawLoadableObjectIdField(position, loadableObjectIdProp, label, loadableType);

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
                var newObj = LoadableObjectIdEditorUtility.DrawLoadableObjectIdField(position, null, label, loadableType);
                if (EditorGUI.EndChangeCheck() && newObj != null)
                {
                    try
                    {
                        var newRef = LoadableObjectIdEditorUtility.CreateLoadableObjectId(newObj);
                        TryAssignManagedReferenceLoadable(property, loadableType, newRef);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}"), e.InnerException?.Message ?? e.Message));
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
        private static bool TryAssignManagedReferenceLoadable(SerializedProperty property, Type loadableType, LoadableObjectId id)
        {
            if (!id.IsValid)
            {
                Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableObjectId."));
                return false;
            }

            SetManagedReferenceLoadable(property, loadableType, id);
            property.serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static void SetManagedReferenceLoadable(SerializedProperty property, Type loadableType, LoadableObjectId id)
        {
            var concreteLoadableType = typeof(Loadable<>).MakeGenericType(loadableType);
            // Activator.CreateInstance cannot bind to constructors with in/ref/out
            // parameters. Loadable<T>'s constructor takes `in LoadableObjectId`, so
            // we must locate the constructor explicitly using MakeByRefType().
            var ctor = concreteLoadableType.GetConstructor(k_LoadableObjectIdByRefCtorSignature);
            if (ctor == null)
                throw new InvalidOperationException($"{concreteLoadableType} does not have a constructor that accepts LoadableObjectId.");
            property.managedReferenceValue = ctor.Invoke(new object[] { id });
        }

        private static Type FindLoadableType(SerializedProperty property)
        {
            // Try to find the generic type of the loadable field by walking up the property path
            FieldInfo field = null;
            var t = property.serializedObject.targetObject.GetType();
            var path = property.propertyPath.Split(".");

            // Find matching field
            var i = 0;
            while (i < path.Length)
            {
                field = t.GetField(path[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = field.FieldType;

                if (path.Length - i >= 2)
                {
                    if (path[i + 1] == "Array" && path[i + 2].StartsWith("data["))
                    {
                        t = t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];
                        i += 3;
                        continue;
                    }
                }

                ++i;
            }

            t = field.FieldType;

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Loadable<>))
                return t.GetGenericArguments()[0];

            if (t.IsArray && t.GetElementType().GetGenericTypeDefinition() == typeof(Loadable<>))
                return t.GetElementType().GetGenericArguments()[0];

            // Assume this is a list
            var genericType = t.GetGenericArguments()[0];
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(Loadable<>))
                return genericType.GetGenericArguments()[0];

            return typeof(UnityEngine.Object);
        }
    }
}
