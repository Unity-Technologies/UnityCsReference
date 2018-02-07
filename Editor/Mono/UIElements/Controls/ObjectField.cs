// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    public class ObjectField : VisualElement, INotifyValueChanged<Object>
    {
        private Object m_Value;
        public Object value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    m_ObjectFieldDisplay.Update();
                }
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

                if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                    OnMouseDown();
                else if (evt.GetEventTypeId() == IMGUIEvent.TypeId())
                    OnIMGUI(evt);
                else if (evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
                    OnMouseLeave();
            }

            private void OnMouseLeave()
            {
                // Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
                RemoveFromClassList("acceptDrop");
            }

            private void OnMouseDown()
            {
                Object actualTargetObject = m_ObjectField.value;
                Component com = actualTargetObject as Component;
                if (com)
                    actualTargetObject = com.gameObject;

                // One click shows where the referenced object is, or pops up a preview
                if (Event.current.clickCount == 1)
                {
                    PingObject(actualTargetObject);
                }
                // Double click opens the asset in external app or changes selection to referenced object
                else if (Event.current.clickCount == 2)
                {
                    if (actualTargetObject)
                    {
                        AssetDatabase.OpenAsset(actualTargetObject);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            private void OnIMGUI(EventBase evt)
            {
                if (evt.imguiEvent.type == EventType.DragUpdated || evt.imguiEvent.type == EventType.DragPerform)
                {
                    Object[] references = DragAndDrop.objectReferences;
                    Object validatedObject = EditorGUI.ValidateObjectFieldAssignment(references, m_ObjectField.objectType, null, EditorGUI.ObjectFieldValidatorOptions.None);

                    if (validatedObject != null)
                    {
                        // If scene objects are not allowed and object is a scene object then clear
                        if (!m_ObjectField.allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                            validatedObject = null;
                    }

                    if (validatedObject != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (evt.imguiEvent.type == EventType.DragPerform)
                        {
                            m_ObjectField.SetValueAndNotify(validatedObject);

                            DragAndDrop.AcceptDrag();

                            evt.StopPropagation();

                            RemoveFromClassList("acceptDrop");
                        }
                        else
                        {
                            AddToClassList("acceptDrop");
                        }
                    }
                }
            }

            private void PingObject(Object targetObject)
            {
                if (targetObject == null)
                    return;

                Event evt = Event.current;
                // ping object
                bool anyModifiersPressed = evt.shift || evt.control;
                if (!anyModifiersPressed)
                {
                    EditorGUIUtility.PingObject(targetObject);
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

                if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                    OnMouseDown();
            }

            private void OnMouseDown()
            {
                ObjectSelector.get.Show(m_ObjectField.value, m_ObjectField.objectType, null, m_ObjectField.allowSceneObjects, null, m_ObjectField.OnObjectChanged, m_ObjectField.OnObjectChanged);
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

        public void SetValueAndNotify(Object newValue)
        {
            if (newValue != value)
            {
                using (ChangeEvent<Object> evt = ChangeEvent<Object>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<Object>> callback)
        {
            RegisterCallback(callback);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_ObjectFieldDisplay.Focus();
        }

        private void OnObjectChanged(Object obj)
        {
            SetValueAndNotify(obj);
        }
    }
}
