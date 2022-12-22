// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    sealed class FoldoutTransitionField : FoldoutField
    {
        const string k_UxmlPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionField.uxml";
        const string k_UssPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionField.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionFieldDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionFieldLight.uss";

        const string k_BaseClass = "unity-foldout-transition-field";
        const string k_RemoveButtonClass = k_BaseClass + "__remove-transition-button";

        static readonly string[] k_BindingPathArray =
        {
            StylePropertyId.TransitionProperty.UssName(),
            StylePropertyId.TransitionDuration.UssName(),
            StylePropertyId.TransitionTimingFunction.UssName(),
            StylePropertyId.TransitionDelay.UssName(),
        };

        public new class UxmlFactory : UxmlFactory<FoldoutTransitionField, UxmlTraits> {}
        public new class UxmlTraits : FoldoutField.UxmlTraits {}


        TransitionChangeType m_Overrides;

        public readonly Button RemoveTransitionButton;
        public readonly CategoryDropdownField propertyField;
        public readonly DimensionStyleField durationField;
        public readonly EnumField timingFunctionField;
        public readonly DimensionStyleField delayField;

        public int index;

        public TransitionChangeType overrides
        {
            get => m_Overrides;
            set
            {
                if (m_Overrides == value)
                    return;
                m_Overrides = value;
                ApplyOverrides();
            }
        }

        public FoldoutTransitionField()
        {
            bindingPathArray = k_BindingPathArray;

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            template.CloneTree(contentContainer);

            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
            styleSheets.Add(styleSheet);

            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssDarkSkinPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssLightSkinPath));

            propertyField = this.Q<CategoryDropdownField>(StylePropertyId.TransitionProperty.UssName());
            propertyField.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, propertyField.GetFirstAncestorOfType<BuilderStyleRow>());
            propertyField.RegisterValueChangedCallback(OnPropertyChanged);

            durationField = this.Q<DimensionStyleField>(StylePropertyId.TransitionDuration.UssName());
            durationField.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, durationField.GetFirstAncestorOfType<BuilderStyleRow>());
            durationField.RegisterValueChangedCallback(OnDurationChanged);

            timingFunctionField = this.Q<EnumField>(StylePropertyId.TransitionTimingFunction.UssName());
            timingFunctionField.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, timingFunctionField.GetFirstAncestorOfType<BuilderStyleRow>());
            timingFunctionField.RegisterValueChangedCallback(OnTimingFunctionChanged);

            delayField = this.Q<DimensionStyleField>(StylePropertyId.TransitionDelay.UssName());
            delayField.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, delayField.GetFirstAncestorOfType<BuilderStyleRow>());
            delayField.RegisterValueChangedCallback(OnDelayChanged);

            RemoveTransitionButton = new Button(RemoveTransition);
            RemoveTransitionButton.AddToClassList(k_RemoveButtonClass);
            header.Add(RemoveTransitionButton);

            AddToClassList(k_BaseClass);

            // Can't override this from uss :(
            this.Q<Toggle>().style.flexGrow = 1;
        }

        void OnPropertyChanged(ChangeEvent<string> evt)
        {
            TransitionChanged(TransitionChangeType.Property);
            evt.StopPropagation();
        }

        void OnDurationChanged(ChangeEvent<string> evt)
        {
            TransitionChanged(TransitionChangeType.Duration);
            evt.StopPropagation();
        }

        void OnTimingFunctionChanged(ChangeEvent<Enum> evt)
        {
            TransitionChanged(TransitionChangeType.TimingFunction);
            evt.StopPropagation();
        }

        void OnDelayChanged(ChangeEvent<string> evt)
        {
            TransitionChanged(TransitionChangeType.Delay);
            evt.StopPropagation();
        }

        void TransitionChanged(TransitionChangeType changeType)
        {
            overrides |= changeType;
            GetFirstAncestorOfType<TransitionsListView>().TransitionChanged(this, changeType);
            UpdateFromChildFields();
        }

        public BuilderTransition GetTransitionData()
        {
            var transition = new BuilderTransition
            {
                property = propertyField.value switch
                {
                    "none" => new UIStyleValue<string>(StyleValueKeyword.None),
                    "initial" => new UIStyleValue<string>(StyleValueKeyword.Initial),
                    _ => propertyField.value
                },
                duration = durationField.isKeyword
                    ? new UIStyleValue<TimeValue>(durationField.keyword)
                    : new TimeValue(durationField.length, durationField.unit == Dimension.Unit.Second ? TimeUnit.Second : TimeUnit.Millisecond),
                timingFunction = (EasingFunction) timingFunctionField.value,
                delay = delayField.isKeyword
                    ? new UIStyleValue<TimeValue>(delayField.keyword)
                    : new TimeValue(delayField.length, delayField.unit == Dimension.Unit.Second ? TimeUnit.Second : TimeUnit.Millisecond)
            };


            return transition;
        }

        public void SetTransitionData(BuilderTransition transition)
        {
            if (transition.property.isKeyword)
                propertyField.SetValueWithoutNotify(transition.property.keyword.ToUssString());
            else
                propertyField.SetValueWithoutNotify(transition.property.value);

            if (transition.duration.isKeyword)
                durationField.keyword = transition.duration.keyword;
            else
                durationField.SetValueWithoutNotify(transition.duration.value.ToString());

            if (transition.timingFunction.isKeyword)
            {
                // We don't support keywords with enum
                timingFunctionField.SetValueWithoutNotify(EasingMode.Ease);
            }
            else
                timingFunctionField.SetValueWithoutNotify(transition.timingFunction.value.mode);

            if (transition.delay.isKeyword)
                delayField.keyword = transition.delay.keyword;
            else
                delayField.SetValueWithoutNotify(transition.delay.value.ToString());
            UpdateFromChildFields();
        }

        void RemoveTransition()
        {
            GetFirstAncestorOfType<TransitionsListView>().RemoveTransition(this);
        }

        public override void UpdateFromChildFields()
        {
            text = $"{GetPropertyString()} {GetDurationString()} {GetTimingFunctionString()} {GetDelayString()}";
        }

        string GetDisplayString<T>(TransitionChangeType type, T value)
        {
            return (overrides & type) == type ? $"<b>{value}</b>" : value.ToString();
        }

        string GetPropertyString() => GetDisplayString(TransitionChangeType.Property, propertyField.value);
        string GetDurationString() => GetDisplayString(TransitionChangeType.Duration, durationField.value);
        string GetTimingFunctionString() => GetDisplayString(TransitionChangeType.TimingFunction, timingFunctionField.value);
        string GetDelayString() => GetDisplayString(TransitionChangeType.Delay, delayField.value);

        void ApplyOverrides()
        {
            header.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, overrides.Any());
            propertyField.parent.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, (overrides & TransitionChangeType.Property) == TransitionChangeType.Property);
            durationField.parent.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, (overrides & TransitionChangeType.Duration) == TransitionChangeType.Duration);
            timingFunctionField.parent.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, (overrides & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction);
            delayField.parent.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, (overrides & TransitionChangeType.Delay) == TransitionChangeType.Delay);
        }
    }
}
