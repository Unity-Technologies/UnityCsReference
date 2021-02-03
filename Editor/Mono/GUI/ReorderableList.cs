// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Object = UnityEngine.Object;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditorInternal
{
    //TODO: better handling for serializedObjects with mixed values
    //TODO: make it not rely on GUILayout at all, so its safe to use under PropertyDrawers.
    public class ReorderableList
    {
        public delegate void HeaderCallbackDelegate(Rect rect);
        public delegate void FooterCallbackDelegate(Rect rect);
        public delegate void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused);
        public delegate float ElementHeightCallbackDelegate(int index);
        public delegate void DrawNoneElementCallback(Rect rect);

        public delegate void ReorderCallbackDelegateWithDetails(ReorderableList list, int oldIndex, int newIndex);
        public delegate void ReorderCallbackDelegate(ReorderableList list);
        public delegate void SelectCallbackDelegate(ReorderableList list);
        public delegate void AddCallbackDelegate(ReorderableList list);
        public delegate void AddDropdownCallbackDelegate(Rect buttonRect, ReorderableList list);
        public delegate void RemoveCallbackDelegate(ReorderableList list);
        public delegate void ChangedCallbackDelegate(ReorderableList list);
        public delegate bool CanRemoveCallbackDelegate(ReorderableList list);
        public delegate bool CanAddCallbackDelegate(ReorderableList list);
        public delegate void DragCallbackDelegate(ReorderableList list);

        // draw callbacks
        public HeaderCallbackDelegate drawHeaderCallback;
        public FooterCallbackDelegate drawFooterCallback = null;
        public ElementCallbackDelegate drawElementCallback;
        public ElementCallbackDelegate drawElementBackgroundCallback;
        public DrawNoneElementCallback drawNoneElementCallback = null;

        // layout callbacks
        // if supplying own element heights, try to cache the results as this may be called frequently
        public ElementHeightCallbackDelegate elementHeightCallback;

        // interaction callbacks
        public ReorderCallbackDelegateWithDetails onReorderCallbackWithDetails;
        public ReorderCallbackDelegate onReorderCallback;
        public SelectCallbackDelegate onSelectCallback;
        public AddCallbackDelegate onAddCallback;
        public AddDropdownCallbackDelegate onAddDropdownCallback;
        public RemoveCallbackDelegate onRemoveCallback;
        public DragCallbackDelegate onMouseDragCallback;
        public SelectCallbackDelegate onMouseUpCallback;
        public CanRemoveCallbackDelegate onCanRemoveCallback;
        public CanAddCallbackDelegate onCanAddCallback;
        public ChangedCallbackDelegate onChangedCallback;

        internal List<int> m_Selection = new List<int>();
        private float m_DragOffset = 0;
        private GUISlideGroup m_SlideGroup;

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_Elements;
        private string m_PropertyPath = string.Empty;
        private IList m_ElementList;
        private bool m_Draggable;
        private float m_DraggedY;
        private bool m_Dragging;
        private List<int> m_NonDragTargetIndices;

        private bool m_DisplayHeader;
        public bool displayAdd;
        public bool displayRemove;

        bool scheduleRemove;

        internal bool m_IsEditable;
        internal bool m_HasPropertyDrawer;

        private int id = -1;

        internal class PropertyCacheEntry
        {
            public SerializedProperty property;
            public float height;
            public float offset;
            public int lastControlCount;

            public PropertyCacheEntry(SerializedProperty property, float height, float offset)
            {
                this.property = property;
                this.height = height;
                this.offset = offset;
                lastControlCount = 0;
            }
        }
        List<PropertyCacheEntry> m_PropertyCache = new List<PropertyCacheEntry>();
        static List<string> m_OutdatedProperties = new List<string>();
        internal bool IsCacheClear => m_PropertyCache.Count == 0;

        static string GetParentListPath(string propertyPath)
        {
            int parentPathLength = propertyPath.LastIndexOf(".Array.data[");

            if (parentPathLength < 0) return null;

            return propertyPath.Substring(0, parentPathLength);
        }

        internal static void InvalidateParentCaches(string propertyPath)
        {
            string parentPath = GetParentListPath(propertyPath);
            while (parentPath != null)
            {
                m_OutdatedProperties.Add(parentPath);
                parentPath = GetParentListPath(parentPath);
            }
        }

        bool CheckForChildInvalidation()
        {
            if (m_OutdatedProperties.BinarySearch(m_PropertyPath) >= 0)
            {
                ClearCache();
                m_OutdatedProperties = m_OutdatedProperties.Where(e => !e.Equals(m_PropertyPath)).ToList();
                return true;
            }
            return false;
        }

        // class for default rendering and behavior of reorderable list - stores styles and is statically available as s_Defaults
        public class Defaults
        {
            public GUIContent iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to the list");
            public GUIContent iconToolbarPlusMore = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose to add to the list");
            public GUIContent iconToolbarMinus = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove selection from the list");
            public readonly GUIStyle draggingHandle = "RL DragHandle";
            public readonly GUIStyle headerBackground = "RL Header";
            private readonly GUIStyle emptyHeaderBackground = "RL Empty Header";
            public readonly GUIStyle footerBackground = "RL Footer";
            public readonly GUIStyle boxBackground = "RL Background";
            public readonly GUIStyle preButton = "RL FooterButton";
            public readonly GUIStyle elementBackground = "RL Element";
            internal readonly GUIStyle defaultLabel = new GUIStyle(EditorStyles.label);
            public const int padding = 6;
            public const int dragHandleWidth = 20;
            internal const int propertyDrawerPadding = 8;
            internal const int minHeaderHeight = 2;
            private int ArrayCountInPropertyPath(SerializedProperty prop) => Regex.Matches(prop.propertyPath, ".Array.data").Count;
            private float FieldLabelSize(Rect r, SerializedProperty prop) => r.xMax * 0.45f - 35 - prop.depth * 15 - ArrayCountInPropertyPath(prop) * 10;
            private static readonly GUIContent s_ListIsEmpty = EditorGUIUtility.TrTextContent("List is Empty");
            internal static readonly string undoAdd = "Add Element To Array";
            internal static readonly string undoRemove = "Remove Element From Array";
            internal static readonly string undoMove = "Reorder Element In Array";
            internal static readonly Rect infinityRect = new Rect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);

            public Defaults()
            {
                defaultLabel.wordWrap = true;
                defaultLabel.alignment = TextAnchor.MiddleCenter;
            }

            private static GUIContent OverMaxMultiEditLimit(int maxMultiEditElementCount) => EditorGUIUtility.TrTextContent($"This field cannot display arrays with more than {maxMultiEditElementCount} elements when multiple objects are selected.");

            // draw the default footer
            public void DrawFooter(Rect rect, ReorderableList list)
            {
                float rightEdge = rect.xMax - 10f;
                float leftEdge = rightEdge - 8f;
                if (list.displayAdd)
                    leftEdge -= 25;
                if (list.displayRemove)
                    leftEdge -= 25;
                rect = new Rect(leftEdge, rect.y, rightEdge - leftEdge, rect.height);
                Rect addRect = new Rect(leftEdge + 4, rect.y, 25, 16);
                Rect removeRect = new Rect(rightEdge - 29, rect.y, 25, 16);
                if (Event.current.type == EventType.Repaint)
                {
                    footerBackground.Draw(rect, false, false, false, false);
                }
                if (list.displayAdd)
                {
                    using (new EditorGUI.DisabledScope(
                        list.onCanAddCallback != null && !list.onCanAddCallback(list) || list.isOverMaxMultiEditLimit))
                    {
                        if (GUI.Button(addRect, list.onAddDropdownCallback != null ? iconToolbarPlusMore : iconToolbarPlus, preButton))
                        {
                            if (list.onAddDropdownCallback != null)
                                list.onAddDropdownCallback(addRect, list);
                            else if (list.onAddCallback != null)
                                list.onAddCallback(list);
                            else
                                DoAddButton(list);

                            list.onChangedCallback?.Invoke(list);
                            list.ClearCacheRecursive();
                        }
                    }
                }
                if (list.displayRemove)
                {
                    using (new EditorGUI.DisabledScope(list.index < 0 || list.index >= list.count
                        || (list.onCanRemoveCallback != null && !list.onCanRemoveCallback(list))
                        || list.isOverMaxMultiEditLimit))
                    {
                        if (GUI.Button(removeRect, iconToolbarMinus, preButton) || GUI.enabled && list.scheduleRemove)
                        {
                            if (list.onRemoveCallback == null)
                            {
                                DoRemoveButton(list);
                            }
                            else
                                list.onRemoveCallback(list);

                            list.onChangedCallback?.Invoke(list);
                            list.ClearCacheRecursive();
                            GUI.changed = true;
                        }
                    }
                }

                list.scheduleRemove = false;
            }

            // default add button behavior
            internal void DoAddButton(ReorderableList list, Object value)
            {
                if (GUIUtility.keyboardControl != list.id) list.GrabKeyboardFocus();

                if (list.serializedProperty != null)
                {
                    if (list.serializedProperty.hasMultipleDifferentValues &&
                        !EditorUtility.DisplayDialog(L10n.Tr("Changing size of the array will copy new value to all other selected objects."),
                            L10n.Tr("Unique values in the different selected object array size properties will be lost"),
                            L10n.Tr("OK"),
                            L10n.Tr("Cancel"))) return;

                    list.serializedProperty = list.serializedProperty.serializedObject.FindProperty(list.m_PropertyPath);
                    list.serializedProperty.arraySize = list.count + 1;
                    list.index = list.serializedProperty.arraySize - 1;

                    if (value != null)
                    {
                        list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue = value;
                    }

                    list.serializedProperty.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    // this is ugly but there are a lot of cases like null types and default constructors

                    //GetElementType() returns the Type of the object encompassed or referred to by the current array, pointer, or reference type,
                    //or null if the current Type is not an array or a pointer, or is not passed by reference,
                    //or represents a generic type or a type parameter in the definition of a generic type or generic method.
                    Type listType = list.list.GetType();
                    Type elementType;
                    if (listType.IsGenericType)
                        elementType = listType.GetTypeInfo().GenericTypeArguments[0];
                    else
                        elementType = listType.GetElementType();
                    if (value != null)
                        list.index = list.list.Add(value);
                    else if (elementType == typeof(string))
                        list.index = list.list.Add("");
                    else if (list.list.GetType().GetGenericArguments()[0] != null)
                        list.index = list.list.Add(Activator.CreateInstance(list.list.GetType().GetGenericArguments()[0]));
                    else if (elementType != null && elementType.GetConstructor(Type.EmptyTypes) == null)
                        Debug.LogError("Cannot add element. Type " + elementType + " has no default constructor. Implement a default constructor or implement your own add behaviour.");
                    else if (elementType != null)
                        list.index = list.list.Add(Activator.CreateInstance(elementType));
                    else
                        Debug.LogError("Cannot add element of type Null.");
                }
                Undo.SetCurrentGroupName(undoAdd);
                list.ClearCache();
            }

            public void DoAddButton(ReorderableList list)
            {
                DoAddButton(list, null);
            }

            // default remove button behavior
            public void DoRemoveButton(ReorderableList list)
            {
                if (GUIUtility.keyboardControl != list.id) list.GrabKeyboardFocus();

                int[] deleteIndexes = list.m_Selection.Count > 0 ? list.selectedIndices.Reverse<int>().ToArray() : new[] { list.index };
                int lastDeletedIndex = -1;
                // Remove in reverse so we do not need to keep track of changing indexes
                foreach (var index in deleteIndexes)
                {
                    if (index >= list.count)
                        continue;

                    if (list.serializedProperty != null)
                    {
                        list.serializedProperty.DeleteArrayElementAtIndex(index);

                        if (index < list.count - 1)
                        {
                            SerializedProperty currentProperty = list.serializedProperty.GetArrayElementAtIndex(index);
                            for (int i = index + 1; i < list.count; i++)
                            {
                                SerializedProperty nextProperty = list.serializedProperty.GetArrayElementAtIndex(i);
                                currentProperty.isExpanded = nextProperty.isExpanded;
                                currentProperty = nextProperty;
                            }
                        }
                        list.serializedProperty.serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        list.list.RemoveAt(list.index);
                    }
                    lastDeletedIndex = index;
                }
                list.index = Mathf.Clamp(lastDeletedIndex - 1, 0, list.count - 1);
                Undo.SetCurrentGroupName(undoRemove);
                list.ClearCache();
            }

            // draw the default header background
            public void DrawHeaderBackground(Rect headerRect)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    // We assume that a height smaller than 5px means a header with no content
                    if (headerRect.height < 5f)
                    {
                        emptyHeaderBackground.Draw(headerRect, false, false, false, false);
                    }
                    else
                    {
                        headerBackground.Draw(headerRect, false, false, false, false);
                    }
                }
            }

            // draw the default header
            public void DrawHeader(Rect headerRect, SerializedObject serializedObject, SerializedProperty element, IList elementList)
            {
                EditorGUI.LabelField(headerRect, EditorGUIUtility.TempContent((element != null) ? "Serialized Property" : "IList"));
            }

            // draw the default element background
            public void DrawElementBackground(Rect rect, int index, bool selected, bool focused, bool draggable)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    elementBackground.Draw(rect, false, selected, selected, focused);
                }
            }

            public void DrawElementDraggingHandle(Rect rect, int index, bool selected, bool focused, bool draggable)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    if (draggable)
                        draggingHandle.Draw(new Rect(rect.x + 5, rect.y + 8, 10, 6), false, false, false, false);
                }
            }

            // draw the default element
            public void DrawElement(Rect rect, SerializedProperty element, System.Object listItem, bool selected, bool focused, bool draggable)
            {
                DrawElement(rect, element, listItem, selected, focused, draggable, false);
            }

            public void DrawElement(Rect rect, SerializedProperty element, System.Object listItem, bool selected, bool focused, bool draggable, bool editable)
            {
                rect.y += elementPadding / 2;
                var prop = element ?? listItem as SerializedProperty;
                if (editable)
                {
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = FieldLabelSize(rect, prop);

                    var handler = ScriptAttributeUtility.GetHandler(prop);
                    handler.OnGUI(rect, prop, null, true);

                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) Event.current.Use();
                    return;
                }

                EditorGUI.LabelField(rect, EditorGUIUtility.TempContent((element != null) ? element.displayName : listItem.ToString()));
            }

            // draw the default element
            public void DrawNoneElement(Rect rect, bool draggable)
            {
                EditorGUI.LabelField(rect, s_ListIsEmpty);
            }

            public void DrawOverMaxMultiEditElement(Rect rect, int maxMultiEditElementCount, bool draggable)
            {
                EditorGUI.LabelField(rect, OverMaxMultiEditLimit(maxMultiEditElementCount), defaultLabel);
            }
        }
        static Defaults s_Defaults;
        public static Defaults defaultBehaviours
        {
            get
            {
                if (s_Defaults == null)
                    s_Defaults = new Defaults();

                return s_Defaults;
            }
        }

        static List<ReorderableList> s_Instances = new List<ReorderableList>();
        internal static void ClearExistingListCaches() => s_Instances.ForEach(list => list.ClearCache());

        // constructors
        public ReorderableList(IList elements, Type elementType)
        {
            InitList(null, null, elements, true, true, true, true);
        }

        public ReorderableList(IList elements, Type elementType, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton)
        {
            InitList(null, null, elements, draggable, displayHeader, displayAddButton, displayRemoveButton);
        }

        public ReorderableList(SerializedObject serializedObject, SerializedProperty elements)
        {
            InitList(serializedObject, elements, null, true, true, true, true);
        }

        public ReorderableList(SerializedObject serializedObject, SerializedProperty elements, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton)
        {
            InitList(serializedObject, elements, null, draggable, displayHeader, displayAddButton, displayRemoveButton);
        }

        private void InitList(SerializedObject serializedObject, SerializedProperty elements, IList elementList, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton)
        {
            id = GUIUtility.GetPermanentControlID();
            m_SerializedObject = serializedObject;
            m_Elements = elements;
            m_ElementList = elementList;
            m_Draggable = draggable;
            m_Dragging = false;
            m_SlideGroup = new GUISlideGroup();
            displayAdd = displayAddButton;
            m_DisplayHeader = displayHeader;
            displayRemove = displayRemoveButton;
            if (m_Elements != null)
            {
                m_PropertyPath = m_Elements.propertyPath;
                if (m_Elements.editable == false) m_Draggable = false;
                if (m_Elements.isArray == false) Debug.LogError("Input elements should be an Array SerializedProperty");
            }

            s_Instances.Add(this);
        }

        public SerializedProperty serializedProperty
        {
            get { return m_Elements; }
            set
            {
                m_Elements = value;
                m_SerializedObject = m_Elements.serializedObject;
                m_PropertyPath = m_Elements.propertyPath;
            }
        }

        public IList list
        {
            get { return m_ElementList; }
            set { m_ElementList = value; }
        }

        // active element index accessor
        public int index
        {
            get { return m_Selection.Count > 0 ? m_Selection[0] : count - 1; }
            set { Select(value); }
        }

        public ReadOnlyCollection<int> selectedIndices => new ReadOnlyCollection<int>(m_Selection);

        public bool multiSelect { get; set; }

        // individual element height accessor
        public float elementHeight = 21;
        // header height accessor
        public float headerHeight = 20;
        // footer height accessor
        public float footerHeight = 20;
        // show default background
        public bool showDefaultBackground = true;

        const float elementPadding = 2;

        private float HeaderHeight { get { return Mathf.Max(m_DisplayHeader ? headerHeight : 0, Defaults.minHeaderHeight); } }

        private float listElementTopPadding => headerHeight > 5 ? 4 : 1; // headerHeight is usually set to 3px when there is no header content. Therefore, we add a 1px top margin to match the 4px bottom margin
        private const float kListElementBottomPadding = 4;

        // draggable accessor
        public bool draggable
        {
            get { return m_Draggable; }
            set { m_Draggable = value; }
        }

        internal void CacheIfNeeded()
        {
            if (isOverMaxMultiEditLimit) return;

            m_PropertyCache.Capacity = Mathf.Max(m_PropertyCache.Capacity, m_Count - 1);
            while (m_Count > m_PropertyCache.Count)
            {
                float offset;
                if (m_Elements != null)
                {
                    SerializedProperty property;
                    if (m_PropertyCache.Count == 0 && count != 0)
                    {
                        property = m_Elements.GetArrayElementAtIndex(0);
                        offset = 0;
                    }
                    else
                    {
                        PropertyCacheEntry lastEntry = m_PropertyCache.Last();

                        property = lastEntry.property.Copy();
                        property.Next(false);
                        offset = lastEntry.offset + lastEntry.height;
                    }

                    float height = elementHeight;
                    if (elementHeightCallback != null)
                    {
                        height = elementHeightCallback(m_PropertyCache.Count);
                    }
                    else if (m_HasPropertyDrawer)
                    {
                        try
                        {
                            height = ScriptAttributeUtility.GetHandler(property).GetHeight(property, null, true);
                        }
                        catch
                        {
                            // Sometimes we find properties that no longer exist so we don't cache them
                            height = 0;
                            m_Count--;
                        }
                    }

                    if (height > 0) m_PropertyCache.Add(new PropertyCacheEntry(property, height + elementPadding, offset));
                }
                else
                {
                    if (m_PropertyCache.Count == 0)
                    {
                        offset = 0;
                    }
                    else
                    {
                        PropertyCacheEntry lastEntry = m_PropertyCache.Last();
                        offset = lastEntry.offset + lastEntry.height;
                    }

                    float height = elementHeight;
                    if (elementHeightCallback != null)
                    {
                        height = elementHeightCallback(m_PropertyCache.Count);
                    }

                    m_PropertyCache.Add(new PropertyCacheEntry(null, height + elementPadding, offset));
                }
            }
        }

        internal void ClearCache()
        {
            m_PropertyCache.Clear();
        }

        internal void ClearCacheRecursive()
        {
            if (m_Elements != null)
            {
                ClearCache();
                PropertyHandler.ClearListCacheIncludingChildren(m_Elements.propertyPath);
            }
            else
            {
                ClearCache();
            }
        }

        public void ClearSelection()
        {
            m_Selection.Clear();
        }

        public void Select(int index, bool append = false)
        {
            int insertionIndex = m_Selection.BinarySearch(index);
            if (insertionIndex < 0 || append == false && m_Selection.Count > 1)
            {
                if (!append)
                {
                    m_Selection.Clear();
                    m_Selection.Add(index);
                }
                else
                {
                    m_Selection.Insert(~insertionIndex, index);
                }
            }
        }

        public void SelectRange(int indexFrom, int indexTo)
        {
            if (!multiSelect) throw new InvalidOperationException("Cannot select range when not in multi select mode.");

            m_Selection.Clear();
            for (int i = Mathf.Min(indexFrom, indexTo); i <= Mathf.Max(indexFrom, indexTo); ++i)
            {
                m_Selection.Add(i);
            }
        }

        public bool IsSelected(int index)
        {
            return m_Selection.BinarySearch(index) >= 0;
        }

        public void Deselect(int index)
        {
            int foundIndex = m_Selection.BinarySearch(index);
            if (foundIndex >= 0)
            {
                m_Selection.RemoveAt(foundIndex);
            }
        }

        private Rect GetContentRect(Rect rect)
        {
            Rect r = rect;

            if (draggable)
                r.xMin += Defaults.dragHandleWidth;
            else
                r.xMin += Defaults.padding;
            if (m_HasPropertyDrawer)
                r.xMin += Defaults.propertyDrawerPadding;
            r.xMax -= Defaults.padding;
            return r;
        }

        private float GetElementYOffset(int index)
        {
            return GetElementYOffset(index, -1);
        }

        private float GetElementYOffset(int index, int skipIndex)
        {
            CacheIfNeeded();

            float skipOffset = 0;
            if (skipIndex >= 0 && skipIndex < index)
            {
                skipOffset = m_PropertyCache[skipIndex].height;
            }

            return m_PropertyCache[index].offset - skipOffset;
        }

        private float GetElementHeight(int index)
        {
            CacheIfNeeded();
            return m_PropertyCache[index].height;
        }

        private Rect GetRowRect(int index, Rect listRect)
        {
            return new Rect(listRect.x, listRect.y + GetElementYOffset(index), listRect.width, GetElementHeight(index));
        }

        bool isOverMaxMultiEditLimit => m_Elements != null && lastKnownBiggestArray > m_Elements.serializedObject.maxArraySizeForMultiEditing && m_Elements.serializedObject.isEditingMultipleObjects;

        public int count
        {
            get
            {
                if (m_Elements != null)
                {
                    int smallerArraySize = m_Elements.arraySize;
                    lastKnownBiggestArray = smallerArraySize;

                    try
                    {
                        if (!m_Elements.hasMultipleDifferentValues)
                            return smallerArraySize;
                    }
                    catch   // If we catch an exception this probably means we have gone through this property and must reset it to read its data again
                    {
                        m_Elements = m_SerializedObject.FindProperty(m_PropertyPath);
                    }

                    foreach (Object targetObject in m_Elements.serializedObject.targetObjects)
                    {
                        using (SerializedObject serializedObject = new SerializedObject(targetObject))
                        {
                            SerializedProperty property = serializedObject.FindProperty(m_Elements.propertyPath);
                            smallerArraySize = Math.Min(property.arraySize, smallerArraySize);
                            lastKnownBiggestArray = Math.Max(property.arraySize, lastKnownBiggestArray);
                        }
                    }

                    if (isOverMaxMultiEditLimit) return 0;

                    return smallerArraySize;
                }
                return m_ElementList != null ? m_ElementList.Count : 0;
            }
        }
        int m_Count;
        int lastKnownBiggestArray;

        public void DoLayoutList() //TODO: better API?
        {
            GUILayout.BeginVertical();

            // do the custom or default header GUI
            Rect headerRect = GUILayoutUtility.GetRect(0, HeaderHeight, GUILayout.ExpandWidth(true));
            //Elements area
            Rect listRect = GUILayoutUtility.GetRect(10, GetListElementHeight(), GUILayout.ExpandWidth(true));
            // do the footer GUI
            Rect footerRect = GUILayoutUtility.GetRect(4, footerHeight, GUILayout.ExpandWidth(true));

            // do the parts of our list
            DoListHeader(headerRect);
            DoListElements(listRect, Defaults.infinityRect);
            DoListFooter(footerRect);

            GUILayout.EndVertical();
        }

        public void DoList(Rect rect) => DoList(rect, Defaults.infinityRect);

        public void DoList(Rect rect, Rect visibleRect) //TODO: better API?
        {
            // do the custom or default header GUI
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            //Elements area
            Rect listRect = new Rect(rect.x, headerRect.y + headerRect.height, rect.width, GetListElementHeight());
            // do the footer GUI
            Rect footerRect = new Rect(rect.x, listRect.y + listRect.height, rect.width, footerHeight);

            visibleRect.y -= headerRect.height;
            visibleRect.height -= headerRect.height;

            // do the parts of our list
            DoListHeader(headerRect);
            DoListElements(listRect, visibleRect);
            DoListFooter(footerRect);
        }

        public float GetHeight()
        {
            float totalHeight = 0f;
            totalHeight += HeaderHeight;
            totalHeight += GetListElementHeight();
            totalHeight += footerHeight;
            return totalHeight;
        }

        private float GetListElementHeight()
        {
            float listElementPadding = kListElementBottomPadding + listElementTopPadding;

            m_Count = count;
            if (m_Count == 0 || isOverMaxMultiEditLimit)
            {
                return elementHeight * (isOverMaxMultiEditLimit ? 2 : 1) + listElementPadding;
            }

            CacheIfNeeded();
            return GetElementYOffset(m_Count - 1) + GetElementHeight(m_Count - 1) + listElementPadding;
        }

        void EnsureValidProperty(SerializedProperty property)
        {
            if (!property.isValid)
            {
                ClearCache();
                CacheIfNeeded();
            }

            try
            {
                ScriptAttributeUtility.GetHandler(property);
            }
            catch
            {
                ClearCache();
                CacheIfNeeded();
            }
        }

        int recursionCounter = 0;
        Rect lastRect = Rect.zero;
        private void DoListElements(Rect listRect, Rect visibleRect)
        {
            if ((drawElementCallback != null || m_HasPropertyDrawer) && Event.current.type == EventType.Repaint && listRect != lastRect)
            {
                // Recalculate cache values in case their height changed due to window resize
                lastRect = listRect;
                ClearCacheRecursive();
            }

            CacheIfNeeded();

            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // draw the background in repaint
            if (showDefaultBackground && Event.current.type == EventType.Repaint)
                defaultBehaviours.boxBackground.Draw(listRect, false, false, false, false);

            // resize to the area that we want to draw our elements into
            listRect.yMin += listElementTopPadding; listRect.yMax -= kListElementBottomPadding;

            if (showDefaultBackground)
            {
                listRect.xMin += 1;
                listRect.xMax -= 1;
            }

            // create the rect for individual elements in the list
            var elementRect = listRect;
            elementRect.height = elementHeight;

            // the content rect is what we will actually draw into -- it doesn't include the drag handle or padding
            var elementContentRect = elementRect;

            bool handlingInput = Event.current.type == EventType.MouseDown;

            if ((m_Elements != null && m_Elements.isArray || m_ElementList != null) && m_Count > 0 && !isOverMaxMultiEditLimit)
            {
                EditorGUI.BeginChangeCheck();
                // If there are elements, we need to draw them -- we will do this differently depending on if we are dragging or not
                if (IsDragging() && Event.current.type == EventType.Repaint)
                {
                    // we are dragging, so we need to build the new list of target indices
                    var targetIndex = CalculateRowIndex(listRect);

                    m_NonDragTargetIndices.Clear();
                    for (var i = 0; i < m_Count; i++)
                    {
                        if (i != index)
                            m_NonDragTargetIndices.Add(i);
                    }
                    m_NonDragTargetIndices.Insert(targetIndex, -1);

                    if (m_Elements != null) EnsureValidProperty(m_PropertyCache.Last().property);

                    // now draw each element in the list (excluding the active element)
                    var targetSeen = false;
                    for (var i = 0; i < m_NonDragTargetIndices.Count; i++)
                    {
                        if (visibleRect.y > GetElementYOffset(i) + GetElementHeight(i)) continue;
                        if (visibleRect.y + visibleRect.height < GetElementYOffset(i > 0 ? i - 1 : i)) break;

                        var nonDragTargetIndex = m_NonDragTargetIndices[i];
                        if (nonDragTargetIndex != -1)
                        {
                            elementRect.height = GetElementHeight(nonDragTargetIndex);
                            // update the position of the rect (based on element position and accounting for sliding)
                            elementRect.y = listRect.y + GetElementYOffset(nonDragTargetIndex, index);
                            if (targetSeen)
                            {
                                elementRect.y += GetElementHeight(index);
                            }

                            Rect r = m_SlideGroup.GetRect(nonDragTargetIndex, elementRect);
                            elementRect.y = r.y;

                            // actually draw the element
                            if (drawElementBackgroundCallback == null)
                                defaultBehaviours.DrawElementBackground(elementRect, nonDragTargetIndex, false, false, m_Draggable);
                            else
                                drawElementBackgroundCallback(elementRect, nonDragTargetIndex, false, false);

                            defaultBehaviours.DrawElementDraggingHandle(elementRect, i, false, false, m_Draggable);

                            elementContentRect = GetContentRect(elementRect);
                            if (drawElementCallback == null)
                            {
                                if (m_Elements != null)
                                    s_Defaults.DrawElement(elementContentRect, m_PropertyCache[nonDragTargetIndex].property, null, false, false, m_Draggable, m_IsEditable);
                                else
                                    defaultBehaviours.DrawElement(elementContentRect, null, m_ElementList[nonDragTargetIndex], false, false, m_Draggable, m_IsEditable);
                            }
                            else
                            {
                                drawElementCallback(elementContentRect, nonDragTargetIndex, false, false);
                            }
                        }
                        else
                        {
                            targetSeen = true;
                        }
                    }

                    if (index >= 0)
                    {
                        // finally get the position of the active element
                        elementRect.y = GetClampedDragPosition(listRect) - m_DragOffset + listRect.y;
                        elementRect.height = GetElementHeight(index);

                        // actually draw the element
                        if (drawElementBackgroundCallback == null)
                            defaultBehaviours.DrawElementBackground(elementRect, index, true, true, m_Draggable);
                        else
                            drawElementBackgroundCallback(elementRect, index, true, true);

                        defaultBehaviours.DrawElementDraggingHandle(elementRect, index, true, true, m_Draggable);

                        elementContentRect = GetContentRect(elementRect);

                        // draw the active element
                        if (drawElementCallback == null)
                        {
                            if (m_Elements != null)
                                s_Defaults.DrawElement(elementContentRect, m_PropertyCache[index].property, null, true, true, m_Draggable, m_IsEditable);
                            else
                                defaultBehaviours.DrawElement(elementContentRect, null, m_ElementList[index], true, true, m_Draggable, m_IsEditable);
                        }
                        else
                        {
                            drawElementCallback(elementContentRect, index, true, true);
                        }
                    }
                }
                else
                {
                    if (m_Elements != null) EnsureValidProperty(m_PropertyCache.Last().property);

                    // if we aren't dragging, we just draw all of the elements in order
                    for (int i = 0; i < m_Count; i++)
                    {
                        if (visibleRect.y > GetElementYOffset(i) + GetElementHeight(i)) continue;
                        if (visibleRect.y + visibleRect.height < GetElementYOffset(i > 0 ? i - 1 : i)) break;

                        int initialProperties = EditorGUI.s_PropertyCount;

                        bool activeElement = m_Selection.Any(id => id == i);
                        bool focusedElement = (activeElement && HasKeyboardControl());

                        // update the position of the element
                        elementRect.height = GetElementHeight(i);
                        elementRect.y = listRect.y + GetElementYOffset(i);

                        // draw the background
                        if (drawElementBackgroundCallback == null)
                            defaultBehaviours.DrawElementBackground(elementRect, i, activeElement, focusedElement, m_Draggable);
                        else
                            drawElementBackgroundCallback(elementRect, i, activeElement, focusedElement);
                        defaultBehaviours.DrawElementDraggingHandle(elementRect, i, activeElement, focusedElement, m_Draggable);

                        elementContentRect = GetContentRect(elementRect);

                        // do the callback for the element
                        if (drawElementCallback == null)
                        {
                            if (m_Elements != null)
                            {
                                s_Defaults.DrawElement(elementContentRect, m_PropertyCache[i].property, null, activeElement, focusedElement, m_Draggable, m_IsEditable);
                            }
                            else
                                defaultBehaviours.DrawElement(elementContentRect, null, m_ElementList[i], activeElement, focusedElement, m_Draggable, m_IsEditable);
                        }
                        else
                        {
                            drawElementCallback(elementContentRect, i, activeElement, focusedElement);
                        }

                        if (handlingInput && Event.current.type == EventType.Used)
                        {
                            index = i;
                            handlingInput = false;
                        }

                        // Element drawing could be changed from distant properties or controls
                        // so if we detect any change in the way the property is drawn, clear cache
                        int currentControlCount = EditorGUI.s_PropertyCount - initialProperties;
                        if (m_PropertyCache[i].lastControlCount > 1 && currentControlCount > 1
                            && m_PropertyCache[i].lastControlCount != currentControlCount)
                        {
                            ClearCacheRecursive();
                            CacheIfNeeded();
                            if ((m_Count = count) >= i) break;
                        }
                        m_PropertyCache[i].lastControlCount = currentControlCount;

                        // If an event was consumed in the course of running this for loop, then there is
                        // a good chance the array data has changed and it is dangerous for us to continue
                        // rendering it in this frame.
                        if (Event.current.type == EventType.Used) break;
                    }
                }

                // handle the interaction
                DoDraggingAndSelection(listRect);

                if (EditorGUI.EndChangeCheck())
                {
                    ClearCacheRecursive();
                }
            }
            else
            {
                // there was no content, so we will draw an empty element
                elementRect.y = listRect.y;
                // draw the background
                if (drawElementBackgroundCallback == null)
                    defaultBehaviours.DrawElementBackground(elementRect, -1, false, false, false);
                else
                    drawElementBackgroundCallback(elementRect, -1, false, false);
                defaultBehaviours.DrawElementDraggingHandle(elementRect, -1, false, false, false);

                elementContentRect = elementRect;
                elementContentRect.xMin += Defaults.padding;
                elementContentRect.xMax -= Defaults.padding;
                if (drawNoneElementCallback == null)
                {
                    if (isOverMaxMultiEditLimit)
                    {
                        elementContentRect.height *= 2;
                        defaultBehaviours.DrawOverMaxMultiEditElement(elementContentRect, m_Elements.serializedObject.maxArraySizeForMultiEditing, m_Draggable);
                    }
                    else
                    {
                        defaultBehaviours.DrawNoneElement(elementContentRect, m_Draggable);
                    }
                }
                else
                    drawNoneElementCallback(elementContentRect);
            }

            EditorGUI.indentLevel = prevIndent;

            // Redraw if a change happened that could have changed child size
            if (CheckForChildInvalidation() && recursionCounter < 2)
            {
                recursionCounter++;
                DoListElements(listRect, visibleRect);
            }
        }

        private void DoListHeader(Rect headerRect)
        {
            // Ensure there's proper Prefab and context menu handling for the list as a whole.
            // This ensures a deleted element in the list is displayed as an override and can
            // be handled by the user via the context menu. Case 1292522
            if (m_Elements != null)
                EditorGUI.BeginProperty(headerRect, GUIContent.none, m_Elements);

            recursionCounter = 0;
            // draw the background on repaint
            if (showDefaultBackground && Event.current.type == EventType.Repaint)
                defaultBehaviours.DrawHeaderBackground(headerRect);

            // apply the padding to get the internal rect
            headerRect.xMin += Defaults.padding;
            headerRect.xMax -= Defaults.padding;
            headerRect.height -= 2;
            headerRect.y += 1;

            // perform the default or overridden callback
            if (drawHeaderCallback != null)
                drawHeaderCallback(headerRect);
            else if (m_DisplayHeader)
                defaultBehaviours.DrawHeader(headerRect, m_SerializedObject, m_Elements, m_ElementList);

            if (m_Elements != null)
                EditorGUI.EndProperty();
        }

        private void DoListFooter(Rect footerRect)
        {
            // perform callback or the default footer
            if (drawFooterCallback != null)
                drawFooterCallback(footerRect);
            else if (displayAdd || displayRemove)
                defaultBehaviours.DrawFooter(footerRect, this); // draw the footer if the add or remove buttons are required
        }

        private void DoDraggingAndSelection(Rect listRect)
        {
            Event evt = Event.current;
            int oldIndex = index;
            bool clicked = false;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != id || m_Dragging || clicked)
                        return;
                    // if we have keyboard focus, arrow through the list
                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        index += 1;
                        if (index >= m_Count) index = 0;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        index -= 1;
                        if (index < 0) index = m_Count - 1;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.LeftArrow)
                    {
                        if (m_Elements != null)
                        {
                            foreach (var index in m_Selection)
                            {
                                if (index < 0) continue;

                                m_Elements.GetArrayElementAtIndex(index).isExpanded = false;
                            }
                        }
                        InvalidateParentCaches(m_PropertyPath);
                        GUI.changed = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.RightArrow)
                    {
                        if (m_Elements != null)
                        {
                            foreach (var index in selectedIndices)
                            {
                                if (index < 0) continue;

                                m_Elements.GetArrayElementAtIndex(index).isExpanded = true;
                            }
                        }
                        InvalidateParentCaches(m_PropertyPath);
                        GUI.changed = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Delete)
                    {
                        scheduleRemove = true;
                        InvalidateParentCaches(m_PropertyPath);
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        m_Dragging = false;
                        evt.Use();
                    }
                    if (evt.type == EventType.Used)
                    {
                        // don't allow arrowing through the ends of the list
                        m_Selection = m_Selection.Where(i => i >= 0 && i < (m_Elements != null ? m_Elements.arraySize : m_ElementList.Count)).ToList();
                    }
                    if (oldIndex != index && PropertyEditor.FocusedPropertyEditor != null)
                    {
                        float offset = 0;
                        if (oldIndex < index) offset = Mathf.Min(GetElementHeight(index), PropertyEditor.FocusedPropertyEditor.scrollViewportRect.height * 0.9f);

                        Vector2 elementPosition = new Vector2(0, listRect.y + GetElementYOffset(index) + offset);
                        elementPosition = GUIUtility.GUIToScreenPoint(elementPosition) - new Vector2(0, PropertyEditor.FocusedPropertyEditor.m_Pos.y);
                        PropertyEditor.FocusedPropertyEditor.ScrollTo(elementPosition);
                    }
                    break;

                case EventType.MouseDown:

                    if (!listRect.Contains(Event.current.mousePosition)
                        || Event.current.button > 0)
                        break;

                    // clicking on the list should end editing any existing edits
                    EditorGUI.EndEditingActiveTextField();
                    // pick the active element based on click position
                    int selected = GetRowIndex(Event.current.mousePosition.y - listRect.y);

                    if (evt.control && multiSelect) // Toggle selection
                    {
                        if (IsSelected(selected))
                            Deselect(selected);
                        else
                            Select(selected, true);
                    }
                    else if (evt.shift && multiSelect && selectedIndices.Count > 0) // Select from - to
                    {
                        int selectionFromTarget = selected < selectedIndices[0] ? selectedIndices[selectedIndices.Count - 1] : selectedIndices[0];
                        SelectRange(selectionFromTarget, selected);
                    }
                    else
                    {
                        Select(selected);
                    }

                    if (index >= 0 && m_Draggable)
                    {
                        float mousePosInList = Event.current.mousePosition.y - listRect.y;
                        // if we can drag, set the hot control and start dragging (storing the offset)
                        m_DragOffset = mousePosInList - GetElementYOffset(GetRowIndex(mousePosInList));
                        UpdateDraggedY(listRect);
                        GUIUtility.hotControl = id;
                        m_SlideGroup.Reset();
                        m_NonDragTargetIndices = new List<int>();
                    }
                    GrabKeyboardFocus();

                    evt.Use();
                    clicked = true;
                    break;

                case EventType.MouseDrag:
                    if (!m_Draggable || GUIUtility.hotControl != id || evt.modifiers != EventModifiers.None) // Ignore dragging on the x axis
                    {
                        m_Dragging = false;
                        break;
                    }

                    // Set m_Dragging state on first MouseDrag event after we got hotcontrol (to prevent animating elements when deleting elements by context menu)
                    m_Dragging = true;

                    // Clear selection to just the dragged item
                    if (m_Selection.Count > 1)
                        index = GetRowIndex(Event.current.mousePosition.y - listRect.y);

                    onMouseDragCallback?.Invoke(this);

                    // if we are dragging, update the position
                    UpdateDraggedY(listRect);

                    // handle inspector auto-scroll
                    if (PropertyEditor.HoveredPropertyEditor != null)
                    {
                        Vector2 mousePoistion = new Vector2(0, Mathf.Clamp(evt.mousePosition.y, listRect.y, listRect.y + listRect.height));
                        mousePoistion = GUIUtility.GUIToScreenPoint(mousePoistion) - new Vector2(0, PropertyEditor.HoveredPropertyEditor.m_Pos.y);
                        PropertyEditor.HoveredPropertyEditor.AutoScroll(mousePoistion);
                    }
                    evt.Use();
                    break;

                case EventType.MouseUp:
                    clicked = false;
                    if (!m_Draggable)
                    {
                        // if mouse up was on the same index as mouse down we fire a mouse up callback (useful if for beginning renaming on mouseup)
                        if (onMouseUpCallback != null && IsMouseInsideActiveElement(listRect))
                        {
                            // set the keyboard control
                            onMouseUpCallback(this);
                        }
                        break;
                    }

                    // hot control is only set when list is draggable
                    if (GUIUtility.hotControl != id)
                        break;

                    evt.Use();

                    try
                    {
                        // What will be the index of this if we release?
                        int targetIndex = CalculateRowIndex(listRect);
                        if (index != targetIndex && m_Dragging)
                        {
                            // if the target index is different than the current index...
                            if (m_SerializedObject != null && m_Elements != null)
                            {
                                Undo.RegisterCompleteObjectUndo(m_SerializedObject.targetObjects, Defaults.undoMove);

                                // if we are working with Serialized Properties, we can handle it for you
                                m_Elements.MoveArrayElement(index, targetIndex);
                                m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                            }
                            else if (m_ElementList != null)
                            {
                                // we are working with the IList, which is probably of a fixed length
                                object tempObject = m_ElementList[index];
                                for (int i = 0; i < m_ElementList.Count - 1; i++)
                                {
                                    if (i >= index)
                                        m_ElementList[i] = m_ElementList[i + 1];
                                }
                                for (int i = m_ElementList.Count - 1; i > 0; i--)
                                {
                                    if (i > targetIndex)
                                        m_ElementList[i] = m_ElementList[i - 1];
                                }
                                m_ElementList[targetIndex] = tempObject;
                            }

                            var oldActiveElement = index;
                            var newActiveElement = targetIndex;

                            // Retain expanded state after reordering properties
                            if (m_SerializedObject != null && m_Elements != null)
                            {
                                SerializedProperty prop1 = m_Elements.GetArrayElementAtIndex(oldActiveElement);
                                SerializedProperty prop2 = m_Elements.GetArrayElementAtIndex(newActiveElement);
                                bool tempExpanded = prop1.isExpanded;
                                prop1.isExpanded = prop2.isExpanded;
                                prop2.isExpanded = tempExpanded;
                            }

                            // update the active element, now that we've moved it
                            index = targetIndex;
                            // give the user a callback
                            if (onReorderCallbackWithDetails != null)
                                onReorderCallbackWithDetails(this, oldActiveElement, newActiveElement);
                            else onReorderCallback?.Invoke(this);

                            onChangedCallback?.Invoke(this);
                            GUI.changed = true;
                        }
                        else
                        {
                            // if mouse up was on the same index as mouse down we fire a mouse up callback (useful if for beginning renaming on mouseup)
                            onMouseUpCallback?.Invoke(this);
                        }
                    }
                    finally
                    {
                        // It's quite possible a call to EndGUI was made in one of our callbacks
                        // (and thus an ExitGUIException thrown). We still need to cleanup before
                        // we exitGUI proper.
                        GUIUtility.hotControl = 0;
                        m_NonDragTargetIndices = null;
                        m_Dragging = false;
                    }
                    break;
            }

            // if the index has changed and there is a selected callback, call it
            if ((index != oldIndex || clicked) && onSelectCallback != null)
                onSelectCallback(this);
        }

        bool IsMouseInsideActiveElement(Rect listRect)
        {
            int mouseRowIndex = GetRowIndex(Event.current.mousePosition.y - listRect.y);
            return IsSelected(mouseRowIndex) && GetRowRect(mouseRowIndex, listRect).Contains(Event.current.mousePosition);
        }

        private void UpdateDraggedY(Rect listRect)
        {
            m_DraggedY = Event.current.mousePosition.y - listRect.y;
        }

        private float GetClampedDragPosition(Rect listRect)
        {
            return Mathf.Clamp(m_DraggedY, m_DragOffset, listRect.height - (GetElementHeight(index)) + m_DragOffset);
        }

        private int CalculateRowIndex(Rect listRect)
        {
            var rowIndex = GetRowIndex(GetClampedDragPosition(listRect) - m_DragOffset, true);

            var activeElementY = m_DraggedY - m_DragOffset;
            var position = activeElementY - GetElementYOffset(index);

            if (position < 0)
            {
                while (rowIndex > 0 && GetElementYOffset(rowIndex - 1) + GetElementHeight(rowIndex - 1) / 2 > activeElementY)
                    rowIndex--;
            }

            return rowIndex;
        }

        // Used to determine the destination index of the dragged element
        // or indexes of of elements that are not interacted with while drag is happening
        private int GetRowIndex(float localY, bool skipActiveElement = false)
        {
            for (int i = 0; i < m_Count; i++)
            {
                float levelOffset = GetElementYOffset(i);

                if (skipActiveElement)
                {
                    if (i >= index)
                    {
                        levelOffset += -GetElementHeight(index) + GetElementHeight(i) / 2;
                    }
                    else if (i < index)
                    {
                        levelOffset -= GetElementHeight(i) / 2;
                    }
                }

                if (levelOffset > localY)
                {
                    return --i;
                }
            }

            return m_Count - 1;
        }

        private bool IsDragging()
        {
            return m_Dragging;
        }

        public void GrabKeyboardFocus()
        {
            GUIUtility.keyboardControl = id;
        }

        public void ReleaseKeyboardFocus()
        {
            if (GUIUtility.keyboardControl == id)
            {
                GUIUtility.keyboardControl = 0;
            }
        }

        public bool HasKeyboardControl()
        {
            return GUIUtility.keyboardControl == id && EditorGUIUtility.HasCurrentWindowKeyFocus();
        }
    }
}
