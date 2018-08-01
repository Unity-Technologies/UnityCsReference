// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    public class ObjectField : BaseField<Object>
    {
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Object>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_AllowSceneObjects = new UxmlBoolAttributeDescription { name = "allow-scene-objects", obsoleteNames = new[] { "allowSceneObjects" }, defaultValue = true };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ObjectField)ve).allowSceneObjects = m_AllowSceneObjects.GetValueFromBag(bag, cc);
            }
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
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

            public ObjectFieldDisplay(ObjectField objectField)
            {
                m_ObjectIcon = new Image {scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore};
                m_ObjectLabel = new Label {pickingMode = PickingMode.Ignore};
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

            protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    OnMouseDown(evt as MouseDownEvent);
                else if ((evt as KeyDownEvent)?.character == '\n')
                    OnKeyboardEnter();
                else if (evt.GetEventTypeId() == DragUpdatedEvent.TypeId())
                    OnDragUpdated(evt);
                else if (evt.GetEventTypeId() == DragPerformEvent.TypeId())
                    OnDragPerform(evt);
                else if (evt.GetEventTypeId() == DragLeaveEvent.TypeId())
                    OnDragLeave();
            }

            private void OnDragLeave()
            {
                // Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
                RemoveFromClassList("acceptDrop");
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
                    AddToClassList("acceptDrop");

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
                    RemoveFromClassList("acceptDrop");

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

            protected internal override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse)
                    m_ObjectField.ShowObjectSelector();
            }
        }

        public override int focusIndex
        {
            get { return base.focusIndex; }
            set
            {
                base.focusIndex = value;
                if (m_ObjectFieldDisplay != null)
                {
                    m_ObjectFieldDisplay.focusIndex = value;
                }
            }
        }

        private readonly ObjectFieldDisplay m_ObjectFieldDisplay;

        public ObjectField()
        {
            allowSceneObjects = true;

            m_ObjectFieldDisplay = new ObjectFieldDisplay(this) {focusIndex = 0};
            var objectSelector = new ObjectFieldSelector(this);

            Add(m_ObjectFieldDisplay);
            Add(objectSelector);
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public override void SetValueAndNotify(Object newValue)
        {
            if (newValue != value)
            {
                value = newValue;
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_ObjectFieldDisplay.Focus();
        }

        private void OnObjectChanged(Object obj)
        {
            value = obj;
        }

        internal void ShowObjectSelector()
        {
            ObjectSelector.get.Show(value, objectType, null, allowSceneObjects, null, OnObjectChanged, OnObjectChanged);
        }
    }
}
