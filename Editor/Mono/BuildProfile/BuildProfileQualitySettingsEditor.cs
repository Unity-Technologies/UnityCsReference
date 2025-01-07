// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    [CustomEditor(typeof(BuildProfileQualitySettings))]
    sealed class BuildProfileQualitySettingsEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileQualitySettings.uxml";
        const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";
        const string k_QualitySettingsWindow = "Project/Quality";
        static readonly GUIContent k_qualitySettingsWindow = EditorGUIUtility.TrTextContent("Quality...");
        static readonly string k_InvalidQualityLevelWarning =
            L10n.Tr("The Quality levels in this profile do not match those that exist in the project. This may result in unexpected results on build.");
        static readonly string k_EmptyQualitySettingsWarning =
            L10n.Tr("When no Quality levels are listed, the build will take from the global list of Quality levels.");
        static readonly string k_SetDefaultQualityLevelMenuText = L10n.Tr("Set as Default");

        SerializedProperty m_QualityLevels;
        SerializedProperty m_DefaultQualityLevel;
        HelpBox warning;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            m_QualityLevels = serializedObject.FindProperty("m_QualityLevels");
            m_DefaultQualityLevel = serializedObject.FindProperty("m_DefaultQualityLevel");

            root.Bind(serializedObject);
            SetupQualityLevelsList(root);

            return root;
        }

        void SetupQualityLevelsList(VisualElement root)
        {
            warning = root.Q<HelpBox>("invalid-quality-levels-warning-help-box");
            UpdateInvalidQualityLevelsWarning();

            var qualityLevelsList = root.Q<ListView>("quality-levels");
            root.TrackPropertyValue(m_QualityLevels, sp => {
                UpdateInvalidQualityLevelsWarning();
                qualityLevelsList.RefreshItems();
            });
            root.TrackPropertyValue(m_DefaultQualityLevel, sp => qualityLevelsList.RefreshItems());

            qualityLevelsList.makeItem = () => new QualityLevelItem(SetDefaultQualityLevelContextMenu());
            qualityLevelsList.bindItem = (element, index) =>
            {
                if (m_QualityLevels.arraySize == 0 || index >= m_QualityLevels.arraySize)
                    return;

                var item = element as QualityLevelItem;
                var qualityLevelName = m_QualityLevels.GetArrayElementAtIndex(index).stringValue;

                item.text = qualityLevelName;
                if (IsDefaultQualityLevel(qualityLevelName))
                    item.SetDefaultIndicator(true);
                else
                    item.SetDefaultIndicator(false);
            };
            qualityLevelsList.onAdd = list =>
            {
                var menu = new GenericMenu();
                var allQualityLevels = QualitySettings.names;
                foreach (var level in allQualityLevels)
                {
                    if (!IsQualityLevelAdded(level))
                        menu.AddItem(new GUIContent(level), false, () => AddQualityLevel(level));
                }

                menu.AddSeparator(string.Empty);
                menu.AddItem(k_qualitySettingsWindow, false, () => SettingsService.OpenProjectSettings(k_QualitySettingsWindow));
                menu.ShowAsContext();
            };
            qualityLevelsList.onRemove = list =>
            {
                RemoveQualityLevel();
            };

            void AddQualityLevel(string newLevel)
            {
                m_QualityLevels.InsertArrayElementAtIndex(m_QualityLevels.arraySize);
                m_QualityLevels.GetArrayElementAtIndex(m_QualityLevels.arraySize - 1).stringValue = newLevel;

                if (m_QualityLevels.arraySize == 1)
                    m_DefaultQualityLevel.stringValue = newLevel;

                serializedObject.ApplyModifiedProperties();
            }

            void RemoveQualityLevel()
            {
                if (m_QualityLevels.arraySize == 0)
                    return;

                var selectedIndex = qualityLevelsList.selectedIndex;

                if (selectedIndex < 0 || selectedIndex >= m_QualityLevels.arraySize)
                    selectedIndex = m_QualityLevels.arraySize - 1;

                if (selectedIndex >= 0)
                {
                    var deletedQualityLevel = m_QualityLevels.GetArrayElementAtIndex(selectedIndex).stringValue;
                    m_QualityLevels.DeleteArrayElementAtIndex(selectedIndex);

                    if (IsDefaultQualityLevel(deletedQualityLevel))
                        m_DefaultQualityLevel.stringValue = m_QualityLevels.arraySize > 0 ?
                            m_QualityLevels.GetArrayElementAtIndex(0).stringValue : string.Empty;

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        bool IsDefaultQualityLevel(string qualityLevel) => m_DefaultQualityLevel.stringValue == qualityLevel;

        bool IsQualityLevelAdded(string level)
        {
            for (var i = 0; i < m_QualityLevels.arraySize; i++)
            {
                if (m_QualityLevels.GetArrayElementAtIndex(i).stringValue == level)
                    return true;
            }

            return false;
        }

        void UpdateInvalidQualityLevelsWarning()
        {
            if (m_QualityLevels.arraySize == 0)
            {
                warning.text = k_EmptyQualitySettingsWarning;
                warning.style.display = DisplayStyle.Flex;
            }
            else if (HasInvalidQualityLevels())
            {
                warning.text = k_InvalidQualityLevelWarning;
                warning.style.display = DisplayStyle.Flex;
            }
            else
                warning.style.display = DisplayStyle.None;
        }

        bool HasInvalidQualityLevels()
        {
            var allQualityLevels = QualitySettings.names;
            for (var i = 0; i < m_QualityLevels.arraySize; i++)
            {
                var level = m_QualityLevels.GetArrayElementAtIndex(i).stringValue;
                if (Array.IndexOf(allQualityLevels, level) == -1)
                    return true;
            }

            return false;
        }

        ContextualMenuManipulator SetDefaultQualityLevelContextMenu()
        {
            var menu = new ContextualMenuManipulator(evt =>
            {
                var selectedQualityLevel = evt.target as QualityLevelItem;
                if (selectedQualityLevel == null)
                    return;

                evt.menu.AppendAction(k_SetDefaultQualityLevelMenuText, action =>
                {
                    m_DefaultQualityLevel.stringValue = selectedQualityLevel.text;
                    serializedObject.ApplyModifiedProperties();
                });
            });

            return menu;
        }

        public bool IsDataEqualToGlobalQualitySettings(BuildProfile profile)
        {
            var buildTarget = profile.buildTarget;
            var buildTargetGroupString = BuildPipeline.GetBuildTargetGroup(buildTarget).ToString();

            var globalQualityLevels = QualitySettings.GetActiveQualityLevelsForPlatform(buildTargetGroupString);
            if (m_QualityLevels.arraySize != globalQualityLevels.Length)
                return false;

            var allQualityLevels = QualitySettings.names;
            for (var i = 0; i < m_QualityLevels.arraySize; i++)
            {
                var levelIndex = globalQualityLevels[i];
                if (m_QualityLevels.GetArrayElementAtIndex(i).stringValue != allQualityLevels[levelIndex])
                    return false;
            }

            var globalDefaultQualityLevelIndex = QualitySettings.GetDefaultQualityForPlatform(buildTargetGroupString);
            if (globalDefaultQualityLevelIndex != -1)
            {
                var globalDefaultQualityLevel = allQualityLevels[globalDefaultQualityLevelIndex];
                if (m_DefaultQualityLevel.stringValue != globalDefaultQualityLevel)
                    return false;
            }

            return true;
        }

        class QualityLevelItem : VisualElement
        {
            const string k_Uxml = "BuildProfile/UXML/BuildProfileQualitySettingsListElement.uxml";
            const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";
            static readonly string k_DefaultIndicatorText = L10n.Tr("Default");
            protected readonly Label m_Text;
            protected readonly Label m_DefaultIndicator;

            internal string text
            {
                get => m_Text.text;
                set => m_Text.text = value;
            }

            internal QualityLevelItem(IManipulator manipulator)
            {
                var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
                var stylesheet = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
                styleSheets.Add(stylesheet);
                uxml.CloneTree(this);

                m_Text = this.Q<Label>("quality-level-name");
                m_DefaultIndicator = this.Q<Label>("default-quality-level-indicator");
                m_DefaultIndicator.text = k_DefaultIndicatorText;
                SetDefaultIndicator(false);

                this.AddManipulator(manipulator);
            }

            internal void SetDefaultIndicator(bool isDefault)
            {
                if (isDefault)
                    m_DefaultIndicator.style.display = DisplayStyle.Flex;
                else
                    m_DefaultIndicator.style.display = DisplayStyle.None;
            }
        }
    }
}
