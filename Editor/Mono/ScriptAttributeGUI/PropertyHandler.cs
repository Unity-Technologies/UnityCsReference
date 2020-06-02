// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class PropertyHandler
    {
        private PropertyDrawer m_PropertyDrawer = null;

        private List<DecoratorDrawer> m_DecoratorDrawers = null;
        public string tooltip = null;

        public bool hasPropertyDrawer { get { return propertyDrawer != null; } }
        internal PropertyDrawer propertyDrawer { get { return isCurrentlyNested ? null : m_PropertyDrawer; } }

        internal static Dictionary<string, ReorderableListWrapper> s_reorderableLists = new Dictionary<string, ReorderableListWrapper>();
        private static int s_LastInspectionTarget;
        private static int s_LastInspectorNumComponents;

        static PropertyHandler()
        {
            Undo.undoRedoPerformed += () =>
            {
                ReorderableList.ClearExistingListCaches();
            };
        }

        public static void ClearCache()
        {
            s_reorderableLists.Clear();
            s_LastInspectionTarget = 0;
        }

        public static void ClearListCacheIncludingChildren(string propertyPath)
        {
            foreach (var listEntry in s_reorderableLists)
            {
                if (listEntry.Key.Contains(propertyPath)) listEntry.Value.ClearCache();
            }
        }

        private bool isCurrentlyNested
        {
            get
            {
                return (m_PropertyDrawer != null
                    && ScriptAttributeUtility.s_DrawerStack.Count > 0
                    && m_PropertyDrawer == ScriptAttributeUtility.s_DrawerStack.Peek());
            }
        }

        public List<ContextMenuItemAttribute> contextMenuItems = null;

        public bool empty
        {
            get
            {
                return m_DecoratorDrawers == null
                    && tooltip == null
                    && propertyDrawer == null
                    && contextMenuItems == null;
            }
        }

        public void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo field, Type propertyType)
        {
            if (attribute is TooltipAttribute)
            {
                tooltip = (attribute as TooltipAttribute).tooltip;
                return;
            }

            if (attribute is ContextMenuItemAttribute)
            {
                // Use context menu items on array elements, not on array itself
                if (propertyType.IsArrayOrList())
                    return;
                if (contextMenuItems == null)
                    contextMenuItems = new List<ContextMenuItemAttribute>();
                contextMenuItems.Add(attribute as ContextMenuItemAttribute);
                return;
            }

            // Look for its drawer type of this attribute
            HandleDrawnType(property, attribute.GetType(), propertyType, field, attribute);
        }

        public void HandleDrawnType(SerializedProperty property, Type drawnType, Type propertyType, FieldInfo field, PropertyAttribute attribute)
        {
            Type drawerType = ScriptAttributeUtility.GetDrawerTypeForPropertyAndType(property, drawnType);

            // If we found a drawer type, instantiate the drawer, cache it, and return it.
            if (drawerType != null)
            {
                if (typeof(PropertyDrawer).IsAssignableFrom(drawerType))
                {
                    // Use PropertyDrawer on array elements, not on array itself.
                    // If there's a PropertyAttribute on an array, we want to apply it to the individual array elements instead.
                    // This is the only convenient way we can let the user apply PropertyDrawer attributes to elements inside an array.
                    if (propertyType != null && propertyType.IsArrayOrList())
                        return;

                    m_PropertyDrawer = (PropertyDrawer)System.Activator.CreateInstance(drawerType);
                    m_PropertyDrawer.m_FieldInfo = field;

                    // Will be null by design if default type drawer!
                    m_PropertyDrawer.m_Attribute = attribute;
                }
                else if (typeof(DecoratorDrawer).IsAssignableFrom(drawerType))
                {
                    // Draw decorators on array itself, not on each array elements
                    if (field != null && field.FieldType.IsArrayOrList() && !propertyType.IsArrayOrList())
                        return;

                    DecoratorDrawer decorator = (DecoratorDrawer)System.Activator.CreateInstance(drawerType);
                    decorator.m_Attribute = attribute;

                    if (m_DecoratorDrawers == null)
                        m_DecoratorDrawers = new List<DecoratorDrawer>();
                    m_DecoratorDrawers.Add(decorator);
                }
            }
        }

        // returns true if children needs to be drawn separately
        public bool OnGUI(Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
        {
            Rect visibleArea = new Rect(0, 0, float.MaxValue, float.MaxValue);
            return OnGUI(position, property, label, includeChildren, visibleArea);
        }

        internal bool OnGUI(Rect position, SerializedProperty property, GUIContent label, bool includeChildren, Rect visibleArea)
        {
            TestInvalidateCache();

            float oldLabelWidth, oldFieldWidth;

            float propHeight = position.height;
            position.height = 0;
            if (m_DecoratorDrawers != null && !isCurrentlyNested)
            {
                foreach (DecoratorDrawer decorator in m_DecoratorDrawers)
                {
                    position.height = decorator.GetHeight();

                    oldLabelWidth = EditorGUIUtility.labelWidth;
                    oldFieldWidth = EditorGUIUtility.fieldWidth;
                    decorator.OnGUI(position);
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    EditorGUIUtility.fieldWidth = oldFieldWidth;

                    position.y += position.height;
                    propHeight -= position.height;
                }
            }

            position.height = propHeight;
            if (propertyDrawer != null)
            {
                // Remember widths
                oldLabelWidth = EditorGUIUtility.labelWidth;
                oldFieldWidth = EditorGUIUtility.fieldWidth;
                // Draw with custom drawer
                propertyDrawer.OnGUISafe(position, property.Copy(), label ?? EditorGUIUtility.TempContent(property.localizedDisplayName));
                // Restore widths
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;

                return false;
            }
            else
            {
                if (IsNonStringArray(property))
                {
                    ReorderableListWrapper reorderableList;
                    string key = ReorderableListWrapper.GetPropertyIdentifier(property);

                    if (!s_reorderableLists.TryGetValue(key, out reorderableList))
                    {
                        throw new IndexOutOfRangeException($"collection with name \"{property.name}\" doesn't have ReorderableList assigned to it.");
                    }

                    reorderableList.Property = property;
                    reorderableList.Draw(position, visibleArea);
                    return false;
                }

                if (!includeChildren)
                    return EditorGUI.DefaultPropertyField(position, property, label);

                // Remember state
                Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
                bool wasEnabled = GUI.enabled;
                int origIndent = EditorGUI.indentLevel;

                int relIndent = origIndent - property.depth;

                SerializedProperty prop = property.Copy();

                position.height = EditorGUI.GetSinglePropertyHeight(prop, label);

                // First property with custom label
                EditorGUI.indentLevel = prop.depth + relIndent;
                bool childrenAreExpanded = EditorGUI.DefaultPropertyField(position, prop, label) && EditorGUI.HasVisibleChildFields(prop);
                position.y += position.height + EditorGUI.kControlVerticalSpacing;

                // Loop through all child properties
                if (childrenAreExpanded)
                {
                    SerializedProperty endProperty = prop.GetEndProperty();
                    while (prop.NextVisible(childrenAreExpanded) && !SerializedProperty.EqualContents(prop, endProperty))
                    {
                        var handler = ScriptAttributeUtility.GetHandler(prop);
                        EditorGUI.indentLevel = prop.depth + relIndent;
                        position.height = handler.GetHeight(prop, null, false);

                        if (position.Overlaps(visibleArea))
                        {
                            EditorGUI.BeginChangeCheck();
                            childrenAreExpanded = handler.OnGUI(position, prop, null, false) && EditorGUI.HasVisibleChildFields(prop);
                            // Changing child properties (like array size) may invalidate the iterator,
                            // so stop now, or we may get errors.
                            if (EditorGUI.EndChangeCheck())
                                break;
                        }

                        position.y += position.height + EditorGUI.kControlVerticalSpacing;
                    }
                }

                // Restore state
                GUI.enabled = wasEnabled;
                EditorGUIUtility.SetIconSize(oldIconSize);
                EditorGUI.indentLevel = origIndent;

                return false;
            }
        }

        public bool OnGUILayout(SerializedProperty property, GUIContent label, bool includeChildren, params GUILayoutOption[] options)
        {
            Rect r;
            if (property.propertyType == SerializedPropertyType.Boolean && propertyDrawer == null && (m_DecoratorDrawers == null || m_DecoratorDrawers.Count == 0))
                r = EditorGUILayout.GetToggleRect(true, options);
            else
                r = EditorGUILayout.GetControlRect(EditorGUI.LabelHasContent(label), GetHeight(property, label, includeChildren), options);
            EditorGUILayout.s_LastRect = r;
            return OnGUI(r, property, label, includeChildren);
        }

        public float GetHeight(SerializedProperty property, GUIContent label, bool includeChildren)
        {
            float height = 0;

            if (IsNonStringArray(property))
            {
                ReorderableListWrapper reorderableList;
                string key = ReorderableListWrapper.GetPropertyIdentifier(property);

                // If collection doesn't have a ReorderableList assigned to it, create one and assign it
                if (!s_reorderableLists.TryGetValue(key, out reorderableList))
                {
                    reorderableList = new ReorderableListWrapper(property, IsArrayReorderable(property));
                    s_reorderableLists[key] = reorderableList;
                }

                reorderableList.Property = property;
                height = s_reorderableLists[key].GetHeight();
                return height;
            }

            if (m_DecoratorDrawers != null && !isCurrentlyNested)
                foreach (DecoratorDrawer drawer in m_DecoratorDrawers)
                    height += drawer.GetHeight();

            if (propertyDrawer != null)
            {
                height += propertyDrawer.GetPropertyHeightSafe(property.Copy(), label ?? EditorGUIUtility.TempContent(property.displayName));
            }
            else if (!includeChildren)
            {
                height += EditorGUI.GetSinglePropertyHeight(property, label);
            }
            else
            {
                property = property.Copy();

                // First property with custom label
                height += EditorGUI.GetSinglePropertyHeight(property, label);
                bool childrenAreExpanded = property.isExpanded && EditorGUI.HasVisibleChildFields(property);

                // Loop through all child properties
                var tc = EditorGUIUtility.TempContent(property.displayName);
                if (childrenAreExpanded)
                {
                    SerializedProperty endProperty = property.GetEndProperty();
                    while (property.NextVisible(childrenAreExpanded) && !SerializedProperty.EqualContents(property, endProperty))
                    {
                        height += ScriptAttributeUtility.GetHandler(property).GetHeight(property, tc, true);
                        childrenAreExpanded = false;
                        height += EditorGUI.kControlVerticalSpacing;
                    }
                }
            }

            return height;
        }

        public bool CanCacheInspectorGUI(SerializedProperty property)
        {
            if (m_DecoratorDrawers != null &&
                !isCurrentlyNested &&
                m_DecoratorDrawers.Any(decorator => !decorator.CanCacheInspectorGUI()))
                return false;

            if (propertyDrawer != null)
                return propertyDrawer.CanCacheInspectorGUISafe(property.Copy());

            property = property.Copy();

            bool childrenAreExpanded = property.isExpanded && EditorGUI.HasVisibleChildFields(property);

            // Loop through all child properties
            if (childrenAreExpanded)
            {
                PropertyHandler handler = null;
                SerializedProperty endProperty = property.GetEndProperty();
                while (property.NextVisible(childrenAreExpanded) && !SerializedProperty.EqualContents(property, endProperty))
                {
                    if (handler == null)
                        handler = ScriptAttributeUtility.GetHandler(property);
                    if (!handler.CanCacheInspectorGUI(property))
                        return false;
                    childrenAreExpanded = false;
                }
            }

            return true;
        }

        public void AddMenuItems(SerializedProperty property, GenericMenu menu)
        {
            if (contextMenuItems == null)
                return;

            Type scriptType = property.serializedObject.targetObject.GetType();
            foreach (ContextMenuItemAttribute attribute in contextMenuItems)
            {
                MethodInfo method = scriptType.GetMethod(attribute.function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    continue;
                menu.AddItem(new GUIContent(attribute.name), false, () => CallMenuCallback(property.serializedObject.targetObjects, method));
            }
        }

        public void CallMenuCallback(object[] targets, MethodInfo method)
        {
            foreach (object target in targets)
                method.Invoke(target, new object[] {});
        }

        internal void TestInvalidateCache()
        {
            GameObject activeObject = Selection.activeObject as GameObject;
            if (activeObject != null)
            {
                var components = activeObject.GetComponents(typeof(Component));
                if (s_LastInspectionTarget != activeObject.GetInstanceID() ||
                    s_LastInspectorNumComponents != components.Length)
                {
                    ClearCache();
                    s_LastInspectionTarget = activeObject.GetInstanceID();
                    s_LastInspectorNumComponents = components.Length;
                }
            }
        }

        internal static bool IsNonStringArray(SerializedProperty property)
        {
            // Strings should not be represented with ReorderableList, they will use custom drawer therefore we don't treat them as other arrays
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        static bool IsArrayReorderable(SerializedProperty property)
        {
            FieldInfo listInfo = null;
            Queue<string> propertyName = new Queue<string>(property.propertyPath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            listInfo = property.serializedObject.targetObject.GetType().GetField(propertyName.Dequeue());

            if (listInfo == null) return false;

            // If we have a nested property we need to find it via reflection in order to verify
            // if it has a non-reorderable attribute
            while (propertyName.Count > 0)
            {
                Type t = listInfo.FieldType;
                if (t.IsArray) t = t.GetElementType();
                else if (t.IsArrayOrList()) t = t.GetGenericArguments().Single();
                FieldInfo f = t.GetField(propertyName.Dequeue());
                if (f != null)
                {
                    listInfo = f;
                }
            }

            return TypeCache.GetFieldsWithAttribute(typeof(ReorderableAttribute)).Any(f => f.Equals(listInfo)) || property.IsReorderable();
        }
    }
}
