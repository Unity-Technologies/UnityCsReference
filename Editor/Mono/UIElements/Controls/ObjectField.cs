// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field to receive any object type. For more information, refer to [[wiki:UIE-uxml-element-ObjectField|UXML element ObjectField]].
    /// </summary>
    public class ObjectField : BaseField<Object>
    {
        internal static readonly BindingId objectTypeProperty = nameof(objectType);
        internal static readonly BindingId allowSceneObjectsProperty = nameof(allowSceneObjects);

        private event Action m_OnObjectSelectorShow = () => { };

        internal event Action onObjectSelectorShow
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            add => m_OnObjectSelectorShow += value;
            remove => m_OnObjectSelectorShow -= value;
        }

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Object>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool allowSceneObjects;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags allowSceneObjects_UxmlAttributeFlags;
            [UxmlAttribute("type"), UxmlTypeReference(typeof(Object))]
            [SerializeField] string objectType;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags objectType_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new ObjectField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ObjectField)obj;
                if (ShouldWriteAttributeValue(allowSceneObjects_UxmlAttributeFlags))
                    e.allowSceneObjects = allowSceneObjects;
                if (ShouldWriteAttributeValue(objectType_UxmlAttributeFlags))
                    e.objectType = UxmlUtility.ParseType(objectType, typeof(Object));
            }
        }

        /// <summary>
        /// Instantiates an <see cref="ObjectField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ObjectField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_AllowSceneObjects = new UxmlBoolAttributeDescription { name = "allow-scene-objects", defaultValue = true };
            UxmlTypeAttributeDescription<Object> m_ObjectType = new UxmlTypeAttributeDescription<Object> { name = "type", defaultValue = typeof(Object)};

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
                UpdateDisplay();
            }
        }

        private Type m_objectType;

        /// <summary>
        /// The type of the objects that can be assigned.
        /// </summary>
        [CreateProperty]
        public Type objectType
        {
            get { return m_objectType; }
            set
            {
                if (m_objectType != value)
                {
                    m_objectType = value;
                    UpdateDisplay();
                    NotifyPropertyChanged(objectTypeProperty);
                }
            }
        }

        internal void SetObjectTypeWithoutDisplayUpdate(Type type)
        {
            m_objectType = type;
        }

        private bool m_AllowSceneObjects;

        /// <summary>
        /// Allows scene objects to be assigned to the field.
        /// </summary>
        [CreateProperty]
        public bool allowSceneObjects
        {
            get => m_AllowSceneObjects;
            set
            {
                if (m_AllowSceneObjects == value)
                    return;
                m_AllowSceneObjects = value;
                NotifyPropertyChanged(allowSceneObjectsProperty);
            }
        }

        protected override void UpdateMixedValueContent()
        {
            m_ObjectFieldDisplay?.ShowMixedValue(showMixedValue);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal virtual void UpdateDisplay()
        {
            UpdateMixedValueContent();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal class ObjectFieldDisplay : VisualElement
        {
            private readonly ObjectField m_ObjectField;
            private readonly Image m_ObjectIcon;
            private readonly Label m_ObjectLabel;

            static readonly string ussClassName = "unity-object-field-display";
            static readonly string iconUssClassName = ussClassName + "__icon";
            internal static readonly string labelUssClassName = ussClassName + "__label";
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
                // While building editor resources ObjectField are instantiated to serialize default values in
                // the Uxml asset. If EditorGUIUtility.ObjectContent is called during that time the editor will crash.
                if (Application.isBuildingEditorResources)
                    return;

                var property = m_ObjectField.GetProperty(serializedPropertyKey) as SerializedProperty;
                // UUM-53334, need to check if property is still valid before updating
                if (property != null && !property.isValid)
                {
                    m_ObjectField.SetProperty(serializedPropertyKey, null);
                    return;
                }
                var content = EditorGUIUtility.ObjectContent(m_ObjectField.value, m_ObjectField.objectType, property);
                m_ObjectIcon.image = content.image;
                m_ObjectLabel.text = content.text;
            }

            [EventInterest(typeof(MouseDownEvent), typeof(KeyDownEvent),
                typeof(DragUpdatedEvent), typeof(DragPerformEvent), typeof(DragLeaveEvent))]
            protected override void HandleEventBubbleUp(EventBase evt)
            {
                base.HandleEventBubbleUp(evt);

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

                // Drag events should not reach this point but we are adding this check for extra safety, because
                // it might cause a crash depending on the object type. See case 1416878.
                if (!enabledInHierarchy)
                    return;

                if (evt.eventTypeId == DragUpdatedEvent.TypeId())
                    OnDragUpdated(evt);
                else if (evt.eventTypeId == DragPerformEvent.TypeId())
                    OnDragPerform(evt);
                else if (evt.eventTypeId == DragLeaveEvent.TypeId())
                    OnDragLeave();
            }

            [EventInterest(typeof(MouseDownEvent))]
            internal override void HandleEventBubbleUpDisabled(EventBase evt)
            {
                base.HandleEventBubbleUpDisabled(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    OnMouseDown(evt as MouseDownEvent);
            }

            private void OnDragLeave()
            {
                // Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
                EnableInClassList(acceptDropVariantUssClassName, false);
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
                m_ObjectField.SetProperty(serializedPropertyKey, null);
                m_ObjectField.value = null;
            }

            private Object DNDValidateObject()
            {
                var references = DragAndDrop.objectReferences;
                var property = m_ObjectField.GetProperty(serializedPropertyKey) as SerializedProperty;
                var validatedObject = EditorGUI.ValidateObjectFieldAssignment(references, m_ObjectField.objectType, property, EditorGUI.ObjectFieldValidatorOptions.None);

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
                    EnableInClassList(acceptDropVariantUssClassName, true);

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
            protected override void HandleEventBubbleUp(EventBase evt)
            {
                base.HandleEventBubbleUp(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                {
                    m_ObjectField.ShowObjectSelector();
                    evt.StopPropagation();
                }
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

        internal static readonly PropertyName serializedPropertyKey = new PropertyName("--unity-object-field-serialized-property");

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
            m_objectType = typeof(Object);

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
            m_OnProjectOrHierarchyChangedCallback = UpdateDisplay;
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

        internal void OnObjectChanged(Object obj)
        {
            value = TryReadComponentFromGameObject(obj, objectType);
        }

        internal void ShowObjectSelector()
        {
            // Since we have nothing useful to do on the object selector closing action, we just do not assign any callback
            // All the object changes will be notified through the OnObjectChanged and a "cancellation" (Escape key) on the ObjectSelector is calling the closing callback without any good object
            ObjectSelector.get.Show(value, objectType, null, allowSceneObjects, null, null, OnObjectChanged);
            m_OnObjectSelectorShow?.Invoke();
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
