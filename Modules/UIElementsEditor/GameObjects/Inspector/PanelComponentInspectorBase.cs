// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements.GameObjects;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Inspector
{
    internal class PanelComponentInspectorBase : Editor
    {
        // Limit the number of editor elements in the inspector element message.
        const int k_MaxElementsInEditorElementsErrorMessage = 5;
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/Inspector/PanelRendererInspector.uss";
        const string k_InspectorVisualTreeAssetPath = "UIPackageResources/UXML/Inspector/PanelRendererInspector.uxml";
        private const string k_StyleClassWithParentHidden = "unity-ui-document-inspector--with-parent--hidden";
        private const string k_StyleClassPanelMissingHidden = "unity-ui-document-inspector--panel-missing--hidden";

        internal const string k_EditorElementsError = "The VisualTreeAsset contains editor-only elements that cannot be used at runtime.\nPlease remove the following elements:";
        const string k_InspectorEditSourceAssetButtonTooltip = "Edit the Visual Tree Asset (UXML) in the UI Builder.";
        const string k_InspectorNewSourceAssetButtonTooltip = "Create a new Visual Tree Asset (UXML).";

        private static StyleSheet s_DefaultStyleSheet;
        private static VisualTreeAsset s_InspectorUxml;

        private VisualElement m_RootVisualElement;

        private ObjectField m_PanelSettingsField;
        private ObjectField m_ParentField;
        private ObjectField m_SourceAssetField;
        private Button m_SourceAssetButton;

        private PropertyField m_SortingOrderField;

        private EnumField m_PositionEnumField;

        private Foldout m_WorldSpaceDimensionsFoldout;
        private EnumField m_WorldSpaceSizeField;
        private FloatField m_WorldSpaceWidthField;
        private FloatField m_WorldSpaceHeightField;

        private EnumField m_PivotReferenceSizeField;
        private EnumField m_PivotField;

        private HelpBox m_DrivenByParentWarning;
        private HelpBox m_MissingPanelSettings;
        private HelpBox m_EditorElementsWarning;
        private VisualElement m_InputConfiguration;

        private PanelSettings m_CurrentPanelSettings;

        protected virtual Type parentObjectType => typeof(IPanelComponent);

        private void ConfigureFields()
        {
            // Using MandatoryQ instead of just Q to make sure modifications of the UXML file don't make the
            // necessary elements disappear unintentionally.

            // Show the PanelRenderer migration warning when inspecting a UIDocument,
            // but only if there isn't a PanelRenderer component on the same GameObject.
            bool shouldShowMigrationWarning = (parentObjectType == typeof(UIDocument));
            bool shouldEnableConversionButton = (target as Component).GetComponent<PanelRenderer>() == null;

            var migrationContainer = m_RootVisualElement.MandatoryQ<VisualElement>("migration-warning-container");
            if (!shouldShowMigrationWarning)
                migrationContainer.style.display = DisplayStyle.None;

            var addPanelRendererButton = migrationContainer.MandatoryQ<Button>("add-panelrenderer-button");
            addPanelRendererButton.SetEnabled(shouldEnableConversionButton);
            addPanelRendererButton.clicked += AddPanelRendererComponent;

            m_DrivenByParentWarning = m_RootVisualElement.MandatoryQ<HelpBox>("driven-by-parent-warning");
            m_MissingPanelSettings = m_RootVisualElement.MandatoryQ<HelpBox>("missing-panel-warning");
            m_EditorElementsWarning = m_RootVisualElement.MandatoryQ<HelpBox>("editor-elements-warning");

            m_PanelSettingsField = m_RootVisualElement.MandatoryQ<ObjectField>("panel-settings-field");
            m_PanelSettingsField.objectType = typeof(PanelSettings);

            m_ParentField = m_RootVisualElement.MandatoryQ<ObjectField>("parent-field");
            m_ParentField.objectType = parentObjectType;
            m_ParentField.SetEnabled(false);

            m_SourceAssetField = m_RootVisualElement.MandatoryQ<ObjectField>("source-asset-field");
            m_SourceAssetField.objectType = typeof(VisualTreeAsset);

            m_SourceAssetButton = m_RootVisualElement.MandatoryQ<Button>("source-asset-button");
            m_SourceAssetButton.clicked += OnSourceAssetButtonClicked;

            m_SortingOrderField = m_RootVisualElement.MandatoryQ<PropertyField>("sort-order-field");

            m_WorldSpaceDimensionsFoldout = m_RootVisualElement.MandatoryQ<Foldout>("world-space-dimensions");

            // Natively serialized enums don't play nicely with the bindings system since they are stored
            // as plain ints. So we manually handle enum value changes.
            var pc = (IPanelComponent)target;

            m_PositionEnumField = m_RootVisualElement.MandatoryQ<EnumField>("position-field");
            m_PositionEnumField.Init(pc.position);
            m_PositionEnumField.RegisterValueChangedCallback(evt =>
            {
                pc.position = (Position)evt.newValue;
            });

            m_WorldSpaceSizeField = m_RootVisualElement.MandatoryQ<EnumField>("size-mode");
            m_WorldSpaceSizeField.Init(pc.worldSpaceSizeMode);
            m_WorldSpaceSizeField.RegisterValueChangedCallback(evt =>
            {
                pc.worldSpaceSizeMode = (WorldSpaceSizeMode)evt.newValue;
            });

            // Since PanelRenderer doesn't have OnValidate() calls, we need to update
            // the size fields when they change.
            m_WorldSpaceWidthField = m_RootVisualElement.MandatoryQ<FloatField>("width-field");
            m_WorldSpaceWidthField.SetValueWithoutNotify(pc.worldSpaceSize.x);
            m_WorldSpaceWidthField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(target, "Change World Space Width");
                var size = pc.worldSpaceSize;
                size.x = evt.newValue;
                pc.worldSpaceSize = size;
            });

            m_WorldSpaceHeightField = m_RootVisualElement.MandatoryQ<FloatField>("height-field");
            m_WorldSpaceHeightField.SetValueWithoutNotify(pc.worldSpaceSize.y);
            m_WorldSpaceHeightField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(target, "Change World Space Height");
                var size = pc.worldSpaceSize;
                size.y = evt.newValue;
                pc.worldSpaceSize = size;
            });

            m_PivotReferenceSizeField = m_RootVisualElement.MandatoryQ<EnumField>("pivot-reference-size-field");
            m_PivotReferenceSizeField.Init(pc.pivotReferenceSize);
            m_PivotReferenceSizeField.RegisterValueChangedCallback(evt =>
            {
                pc.pivotReferenceSize = (PivotReferenceSize)evt.newValue;
            });

            m_PivotField = m_RootVisualElement.MandatoryQ<EnumField>("pivot-field");
            m_PivotField.Init(pc.pivot);
            m_PivotField.RegisterValueChangedCallback(evt =>
            {
                pc.pivot = (Pivot)evt.newValue;
            });

            m_WorldSpaceDimensionsFoldout.style.display = DisplayStyle.None;

            m_InputConfiguration = m_RootVisualElement.MandatoryQ<VisualElement>("input-configuration");
            m_InputConfiguration.style.display = DisplayStyle.None;
            m_InputConfiguration.MandatoryQ<Button>("input-configuration-create").clicked += () =>
            {
                if (PanelInputConfiguration.current == null)
                    PlayModeMenuItems.AddPanelInputConfiguration();
            };
        }

        private void BindFields()
        {
            m_ParentField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_PanelSettingsField.RegisterCallback<ChangeEvent<Object>>(evt =>
            {
                if (target == null)
                    return;

                if (evt.newValue != m_CurrentPanelSettings)
                {
                    m_CurrentPanelSettings = evt.newValue as PanelSettings;
                    ((IPanelComponent)target).PerformValidation(true);
                }
                UpdateValues();
            });
            m_SourceAssetField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_PositionEnumField.RegisterCallback<ChangeEvent<Enum>>(evt => UpdateValues());
            m_WorldSpaceSizeField.RegisterCallback<ChangeEvent<Enum>>(evt => UpdateValues());
        }

        private void UpdateValues()
        {
            var panelComponent = target as IPanelComponent;
            if (panelComponent == null)
                return;

            bool hideDrivenByParentWarning = panelComponent.parentUI == null;
            m_DrivenByParentWarning.EnableInClassList(k_StyleClassWithParentHidden, hideDrivenByParentWarning);
            m_ParentField.EnableInClassList(k_StyleClassWithParentHidden, hideDrivenByParentWarning);

            bool hidePanelMissingWarning = panelComponent.panelSettings != null || panelComponent.parentUI != null;
            m_MissingPanelSettings.EnableInClassList(k_StyleClassPanelMissingHidden, hidePanelMissingWarning);

            m_PanelSettingsField.SetEnabled(panelComponent.parentUI == null);

            if (panelComponent.visualTreeAsset?.hasEditorElements == true)
            {
                m_EditorElementsWarning.text = GenerateEditorElementsErrorMessage(k_MaxElementsInEditorElementsErrorMessage);
                m_EditorElementsWarning.EnableInClassList(k_StyleClassPanelMissingHidden, false);
            }
            else
            {
                m_EditorElementsWarning.EnableInClassList(k_StyleClassPanelMissingHidden, true);
            }

            bool isWorldSpace = panelComponent.panelSettings?.renderMode == PanelRenderMode.WorldSpace;

            DisplayStyle sortingDisplayStyle = (!isWorldSpace || panelComponent.parentUI != null) ? DisplayStyle.Flex : DisplayStyle.None;
            m_PositionEnumField.style.display = panelComponent.parentUI != null? DisplayStyle.Flex : DisplayStyle.None;

            m_WorldSpaceDimensionsFoldout.style.display = PanelComponentUtils.IsTransformControlledByGameObject(panelComponent) ? DisplayStyle.Flex : DisplayStyle.None;

            bool isFixedSize = (panelComponent.worldSpaceSizeMode == WorldSpaceSizeMode.Fixed);
            var display = isFixedSize ? DisplayStyle.Flex : DisplayStyle.None;
            m_WorldSpaceWidthField.style.display = display;
            m_WorldSpaceHeightField.style.display = display;

            // Update button text and visibility based on whether a VisualTreeAsset is assigned
            bool hasVisualTreeAsset = panelComponent.visualTreeAsset != null;
            m_SourceAssetButton.text = hasVisualTreeAsset ? "Edit..." : "New";
            m_SourceAssetButton.tooltip = hasVisualTreeAsset ? k_InspectorEditSourceAssetButtonTooltip : k_InspectorNewSourceAssetButtonTooltip;

            // Let the component update its rendering properties (UUM-105765)
            panelComponent.PerformUpdate();
        }

        string GenerateEditorElementsErrorMessage(int maxElements)
        {
            var pc = (IPanelComponent)target;

            using (var pool = StringBuilderPool.Get(out var sb))
            {
                sb.AppendLine(k_EditorElementsError);

                int found = 0;
                using (var hashsetPool = HashSetPool<string>.Get(out var hashSet))
                {
                    foreach (var vta in pc.visualTreeAsset.DepthFirstTraversal())
                    {
                        var desc = UxmlSerializedDataRegistry.GetDescription(vta.fullTypeName);
                        if (desc?.isEditorOnly == true && !hashSet.Contains(vta.fullTypeName))
                        {
                            if (++found > maxElements)
                            {
                                sb.AppendLine("...");
                                break;
                            }

                            // Add the type name to the hash set to avoid duplicates.
                            hashSet.Add(vta.fullTypeName);

                            sb.Append("- ");
                            sb.AppendLine(vta.fullTypeName);
                        }
                    }
                }
                return sb.ToString();
            }
        }

        private void OnSourceAssetButtonClicked()
        {
            var pc = (IPanelComponent)target;
            var visualTreeAsset = pc.visualTreeAsset;

            if (visualTreeAsset != null)
            {
                // Open VTA asset in UI Builder
                AssetDatabase.OpenAsset(visualTreeAsset.GetEntityId());
            }
            else
            {
                // Creates new asset and upon successful rename will link asset to UIDocument
                UIElementsTemplate.CreateUXMLAssetWithCallback((instanceID =>
                {
                    pc.visualTreeAsset = EditorUtility.EntityIdToObject(instanceID) as VisualTreeAsset;
                    Selection.activeObject = pc.gameObject;
                    UpdateValues();
                }));
            }
        }

        private void UpdateInputConfigurationOptions()
        {
            var panelComp = (IPanelComponent)target;
            if ((panelComp as Object) == null)
                return;

            bool isWorldSpace = panelComp.panelSettings?.renderMode == PanelRenderMode.WorldSpace;
            bool hasNoConfig = PanelInputConfiguration.s_ActiveInstances == 0;
            m_InputConfiguration.style.display = hasNoConfig && isWorldSpace ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_RootVisualElement = new VisualElement();

            // BindTree can dispatch SerializedObjectBindEvent / SerializedPropertyBindEvent
            // to a stale InspectorElement during play-mode transitions.
            if (target == null)
                return m_RootVisualElement;

            if (s_InspectorUxml == null)
            {
                s_InspectorUxml = EditorGUIUtility.Load(k_InspectorVisualTreeAssetPath) as VisualTreeAsset;
            }

            if (s_DefaultStyleSheet == null)
            {
                s_DefaultStyleSheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            }
            m_RootVisualElement.styleSheets.Add(s_DefaultStyleSheet);

            s_InspectorUxml.CloneTree(m_RootVisualElement);
            ConfigureFields();

            m_CurrentPanelSettings = (target as IPanelComponent)?.panelSettings;
            
            BindFields();
            UpdateValues();

            UpdateInputConfigurationOptions();
            m_InputConfiguration.schedule.Execute(UpdateInputConfigurationOptions).Every(200);

            return m_RootVisualElement;
        }

        void AddPanelRendererComponent()
        {
            var component = (Component)target;
            var pr = component.gameObject.AddComponent<PanelRenderer>();

            var uiDoc = (target as UIDocument);
            if (uiDoc == null)
                return;

            // Copy properties from the UIDocument to the new PanelRenderer
            SerializedObject srcSO = new SerializedObject(uiDoc);
            SerializedObject dstSO = new SerializedObject(pr);

            SerializedProperty srcProp = srcSO.GetIterator();
            if (srcProp.NextVisible(true))
            {
                do
                {
                    // Skip m_SortingOrder as the type won't match (float in UIDocument, int in Renderer)
                    if (srcProp.propertyPath == "m_SortingOrder")
                        continue;

                    // Skip m_ParentUI, which is evaluated at insertion time
                    if (srcProp.propertyPath == "m_ParentUI")
                        continue;

                    SerializedProperty dstProp = dstSO.FindProperty(srcProp.propertyPath);
                    if (dstProp != null)
                    {
                        dstSO.CopyFromSerializedProperty(srcProp);
                    }
                }
                while (srcProp.NextVisible(false));
            }

            // Manually copy m_SortingOrder with type conversion (float -> int)
            SerializedProperty srcSortingOrder = srcSO.FindProperty("m_SortingOrder");
            SerializedProperty dstSortingOrder = dstSO.FindProperty("m_SortingOrder");
            if (srcSortingOrder != null && dstSortingOrder != null)
                dstSortingOrder.intValue = (int)srcSortingOrder.floatValue;

            dstSO.ApplyModifiedProperties();
        }
    }
}
