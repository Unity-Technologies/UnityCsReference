// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class UnityEventItem : VisualElement
    {
        public struct PropertyData
        {
            public SerializedProperty listener;
            public SerializedProperty mode;
            public SerializedProperty arguments;
            public SerializedProperty callState;
            public SerializedProperty listenerTarget;
            public SerializedProperty methodName;
            public SerializedProperty objectArgument;
        }

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new UnityEventItem();
        }

        // uss names
        internal const string kUssClassName = "unity-event";
        internal const string kLeftColumnClassName = kUssClassName + "__left-column";
        internal const string kRightColumnClassName = kUssClassName + "__right-column";
        internal const string kListViewItemClassName = kUssClassName + "__list-view-item";

        internal const string kCallStateDropdownName = kUssClassName + "__call-state-dropdown";
        internal const string kListenerTargetName = kUssClassName + "__listener-target";
        internal const string kFunctionDropdownName = kUssClassName + "__function-dropdown";
        internal const string kParameterPropertyName = kUssClassName + "__parameter-property";
        internal const string kObjectParameterName = kUssClassName + "__object-parameter";

        PropertyData m_PropertyData;

        public PropertyField callStateDropdown
        {
            get;
            private set;
        }

        public PropertyField listenerTarget
        {
            get;
            private set;
        }

        public DropdownField functionDropdown
        {
            get;
            private set;
        }

        public PropertyField parameterProperty
        {
            get;
            private set;
        }

        public ObjectField objectParameter
        {
            get;
            private set;
        }

        public Func<PropertyData, GenericMenu> createMenuCallback
        {
            get;
            set;
        }

        public Func<PropertyData, string> formatSelectedValueCallback
        {
            get;
            set;
        }

        public Func<PropertyData, SerializedProperty> getArgumentCallback
        {
            get;
            set;
        }

        public UnityEventItem()
        {
            AddToClassList(kListViewItemClassName);

            var leftColumn = new VisualElement();
            leftColumn.AddToClassList(kLeftColumnClassName);
            Add(leftColumn);

            var rightColumn = new VisualElement();
            rightColumn.AddToClassList(kRightColumnClassName);
            Add(rightColumn);

            callStateDropdown = new PropertyField();
            callStateDropdown.label = "";
            callStateDropdown.name = kCallStateDropdownName;
            leftColumn.Add(callStateDropdown);

            listenerTarget = new PropertyField();
            listenerTarget.label = "";
            listenerTarget.name = kListenerTargetName;
            leftColumn.Add(listenerTarget);
            listenerTarget.RegisterCallback<ChangeEvent<UnityEngine.Object>>((e) =>
            {
                var isTargetValid = e.newValue != null;

                if (!isTargetValid)
                {
                    functionDropdown.value = null;
                }

                functionDropdown.SetEnabled(isTargetValid);

                UpdateParameterField();
            });

            functionDropdown = new DropdownField();
            functionDropdown.name = kFunctionDropdownName;
            rightColumn.Add(functionDropdown);
            functionDropdown.createMenuCallback = () =>
            {
                var genericMenu = createMenuCallback.Invoke(m_PropertyData);
                var osMenu = new GenericOSMenu(genericMenu);
                return osMenu;
            };
            functionDropdown.formatSelectedValueCallback = _ =>
            {
                return formatSelectedValueCallback?.Invoke(m_PropertyData);
            };
            functionDropdown.RegisterValueChangedCallback(_ =>
            {
                UpdateParameterField();
            });

            parameterProperty = new PropertyField();
            parameterProperty.label = "";
            parameterProperty.name = kParameterPropertyName;
            rightColumn.Add(parameterProperty);

            objectParameter = new ObjectField();
            objectParameter.name = kObjectParameterName;
            objectParameter.allowSceneObjects = true;
            rightColumn.Add(objectParameter);
        }

        internal void BindFields(PropertyData data)
        {
            m_PropertyData = data;
            callStateDropdown.BindProperty(m_PropertyData.callState);
            listenerTarget.BindProperty(m_PropertyData.listenerTarget);
            functionDropdown.BindProperty(m_PropertyData.methodName);
            objectParameter.BindProperty(m_PropertyData.objectArgument);

            UpdateParameterField();
        }

        internal void UpdateParameterField()
        {
            var modeEnum = (PersistentListenerMode)m_PropertyData.mode.enumValueIndex;
            var argument = getArgumentCallback.Invoke(m_PropertyData);

            //only allow argument if we have a valid target / method
            if (m_PropertyData.listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(m_PropertyData.methodName.stringValue))
            {
                modeEnum = PersistentListenerMode.Void;
            }

            if (modeEnum == PersistentListenerMode.Void || modeEnum == PersistentListenerMode.EventDefined)
            {
                parameterProperty.style.display = DisplayStyle.None;
                objectParameter.style.display = DisplayStyle.None;
            }
            else if (modeEnum == PersistentListenerMode.Object)
            {
                parameterProperty.style.display = DisplayStyle.None;
                objectParameter.style.display = DisplayStyle.Flex;

                var desiredArgTypeName = m_PropertyData.arguments.FindPropertyRelative(UnityEventDrawer.kObjectArgumentAssemblyTypeName).stringValue;
                var desiredType = typeof(UnityEngine.Object);

                if (!string.IsNullOrEmpty(desiredArgTypeName))
                {
                    desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(UnityEngine.Object);
                }

                objectParameter.objectType = desiredType;
                objectParameter.value = argument.objectReferenceValue;
           }
            else
            {
                parameterProperty.BindProperty(argument);
                objectParameter.style.display = DisplayStyle.None;
                parameterProperty.style.display = DisplayStyle.Flex;
            }
        }
    }
}
