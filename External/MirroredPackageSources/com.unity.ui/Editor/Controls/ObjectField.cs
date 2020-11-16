using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field to receive any object type.
    /// </summary>
    public class ObjectField : BaseField<Object>
    {
        /// <summary>
        /// Instantiates an <see cref="ObjectField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ObjectField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_AllowSceneObjects = new UxmlBoolAttributeDescription { name = "allow-scene-objects", defaultValue = true };
            UxmlTypeAttributeDescription<Object> m_ObjectType = new UxmlTypeAttributeDescription<Object> { name = "type" };

            /// <summary>
            /// Initialize <see cref="ObjectField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ObjectField)ve).allowSceneObjects = m_AllowSceneObjects.GetValueFromBag(bag, cc);
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

        private Type m_objectType;

        /// <summary>
        /// The type of the objects that can be assigned.
        /// </summary>
        public Type objectType
        {
            get { return m_objectType; }
            set
            {
                if (m_objectType != value)
                {
                    m_objectType = value;
                    m_ObjectFieldDisplay.Update();
                }
            }
        }

        /// <summary>
        /// Allows scene objects to be assigned to the field.
        /// </summary>
        public bool allowSceneObjects { get; set; }

        protected override void UpdateMixedValueContent()
        {
            m_ObjectFieldDisplay?.ShowMixedValue(showMixedValue);
        }

        private class ObjectFieldDisplay : VisualElement
        {
            private readonly ObjectField m_ObjectField;
            private readonly Image m_ObjectIcon;
            private readonly Label m_ObjectLabel;

            static readonly string ussClassName = "unity-object-field-display";
            static readonly string iconUssClassName = ussClassName + "__icon";
            static readonly string labelUssClassName = ussClassName + "__label";
            static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";

            internal void ShowMixedValue(bool show)
            {
                if (show)
                {
                    m_ObjectLabel.text = mixedValueString;
                    m_ObjectLabel.AddToClassList(mixedValueLabelUssClassName);
                    m_ObjectIcon.image = null;
                }
                else
                {
                    m_ObjectLabel.RemoveFromClassList(mixedValueLabelUssClassName);
                    Update();
                }
            }

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

                if (validatedObject != null)
                {
                    // If scene objects are not allowed and object is a scene object then clear
                    if (!m_ObjectField.allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                        validatedObject = null;
                }
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
        public new static readonly string ussClassName = "unity-object-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of object elements in elements of this type.
        /// </summary>
        public static readonly string objectUssClassName = ussClassName + "__object";
        /// <summary>
        /// USS class name of selector elements in elements of this type.
        /// </summary>
        public static readonly string selectorUssClassName = ussClassName + "__selector";

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

            allowSceneObjects = true;

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

        internal void ShowObjectSelector()
        {
            // Since we have nothing useful to do on the object selector closing action, we just do not assign any callback
            // All the object changes will be notified through the OnObjectChanged and a "cancellation" (Escape key) on the ObjectSelector is calling the closing callback without any good object
            ObjectSelector.get.Show(value, objectType, null, allowSceneObjects, null, null, OnObjectChanged);
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
    }
}
