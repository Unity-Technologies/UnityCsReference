// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace UnityEditor.Search
{
    /// <summary>
    /// Makes a field to receive any object type.
    /// </summary>
    public class ObjectField : BaseField<Object>
    {
        [Obsolete("Use singleLineHeight instead. (UnityUpgradable) -> singleLineHeight", error: false)]
        public static float kSingleLineHeight => singleLineHeight;
        public static float singleLineHeight
        {
            get
            {
                return EditorGUI.kSingleLineHeight;
            }
        }

        internal new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}

        internal new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlTypeAttributeDescription<Object> m_ObjectType = new UxmlTypeAttributeDescription<Object> { name = "type" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((ObjectField)ve).objectType = m_ObjectType.GetValueFromBag(bag, cc);
            }
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            newValue = TryReadComponentFromGameObject(newValue, objectType);
            var valueChanged = !EqualityComparer<Object>.Default.Equals(this.value, newValue);

            base.SetValueWithoutNotify(newValue);

            if (valueChanged)
            {
                m_ObjectFieldDisplay.Update();
            }
        }

        private Type m_ObjectType = typeof(Object);
        Object m_OriginalObject = null;

        /// <summary>
        /// The type of the objects that can be assigned.
        /// </summary>
        public Type objectType
        {
            get { return m_ObjectType; }
            set
            {
                if (m_ObjectType != value)
                {
                    m_ObjectType = value;
                    m_ObjectFieldDisplay.Update();
                }
            }
        }


        public SearchContext searchContext { get; set; }
        public SearchViewFlags searchViewFlags { get; set; }


        private class ObjectFieldDisplay : VisualElement
        {
            private readonly ObjectField m_ObjectField;
            private readonly Image m_ObjectIcon;
            private readonly Label m_ObjectLabel;

            static readonly string ussClassName = "unity-object-field-display";
            static readonly string iconUssClassName = ussClassName + "__icon";
            static readonly string labelUssClassName = ussClassName + "__label";
            static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";


            public ObjectFieldDisplay(ObjectField objectField)
            {
                AddToClassList(ussClassName);
                m_ObjectIcon = new Image {scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore};
                m_ObjectIcon.AddToClassList(iconUssClassName);
                m_ObjectLabel = new Label {pickingMode = PickingMode.Ignore};
                m_ObjectLabel.AddToClassList(labelUssClassName);
                m_ObjectField = objectField;

                Update();

                Add(m_ObjectIcon);
                Add(m_ObjectLabel);
            }

            public void Update()
            {
                GUIContent content = EditorGUIUtility.ObjectContent(m_ObjectField.value, m_ObjectField.objectType);
                m_ObjectIcon.image = content.image;
                m_ObjectLabel.text = content.text;
            }

            [EventInterest(typeof(MouseDownEvent), typeof(KeyDownEvent),
                typeof(DragUpdatedEvent), typeof(DragPerformEvent), typeof(DragLeaveEvent))]
            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if (evt == null)
                {
                    return;
                }

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    OnMouseDown(evt as MouseDownEvent);
                else if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    var kdEvt = evt as KeyDownEvent;

                    if (((evt as KeyDownEvent)?.keyCode == KeyCode.Space) ||
                        ((evt as KeyDownEvent)?.keyCode == KeyCode.KeypadEnter) ||
                        ((evt as KeyDownEvent)?.keyCode == KeyCode.Return))
                    {
                        OnKeyboardEnter();
                    }
                    else if (kdEvt.keyCode == KeyCode.Delete ||
                             kdEvt.keyCode == KeyCode.Backspace)
                    {
                        OnKeyboardDelete();
                    }
                }
                else if (evt.eventTypeId == DragUpdatedEvent.TypeId())
                    OnDragUpdated(evt);
                else if (evt.eventTypeId == DragPerformEvent.TypeId())
                    OnDragPerform(evt);
                else if (evt.eventTypeId == DragLeaveEvent.TypeId())
                    OnDragLeave();
            }

            private void OnDragLeave()
            {
                // Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
                RemoveFromClassList(acceptDropVariantUssClassName);
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                Object actualTargetObject = m_ObjectField.value;
                Component com = actualTargetObject as Component;
                if (com)
                    actualTargetObject = com.gameObject;

                if (actualTargetObject == null)
                    return;

                // One click shows where the referenced object is, or pops up a preview
                if (evt.clickCount == 1)
                {
                    // ping object
                    bool anyModifiersPressed = evt.shiftKey || evt.ctrlKey;
                    if (!anyModifiersPressed && actualTargetObject)
                    {
                        EditorGUIUtility.PingObject(actualTargetObject);
                    }
                    evt.StopPropagation();
                }
                // Double click opens the asset in external app or changes selection to referenced object
                else if (evt.clickCount == 2)
                {
                    if (actualTargetObject)
                    {
                        AssetDatabase.OpenAsset(actualTargetObject);
                        GUIUtility.ExitGUI();
                    }
                    evt.StopPropagation();
                }
            }

            private void OnKeyboardEnter()
            {
                m_ObjectField.ShowObjectSelector();
            }

            private void OnKeyboardDelete()
            {
                m_ObjectField.value = null;
            }

            private Object DNDValidateObject()
            {
                Object[] references = DragAndDrop.objectReferences;
                Object validatedObject = EditorGUI.ValidateObjectFieldAssignment(references, m_ObjectField.objectType, null, EditorGUI.ObjectFieldValidatorOptions.None);
                return validatedObject;
            }

            private void OnDragUpdated(EventBase evt)
            {
                Object validatedObject = DNDValidateObject();
                if (validatedObject != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    AddToClassList(acceptDropVariantUssClassName);

                    evt.StopPropagation();
                }
            }

            private void OnDragPerform(EventBase evt)
            {
                Object validatedObject = DNDValidateObject();
                if (validatedObject != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    m_ObjectField.value = validatedObject;

                    DragAndDrop.AcceptDrag();
                    RemoveFromClassList(acceptDropVariantUssClassName);

                    evt.StopPropagation();
                }
            }
        }

        private class ObjectFieldSelector : VisualElement
        {
            private readonly ObjectField m_ObjectField;

            public ObjectFieldSelector(ObjectField objectField)
            {
                m_ObjectField = objectField;
            }

            [EventInterest(typeof(MouseDownEvent))]
            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    m_ObjectField.ShowObjectSelector();
            }
        }

        private readonly ObjectFieldDisplay m_ObjectFieldDisplay;
        private readonly Action m_AsyncOnProjectOrHierarchyChangedCallback;
        private readonly Action m_OnProjectOrHierarchyChangedCallback;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        internal new static readonly string ussClassName = "unity-object-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        internal new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        internal new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of object elements in elements of this type.
        /// </summary>
        internal static readonly string objectUssClassName = ussClassName + "__object";
        /// <summary>
        /// USS class name of selector elements in elements of this type.
        /// </summary>
        internal static readonly string selectorUssClassName = ussClassName + "__selector";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectField()
            : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectField(string label)
            : base(label, null)
        {
            visualInput.focusable = false;
            labelElement.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_ObjectFieldDisplay = new ObjectFieldDisplay(this) { focusable = true };
            m_ObjectFieldDisplay.AddToClassList(objectUssClassName);
            var objectSelector = new ObjectFieldSelector(this);
            objectSelector.AddToClassList(selectorUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectFieldDisplay);
            visualInput.Add(objectSelector);

            // Get notified when hierarchy or project changes so we can update the display to handle renamed/missing objects.
            // This event is occasionally triggered before the reference in memory is updated, so we give it time to process.
            m_AsyncOnProjectOrHierarchyChangedCallback = () => schedule.Execute(m_OnProjectOrHierarchyChangedCallback);
            m_OnProjectOrHierarchyChangedCallback = () => m_ObjectFieldDisplay.Update();
            RegisterCallback<AttachToPanelEvent>((evt) =>
            {
                EditorApplication.projectChanged += m_AsyncOnProjectOrHierarchyChangedCallback;
                EditorApplication.hierarchyChanged += m_AsyncOnProjectOrHierarchyChangedCallback;
            });
            RegisterCallback<DetachFromPanelEvent>((evt) =>
            {
                EditorApplication.projectChanged -= m_AsyncOnProjectOrHierarchyChangedCallback;
                EditorApplication.hierarchyChanged -= m_AsyncOnProjectOrHierarchyChangedCallback;
            });
        }

        private void OnObjectChanged(Object obj)
        {
            value = TryReadComponentFromGameObject(obj, objectType);
        }

        void OnSelection(Object item, bool canceled)
        {
            if (canceled)
                value = m_OriginalObject;
        }

        internal void ShowObjectSelector()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, searchContext.searchText, "object", "objectfield");
            m_OriginalObject = value;
            var runtimeContext = new RuntimeSearchContext
            {
                contextId = bindingPath ?? name ?? label,
                pickerType = SearchPickerType.ObjectField,
                currentObject = m_OriginalObject,
                requiredTypes = new[] { objectType },
                requiredTypeNames = new[] { objectType.ToString() }
            };
            var newContext = new SearchContext(searchContext.providers, searchContext.searchText, searchContext.options, runtimeContext);
            var searchViewState = new SearchViewState(newContext, OnSelection, OnObjectChanged, objectType.ToString(), objectType)
            {
                title = $"{objectType.Name}"
            }.SetSearchViewFlags(searchViewFlags);
            if (m_OriginalObject)
                searchViewState.selectedIds = new int[] { m_OriginalObject.GetInstanceID() };
            SearchService.ShowPicker(searchViewState);
        }

        private Object TryReadComponentFromGameObject(Object obj, Type type)
        {
            var go = obj as GameObject;
            if (go != null && type != null && type.IsSubclassOf(typeof(Component)))
            {
                var comp = go.GetComponent(objectType);
                if (comp != null)
                    return comp;
            }
            return obj;
        }

        public static void DoObjectField(Rect position, SerializedProperty property, Type objType, GUIContent label, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var id = GUIUtility.GetControlID(ObjectFieldGUI.objectFieldHash, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, id, label);
            ObjectFieldGUI.DoObjectField(position, position, id, objType, property, null, context, searchViewFlags);
            EditorGUI.EndProperty();
        }

        public static Object DoObjectField(Rect position, Object obj, Type objType, GUIContent label, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None)
        {
            var id = GUIUtility.GetControlID(ObjectFieldGUI.objectFieldHash, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, id, label);
            return ObjectFieldGUI.DoObjectField(position, position, id, obj, null, objType, null, context, searchViewFlags);
        }
    }

    static class ObjectFieldGUI
    {
        static private GUIContent s_SceneMismatch = EditorGUIUtility.TrTextContent("Scene mismatch (cross scene references not supported)");
        static private GUIContent s_TypeMismatch = EditorGUIUtility.TrTextContent("Type mismatch");
        static private GUIContent s_Select = EditorGUIUtility.TrTextContent("Select");

        const string k_PickerClosedCommand = "SearchPickerClosed";
        const string k_PickerUpdatedCommand = "SearchPickerUpdated";

        static EditorWindow s_DelegateWindow;
        static Object s_LastSelectedItem;
        static Object s_OriginalItem;
        public static readonly int objectFieldHash = "s_ObjectFieldHash".GetHashCode();
        static int s_LastPickerId;
        static bool s_LastSelectionWasCanceled;
        static int s_ModalUndoGroup = -1;

        // Takes object directly, no SerializedProperty.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, EditorGUI.ObjectFieldValidator validator, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None, GUIStyle style = null)
        {
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, null, validator, style != null ? style : EditorStyles.objectField, context, searchViewFlags);
        }

        // Takes SerializedProperty, no direct reference to object.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidator validator, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None, GUIStyle style = null)
        {
            return DoObjectField(position, dropRect, id, null, null, objType, property, validator, style != null ? style : EditorStyles.objectField, context, searchViewFlags);
        }

        internal enum ObjectFieldVisualType { IconAndText, LargePreview, MiniPreview }

        // when current event is mouse click, this function pings the object, or
        // if shift/control is pressed and object is a texture, pops up a large texture
        // preview window
        internal static void PingObjectOrShowPreviewOnClick(Object targetObject, Rect position)
        {
            if (targetObject == null)
                return;

            Event evt = Event.current;
            // ping object
            bool anyModifiersPressed = evt.shift || evt.control;
            if (!anyModifiersPressed)
            {
                EditorGUIUtility.PingObject(targetObject);
                return;
            }

            // show large object preview popup; right now only for textures
            if (targetObject is Texture)
            {
                Utils.PopupWindowWithoutFocus(new RectOffset(6, 3, 0, 3).Add(position), new ObjectPreviewPopup(targetObject));
            }
        }

        static Object AssignSelectedObject(SerializedProperty property, EditorGUI.ObjectFieldValidator validator, System.Type objectType, Event evt)
        {
            Object[] references = { s_LastSelectedItem };
            Object assigned = validator(references, objectType, property, EditorGUI.ObjectFieldValidatorOptions.None);

            // Assign the value
            if (property != null)
                property.objectReferenceValue = assigned;

            GUI.changed = true;
            evt.Use();
            return assigned;
        }

        static private Rect GetButtonRect(ObjectFieldVisualType visualType, Rect position)
        {
            switch (visualType)
            {
                case ObjectFieldVisualType.IconAndText:
                    return new Rect(position.xMax - 19, position.y, 19, position.height);
                case ObjectFieldVisualType.MiniPreview:
                    return new Rect(position.xMax - 14, position.y, 14, position.height);
                case ObjectFieldVisualType.LargePreview:
                    return new Rect(position.xMax - 36, position.yMax - 14, 36, 14);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static bool HasValidScript(UnityEngine.Object obj)
        {
            MonoScript script = Utils.MonoScriptFromScriptedObject(obj);
            if (script == null)
            {
                return false;
            }
            Type type = script.GetClass();
            if (type == null)
            {
                return false;
            }
            return true;
        }

        static bool ValidDroppedObject(Object[] references, System.Type objType, out string errorString)
        {
            errorString = "";
            if (references == null || references.Length == 0)
            {
                return true;
            }

            var reference = references[0];
            Object obj = EditorUtility.InstanceIDToObject(reference.GetInstanceID());
            if (obj is MonoBehaviour || obj is ScriptableObject)
            {
                if (!HasValidScript(obj))
                {
                    errorString = $"Type cannot be found: {reference.GetType()}. Containing file and class name must match.";
                    return false;
                }
            }
            return true;
        }

        // Timeline package is using this internal overload so can't remove until that's fixed.
        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidator validator, GUIStyle style, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None)
        {
            return DoObjectField(position, dropRect, id, objType, property, validator, context, searchViewFlags);
        }

        static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidator validator, GUIStyle style, SearchContext context, SearchViewFlags searchViewFlags = SearchViewFlags.None)
        {
            if (validator == null)
                validator = EditorGUI.ValidateObjectFieldAssignment;
            if (property != null)
                obj = property.objectReferenceValue;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && Utils.IsGUIClipEnabled() && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            bool hasThumbnail = EditorGUIUtility.HasObjectThumbnail(objType);

            // Determine visual type
            ObjectFieldVisualType visualType = ObjectFieldVisualType.IconAndText;
            if (hasThumbnail && position.height <= EditorGUI.kObjectFieldMiniThumbnailHeight && position.width <= EditorGUI.kObjectFieldMiniThumbnailWidth)
                visualType = ObjectFieldVisualType.MiniPreview;
            else if (hasThumbnail && position.height > EditorGUI.kSingleLineHeight)
                visualType = ObjectFieldVisualType.LargePreview;

            Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
            if (visualType == ObjectFieldVisualType.IconAndText)
                EditorGUIUtility.SetIconSize(new Vector2(12, 12));  // Have to be this small to fit inside a single line height ObjectField
            else if (visualType == ObjectFieldVisualType.LargePreview)
                EditorGUIUtility.SetIconSize(new Vector2(64, 64));

            if ((eventType == EventType.MouseDown && Event.current.button == 1 || eventType == EventType.ContextClick) &&
                position.Contains(Event.current.mousePosition))
            {
                var actualObject = property != null ? property.objectReferenceValue : obj;
                var contextMenu = new GenericMenu();

                if (EditorGUI.FillPropertyContextMenu(property, null, contextMenu) != null)
                    contextMenu.AddSeparator("");
                contextMenu.AddItem(Utils.GUIContentTemp("Properties..."), false, () => Utils.OpenPropertyEditor(actualObject));
                contextMenu.DropDown(position);
                Event.current.Use();
            }

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();

                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (eventType == EventType.DragPerform)
                    {
                        string errorString;
                        if (!ValidDroppedObject(DragAndDrop.objectReferences, objType, out errorString))
                        {
                            Object reference = DragAndDrop.objectReferences[0];
                            EditorUtility.DisplayDialog("Can't assign script", errorString, "OK");
                            break;
                        }
                    }

                    if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, property, EditorGUI.ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                if (property != null)
                                    property.objectReferenceValue = validatedObject;
                                else
                                    obj = validatedObject;

                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                            }
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        // Get button rect for Object Selector
                        Rect buttonRect = GetButtonRect(visualType, position);

                        EditorGUIUtility.editingTextField = false;

                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            if (GUI.enabled)
                            {
                                GUIUtility.keyboardControl = id;
                                ShowSearchPicker(context, searchViewFlags, property, property == null ? obj : null, id, evt, objType);
                            }
                        }
                        else
                        {
                            Object actualTargetObject = property != null ? property.objectReferenceValue : obj;
                            Component com = actualTargetObject as Component;
                            if (com)
                                actualTargetObject = com.gameObject;
                            if (EditorGUI.showMixedValue)
                                actualTargetObject = null;

                            // One click shows where the referenced object is, or pops up a preview
                            if (Event.current.clickCount == 1)
                            {
                                GUIUtility.keyboardControl = id;

                                PingObjectOrShowPreviewOnClick(actualTargetObject, position);
                                evt.Use();
                            }
                            // Double click opens the asset in external app or changes selection to referenced object
                            else if (Event.current.clickCount == 2)
                            {
                                if (actualTargetObject)
                                {
                                    AssetDatabase.OpenAsset(actualTargetObject);
                                    evt.Use();
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    string commandName = evt.commandName;
                    if (commandName == k_PickerUpdatedCommand && s_LastPickerId == id && GUIUtility.keyboardControl == id && (property == null || !Utils.SerializedPropertyIsScript(property)))
                        return AssignSelectedObject(property, validator, objType, evt);
                    else if (commandName == k_PickerClosedCommand && s_LastPickerId == id && GUIUtility.keyboardControl == id)
                    {
                        if (s_LastSelectionWasCanceled)
                        {
                            // User canceled object selection; don't apply
                            evt.Use();

                            // When we operate directly on objects, the undo system doesn't work.
                            // We added a hack that sets the s_LastSelectedItem to the original item
                            // when canceling with an object.
                            if (property == null)
                                return s_LastSelectedItem;

                            break;
                        }

                        // When property is script, it is not assigned on update, so assign it on close
                        if (property != null && Utils.SerializedPropertyIsScript(property))
                            return AssignSelectedObject(property, validator, objType, evt);

                        return property != null ? property.objectReferenceValue : obj;
                    }
                    else if (Utils.IsCommandDelete(evt.commandName) && GUIUtility.keyboardControl == id)
                    {
                        if (property != null)
                            property.objectReferenceValue = null;
                        else
                            obj = null;

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.ValidateCommand:
                    if (Utils.IsCommandDelete(evt.commandName) &&  GUIUtility.keyboardControl == id)
                    {
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.Backspace || (evt.keyCode == KeyCode.Delete && (evt.modifiers & EventModifiers.Shift) == 0))
                        {
                            if (property != null)
                                property.objectReferenceValue = null;
                            else
                                obj = null;

                            GUI.changed = true;
                            evt.Use();
                        }

                        // Apparently we have to check for the character being space instead of the keyCode,
                        // otherwise the Inspector will maximize upon pressing space.
                        if (Utils.MainActionKeyForControl(evt, id))
                        {
                            ShowSearchPicker(context, searchViewFlags, property, property == null ? obj : null, id, evt, objType);
                        }
                    }
                    break;
                case EventType.Repaint:
                    GUIContent temp;
                    if (EditorGUI.showMixedValue)
                    {
                        temp = EditorGUI.mixedValueContent;
                    }
                    else
                    {
                        // If obj or objType are both null, we have to rely on
                        // property.objectReferenceStringValue to display None/Missing and the
                        // correct type. But if not, EditorGUIUtility.ObjectContent is more reliable.
                        // It can take a more specific object type specified as argument into account,
                        // and it gets the icon at the same time.
                        if (obj == null && objType == null && property != null)
                        {
                            temp = Utils.GUIContentTemp(Utils.SerializedPropertyObjectReferenceStringValue(property));
                        }
                        else
                        {
                            // In order for ObjectContext to be able to distinguish between None/Missing,
                            // we need to supply an instanceID. For some reason, getting the instanceID
                            // from property.objectReferenceValue is not reliable, so we have to
                            // explicitly check property.objectReferenceInstanceIDValue if a property exists.
                            if (property != null)
                                temp = Utils.ObjectContent(obj, objType, property.objectReferenceInstanceIDValue);
                            else
                                temp = EditorGUIUtility.ObjectContent(obj, objType);
                        }

                        if (property != null)
                        {
                            if (obj != null)
                            {
                                Object[] references = { obj };
                                if (EditorSceneManager.preventCrossSceneReferences && EditorGUI.CheckForCrossSceneReferencing(obj, property.serializedObject.targetObject))
                                {
                                    if (!EditorApplication.isPlaying)
                                        temp = s_SceneMismatch;
                                    else
                                        temp.text = temp.text + string.Format(" ({0})", EditorGUI.GetGameObjectFromObject(obj).scene.name);
                                }
                                else if (validator(references, objType, property, EditorGUI.ObjectFieldValidatorOptions.ExactObjectTypeValidation) == null)
                                    temp = s_TypeMismatch;
                            }
                        }
                    }

                    switch (visualType)
                    {
                        case ObjectFieldVisualType.IconAndText:
                            EditorGUI.BeginHandleMixedValueContentColor();
                            style.Draw(position, temp, id, DragAndDrop.activeControlID == id, position.Contains(Event.current.mousePosition));

                            Rect buttonRect = Utils.objectFieldButton.margin.Remove(GetButtonRect(visualType, position));
                            Utils.objectFieldButton.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, buttonRect.Contains(Event.current.mousePosition));
                            EditorGUI.EndHandleMixedValueContentColor();
                            break;
                        case ObjectFieldVisualType.LargePreview:
                            DrawObjectFieldLargeThumb(position, id, obj, temp);
                            break;
                        case ObjectFieldVisualType.MiniPreview:
                            DrawObjectFieldMiniThumb(position, id, obj, temp);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
            }

            EditorGUIUtility.SetIconSize(oldIconSize);

            return obj;
        }

        private static void DrawObjectFieldLargeThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldThumb;
            thumbStyle.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id, position.Contains(Event.current.mousePosition));

            if (obj != null && !EditorGUI.showMixedValue)
            {
                Matrix4x4 guiMatrix = GUI.matrix; // Initial matrix is saved in order to be able to reset it to default
                bool isSprite = obj is Sprite;
                bool alphaIsTransparencyTex2D = (obj is Texture2D && (obj as Texture2D).alphaIsTransparency);
                Rect thumbRect = thumbStyle.padding.Remove(position);

                Texture2D t2d = AssetPreview.GetAssetPreview(obj);
                if (t2d != null)
                {
                    // A checkerboard background is drawn behind transparent textures (for visibility)
                    if (isSprite || t2d.alphaIsTransparency || alphaIsTransparencyTex2D)
                        GUI.DrawTexture(thumbRect, EditorGUI.transparentCheckerTexture, ScaleMode.StretchToFill, false);

                    // Draw asset preview (scaled to fit inside the frame)
                    GUIUtility.ScaleAroundPivot(thumbRect.size / position.size, thumbRect.position);
                    GUIStyle.none.Draw(thumbRect, t2d, false, false, false, false);
                    GUI.matrix = guiMatrix;
                }
                else
                {
                    // Preview not loaded -> Draw icon
                    if (isSprite || alphaIsTransparencyTex2D)
                    {
                        // A checkerboard background is drawn behind transparent textures (for visibility)
                        GUI.DrawTexture(thumbRect, EditorGUI.transparentCheckerTexture, ScaleMode.StretchToFill, false);
                        GUI.DrawTexture(thumbRect, content.image, ScaleMode.StretchToFill, true);
                    }
                    else
                        EditorGUI.DrawPreviewTexture(thumbRect, content.image);

                    // Keep repainting until the object field has a proper preview
                    HandleUtility.Repaint();
                }
            }
            else
            {
                GUIStyle s2 = thumbStyle.name + "Overlay";
                EditorGUI.BeginHandleMixedValueContentColor();

                s2.Draw(position, content, id);
                EditorGUI.EndHandleMixedValueContentColor();
            }
            GUIStyle s3 = thumbStyle.name + "Overlay2";
            s3.Draw(position, s_Select, id);
        }

        private static void DrawObjectFieldMiniThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldMiniThumb;
            position.width = EditorGUI.kObjectFieldMiniThumbnailWidth;
            EditorGUI.BeginHandleMixedValueContentColor();
            bool hover = obj != null; // we use hover texture for enhancing the border if we have a reference
            bool on =  DragAndDrop.activeControlID == id;
            bool keyFocus = GUIUtility.keyboardControl == id;
            thumbStyle.Draw(position, hover, false, on, keyFocus);
            EditorGUI.EndHandleMixedValueContentColor();

            if (obj != null && !EditorGUI.showMixedValue)
            {
                Rect thumbRect = new Rect(position.x + 1, position.y + 1, position.height - 2, position.height - 2); // subtract 1 px border
                Texture2D t2d = content.image as Texture2D;
                if (t2d != null && t2d.alphaIsTransparency)
                    EditorGUI.DrawTextureTransparent(thumbRect, t2d);
                else
                    EditorGUI.DrawPreviewTexture(thumbRect, content.image);

                // Tooltip
                if (thumbRect.Contains(Event.current.mousePosition))
                    GUI.Label(thumbRect, Search.Utils.GUIContentTemp(string.Empty, "Ctrl + Click to show preview"));
            }
        }

        internal static Object DoDropField(Rect position, int id, System.Type objType, EditorGUI.ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            if (validator == null)
                validator = EditorGUI.ValidateObjectFieldAssignment;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && Utils.IsGUIClipEnabled() && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (position.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, null, EditorGUI.ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                                validatedObject = null;
                        }

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                                return validatedObject;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                                Event.current.Use();
                            }
                        }
                    }
                    break;
                case EventType.Repaint:
                    style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
                    break;
            }
            return null;
        }

        static void ShowSearchPicker(SearchContext context, SearchViewFlags searchViewFlags, SerializedProperty property, Object originalObject, int id, Event evt, Type objType)
        {
            s_DelegateWindow = EditorWindow.focusedWindow;
            s_ModalUndoGroup = Undo.GetCurrentGroup();
            s_OriginalItem = originalObject;
            var searchViewState = new SearchViewState(context,
                (item, canceled) => SendSelectionEvent(item, canceled, id),
                item => SendTrackingEvent(item, id), objType.ToString(), objType)
                    .SetSearchViewFlags(searchViewFlags);
            if (property != null)
            { 
                searchViewState.title = $"{property.displayName} ({objType.Name})";
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue)
                    searchViewState.selectedIds = new int[] { property.objectReferenceValue.GetInstanceID() };
            }
            else if (originalObject)
                searchViewState.selectedIds = new int[] { originalObject.GetInstanceID() };
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, context.searchText, "object", "objectfield");
            SearchService.ShowPicker(searchViewState);

            evt.Use();
            GUIUtility.ExitGUI();
        }

        static void SendSelectionEvent(Object item, bool canceled, int id)
        {
            s_LastPickerId = id;
            s_LastSelectedItem = item;
            s_LastSelectionWasCanceled = canceled;

            if (canceled)
            {
                if (s_OriginalItem)
                {
                    s_LastSelectedItem = s_OriginalItem;
                    s_OriginalItem = null;
                }
                Undo.RevertAllDownToGroup(s_ModalUndoGroup);
            }
            Undo.CollapseUndoOperations(s_ModalUndoGroup);

            SendEvent(k_PickerClosedCommand, false);
        }

        static void SendTrackingEvent(Object item, int id)
        {
            s_LastPickerId = id;
            s_LastSelectedItem = item;

            SendEvent(k_PickerUpdatedCommand, false);
        }

        static void SendEvent(string eventName, bool exitGUI)
        {
            if (s_DelegateWindow && s_DelegateWindow.m_Parent != null)
            {
                Event e = EditorGUIUtility.CommandEvent(eventName);

                try
                {
                    s_DelegateWindow.SendEvent(e);
                }
                finally
                {
                    if (exitGUI)
                        GUIUtility.ExitGUI();
                }
            }
        }

        class ObjectPreviewPopup : PopupWindowContent
        {
            readonly Editor m_Editor;
            readonly GUIContent m_ObjectName;
            const float kToolbarHeight = 22f;

            internal class Styles
            {
                public readonly GUIStyle toolbar = "preToolbar";
                public readonly GUIStyle toolbarText = "ToolbarBoldLabel";
                public GUIStyle background = "preBackground";
            }
            Styles s_Styles;

            public ObjectPreviewPopup(Object previewObject)
            {
                if (previewObject == null)
                {
                    Debug.LogError("ObjectPreviewPopup: Check object is not null, before trying to show it!");
                    return;
                }
                m_ObjectName = new GUIContent(previewObject.name, AssetDatabase.GetAssetPath(previewObject));   // Show path as tooltip on label
                m_Editor = Editor.CreateEditor(previewObject);
            }

            public override void OnClose()
            {
                if (m_Editor != null)
                    Editor.DestroyImmediate(m_Editor);
            }

            public override void OnGUI(Rect rect)
            {
                if (m_Editor == null)
                {
                    editorWindow.Close();
                    return;
                }

                if (s_Styles == null)
                    s_Styles = new Styles();

                // Toolbar
                Rect toolbarRect = Utils.BeginHorizontal(GUIContent.none, s_Styles.toolbar, GUILayout.Height(kToolbarHeight));
                {
                    GUILayout.FlexibleSpace();
                    Rect contentRect = EditorGUILayout.BeginHorizontal();
                    m_Editor.OnPreviewSettings();
                    EditorGUILayout.EndHorizontal();

                    const float kPadding = 5f;
                    Rect labelRect = new Rect(toolbarRect.x + kPadding, toolbarRect.y, toolbarRect.width - contentRect.width - 2 * kPadding, toolbarRect.height);
                    Vector2 labelSize = s_Styles.toolbarText.CalcSize(m_ObjectName);
                    labelRect.width = Mathf.Min(labelRect.width, labelSize.x);
                    m_ObjectName.tooltip = m_ObjectName.text;
                    GUI.Label(labelRect, m_ObjectName, s_Styles.toolbarText);
                }
                EditorGUILayout.EndHorizontal();

                // Object preview
                Rect previewRect = new Rect(rect.x, rect.y + kToolbarHeight, rect.width, rect.height - kToolbarHeight);
                m_Editor.OnPreviewGUI(previewRect, s_Styles.background);
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(600f, 300f + kToolbarHeight);
            }
        }

    }
}
