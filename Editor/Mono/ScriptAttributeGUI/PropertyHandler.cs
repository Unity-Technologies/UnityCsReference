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
            Undo.undoRedoPerformed += () => ReorderableList.ClearExistingListCaches();
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
                propertyDrawer.OnGUISafe(position, property.Copy(), label ?? EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip));
                // Restore widths
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;

                return false;
            }
            else
            {
                if (!includeChildren)
                    return EditorGUI.DefaultPropertyField(position, property, label);

                if (UseReorderabelListControl(property))
                {
                    ReorderableListWrapper reorderableList;
                    string key = ReorderableListWrapper.GetPropertyIdentifier(property);

                    if (!s_reorderableLists.TryGetValue(key, out reorderableList))
                    {
                        // Manual layout controls don't call GetHeight() method so we need to have a way to initialized list as we prepare to render it here
                        reorderableList = new ReorderableListWrapper(property, label, true);
                        s_reorderableLists[key] = reorderableList;
                    }

                    reorderableList.Property = property;
                    reorderableList.Draw(label, position, visibleArea, tooltip, includeChildren);
                    return !includeChildren && property.isExpanded;
                }

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

                if (property.isArray)
                    EditorGUI.BeginIsInsideList(prop.depth);

                // Loop through all child properties
                if (childrenAreExpanded)
                {
                    SerializedProperty endProperty = prop.GetEndProperty();
                    while (prop.NextVisible(childrenAreExpanded) && !SerializedProperty.EqualContents(prop, endProperty))
                    {
                        if (GUI.isInsideList && prop.depth <= EditorGUI.GetInsideListDepth())
                            EditorGUI.EndIsInsideList();

                        if (prop.isArray)
                            EditorGUI.BeginIsInsideList(prop.depth);

                        var handler = ScriptAttributeUtility.GetHandler(prop);
                        EditorGUI.indentLevel = prop.depth + relIndent;
                        position.height = handler.GetHeight(prop, null, UseReorderabelListControl(prop) && includeChildren);

                        if (position.Overlaps(visibleArea))
                        {
                            EditorGUI.BeginChangeCheck();
                            childrenAreExpanded = handler.OnGUI(position, prop, null, UseReorderabelListControl(prop)) && EditorGUI.HasVisibleChildFields(prop);
                            // Changing child properties (like array size) may invalidate the iterator,
                            // so stop now, or we may get errors.
                            if (EditorGUI.EndChangeCheck())
                                break;
                        }

                        position.y += position.height + EditorGUI.kControlVerticalSpacing;
                    }
                }

                // Restore state
                if (GUI.isInsideList && property.depth <= EditorGUI.GetInsideListDepth())
                    EditorGUI.EndIsInsideList();
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

            if (m_DecoratorDrawers != null && !isCurrentlyNested)
                foreach (DecoratorDrawer drawer in m_DecoratorDrawers)
                    height += drawer.GetHeight();


            if (propertyDrawer != null)
            {
                height += propertyDrawer.GetPropertyHeightSafe(property.Copy(), label ?? EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip));
            }
            else if (!includeChildren)
            {
                height += EditorGUI.GetSinglePropertyHeight(property, label);
            }
            else if (UseReorderabelListControl(property))
            {
                ReorderableListWrapper reorderableList;
                string key = ReorderableListWrapper.GetPropertyIdentifier(property);

                // If collection doesn't have a ReorderableList assigned to it, create one and assign it
                if (!s_reorderableLists.TryGetValue(key, out reorderableList))
                {
                    reorderableList = new ReorderableListWrapper(property, label, true);
                    s_reorderableLists[key] = reorderableList;
                }

                reorderableList.Property = property;
                height += s_reorderableLists[key].GetHeight();
                return height;
            }
            else
            {
                property = property.Copy();

                // First property with custom label
                height += EditorGUI.GetSinglePropertyHeight(property, label);
                bool childrenAreExpanded = property.isExpanded && EditorGUI.HasVisibleChildFields(property);

                // Loop through all child properties
                var tc = EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip);
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

        static List<Component> s_CachedComponents = new List<Component>();

        internal void TestInvalidateCache()
        {
            GameObject activeObject = Selection.activeObject as GameObject;
            if (activeObject != null)
            {
                activeObject.GetComponents(s_CachedComponents);
                var componentCount = s_CachedComponents.Count;
                s_CachedComponents.Clear();

                if (s_LastInspectionTarget != activeObject.GetInstanceID() ||
                    s_LastInspectorNumComponents != componentCount)
                {
                    ClearCache();
                    s_LastInspectionTarget = activeObject.GetInstanceID();
                    s_LastInspectorNumComponents = componentCount;
                }
            }
        }

        static bool IsNonStringArray(SerializedProperty property)
        {
            if (property == null) return false;
            // Strings should not be represented with ReorderableList, they will use custom drawer therefore we don't treat them as other arrays
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        internal static bool IsArrayReorderable(SerializedProperty property)
        {
            const BindingFlags fieldFilter = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (property == null) return false;
            if (property.IsReorderable()) return true;

            FieldInfo listInfo = null;
            Queue<string> propertyName = new Queue<string>(property.propertyPath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            Type type = property.serializedObject.targetObject.GetType();
            var name = propertyName.Dequeue();
            listInfo = type.GetField(name, fieldFilter);

            if (listInfo == null)
            {
                // it may be private in any parent and still serializable
                type = type.BaseType;
                while (listInfo == null && type != null)
                {
                    listInfo = type.GetField(name, fieldFilter);
                    type = type.BaseType;
                }
                if (listInfo == null) return false;
            }

            // If we have a nested property we need to find it via reflection in order to verify
            // if it has a non-reorderable attribute
            while (propertyName.Count > 0)
            {
                try
                {
                    // We should at least try to get the type from object instance
                    // in order to handle interfaces and abstractions correctly
                    type = listInfo.GetValue(property.serializedObject.targetObject).GetType();
                }
                catch
                {
                    type = listInfo.FieldType;
                }

                if (type.IsArray) type = type.GetElementType();
                else if (type.IsArrayOrList()) type = type.GetGenericArguments().Single();

                FieldInfo field = type.GetField(propertyName.Dequeue(), fieldFilter);
                if (field != null) listInfo = field;
            }

            // Since we're using TypeCache to find NonReorderableAttribute, we will need to manually check base fields
            List<FieldInfo> baseFields = new List<FieldInfo>();
            baseFields.Add(listInfo);
            while ((type = type.BaseType) != null)
            {
                var field = type.GetField(listInfo.Name, fieldFilter);
                if (field != null) baseFields.Add(field);
            }

            return !TypeCache.GetFieldsWithAttribute(typeof(NonReorderableAttribute)).Any(f => baseFields.Any(b => f.Equals(b)));
        }

        internal static bool UseReorderabelListControl(SerializedProperty property) => IsNonStringArray(property) && IsArrayReorderable(property);
    }
}
