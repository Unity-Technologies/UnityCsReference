// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    sealed class TransitionsListView : BindableElement
    {
        readonly UnityEngine.Pool.ObjectPool<FoldoutTransitionField> ss_Pool =
            new UnityEngine.Pool.ObjectPool<FoldoutTransitionField>(MakeItem);

        static FoldoutTransitionField MakeItem()
        {
            var foldoutField = new FoldoutTransitionField();
            foldoutField.header.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutField);

            foldoutField.propertyField.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutField);
            foldoutField.propertyField.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, StylePropertyId.TransitionProperty.UssName());

            foldoutField.durationField.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutField);
            foldoutField.durationField.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, StylePropertyId.TransitionDuration.UssName());

            foldoutField.timingFunctionField.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutField);
            foldoutField.timingFunctionField.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, StylePropertyId.TransitionTimingFunction.UssName());

            foldoutField.delayField.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutField);
            foldoutField.delayField.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, StylePropertyId.TransitionDelay.UssName());
            return foldoutField;
        }

        const string k_UssPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListView.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListViewDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/Transitions/TransitionsListViewLight.uss";

        const string k_BaseClass = "unity-transition-list-view";
        const string k_ContentClass = k_BaseClass + "__content";
        const string k_ItemClass = k_BaseClass + "__item";
        const string k_FirstItemClass = k_ItemClass + "--first";
        const string k_LastItemClass = k_ItemClass + "--last";
        const string k_OddItemClass = k_ItemClass + "--odd";
        const string k_EvenItemClass = k_ItemClass + "--even";
        const string k_AddButtonClass = k_BaseClass + "__add-transition-button";
        const string k_AddButtonIconClass = k_BaseClass + "__add-transition-button__icon";
        const string k_AddButtonLabelClass = k_BaseClass + "__add-transition-button__label";
        const string k_DisplayWarningClass = k_BaseClass + "__display-warning";
        const string k_NoDurationWarningClass = k_BaseClass + "__no-duration-warning";
        const string k_EditPropertyToAddWarningClass = k_BaseClass + "__edit-to-add-warning";

        const string k_AddButtonText = "Add Transition";

        public new class UxmlFactory : UxmlFactory<TransitionsListView, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits {}

        public override VisualElement contentContainer { get; }

        readonly Button m_AddTransitionButton;

        readonly HelpBox m_TransitionNotVisibleWarning;
        readonly HelpBox m_EditPropertyToAddNewTransitionWarning;

        public TransitionsListView()
        {
            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath);
            styleSheets.Add(styleSheet);

            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssDarkSkinPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssLightSkinPath));

            AddToClassList(k_BaseClass);

            var content = new VisualElement();
            contentContainer = content;
            content.AddToClassList(k_ContentClass);

            hierarchy.Add(content);

            m_AddTransitionButton = new Button(OnTransitionAdded);
            var addButtonIcon = new VisualElement();
            addButtonIcon.AddToClassList(k_AddButtonIconClass);
            m_AddTransitionButton.Add(addButtonIcon);
            var buttonLabel = new Label(k_AddButtonText);
            buttonLabel.AddToClassList(k_AddButtonLabelClass);
            m_AddTransitionButton.Add(buttonLabel);
            m_AddTransitionButton.AddToClassList(k_AddButtonClass);
            hierarchy.Add(m_AddTransitionButton);

            m_TransitionNotVisibleWarning = new HelpBox
            {
                text = BuilderConstants.TransitionWillNotBeVisibleBecauseOfDuration
            };
            m_TransitionNotVisibleWarning.AddToClassList(k_NoDurationWarningClass);
            hierarchy.Add(m_TransitionNotVisibleWarning);

            m_EditPropertyToAddNewTransitionWarning = new HelpBox
            {
                text = BuilderConstants.EditPropertyToAddNewTransition
            };
            m_EditPropertyToAddNewTransitionWarning.AddToClassList(k_EditPropertyToAddWarningClass);
            hierarchy.Add(m_EditPropertyToAddNewTransitionWarning);
        }

        void OnTransitionAdded()
        {
            var transition = BuilderTransition.Default;
            transition.property = BuilderTransition.IgnoredProperty;

            using (var evt = TransitionAddedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.transition = transition;
                SendEvent(evt);
            }
            // The usual GeometryChangedEvent doesn't seem to work at all here.
            schedule.Execute(ScrollToLastItem).StartingIn(20);
        }

        void ScrollToLastItem()
        {
            var sv = GetFirstAncestorOfType<ScrollView>();
            sv?.ScrollTo(m_AddTransitionButton);
        }

        internal void RemoveTransition(FoldoutTransitionField field)
        {
            using (var evt = TransitionRemovedEvent.GetPooled())
            {
                evt.elementTarget = this;
                evt.index = field.index;
                SendEvent(evt);
            }
        }

        internal void TransitionChanged(FoldoutTransitionField field, TransitionChangeType changeType)
        {
            using (var pooled = TransitionChangedEvent.GetPooled())
            {
                pooled.elementTarget = this;
                pooled.field = field;
                pooled.transition = field.GetTransitionData();
                pooled.changeType = changeType;
                pooled.index = field.index;
                SendEvent(pooled);
            }
        }

        public void TrimToCount(int count)
        {
            while (childCount > count)
            {
                var foldout = (FoldoutTransitionField) contentContainer[childCount - 1];
                ss_Pool.Release(foldout);
                contentContainer.RemoveAt(childCount - 1);
            }
        }

        public void Refresh(TransitionChangeType overrides, TransitionChangeType keywords, TransitionChangeType bindings)
        {
            var durationAllZeroes = true;
            using (ListPool<FoldoutTransitionField>.Get(out var list))
            {
                this.Query<FoldoutTransitionField>().ToList(list);

                if (list.Count == 1)
                    list[0].RemoveTransitionButton.SetEnabled(overrides.HasAnyFlag());
                else if (list.Count > 0)
                    list[0].RemoveTransitionButton.SetEnabled(true);

                for (var i = 0; i < list.Count; ++i)
                {
                    var foldout = list[i];

                    if (bindings.IsSet(TransitionChangeType.Property) && !BuilderBindingUtility.IsInlineEditingEnabled(foldout.propertyField))
                        foldout.propertyField.SetEnabled(false);
                    else if (keywords.IsSet(TransitionChangeType.Property))
                        foldout.propertyField.SetEnabled(i == 0);
                    else
                        foldout.propertyField.SetEnabled(true);

                    if (!foldout.durationField.isKeyword && foldout.durationField.length != 0)
                        durationAllZeroes = false;

                    if (bindings.IsSet(TransitionChangeType.Duration) && !BuilderBindingUtility.IsInlineEditingEnabled(foldout.durationField))
                        foldout.durationField.SetEnabled(false);
                    else if (keywords.IsSet(TransitionChangeType.Duration))
                        foldout.durationField.SetEnabled(i == 0);
                    else
                        foldout.durationField.SetEnabled(true);

                    if (bindings.IsSet(TransitionChangeType.TimingFunction) && !BuilderBindingUtility.IsInlineEditingEnabled(foldout.timingFunctionField))
                        foldout.timingFunctionField.SetEnabled(false);
                    else if (keywords.IsSet(TransitionChangeType.TimingFunction))
                        foldout.timingFunctionField.SetEnabled(i == 0);
                    else
                        foldout.timingFunctionField.SetEnabled(true);

                    if (bindings.IsSet(TransitionChangeType.Delay)&& !BuilderBindingUtility.IsInlineEditingEnabled(foldout.delayField))
                        foldout.delayField.SetEnabled(false);
                    else if (keywords.IsSet(TransitionChangeType.Delay))
                        foldout.delayField.SetEnabled(i == 0);
                    else
                        foldout.delayField.SetEnabled(true);
                    foldout.overrides = overrides;

                    foldout.EnableInClassList(k_FirstItemClass, i == 0);
                    foldout.EnableInClassList(k_LastItemClass, i == contentContainer.childCount - 1);
                    foldout.EnableInClassList(k_EvenItemClass, i % 2 == 0);
                    foldout.EnableInClassList(k_OddItemClass, i % 2 != 0);
                }
            }

            var hasBindings = bindings.HasAnyFlag();
            var mustEditToAddTransition = (overrides == keywords && overrides.HasAnyFlag());

            m_AddTransitionButton.SetEnabled(!mustEditToAddTransition && !hasBindings);
            m_EditPropertyToAddNewTransitionWarning.EnableInClassList(k_DisplayWarningClass, mustEditToAddTransition);

            var transitionWillBeInstant = durationAllZeroes && overrides.IsSet(TransitionChangeType.Duration);
            m_TransitionNotVisibleWarning.EnableInClassList(k_DisplayWarningClass, transitionWillBeInstant);
        }

        public bool GetOrCreateTransitionField(out FoldoutTransitionField field)
        {
            var newItem = ss_Pool.CountInactive == 0;
            field = ss_Pool.Get();
            Add(field);
            return newItem;
        }
    }
}
