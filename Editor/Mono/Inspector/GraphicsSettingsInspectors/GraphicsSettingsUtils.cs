// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEditor.UIElements.ProjectSettings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    internal static class GraphicsSettingsUtils
    {
        #region Localization

        internal static void Localize(VisualElement visualElement, Func<VisualElement, string> get, Action<VisualElement, string> set)
        {
            if (get == null)
                throw new InvalidOperationException("get function cannot be null");
            if (set == null)
                throw new InvalidOperationException("set function cannot be null");

            var extractedText = get.Invoke(visualElement);
            if (string.IsNullOrWhiteSpace(extractedText))
                return;

            var localizedString = L10n.Tr(extractedText);
            set.Invoke(visualElement, localizedString);
        }

        internal static void LocalizeTooltip(VisualElement visualElement)
        {
            Localize(visualElement, e => e.tooltip, (e, s) => e.tooltip = s);
        }

        internal static void LocalizeText(Label visualElement)
        {
            Localize(visualElement, e => ((Label)e).text, (e, s) => ((Label)e).text = s);
        }

        internal static void LocalizeVisualTree(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(LocalizeTooltip);
            root.Query<Label>().ForEach(label =>
            {
                //Ignore text inside ObjectField because it's an asset name
                if (label.ClassListContains("unity-object-field-display__label"))
                    return;
                LocalizeText(label);
            });
        }

        #endregion

        #region Render Pipeline Assets extraction

        internal class GlobalSettingsContainer
        {
            public readonly string name;
            public readonly Type renderPipelineAssetType;
            public readonly SerializedProperty property;
            public readonly SerializedObject serializedObject;

            public GlobalSettingsContainer(string name, Type renderPipelineAssetType, SerializedProperty property, SerializedObject serializedObject)
            {
                this.name = name;
                this.renderPipelineAssetType = renderPipelineAssetType;
                this.property = property;
                this.serializedObject = serializedObject;
            }
        }

        internal static List<GlobalSettingsContainer> CollectRenderPipelineAssetsByGlobalSettings(SerializedProperty renderPipelineGlobalSettingsMap)
        {
            var existedGlobalSettings = new List<GlobalSettingsContainer>();
            for (int i = 0; i < renderPipelineGlobalSettingsMap.arraySize; ++i)
            {
                var globalSettings = GetRenderPipelineGlobalSettingsByIndex(renderPipelineGlobalSettingsMap, i);
                if (TryCreateNewGlobalSettingsContainer(globalSettings, out var globalSettingsContainer))
                    existedGlobalSettings.Add(globalSettingsContainer);
            }
            return existedGlobalSettings;
        }

        internal static bool TryCreateNewGlobalSettingsContainer(RenderPipelineGlobalSettings globalSettings, out GlobalSettingsContainer globalSettingsContainer)
        {
            globalSettingsContainer = null;

            if (globalSettings == null)
                return false;
                
            var globalSettingsSerializedObject = new SerializedObject(globalSettings);
            var settingsContainer = globalSettingsSerializedObject.FindProperty("m_Settings");

            var settingsListInContainer = settingsContainer != null
                ? settingsContainer.FindPropertyRelative("m_SettingsList")
                : null;

            if (settingsListInContainer != null && settingsListInContainer.arraySize != 0)
            {
                var globalSettingsType = globalSettings.GetType();
                if (ExtractSupportedOnRenderPipelineAttribute(globalSettingsType, out var supportedOnRenderPipelineAttribute))
                {
                    var tabName = CreateNewTabName(globalSettingsType, supportedOnRenderPipelineAttribute);
                    globalSettingsContainer = new GlobalSettingsContainer(tabName, supportedOnRenderPipelineAttribute.renderPipelineTypes[0], settingsContainer, globalSettingsSerializedObject);
                }
            }

            return globalSettingsContainer != null;
        }

        internal static RenderPipelineGlobalSettings GetRenderPipelineGlobalSettingsByIndex(SerializedProperty srpDefaultSettings, int i)
        {
            var property = srpDefaultSettings.GetArrayElementAtIndex(i);
            var second = property.FindPropertyRelative("second");
            var globalSettings = second.objectReferenceValue as RenderPipelineGlobalSettings;
            return globalSettings;
        }

        internal static string CreateNewTabName(Type globalSettingsType, SupportedOnRenderPipelineAttribute supportedOnRenderPipelineAttribute)
        {
            string tabName;
            var inspectorName = globalSettingsType.GetCustomAttribute<DisplayNameAttribute>();
            if (inspectorName != null)
                tabName = inspectorName.DisplayName;
            else
            {
                var pipelineAssetName = supportedOnRenderPipelineAttribute.renderPipelineTypes[0].Name;
                if (pipelineAssetName.EndsWith("Asset", StringComparison.Ordinal))
                    pipelineAssetName = pipelineAssetName[..^"Asset".Length];

                tabName = GetAbbreviation(pipelineAssetName);
            }

            return tabName;
        }

        internal static bool ExtractSupportedOnRenderPipelineAttribute(Type globalSettingsType, out SupportedOnRenderPipelineAttribute supportedOnRenderPipelineAttribute)
        {
            supportedOnRenderPipelineAttribute = globalSettingsType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
            if (supportedOnRenderPipelineAttribute == null)
            {
                Debug.LogWarning($"Cannot associate {globalSettingsType.FullName} settings with appropriate {nameof(RenderPipelineAsset)} without {nameof(SupportedOnRenderPipelineAttribute)}. Settings will be skipped and not displayed.");
                return false;
            }

            if (supportedOnRenderPipelineAttribute.renderPipelineTypes.Length != 1)
            {
                Debug.LogWarning($"{nameof(SupportedOnRenderPipelineAttribute)} for {globalSettingsType.FullName} settings must have exactly one parameter. Settings will be skipped and not displayed.");
                return false;
            }

            if (supportedOnRenderPipelineAttribute.renderPipelineTypes.Length == 1 && supportedOnRenderPipelineAttribute.renderPipelineTypes[0] == typeof(RenderPipelineAsset) )
            {
                Debug.LogWarning($"{nameof(SupportedOnRenderPipelineAttribute)} for {globalSettingsType.FullName} settings must have specific non-absract {nameof(RenderPipelineAsset)} type");
                return false;
            }

            return true;
        }

        internal static string GetAbbreviation(string text)
        {
            var nameArray = text.ToCharArray();
            var builder = new StringBuilder();
            for (int i = 0; i < nameArray.Length; i++)
            {
                if (char.IsUpper(nameArray[i]))
                    builder.Append(nameArray[i]);
            }

            var abbreviation = builder.ToString();
            return abbreviation.Length == 0 ? text : abbreviation;
        }

        #endregion

        #region UI-relative methods

        //Temp solution until we introduce custom editor support and title support for pipeline assets
        internal static void CreateNewTab(TabbedView tabView, string tabName, VisualElement tabTarget, bool active = false)
        {
            tabTarget.name = $"{tabName}SettingsContainer";
            LocalizeVisualTree(tabTarget);

            var tab = new TabButton(tabName, tabTarget)
            {
                name = $"{tabName}TabButton"
            };
            LocalizeVisualTree(tab);
            tabView.AddTab(tab, active);
        }

        internal static VisualElement CreateRPHelpBox(VisibilityControllerBasedOnRenderPipeline visibilityController, Type currentAssetType)
        {
            var helpBoxTemplate = EditorGUIUtility.Load(GraphicsSettingsInspector.ResourcesPaths.helpBoxesTemplateForSRP) as VisualTreeAsset;
            var helpBoxContainer = helpBoxTemplate.Instantiate();
            LocalizeVisualTree(helpBoxContainer);

            var allRenderPipelineAssetTypes = TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>();
            var allAssetsExceptCurrent = new Type[allRenderPipelineAssetTypes.Count];
            for (int j = 0, index = 0; j < allRenderPipelineAssetTypes.Count; j++, index++)
            {
                if (currentAssetType != null && allRenderPipelineAssetTypes[j] == currentAssetType)
                {
                    index--;
                    continue;
                }

                allAssetsExceptCurrent[index] = allRenderPipelineAssetTypes[j] == null ? null : allRenderPipelineAssetTypes[j];
            }

            var infoHelpBox = helpBoxContainer.MandatoryQ<HelpBox>("CurrentPipelineInfoHelpBox");
            var warningHelpBox = helpBoxContainer.MandatoryQ<HelpBox>("CurrentPipelineWarningHelpBox");
            visibilityController.RegisterVisualElement(infoHelpBox, currentAssetType);
            visibilityController.RegisterVisualElement(warningHelpBox, allAssetsExceptCurrent);
            return helpBoxContainer;
        }

        internal static IEnumerable<string> GetSearchKeywordsFromUXMLInEditorResources(string path)
        {
            var uxmlTree = EditorGUIUtility.Load(path) as VisualTreeAsset;
            if (uxmlTree == null)
                return Array.Empty<string>();

            var elements = uxmlTree.Instantiate();
            var labelTexts = new List<string>();
            elements
                .Query<Label>()
                .Where(l => !string.IsNullOrWhiteSpace(l.text))
                .ForEach(l => labelTexts.Add(l.text));
            return labelTexts;
        }

        #endregion

        #region Temporary methods

        // Stolen from BindingStyleHelpers.cs that cannot be accessed from here.
        static void RightClickFieldMenuEvent(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var element = evt.elementTarget;

            var property = element?.userData as SerializedProperty;
            if (property == null)
                return;

            var wasEnabled = GUI.enabled;
            if (!element.enabledInHierarchy)
                GUI.enabled = false;

            var menu = EditorGUI.FillPropertyContextMenu(property);
            GUI.enabled = wasEnabled;

            if (menu == null)
                return;

            var menuRect = new Rect(evt.position, Vector2.zero);
            menu.DropDown(menuRect);

            evt.StopPropagation();
        }

        // Stop ContextClickEvent because the context menu in the UITk inspector is shown on PointerUpEvent and not on ContextualMenuPopulateEvent (UUM-11643).
        static void StopContextClickEvent(ContextClickEvent e)
        {
            e.StopImmediatePropagation();
        }

        internal static Action BindSerializedProperty(DropdownField dropdown, SerializedProperty property, Func<SerializedProperty, int> getter, Action<int, SerializedProperty> setter)
        {
            dropdown.userData = property?.Copy();
            dropdown.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            dropdown.RegisterCallback<PointerUpEvent>(RightClickFieldMenuEvent, InvokePolicy.IncludeDisabled);
            dropdown.RegisterCallback<ContextClickEvent>(StopContextClickEvent, TrickleDown.TrickleDown);
            dropdown.RegisterValueChangedCallback(e => setter.Invoke(dropdown.index, property));

            var updateCallback = () =>
            {
                dropdown.index = getter.Invoke(property);
                dropdown.showMixedValue = property.hasMultipleDifferentValues;
            };
            updateCallback?.Invoke();
            return updateCallback;
        }

        internal static Action BindSerializedProperty<T>(EnumField enumField, SerializedProperty property, Action<T> onValueChange = null)
            where T : struct, Enum, IConvertible
        {
            var boolProperty = property.type == "bool";

            enumField.userData = property?.Copy();
            enumField.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            enumField.RegisterCallback<PointerUpEvent>(RightClickFieldMenuEvent, InvokePolicy.IncludeDisabled);
            enumField.RegisterCallback<ContextClickEvent>(StopContextClickEvent, TrickleDown.TrickleDown);
            enumField.RegisterValueChangedCallback(e =>
            {
                if (boolProperty)
                    property.boolValue = Convert.ToInt32(e.newValue) == 1;
                else
                    property.intValue = Convert.ToInt32(e.newValue);

                property.serializedObject.ApplyModifiedProperties();
                onValueChange?.Invoke((T)e.newValue);
            });

            var updateCallback = () =>
            {
                foreach (T value in Enum.GetValues(typeof(T)))
                {
                    var propertyValue = boolProperty ? (property.boolValue ? 1 : 0) : property.intValue;

                    if (value.ToInt32(null) != propertyValue)
                        continue;

                    enumField.value = value;
                    break;
                }

                enumField.showMixedValue = property.hasMultipleDifferentValues;
            };
            updateCallback?.Invoke();
            return updateCallback;
        }

        #endregion
    }
}
