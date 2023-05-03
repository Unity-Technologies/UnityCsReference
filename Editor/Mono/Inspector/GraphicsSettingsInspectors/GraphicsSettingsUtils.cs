// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
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
                if (label.ClassListContains("unity-object-field-display__label"))
                    return;
                LocalizeText(label);
            });
        }

        #endregion

        #region Render Pipeline Assets extraction

        internal static List<RenderPipelineAsset> CollectRenderPipelineAssetsFromSettings()
        {
            var usedRenderPipelineAssets = new List<RenderPipelineAsset>
            {
                GraphicsSettings.defaultRenderPipeline
            };
            for (var i = 0; i < QualitySettings.count; i++)
            {
                var qualityRPAsset = QualitySettings.GetRenderPipelineAssetAt(i);

                var willAdd = true;
                for (int j = 0; j < usedRenderPipelineAssets.Count; j++)
                {
                    var added = usedRenderPipelineAssets[j];
                    if (qualityRPAsset == null)
                    {
                        if (added == null)
                            willAdd = false;
                    }
                    else
                    {
                        if (added != null && added.GetType() == qualityRPAsset.GetType())
                            willAdd = false;
                    }
                }

                if (willAdd)
                    usedRenderPipelineAssets.Add(qualityRPAsset);
            }

            return usedRenderPipelineAssets;
        }

        internal static List<RenderPipelineAsset> GetCurrentPipelines(out bool srpInUse)
        {
            var usedRenderPipelineAssets = CollectRenderPipelineAssetsFromSettings();
            for (var i = 0; i < usedRenderPipelineAssets.Count; i++)
            {
                if (usedRenderPipelineAssets[i] == null || IsThereAnyPipelineSettings(usedRenderPipelineAssets[i]))
                    continue;
                usedRenderPipelineAssets.RemoveAt(i);
                --i;
            }

            srpInUse = IsSRPUsedSettings(usedRenderPipelineAssets);
            return usedRenderPipelineAssets;
        }

        static bool IsThereAnyPipelineSettings(RenderPipelineAsset pipelineAsset)
        {
            return false;
        }

        internal static bool IsSRPUsedSettings(List<RenderPipelineAsset> pipelineAssets)
        {
            if (pipelineAssets.Count == 0)
                return false;

            if (pipelineAssets.Count > 1)
                return true;

            return pipelineAssets[0] != null;
        }

        #endregion

        #region UI-relative methods

        //Temp solution until we introduce custom editor support and title support for pipeline assets
        internal static string GetPipelineAssetAbbreviation(RenderPipelineAsset asset)
        {
            var name = asset.GetType().Name;
            if (name.Contains("Asset"))
                name = name.Replace("Asset", "", StringComparison.Ordinal);

            var nameArray = name.ToCharArray();
            var resultedName = "";
            for (int i = 0; i < nameArray.Length; i++)
            {
                if (char.IsUpper(nameArray[i]))
                    resultedName += nameArray[i];
            }
            return string.Concat(resultedName);
        }

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

        internal static (VisualElement warning, Type[] activePipelines) CreateSRPWarning(string warningTemplatePath, List<RenderPipelineAsset> assets, RenderPipelineAsset srpAsset)
        {
            var warningAsset = EditorGUIUtility.Load(warningTemplatePath) as VisualTreeAsset;
            var warning = warningAsset.Instantiate();
            LocalizeVisualTree(warning);
            var allAssetsExceptCurrent = new Type[assets.Count - 1];
            for (int j = 0, index = 0; j < allAssetsExceptCurrent.Length; j++, index++)
            {
                if (assets[j] == srpAsset)
                    index++;

                allAssetsExceptCurrent[j] = assets[index] == null ? null : assets[index].GetType();
            }

            return (warning, allAssetsExceptCurrent);
        }

        internal static IEnumerable<string> GetSearchKeywordsFromUXMLInEditorResources(string path)
        {
            var uxmlTree = EditorGUIUtility.Load(path) as VisualTreeAsset;
            if (uxmlTree == null)
                return Array.Empty<string>();

            var elements = uxmlTree.Instantiate();
            var labels = elements
                .Query<Label>()
                .Where(l => !string.IsNullOrWhiteSpace(l.text))
                .Build()
                .ToList();

            var labelTexts = new List<string>();
            foreach (var label in labels)
            {
                labelTexts.Add(label.text);
            }

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
