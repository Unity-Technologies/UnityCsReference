// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    [CustomPropertyDrawer(typeof(IRenderPipelineGraphicsSettings), useForChildren: true)]
    class IRenderPipelineGraphicsSettingsPropertyDrawer : PropertyDrawer
    {
        public static bool IsEmpty(SerializedProperty property, out string warnings)
        {
            if (!property.hasVisibleChildren)
            {
                warnings = $"This {nameof(IRenderPipelineGraphicsSettings)} has no visible children. Consider using {nameof(HideInInspector)} if you want to completely hide the setting.";
                return true;
            }

            warnings = string.Empty;
            return false;
        }

        // Used in Unit tests
        public static IEnumerable<SerializedProperty> VisibleChildrenEnumerator(SerializedProperty property)
        {
            if (!property.hasVisibleChildren)
                yield break;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            iterator.NextVisible(true); // Move to the first child property
            do
            {
                yield return iterator;
                iterator.NextVisible(false);
            } while (!SerializedProperty.EqualContents(iterator, end)); // Move to the next sibling property
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsEmpty(property, out var warnings))
            {
                EditorGUI.HelpBox(position, warnings, MessageType.Warning);
                return;
            }

            int baseNameLength = property.propertyPath.Length + 1;
            foreach (var child in VisibleChildrenEnumerator(property))
            {
                Rect childPosition = position;
                childPosition.height = EditorGUI.GetPropertyHeight(child);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(childPosition, child);
                if (EditorGUI.EndChangeCheck())
                {
                    child.serializedObject.ApplyModifiedProperties();
                    var settings = property.boxedValue as IRenderPipelineGraphicsSettings;
                    settings.NotifyValueChanged(child.propertyPath.Substring(baseNameLength));
                }
                position.y += childPosition.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0f;
            foreach (var child in VisibleChildrenEnumerator(property))
                height += EditorGUI.GetPropertyHeight(child) + EditorGUIUtility.standardVerticalSpacing;
            if (height > 0)
                height -= EditorGUIUtility.standardVerticalSpacing; //remove last one
            return height;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            if (IsEmpty(property, out var warnings))
            {
                root.Add(new HelpBox(warnings, HelpBoxMessageType.Warning));
                return root;
            }

            notifier = new(property);
            foreach (var child in VisibleChildrenEnumerator(property))
            {
                var propertyField = new PropertyField(child);
                propertyField.RegisterCallback(PropagateOnChangeRecursivelyAtBinding(propertyField));
                root.Add(propertyField);
            }

            return root;
        }

        EventCallback<SerializedPropertyBindEvent> PropagateOnChangeRecursivelyAtBinding(PropertyField propertyField)
        {
            EventCallback<SerializedPropertyBindEvent> callback = null;
            callback = (SerializedPropertyBindEvent evt) => {
                UQueryState<VisualElement> childs;
                if(evt.bindProperty.isArray
                    || (childs = propertyField.Query(name: "unity-content").Visible().Build())
                        .AtIndex(0) == null) //no child
                {
                    // Fields that have no childs or array (handle all resizing and cell changes directly)
                    propertyField.RegisterCallback<SerializedPropertyChangeEvent>(notifier.AddNotificationRequest);
                }
                else
                {
                    // Propagate to custom struct and class childs
                    childs.ForEach(e => e.Query<PropertyField>().Visible().ForEach(p => p.RegisterCallback(PropagateOnChangeRecursivelyAtBinding(p))));
                }
                
                propertyField.UnregisterCallback(callback);
                callback = null;
            };
            return callback;
        }

        // Aim is to batch notification for case where several are trigerred.
        // Example:
        //  - Modifying a color, several channel amongst r, g, b, a can be altered with same editor modification
        //  - This result in several time the SerializedPropertyChangeEvent being raised.
        // Also at binding we do not want to raise notification. As there is a previous SerializedObject state to
        // compare, we can trim that, even for Arrays that have a lot of elements to bind/update.
        class Notifier
        {
            HashSet<string> notificationRequests = new();
            SerializedObject lastSO;
            IRenderPipelineGraphicsSettings rpgs;
            int baseNameLength;

            public Notifier(SerializedProperty property)
            {
                rpgs = property.boxedValue as IRenderPipelineGraphicsSettings;
                baseNameLength = property.propertyPath.Length + 1;
                lastSO = new SerializedObject(property.serializedObject.targetObject);
            }

            public void AddNotificationRequest(SerializedPropertyChangeEvent evt)
            {
                SerializedProperty property = evt?.changedProperty;
                if (property == null)
                    throw new System.ArgumentException("ChangedProperty cannot be null.", $"{nameof(evt)}.changedProperty");

                if (SameThanLast(property))
                    return;

                Register(property.propertyPath);
            }

            bool SameThanLast(SerializedProperty property)
            {
                if (SerializedProperty.DataEquals(property, lastSO?.FindProperty(property.propertyPath)))
                    return true;

                lastSO = new SerializedObject(property.serializedObject.targetObject);
                return false;
            }

            string TrimPath(string path)
            {
                int length = path.IndexOf('.', baseNameLength) - baseNameLength;
                path = length < 0
                    ? path.Substring(baseNameLength)
                    : path.Substring(baseNameLength, length);
                return path;
            }

            void Register(string propertyPath)
            {
                if (notificationRequests.Count == 0)
                    EditorApplication.CallDelayed(Notify);

                notificationRequests.Add(TrimPath(propertyPath));
            }

            void Notify()
            {
                foreach (var path in notificationRequests)
                    rpgs.NotifyValueChanged(path);
                notificationRequests.Clear();
            }
        }

        Notifier notifier;
    }
}
