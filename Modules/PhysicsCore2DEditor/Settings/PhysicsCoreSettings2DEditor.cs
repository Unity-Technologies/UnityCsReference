// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.U2D.Physics.Editor
{
    [CustomEditor(typeof(PhysicsCoreSettings2D))]
    sealed class PhysicsCoreSettings2DEditor : UnityEditor.Editor
    {
        static class ViewDataKey
        {
            public const string prefix = "PhysicsCore2D.ProjectSettings.";
        }

        static class Tooltips
        {
            public const string physicsLayerNames = $"A set of 64 layer names associated with each bit in a {nameof(PhysicsMask)} when used for contacts and queries.";

            public const string physicsWorldDefinition = $"A {nameof(PhysicsWorldDefinition)} used to specify important initial properties during {nameof(PhysicsWorld)} creation.";
            public const string physicsBodyDefinition = $"A {nameof(PhysicsBodyDefinition)} used to specify important initial properties during {nameof(PhysicsBody)} creation.";
            public const string physicsShapeDefinition = $"A {nameof(PhysicsShapeDefinition)} used to specify important initial properties during {nameof(PhysicsShape)} creation.";
            public const string physicsChainDefinition = $"A {nameof(PhysicsChainDefinition)} used to specify important initial properties during {nameof(PhysicsChain)} creation.";

            public const string physicsDistanceJointDefinition = $"A {nameof(PhysicsDistanceJointDefinition)} used to specify important initial properties during {nameof(PhysicsDistanceJoint)} creation.";
            public const string physicsFixedJointDefinition = $"A {nameof(PhysicsFixedJointDefinition)} used to specify important initial properties during {nameof(PhysicsFixedJoint)} creation.";
            public const string physicsHingeJointDefinition = $"A {nameof(PhysicsHingeJointDefinition)} used to specify important initial properties during {nameof(PhysicsHingeJoint)} creation.";
            public const string physicsRelativeJointDefinition = $"A {nameof(PhysicsRelativeJointDefinition)} used to specify important initial properties during {nameof(PhysicsRelativeJoint)} creation.";
            public const string physicsSliderJointDefinition = $"A {nameof(PhysicsSliderJointDefinition)} used to specify important initial properties during {nameof(PhysicsSliderJoint)} creation.";
            public const string physicsWheelJointDefinition = $"A {nameof(PhysicsWheelJointDefinition)} used to specify important initial properties during {nameof(PhysicsWheelJoint)} creation.";

            public const string transformChangeMode = $"Defines when changes to {nameof(UnityEngine.Transform)} that has are registered with {nameof(PhysicsWorld.RegisterTransformChange)} are called.";
            public const string contactFilterMode = $"The mode used for the {nameof(PhysicsShape.ContactFilter)} when determining if two {nameof(PhysicsShape)} can contact.";
            public const string renderingMode = "Controls drawing and rendering is allowed.";
            public const string maximumWorlds = "Controls the maximum number of worlds that can be created. The larger the number of worlds, the more memory that is initially allocated so care must be taken. Setting this value to one will reduce start-up memory usage to a minimum but will not allow any additional worlds to be created. Any change will only be actioned by Exiting Play mode in the Editor or restarting the player build. A single PhysicsWorld.defaultWorld is automatically created therefore occupies one of the available worlds.";
            public const string concurrentSimulations = $"Controls how many simulations can be started in parallel. Each one is started on its own worker and acts as its own main-thread. Workers should ideally be left free for the solver otherwise it may degrade solving performance. The actual quantity of workers used will always be capped to those available on the current device. If the total number of workers available is below 4 then parallel simulation won't occur however parallel solving using workers will. This should not be confused with the quantity of workers used when solving a simulation.";
            public const string lengthUnitsPerMeter = "The physics system relates all length units on meters but you may need different units for your project. You can set this value to use different units but it should only be modified before any other calls to the physics system occur and only modified once. Changing this value after any physics object has been created can result in severe simulation instabilities.";
            public const string usePhysicsLayers = $"Controls if the physics 64-bit layers are used based upon {nameof(PhysicsCoreSettings2D.physicsLayerNames)} or if not, the standard 32-bit layers based upon {nameof(UnityEngine.LayerMask)}.";
            public const string disableSimulation = $"Controls the simulation of any {nameof(PhysicsWorld)} temporarily removing simulation overhead. When true, no automatic simulation will occur. When false, normal operation occurs with automatic simulation.";
            public const string alwaysDrawWorlds = "Controls if worlds are always drawn independent of whether rendering is currently active or not as specified by PhysicsWorld.renderingMode. When true, world drawing is always active and a PhysicsEvents.WorldDraw event is produced with the PhysicsWorld.DrawResults. When false, world drawing only occurs depending on the PhysicsWorld.renderingMode setting. CAUTION: Drawing the world has a performance cost associated with it therefore when using this without rendering, that cost can become hidden.";
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Create the open button.
            var openButton = new Button { text = "Open in Project Settings" };
            openButton.clicked += () => SettingsService.OpenProjectSettings(PhysicsCoreSettings2DProvider.SettingsPath);
            root.Add(openButton);

            return root;
        }

        public void OnEnable()
        {
            var root = CreatePropertyGUI(serializedObject);
            PhysicsCoreSettings2DProvider.SetSettingsObject(root, serializedObject);
        }

        public void OnDisable() => PhysicsCoreSettings2DProvider.ClearSettingsObject();

        protected override bool ShouldHideOpenButton() => true;

        static public VisualElement CreatePropertyGUI(SerializedObject serializedObject)
        {
            // Create settings root.
            var physicsCoreSettingsUXML = EditorGUIUtility.Load(PhysicsCoreProjectSettings2DProvider.UXMLPath.physicsCoreSettings2D) as VisualTreeAsset;
            var root = physicsCoreSettingsUXML.Instantiate();

            // Fetch the selected settings.
            var selectedSettings = serializedObject.targetObject as PhysicsCoreSettings2D;

            // Set title.
            var titleLabel = root.Q<Label>("settings-title");
            titleLabel.text = selectedSettings.name + (PhysicsEditorOnly.physicsSettings == selectedSettings ? " (Active)" : " (Inactive)");

            // Add styles.
            root.styleSheets.Add(EditorGUIUtility.Load(PhysicsCoreProjectSettings2DProvider.StyleSheetPath.projectSettingsSheet) as StyleSheet);
            root.styleSheets.Add(EditorGUIUtility.Load(PhysicsCoreProjectSettings2DProvider.StyleSheetPath.projectSettingsCommonSheet) as StyleSheet);
            root.styleSheets.Add(EditorGUIUtility.Load(PhysicsCoreProjectSettings2DProvider.StyleSheetPath.commonSheet) as StyleSheet);
            root.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? PhysicsCoreProjectSettings2DProvider.StyleSheetPath.darkSheet : PhysicsCoreProjectSettings2DProvider.StyleSheetPath.lightSheet) as StyleSheet);

            // Add tabs.
            SetupTabLayers(root.Q("tab-layers-content"));
            SetupTabDefaultDefinitions(root.Q("tab-default-definitions-content"));
            SetupTabGlobal(root.Q("tab-global-content"));

            // Tab view.
            var tabView = root.Q<TabView>("settings-tabs");
            tabView.selectedTabIndex = EditorPrefs.GetInt(ViewDataKey.prefix + "selectedTabIndex", 0);
            tabView.activeTabChanged += (s, e) => { EditorPrefs.SetInt(ViewDataKey.prefix + "selectedTabIndex", tabView.selectedTabIndex); };

            // Bind.
            root.Bind(serializedObject);

            return root;

            #region Locals

            void SetupTabLayers(VisualElement root)
            {
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsLayerNames), Tooltips.physicsLayerNames, (settings) => settings.physicsLayerNames = PhysicsLayers.LayerNames.DefaultLayerNames));
            }

            void SetupTabDefaultDefinitions(VisualElement root)
            {
                // General.
                {
                    var definitionSection = new Foldout { text = "General", viewDataKey = typeof(PhysicsCoreSettings2DEditor).ToString() + "_GeneralDefinitionsFoldout" };
                    root.Add(definitionSection);

                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsWorldDefinition), Tooltips.physicsWorldDefinition, (settings) => settings.physicsWorldDefinition = new PhysicsWorldDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsBodyDefinition), Tooltips.physicsBodyDefinition, (settings) => settings.physicsBodyDefinition = new PhysicsBodyDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsShapeDefinition), Tooltips.physicsShapeDefinition, (settings) => settings.physicsShapeDefinition = new PhysicsShapeDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsChainDefinition), Tooltips.physicsChainDefinition, (settings) => settings.physicsChainDefinition = new PhysicsChainDefinition(false)));
                }

                // Joints.
                {
                    var definitionSection = new Foldout { text = "Joints", viewDataKey = typeof(PhysicsCoreSettings2DEditor).ToString() + "_JointDefinitionsFoldout" };
                    root.Add(definitionSection);

                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsDistanceJointDefinition), Tooltips.physicsDistanceJointDefinition, (settings) => settings.physicsDistanceJointDefinition = new PhysicsDistanceJointDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsFixedJointDefinition), Tooltips.physicsFixedJointDefinition, (settings) => settings.physicsFixedJointDefinition = new PhysicsFixedJointDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsHingeJointDefinition), Tooltips.physicsHingeJointDefinition, (settings) => settings.physicsHingeJointDefinition = new PhysicsHingeJointDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsRelativeJointDefinition), Tooltips.physicsRelativeJointDefinition, (settings) => settings.physicsRelativeJointDefinition = new PhysicsRelativeJointDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsSliderJointDefinition), Tooltips.physicsSliderJointDefinition, (settings) => settings.physicsSliderJointDefinition = new PhysicsSliderJointDefinition(false)));
                    definitionSection.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_PhysicsWheelJointDefinition), Tooltips.physicsWheelJointDefinition, (settings) => settings.physicsWheelJointDefinition = new PhysicsWheelJointDefinition(false)));
                }
            }

            void SetupTabGlobal(VisualElement root)
            {
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_TransformChangeMode), Tooltips.transformChangeMode, (settings) => settings.transformChangeMode = PhysicsWorld.TransformChangeMode.FixedUpdate,  false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_ContactFilterMode), Tooltips.contactFilterMode, (settings) => settings.contactFilterMode = PhysicsShape.ContactFilterMode.Both, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_RenderingMode), Tooltips.renderingMode, (settings) => settings.renderingMode = PhysicsWorld.RenderingMode.EditorOnly, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_MaximumWorlds), Tooltips.maximumWorlds, (settings) => settings.maximumWorlds = 128, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_ConcurrentSimulations), Tooltips.concurrentSimulations, (settings) => settings.concurrentSimulations = 2, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_LengthUnitsPerMeter), Tooltips.lengthUnitsPerMeter, (settings) => settings.lengthUnitsPerMeter = 1.0f, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_UsePhysicsLayers), Tooltips.usePhysicsLayers, (settings) => settings.usePhysicsLayers = false, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_DisableSimulation), Tooltips.disableSimulation, (settings) => settings.disableSimulation = false, false));
                root.Add(CreateInspectorPropertyField(nameof(PhysicsCoreSettings2D.m_AlwaysDrawWorlds), Tooltips.alwaysDrawWorlds, (settings) => settings.alwaysDrawWorlds = false, false));
            }

            VisualElement CreateInspectorPropertyField(string propertyName, string tooltip, Action<PhysicsCoreSettings2D> resetAction, bool useSection = true)
            {
                var property = serializedObject.FindProperty(propertyName);
                var root = new PropertyField(property)
                {
                    tooltip = tooltip,
                    viewDataKey = ViewDataKey.prefix + propertyName
                };

                root.AddManipulator(new ContextualMenuManipulator((evt) => { evt.menu.AppendAction($"Reset \"{property.displayName}\" to Default", (_) =>
                {
                    var obj = serializedObject.targetObject as PhysicsCoreSettings2D;
                    Undo.RecordObject(obj, nameof(PhysicsCoreSettings2DEditor));
                    resetAction(obj);
                    EditorUtility.SetDirty(obj);
                    AssetDatabase.SaveAssetIfDirty(obj);
                }); }));

                root.AddToClassList(InspectorElement.ussClassName);

                if (useSection)
                {
                    var section = new VisualElement();
                    section.AddToClassList("project-settings__physics__section");
                    section.Add(root);
                    return section;
                }

                return root;
            }

            #endregion
        }
    }

    #region Provider

    class PhysicsCoreSettings2DProvider : SettingsProvider
    {
        public const string SettingsPath = "Project/PhysicsCore2D/Settings";
        const string EmptySettingsLabel = $"Select a {nameof(PhysicsCoreSettings2D)} Asset to edit ...";

        public static PhysicsCoreSettings2DProvider Instance { get; private set; }

        private VisualElement m_ProviderRoot;

        public PhysicsCoreSettings2DProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        public static void SetSettingsObject(VisualElement root, SerializedObject serializedObject)
        {
            if (Instance != null && Instance.m_ProviderRoot != null)
            {
                Instance.m_ProviderRoot.Clear();
                Instance.m_ProviderRoot.Add(root);
                Instance.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);
            }
        }

        public static void ClearSettingsObject()
        {
            if (Instance != null && Instance.m_ProviderRoot != null)
            {
                Instance.m_ProviderRoot.Clear();
                Instance.m_ProviderRoot.Add(CreateEmptyPropertyGUI());
            }
        }

        static VisualElement CreateEmptyPropertyGUI()
        {
            var root = new VisualElement();
            root.style.paddingTop = 8;
            root.style.paddingLeft = 8;
            root.Add(new Label(EmptySettingsLabel));
            return root;
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            Instance = new PhysicsCoreSettings2DProvider(SettingsPath, SettingsScope.Project)
            {
                activateHandler = (searchContext, root) =>
                {
                    Instance.m_ProviderRoot = root;

                    if (Selection.activeObject is PhysicsCoreSettings2D)
                    {
                        var serializedObject = new SerializedObject(Selection.activeObject);
                        var editorRoot = PhysicsCoreSettings2DEditor.CreatePropertyGUI(serializedObject);

                        SetSettingsObject(editorRoot, serializedObject);
                        return;
                    }

                    // Not core settings so create an empty page (Empty State).
                    Instance.m_ProviderRoot.Clear();
                    Instance.m_ProviderRoot.Add(CreateEmptyPropertyGUI());
                },

                deactivateHandler = () => Instance.m_ProviderRoot = null,
            };

            return Instance;
        }
    }

    #endregion
}
