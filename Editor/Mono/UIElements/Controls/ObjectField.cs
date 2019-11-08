// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    public class ObjectField : BaseField<Object>
    {
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_AllowSceneObjects = new UxmlBoolAttributeDescription { name = "allow-scene-objects", defaultValue = true };
            UxmlTypeAttributeDescription<Object> m_ObjectType = new UxmlTypeAttributeDescription<Object> { name = "type" };

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

        public bool allowSceneObjects { get; set; }

        private class ObjectFieldDisplay : VisualElement
        {
            private readonly ObjectField m_ObjectField;
            private readonly Image m_ObjectIcon;
            private readonly Label m_ObjectLabel;

            public static readonly string ussClassName = "unity-object-field-display";
            public static readonly string iconUssClassName = ussClassName + "__icon";
            public static readonly string labelUssClassName = ussClassName + "__label";
            public static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";


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

        public new static readonly string ussClassName = "unity-object-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string objectUssClassName = ussClassName + "__object";
        public static readonly string selectorUssClassName = ussClassName + "__selector";

        public ObjectField()
            : this(null) {}

        public ObjectField(string label)
            : base(label, null)
        {
            visualInput.focusable = false;
            labelElement.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            allowSceneObjects = true;

            m_ObjectFieldDisplay = new ObjectFieldDisplay(this) {focusable = true};
            m_ObjectFieldDisplay.AddToClassList(objectUssClassName);
            var objectSelector = new ObjectFieldSelector(this);
            objectSelector.AddToClassList(selectorUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectFieldDisplay);
            visualInput.Add(objectSelector);
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
