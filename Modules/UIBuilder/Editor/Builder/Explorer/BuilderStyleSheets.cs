// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheets : BuilderExplorer
    {
        static readonly string kToolbarPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorControls.uxml";
        private static readonly string kMessageLinkClassName = "unity-builder-message-link";

        ToolbarMenu m_AddUSSMenu;
        BuilderNewSelectorField m_NewSelectorField;
        BuilderTooltipPreview m_TooltipPreview;
        Label m_MessageLink;
        BuilderStyleSheetsDragger m_StyleSheetsDragger;
        Label m_EmptyStyleSheetsPaneLabel;

        BuilderDocument document => m_PaneWindow?.document;
        public BuilderNewSelectorField newSelectorField => m_NewSelectorField;

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
                kToolbarPath,
                "StyleSheets")
        {
            m_TooltipPreview = tooltipPreview;
            if (m_TooltipPreview != null)
            {
                m_TooltipPreview.Add(BuilderStyleSheetsNewSelectorHelpTips.Create());
                m_MessageLink = m_TooltipPreview.Q<Label>(null, kMessageLinkClassName);
                m_MessageLink.focusable = true;
            }

            viewDataKey = "builder-style-sheets";
            AddToClassList(BuilderConstants.ExplorerStyleSheetsPaneClassName);

            var parent = this.Q("new-selector-item");

            // Init text field.
            m_NewSelectorField = parent.Q<BuilderNewSelectorField>("new-selector-field");

            m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnNewSelectorSubmit);
            UpdateNewSelectorFieldEnabledStateFromDocument();

            m_NewSelectorField.RegisterCallback<FocusEvent>((evt) =>
            {
                ShowTooltip();
            }, TrickleDown.TrickleDown);

            m_NewSelectorField.RegisterCallback<BlurEvent>((evt) =>
            {
                if (focusController.focusedElement == m_MessageLink)
                    return;

                if (focusController.IsPendingFocus(m_MessageLink))
                    return;

                HideTooltip();
            }, TrickleDown.TrickleDown);

            m_MessageLink?.RegisterCallback<BlurEvent>(evt =>
            {
                HideTooltip();
            });

            m_MessageLink?.RegisterCallback<ClickEvent>(evt =>
            {
                HideTooltip();
            });

            // Setup New USS Menu.
            m_AddUSSMenu = parent.Q<ToolbarMenu>("add-uss-menu");
            SetUpAddUSSMenu();

            // Update sub title.
            UpdateSubtitleFromActiveUSS();

            // Init drag stylesheet root
            classDragger.builderStylesheetRoot = container;
            styleSheetsDragger.builderStylesheetRoot = container;
            m_StyleSheetsDragger = styleSheetsDragger;

            RegisterCallback<GeometryChangedEvent>(e => AdjustPosition());

            // Create the empty state label here because this file shares a UXML with BuilderHierarchy
            m_EmptyStyleSheetsPaneLabel = new Label("Click the + icon to create a new StyleSheet.");
            m_EmptyStyleSheetsPaneLabel.AddToClassList(BuilderConstants.ExplorerDayZeroStateLabelClassName);
            m_EmptyStyleSheetsPaneLabel.style.display = DisplayStyle.None;
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
            {
                return;
            }

            pane.AppendActionToEllipsisMenu(L10n.Tr("Full selector text"),
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.FullSelectorText),
            a => m_ElementHierarchyView.elementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.FullSelectorText)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isCS = element.GetStyleComplexSelector() != null;
            var isSS = element.GetStyleSheet() != null;

            return isCS || isSS;
        }

        void OnNewSelectorSubmit(NewSelectorSubmitEvent evt)
        {
            if (string.IsNullOrEmpty(evt.selectorStr))
                return;

            // TODO: Add validation
            CreateNewSelector(document.activeStyleSheet, evt.selectorStr);
        }

        void CreateNewSelector(StyleSheet styleSheet, string selectorStr)
        {
            if (styleSheet == null)
            {
                if (BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow))
                {
                    styleSheet = m_PaneWindow.document.firstStyleSheet;

                    // The EditorWindow will no longer have Focus after we show the
                    // Save dialog, so we need to manually refocus it.
                    var p = (EditorPanel) panel;
                    if (p.ownerObject is HostView view && view)
                    {
                        view.Focus();
                    }
                }
                else
                {
                    return;
                }
            }

            var newSelectorStr = selectorStr.Trim();
            var selectorTypeSymbol = (newSelectorStr[0]) switch
            {
                '.' => BuilderConstants.UssSelectorClassNameSymbol,
                '#' => BuilderConstants.UssSelectorNameSymbol,
                ':' => BuilderConstants.UssSelectorPseudoStateSymbol,
                _ => ""
            };
            if (!string.IsNullOrEmpty(selectorTypeSymbol))
            {
                newSelectorStr = selectorTypeSymbol + newSelectorStr.Trim(selectorTypeSymbol[0]).Trim();
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
                m_NewSelectorField.schedule.Execute(() =>
                {
                    m_NewSelectorField.value = selectorStr;
                    m_NewSelectorField.SelectAll();
                });
                return;
            }

            var selectorContainerElement = m_Viewport.styleSelectorElementContainer;
            if (!SelectorUtility.TryCreateSelector(newSelectorStr, out var complexSelector, out var error))
            {
                Builder.ShowWarning(error);
                return;
            }

            var newComplexSelector = BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, complexSelector);

            m_Selection.NotifyOfHierarchyChange();
            m_Selection.NotifyOfStylingChange();

            // Try to select newly created selector.
            var newSelectorElement =
                m_Viewport.styleSelectorElementContainer.FindElement(
                    (e) => e.GetStyleComplexSelector() == newComplexSelector);
            if (newSelectorElement != null)
                m_Selection.Select(null, newSelectorElement);
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

            // Show empty state if no stylesheet loaded
            if (document.activeStyleSheet == null)
            {
                m_ElementHierarchyView.container.style.justifyContent = Justify.Center;
                m_ElementHierarchyView.treeView.style.flexGrow = document.activeOpenUXMLFile.isChildSubDocument ? 1 : 0;
                m_EmptyStyleSheetsPaneLabel.style.display = DisplayStyle.Flex;
                m_ElementHierarchyView.container.Add(m_EmptyStyleSheetsPaneLabel);
                m_EmptyStyleSheetsPaneLabel.SendToBack();
            }
            else
            {
                if (m_EmptyStyleSheetsPaneLabel.parent != m_ElementHierarchyView.container)
                    return;

                // Revert inline style changes to default
                m_ElementHierarchyView.container.style.justifyContent = Justify.FlexStart;
                elementHierarchyView.treeView.style.flexGrow = 1;
                m_EmptyStyleSheetsPaneLabel.style.display = DisplayStyle.None;
                m_EmptyStyleSheetsPaneLabel.RemoveFromHierarchy();
            }
        }

        // Used by unit tests to reset state after stylesheets drag
        internal void ResetStyleSheetsDragger()
        {
            m_StyleSheetsDragger.Reset();
        }
    }
}
