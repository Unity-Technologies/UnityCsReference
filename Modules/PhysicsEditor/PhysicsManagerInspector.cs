// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEditor.UIElements;
using System.Runtime.CompilerServices;
using IPhysicsProjectSettingsECSInspectorExtension = UnityEditorInternal.IPhysicsProjectSettingsECSInspectorExtension;

[assembly: InternalsVisibleTo("Unity.Physics.Editor.ProjectSettingsBridge")]
namespace UnityEditor
{
    class PhysicsManagerInspector
    {
        public static IPhysicsProjectSettingsECSInspectorExtension EcsExtension = null;

        static class AssetPath
        {
            public const string dynamics = "ProjectSettings/DynamicsManager.asset";
            public static readonly string dynamicsError = $"{nameof(PhysicsManagerInspector.CreatePhysicsSettingsItemProvider)} failed to load asset {dynamics} containing data for the PhysicsManager.";

            public const string tags = "ProjectSettings/TagManager.asset";
            public static readonly string tagsError = $"{nameof(PhysicsManagerInspector.CreatePhysicsSettingsItemProvider)} failed to load asset {tags} containing data for the TagManager.";
        }

        static class StyleSheetPath
        {
            public const string projectSettingsSheet = "StyleSheets/ProjectSettings/ProjectSettingsCommon.uss";
            public const string commonSheet = "StyleSheets/Extensions/base/common.uss";
            public const string darkSheet = "StyleSheets/Extensions/base/dark.uss";
            public const string lightSheet = "StyleSheets/Extensions/base/light.uss";
        }

        static class UXMLPath
        {
            public const string projectSettingsMain = "Physics/UXML/ProjectSettings.uxml";
            public const string projectSettingsSub = "Physics/UXML/ProjectSettingsSub.uxml";
            public const string physicsLayerGrid = "Physics/UXML/PhysicsLayerGrid.uxml";
        }

        const string k_layerMatrixFoldoutPref = "project-settings-collision-matrix-unfold";
        const int k_MaxLayers = 32;

        static SerializedObject LoadGameManagerAssetAtPath(string path)
        {
            var found = AssetDatabase.LoadAllAssetsAtPath(path);
            if (found == null)
                return null;

            return new SerializedObject(found[0]);
        }

