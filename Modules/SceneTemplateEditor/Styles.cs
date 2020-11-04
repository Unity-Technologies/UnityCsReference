// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.SceneTemplate
{
    internal static class Styles
    {
        public const string classOffsetContainer = "scene-template-dialog-offset-container";
        public const string classMainContainer = "scene-template-dialog-main-container";
        public const string classTemplatesContainer = "scene-template-dialog-templates-container";
        public const string classDescriptionContainer = "scene-template-dialog-description-container";
        public const string classToggleLabel = "scene-template-dialog-toggle-label";
        public const string classButtons = "scene-template-dialog-buttons";
        public const string classButton = "scene-template-dialog-button";
        public const string classHeaderLabel = "scene-template-header-label";
        public const string classListView = "scene-template-dialog-list-view";
        public const string classTemplateListView = "scene-template-dialog-template-list-view";
        public const string classBorder = "scene-template-dialog-border";
        public const string classWrappingText = "scene-template-dialog-wrapping-text";
        public const string classPreviewArea = "scene-template-preview-area";
        public const string classUnityBaseField = "unity-base-field";
        public const string classUnityLabel = "unity-label";
        public const string classUnityBaseFieldLabel = "unity-base-field__label";
        public const string classUnityBaseFieldInput = "unity-base-field__input";
        public const string classUnityPropertyFieldLabel = "unity-property-field__label";
        public const string classTextLink = "scene-template-text-link";
        public const string classElementSelected = "scene-template-element-selected";
        public const string classInspectorFoldoutHeader = "Inspector-Title";
        public const string classInspectorFoldoutHeaderText = "Inspector-TitleText";
        public const string classFoldoutHelpButton = "scene-template-asset-inspector-foldout-help-button";
        public const string unityThemeVariables = "unity-theme-env-variables";
        public const string sceneTemplateThemeVariables = "scene-template-variables";
        public const string sceneTemplateNoTemplateHelpBox = "scene-template-no-template-help-box";
        public const string sceneTemplateDialogFooter = "scene-template-dialog-footer";
        public const string sceneTemplateDialogBorder = "scene-template-dialog-border";

        public const string selected = "selected";
        public const string pinned = "pinned";
        public const string gridView = "grid-view";
        public const string gridViewHeader = "grid-view-header";
        public const string gridViewItemIcon = "grid-view-item-icon";
        public const string gridViewItemPin = "grid-view-item-pin";
        public const string gridViewItemLabel = "grid-view-item-label";
        public const string gridViewHeaderSearchField = "grid-view-header-search-field";
        public const string gridViewItemsScrollView = "grid-view-items-scrollview";

        public const string gridViewItemElement = "grid-view-item-element";

        public const string gridViewItems = "grid-view-items";
        public const string gridViewFooter = "grid-view-footer";
        public const string gridViewFooterTileSize = "grid-view-footer-tile-size";
        public const string gridViewHeaderLabel = "grid-view-header-label";
        public const string gridViewItemsContainerGrid = "grid-view-items-container-grid";
        public const string gridViewItemsContainerList = "grid-view-items-container-list";

        public static readonly string k_IconsFolderFolder = $"Icons/SceneTemplate/";
        public static readonly string k_StyleSheetsFolder = $"StyleSheets/SceneTemplate/";
        public static readonly string k_CommonStyleSheetPath = $"{k_StyleSheetsFolder}Common.uss";
        public static readonly string k_DarkStyleSheetPath = $"{k_StyleSheetsFolder}Dark.uss";
        public static readonly string k_LightStyleSheetPath = $"{k_StyleSheetsFolder}Light.uss";
        public static string variableStyleSheet => EditorGUIUtility.isProSkin ? k_DarkStyleSheetPath : k_LightStyleSheetPath;
    }
}
