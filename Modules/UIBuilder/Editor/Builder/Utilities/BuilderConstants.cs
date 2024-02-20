// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class BuilderConstants
    {
        // Builder
        public const string BuilderWindowTitle = "UI Builder";
        public const string BuilderWindowIcon = IconsResourcesPath + "/Generic/UIBuilder";
        public const string BuilderMenuEntry = "Window/UI Toolkit/UI Builder";
        // These sizes are copied from EditorWindow.cs. See the default values of EditorWindow.m_MinSize and EditorWindow.m_MaxSize.
        public static readonly Vector2 BuilderWindowDefaultMinSize = new Vector2(100, 100);
        public static readonly Vector2 BuilderWindowDefaultMaxSize = new Vector2(4000, 4000);

        // Numbers
        public static readonly int VisualTreeAssetOrderIncrement = 10;
        public static readonly int VisualTreeAssetOrderHalfIncrement = 5;
        public static readonly float CanvasInitialWidth = 350;
        public static readonly float CanvasInitialHeight = 450; // Making this too large might break tests.
        public static readonly float CanvasMinWidth = 100;
        public static readonly float CanvasMinHeight = 100;
        public static readonly float ClassNameInPillMinWidth = 180f;
        public static readonly float TooltipPreviewYOffset = 20;
        public static readonly float ViewportInitialZoom = 1.0f;
        public static readonly Vector2 ViewportInitialContentOffset = new Vector2(20.0f, 20.0f);
        public static readonly int DoubleClickDelay = 50;
        public static readonly int CanvasGameViewSyncInterval = 100;
        public static readonly float OpacityFadeOutFactor = 0.5f;
        public const int MaxTextMeshVertices = 48 * 1024; // Max 48k vertices. We leave room for masking, borders, background, etc. see UIRMeshBuilder.cs
        public const int MaxTextPrintableCharCount = (int)((2 / 3.0) * MaxTextMeshVertices / 4 /* = vertices per quad*/);
        public static readonly float PickSelectionRepeatRectSize = 2f;
        public static readonly float PickSelectionRepeatRectHalfSize = PickSelectionRepeatRectSize / 2;
        public static readonly double PickSelectionRepeatMinTimeDelay = 0.5;
        public static readonly int OpenInIDELineNumber = 1;

        // Paths
        public const string UIBuilderPackageRootPath = "UIBuilderPackageResources";
        public const string UIBuilderPackagePath = UIBuilderPackageRootPath + "/UI";
        public const string UtilitiesPath = UIBuilderPackageRootPath + "/Utilities";
        public const string IconsResourcesPath = UIBuilderPackageRootPath + "/Icons";
        public const string UIBuilderTestsRootPath = "Assets/Editor";
        public const string LibraryUIPath = UIBuilderPackagePath + "/Library";
        public const string LibraryUssPathNoExt = UIBuilderPackagePath + "/Library/BuilderLibrary";
        public const string InspectorUssPathNoExt = UIBuilderPackagePath + "/Inspector/BuilderInspector";

        public const string UIBuilderTestsTestFilesPath = UIBuilderTestsRootPath + "/TestFiles";
        const string BuilderDocumentDiskJsonFileName = "UIBuilderDocument.json";
        const string BuilderDocumentDiskJsonFolderPath = "Library/UIBuilder";
        const string BuilderDocumentDiskSettingsJsonFolderPath = "Library/UIBuilder/DocumentSettings";
        public const string EditorResourcesBundlePath = "Library/unity editor resources";

        public const string UssPath_BuilderWindow = BuilderConstants.UIBuilderPackagePath + "/Builder.uss";
        public const string UssPath_BuilderWindow_Dark = BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss";
        public const string UssPath_BuilderWindow_Light = BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss";
        public static string UssPath_BuilderWindow_Themed => EditorGUIUtility.isProSkin ? UssPath_BuilderWindow_Dark : UssPath_BuilderWindow_Light;

        public const string UssPath_InspectorWindow = BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderInspector.uss";
        public const string UssPath_InspectorWindow_Dark = BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderInspectorDark.uss";
        public const string UssPath_InspectorWindow_Light = BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderInspectorLight.uss";
        public static string UssPath_InspectorWindow_Themed => EditorGUIUtility.isProSkin ? UssPath_InspectorWindow_Dark : UssPath_InspectorWindow_Light;

        public const string UssPath_BindingWindow = BuilderConstants.UIBuilderPackagePath + "/Inspector/BindingWindow.uss";

        // Global Style Class Names
        public static readonly string HiddenStyleClassName = "unity-builder-hidden";
        public static readonly string ReadOnlyStyleClassName = "unity-builder--readonly";
        public static readonly string ElementTypeClassName = "unity-builder-code-label--element-type";
        public static readonly string ElementNameClassName = "unity-builder-code-label--element-name";
        public static readonly string ElementClassNameClassName = "unity-builder-code-label--element-class-name";
        public static readonly string ElementAttachedStyleSheetClassName = "unity-builder-code-label--element-attached-stylesheet";
        public static readonly string ElementPseudoStateClassName = "unity-builder-code-label--element-pseudo-state";
        public static readonly string TagPillClassName = "unity-builder-tag-pill";
        public static readonly string StyleSelectorBelongsParent = "unity-selector-parent-subdocument";
        public static readonly string SeparatorLineStyleClassName = "unity-builder-separator-line";

        // Random Symbols
        public static readonly string SingleSpace = " ";
        public static readonly string Underscore = "_";
        public static readonly string TripleSpace = "   "; // Don't ask.
        public static readonly string SubtitlePrefix = " - ";
        public const string WindowsNewlineChar = "\r\n";
        public const string UnixNewlineChar = "\n";
        public const string NewlineChar = UnixNewlineChar;
        public const string OpenBracket = "(";
        public const string CloseBracket = ")";
        public const string EllipsisText = "...";

        // Notifications
        public static readonly string previewNotificationKey = "builder-preview-mode-notification";
        public static readonly string inlineEditingNotificationKey = "builder-inline-editing-notification";

        //
        // Elements
        //

        // Special Element Names
        public static readonly string StyleSelectorElementContainerName = "__unity-selector-container-element";
        public static readonly string StyleSelectorElementName = "__unity-selector-element";

        // Element Linked VE Property Names
        public static readonly string ElementLinkedStyleSheetVEPropertyName = "__unity-ui-builder-linked-stylesheet";
        public static readonly string ElementLinkedStyleSheetIndexVEPropertyName = "__unity-ui-builder-linked-stylesheet-index";
        public static readonly string ElementLinkedStyleSelectorVEPropertyName = "__unity-ui-builder-linked-style-selector";
        public static readonly string ElementLinkedFakeStyleSelectorVEPropertyName = "__unity-ui-builder-linked-fake-style-selector";
        public static readonly string ElementLinkedVisualTreeAssetVEPropertyName = "__unity-ui-builder-linked-visual-tree-asset";
        public static readonly string ElementLinkedVisualElementAssetVEPropertyName = "__unity-ui-builder-linked-visual-element-asset";
        public static readonly string ElementLinkedInstancedVisualTreeAssetVEPropertyName = "__unity-ui-builder-instanced-visual-tree-asset";
        public static readonly string ElementLinkedBelongingVisualTreeAssetVEPropertyName = "__unity-ui-builder-belonging-visual-tree-asset";
        public static readonly string ElementLinkedExplorerItemVEPropertyName = "__unity-ui-builder-linked-explorer-item-element";
        public static readonly string ElementLinkedDocumentVisualElementVEPropertyName = "__unity-ui-builder-linked-document-visual-element";
        public static readonly string ElementLinkedVariableHandlerVEPropertyName = "__unity-ui-builder-linked-variable-handler";
        public static readonly string ElementLinkedVariableTooltipVEPropertyName = "__unity-ui-builder-linked-variable-tooltip";
        public static readonly string ElementLinkedActiveThemeStyleSheetVEPropertyName = "__unity-ui-builder-linked-active-theme-stylesheet";

        //
        // Inspector
        //

        // Inspector Style VE Property Names
        public static readonly string InspectorStylePropertyNameVEPropertyName = "__unity-ui-builder-style-property-name";
        public static readonly string InspectorAttributeBindingPropertyNameVEPropertyName = "__unity-ui-builder-attribute-binding-property-name";
        public static readonly string InspectorClassPillLinkedSelectorElementVEPropertyName = "__unity-ui-builder-class-linked-pill-selector-element";

        // Inspector Style Property and Class Names
        public static readonly string InspectorFoldoutOverrideClassName = "unity-foldout--override";
        public static readonly string InspectorCategoryFoldoutOverrideClassName = "unity-builder-inspector__category-foldout--override";
        public static readonly string BuilderStyleRowBlueOverrideBoxClassName = "unity-builder-inspector-blue-override-box";
        public static readonly string FoldoutFieldPropertyName = "unity-foldout-field";
        public static readonly string BoundFoldoutFieldClassName = FoldoutFieldPropertyName + "--binding";
        public static readonly string FoldoutFieldHeaderClassName = FoldoutFieldPropertyName + "__header";
        public static readonly string InspectorMultiLineTextFieldClassName = "unity-builder-inspector__multi-line-text-field";
        public static readonly string InspectorFlexColumnModeClassName = "unity-builder-inspector--flex-column";
        public static readonly string InspectorFlexColumnReverseModeClassName = "unity-builder-inspector--flex-column-reverse";
        public static readonly string InspectorFlexRowModeClassName = "unity-builder-inspector--flex-row";
        public static readonly string InspectorFlexRowReverseModeClassName = "unity-builder-inspector--flex-row-reverse";
        public static readonly string InspectorStyleCategoryFoldoutOverrideClassName = "unity-builder-inspector__style-category-foldout--override";
        public static readonly string InspectorCategoryFoldoutBindingClassName = "unity-builder-inspector__style-category-foldout--binding";
        public static readonly string InspectorLocalStyleOverrideClassName = "unity-builder-inspector__style--override";
        public static readonly string InspectorLocalStyleResetClassName = "unity-builder-inspector__style--reset"; // used to reset font style of children
        public static readonly string InspectorLocalStyleUnresolvedVariableClassName = "unity-builder-inspector__style--unresolved-variable";
        public static readonly string InspectorLocalStyleVariableClassName = "unity-builder-inspector__style--variable";
        public static readonly string InspectorLocalStyleVariableEditingClassName = "unity-builder-inspector__style--variable-editing";
        public static readonly string InspectorLocalStyleInheritedClassName = "unity-builder-inspector__style--inherited";
        public static readonly string InspectorLocalStyleSelectorClassName = "unity-builder-inspector__style--uss-selector";
        public static readonly string InspectorLocalStyleBindingClassName = "unity-builder-inspector__style--binding";
        public static readonly string InspectorLocalStyleUnresolvedBindingClassName = "unity-builder-inspector__style--unresolved-binding";
        public static readonly string InspectorLocalStyleSelectorElementClassName = "unity-builder-inspector__style--selector-element";
        public static readonly string InspectorLocalStyleDefaultStatusClassName = "unity-builder-inspector__style--default";
        public static readonly string InspectorEmptyFoldoutLabelClassName = "unity-builder-inspector__empty-foldout-label";
        public static readonly string InspectorClassPillNotInDocumentClassName = "unity-builder-class-pill--not-in-document";
        public static readonly string InspectorClassHelpBox = "unity-builder-inspector__help-box";
        public static readonly string InspectorContainerClassName = "unity-builder-inspector__container";
        public static readonly string InspectorMultiFieldsRowClassName = "unity-builder-composite-field-row";
        public static readonly string InspectorCompositeStyleRowElementClassName = "unity-builder-composite-style-row-element";
        public static readonly string InspectorBindingIndicatorClassName = "unity-builder-foldout-binding-indicator";
        public static readonly string InspectorFieldBindingInlineEditingEnabledClassName = "unity-builder-binding-inline-editing-enabled";
        public static readonly string InspectorFixedItemHeightFieldClassName = "unity-builder-uxml-attribute__fixed-item-height";
        public static readonly string InspectorShownNegativeWarningMessageClassName = "unity-builder-uxml-attribute__negative-warning--shown";
        public static readonly string InspectorHiddenNegativeWarningMessageClassName = "unity-builder-uxml-attribute__negative-warning--hidden";
        public static readonly string InspectorListViewAllowAddRemoveFieldClassName = "unity-builder-uxml-attribute__allow-add-remove";

        // Inspector Links VE Property Names
        public static readonly string InspectorLinkedFieldsForStyleRowVEPropertyName = "__unity-ui-builder-fields-for-row";
        public static readonly string InspectorLinkedStyleRowVEPropertyName = "__unity-ui-builder-style-row";
        public static readonly string InspectorLinkedAttributeDescriptionVEPropertyName = "__unity-ui-builder-attribute-description";
        public static readonly string InspectorFieldValueInfoVEPropertyName = "__unity-ui-builder-property-value-info";
        public static readonly string InspectorFieldBindingInlineEditingEnabledPropertyName =
            "__unity-builder-binding-inline-editing-enabled";
        public static readonly string InspectorTempSerializedDataPropertyName = "__unity-ui-builder-temp-serialized-data";

        // Inspector Header
        public static readonly string BuilderInspectorSelector = "Selector";
        public static readonly string BuilderInspectorTemplateInstance = "Template Instance";
        public static readonly string BuilderAttributesHeader = "Attributes";

        // Inspector Messages
        public static readonly string AddStyleClassValidationSpaces = "Class names cannot contain spaces.";
        public static readonly string AddStyleClassValidationSpacialCharacters = "Class names can only contain letters, numbers, underscores, and dashes.";
        public static readonly string ContextMenuSetAsInlineValueMessage = "Set as inline value";
        public static readonly string ContextMenuSetAsValueMessage = "Set as value";
        public static readonly string ContextMenuUnsetMessage = "Unset";
        public static readonly string ContextMenuUnsetObjectMessage = "Unset Object Source";
        public static readonly string ContextMenuUnsetTypeMessage = "Unset Type Source";
        public static readonly string ContextMenuUnsetAllMessage = "Unset all";
        public static readonly string ContextMenuViewVariableMessage = "View variable";
        public static readonly string ContextMenuSetVariableMessage = "Set variable";
        public static readonly string ContextMenuEditVariableMessage = "Edit variable";
        public static readonly string ContextMenuRemoveVariableMessage = "Remove variable";
        public static readonly string ContextMenuOpenSelectorInIDEMessage = "Open selector in IDE";
        public static readonly string ContextMenuGoToSelectorMessage = "Go to selector";
        public static readonly string ContextMenuAddBindingMessage = "Add binding...";
        public static readonly string ContextMenuEditBindingMessage = "Edit binding...";
        public static readonly string ContextMenuRemoveBindingMessage = "Remove binding";
        public static readonly string ContextMenuEditInlineValueMessage = "Edit inline value...";
        public static readonly string ContextMenuUnsetInlineValueMessage = "Unset inline value";
        public static readonly string InspectorClassPillDoubleClickToCreate = "Double-click to create new USS selector.";
        public static readonly string InspectorClassPillDoubleClickToSelect = "Double-click to select and edit USS selector.";
        public static readonly string InspectorLocalStylesSectionTitleForSelector = "Styles";
        public static readonly string InspectorLocalStylesSectionTitleForElement = "Inlined Styles";
        public static readonly string MultiSelectionNotSupportedMessage = "Multi-selection editing is not supported.";
        public static readonly string InspectorEditorExtensionAuthoringActivated = "You can now use Editor-only controls in this document.";
        public static readonly string VariableNotSupportedInInlineStyleMessage = "Setting variables in inline style is not yet supported.";
        public static readonly string VariableDescriptionsCouldNotBeLoadedMessage = "Could not load the variable descriptions file.";
        public static readonly string NoNameElementAttributes = "A name is required in order to edit attributes.";
        public static readonly string TransitionWillNotBeVisibleBecauseOfDuration = "In order to be visible, at least one transition on this element should have a duration greater than 0.";
        public static readonly string EditPropertyToAddNewTransition = "Set a property to a non-keyword value to add a new transition.";
        public static readonly string FileNotFoundMessage = "File not found";
        public static readonly string HeaderSectionHelpBoxMessage = "This control is not supported in Runtime UI. Remove it, or enable Editor Extension Authoring in the Library.";
        public static readonly string UnresolvedValue = "Unresolved";
        public static readonly string HeightIntFieldValueCannotBeNegativeMessage = "Please enter a positive number. Non-positive numbers will default to 1.";
        public static readonly string UnnamedValue = "<No Name>";

        // Tooltip Messages
        public static readonly string FieldStatusIndicatorDefaultTooltip = "Default Value";
        public static readonly string FieldStatusIndicatorInlineTooltip = "Inline Value\n\nValue is set directly from the property field.";
        public static readonly string FieldStatusIndicatorInheritedTooltip = "Inherited Value\n\nParent: {0} {1}";
        public static readonly string FieldStatusIndicatorLocalTooltip = "Local Value\n\nValue is set directly from the property field.";
        public static readonly string FieldStatusIndicatorFromSelectorTooltip = "Selector Value\n\nSelector: {0}\nSheet: {1}";
        public static readonly string FieldStatusIndicatorVariableTooltip = "Variable Value\n\nVariable: {0}\nSheet: {1}";
        public static readonly string FieldStatusIndicatorUnresolvedVariableTooltip = "Unresolved variable\n\nVariable or Sheet is missing.";
        public static readonly string FieldStatusIndicatorResolvedBindingTooltip = "Resolved binding";
        public static readonly string FieldStatusIndicatorUnresolvedBindingTooltip = "Unresolved binding\nEdit binding for more details.";
        public static readonly string FieldStatusIndicatorUnhandledBindingTooltip = "Unhandled binding (resolution pending)";

        public static readonly string FieldValueBindingInfoTypeEnumUSSVariableDisplayString = "USS variable";

        public static readonly string FieldValueSourceInfoTypeEnumInheritedDisplayString = "Inherited from parent";
        public static readonly string FieldValueSourceInfoTypeEnumUSSSelectorDisplayString = "USS selector";
        public static readonly string FieldValueSourceInfoTypeEnumLocalUSSSelectorDisplayString = "Local";

        public static readonly string FieldValueInfoTypeEnumUSSPropertyDisplayString = "USS property";
        public static readonly string FieldValueInfoTypeEnumUXMLAttributeDisplayString = "UXML attribute";

        public static readonly string FieldTooltipNameOnlyFormatString = "{0}: {1}";
        public static readonly string FieldTooltipValueOnlyFormatString = "Value: {0}{1}\n\nValue definition: {2}{3}";
        public static readonly string FieldTooltipFormatString = "{0}: {1}\n\nValue: {2}{3}\n\nValue definition: {4}{5}";
        public static readonly string FieldTooltipWithoutValueFormatString = "{0}: {1}\n\nValue definition: {4}{5}";
        public static readonly string FieldTooltipDataDefinitionBindingFormatString = "{0}\n\nData Source: {1}\nData Source Path: {2}\nBinding Mode: {3}\nConverter(s) Used: {4}";
        public static readonly string FieldTooltipWithDescription = "{0}: {1}\n\n{2}";
        public static readonly string InputFieldStyleValueTooltipWithDescription = "value: {0}\n\n{1}";
        public static readonly string FieldTooltipDictionarySeparator = "__";
        public static readonly string InputFieldStyleValueTooltipDictionaryKeyFormat = "{0}"+FieldTooltipDictionarySeparator+"{1}";
        public static readonly string MatchingStyleSheetRuleSourceTooltipFormatString = "    Selector: {0}\n    Sheet: {1}";
        public static readonly string VariableBindingTooltipFormatString =  "    Name: {0}\n    Sheet: {1}";
        public static readonly Dictionary<string, string> InspectorStylePropertiesTooltipsDictionary = new Dictionary<string, string>()
        {
            {"align-content", "Alignment of the whole area of children on the cross axis if they span over multiple lines in this container."},
            {"align-items", "Alignment of children on the cross axis of this container."},
            {"align-self", "Similar to align-items, but only for this specific element."},
            {"all", "Allows resetting all properties with the initial keyword. Does not apply to custom USS properties."},
            {"background-color", "Background color to paint in the element's box."},
            {"background-image", "Background image to paint in the element's box."},
            {"background-position", "Background image position value."},
            {"background-position-x", "Background image x position value."},
            {"background-position-y", "Background image y position value."},
            {"background-repeat", "Background image repeat value."},
            {"background-size", "Background image size value."},
            {"border-bottom-color", "Color of the element's bottom border."},
            {"border-bottom-left-radius", "The radius of the bottom-left corner when a rounded rectangle is drawn in the element's box."},
            {"border-bottom-right-radius", "The radius of the bottom-right corner when a rounded rectangle is drawn in the element's box."},
            {"border-bottom-width", "Space reserved for the bottom edge of the border during the layout phase."},
            {"border-color", ""},
            {"border-left-color", "Color of the element's left border."},
            {"border-left-width", "Space reserved for the left edge of the border during the layout phase."},
            {"border-radius", ""},
            {"border-right-color", "Color of the element's right border."},
            {"border-right-width", "Space reserved for the right edge of the border during the layout phase."},
            {"border-top-color", "Color of the element's top border."},
            {"border-top-left-radius", "The radius of the top-left corner when a rounded rectangle is drawn in the element's box."},
            {"border-top-right-radius", "The radius of the top-right corner when a rounded rectangle is drawn in the element's box."},
            {"border-top-width", "Space reserved for the top edge of the border during the layout phase."},
            {"border-width", ""},
            {"bottom", "Bottom distance from the element's box during layout."},
            {"color", "Color to use when drawing the text of an element."},
            {"cursor", "Mouse cursor to display when the mouse pointer is over an element."},
            {"display", "Defines how an element is displayed in the layout."},
            {"flex", ""},
            {"flex-basis", "Initial main size of a flex item, on the main flex axis. The final layout might be smaller or larger, according to the flex shrinking and growing determined by the other flex properties."},
            {"flex-direction", "Direction of the main axis to layout children in a container."},
            {"flex-grow", "Specifies how the item will shrink relative to the rest of the flexible items inside the same container."},
            {"flex-shrink", "Specifies how the item will shrink relative to the rest of the flexible items inside the same container."},
            {"flex-wrap", "Placement of children over multiple lines if not enough space is available in this container."},
            {"font-size", "Font size to draw the element's text."},
            {"height", "Fixed height of an element for the layout."},
            {"justify-content", "Justification of children on the main axis of this container."},
            {"left", "Left distance from the element's box during layout."},
            {"letter-spacing", "Increases or decreases the space between characters."},
            {"margin", ""},
            {"margin-bottom", "Space reserved for the bottom edge of the margin during the layout phase."},
            {"margin-left", "Space reserved for the left edge of the margin during the layout phase."},
            {"margin-right", "Space reserved for the right edge of the margin during the layout phase."},
            {"margin-top", "Space reserved for the top edge of the margin during the layout phase."},
            {"max-height", "Maximum height for an element, when it is flexible or measures its own size."},
            {"max-width", "Maximum width for an element, when it is flexible or measures its own size."},
            {"min-height", "Minimum height for an element, when it is flexible or measures its own size."},
            {"min-width", "Minimum width for an element, when it is flexible or measures its own size."},
            {"opacity", "Specifies the transparency of an element and of its children. Each child element has the same opacity value as the parent element."},
            {"overflow", "How a container behaves if its content overflows its own box."},
            {"padding", ""},
            {"padding-bottom", "Space reserved for the bottom edge of the padding during the layout phase."},
            {"padding-left", "Space reserved for the left edge of the padding during the layout phase."},
            {"padding-right", "Space reserved for the right edge of the padding during the layout phase."},
            {"padding-top", "Space reserved for the top edge of the padding during the layout phase."},
            {"position", "Element's positioning in its parent container."},
            {"right", "Right distance from the element's box during layout."},
            {"rotate", "A rotation transformation."},
            {"scale", "A scaling transformation."},
            {"text-overflow", "The element's text overflow mode."},
            {"text-shadow", "Drop shadow of the text."},
            {"top", "Top distance from the element's box during layout."},
            {"transform-origin", "The transformation origin is the point around which a transformation is applied."},
            {"transition", ""},
            {"transition-delay", "Duration to wait before starting a property's transition effect when its value changes."},
            {"transition-duration", "Time a transition animation should take to complete."},
            {"transition-property", "Properties to which a transition effect should be applied."},
            {"transition-timing-function", "Determines how intermediate values are calculated for properties modified by a transition effect."},
            {"translate", "A translate transformation."},
            {"-unity-background-image-tint-color", "Tinting color for the element's backgroundImage."},
            {"-unity-background-scale-mode", "Background image scaling in the element's box."},
            {"-unity-font", "Font to draw the element's text, defined as a Font object."},
            {"-unity-font-definition", "Font to draw the element's text, defined as a FontDefinition structure. It takes precedence over `-unity-font`."},
            {"-unity-font-style", "Font style and weight (normal, bold, italic) to draw the element's text."},
            {"-unity-overflow-clip-box", "Specifies which box the element content is clipped against."},
            {"-unity-paragraph-spacing", "Increases or decreases the space between paragraphs."},
            {"-unity-slice-bottom", "Size of the 9-slice's bottom edge when painting an element's background image."},
            {"-unity-slice-left", "Size of the 9-slice's left edge when painting an element's background image."},
            {"-unity-slice-right", "Size of the 9-slice's right edge when painting an element's background image."},
            {"-unity-slice-scale", "Scale applied to an element's slices."},
            {"-unity-slice-top", "Size of the 9-slice's top edge when painting an element's background image."},
            {"-unity-text-align", "Horizontal and vertical text alignment in the element's box."},
            {"-unity-text-outline", "Outline width and color of the text."},
            {"-unity-text-outline-color", "Outline color of the text."},
            {"-unity-text-outline-width", "Outline width of the text."},
            {"-unity-text-overflow-position", "The element's text overflow position."},
            {"visibility", "Specifies whether or not an element is visible."},
            {"white-space", "Word wrap over multiple lines if not enough space is available to draw the text of an element."},
            {"width", "Fixed width of an element for the layout."},
            {"word-spacing", "Increases or decreases the space between words."}
        };

        internal const string PixelPercentageInitialValue = "Enter a value as a pixel, percentage, or initial.";
        internal const string PixelOrInitialValue = "Enter a value as a pixel or initial.";

        public static readonly Dictionary<string, string> InspectorStylePropertiesValuesTooltipsDictionary =
            new Dictionary<string, string>
            {
                {$"display{FieldTooltipDictionarySeparator}flex", "Turns the element into a flexible container for aligning and distributing items."},
                {$"display{FieldTooltipDictionarySeparator}none", "Hides the element in the container. This might have an impact on the layout."},
                {$"visibility{FieldTooltipDictionarySeparator}visible", "Makes the UI element visible in its container. "},
                {$"visibility{FieldTooltipDictionarySeparator}hidden", "Makes the UI element hidden in its container. "},
                {$"overflow{FieldTooltipDictionarySeparator}visible", "Overflowing content is not clipped and may be visible outside the element's container."},
                {$"overflow{FieldTooltipDictionarySeparator}hidden", "Overflowing content is clipped and clipped content is hidden from view."},
                {$"position{FieldTooltipDictionarySeparator}absolute", "The item is removed from the normal document flow, and no space is created for it in the layout."},
                {$"position{FieldTooltipDictionarySeparator}relative", "The item is positioned according to the normal flow of the page/screen, and can be offset relative to itself."},
                {$"flex-direction{FieldTooltipDictionarySeparator}column", "Changes the main axis direction of a flex container, arranging its items from top to bottom."},
                {$"flex-direction{FieldTooltipDictionarySeparator}column-reverse", "Changes the main axis direction of a flex container, arranging its items from bottom to top."},
                {$"flex-direction{FieldTooltipDictionarySeparator}row", "Changes the main axis direction of a flex container, arranging its items from left to right."},
                {$"flex-direction{FieldTooltipDictionarySeparator}row-reverse", "Changes the main axis direction of a flex container, arranging its items from right to left."},
                {$"flex-wrap{FieldTooltipDictionarySeparator}nowrap", "Forces items to remain in a single line, which might cause overflow if available space is insufficient to fit all items."},
                {$"flex-wrap{FieldTooltipDictionarySeparator}wrap", "Items will wrap onto multiple lines if there is not enough space to fit them on a single line. "},
                {$"flex-wrap{FieldTooltipDictionarySeparator}wrap-reverse", "Items will wrap onto multiple lines, with the last item appearing first and subsequent items following in reverse order."},
                {$"align-items{FieldTooltipDictionarySeparator}auto", "Items within the container are aligned the same way as they would be with flex-end."},
                {$"align-items{FieldTooltipDictionarySeparator}flex-start", "Items within the container are packed flush to each other and will be aligned at the top of the container if the main axis is horizontal, or at the left side if the main axis is vertical."},
                {$"align-items{FieldTooltipDictionarySeparator}center", "Items within the container are centered vertically if the main axis is horizontal, or centered horizontally if the main axis is vertical."},
                {$"align-items{FieldTooltipDictionarySeparator}flex-end", "Items within the container are packed flush to each other and will be aligned at the bottom of the container if the main axis is horizontal, or at the right side if the main axis is vertical."},
                {$"align-items{FieldTooltipDictionarySeparator}stretch", "Items will fill the whole container vertically if the main axis is horizontal, or stretch horizontally if the main axis is vertical."},
                {$"justify-content{FieldTooltipDictionarySeparator}flex-start", "Items are packed flush to each other to the left edge of the container if the main axis is horizontal or to the top edge of the container if the main axis is vertical."},
                {$"justify-content{FieldTooltipDictionarySeparator}center", "Items are packed to the center of the container along the main axis."},
                {$"justify-content{FieldTooltipDictionarySeparator}flex-end", "Items are packed flush to each other to the right edge of the container if the main axis is horizontal or to the bottom edge of the container if the main axis is vertical."},
                {$"justify-content{FieldTooltipDictionarySeparator}space-between", "Items are spaced out evenly along the main axis with equal spacing between them. The first item is aligned to the start of the container, and the last item is aligned to the end."},
                {$"justify-content{FieldTooltipDictionarySeparator}space-around", "Items are spaced out evenly along the main axis with equal spacing between each item, and half the space before the first item and after the last item."},
                {$"justify-content{FieldTooltipDictionarySeparator}space-evenly", "Items are spaced out evenly along the main axis with equal spacing between each item, before the first item and after the last item."},
                {$"align-self{FieldTooltipDictionarySeparator}auto", "Items within the container are aligned according to the value of the align-items property."},
                {$"align-self{FieldTooltipDictionarySeparator}flex-start", "Items are aligned with the top of the cross-axis container."},
                {$"align-self{FieldTooltipDictionarySeparator}center", "Items are aligned with the center of the cross-axis container."},
                {$"align-self{FieldTooltipDictionarySeparator}flex-end", "Items are aligned with the bottom of the cross-axis container."},
                {$"align-self{FieldTooltipDictionarySeparator}stretch", "Items are stretched across the cross-axis container."},
                {$"-unity-font-style{FieldTooltipDictionarySeparator}bold", "Text is bolded."},
                {$"-unity-font-style{FieldTooltipDictionarySeparator}italic", "Text is italicized."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}left", "Aligns text to the left edge of the container."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}center", "Aligns text to the center of the container."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}right", "Aligns text to the right edge of the container."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}upper", "Aligns text to the upper edge of the container."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}middle", "Aligns text to the middle of the container."},
                {$"-unity-text-align{FieldTooltipDictionarySeparator}lower", "Aligns text to the lower edge of the container."},
                {$"white-space{FieldTooltipDictionarySeparator}normal", "Consecutive white spaces are collapsed into one and text wraps to fit the container."},
                {$"white-space{FieldTooltipDictionarySeparator}nowrap", "Consecutive white spaces are collapsed into one, and text doesn't wrap and continues on the same line in the container."},
                {$"text-overflow{FieldTooltipDictionarySeparator}clip", "Text that extends beyond the boundaries of its container will be cut off and will not be visible."},
                {$"text-overflow{FieldTooltipDictionarySeparator}ellipsis", "Text that extends beyond the boundaries of its container will be truncated with an ellipsis."},
                {$"-unity-background-scale-mode{FieldTooltipDictionarySeparator}stretch-to-fill", "Stretches an image to cover the entire container. "},
                {$"-unity-background-scale-mode{FieldTooltipDictionarySeparator}scale-and-crop", "Crops an image to fill the entire container while maintaining it's aspect ratio."},
                {$"-unity-background-scale-mode{FieldTooltipDictionarySeparator}scale-to-fit", "Scales an image to fit the container while preserving its aspect ratio, may result in empty spaces on either side."},
                {$"-unity-font-definition{FieldTooltipDictionarySeparator}", "Select a font or font asset."},
                {$"opacity{FieldTooltipDictionarySeparator}", "Drag the slider to specify the transparency of an element and of its children."},
                {$"background-image{FieldTooltipDictionarySeparator}", "Select a background image to paint in the element's box."},
                {$"rotate{FieldTooltipDictionarySeparator}", "Enter a rotation transformation value to apply to the the element."},
                {$"scale{FieldTooltipDictionarySeparator}", "Enter a scale transformation value to apply to the the element."},
                {$"cursor{FieldTooltipDictionarySeparator}", "Specify a mouse cursor to display when the mouse pointer is over an element. This will override the default cursor."},
                {$"transition-property{FieldTooltipDictionarySeparator}", "Select property to which transition effect will be applied."},
                {$"transition-duration{FieldTooltipDictionarySeparator}", "Time a transition animation should take to complete."},
                {$"transition-easing{FieldTooltipDictionarySeparator}", "Determines the smoothness of an animation. It controls how quickly the animation starts and stops."},
                {$"transition-delay{FieldTooltipDictionarySeparator}", "Duration to wait before starting a property’s transition effect when its value changes."},
                {$"text-shadow{FieldTooltipDictionarySeparator}offset-x", "Move text shadow horizontally (left and right)"},
                {$"text-shadow{FieldTooltipDictionarySeparator}offset-y", "Move text shadow vertically (up and down)"},
                {$"text-shadow{FieldTooltipDictionarySeparator}color", "Select color to use for text shadow. X or Y offset must be set to be visible."},
                {$"text-shadow{FieldTooltipDictionarySeparator}blur-radius", "Strength of blur effect to be applied to text shadow."},
                {$"ColorField{FieldTooltipDictionarySeparator}", "Choose from a color swatch, change the alpha from zero to see the result."},
                {$"IntegerField{FieldTooltipDictionarySeparator}", PixelPercentageInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}", PixelPercentageInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}border-top-width", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}border-right-width", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}border-bottom-width", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}border-left-width", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}-unity-text-outline-width", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}letter-spacing", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}word-spacing", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}-unity-paragraph-spacing", PixelOrInitialValue},
                {$"DimensionStyleField{FieldTooltipDictionarySeparator}-unity-slice-scale", PixelOrInitialValue},
                {$"PercentSlider{FieldTooltipDictionarySeparator}", "Drag the slider to select a percentage between 0% and 100%."},
                {$"IntegerStyleField{FieldTooltipDictionarySeparator}", PixelPercentageInitialValue},
                {$"IntegerStyleField{FieldTooltipDictionarySeparator}-unity-slice-top", PixelOrInitialValue},
                {$"IntegerStyleField{FieldTooltipDictionarySeparator}-unity-slice-left", PixelOrInitialValue},
                {$"IntegerStyleField{FieldTooltipDictionarySeparator}-unity-slice-bottom", PixelOrInitialValue},
                {$"IntegerStyleField{FieldTooltipDictionarySeparator}-unity-slice-right", PixelOrInitialValue},
                {$"ObjectField{FieldTooltipDictionarySeparator}", "Assign an object by either dragging and dropping it, or by selecting it with the Object Picker."},
            };

        public static readonly string FoldoutContainsBindingsString = "One or more properties contain a binding (resolved or unresolved).";
        public static readonly string BindingNotDefinedAttributeString = "Not Defined";
        public static readonly string EmptyConvertersString = "None";

        // Selector Preview
        public const string PreviewConvertToFloatingWindow = "Convert to Floating Window";
        public const string PreviewDockToInspector = "Dock Preview to Inspector";
        public const string PreviewWindowTitle = "UI Builder Text Property Preview";
        public const string PreviewMinimizeInInspector = "Minimize in Inspector";
        public const string PreviewRestoreInInspector = "Restore in Inspector";
        public const string PreviewTransparencyToggleTooltip = "Show or hide transparency view.";

        // Attribute fields
        public static readonly string AttributeFieldFactoryVEPropertyName = "__unity-ui-builder-attribute-field-factory";

        // Completion
        public static readonly string CompleterAnchoredControlScreenRectVEPropertyName = "__unity-ui-builder-completer-popup-window-screen-rect";
        public static readonly string CompleterCurrentEntryItemId = "__unity-ui-builder-completer-current-entry-item";

        // Dimension Style Field specific
        public const float DimensionStyleFieldReducedDragStep = 0.1f;

        //
        // Explorer
        //

        // Explorer Links VE Property Names
        public static readonly string ExplorerItemElementLinkVEPropertyName = "__unity-ui-builder-explorer-item-link";
        public static readonly string ExplorerItemFillItemCallbackVEPropertyName = "__unity-ui-builder-explorer-item-override-template";
        public static readonly string ExplorerStyleClassPillClassNameVEPropertyName = "__unity-ui-builder-explorer-style-class-pill-name";
        public static readonly string ExplorerItemLinkedUXMLFileName = "__unity-ui-builder-linked-uxml-file-name";

        // Explorer Names
        public static readonly string ExplorerItemRenameTextfieldName = "unity-builder-explorer__rename-textfield";

        // Explorer Style Class Names
        public static readonly string ExplorerHeaderRowClassName = "unity-builder-explorer__header";
        public static readonly string ExplorerItemUnselectableClassName = "unity-builder-explorer--unselectable";
        public static readonly string ExplorerItemHiddenClassName = "unity-builder-explorer--hidden";
        public static readonly string ExplorerItemHoverClassName = "unity-builder-explorer__item--hover";
        public static readonly string ExplorerItemReorderZoneClassName = "unity-builder-explorer__reorder-zone";
        public static readonly string ExplorerItemReorderZoneAboveClassName = "unity-builder-explorer__reorder-zone-above";
        public static readonly string ExplorerItemReorderZoneBelowClassName = "unity-builder-explorer__reorder-zone-below";
        public static readonly string ExplorerItemRenameTextfieldClassName = "unity-builder-explorer__rename-textfield";
        public static readonly string ExplorerItemNameLabelClassName = "unity-builder-explorer__name-label";
        public static readonly string ExplorerItemTypeLabelClassName = "unity-builder-explorer__type-label";
        public static readonly string ExplorerItemLabelContClassName = "unity-builder-explorer-tree-item-label-cont";
        public static readonly string ExplorerItemSelectorLabelContClassName = "unity-builder-explorer-tree-item-selector-label-cont";
        public static readonly string ExplorerItemLabelClassName = "unity-builder-explorer-tree-item-label";
        public static readonly string ExplorerItemIconClassName = "unity-builder-explorer-tree-item-icon";
        public static readonly string ExplorerStyleSheetsPaneClassName = "unity-builder-stylesheets-pane";
        public static readonly string ExplorerActiveStyleSheetClassName = "unity-builder-stylesheets-pane--active-stylesheet";
        public static readonly string ExplorerItemBelongsToOpenDocument = "unity-builder-explorer-excluded";
        public static readonly string ExplorerDayZeroStateLabelClassName = "unity-builder-explorer__day-zero-label";

        // Selector labels
        public static readonly string SelectorLabelClassName = "unity-builder-selector-label";
        public static readonly string SelectorLabelMultiplePartsClassName = "unity-builder-selector-label-multiple-parts";

        // StyleSheets Pane Menu
        public static readonly string ExplorerStyleSheetsPanePlusMenuNoElementsMessage = "Need at least one element in UXML to add StyleSheets.";
        public static readonly string ExplorerStyleSheetsPaneSetActiveUSS = "Set as Active USS";
        public static readonly string ExplorerStyleSheetsPaneCreateNewUSSMenu = "Create New USS";
        public static readonly string ExplorerStyleSheetsPaneAddExistingUSSMenu = "Add Existing USS";
        public static readonly string ExplorerStyleSheetsPaneRemoveUSSMenu = "Remove USS";
        public static readonly string ExplorerStyleSheetsPaneAddToNewUSSMenu = "Add to New USS";
        public static readonly string ExplorerStyleSheetsPaneAddToExistingUSSMenu = "Add to Existing USS";

        // Hierarchy Pane Menu
        public static readonly string ExplorerHierarchyPaneOpenSubDocument = "Open Instance in Isolation";
        public static readonly string ExplorerHierarchyPaneOpenSubDocumentInPlace = "Open Instance in Context";
        public static readonly string ExplorerHierarchyReturnToParentDocument = "Return to Parent Document";
        public static readonly string ExplorerHierarchyOpenInBuilder = "Open in UI Builder";
        public static readonly string ExplorerHierarchySelectTemplate = "Show in Project";
        public static readonly string ExplorerHierarchyUnpackTemplate = "Unpack Instance";
        public static readonly string ExplorerHierarchyUnpackCompletely = "Unpack Instance Completely";
        public static readonly string ExplorerHierarchyCreateTemplate = "Create Template";

        // Explorer Messages
        public static readonly string ExplorerInExplorerNewClassSelectorInfoMessage = "Add new selector...";

        // Code Preview Messages
        public static readonly string CodePreviewTruncatedTextMessage = "The content is truncated because it is too long";

        //
        // Library
        //

        // Library Item Links VE Property Names
        public static readonly string LibraryItemLinkedManipulatorVEPropertyName = "__unity-ui-builder-dragger";
        public static readonly string LibraryItemLinkedTemplateContainerPathVEPropertyName = "__unity-ui-builder-template-container-path";

        // Library Style Class Names
        public static readonly string LibraryCurrentlyOpenFileItemClassName = "unity-builder-library__currently-open-file";

        // Library Menu
        public const string LibraryShowPackageFiles = "Show Package Files";
        public const string LibraryViewModeToggle = "Tree View";
        public const string LibraryEditorExtensionsAuthoring = "Editor Extension Authoring";
        public const string LibraryDefaultVisualElementType = "Default Visual Element Type";
        public const string LibraryDefaultVisualElementStyledName = "Styled";
        public const string LibraryDefaultVisualElementNoStylesName = "No styles";
        public const string LibraryProjectTabName = "Project";
        public const string LibraryStandardControlsTabName = "Standard";

        // Library Content
        public const string LibraryContainersSectionHeaderName = "Containers";
        public const string LibraryEditorContainersSectionHeaderName = "Containers";
        public const string LibraryControlsSectionHeaderName = "Controls";
        public const string LibraryAssetsSectionHeaderName = "UI Documents (UXML)";
        public const string LibraryCustomControlsSectionHeaderName = "Custom Controls (C#)";
        public const string LibraryCustomControlsSectionUxmlSerializedData = "UxmlSerializedData";
        public const string LibraryCustomControlsSectionUxmTraits = "UxmlTraits";
        public const string EditorOnlyTag = "Editor Only";

        //
        // Selection
        //

        // Special Selection Asset Marker Names
        public static readonly string SelectedVisualElementAssetAttributeName = "__unity-builder-selected-element";
        public static readonly string SelectedVisualElementAssetAttributeValue = "selected";
        public static readonly string SelectedStyleRulePropertyName = "--ui-builder-selected-style-property";
        public static readonly string SelectedStyleSheetSelectorName = "__unity_ui_builder_selected_stylesheet";
        public static readonly string SelectedVisualTreeAssetSpecialElementTypeName = typeof(UnityUIBuilderSelectionMarker).FullName;

        //
        // Canvas
        //

        // Canvas Container Style Class Names
        public static readonly string CanvasContainerDarkStyleClassName = "unity-builder-canvas__container--dark";
        public static readonly string CanvasContainerLightStyleClassName = "unity-builder-canvas__container--light";
        public static readonly string CanvasContainerRuntimeStyleClassName = "unity-builder-canvas__container--runtime";

        // Canvas Messages
        public static readonly string CannotManipulateResizedOrScaledItemMessage = "The selected item is rotated or scaled.\nTransforming it from the viewport is not properly supported yet.";
        public static readonly string CannotResizeBecauseOfBoundPropertiesMessage = "Cannot resize because of bound property(ies): {0}";
        public static readonly string CannotEditBoundPropertyMessage = "Cannot edit bound property '{0}'";

        //
        // Toolbar
        //

        // Toolbar Messages
        public static readonly string ToolbarLoadUxmlDialogTitle = "Load UXML File";
        public static readonly string ToolbarCannotLoadUxmlOutsideProjectMessage = "UI Builder: Cannot load .uxml files outside the Project.";
        public static readonly string ToolbarSelectedAssetIsInvalidMessage = "UI Builder: The asset selected was not a valid UXML asset.";
        public static readonly string ToolbarUnsavedFileSuffix = "*";
        public static readonly string ToolbarUnsavedFileDisplayText = "<unsaved file>";
        public static readonly string ToolbarUnsavedFileDisplayMessage = ToolbarUnsavedFileDisplayText + ToolbarUnsavedFileSuffix;

        // Toolbar tooltips
        public static readonly string ToolbarCanvasThemeMenuEmptyTooltip = "Preview theme list is empty.\n\nYou can create new themes or a default Runtime theme from the \"Assets/Create/UI Toolkit\" menu.";
        public static readonly string ToolbarCanvasThemeMenuEditorTooltip = "Select a preview theme for the viewport.\n\nNote: List contains some themes available only with the Editor Extensions Authoring mode.";
        public static readonly string ToolbarCanvasThemeMenuTooltip = "Select a preview theme for the viewport.";

        //
        // Undo/Redo
        //

        // User Undo/Redo Messages
        public static readonly string ChangeAttributeValueUndoMessage = "Change UI Attribute Value";
        public static readonly string ChangeUIStyleValueUndoMessage = "Change UI Style Value";
        public static readonly string ChangeSelectionUndoMessage = "Change UI Builder Selection";
        public static readonly string CreateUIElementUndoMessage = "Create UI Element";
        public static readonly string DeleteUIElementUndoMessage = "Delete UI Element";
        public static readonly string ReparentUIElementUndoMessage = "Reparent UI Element";
        public static readonly string AddStyleClassUndoMessage = "Add UI Style Class";
        public static readonly string CreateStyleClassUndoMessage = "Extract Local Style to New Class";
        public static readonly string RemoveStyleClassUndoMessage = "Remove UI Style Class";
        public static readonly string AddNewSelectorUndoMessage = "Create USS Selector";
        public static readonly string RenameSelectorUndoMessage = "Rename USS Selector";
        public static readonly string DeleteSelectorUndoMessage = "Delete USS Selector";
        public static readonly string AddUxmlObject = "Add UXML Object";
        public static readonly string ModifyUxmlObject = "Modify UXML Object";
        public static readonly string RemoveUxmlObject = "Remove UXML Object";
        public static readonly string MoveUSSSelectorUndoMessage = "Move USS Selector";
        public static readonly string AddBindingUndoMessage = "Add Binding";
        public static readonly string DeleteBindingUndoMessage = "Delete Binding";
        public static readonly string ChangeCanvasDimensionsOrMatchViewUndoMessage = "Change Canvas Dimensions or Match View";
        public static readonly string SaveAsNewDocumentsDialogMessage = "Save As New UI Documents";
        public static readonly string NewDocumentsDialogMessage = "New UI Documents";
        public static readonly string UnsetStyleUndoMessage = "Unset Style Value";

        //
        // Binding Window
        //
        public static readonly string AddBindingTitle = "Add Binding";
        public static readonly string EditBindingTitle = "Edit Binding";
        public static readonly string BindingWindowMissingDataSourceErrorMessage = "Data source is not required but only if it is defined elsewhere in code";
        public static readonly string BindingWindowNotResolvedPathErrorMessage = "Path cannot be resolved";
        public static readonly string BindingWindowMissingPathErrorMessage = "Path is missing";
        public static readonly string BindingWindowCannotCreateBindingErrorMessage = "Cannot create binding on {0}";
        public static readonly string BindingWindowCannotEditBindingErrorMessage = "Cannot edit binding on {0}";
        public static readonly string BindingWindowLocalConverterPlaceholderText = "Enter a converter group";
        public static readonly string BindingWindowLocalConverterNotApplicableMessage = "It is not applicable for the specified binding mode";
        public static readonly string BindingWindowConverterCompleter_IncompatibleMessage = "Not currently compatible with type";
        public static readonly string BindingWindowConverterCompleter_CompatibleMessage = "Compatible with type";
        public static readonly string BindingWindowConverterCompleter_UnknownCompatibilityMessage = "Unknown";
        public static readonly string BindingWindowConverterCompleter_SelectEditedText = "Select to use a custom group ID";
        public static readonly string BindingWindowConverterCompleter_UseCurrentEntryMessage = "Use \"{0}\" as group ID";
        public static readonly string BindingWindowShowOnlyCompatibleMessage = "Show only compatible";

        //
        // Dialogs
        //

        // Generic Dialog Messages
        public static readonly string DialogOkOption = "Ok";
        public static readonly string DialogCancelOption = "Cancel";
        public static readonly string DialogDiscardOption = "Discard changes and {0}";
        public static readonly string DialogAbortActionOption = "Do not {0}";
        public static readonly string DialogSaveActionOption = "Save";
        public static readonly string DialogDontSaveActionOption = "Discard";

        // Save Dialog Messages
        public static readonly string SaveDialogChooseUxmlPathDialogTitle = "Choose UXML File Location";
        public static readonly string SaveDialogChooseUssPathDialogTitle = "Choose USS File Location";
        public static readonly string SaveDialogSaveChangesPromptTitle = "UI Builder - Unsaved Changes Detected";
        public static readonly string SaveDialogSaveChangesPromptMessage = "Do you want to save changes you made?";
        public static readonly string SaveDialogExternalChangesPromptTitle = "The document, {0}, has been changed outside of the UI Builder";
        public static readonly string SaveDialogExternalChangesPromptMessage =
            "The UI Builder will now apply the changes made outside of the UI Builder. This overwrites any unsaved changes you made in the UI Builder.\n\n" +
            "Note: To avoid conflicting changes, make sure to save any changes you make in the UI Builder before editing a file in an external editor or " +
            "in a dedicated editor inside Unity (this includes resource files like Font and Sprite assets which have their own editor).";
        public static readonly string SaveDialogInvalidPathMessage = "Can only save in the 'Assets/' or 'Packages/' folders.";
        public static readonly string SaveDialogReplaceWithNewTemplateMessage = "Changes to your template affect this file. In order to replace this template, we need to save your current file.";

        // Error Dialog Messages
        public static readonly string ErrorDialogNotice = "UI Builder: Notice";

        public static readonly string ErrorIncompatibleFileActionMessage =
            "You are about to {0}:\n\n{1}\n\n" +
            "This file is currently open in the UI Builder. " +
            "If you {0} the file, the UI Builder document will close the current document, and discard any unsaved changes.";
        public static readonly string InvalidUXMLOrUSSAssetNameSuffix = "[UNSUPPORTED_IN_UI_BUILDER]";
        public static readonly string InvalidUSSDialogTitle = "UI Builder - Unable to parse USS file.";
        public static readonly string InvalidUSSDialogMessage = "UI Builder Failed to open {0} asset. This may be due to invalid USS syntax or USS syntax the UI Builder does not yet support (ie. Variables). Check console for details.";
        public static readonly string InvalidUXMLDialogTitle = "UI Builder - Unable to parse UXML file.";
        public static readonly string InvalidUXMLDialogMessage = "UI Builder Failed to open {0} asset. This may be due to invalid UXML syntax or UXML syntax the UI Builder does not yet support. Check console for details.";
        public static readonly string InvalidCreateTemplatePathTitle = "UI Builder - Invalid Path";
        public static readonly string InvalidCreateTemplatePathMessage = "The currently open document cannot be replaced.";

        public static readonly string CouldNotOpenSelectorMessage = "Could not open the stylesheet containing the selector";

        // StyleSheets Dialogs
        public static readonly string ExtractInlineStylesNoUSSDialogTitle = "UI Builder - No USS in current document";
        public static readonly string ExtractInlineStylesNoUSSDialogMessage = "There is no StyleSheet (USS) added to this UXML document. Where would you like to add this new USS rule?";
        public static readonly string ExtractInlineStylesNoUSSDialogNewUSSOption = "Add to New USS";
        public static readonly string ExtractInlineStylesNoUSSDialogExistingUSSOption = "Add to Existing USS";
        public static readonly string DeleteLastElementDialogTitle = "UI Builder - Deleting last element";
        public static readonly string DeleteLastElementDialogMessage = "You are about to delete the last element. Since USS files are attached to root elements, with no elements in the document, no USS files can be attached. Any existing USS files attached will be removed. You can always undo this operation and get everything back. Continue?";
        public static readonly string InvalidWouldCauseCircularDependencyMessage = "Invalid operation.";
        public static readonly string InvalidWouldCauseCircularDependencyMessageDescription = "Can not add as TemplateContainer, as would create a circular dependency.";

        //
        // Messages
        //

        // Warnings
        public static readonly string ClassNameValidationSpacialCharacters = "Class name can only contain letters, numbers, underscores, and dashes.";
        public static readonly string AttributeValidationSpacialCharacters = "{0} attribute can only contain letters, numbers, underscores, and dashes.";
        public static readonly string BindingPathAttributeValidationSpacialCharacters = "{0} attribute can only contain letters, numbers, underscores, dots and dashes.";
        public static readonly string StyleSelectorValidationSpacialCharacters = "Style Selector can only contain *_-.#>, letters, and numbers.";
        public static readonly string TypeAttributeInvalidTypeMessage = "{0} attribute is an invalid type. Make sure to include assembly name.";
        public static readonly string TypeAttributeMustDeriveFromMessage = "{0} attribute type must derive from {1}";
        public static readonly string BuiltInAssetPathsNotSupportedMessage = "Built-in resource paths are not supported in USS.";
        public static readonly string BuiltInAssetPathsNotSupportedMessageUxml = "Built-in resource paths are not supported in UXML.";
        public static readonly string DocumentMatchGameViewModeDisabled = "Match Game View mode disabled.";

        // Settings
        public const string BuilderEditorExtensionModeToggleLabel = "Enable Editor Extension by default";

        // Notifications
        public const string NoUIToolkitPackageInstalledNotification = "Your Project is not configured to support UI Toolkit runtime UI. To enable runtime support, install the UI Toolkit package.";
        public const string InlineValuesOnCanvasNotification = "Changes made to inline values on properties with resolved bindings do not appear on the canvas.";
        public const string BindingsOnPreviewModeNotification = "Modifying attribute values in Preview could change a resolved binding's value depending on binding mode.";
        public const string DoNotShowAgainNotificationButtonText = "Do not show again";

        //
        // UXML/USS
        //

        // UXML/USS Trivials
        public static readonly string Uxml = "uxml";
        public static readonly string Uss = "uss";
        public static readonly string UxmlExtension = ".uxml";
        public static readonly string UssExtension = ".uss";
        public static readonly string TssExtension = ".tss";

        // UXML
        public static readonly string UxmlOpenTagSymbol = "<";
        public static readonly string UxmlCloseTagSymbol = ">";
        public static readonly string UxmlEndTagSymbol = " /" + UxmlCloseTagSymbol;
        public static readonly string UxmlTemplateClassTag = "Template";
        public static readonly string UxmlNameAttr = "name";
        public static readonly string UxmlEngineNamespace = "UnityEngine.UIElements";
        public static readonly string UxmlDefaultEngineNamespacePrefix = "ui";
        public static readonly string UxmlEditorNamespace = "UnityEditor.UIElements";
        public static readonly string UxmlDefaultEditorNamespacePrefix = "uie";
        public static readonly string UxmlTagTypeName = "UnityEngine.UIElements.UXML";
        public static readonly string UxmlInstanceTypeName = "UnityEngine.UIElements.Instance";


        // USS
        public static readonly string UssSelectorNameSymbol = "#";
        public static readonly string UssSelectorClassNameSymbol = ".";
        public static readonly string UssSelectorPseudoStateSymbol = ":";
        public static readonly string UssVariablePrefix = "--";
        public static readonly string USSVariablePattern = @"[^a-z0-9A-Z_-]";
        public static readonly string USSVariableInvalidCharFiller = "-";
        public static readonly string USSVariableUIBuilderPrefix = "--unity_builder";

        // Styles
        public static readonly string StylePropertyPathPrefix = "style.";

        public static readonly List<string> SpecialSnowflakeLengthStyles = new List<string>()
        {
            "border-left-width",
            "border-right-width",
            "border-top-width",
            "border-bottom-width"
        };

        internal static readonly List<string> ViewportOverlayEnablingStyleProperties = new List<string>()
        {
            "width",
            "height",
            "margin-left",
            "margin-right",
            "margin-top",
            "margin-bottom",
            "padding-left",
            "padding-right",
            "padding-top",
            "padding-bottom",
            "border-left-width",
            "border-right-width",
            "border-top-width",
            "border-bottom-width"
        };

        public static readonly Dictionary<string, string> SpecialEnumNamesCases = new Dictionary<string, string>
        {
            {"nowrap", "no-wrap"},
            {"tabindex", "tab-index"}
        };

        //
        // Complex Getters
        //

        public static string newlineCharFromEditorSettings
        {
            get
            {
                string preferredLineEndings;
                switch (EditorSettings.lineEndingsForNewScripts)
                {
                    case LineEndingsMode.OSNative:
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                            preferredLineEndings = WindowsNewlineChar;
                        else
                            preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Unix:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Windows:
                        preferredLineEndings = WindowsNewlineChar;
                        break;
                    default:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                }
                return preferredLineEndings;
            }
        }

        public static string builderDocumentDiskJsonFolderAbsolutePath
        {
            get
            {
                var path = BuilderAssetUtilities.projectPath + "/" + BuilderDocumentDiskJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }

        public static string builderDocumentDiskJsonFileAbsolutePath
        {
            get
            {
                var path = builderDocumentDiskJsonFolderAbsolutePath + "/" + BuilderDocumentDiskJsonFileName;
                return path;
            }
        }

        public static string builderDocumentDiskSettingsJsonFolderAbsolutePath
        {
            get
            {
                var path = BuilderAssetUtilities.projectPath + "/" + BuilderDocumentDiskSettingsJsonFolderPath;
                path = Path.GetFullPath(path);
                return path;
            }
        }
    }
}
