// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class TransitionFoldoutField : StyleFoldout
    {
        const string k_BaseClass = "unity-foldout-transition-field";
        const string k_RemoveButtonClass = k_BaseClass + "__remove-transition-button";

        const string k_UssPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionField.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionFieldDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/FoldoutField/FoldoutTransitionFieldLight.uss";

        private TransitionField m_TransitionField;
        private Button m_RemoveTransitionButton;

        public TransitionField transitionField => m_TransitionField;
        public Button removeTransitionButton => m_RemoveTransitionButton;

        public TransitionFoldoutField()
        {
            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
            styleSheets.Add(styleSheet);

            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssDarkSkinPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssLightSkinPath));

            Track(StyleTransitionListView.k_TransitionPropertyName);
            Track(StyleTransitionListView.k_TransitionDurationName);
            Track(StyleTransitionListView.k_TransitionTimingFunctionName);
            Track(StyleTransitionListView.k_TransitionDelayName);

            m_TransitionField = new TransitionField();

            m_RemoveTransitionButton = new Button();
            m_RemoveTransitionButton.AddToClassList(k_RemoveButtonClass);
            foldout.header.Add(m_RemoveTransitionButton);

            Add(m_TransitionField);
        }
    }

    class TransitionField : VisualElement
    {
        public TextField property;
        public TimeValueField duration;
        public EnumField timingFunction;
        public TimeValueField delay;

        public TransitionField()
        {
            var propertyRow = new StyleRow();
            propertyRow.Track(StyleTransitionListView.k_TransitionPropertyName);

            property = new TextField("Property")
            {
                isDelayed = true
            };
            propertyRow.Add(property);
            Add(propertyRow);

            var durationRow = new StyleRow();
            durationRow.Track(StyleTransitionListView.k_TransitionDurationName);
            duration = new TimeValueField("Duration")
            {
                isDelayed = true,
                showUnitAsDropdown = true
            };
            durationRow.Add(duration);
            Add(durationRow);

            var timingFunctionRow = new StyleRow();
            timingFunctionRow.Track(StyleTransitionListView.k_TransitionTimingFunctionName);
            timingFunction = new EnumField("Easing", EasingMode.Ease);
            timingFunctionRow.Add(timingFunction);
            Add(timingFunctionRow);

            var delayRow = new StyleRow();
            delayRow.Track(StyleTransitionListView.k_TransitionDelayName);
            delay = new TimeValueField("Delay")
            {
                isDelayed = true,
                showUnitAsDropdown = true
            };
            delayRow.Add(delay);
            Add(delayRow);
        }
    }

    internal class StyleTransitionListView : VisualElement
    {
        const string k_UssPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListView.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListViewDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListViewLight.uss";
        const string k_BaseClass = "unity-transition-list-view";
        const string k_AddButtonClass = k_BaseClass + "__add-transition-button";

        public const string k_TransitionPropertyName = "transitionProperty";
        public const string k_TransitionDurationName = "transitionDuration";
        public const string k_TransitionTimingFunctionName = "transitionTimingFunction";
        public const string k_TransitionDelayName = "transitionDelay";
        public const string k_IgnoredProperty = "ignored";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new StyleTransitionListView();
        }

        internal struct TransitionData
        {
            public StylePropertyName property;
            public TimeValue duration;
            public EasingFunction timingFunction;
            public TimeValue delay;

            public string ToString(bool p, bool du, bool tf, bool de)
            {
                var propertyName = property.id == StylePropertyId.Unknown ? k_IgnoredProperty : property.ToString();
                return $"{Bold(propertyName, p)} {Bold(duration.ToString(), du)} {Bold(StyleSheetUtility.GetEnumExportString(timingFunction.mode), tf)} {Bold(delay.ToString(), de)}";
            }

            private string Bold(string input, bool bold)
            {
                return $"{(bold ? "<b>":"")}{input}{(bold ? "</b>":"")}";
            }
        }

        private List<StylePropertyName> m_TransitionProperty;
        private List<TimeValue> m_TransitionDuration;
        private List<TimeValue> m_TransitionDelay;
        private List<EasingFunction> m_TransitionTimingFunction;

        private List<TransitionData> m_Data;

        private ListView m_ListView;
        private Button m_AddTransitionButton;

        public Button addTransitionButton => m_AddTransitionButton;
        public ListView listView => m_ListView;

        [CreateProperty]
        public List<StylePropertyName> transitionProperty
        {
            get => m_TransitionProperty;
            set
            {
                if (m_TransitionProperty == value)
                    return;

                m_TransitionProperty = value;
                Refresh();
            }
        }

        [CreateProperty] public List<TimeValue> transitionDuration
        {
            get => m_TransitionDuration;
            set
            {
                if (m_TransitionDuration == value)
                    return;

                m_TransitionDuration = value;
                Refresh();
            }
        }

        [CreateProperty] public List<TimeValue> transitionDelay
        {
            get => m_TransitionDelay;
            set
            {
                if (m_TransitionDelay == value)
                    return;

                m_TransitionDelay = value;
                Refresh();
            }
        }

        [CreateProperty] public List<EasingFunction> transitionTimingFunction
        {
            get => m_TransitionTimingFunction;
            set
            {
                if (m_TransitionTimingFunction == value)
                    return;

                m_TransitionTimingFunction = value;
                Refresh();
            }
        }

        private bool propertySet;
        private bool durationSet;
        private bool timingFunctionSet;
        private bool delaySet;

        private HashSet<string> m_OverriddenProperties;

        [CreateProperty]
        internal HashSet<string> overriddenProperties
        {
            get => m_OverriddenProperties;
            set
            {
                m_OverriddenProperties = value;
                propertySet = m_OverriddenProperties.Contains(k_TransitionPropertyName);
                durationSet = m_OverriddenProperties.Contains(k_TransitionDurationName);
                timingFunctionSet = m_OverriddenProperties.Contains(k_TransitionTimingFunctionName);
                delaySet = m_OverriddenProperties.Contains(k_TransitionDelayName);
            }
        }

        public StyleTransitionListView()
        {
            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
            styleSheets.Add(styleSheet);

            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssDarkSkinPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssLightSkinPath));

            m_Data = new List<TransitionData>();
            m_ListView = new ListView
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                itemsSource = m_Data,
                makeItem = () => new TransitionFoldoutField(),
                bindItem = (element, i) =>
                {
                    if (element is TransitionFoldoutField transitionFoldout)
                    {
                        transitionFoldout.EnableInClassList("last-item", i == m_Data.Count - 1);
                        var data = m_Data[i];
                        transitionFoldout.text = data.ToString(propertySet, durationSet, timingFunctionSet, delaySet);
                        var transitionField = transitionFoldout.transitionField;
                        transitionField.property.value = data.property.id == StylePropertyId.Unknown ? k_IgnoredProperty : data.property.ToString();

                        transitionField.duration.value = data.duration;
                        transitionField.delay.value = data.delay;
                        transitionField.timingFunction.value = data.timingFunction.mode;

                        // Create and store the action in user data
                        var action = () => RemoveTransition(i);
                        transitionFoldout.userData = action;

                        transitionFoldout.removeTransitionButton.clicked += action;


                        transitionField.property.RegisterValueChangedCallback(e =>
                        {
                            transitionProperty[i] = e.newValue != null ? new StylePropertyName(e.newValue) : new StylePropertyName(StylePropertyId.Unknown);
                            transitionFoldout.text = data.ToString(propertySet, durationSet, timingFunctionSet, delaySet);
                            Refresh();
                        });
                        transitionField.delay.RegisterValueChangedCallback(e =>
                        {
                            transitionDelay[i] = e.newValue;
                            transitionFoldout.text = data.ToString(propertySet, durationSet, timingFunctionSet, delaySet);
                            Refresh();
                        });
                        transitionField.duration.RegisterValueChangedCallback(e =>
                        {
                            transitionDuration[i] = e.newValue;
                            transitionFoldout.text = data.ToString(propertySet, durationSet, timingFunctionSet, delaySet);
                            Refresh();
                        });
                        transitionField.timingFunction.RegisterValueChangedCallback(e =>
                        {
                            transitionTimingFunction[i] = new EasingFunction((EasingMode)e.newValue);
                            transitionFoldout.text = data.ToString(propertySet, durationSet, timingFunctionSet, delaySet);
                            Refresh();
                        });
                    }
                },
                unbindItem = (element, i) =>
                {
                    if (element is TransitionFoldoutField { userData: Action action } transitionFoldout)
                    {
                        transitionFoldout.removeTransitionButton.clicked -= action;
                        transitionFoldout.userData = null;
                    }
                },
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeFooter = () =>
                {
                    m_AddTransitionButton = new Button(OnTransitionAdded);
                    m_AddTransitionButton.text = "+ Add Transition";
                    m_AddTransitionButton.AddToClassList(k_AddButtonClass);
                    return m_AddTransitionButton;
                }
            };
            Add(m_ListView);
        }

        void OnTransitionAdded()
        {
            m_TransitionProperty ??= new List<StylePropertyName>();
            m_TransitionDuration ??= new List<TimeValue>();
            m_TransitionDelay ??= new List<TimeValue>();
            m_TransitionTimingFunction ??= new List<EasingFunction>();

            if (m_TransitionProperty.Count == 0)
                m_TransitionProperty.Add(new StylePropertyName(StylePropertyId.All));
            else
                m_TransitionProperty.Add(new StylePropertyName(StylePropertyId.Unknown));

            m_TransitionDuration.Add(new TimeValue(0, TimeUnit.Second));
            m_TransitionDelay.Add(new TimeValue(0, TimeUnit.Second));
            m_TransitionTimingFunction.Add(new EasingFunction(EasingMode.Ease));

            Refresh();

            // The usual GeometryChangedEvent doesn't seem to work at all here.
            schedule.Execute(ScrollToLastItem).StartingIn(20);
        }

        void ScrollToLastItem()
        {
            var sv = GetFirstAncestorOfType<ScrollView>();
            sv?.ScrollTo(m_AddTransitionButton);
        }

        internal void RemoveTransition(int index)
        {
            var count = GetMaxCount();
            if (count > index)
            {
                m_TransitionProperty.RemoveAt(index);
                m_TransitionDelay.RemoveAt(index);
                m_TransitionDuration.RemoveAt(index);
                m_TransitionTimingFunction.RemoveAt(index);
            }

            Refresh();

            using (var evt = TransitionRemovedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.index = index;
                SendEvent(evt);
            }
        }

        private void Refresh()
        {
            m_Data.Clear();

            var count = GetMaxCount();
            for (var i = 0; i < count; ++i)
            {
                var transition = new TransitionData();
                transition.property = m_TransitionProperty?.Count > i ? m_TransitionProperty[i] : new StylePropertyName(StylePropertyId.Unknown);

                if (null == m_TransitionDuration)
                    transition.duration = new TimeValue(0, TimeUnit.Millisecond);
                else if (m_TransitionDuration.Count > i)
                    transition.duration = m_TransitionDuration[i];
                else
                    transition.duration = m_TransitionDuration[i%m_TransitionDuration.Count];

                if (null == m_TransitionDelay)
                    transition.delay = new TimeValue(0, TimeUnit.Millisecond);
                else if (m_TransitionDelay.Count > i)
                    transition.delay = m_TransitionDelay[i];
                else
                    transition.delay = m_TransitionDelay[i%m_TransitionDelay.Count];

                if (null == m_TransitionTimingFunction)
                    transition.timingFunction = new EasingFunction(EasingMode.Ease);
                else if (m_TransitionTimingFunction.Count > i)
                    transition.timingFunction = m_TransitionTimingFunction[i];
                else
                    transition.timingFunction = m_TransitionTimingFunction[i%m_TransitionTimingFunction.Count];

                m_Data.Add(transition);
            }
            m_ListView.RefreshItems();
        }

        private int GetMaxCount()
        {
            var count = 0;
            if (m_TransitionProperty?.Count > count)
            {
                count = m_TransitionProperty.Count;
            }
            if (m_TransitionDuration?.Count > count)
            {
                count = m_TransitionDuration.Count;
            }
            if (m_TransitionDelay?.Count > count)
            {
                count = m_TransitionDelay.Count;
            }
            if (m_TransitionTimingFunction?.Count > count)
            {
                count = m_TransitionTimingFunction.Count;
            }

            return count;
        }
    }
}
