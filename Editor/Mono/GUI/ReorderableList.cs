// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;


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

        public delegate void ReorderCallbackDelegate(ReorderableList list);
        public delegate void SelectCallbackDelegate(ReorderableList list);
        public delegate void AddCallbackDelegate(ReorderableList list);
        public delegate void AddDropdownCallbackDelegate(Rect buttonRect, ReorderableList list);
        public delegate void RemoveCallbackDelegate(ReorderableList list);
        public delegate void ChangedCallbackDelegate(ReorderableList list);
        public delegate bool CanRemoveCallbackDelegate(ReorderableList list);
        public delegate bool CanAddCallbackDelegate(ReorderableList list);


        // draw callbacks
        public HeaderCallbackDelegate drawHeaderCallback;
        public FooterCallbackDelegate drawFooterCallback = null;
        public ElementCallbackDelegate drawElementCallback;
        public ElementCallbackDelegate drawElementBackgroundCallback;

        // layout callbacks
        // if supplying own element heights, try to cache the results as this may be called frequently
        public ElementHeightCallbackDelegate elementHeightCallback;

        // interaction callbacks
        public ReorderCallbackDelegate onReorderCallback;
        public SelectCallbackDelegate onSelectCallback;
        public AddCallbackDelegate onAddCallback;
        public AddDropdownCallbackDelegate onAddDropdownCallback;
        public RemoveCallbackDelegate onRemoveCallback;
        public SelectCallbackDelegate onMouseUpCallback;
        public CanRemoveCallbackDelegate onCanRemoveCallback;
        public CanAddCallbackDelegate onCanAddCallback;
        public ChangedCallbackDelegate onChangedCallback;

        private int m_ActiveElement = -1;
        private float m_DragOffset = 0;
        private GUISlideGroup m_SlideGroup;

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_Elements;
        private IList m_ElementList;
        private bool m_Draggable;
        private float m_DraggedY;
        private bool m_Dragging;
        private List<int> m_NonDragTargetIndices;

        private bool m_DisplayHeader;
        public bool displayAdd;
        public bool displayRemove;

        private int id = -1;

        // class for default rendering and behavior of reorderable list - stores styles and is statically available as s_Defaults
        public class Defaults
        {
            public GUIContent iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus", "|Add to list");
            public GUIContent iconToolbarPlusMore = EditorGUIUtility.IconContent("Toolbar Plus More", "|Choose to add to list");
            public GUIContent iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove selection from list");
            public readonly GUIStyle draggingHandle = "RL DragHandle";
            public readonly GUIStyle headerBackground = "RL Header";
            public readonly GUIStyle footerBackground = "RL Footer";
            public readonly GUIStyle boxBackground = "RL Background";
            public readonly GUIStyle preButton = "RL FooterButton";
            public GUIStyle elementBackground = new GUIStyle("RL Element");
            public const int padding = 6;
            public const int dragHandleWidth = 20;

            // draw the default footer
            public void DrawFooter(Rect rect, ReorderableList list)
            {
                float rightEdge = rect.xMax;
                float leftEdge = rightEdge - 8f;
                if (list.displayAdd)
                    leftEdge -= 25;
                if (list.displayRemove)
                    leftEdge -= 25;
                rect = new Rect(leftEdge, rect.y, rightEdge - leftEdge, rect.height);
                Rect addRect = new Rect(leftEdge + 4, rect.y - 3, 25, 13);
                Rect removeRect = new Rect(rightEdge - 29, rect.y - 3, 25, 13);
                if (Event.current.type == EventType.Repaint)
                {
                    footerBackground.Draw(rect, false, false, false, false);
                }
                if (list.displayAdd)
                {
                    using (new EditorGUI.DisabledScope(
                               list.onCanAddCallback != null && !list.onCanAddCallback(list)))
                    {
                        if (GUI.Button(addRect, list.onAddDropdownCallback != null ? iconToolbarPlusMore : iconToolbarPlus, preButton))
                        {
                            if (list.onAddDropdownCallback != null)
                                list.onAddDropdownCallback(addRect, list);
                            else if (list.onAddCallback != null)
                                list.onAddCallback(list);
                            else
                                DoAddButton(list);

                            if (list.onChangedCallback != null)
                                list.onChangedCallback(list);
                        }
                    }
                }
                if (list.displayRemove)
                {
                    using (new EditorGUI.DisabledScope(
                               list.index < 0 || list.index >= list.count ||
                               (list.onCanRemoveCallback != null && !list.onCanRemoveCallback(list))))
                    {
                        if (GUI.Button(removeRect, iconToolbarMinus, preButton))
                        {
                            if (list.onRemoveCallback == null)
                                DoRemoveButton(list);
                            else
                                list.onRemoveCallback(list);

                            if (list.onChangedCallback != null)
                                list.onChangedCallback(list);
                        }
                    }
                }
            }

            // default add button behavior
            public void DoAddButton(ReorderableList list)
            {
                if (list.serializedProperty != null)
                {
                    list.serializedProperty.arraySize += 1;
                    list.index = list.serializedProperty.arraySize - 1;
                }
                else
                {
                    // this is ugly but there are a lot of cases like null types and default constructors
                    Type elementType = list.list.GetType().GetElementType();
                    if (elementType == typeof(string))
                        list.index = list.list.Add("");
                    else if (elementType != null && elementType.GetConstructor(Type.EmptyTypes) == null)
                        Debug.LogError("Cannot add element. Type " + elementType.ToString() + " has no default constructor. Implement a default constructor or implement your own add behaviour.");
                    else if (list.list.GetType().GetGenericArguments()[0] != null)
                        list.index = list.list.Add(Activator.CreateInstance(list.list.GetType().GetGenericArguments()[0]));
                    else if (elementType != null)
                        list.index = list.list.Add(Activator.CreateInstance(elementType));
                    else
                        Debug.LogError("Cannot add element of type Null.");
                }
            }

            // default remove button behavior
            public void DoRemoveButton(ReorderableList list)
            {
                if (list.serializedProperty != null)
                {
                    list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                    if (list.index >= list.serializedProperty.arraySize - 1)
                        list.index = list.serializedProperty.arraySize - 1;
                }
                else
                {
                    list.list.RemoveAt(list.index);
                    if (list.index >= list.list.Count - 1)
                        list.index = list.list.Count - 1;
                }
            }

            // draw the default header background
            public void DrawHeaderBackground(Rect headerRect)
            {
                if (Event.current.type == EventType.Repaint)
                    headerBackground.Draw(headerRect, false, false, false, false);
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
                        draggingHandle.Draw(new Rect(rect.x + 5, rect.y + 7, 10, rect.height - (rect.height - 7)), false, false, false, false);
                }
            }

            // draw the default element
            public void DrawElement(Rect rect, SerializedProperty element, System.Object listItem, bool selected, bool focused, bool draggable)
            {
                EditorGUI.LabelField(rect, EditorGUIUtility.TempContent((element != null) ? element.displayName : listItem.ToString()));
            }

            // draw the default element
            public void DrawNoneElement(Rect rect, bool draggable)
            {
                EditorGUI.LabelField(rect, EditorGUIUtility.TempContent("List is Empty"));
            }
        }
        static Defaults s_Defaults;
        public static Defaults defaultBehaviours
        {
            get
            {
                return s_Defaults;
            }
        }

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
            if (m_Elements != null && m_Elements.editable == false)
                m_Draggable = false;
            if (m_Elements != null && m_Elements.isArray == false)
                Debug.LogError("Input elements should be an Array SerializedProperty");
        }

        public SerializedProperty serializedProperty
        {
            get { return m_Elements; }
            set { m_Elements = value; }
        }

        public IList list
        {
            get { return m_ElementList; }
            set { m_ElementList = value; }
        }


        // active element index accessor
        public int index
        {
            get { return m_ActiveElement; }
            set { m_ActiveElement = value; }
        }

        // individual element height accessor
        public float elementHeight = 21;
        // header height accessor
        public float headerHeight = 18;
        // footer height accessor
        public float footerHeight = 13;
        // show default background
        public bool showDefaultBackground = true;

        // draggable accessor
        public bool draggable
        {
            get { return m_Draggable; }
            set { m_Draggable = value; }
        }


        private Rect GetContentRect(Rect rect)
        {
            Rect r = rect;

            if (draggable)
                r.xMin += Defaults.dragHandleWidth;
            else
                r.xMin += Defaults.padding;
            r.xMax -= Defaults.padding;
            return r;
        }

        private float GetElementYOffset(int index)
        {
            return GetElementYOffset(index, -1);
        }

        private float GetElementYOffset(int index, int skipIndex)
        {
            if (elementHeightCallback == null)
                return index * elementHeight;

            float offset = 0;
            for (int i = 0; i < index; i++)
            {
                if (i != skipIndex)
                    offset += elementHeightCallback(i);
            }
            return offset;
        }

        private float GetElementHeight(int index)
        {
            if (elementHeightCallback == null)
                return elementHeight;

            return elementHeightCallback(index);
        }

        private Rect GetRowRect(int index, Rect listRect)
        {
            return new Rect(listRect.x, listRect.y + GetElementYOffset(index), listRect.width, GetElementHeight(index));
        }

        public int count
        {
            get
            {
                if (m_Elements != null)
                {
                    if (!m_Elements.hasMultipleDifferentValues)
                        return m_Elements.arraySize;

                    int smallerArraySize = m_Elements.arraySize;
                    foreach (Object targetObject in m_Elements.serializedObject.targetObjects)
                    {
                        SerializedObject serializedObject = new SerializedObject(targetObject);
                        SerializedProperty property = serializedObject.FindProperty(m_Elements.propertyPath);
                        smallerArraySize = Math.Min(property.arraySize, smallerArraySize);
                    }
                    return smallerArraySize;
                }
                return m_ElementList.Count;
            }
        }

        public void DoLayoutList() //TODO: better API?
        {
            if (s_Defaults == null)
                s_Defaults = new Defaults();

            // do the custom or default header GUI
            Rect headerRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayout.ExpandWidth(true));
            //Elements area
            Rect listRect = GUILayoutUtility.GetRect(10, GetListElementHeight(), GUILayout.ExpandWidth(true));
            // do the footer GUI
            Rect footerRect = GUILayoutUtility.GetRect(4, footerHeight, GUILayout.ExpandWidth(true));

            // do the parts of our list
            DoListHeader(headerRect);
            DoListElements(listRect);
            DoListFooter(footerRect);
        }

        public void DoList(Rect rect) //TODO: better API?
        {
            if (s_Defaults == null)
                s_Defaults = new Defaults();

            // do the custom or default header GUI
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            //Elements area
            Rect listRect = new Rect(rect.x, headerRect.y + headerRect.height, rect.width, GetListElementHeight());
            // do the footer GUI
            Rect footerRect = new Rect(rect.x, listRect.y + listRect.height, rect.width, footerHeight);

            // do the parts of our list
            DoListHeader(headerRect);
            DoListElements(listRect);
            DoListFooter(footerRect);
        }

        public float GetHeight()
        {
            float totalheight = 0f;
            totalheight += GetListElementHeight();
            totalheight += headerHeight;
            totalheight += footerHeight;
            return totalheight;
        }

        private float GetListElementHeight()
        {
            const float listElementPadding = 7;

            int arraySize = count;
            if (arraySize == 0)
            {
                return elementHeight + listElementPadding;
            }

            if (elementHeightCallback != null)
            {
                return GetElementYOffset(arraySize - 1) + GetElementHeight(arraySize - 1) + listElementPadding;
            }

            return (elementHeight * arraySize) + listElementPadding;
        }

        private void DoListElements(Rect listRect)
        {
            // How many elements? If none, make space for showing default line that shows no elements are present
            int arraySize = count;

            // draw the background in repaint
            if (showDefaultBackground && Event.current.type == EventType.Repaint)
                s_Defaults.boxBackground.Draw(listRect, false, false, false, false);

            // resize to the area that we want to draw our elements into
            listRect.yMin += 2; listRect.yMax -= 5;


            // create the rect for individual elements in the list
            Rect elementRect = listRect;
            elementRect.height = elementHeight;

            // the content rect is what we will actually draw into -- it doesn't include the drag handle or padding
            Rect elementContentRect = elementRect;


            if (((m_Elements != null && m_Elements.isArray == true) || m_ElementList != null) && arraySize > 0)
            {
                // If there are elements, we need to draw them -- we will do this differently depending on if we are dragging or not
                if (IsDragging() && Event.current.type == EventType.Repaint)
                {
                    // we are dragging, so we need to build the new list of target indices
                    int targetIndex = CalculateRowIndex();
                    m_NonDragTargetIndices.Clear();
                    for (int i = 0; i < arraySize; i++)
                    {
                        if (i != m_ActiveElement)
                            m_NonDragTargetIndices.Add(i);
                    }
                    m_NonDragTargetIndices.Insert(targetIndex, -1);

                    // now draw each element in the list (excluding the active element)
                    bool targetSeen = false;
                    for (int i = 0; i < m_NonDragTargetIndices.Count; i++)
                    {
                        if (m_NonDragTargetIndices[i] != -1)
                        {
                            elementRect.height = GetElementHeight(i);
                            // update the position of the rect (based on element position and accounting for sliding)
                            if (elementHeightCallback == null)
                            {
                                elementRect.y = listRect.y + GetElementYOffset(i, m_ActiveElement);
                            }
                            else
                            {
                                elementRect.y = listRect.y + GetElementYOffset(m_NonDragTargetIndices[i], m_ActiveElement);
                                if (targetSeen)
                                {
                                    elementRect.y += elementHeightCallback(m_ActiveElement);
                                }
                            }
                            elementRect = m_SlideGroup.GetRect(m_NonDragTargetIndices[i], elementRect);

                            // actually draw the element
                            if (drawElementBackgroundCallback == null)
                                s_Defaults.DrawElementBackground(elementRect, i, false, false, m_Draggable);
                            else
                                drawElementBackgroundCallback(elementRect, i, false, false);

                            s_Defaults.DrawElementDraggingHandle(elementRect, i, false, false, m_Draggable);

                            elementContentRect = GetContentRect(elementRect);
                            if (drawElementCallback == null)
                            {
                                if (m_Elements != null)
                                    s_Defaults.DrawElement(elementContentRect, m_Elements.GetArrayElementAtIndex(m_NonDragTargetIndices[i]), null, false, false, m_Draggable);
                                else
                                    s_Defaults.DrawElement(elementContentRect, null, m_ElementList[m_NonDragTargetIndices[i]], false, false, m_Draggable);
                            }
                            else
                            {
                                drawElementCallback(elementContentRect, m_NonDragTargetIndices[i], false, false);
                            }
                        }
                        else
                        {
                            targetSeen = true;
                        }
                    }

                    // finally get the position of the active element
                    elementRect.y = m_DraggedY - m_DragOffset + listRect.y;

                    // actually draw the element
                    if (drawElementBackgroundCallback == null)
                        s_Defaults.DrawElementBackground(elementRect, m_ActiveElement, true, true, m_Draggable);
                    else
                        drawElementBackgroundCallback(elementRect, m_ActiveElement, true, true);

                    s_Defaults.DrawElementDraggingHandle(elementRect, m_ActiveElement, true, true, m_Draggable);


                    elementContentRect = GetContentRect(elementRect);

                    // draw the active element
                    if (drawElementCallback == null)
                    {
                        if (m_Elements != null)
                            s_Defaults.DrawElement(elementContentRect, m_Elements.GetArrayElementAtIndex(m_ActiveElement), null, true, true, m_Draggable);
                        else
                            s_Defaults.DrawElement(elementContentRect, null, m_ElementList[m_ActiveElement], true, true, m_Draggable);
                    }
                    else
                    {
                        drawElementCallback(elementContentRect, m_ActiveElement, true, true);
                    }
                }
                else
                {
                    // if we aren't dragging, we just draw all of the elements in order
                    for (int i = 0; i < arraySize; i++)
                    {
                        bool activeElement = (i == m_ActiveElement);
                        bool focusedElement =  (i == m_ActiveElement && HasKeyboardControl());

                        // update the position of the element
                        elementRect.height = GetElementHeight(i);
                        elementRect.y = listRect.y + GetElementYOffset(i);

                        // draw the background
                        if (drawElementBackgroundCallback == null)
                            s_Defaults.DrawElementBackground(elementRect, i, activeElement, focusedElement, m_Draggable);
                        else
                            drawElementBackgroundCallback(elementRect, i, activeElement, focusedElement);
                        s_Defaults.DrawElementDraggingHandle(elementRect, i, activeElement, focusedElement, m_Draggable);


                        elementContentRect = GetContentRect(elementRect);

                        // do the callback for the element
                        if (drawElementCallback == null)
                        {
                            if (m_Elements != null)
                                s_Defaults.DrawElement(elementContentRect, m_Elements.GetArrayElementAtIndex(i), null, activeElement, focusedElement, m_Draggable);
                            else
                                s_Defaults.DrawElement(elementContentRect, null, m_ElementList[i], activeElement, focusedElement, m_Draggable);
                        }
                        else
                        {
                            drawElementCallback(elementContentRect, i, activeElement, focusedElement);
                        }
                    }
                }

                // handle the interaction
                DoDraggingAndSelection(listRect);
            }
            else
            {
                // there was no content, so we will draw an empty element
                elementRect.y = listRect.y;
                // draw the background
                if (drawElementBackgroundCallback == null)
                    s_Defaults.DrawElementBackground(elementRect, -1, false, false, false);
                else
                    drawElementBackgroundCallback(elementRect, -1, false, false);
                s_Defaults.DrawElementDraggingHandle(elementRect, -1, false, false, false);

                elementContentRect = elementRect;
                elementContentRect.xMin += Defaults.padding;
                elementContentRect.xMax -= Defaults.padding;
                s_Defaults.DrawNoneElement(elementContentRect, m_Draggable);
            }
        }

        private void DoListHeader(Rect headerRect)
        {
            // draw the background on repaint
            if (showDefaultBackground && Event.current.type == EventType.Repaint)
                s_Defaults.DrawHeaderBackground(headerRect);

            // apply the padding to get the internal rect
            headerRect.xMin += Defaults.padding;
            headerRect.xMax -= Defaults.padding;
            headerRect.height -= 2;
            headerRect.y += 1;

            // perform the default or overridden callback
            if (drawHeaderCallback != null)
                drawHeaderCallback(headerRect);
            else if (m_DisplayHeader)
                s_Defaults.DrawHeader(headerRect, m_SerializedObject, m_Elements, m_ElementList);
        }

        private void DoListFooter(Rect footerRect)
        {
            // perform callback or the default footer
            if (drawFooterCallback != null)
                drawFooterCallback(footerRect);
            else if (displayAdd || displayRemove)
                s_Defaults.DrawFooter(footerRect, this); // draw the footer if the add or remove buttons are required
        }

        private void DoDraggingAndSelection(Rect listRect)
        {
            Event evt = Event.current;
            int oldIndex = m_ActiveElement;
            bool clicked = false;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != id)
                        return;
                    // if we have keyboard focus, arrow through the list
                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        m_ActiveElement += 1;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        m_ActiveElement -= 1;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        m_Dragging = false;
                        evt.Use();
                    }
                    // don't allow arrowing through the ends of the list
                    m_ActiveElement = Mathf.Clamp(m_ActiveElement, 0, (m_Elements != null) ? m_Elements.arraySize - 1 : m_ElementList.Count - 1);
                    break;

                case EventType.MouseDown:
                    if (!listRect.Contains(Event.current.mousePosition) || Event.current.button != 0)
                        break;
                    // clicking on the list should end editing any existing edits
                    EditorGUI.EndEditingActiveTextField();
                    // pick the active element based on click position
                    m_ActiveElement = GetRowIndex(Event.current.mousePosition.y - listRect.y);

                    if (m_Draggable)
                    {
                        // if we can drag, set the hot control and start dragging (storing the offset)
                        m_DragOffset = (Event.current.mousePosition.y - listRect.y) - GetElementYOffset(m_ActiveElement);
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
                    if (!m_Draggable || GUIUtility.hotControl != id)
                        break;

                    // Set m_Dragging state on first MouseDrag event after we got hotcontrol (to prevent animating elements when deleting elements by context menu)
                    m_Dragging = true;

                    // if we are dragging, update the position
                    UpdateDraggedY(listRect);
                    evt.Use();
                    break;

                case EventType.MouseUp:
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

                    // hotcontrol is only set when list is draggable
                    if (GUIUtility.hotControl != id)
                        break;
                    evt.Use();
                    m_Dragging = false;

                    try
                    {
                        // What will be the index of this if we release?
                        int targetIndex = CalculateRowIndex();
                        if (m_ActiveElement != targetIndex)
                        {
                            // if the target index is different than the current index...
                            if (m_SerializedObject != null && m_Elements != null)
                            {
                                // if we are working with Serialized Properties, we can handle it for you
                                m_Elements.MoveArrayElement(m_ActiveElement, targetIndex);
                                m_SerializedObject.ApplyModifiedProperties();
                                m_SerializedObject.Update();
                            }
                            else if (m_ElementList != null)
                            {
                                // we are working with the IList, which is probably of a fixed length
                                System.Object tempObject = m_ElementList[m_ActiveElement];
                                for (int i = 0; i < m_ElementList.Count - 1; i++)
                                {
                                    if (i >= m_ActiveElement)
                                        m_ElementList[i] = m_ElementList[i + 1];
                                }
                                for (int i = m_ElementList.Count - 1; i > 0; i--)
                                {
                                    if (i > targetIndex)
                                        m_ElementList[i] = m_ElementList[i - 1];
                                }
                                m_ElementList[targetIndex] = tempObject;
                            }
                            // update the active element, now that we've moved it
                            m_ActiveElement = targetIndex;
                            // give the user a callback
                            if (onReorderCallback != null)
                                onReorderCallback(this);

                            if (onChangedCallback != null)
                                onChangedCallback(this);
                        }
                        else
                        {
                            // if mouse up was on the same index as mouse down we fire a mouse up callback (useful if for beginning renaming on mouseup)
                            if (onMouseUpCallback != null)
                                onMouseUpCallback(this);
                        }
                    }
                    finally
                    {
                        // It's quite possible a call to EndGUI was made in one of our callbacks
                        // (and thus an ExitGUIException thrown). We still need to cleanup before
                        // we exitGUI proper.
                        GUIUtility.hotControl = 0;
                        m_NonDragTargetIndices = null;
                    }
                    break;
            }
            // if the index has changed and there is a selected callback, call it
            if ((m_ActiveElement != oldIndex || clicked) && onSelectCallback != null)
                onSelectCallback(this);
        }

        bool IsMouseInsideActiveElement(Rect listRect)
        {
            int mouseRowIndex = GetRowIndex(Event.current.mousePosition.y - listRect.y);
            return mouseRowIndex == m_ActiveElement && GetRowRect(mouseRowIndex, listRect).Contains(Event.current.mousePosition);
        }

        private void UpdateDraggedY(Rect listRect)
        {
            m_DraggedY = Mathf.Clamp(Event.current.mousePosition.y - listRect.y, m_DragOffset, listRect.height - (GetElementHeight(m_ActiveElement) - m_DragOffset));
        }

        private int CalculateRowIndex()
        {
            return GetRowIndex(m_DraggedY);
        }

        private int GetRowIndex(float localY)
        {
            if (elementHeightCallback == null)
                return Mathf.Clamp(Mathf.FloorToInt(localY / elementHeight), 0, count - 1);

            float rowYOffset = 0;
            for (int i = 0; i < count; i++)
            {
                float rowYHeight = elementHeightCallback(i);
                float rowYEnd = rowYOffset + rowYHeight;
                if (localY >= rowYOffset && localY < rowYEnd)
                {
                    return i;
                }
                rowYOffset += rowYHeight;
            }
            return count - 1;
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
            return GUIUtility.keyboardControl == id;
        }
    }
}
