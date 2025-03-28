// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using UnityEngine.Pool;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class PropertyHandler : IDisposable
    {
        readonly static Dictionary<Type, List<(FieldInfo fieldInfo, Object objectReference)>> s_DefaultObjectReferenceCache = new();

        List<PropertyDrawer> m_PropertyDrawers;
        List<DecoratorDrawer> m_DecoratorDrawers;
        public string tooltip;

        public bool hasPropertyDrawer => propertyDrawer != null;

        internal PropertyDrawer propertyDrawer
        {
            get
            {
                if (m_PropertyDrawers == null || m_NestingLevel >= m_PropertyDrawers.Count)
                    return null;
                return m_PropertyDrawers[m_NestingLevel];
            }
        }

        internal List<DecoratorDrawer> decoratorDrawers => m_DecoratorDrawers;
        internal bool skipDecoratorDrawers { get; set; }

        int m_NestingLevel;

        bool isCurrentlyNested => m_NestingLevel > 0;

        internal static Dictionary<string, ReorderableListWrapper> s_reorderableLists = new Dictionary<string, ReorderableListWrapper>();
        static int s_LastInspectionTarget;
        static int s_LastInspectorNumComponents;

        static PropertyHandler()
        {
            Undo.undoRedoEvent += OnUndoRedo;
        }

        static void OnUndoRedo(in UndoRedoInfo info)
        {
            ReorderableList.InvalidateExistingListCaches();
        }

        public static void ClearCache()
        {
            s_reorderableLists.Clear();
            s_LastInspectionTarget = 0;
        }

        public static void InvalidateListCacheIncludingChildren(SerializedProperty property)
        {
            foreach (var listEntry in s_reorderableLists)
            {
                if (listEntry.Key.Contains(property.propertyPath)
                    && listEntry.Key.Contains(property.serializedObject.targetObject.GetInstanceID().ToString() + (GUIView.current?.nativeHandle.ToInt32() ?? -1)))
                    listEntry.Value.InvalidateCache();
            }
        }

        public List<ContextMenuItemAttribute> contextMenuItems;

        public bool empty =>
            m_DecoratorDrawers == null
            && tooltip == null
            && m_PropertyDrawers == null
            && contextMenuItems == null;

        public void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo field, Type propertyType)
        {
            if (attribute is TooltipAttribute)
            {
                tooltip = (attribute as TooltipAttribute).tooltip;
                return;
            }

            if (attribute is ContextMenuItemAttribute)
            {
                if (contextMenuItems == null)
                    contextMenuItems = new List<ContextMenuItemAttribute>();
                contextMenuItems.Add(attribute as ContextMenuItemAttribute);
                return;
            }

            // Case 1: If property is a collection, applyToCollection == false, early return to avoid custom drawer;
            // Case 2: If property is not a collection but within a collection, applyToCollection == true, early return
            //         to avoid custom drawer;
            // Case 3: If property is not a collection nor within a collection, applyToCollection value should
            //         NOT have any effects on it. Custom drawer should be used.
            // Case 4: Rest of the cases, custom drawer should be used.
            switch (attribute.applyToCollection)
            {
                // Case 1.
                case false when propertyType.IsArrayOrList():
                // Case 2.
                case true when !propertyType.IsArrayOrList() && property.propertyPath.Contains("["):
                    return;
                // Case 3 & 4.
                default:
                    // Look for its drawer type of this attribute
                    HandleDrawnType(property, attribute.GetType(), propertyType, field, attribute);
                    break;
            }
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
                    if (propertyType != null && propertyType.IsArrayOrList() && (attribute == null || !attribute.applyToCollection))
                        return;

                    var propertyDrawerForType = CreatePropertyDrawerWithDefaultObjectReferences(drawerType);
                    propertyDrawerForType.m_FieldInfo = field;

                    // Will be null by design if default type drawer!
                    propertyDrawerForType.m_Attribute = attribute;

                    if (m_PropertyDrawers == null)
                        m_PropertyDrawers = new List<PropertyDrawer>();
                    m_PropertyDrawers.Add(propertyDrawerForType);
                }
                else if (typeof(DecoratorDrawer).IsAssignableFrom(drawerType))
                {
                    // Draw decorators on array itself, not on each array elements
                    if (field != null && field.FieldType.IsArrayOrList() && !propertyType.IsArrayOrList())
                        return;

                    DecoratorDrawer decoratorDrawerForType = (DecoratorDrawer)System.Activator.CreateInstance(drawerType);
                    decoratorDrawerForType.m_Attribute = attribute;

                    if (m_DecoratorDrawers == null)
                        m_DecoratorDrawers = new List<DecoratorDrawer>();
                    m_DecoratorDrawers.Add(decoratorDrawerForType);
                }
            }
        }

        static PropertyDrawer CreatePropertyDrawerWithDefaultObjectReferences(Type drawerType)
        {
            var propertyDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType);

            // We cache the default values for the object references in the drawer as the lookup process can be slow.
            if (!s_DefaultObjectReferenceCache.TryGetValue(drawerType, out var defaultObjectReferences))
            {
                var monoScript = MonoScript.FromType(drawerType);
                if (monoScript != null)
                {
                    using var namePool = ListPool<string>.Get(out var names);
                    using var targetsPool = ListPool<Object>.Get(out var targets);

                    MonoImporter.GetDefaultReferencesInternal(monoScript, names, targets);
                    Debug.Assert(names.Count == targets.Count);

                    for (int i = 0; i < names.Count; i++)
                    {
                        defaultObjectReferences ??= new List<(FieldInfo, Object)>();
                        var field = drawerType.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            defaultObjectReferences.Add((field, targets[i]));
                        }
                    }
                }

                s_DefaultObjectReferenceCache[drawerType] = defaultObjectReferences;
            }

            if (defaultObjectReferences != null)
            {
                foreach (var (field, value) in defaultObjectReferences)
                {
                    field.SetValue(propertyDrawer, value);
                }
            }

            return propertyDrawer;
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

            if (!skipDecoratorDrawers && m_DecoratorDrawers != null && !isCurrentlyNested)
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
                // Draw with custom drawer - retrieve it BEFORE increasing nesting.
                PropertyDrawer drawer = propertyDrawer;

                using (var nestingContext = IncrementNestingContext())
                {
                    drawer.OnGUISafe(position, property.Copy(), label ?? EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip));
                }

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

                    // Calculate visibility rect specifically for reorderable list as when applied for the whole serialized object,
                    // it causes collapsed out of sight array elements appear thus messing up scroll-bar experience
                    var screenPos = GUIUtility.GUIToScreenPoint(position.position);

                    screenPos.y = Mathf.Clamp(screenPos.y,
                        GUIView.current?.screenPosition.yMin ?? 0,
                        GUIView.current?.screenPosition.yMax ?? Screen.height);

                    Rect listVisibility = new Rect(screenPos.x, screenPos.y,
                        GUIView.current?.screenPosition.width ?? Screen.width,
                        GUIView.current?.screenPosition.height ?? Screen.height);

                    listVisibility = GUIUtility.ScreenToGUIRect(listVisibility);

                    // Copy helps with recursive list rendering
                    reorderableList.Property = property.Copy();
                    reorderableList.Draw(label, position, listVisibility, tooltip, includeChildren);
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

            if (!skipDecoratorDrawers && m_DecoratorDrawers != null && !isCurrentlyNested)
                foreach (DecoratorDrawer drawer in m_DecoratorDrawers)
                    height += drawer.GetHeight();


            if (propertyDrawer != null)
            {
                // Retrieve drawer BEFORE increasing nesting.
                PropertyDrawer drawer = propertyDrawer;
                using (var nestingContext = IncrementNestingContext())
                {
                    height += drawer.GetPropertyHeightSafe(property.Copy(), label ?? EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip));
                }
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

                // Copy helps with recursive list rendering
                reorderableList.Property = property.Copy();
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
                if (childrenAreExpanded)
                {
                    var tc = EditorGUIUtility.TempContent(property.localizedDisplayName, tooltip);
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

        public void AddMenuItems(SerializedProperty property, GenericMenu menu)
        {
            if (contextMenuItems != null)
            {
                Type scriptType = property.serializedObject.targetObject.GetType();
                foreach (ContextMenuItemAttribute attribute in contextMenuItems)
                {
                    MethodInfo method = scriptType.GetMethod(attribute.function, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method == null)
                        continue;
                    menu.AddItem(new GUIContent(attribute.name), false, () => CallMenuCallback(property.serializedObject.targetObjects, method));
                }
            }

            var propertyPath = property.propertyPath.Replace(" ", "");
            menu.AddItem(new GUIContent("Copy Property Path"), false, () => EditorGUIUtility.systemCopyBuffer = propertyPath);

            if (CanSearchProperty(property))
            {
                menu.AddItem(new GUIContent("Search Same Property Value"), false, () => SearchProperty(property));
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue)
                {
                    menu.AddItem(new GUIContent($"Find references to {property.objectReferenceValue.GetType().Name} {property.objectReferenceValue.name}"), false, () => FindReferences(property.objectReferenceValue));
                }
            }
        }

        private void SearchProperty(SerializedProperty property)
        {
            CommandService.Execute("OpenToSearchByProperty", CommandHint.Menu, property);
        }

        private void FindReferences(UnityEngine.Object obj)
        {
            CommandService.Execute("OpenToFindReferenceOnObject", CommandHint.Menu, obj);
        }

        private bool CanSearchProperty(SerializedProperty property)
        {
            if (!CommandService.Exists("OpenToSearchByProperty") || !CommandService.Exists("IsPropertyValidForQuery"))
                return false;

            var result = CommandService.Execute("IsPropertyValidForQuery", CommandHint.Menu, property);
            return (bool)result;
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

            if (property == null || property.serializedObject == null || !property.serializedObject.targetObject)
                return false;

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

        public NestingContext ApplyNestingContext(int nestingLevel)
        {
            return NestingContext.Get(this, nestingLevel);
        }

        public NestingContext IncrementNestingContext()
        {
            return NestingContext.Get(this, m_NestingLevel + 1);
        }

        public void Dispose()
        {
            if (m_PropertyDrawers?.Count > 0)
            {
                foreach (var propertyDrawer in m_PropertyDrawers)
                {
                    if (propertyDrawer is IDisposable disposable)
                        disposable.Dispose();
                }
                m_PropertyDrawers.Clear();
            }

            if (m_DecoratorDrawers?.Count > 0)
            {
                foreach (var decoratorDrawer in m_DecoratorDrawers)
                {
                    if (decoratorDrawer is IDisposable disposable)
                        disposable.Dispose();
                }
                m_DecoratorDrawers.Clear();
            }
        }

        public struct NestingContext : IDisposable
        {
            PropertyHandler m_Handler;
            int m_NestingLevel;
            int m_OldNestingLevel;

            public static NestingContext Get(PropertyHandler handler, int nestingLevel)
            {
                var result = new NestingContext {m_Handler = handler, m_NestingLevel = nestingLevel};
                result.Open();
                return result;
            }

            public void Dispose()
            {
                Close();
            }

            void Open()
            {
                m_OldNestingLevel = m_Handler.m_NestingLevel;
                m_Handler.m_NestingLevel = m_NestingLevel;
            }

            void Close()
            {
                m_Handler.m_NestingLevel = m_OldNestingLevel;
            }
        }
    }
}
