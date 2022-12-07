using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheets : BuilderExplorer
    {
        static readonly string kToolbarPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorControls.uxml";
        static readonly string kHelpTooltipPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorHelpTips.uxml";

        ToolbarMenu m_AddUSSMenu;
        BuilderNewSelectorField m_NewSelectorField;
        TextField m_NewSelectorTextField;
        VisualElement m_NewSelectorTextInputField;
        ToolbarMenu m_PseudoStatesMenu;
        BuilderTooltipPreview m_TooltipPreview;
        BuilderStyleSheetsDragger m_StyleSheetsDragger;

        enum FieldFocusStep
        {
            Idle,
            FocusedFromStandby,
            NeedsSelectionOverride
        }

        FieldFocusStep m_FieldFocusStep;
        bool m_ShouldRefocusSelectorFieldOnBlur;

        BuilderDocument document => m_PaneWindow?.document;

        public BuilderStyleSheets(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderStyleSheetsDragger styleSheetsDragger,
            HighlightOverlayPainter highlightOverlayPainter,
            BuilderTooltipPreview tooltipPreview)
            : base(
                paneWindow,
                viewport,
                selection,
                classDragger,
                styleSheetsDragger,
                new BuilderStyleSheetsContextMenu(paneWindow, selection),
                viewport.styleSelectorElementContainer,
                false,
                highlightOverlayPainter,
                kToolbarPath)
        {
            m_TooltipPreview = tooltipPreview;
            if (m_TooltipPreview != null)
            {
                var helpTooltipTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(kHelpTooltipPath);
                var helpTooltipContainer = helpTooltipTemplate.CloneTree();
                m_TooltipPreview.Add(helpTooltipContainer); // We are the only ones using it so just add the contents and be done.
            }

            viewDataKey = "builder-style-sheets";
            AddToClassList(BuilderConstants.ExplorerStyleSheetsPaneClassName);

            var parent = this.Q("new-selector-item");

            // Init text field.
            m_NewSelectorField = parent.Q<BuilderNewSelectorField>("new-selector-field");
            m_NewSelectorTextField = m_NewSelectorField.textField;
            m_NewSelectorTextField.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            m_NewSelectorTextInputField = m_NewSelectorTextField.Q("unity-text-input");
            m_NewSelectorTextInputField.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);
            UpdateNewSelectorFieldEnabledStateFromDocument();

            m_NewSelectorTextInputField.RegisterCallback<FocusEvent>((evt) =>
            {
                var input = evt.elementTarget;
                var field = GetTextFieldParent(input);
                m_FieldFocusStep = FieldFocusStep.FocusedFromStandby;
                if (field.text == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage || m_ShouldRefocusSelectorFieldOnBlur)
                {
                    m_ShouldRefocusSelectorFieldOnBlur = false;
                    field.value = BuilderConstants.UssSelectorClassNameSymbol;
                }

                ShowTooltip();
            }, TrickleDown.TrickleDown);

            m_NewSelectorTextField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                if (m_FieldFocusStep != FieldFocusStep.FocusedFromStandby)
                    return;

                m_FieldFocusStep = m_NewSelectorTextField.value == BuilderConstants.UssSelectorClassNameSymbol ? FieldFocusStep.NeedsSelectionOverride : FieldFocusStep.Idle;

                // We don't want the '.' we just inserted in the FocusEvent to be highlighted,
                // which is the default behavior. Same goes for when we add pseudo states with options menu.
                m_NewSelectorTextField.textSelection.SelectRange(m_NewSelectorTextField.value.Length, m_NewSelectorTextField.value.Length);
            });

            // Since MouseDown captures the mouse, we need to RegisterCallback directly on the target in order to intercept the event.
            // This could be replaced by setting selectAllOnMouseUp to false.
            m_NewSelectorTextInputField.Q<TextElement>().RegisterCallback<MouseUpEvent>((evt) =>
            {
                // We want to prevent the default action on mouse up in KeyboardTextEditor, but only when
                // the field selection behaviour was changed by us.
                if (m_FieldFocusStep != FieldFocusStep.NeedsSelectionOverride)
                    return;

                m_FieldFocusStep = FieldFocusStep.Idle;

                // Reselect on the next execution, after the KeyboardTextEditor selects all.
                m_NewSelectorTextInputField.schedule.Execute(() =>
                {
                    m_NewSelectorTextField.textSelection.SelectRange(m_NewSelectorTextField.value.Length, m_NewSelectorTextField.value.Length);
                });

            }, TrickleDown.TrickleDown);

            m_NewSelectorTextInputField.RegisterCallback<BlurEvent>((evt) =>
            {
                var input = evt.elementTarget;
                var field = GetTextFieldParent(input);

                HideTooltip();
                if (m_ShouldRefocusSelectorFieldOnBlur)
                {
                    field.schedule.Execute(PostEnterRefocus);
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                    return;
                }

                if (string.IsNullOrEmpty(field.text) || field.text == BuilderConstants.UssSelectorClassNameSymbol)
                {
                    field.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
                    m_PseudoStatesMenu.SetEnabled(false);
                }

            }, TrickleDown.TrickleDown);

            // Setup New USS Menu.
            m_AddUSSMenu = parent.Q<ToolbarMenu>("add-uss-menu");
            SetUpAddUSSMenu();

            // Setup pseudo states menu.
            m_PseudoStatesMenu = m_NewSelectorField.pseudoStatesMenu;

            // Update sub title.
            UpdateSubtitleFromActiveUSS();

            // Init drag stylesheet root
            classDragger.builderStylesheetRoot = container;
            styleSheetsDragger.builderStylesheetRoot = container;
            m_StyleSheetsDragger = styleSheetsDragger;

            RegisterCallback<GeometryChangedEvent>(e => AdjustPosition());
        }

        TextField GetTextFieldParent(VisualElement ve)
        {
            return ve.GetFirstAncestorOfType<TextField>();
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isCS = element.GetStyleComplexSelector() != null;
            var isSS = element.GetStyleSheet() != null;

            return isCS || isSS;
        }

        void PostEnterRefocus()
        {
            m_NewSelectorTextInputField.Focus();
        }

        void OnEnter(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            CreateNewSelector(document.activeStyleSheet);

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        void CreateNewSelector(StyleSheet styleSheet)
        {
            var newValue = m_NewSelectorTextField.text;
            if (newValue == BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage)
                return;

            if (styleSheet == null)
            {
                if (BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow))
                {
                    styleSheet = m_PaneWindow.document.firstStyleSheet;

                    // The EditorWindow will no longer have Focus after we show the
                    // Save Dialog so even though the New Selector field will appear
                    // focused, typing won't do anything. As such, it's better, in
                    // this one case to remove focus from this field so users know
                    // to re-focus it themselves before they can add more selectors.
                    m_NewSelectorTextField.value = string.Empty;
                    m_NewSelectorTextField.Blur();
                }
                else
                {
                    return;
                }
            }
            else
            {
                m_ShouldRefocusSelectorFieldOnBlur = true;
            }

            var newSelectorStr = newValue;
            if (newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol))
            {
                newSelectorStr = BuilderConstants.UssSelectorClassNameSymbol + newSelectorStr.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            }

            if (string.IsNullOrEmpty(newSelectorStr))
                return;

            if (newSelectorStr.Length == 1 && (
                newSelectorStr.StartsWith(BuilderConstants.UssSelectorClassNameSymbol)
                || newSelectorStr.StartsWith("-")
                || newSelectorStr.StartsWith("_")))
                return;

            if (!BuilderNameUtilities.styleSelectorRegex.IsMatch(newSelectorStr))
            {
                Builder.ShowWarning(BuilderConstants.StyleSelectorValidationSpacialCharacters);
                m_NewSelectorTextField.schedule.Execute(() =>
                {
                    m_NewSelectorTextField.SetValueWithoutNotify(newValue);
                    m_NewSelectorTextField.textSelection.SelectAll();
                });
                return;
            }

            var selectorContainerElement = m_Viewport.styleSelectorElementContainer;
            var newComplexSelector = BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, newSelectorStr);

            m_Selection.NotifyOfHierarchyChange();
            m_Selection.NotifyOfStylingChange();

            // Try to selected newly created selector.
            var newSelectorElement =
                m_Viewport.styleSelectorElementContainer.FindElement(
                    (e) => e.GetStyleComplexSelector() == newComplexSelector);
            if (newSelectorElement != null)
                m_Selection.Select(null, newSelectorElement);

            schedule.Execute(() =>
            {
                m_NewSelectorTextField.Blur();
                m_NewSelectorTextField.SetValueWithoutNotify(BuilderConstants.ExplorerInExplorerNewClassSelectorInfoMessage);
            });
        }

        void SetUpAddUSSMenu()
        {
            if (m_AddUSSMenu == null)
                return;

            m_AddUSSMenu.menu.MenuItems().Clear();

            {
                m_AddUSSMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu,
                    action =>
                    {
                        BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow);
                    });
                m_AddUSSMenu.menu.AppendAction(
                    BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu,
                    action =>
                    {
                        BuilderStyleSheetsUtilities.AddExistingUSSToAsset(m_PaneWindow);
                    });
            }
        }

        void ShowTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            if (m_TooltipPreview.isShowing)
                return;

            m_TooltipPreview.Show();

            AdjustPosition();
        }

        void AdjustPosition()
        {
            m_TooltipPreview.style.left = Mathf.Max(0, this.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset);
            m_TooltipPreview.style.top = m_Viewport.viewportWrapper.worldBound.y;
        }

        void HideTooltip()
        {
            if (m_TooltipPreview == null)
                return;

            m_TooltipPreview.Hide();
        }

        void UpdateNewSelectorFieldEnabledStateFromDocument()
        {
            m_NewSelectorTextField.SetEnabled(true);
            SetUpAddUSSMenu();
        }

        void UpdateSubtitleFromActiveUSS()
        {
            if (pane == null)
                return;

            if (document == null || document.activeStyleSheet == null)
            {
                pane.subTitle = string.Empty;
                return;
            }

            pane.subTitle = document.activeStyleSheet.name + BuilderConstants.UssExtension;
        }

        protected override void ElementSelectionChanged(List<VisualElement> elements)
        {
            base.ElementSelectionChanged(elements);

            UpdateSubtitleFromActiveUSS();
        }

        public override void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            base.HierarchyChanged(element, changeType);
            m_ElementHierarchyView.hasUssChanges = true;
            UpdateNewSelectorFieldEnabledStateFromDocument();
            UpdateSubtitleFromActiveUSS();
        }

        // Used by unit tests to reset state after stylesheets drag
        internal void ResetStyleSheetsDragger()
        {
            m_StyleSheetsDragger.Reset();
        }
    }
}
