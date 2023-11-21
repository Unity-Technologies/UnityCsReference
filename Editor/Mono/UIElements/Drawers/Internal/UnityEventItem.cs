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

            functionDropdown = new DropdownField();
            functionDropdown.name = kFunctionDropdownName;
            rightColumn.Add(functionDropdown);

            parameterProperty = new PropertyField();
            parameterProperty.label = "";
            parameterProperty.name = kParameterPropertyName;
            rightColumn.Add(parameterProperty);

            objectParameter = new ObjectField();
            objectParameter.name = kObjectParameterName;
            objectParameter.allowSceneObjects = true;
            rightColumn.Add(objectParameter);
        }

        internal void BindFields(UnityEventDrawer.PropertyData propertyData, Func<GenericMenu> createMenuCallback, Func<string, string> formatSelectedValueCallback, Func<SerializedProperty> getArgumentCallback)
        {
            callStateDropdown.BindProperty(propertyData.callState);
            listenerTarget.BindProperty(propertyData.listenerTarget);
            functionDropdown.BindProperty(propertyData.methodName);
            objectParameter.BindProperty(propertyData.objectArgument);

            listenerTarget.RegisterCallback<ChangeEvent<UnityEngine.Object>>((e) =>
            {
                var isTargetValid = e.newValue != null;

                if (!isTargetValid)
                {
                    functionDropdown.value = null;
                }

                functionDropdown.SetEnabled(isTargetValid);

                UpdateParameterField(propertyData, getArgumentCallback);
            });

            functionDropdown.RegisterValueChangedCallback((e) =>
            {
                UpdateParameterField(propertyData, getArgumentCallback);
            });

            functionDropdown.createMenuCallback = () =>
            {
                var genericMenu = createMenuCallback.Invoke();
                var osMenu = new GenericOSMenu(genericMenu);
                return osMenu;
            };

            functionDropdown.formatSelectedValueCallback = formatSelectedValueCallback;

            UpdateParameterField(propertyData, getArgumentCallback);
        }

        internal void UpdateParameterField(UnityEventDrawer.PropertyData propertyData, Func<SerializedProperty> getArgumentCallback)
        {
            var modeEnum = (PersistentListenerMode)propertyData.mode.enumValueIndex;
            var argument = getArgumentCallback.Invoke();

            //only allow argument if we have a valid target / method
            if (propertyData.listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(propertyData.methodName.stringValue))
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

                var desiredArgTypeName = propertyData.arguments.FindPropertyRelative(UnityEventDrawer.kObjectArgumentAssemblyTypeName).stringValue;
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
