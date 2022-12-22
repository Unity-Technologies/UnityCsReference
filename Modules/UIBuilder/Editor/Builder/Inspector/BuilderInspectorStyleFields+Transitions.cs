// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    partial class BuilderInspectorStyleFields
    {
        static class TransitionConstants
        {
            public static readonly string Transition = StylePropertyId.Transition.UssName();
            public static readonly string Property = StylePropertyId.TransitionProperty.UssName();
            public static readonly string Duration = StylePropertyId.TransitionDuration.UssName();
            public static readonly string TimingFunction = StylePropertyId.TransitionTimingFunction.UssName();
            public static readonly string Delay = StylePropertyId.TransitionDelay.UssName();
            public const string CurrentOverrides = "Current Overrides";
        }


        public void BindStyleField(BuilderStyleRow styleRow, TransitionsListView transitionsListView)
        {
            var transitionList = GetOrCreateFieldListForStyleName(TransitionConstants.Transition);
            transitionList.Add(transitionsListView);

            transitionsListView.RegisterCallback<TransitionAddedEvent, TransitionsListView>(OnTransitionAdded, transitionsListView);
            transitionsListView.RegisterCallback<TransitionChangedEvent, TransitionsListView>(OnTransitionChanged, transitionsListView);
            transitionsListView.RegisterCallback<TransitionRemovedEvent, TransitionsListView>(OnTransitionRemoved, transitionsListView);
        }

        public void RefreshStyleField(TransitionsListView transitionsListView)
        {
            var computedData = currentVisualElement.computedStyle.transitionData.Read();
            using var setData = GetBuilderTransitionData();

            var max = Mathf.Max(computedData.MaxCount(), setData.MaxCount());
            var builderTransitions = ListPool<BuilderTransition>.Get();
            try
            {
                for (var i = 0; i < max; i++)
                    builderTransitions.Add(default);

                ClearTransitionStyleFieldLists();

                RefreshTransitionStyleProperties(builderTransitions, setData, computedData);

                transitionsListView.TrimToCount(max);
                for (var i = 0; i < builderTransitions.Count; ++i)
                {
                    var transition = builderTransitions[i];
                    var foldoutTransitionField = GetPooledTransitionField(transitionsListView, transition, i);
                    foldoutTransitionField.index = i;

                    SetTransitionVariableEditors(foldoutTransitionField, i);
                    UpdateFieldStatus(foldoutTransitionField.propertyField, setData.transitionProperty.styleProperty);
                    UpdateFieldStatus(foldoutTransitionField.durationField, setData.transitionDuration.styleProperty);
                    UpdateFieldStatus(foldoutTransitionField.timingFunctionField, setData.transitionTimingFunction.styleProperty);
                    UpdateFieldStatus(foldoutTransitionField.delayField, setData.transitionDelay.styleProperty);
                }
            }
            finally
            {
                ListPool<BuilderTransition>.Release(builderTransitions);
            }

            transitionsListView.Refresh(setData.GetOverrides(), setData.GetKeywords());
            updateStyleCategoryFoldoutOverrides();
        }

        void ClearTransitionStyleFieldLists()
        {
            GetOrCreateFieldListForStyleName(TransitionConstants.Property).Clear();
            GetOrCreateFieldListForStyleName(TransitionConstants.Duration).Clear();
            GetOrCreateFieldListForStyleName(TransitionConstants.TimingFunction).Clear();
            GetOrCreateFieldListForStyleName(TransitionConstants.Delay).Clear();
        }

        static void RefreshTransitionStyleProperties(List<BuilderTransition> transitions, BuilderTransitionData setData, TransitionData computedData)
        {
            RefreshTransitionProperty(transitions, setData.transitionProperty, computedData.transitionProperty);
            RefreshTransitionDuration(transitions, setData.transitionDuration, computedData.transitionDuration);
            RefreshTransitionTimingFunction(transitions, setData.transitionTimingFunction, computedData.transitionTimingFunction);
            RefreshTransitionDelay(transitions, setData.transitionDelay, computedData.transitionDelay);
        }

        void SetTransitionVariableEditors(FoldoutTransitionField foldoutTransitionField, int index)
        {
            SetVariableEditor(foldoutTransitionField.propertyField, index);
            SetVariableEditor(foldoutTransitionField.durationField, index);
            SetVariableEditor(foldoutTransitionField.timingFunctionField, index);
            SetVariableEditor(foldoutTransitionField.delayField, index);
        }

        static void RefreshTransitionProperty(List<BuilderTransition> transitions, StylePropertyManipulator manipulator, List<StylePropertyName> computedData)
        {
            var isSet = null != manipulator.styleProperty;
            var valueCount = isSet ? manipulator.GetValuesCount() : computedData.Count;

            for (var i = 0; i < transitions.Count; ++i)
            {
                var transition = transitions[i];
                if (isSet)
                {
                    if (i < valueCount)
                    {
                        var handle = manipulator.GetValueContextAtIndex(i % valueCount);
                        if (handle.handle.valueType == StyleValueType.Keyword)
                            transition.property = new UIStyleValue<string>(handle.AsKeyword());
                        else if (handle.handle.valueType == StyleValueType.Enum)
                            transition.property = handle.AsEnum();
                        else if (handle.handle.valueType == StyleValueType.String)
                            transition.property = handle.AsString();
                        // Error, use default value
                        else
                            transition.property = BuilderTransition.IgnoredProperty;
                    }
                    else
                    {
                        transition.property = BuilderTransition.IgnoredProperty;
                    }
                }
                else
                {
                    transition.property = i < valueCount
                        ? computedData[i].ToString() ?? BuilderTransition.IgnoredProperty
                        : BuilderTransition.IgnoredProperty;
                }
                transitions[i] = transition;
            }
        }

        static void RefreshTransitionDuration(List<BuilderTransition> transitions, StylePropertyManipulator manipulator, List<TimeValue> computedData)
        {
            var isSet = null != manipulator.styleProperty;
            var valueCount = isSet ? manipulator.GetValuesCount() : computedData.Count;

            for (var i = 0; i < transitions.Count; ++i)
            {
                var transition = transitions[i];
                if (isSet)
                {
                    var handle = manipulator.GetValueContextAtIndex(i % valueCount);
                    if (handle.handle.valueType == StyleValueType.Keyword)
                        transition.duration = new UIStyleValue<TimeValue>(handle.AsKeyword());
                    else if (handle.handle.valueType == StyleValueType.Dimension)
                        transition.duration = handle.AsDimension().ToTime();
                    else if (handle.handle.valueType == StyleValueType.Float)
                        transition.duration = new TimeValue(handle.AsFloat());
                    // Error, use default value
                    else
                        transition.duration = BuilderTransition.DefaultDuration;
                }
                else
                {
                    transition.duration = i < valueCount
                            ? computedData[i]
                            : computedData.Count > 0
                                ? computedData[i % valueCount]
                                : BuilderTransition.DefaultDuration;
                }

                transitions[i] = transition;
            }
        }

        static void RefreshTransitionTimingFunction(List<BuilderTransition> transitions, StylePropertyManipulator manipulator, List<EasingFunction> computedData)
        {
            var isSet = null != manipulator.styleProperty;
            var valueCount = isSet ? manipulator.GetValuesCount() : computedData.Count;

            for (var i = 0; i < transitions.Count; ++i)
            {
                var transition = transitions[i];
                if (isSet)
                {
                    var handle = manipulator.GetValueContextAtIndex(i % valueCount);
                    if (handle.handle.valueType == StyleValueType.Keyword)
                        transition.timingFunction = new UIStyleValue<EasingFunction>(handle.AsKeyword());
                    else if (handle.handle.valueType == StyleValueType.Enum && StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.EasingMode, handle.AsEnum(), out var value))
                        transition.timingFunction = new EasingFunction((EasingMode)value);
                    // Error, use default value
                    else
                        transition.timingFunction = BuilderTransition.DefaultTimingFunction;
                }
                else
                {
                    transition.timingFunction = i < valueCount
                        ? computedData[i]
                        : computedData.Count > 0
                            ? computedData[i % valueCount]
                            : BuilderTransition.DefaultTimingFunction;
                }

                transitions[i] = transition;
            }
        }

        static void RefreshTransitionDelay(List<BuilderTransition> transitions, StylePropertyManipulator manipulator, List<TimeValue> computedData)
        {
            var isSet = null != manipulator.styleProperty;
            var valueCount = isSet ? manipulator.GetValuesCount() : computedData.Count;

            for (var i = 0; i < transitions.Count; ++i)
            {
                var transition = transitions[i];
                if (isSet)
                {
                    var handle = manipulator.GetValueContextAtIndex(i % valueCount);
                    if (handle.handle.valueType == StyleValueType.Keyword)
                        transition.delay = new UIStyleValue<TimeValue>(handle.AsKeyword());
                    else if (handle.handle.valueType == StyleValueType.Dimension)
                        transition.delay = handle.AsDimension().ToTime();
                    else if (handle.handle.valueType == StyleValueType.Float)
                        transition.delay = new TimeValue(handle.AsFloat());
                    // Error, use default value
                    else
                        transition.delay = BuilderTransition.DefaultDelay;
                }
                else
                {
                    transition.delay = i < valueCount
                        ? computedData[i]
                        : computedData.Count > 0
                            ? computedData[i % valueCount]
                            : BuilderTransition.DefaultDelay;
                }

                transitions[i] = transition;
            }
        }

        BuilderTransitionData GetBuilderTransitionData()
        {
            return new BuilderTransitionData(styleSheet, currentRule, currentVisualElement, m_Inspector.document.fileSettings.editorExtensionMode);
        }

        FoldoutTransitionField GetPooledTransitionField(TransitionsListView transitionsListView, BuilderTransition transition, int index)
        {
            FoldoutTransitionField foldoutField;

            // Reuse pre-existing item
            if (index < transitionsListView.childCount)
            {
                foldoutField = (FoldoutTransitionField)transitionsListView[index];
            }
            // Configure a new element
            else if (transitionsListView.GetOrCreateTransitionField(out foldoutField))
            {
                foldoutField.header.AddManipulator(new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
                SetUpContextualMenuOnStyleField(foldoutField.propertyField);
                foldoutField.propertyField.getContent = GetTransitionPropertyContent;

                SetUpContextualMenuOnStyleField(foldoutField.durationField);
                SetUpContextualMenuOnStyleField(foldoutField.timingFunctionField);
                SetUpContextualMenuOnStyleField(foldoutField.delayField);
            }

            GetOrCreateFieldListForStyleName(TransitionConstants.Property).Add(foldoutField.propertyField);
            GetOrCreateFieldListForStyleName(TransitionConstants.Duration).Add(foldoutField.durationField);
            GetOrCreateFieldListForStyleName(TransitionConstants.TimingFunction).Add(foldoutField.timingFunctionField);
            GetOrCreateFieldListForStyleName(TransitionConstants.Delay).Add(foldoutField.delayField);

            // These need to be called after the field has been added to the hierarchy
            foldoutField.SetTransitionData(transition);
            return foldoutField;
        }

        static void UnpackTransitionProperty(StylePropertyManipulator manipulator, List<StylePropertyName> computedData, int maxCount)
        {
            if (null == manipulator.styleProperty || manipulator.GetValuesCount() != computedData.Count)
                TransferTransitionComputedData(manipulator, computedData, StyleValueType.Enum);

            for (var i = computedData.Count; i < maxCount; ++i)
            {
                manipulator.AddValue(BuilderTransition.IgnoredProperty, StyleValueType.Enum);
            }
        }

        static void TransferTransitionComputedData(StylePropertyManipulator manipulator, List<StylePropertyName> computedData, StyleValueType valueType)
        {
            manipulator.ClearValues();
            foreach (var value in computedData)
            {
                manipulator.AddValue(value.ToString(), valueType);
            }
        }

        static  void UnpackTransitionDurationOrDelay(StylePropertyManipulator manipulator, List<TimeValue> computedData, int maxCount)
        {
            if (null == manipulator.styleProperty || manipulator.GetValuesCount() != computedData.Count)
                TransferTransitionComputedData(manipulator, computedData, StyleValueType.Dimension);

            for (var i = computedData.Count; i < maxCount; ++i)
            {
                manipulator.AddValue(computedData[i%computedData.Count].ToDimension(), StyleValueType.Dimension);
            }
        }

        static void TransferTransitionComputedData(StylePropertyManipulator manipulator, List<TimeValue> computedData, StyleValueType valueType)
        {
            manipulator.ClearValues();
            foreach (var value in computedData)
            {
                manipulator.AddValue(value.ToDimension(), valueType);
            }
        }

        static void UnpackTransitionTimingFunction(StylePropertyManipulator manipulator, List<EasingFunction> computedData, int maxCount)
        {
            if (null == manipulator.styleProperty || manipulator.GetValuesCount() != computedData.Count)
                TransferTransitionComputedData(manipulator, computedData, StyleValueType.Enum);

            for (var i = computedData.Count; i < maxCount; ++i)
            {
                manipulator.AddValue(computedData[i%computedData.Count].mode, StyleValueType.Enum);
            }
        }

        static void TransferTransitionComputedData(StylePropertyManipulator manipulator, List<EasingFunction> computedData, StyleValueType valueType)
        {
            manipulator.ClearValues();
            foreach (var value in computedData)
            {
                manipulator.AddValue(value.mode, valueType);
            }
        }

        static bool OnTransitionStringChanged(StylePropertyManipulator manipulator, List<StylePropertyName> computedData, UIStyleValue<string> property, int index, int maxCount)
        {
            UnpackTransitionProperty(manipulator, computedData, maxCount);

            var requiresRefresh = false;
            if (property.isKeyword)
            {
                requiresRefresh = true;
                manipulator.ClearValues();
                manipulator.AddValue(property.keyword, StyleValueType.Keyword);
            }
            else
            {
                if (manipulator.IsKeywordAtIndex(0))
                {
                    requiresRefresh = true;
                    var valueCount = manipulator.GetValuesCount();
                    manipulator.ClearValues();
                    for (var i = 0; i < valueCount; ++i)
                        manipulator.AddValue(BuilderTransition.IgnoredProperty, StyleValueType.Enum);
                }
                manipulator.SetValueAtIndex(index, property.value, StyleValueType.Enum);
            }

            return requiresRefresh;
        }

        static bool OnTransitionTimeValueChanged(StylePropertyManipulator manipulator, List<TimeValue> computedData, UIStyleValue<TimeValue> value, int index, int maxCount)
        {
            UnpackTransitionDurationOrDelay(manipulator, computedData, maxCount);

            var requiresRefresh = false;
            if (value.isKeyword)
            {
                requiresRefresh = true;
                manipulator.ClearValues();
                manipulator.AddValue(value.keyword, StyleValueType.Keyword);
            }
            else
            {
                if (manipulator.IsKeywordAtIndex(0))
                {
                    requiresRefresh = true;
                    var valueCount = manipulator.GetValuesCount();
                    manipulator.ClearValues();
                    for (var i = 0; i < valueCount; ++i)
                        manipulator.AddValue(BuilderTransition.DefaultDuration.ToDimension(), StyleValueType.Dimension);
                }

                manipulator.SetValueAtIndex(index, value.value.ToDimension(), StyleValueType.Dimension);
            }

            return requiresRefresh;
        }

        static bool OnTransitionTimingFunctionChanged(StylePropertyManipulator manipulator, List<EasingFunction> computedData, UIStyleValue<EasingFunction> uiValue, int index, int maxCount)
        {
            UnpackTransitionTimingFunction(manipulator, computedData, maxCount);

            var requiresRefresh = false;
            if (uiValue.isKeyword)
            {
                requiresRefresh = true;
                manipulator.ClearValues();
                manipulator.AddValue(uiValue.keyword, StyleValueType.Keyword);
            }
            else
            {
                if (manipulator.IsKeywordAtIndex(0))
                {
                    requiresRefresh = true;
                    var valueCount = manipulator.GetValuesCount();
                    manipulator.ClearValues();
                    for (var i = 0; i < valueCount; ++i)
                        manipulator.AddValue(BuilderTransition.DefaultTimingFunction.mode, StyleValueType.Enum);
                }
                manipulator.SetValueAtIndex(index, uiValue.value.mode, StyleValueType.Enum);
            }

            return requiresRefresh;
        }

        void OnTransitionChanged(TransitionChangedEvent evt, TransitionsListView transitionsListView)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            var foldout = evt.field;
            var computedData = currentVisualElement.computedStyle.transitionData.Read();
            using var setData = GetBuilderTransitionData();

            var maxCount = Mathf.Max(computedData.MaxCount(), setData.MaxCount());

            var index = evt.index;
            var transition = evt.transition;
            var changeType = evt.changeType;

            s_StyleChangeList.Clear();
            var requiresRefresh = false;

            var foldouts = transitionsListView.Query<FoldoutTransitionField>().ToList();

            if ((changeType & TransitionChangeType.Property) == TransitionChangeType.Property)
            {
                if (OnTransitionStringChanged(setData.transitionProperty, computedData.transitionProperty, transition.property, index, maxCount))
                    requiresRefresh = true;
                else
                {
                    for (var i = 0; i < foldouts.Count; ++i)
                        PostStyleFieldSteps(foldouts[i].propertyField, setData.transitionProperty.styleProperty, TransitionConstants.Property, false, setData.transitionProperty.IsVariableAtIndex(i));
                }

                s_StyleChangeList.Add(TransitionConstants.Property);
            }

            if ((changeType & TransitionChangeType.Duration) == TransitionChangeType.Duration)
            {
                if (OnTransitionTimeValueChanged(setData.transitionDuration, computedData.transitionDuration, transition.duration, index, maxCount))
                    requiresRefresh = true;
                else
                {
                    for (var i = 0; i < foldouts.Count; ++i)
                        PostStyleFieldSteps(foldouts[i].durationField, setData.transitionDuration.styleProperty, TransitionConstants.Duration, false, setData.transitionDuration.IsVariableAtIndex(i));
                }

                s_StyleChangeList.Add(TransitionConstants.Duration);
            }

            if ((changeType & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction)
            {
                if (OnTransitionTimingFunctionChanged(setData.transitionTimingFunction, computedData.transitionTimingFunction, transition.timingFunction, index, maxCount))
                    requiresRefresh = true;
                else
                {
                    for (var i = 0; i < foldouts.Count; ++i)
                        PostStyleFieldSteps(foldouts[i].timingFunctionField, setData.transitionTimingFunction.styleProperty, TransitionConstants.TimingFunction, false,
                            setData.transitionTimingFunction.IsVariableAtIndex(i));
                }

                s_StyleChangeList.Add(TransitionConstants.TimingFunction);
            }

            if ((changeType & TransitionChangeType.Delay) == TransitionChangeType.Delay)
            {
                if (OnTransitionTimeValueChanged(setData.transitionDelay, computedData.transitionDelay, transition.delay, index, maxCount))
                    requiresRefresh = true;
                else
                {
                    for (var i = 0; i < foldouts.Count; ++i)
                        PostStyleFieldSteps(foldouts[i].delayField, setData.transitionDelay.styleProperty, TransitionConstants.Delay, false,
                            setData.transitionDelay.IsVariableAtIndex(i));
                }

                s_StyleChangeList.Add(TransitionConstants.Delay);
            }

            if (requiresRefresh)
                s_StyleChangeList.Add(TransitionConstants.Transition);
            else
                updateStyleCategoryFoldoutOverrides();

            NotifyStyleChanges(s_StyleChangeList, true);
            foldout.UpdateFromChildFields();
            transitionsListView.Refresh(setData.GetOverrides() | changeType, setData.GetKeywords());
        }

        void OnTransitionAdded(TransitionAddedEvent evt, TransitionsListView listView)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var computedData = currentVisualElement.computedStyle.transitionData.Read();
            using var setData = GetBuilderTransitionData();

            var changeType = setData.GetOverrides();
            if (changeType == TransitionChangeType.None)
                changeType = TransitionChangeType.All;
            var maxCount = Mathf.Max(computedData.MaxCount(), setData.MaxCount());

            if ((changeType & TransitionChangeType.Property) == TransitionChangeType.Property)
            {

                if (setData.transitionProperty.GetValuesCount() == 1 &&
                    setData.transitionProperty.GetValueContextAtIndex(0).handle.valueType ==
                    StyleValueType.Keyword)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionProperty(setData.transitionProperty,
                        computedData.transitionProperty, maxCount);
                    if (setData.transitionProperty.IsKeywordAtIndex(0))
                        setData.transitionProperty.SetValueAtIndex(0, BuilderTransition.IgnoredProperty,
                            StyleValueType.Enum);

                    setData.transitionProperty.AddValue(BuilderTransition.IgnoredProperty,
                        StyleValueType.Enum);
                }
            }

            if ((changeType & TransitionChangeType.Duration) == TransitionChangeType.Duration)
            {
                if (setData.transitionDuration.GetValuesCount() == 1 &&
                    setData.transitionDuration.GetValueContextAtIndex(0).handle.valueType ==
                    StyleValueType.Keyword)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionDurationOrDelay(setData.transitionDuration,
                        computedData.transitionDuration, maxCount);
                    if (setData.transitionDuration.IsKeywordAtIndex(0))
                        setData.transitionDuration.SetValueAtIndex(0,
                            BuilderTransition.DefaultDuration.ToDimension(), StyleValueType.Dimension);

                    setData.transitionDuration.AddValue(BuilderTransition.DefaultDuration.ToDimension(),
                        StyleValueType.Dimension);
                }
            }

            if ((changeType & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction)
            {
                if (setData.transitionTimingFunction.GetValuesCount() == 1 &&
                    setData.transitionTimingFunction.GetValueContextAtIndex(0).handle.valueType ==
                    StyleValueType.Keyword)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionTimingFunction(setData.transitionTimingFunction,
                        computedData.transitionTimingFunction, maxCount);
                    if (setData.transitionTimingFunction.IsKeywordAtIndex(0))
                        setData.transitionTimingFunction.SetValueAtIndex(0, EasingMode.Ease,
                            StyleValueType.Enum);

                    setData.transitionTimingFunction.AddValue(EasingMode.Ease, StyleValueType.Enum);
                }
            }

            if ((changeType & TransitionChangeType.Delay) == TransitionChangeType.Delay)
            {
                if (setData.transitionDelay.GetValuesCount() == 1 &&
                    setData.transitionDelay.GetValueContextAtIndex(0).handle.valueType ==
                    StyleValueType.Keyword)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionDurationOrDelay(setData.transitionDelay,
                        computedData.transitionDelay, maxCount);
                    if (setData.transitionDelay.IsKeywordAtIndex(0))
                        setData.transitionDelay.SetValueAtIndex(0,
                            BuilderTransition.DefaultDelay.ToDimension(), StyleValueType.Dimension);

                    setData.transitionDelay.AddValue(BuilderTransition.DefaultDelay.ToDimension(),
                        StyleValueType.Dimension);
                }
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(TransitionConstants.Transition);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void OnTransitionRemoved(TransitionRemovedEvent evt, TransitionsListView listView)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var computedData = currentVisualElement.computedStyle.transitionData.Read();
            var setData = GetBuilderTransitionData();

            var maxCount = Mathf.Max(computedData.MaxCount(), setData.MaxCount());
            var index = evt.index;

            var changeType = setData.GetOverrides();
            if (changeType == TransitionChangeType.None)
            {
                changeType = TransitionChangeType.All;
            }

            if ((changeType & TransitionChangeType.Property) == TransitionChangeType.Property)
            {
                if (setData.transitionProperty.GetValueContextAtIndex(0).handle.valueType == StyleValueType.Keyword &&
                    index != 0)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionProperty(setData.transitionProperty, computedData.transitionProperty, maxCount);
                    setData.transitionProperty.RemoveAtIndex(index);
                }
            }

            if ((changeType & TransitionChangeType.Duration) == TransitionChangeType.Duration)
            {
                if (setData.transitionDuration.GetValueContextAtIndex(0).handle.valueType == StyleValueType.Keyword &&
                    index != 0)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionDurationOrDelay(setData.transitionDuration, computedData.transitionDuration,
                        maxCount);
                    setData.transitionDuration.RemoveAtIndex(index);
                }
            }

            if ((changeType & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction)
            {
                if (setData.transitionTimingFunction.GetValueContextAtIndex(0).handle.valueType == StyleValueType.Keyword &&
                    index != 0)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionTimingFunction(setData.transitionTimingFunction,
                        computedData.transitionTimingFunction, maxCount);
                    setData.transitionTimingFunction.RemoveAtIndex(index);
                }
            }

            if ((changeType & TransitionChangeType.Delay) == TransitionChangeType.Delay)
            {
                if (setData.transitionDelay.GetValueContextAtIndex(0).handle.valueType == StyleValueType.Keyword &&
                    index != 0)
                {
                    // Nothing to do..
                }
                else
                {
                    UnpackTransitionDurationOrDelay(setData.transitionDelay, computedData.transitionDelay, maxCount);
                    setData.transitionDelay.RemoveAtIndex(index);
                }
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(TransitionConstants.Transition);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        CategoryDropdownContent GetTransitionPropertyContent()
        {
            var content = new CategoryDropdownContent();

            var setProperties = ListPool<string>.Get();
            try
            {
                setProperties.AddRange(currentRule.GetAllSetStyleProperties().Where(p => !p.StartsWith(TransitionConstants.Transition)));

                if (setProperties.Count > 0)
                {
                    content.AppendCategory(new CategoryDropdownContent.Category{ name = TransitionConstants.CurrentOverrides});
                    foreach (var overriddenProperty in setProperties)
                    {
                        content.AppendValue(new CategoryDropdownContent.ValueItem
                        {
                            value = overriddenProperty,
                            displayName =  ObjectNames.NicifyVariableName(StylePropertyUtil.s_NameToId[overriddenProperty].ToString()),
                            categoryName = TransitionConstants.CurrentOverrides
                        });
                    }
                    content.AppendSeparator();
                }
            }
            finally
            {
                ListPool<string>.Release(setProperties);
            }

            content.AppendContent(TransitionPropertyDropdownContent.Content);

            return content;
        }
    }
}