        //create the main page, shows warnings/info
        [SettingsProvider]
        static SettingsProvider CreatePhysicsSettingsItemProvider()
        {
            var provider = new SettingsProvider("Project/Physics", SettingsScope.Project)
            {
                label = "Physics",
                keywords = SettingsProvider.GetSearchKeywordsFromPath(AssetPath.dynamics),
                activateHandler = (searchContext, rootElement) =>
                {
                    var serializedObject = LoadGameManagerAssetAtPath(AssetPath.dynamics);
                    if (serializedObject == null)
                    {
                        Debug.LogError(AssetPath.dynamicsError);
                        return;
                    }

                    var mainUXML = EditorGUIUtility.Load(UXMLPath.projectSettingsMain) as VisualTreeAsset;
                    mainUXML.CloneTree(rootElement);

                    var content = rootElement.Q<ScrollView>(className: "project-settings-section-content");
                    content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.projectSettingsSheet) as StyleSheet);
                    content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.commonSheet) as StyleSheet);
                    content.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? StyleSheetPath.darkSheet : StyleSheetPath.lightSheet) as StyleSheet);

                    //direct memory access to the infos array, shouldn't be bound to any callback
                    ReadOnlySpan<IntegrationInfo> infos = Physics.GetIntegrationInfos();

                    var classicEngineDropdown = rootElement.Q<DropdownField>(name: "classic-dropdown");
                    var classicEngineHelpboxWarning = rootElement.Q<HelpBox>(name: "classic-helpbox-warning");
                    classicEngineHelpboxWarning.text = "You've changed the active physics SDK integration. This requires a restart of the Editor for the change to take effect.";
                    classicEngineHelpboxWarning.visible = false;

                    int currentChoiceIndex = 0;
                    uint currentId = Physics.GetCurrentIntegrationId();
                    for (int i = 0; i < infos.Length; ++i)
                    {
                        IntegrationInfo info = infos[i];
                        classicEngineDropdown.choices.Add(info.Name);
                        if (currentId == info.Id)
                            currentChoiceIndex = i;
                    }

                    classicEngineDropdown.value = classicEngineDropdown.choices[currentChoiceIndex];

                    if (!Unsupported.IsDeveloperMode() || Application.isPlaying)
                        classicEngineDropdown.SetEnabled(false);
                    else
                    {
                        classicEngineDropdown.RegisterValueChangedCallback((evt) =>
                        {
                            if (evt.newValue == evt.previousValue)
                                return;

                            uint oldIntegrationId = Physics.GetCurrentIntegrationId();
                            uint newIntegrationId = 0;
                            ReadOnlySpan<IntegrationInfo> integrationInfos = Physics.GetIntegrationInfos();
                            for (int i = 0; i < integrationInfos.Length; ++i)
                            {
                                IntegrationInfo info = integrationInfos[i];
                                if (info.Name == evt.newValue)
                                {
                                    newIntegrationId = info.Id;
                                }
                            }

                            var idProp = serializedObject.FindProperty("m_CurrentBackendId");
                            idProp.uintValue = newIntegrationId;

                            //force apply the property here as we want to ensure that the change is done immediately 
                            serializedObject.ApplyModifiedProperties();

                            //enable warning box if we are swapping
                            classicEngineHelpboxWarning.visible = newIntegrationId != Physics.GetCurrentIntegrationId();
                        });
                    }

                    var ecsEngineDropdown = rootElement.Q<DropdownField>(name: "ecs-dropdown");
                    var ecsEngineHelpboxInfo = rootElement.Q<HelpBox>(name: "ecs-helpbox-info");
                    var ecsEngineHelpboxWarning = rootElement.Q<HelpBox>(name: "ecs-helpbox-warning");

                    if (EcsExtension != null)
                    {
                        EcsExtension.SetupMainPageItems(ecsEngineDropdown, ecsEngineHelpboxInfo, ecsEngineHelpboxWarning, serializedObject);
                    }
                    else
                    {
                        ecsEngineDropdown.choices.Add("None");
                        ecsEngineDropdown.value = ecsEngineDropdown.choices[0];
                        ecsEngineDropdown.visible = false;
                        ecsEngineHelpboxInfo.visible = false;
                        ecsEngineHelpboxWarning.visible = false;
                    }

                    //bind data object
                    rootElement.Bind(serializedObject);
                }
            };

            return provider;
        }

        struct LayerData
        {
            public int bit;
            public string name;
        }

        class ToggleData
        {
            public int gridX;
            public int gridY;
            public int layerBit0;
            public int layerBit1;
            public VisualElement sideLabels;
            public VisualElement topLabels;
            public VisualElement overlay;
        }

        class LayerGridData
        {
            public List<LayerData> layers;
            public SerializedObject physicsManager;
            public SerializedObject tagManager;
        }

        static void AddGridOverlayLines(VisualElement toggle)
        {
            var userData = toggle.userData as ToggleData;

            var horizontalLabel = userData.sideLabels[userData.gridX];
            var verticalLabel = userData.topLabels[(userData.topLabels.childCount - 1) - userData.gridY];

            //both side and top labels have the same number of active labels
            //but only top labels has the exact number of children as it is being generated.
            int totalLabelsPerside = userData.topLabels.childCount;

            //horizontal bar
            var horizontalBox = new Box();
            horizontalBox.AddToClassList("project-settings___physics__highlight-box");
            horizontalBox.style.height = horizontalLabel.resolvedStyle.height;
            horizontalBox.style.width =
                horizontalLabel.resolvedStyle.width + (totalLabelsPerside - userData.gridX) * (toggle.resolvedStyle.width + toggle.resolvedStyle.marginLeft + toggle.resolvedStyle.marginRight);

            //absolute pos calculate for horizontal bar
            horizontalBox.style.left = userData.sideLabels.resolvedStyle.width - horizontalLabel.resolvedStyle.width;
            horizontalBox.style.top = userData.topLabels.resolvedStyle.width
            + ((horizontalLabel.resolvedStyle.height) * userData.gridX) + horizontalLabel.resolvedStyle.paddingTop;


            //vertical bar
            var verticalBox = new Box();
            verticalBox.AddToClassList("project-settings___physics__highlight-box");
            verticalBox.style.width = verticalLabel.resolvedStyle.height;
            verticalBox.style.height =
                verticalLabel.resolvedStyle.width +
                horizontalLabel.resolvedStyle.paddingRight +
                (totalLabelsPerside - userData.gridY) * (toggle.resolvedStyle.height + toggle.resolvedStyle.marginTop + toggle.resolvedStyle.marginBottom);

            //absolute pos calculate for vertical bar
            verticalBox.style.left = userData.sideLabels.resolvedStyle.width + verticalLabel.resolvedStyle.height * userData.gridY;
            verticalBox.style.top = userData.topLabels.resolvedStyle.width - verticalLabel.resolvedStyle.width;

            userData.overlay.Add(horizontalBox);
            userData.overlay.Add(verticalBox);
        }

        static void ClearGridOverlayLines(VisualElement toggle)
        {
            var userData = toggle.userData as ToggleData;

            while (userData.overlay.childCount > 0)
                userData.overlay.RemoveAt(0);
        }

        static void SetupLayerCollisionMatrix(VisualElement layerGridContainer)
        {
            var topLeftSpacer = layerGridContainer.Q<VisualElement>(name: "top-left-spacer");
            var topLabels = layerGridContainer.Q<VisualElement>(name: "top-labels");
            var sideLabels = layerGridContainer.Q<VisualElement>(name: "side-labels");
            var toggles = layerGridContainer.Q<VisualElement>(name: "toggles");
            var overlay = layerGridContainer.Q<VisualElement>(name: "layer-grid-overlay");

            Assert.AreEqual(k_MaxLayers, sideLabels.childCount);
            Assert.AreEqual(sideLabels.childCount, toggles.childCount);

            var data = layerGridContainer.userData as LayerGridData;
            data.layers.Clear();
            data.layers.Capacity = k_MaxLayers;

            for (int i = 0; i < k_MaxLayers; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (LayerMask.LayerToName(i) == string.Empty)
                    continue;

                data.layers.Add(new LayerData() { bit = i, name = layerName });
            }

            for (int i = 0; i < data.layers.Count; ++i)
            {
                var layer0 = data.layers[i];
                var label = sideLabels[i] as Label;
                label.text = layer0.name;

                var tLabel = new Label();
                tLabel.AddToClassList("project-settings___physics__toggle-col-label");
                tLabel.text = layer0.name;
                topLabels.Add(tLabel);

                var line = toggles[i];

                //generate visible toggles,
                for (int j = 0; j < data.layers.Count - i; ++j)
                {
                    var toggle = line[j] as Toggle;
                    toggle.AddToClassList("project-settings___physics__toggle");
                    var layer1 = data.layers[(data.layers.Count - 1) - j];

                    toggle.value = !Physics.GetIgnoreLayerCollision(layer0.bit, layer1.bit);
                    toggle.userData = new ToggleData() { gridX = i, gridY = j, layerBit0 = layer0.bit, layerBit1 = layer1.bit, sideLabels = sideLabels, topLabels = topLabels, overlay = overlay };

                    toggle.RegisterValueChangedCallback((evt) =>
                    {
                        if (evt.newValue == evt.previousValue)
                            return;

                        var userData = ((evt.target as VisualElement).userData as ToggleData);
                        Physics.IgnoreLayerCollision(userData.layerBit0, userData.layerBit1, !evt.newValue);
                    });

                    toggle.RegisterCallback((MouseEnterEvent evt) =>
                    {
                        AddGridOverlayLines((evt.target as VisualElement));

                    }, TrickleDown.NoTrickleDown);

                    toggle.RegisterCallback((MouseLeaveEvent evt) =>
                    {
                        ClearGridOverlayLines((evt.target as VisualElement));

                    }, TrickleDown.NoTrickleDown);
                }

                //disable invisible toggles inside the active lines
                for (int j = data.layers.Count - i; j < line.childCount; ++j)
                {
                    var toggle = line[j] as Toggle;
                    toggle.style.display = DisplayStyle.None;
                }
            }

            //loop over the following labels + toggle lines and disable them
            for (int i = data.layers.Count; i < sideLabels.childCount; ++i)
            {
                sideLabels[i].style.display = DisplayStyle.None;
                toggles[i].style.display = DisplayStyle.None;
            }

            SerializedProperty layerCollisionMatrix = data.physicsManager.FindProperty("m_LayerCollisionMatrix");
            toggles.TrackPropertyValue(layerCollisionMatrix);
            toggles.TrackPropertyValue(layerCollisionMatrix, (prop) =>
            {
                for (int i = 0; i < k_MaxLayers; ++i)
                {
                    var line = toggles[i];
                    if (line.style.display == DisplayStyle.None)
                        continue;

                    for (int j = 0; j < line.childCount; ++j)
                    {
                        var toggle = line[j] as Toggle;
                        if (toggle.style.display == DisplayStyle.None)
                            continue;

                        var userData = toggle.userData as ToggleData;
                        toggle.value = !Physics.GetIgnoreLayerCollision(userData.layerBit0, userData.layerBit1);
                    }
                }
            });

            var enableAll = layerGridContainer.Q<Button>("enable-all");
            enableAll.clicked += () =>
            {
                for (int i = 0; i < k_MaxLayers; ++i)
                {
                    var line = toggles[i];
                    for (int j = i; j < k_MaxLayers; ++j)
                    {
                        Physics.IgnoreLayerCollision(i, j, false);

                        if (line.style.display == DisplayStyle.None || j >= line.childCount)
                            continue;

                        var toggle = line[j] as Toggle;
                        if (toggle.style.display == DisplayStyle.None)
                            continue;

                        toggle.SetValueWithoutNotify(true);
                    }
                }
            };

            var disableAll = layerGridContainer.Q<Button>("disable-all");
            disableAll.clicked += () =>
            {
                for (int i = 0; i < k_MaxLayers; ++i)
                {
                    var line = toggles[i];
                    for (int j = i; j < k_MaxLayers; ++j)
                    {
                        Physics.IgnoreLayerCollision(i, j, true);

                        if (line.style.display == DisplayStyle.None || j >= line.childCount)
                            continue;

                        var toggle = line[j] as Toggle;
                        if (toggle.style.display == DisplayStyle.None)
                            continue;

                        toggle.SetValueWithoutNotify(false);
                    }
                }
            };

            layerGridContainer.RegisterCallbackOnce<GeometryChangedEvent>((evt) =>
            {
                //resize elements so they properly align
                topLeftSpacer.style.width = sideLabels.resolvedStyle.width;
                topLeftSpacer.style.height = sideLabels.resolvedStyle.width;

                float longestLineWidth = 0.0f;
                for (int i = 0; i < toggles.childCount; ++i)
                {
                    var lineWidth = toggles[i].resolvedStyle.width;
                    if (longestLineWidth < lineWidth)
                        longestLineWidth = lineWidth;
                }
                toggles.style.width = longestLineWidth;

                topLabels.style.width = sideLabels.resolvedStyle.width;
                topLabels.style.height = sideLabels.resolvedStyle.height;
                topLabels.style.left = sideLabels.resolvedStyle.width + longestLineWidth;
            });
        }

        static void SetupSharedTab(VisualElement tab, SerializedObject serializedObject, SerializedObject tagManager)
        {
            tab.Add(new PropertyField(serializedObject.FindProperty("m_Gravity")));

            bool fold = false;
            if (EditorPrefs.HasKey(k_layerMatrixFoldoutPref))
                fold = EditorPrefs.GetBool(k_layerMatrixFoldoutPref);

            var layerGridContainer = new VisualElement();
            var layerGridUXML = EditorGUIUtility.Load(UXMLPath.physicsLayerGrid) as VisualTreeAsset;
            layerGridUXML.CloneTree(layerGridContainer);

            layerGridContainer.userData = new LayerGridData() { layers = new List<LayerData>(), physicsManager = serializedObject, tagManager = tagManager };
            SetupLayerCollisionMatrix(layerGridContainer);

            layerGridContainer.TrackPropertyValue(tagManager.FindProperty("layers"), (prop) =>
            {
                //scrap the whole tree
                layerGridContainer.RemoveAt(0);
                layerGridUXML.CloneTree(layerGridContainer);

                SetupLayerCollisionMatrix(layerGridContainer);
            });

            var foldOut = new Foldout() { text = "Layer Collision Matrix", value = true };
            foldOut.RegisterValueChangedCallback((evt) => { EditorPrefs.SetBool(k_layerMatrixFoldoutPref, evt.newValue); });

            //patch in the correct initial value now that we've computed the layout with the unfolded values
            //we do this step due to the deffered layout computation for the top labels of the collision layer matrix
            foldOut.RegisterCallbackOnce<GeometryChangedEvent>((evt) => { foldOut.SetValueWithoutNotify(fold); });

            foldOut.Add(layerGridContainer);
            tab.Add(foldOut);
        }

        static void SetupClassicTab(VisualElement tab, SerializedObject serializedObject)
        {
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultMaterial")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_BounceThreshold")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultMaxDepenetrationVelocity")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_SleepThreshold")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultContactOffset")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultSolverIterations")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultSolverVelocityIterations")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_QueriesHitBackfaces")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_QueriesHitTriggers")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_EnableAdaptiveForce")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_SimulationMode")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_AutoSyncTransforms")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_ReuseCollisionCallbacks")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_InvokeCollisionCallbacks")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_ContactPairsMode")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_BroadphaseType")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_FrictionType")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_EnableEnhancedDeterminism")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_EnableUnifiedHeightmaps")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_ImprovedPatchFriction")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_SolverType")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_DefaultMaxAngularSpeed")));
            tab.Add(new PropertyField(serializedObject.FindProperty("m_ScratchBufferChunkCount")));
        }

        static void SetupClothTab(VisualElement tab, SerializedObject serializedObject)
        {
            tab.Add(new PropertyField(serializedObject.FindProperty("m_ClothGravity"), "Gravity Override"));

            var interCollisionToggle = new PropertyField(serializedObject.FindProperty("m_ClothInterCollisionSettingsToggle"), "Enable Inter-Collision");
            var interCollisionDistance = new PropertyField(serializedObject.FindProperty("m_ClothInterCollisionDistance"), "Inter-Collision Distance");
            var interCollisionStiffness = new PropertyField(serializedObject.FindProperty("m_ClothInterCollisionStiffness"), "Inter-Collision Stiffness");

            interCollisionToggle.RegisterValueChangeCallback(
                (evt) =>
                {
                    bool res = evt.changedProperty.boolValue;

                    interCollisionDistance.style.display = res ? DisplayStyle.Flex : DisplayStyle.None;
                    interCollisionStiffness.style.display = res ? DisplayStyle.Flex : DisplayStyle.None;
                });

            tab.Add(interCollisionToggle);
            tab.Add(interCollisionDistance);
            tab.Add(interCollisionStiffness);
        }

        static void SetupECSTab(TabView tabs, SerializedObject serializedObject)
        {
            if (EcsExtension == null)
                return;

            var tab = new Tab() { label = "ECS", name = "tab__ecs" };
            var tabContent = new VisualElement() { name = "tab-content__ecs" };
            tabContent.AddToClassList("project-settings__physics__tab-content");

            try
            {
                EcsExtension.SetupSettingsTab(tab, serializedObject);

                tab.Add(tabContent);
                tabs.Add(tab);
            }
            catch (Exception ex)
            {
                //consume the exception without breaking the settings pane
                Debug.LogException(ex);
            }
        }

        [SettingsProvider]
        static SettingsProvider CreatePhysicsSettingsPageProvider()
        {
            var provider = new SettingsProvider("Project/Physics/Settings", SettingsScope.Project)
            {
                label = "Settings",
                keywords = SettingsProvider.GetSearchKeywordsFromPath(AssetPath.dynamics)
            };

            provider.activateHandler = (searchContext, rootElement) =>
            {
                var serializedObject = LoadGameManagerAssetAtPath(AssetPath.dynamics);
                if (serializedObject == null)
                {
                    Debug.LogError(AssetPath.dynamicsError);
                    return;
                }

                var serializedTagManager = LoadGameManagerAssetAtPath(AssetPath.tags);
                if (serializedObject == null)
                {
                    Debug.LogError(AssetPath.tagsError);
                    return;
                }

                var subUXML = EditorGUIUtility.Load(UXMLPath.projectSettingsSub) as VisualTreeAsset;
                subUXML.CloneTree(rootElement);

                var content = rootElement.Q<ScrollView>(className: "project-settings-section-content");
                content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.projectSettingsSheet) as StyleSheet);
                content.styleSheets.Add(EditorGUIUtility.Load(StyleSheetPath.commonSheet) as StyleSheet);
                content.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? StyleSheetPath.darkSheet : StyleSheetPath.lightSheet) as StyleSheet);

                SetupSharedTab(content.Q(name: "tab-content__shared"), serializedObject, serializedTagManager);
                SetupClassicTab(content.Q(name: "tab-content__classic"), serializedObject);
                SetupClothTab(content.Q(name: "tab-content__cloth"), serializedObject);
                SetupECSTab(content.Q<TabView>(name: "setting-tabs"), serializedObject);

                //bind data object
                rootElement.Bind(serializedObject);
            };

            return provider;
        }
    }
}
