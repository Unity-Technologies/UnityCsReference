// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// Builds the UI Toolkit fields for a <see cref="PhysicsUserData"/> serialized property.
    /// The entity id is presented as the object it references, alongside the mask, float, int, long, Vector3Int, and bool slots.
    /// Add the returned element directly to embed the fields inline, or wrap it in a <see cref="Foldout"/> for a collapsible section.
    /// </summary>
    public static class PhysicsUserDataInspector
    {
        /// <summary>
        /// Create the editable fields for a <see cref="PhysicsUserData"/> property, with no wrapping foldout.
        /// </summary>
        /// <param name="property">The serialized <see cref="PhysicsUserData"/> property to build the fields for.</param>
        /// <returns>A container with the referenced object field and the mask, float, int, long, Vector3Int, and bool fields.</returns>
        public static VisualElement CreateFields(SerializedProperty property)
        {
            var body = new VisualElement();

            // The entity id is shown as the object it references, so the field is linked to the property rather than bound to it.
            var objectField = new ObjectField(objectLabel);
            objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            SerializedPropertyProxy.Link(
                objectField,
                property.FindPropertyRelative(nameof(PhysicsUserData.m_EntityId)),
                ReadObject,
                WriteObject,
                entityIdProperty => objectField.tooltip = GetEntityTooltip(entityIdProperty.entityIdValue));
            body.Add(objectField);

            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_PhysicsMask))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_Float))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_Int))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_Int64))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_Vector3Int))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PhysicsUserData.m_Bool))));

            return body;
        }

        // Read the object the stored entity id references.
        internal static UnityEngine.Object ReadObject(SerializedProperty entityIdProperty) => PhysicsGlobal_GetObject(entityIdProperty.entityIdValue);

        // Store the entity id of the object, or no entity id when there is no object.
        internal static void WriteObject(SerializedProperty entityIdProperty, UnityEngine.Object obj) => entityIdProperty.entityIdValue = obj != null ? obj.GetEntityId() : EntityId.None;

        static string GetEntityTooltip(EntityId entityId)
        {
            if (entityId == EntityId.None)
                return "None";

            var obj = PhysicsGlobal_GetObject(entityId);
            if (obj == null)
                return "Invalid EntityId";

            return $"EntityId: {entityId.ToString()} - \"{obj.name}\" ({obj.GetType()})";
        }

        internal const string objectLabel = "Object";
    }

    [CustomPropertyDrawer(typeof(PhysicsUserData))]
    sealed class PhysicsUserDataPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // The foldout is bound to the property so its expanded state is the property's own, as for any struct in an inspector.
            var foldout = new Foldout { text = property.displayName, value = false, bindingPath = property.propertyPath };
            foldout.Add(PhysicsUserDataInspector.CreateFields(property));
            return foldout;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            // The object row, then one row per remaining field at that field's own height (a Vector3Int takes two lines in a narrow inspector).
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            foreach (var fieldName in k_FieldNames)
            {
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(fieldName), false);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // The user data's property scope covers only its header, so the override bar and context menu of each field below are that field's own.
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            label = EditorGUI.BeginProperty(foldoutRect, label, property);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            EditorGUI.EndProperty();

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                // The object row has its own property scope so its mixed value, prefab override and context menu are the entity id's alone, not the whole user data's.
                var entityIdProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_EntityId));
                var objectRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                var objectLabel = EditorGUI.BeginProperty(objectRect, new GUIContent(PhysicsUserDataInspector.objectLabel), entityIdProperty);
                EditorGUI.BeginChangeCheck();
                var newObject = EditorGUI.ObjectField(objectRect, objectLabel, PhysicsUserDataInspector.ReadObject(entityIdProperty), typeof(UnityEngine.Object), true);
                if (EditorGUI.EndChangeCheck())
                    PhysicsUserDataInspector.WriteObject(entityIdProperty, newObject);
                EditorGUI.EndProperty();

                y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                foreach (var fieldName in k_FieldNames)
                {
                    var fieldProperty = property.FindPropertyRelative(fieldName);
                    var fieldHeight = EditorGUI.GetPropertyHeight(fieldProperty, false);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, fieldHeight), fieldProperty, false);
                    y += fieldHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }
        }

        #endregion

        #region Internal

        // The fields drawn below the object row, in the same order as the UI Toolkit path.
        private static readonly string[] k_FieldNames =
        {
            nameof(PhysicsUserData.m_PhysicsMask),
            nameof(PhysicsUserData.m_Float),
            nameof(PhysicsUserData.m_Int),
            nameof(PhysicsUserData.m_Int64),
            nameof(PhysicsUserData.m_Vector3Int),
            nameof(PhysicsUserData.m_Bool),
        };

        #endregion
    }
}
