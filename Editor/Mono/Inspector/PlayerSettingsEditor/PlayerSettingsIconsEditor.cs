// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditor.PlatformSupport;
using UnityEngine;

namespace UnityEditor
{
    internal class PlayerSettingsIconsEditor
    {
        class SettingsContent
        {
            public static readonly GUIContent iconTitle = EditorGUIUtility.TrTextContent("Icon");
            public static readonly GUIContent defaultIcon = EditorGUIUtility.TrTextContent("Default Icon");
            public static readonly GUIContent UIPrerenderedIcon = EditorGUIUtility.TrTextContent("Prerendered Icon");
            public static string undoChangedIconString { get { return LocalizationDatabase.GetLocalizedString("Changed Icon"); } }
        }

        // Icon layout constants
        const int kSlotSize = 64;
        const int kMaxPreviewSize = 96;
        const int kIconSpacing = 6;

        PlayerSettingsEditor m_Owner;

        int m_SelectedPlatform = 0;

        BuildPlatform[] m_ValidPlatforms;

        // Serialized icons
        SerializedProperty m_PlatformIcons;
        SerializedProperty m_LegacyPlatformIcons;
        SerializedProperty m_UIPrerenderedIcon;


        // Deserialized icons (all platforms)
        BuildTargetIcons[] m_AllIcons;
        // Deserialized legacy icons (all platforms)
        LegacyBuildTargetIcons[] m_AllLegacyIcons;

        // Required icons for platform. Provided by platform extension
        Dictionary<PlatformIconKind, PlatformIcon[]> m_RequiredIcons;

        public PlayerSettingsIconsEditor(PlayerSettingsEditor owner)
        {
            m_Owner = owner;
        }

        public void OnEnable()
        {
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms(true).ToArray();

            m_PlatformIcons       = m_Owner.FindPropertyAssert("m_BuildTargetPlatformIcons");
            m_LegacyPlatformIcons = m_Owner.FindPropertyAssert("m_BuildTargetIcons");
            m_UIPrerenderedIcon   = m_Owner.FindPropertyAssert("uIPrerenderedIcon");

            DeserializeIcons();
            DeserializeLegacyIcons();
        }

        private void DeserializeIcons()
        {
            var allIconsList = new List<BuildTargetIcons>();
            for (int i = 0; i < m_PlatformIcons.arraySize; i++)
            {
                var platform = m_PlatformIcons.GetArrayElementAtIndex(i);
                var icons = platform.FindPropertyRelative("m_Icons");
                var platformName = platform.FindPropertyRelative("m_BuildTarget").stringValue;
                if (platformName == null || icons.arraySize <= 0)
                    continue;

                var platformIcons = new BuildTargetIcons
                {
                    BuildTarget = platformName,
                    Icons = new PlatformIconStruct[icons.arraySize]
                };

                for (int j = 0; j < icons.arraySize; j++)
                {
                    var platformIconsEntry = icons.GetArrayElementAtIndex(j);
                    var icon = new PlatformIconStruct()
                    {
                        Width = platformIconsEntry.FindPropertyRelative("m_Width").intValue,
                        Height = platformIconsEntry.FindPropertyRelative("m_Height").intValue,
                        Kind = platformIconsEntry.FindPropertyRelative("m_Kind").intValue,
                        SubKind = platformIconsEntry.FindPropertyRelative("m_SubKind").stringValue,
                    };
                    var texturesEntry = platformIconsEntry.FindPropertyRelative("m_Textures");
                    icon.Textures = new Texture2D[texturesEntry.arraySize];
                    for (int k = 0; k < texturesEntry.arraySize; k++)
                    {
                        icon.Textures[k] = (Texture2D)texturesEntry.GetArrayElementAtIndex(k).objectReferenceValue;
                    }

                    platformIcons.Icons[j] = icon;
                }
                allIconsList.Add(platformIcons);
            }

            m_AllIcons = allIconsList.ToArray();
        }

        private void DeserializeLegacyIcons()
        {
            var allIconsList = new List<LegacyBuildTargetIcons>();
            for (int i = 0; i < m_LegacyPlatformIcons.arraySize; i++)
            {
                var platform = m_LegacyPlatformIcons.GetArrayElementAtIndex(i);
                var icons = platform.FindPropertyRelative("m_Icons");
                var platformName = platform.FindPropertyRelative("m_BuildTarget").stringValue;
                if (platformName == null || icons.arraySize <= 0)
                    continue;

                var platformIcons = new LegacyBuildTargetIcons
                {
                    BuildTarget = platformName,
                    Icons = new LegacyPlatformIcon[icons.arraySize]
                };

                for (int j = 0; j < icons.arraySize; j++)
                {
                    var platformIconsEntry = icons.GetArrayElementAtIndex(j);
                    var icon = new LegacyPlatformIcon()
                    {
                        Width = platformIconsEntry.FindPropertyRelative("m_Width").intValue,
                        Height = platformIconsEntry.FindPropertyRelative("m_Height").intValue,
                        Kind = (IconKind)platformIconsEntry.FindPropertyRelative("m_Kind").intValue,
                    };
                    var texture = platformIconsEntry.FindPropertyRelative("m_Icon");
                    icon.Icon = (Texture2D)texture.objectReferenceValue;
                    platformIcons.Icons[j] = icon;
                }
                allIconsList.Add(platformIcons);
            }

            m_AllLegacyIcons = allIconsList.ToArray();
        }

        private void SerializeIcons()
        {
            m_PlatformIcons.ClearArray();
            for (int i = 0; i < m_AllIcons.Length; i++)
            {
                var platformIcons = m_AllIcons[i];
                var platformName = platformIcons.BuildTarget;

                m_PlatformIcons.InsertArrayElementAtIndex(i);
                var serializedPlatform = m_PlatformIcons.GetArrayElementAtIndex(i);

                if (platformName == null || platformIcons.Icons == null)
                    return;

                serializedPlatform.FindPropertyRelative("m_BuildTarget").stringValue = platformName;
                var iconsMember = serializedPlatform.FindPropertyRelative("m_Icons");
                iconsMember.ClearArray(); // Even after clearing parent array, this is not cleared
                for (int k = 0; k < platformIcons.Icons.Length; k++)
                {
                    var icon = platformIcons.Icons[k];
                    iconsMember.InsertArrayElementAtIndex(k);
                    var platformIconsEntry = iconsMember.GetArrayElementAtIndex(k);
                    platformIconsEntry.FindPropertyRelative("m_Width").intValue = icon.Width;
                    platformIconsEntry.FindPropertyRelative("m_Height").intValue = icon.Height;
                    platformIconsEntry.FindPropertyRelative("m_Kind").intValue = icon.Kind;
                    platformIconsEntry.FindPropertyRelative("m_SubKind").stringValue = icon.SubKind;

                    var texturesMember = platformIconsEntry.FindPropertyRelative("m_Textures");
                    texturesMember.ClearArray(); // Even after clearing parent array, this is not cleared
                    for (int l = 0; l < icon.Textures.Length; l++)
                    {
                        texturesMember.InsertArrayElementAtIndex(l);
                        texturesMember.GetArrayElementAtIndex(l).objectReferenceValue = icon.Textures[l];
                    }
                }
            }
        }

        private void SerializeLegacyIcons()
        {
            m_LegacyPlatformIcons.ClearArray();
            for (int i = 0; i < m_AllLegacyIcons.Length; i++)
            {
                var platformIcons = m_AllLegacyIcons[i];
                var platformName = platformIcons.BuildTarget;

                m_LegacyPlatformIcons.InsertArrayElementAtIndex(i);
                var serializedPlatform = m_LegacyPlatformIcons.GetArrayElementAtIndex(i);

                if (platformName == null || platformIcons.Icons == null)
                    return;

                serializedPlatform.FindPropertyRelative("m_BuildTarget").stringValue = platformName;
                var iconsMember = serializedPlatform.FindPropertyRelative("m_Icons");
                iconsMember.ClearArray(); // Even after clearing parent array, this is not cleared
                for (int k = 0; k < platformIcons.Icons.Length; k++)
                {
                    var icon = platformIcons.Icons[k];
                    iconsMember.InsertArrayElementAtIndex(k);
                    var platformIconsEntry = iconsMember.GetArrayElementAtIndex(k);
                    platformIconsEntry.FindPropertyRelative("m_Width").intValue = icon.Width;
                    platformIconsEntry.FindPropertyRelative("m_Height").intValue = icon.Height;
                    platformIconsEntry.FindPropertyRelative("m_Kind").intValue = (int)icon.Kind;

                    var iconTexture = platformIconsEntry.FindPropertyRelative("m_Icon");
                    iconTexture.objectReferenceValue = icon.Icon;
                }
            }
        }

        private void SetLegacyPlatformIcons(string platform, Texture2D[] icons, IconKind kind, ref LegacyBuildTargetIcons[] allIcons)
        {
            allIcons = PlayerSettings.SetPlatformIconsForTargetIcons(platform, icons, kind, allIcons);
            SerializeLegacyIcons();
        }

        static void ImportLegacyIcons(string platform, PlatformIconKind kind, PlatformIcon[] platformIcons, LegacyBuildTargetIcons[] allLegacyIcons)
        {
            if (!Enum.IsDefined(typeof(IconKind), kind.kind))
                return;

            var iconKind = (IconKind)kind.kind;

            var legacyIcons = PlayerSettings.GetPlatformIconsForTargetIcons(platform, iconKind, allLegacyIcons);
            var legacyIconWidths = PlayerSettings.GetIconWidthsForPlatform(platform, iconKind);
            var legacyIconHeights  = PlayerSettings.GetIconHeightsForPlatform(platform, iconKind);

            for (var i = 0; i < legacyIcons.Length; i++)
            {
                var selectedIcons = new List<PlatformIcon>();
                foreach (var icon in platformIcons)
                {
                    if (icon.width == legacyIconWidths[i] && icon.height == legacyIconHeights[i])
                    {
                        selectedIcons.Add(icon);
                    }
                }
                foreach (var selectedIcon in selectedIcons)
                    selectedIcon.SetTextures(legacyIcons[i]);
            }
        }

        private void SetPreviewTextures(PlatformIcon platformIcon)
        {
            Texture2D[] previewTextures = new Texture2D[platformIcon.maxLayerCount];

            for (int i = 0; i < platformIcon.maxLayerCount; i++)
            {
                previewTextures[i] = PlayerSettings.GetPlatformIconAtSizeForTargetIcons(platformIcon.kind.platform, platformIcon.width, platformIcon.height, m_AllIcons, platformIcon.kind.kind, platformIcon.iconSubKind, i);
            }

            platformIcon.SetPreviewTextures(previewTextures);
        }

        internal PlatformIcon[] GetPlatformIcons(BuildTargetGroup platform, PlatformIconKind kind, ref BuildTargetIcons[] allIcons)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(platform);
            if (!BuildTargetDiscovery.TryGetBuildTarget(BuildPipeline.GetBuildTargetByName(namedBuildTarget.TargetName), out var iBuildTarget))
                return Array.Empty<PlatformIcon>();
            var requiredIcons = iBuildTarget.IconPlatformProperties?.GetRequiredPlatformIcons();
            if (requiredIcons == null)
                return Array.Empty<PlatformIcon>();

            var serializedIcons = PlayerSettings.GetPlatformIconsFromTargetIcons(namedBuildTarget.TargetName, kind.kind, allIcons);
            if (m_RequiredIcons == null)
                m_RequiredIcons = new Dictionary<PlatformIconKind, PlatformIcon[]>();
            if (!m_RequiredIcons.ContainsKey(kind))
            {
                foreach (var requiredIcon in requiredIcons)
                {
                    if (!m_RequiredIcons.ContainsKey(requiredIcon.Key))
                        m_RequiredIcons.Add(requiredIcon.Key, requiredIcon.Value);
                }
            }

            var icons = PlatformIcon.GetRequiredPlatformIconsByType(kind, m_RequiredIcons);
            if (serializedIcons.Length <= 0)
            {
                // Map legacy icons to required icons
                ImportLegacyIcons(namedBuildTarget.TargetName, kind, icons, m_AllLegacyIcons);
                // Serialize required icons
                SetPlatformIcons(platform, kind, icons, ref allIcons);

                foreach (var icon in icons)
                    if (icon.IsEmpty())
                        icon.SetTextures(null);
            }
            else
            {
                // Map serialized icons to required icons
                icons = PlayerSettings.GetPlatformIconsFromStruct(icons, kind, serializedIcons.ToArray());
            }

            return icons;
        }

        void SetIconsForPlatform(BuildTargetGroup targetGroup, PlatformIcon[] icons, PlatformIconKind kind, ref BuildTargetIcons[] allIcons)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            if (!BuildTargetDiscovery.TryGetBuildTarget(BuildPipeline.GetBuildTargetByName(namedBuildTarget.TargetName), out var iBuildTarget))
                return;

            var requiredIcons = iBuildTarget.IconPlatformProperties?.GetRequiredPlatformIcons();
            if (requiredIcons == null)
                return;

            if (m_RequiredIcons == null)
                m_RequiredIcons = new Dictionary<PlatformIconKind, PlatformIcon[]>();
            if (!m_RequiredIcons.ContainsKey(kind))
            {
                foreach (var requiredIcon in requiredIcons)
                {
                    if (!m_RequiredIcons.ContainsKey(requiredIcon.Key))
                        m_RequiredIcons.Add(requiredIcon.Key, requiredIcon.Value);
                }
            }

            var requiredIconCount = PlatformIcon.GetRequiredPlatformIconsByType(kind, m_RequiredIcons).Length;

            PlatformIconStruct[] iconStructs;
            if (icons == null)
                iconStructs = new PlatformIconStruct[0];
            else if (requiredIconCount != icons.Length)
            {
                throw new InvalidOperationException($"Attempting to set an incorrect number of icons for {namedBuildTarget} {kind} kind, it requires {requiredIconCount} icons but trying to assign {icons.Length}.");
            }
            else
            {
                iconStructs = icons.Select(
                    i => i.GetPlatformIconStruct()
                    ).ToArray<PlatformIconStruct>();
            }

            allIcons = PlayerSettings.SetIconsForPlatformForTargetIcons(namedBuildTarget.TargetName, iconStructs, kind.kind, allIcons);
        }

        void SetPlatformIcons(BuildTargetGroup targetGroup, PlatformIconKind kind, PlatformIcon[] icons, ref BuildTargetIcons[] allIcons)
        {
            SetIconsForPlatform(targetGroup, icons, kind, ref allIcons);
            SerializeIcons();
        }

        public void LegacyIconSectionGUI()
        {
            // Both default icon and Legacy icons are serialized to the same map
            // That's why m_LegacyPlatformIcons can be excluded in two places (other place in IconSectionGUI())
            using (var vertical = new EditorGUILayout.VerticalScope())
            using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_LegacyPlatformIcons))
            {
                // Get icons and icon sizes for selected platform (or default)
                EditorGUI.BeginChangeCheck();
                string platformName = "";
                Texture2D[] icons = PlayerSettings.GetPlatformIconsForTargetIcons(platformName, IconKind.Any, m_AllLegacyIcons);
                int[] widths = PlayerSettings.GetIconWidthsForPlatform(platformName, IconKind.Any);

                // Ensure the default icon list is always populated correctly
                if (icons.Length != widths.Length)
                {
                    icons = new Texture2D[widths.Length];
                }

                icons[0] = (Texture2D)EditorGUILayout.ObjectField(SettingsContent.defaultIcon, icons[0], typeof(Texture2D), false);
                // Save changes
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(m_Owner.targets, SettingsContent.undoChangedIconString);
                    SetLegacyPlatformIcons(platformName, icons, IconKind.Any, ref m_AllLegacyIcons);
                }
            }
        }

        public void SerializedObjectUpdated()
        {
            DeserializeIcons();
            DeserializeLegacyIcons();
        }

        public void IconSectionGUI(NamedBuildTarget namedBuildTarget, ISettingEditorExtension settingsExtension, int platformID, int sectionIndex)
        {
            m_SelectedPlatform = platformID;
            if (!m_Owner.BeginSettingsBox(sectionIndex, SettingsContent.iconTitle))
            {
                m_Owner.EndSettingsBox();
                return;
            }

            var platformUsesStandardIcons = true;
            if (settingsExtension != null)
                platformUsesStandardIcons = settingsExtension.UsesStandardIcons();

            if (platformUsesStandardIcons)
            {
                var selectedDefault = (m_SelectedPlatform < 0);
                // Set default platform variables
                BuildPlatform platform = null;
                var platformName = "";

                // Override if a platform is selected
                if (!selectedDefault)
                {
                    platform = m_ValidPlatforms[m_SelectedPlatform];
                    platformName = platform.name;
                }

                var iconUISettings = IconSettings.StandardIcons;
                if (BuildTargetDiscovery.TryGetBuildTarget(platform.defaultTarget, out IBuildTarget iBuildTarget))
                    iconUISettings = iBuildTarget.IconPlatformProperties?.IconUISettings ?? IconSettings.StandardIcons;

                if (iconUISettings == IconSettings.None)
                {
                    PlayerSettingsEditor.ShowNoSettings();
                    EditorGUILayout.Space();
                }
                else if (iconUISettings == IconSettings.StandardIcons)
                {
                    // Both default icon and Legacy icons are serialized to the same map
                    // That's why m_LegacyPlatformIcons can be excluded in two places (other place in CommonSettings())
                    using (var vertical = new EditorGUILayout.VerticalScope())
                    using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_LegacyPlatformIcons))
                    {
                        // Get icons and icon sizes for selected platform (or default)
                        var icons = PlayerSettings.GetPlatformIconsForTargetIcons(platformName, IconKind.Any, m_AllLegacyIcons);
                        var widths = PlayerSettings.GetIconWidthsForPlatform(platformName, IconKind.Any);
                        var heights = PlayerSettings.GetIconHeightsForPlatform(platformName, IconKind.Any);
                        var kinds = PlayerSettings.GetIconKindsForPlatform(platformName);

                        var overrideIcons = true;

                        if (!selectedDefault)
                        {
                            // If the list of icons for this platform is not empty (and has the correct size),
                            // consider the icon overridden for this platform
                            EditorGUI.BeginChangeCheck();
                            overrideIcons = (icons.Length == widths.Length);
                            overrideIcons = GUILayout.Toggle(overrideIcons, IsInBuildProfileEditor() ? L10n.Tr("Override") : string.Format(L10n.Tr("Override for {0}"), platform.title.text));
                            EditorGUI.BeginDisabled(!overrideIcons);
                            var changed = EditorGUI.EndChangeCheck();
                            if (changed || (!overrideIcons && icons.Length > 0))
                            {
                                // Set the list of icons to correct length if overridden, otherwise to an empty list
                                if (overrideIcons)
                                    icons = new Texture2D[widths.Length];
                                else
                                    icons = new Texture2D[0];

                                if (changed)
                                    SetLegacyPlatformIcons(platformName, icons, IconKind.Any, ref m_AllLegacyIcons);
                            }
                        }

                        // Show the icons for this platform (or default)
                        EditorGUI.BeginChangeCheck();
                        for (int i = 0; i < widths.Length; i++)
                        {
                            var previewWidth = Mathf.Min(kMaxPreviewSize, widths[i]);
                            var previewHeight = (int)((float)heights[i] * previewWidth / widths[i]);   // take into account the aspect ratio
                            var rect = GUILayoutUtility.GetRect(kSlotSize, Mathf.Max(kSlotSize, previewHeight) + kIconSpacing);
                            var width = Mathf.Min(rect.width, EditorGUIUtility.labelWidth + 4 + kSlotSize + kIconSpacing + kMaxPreviewSize);

                            // Label
                            var label = widths[i] + "x" + heights[i];
                            GUI.Label(new Rect(rect.x, rect.y, width - kMaxPreviewSize - kSlotSize - 2 * kIconSpacing, 20), label);

                            // Texture slot
                            if (overrideIcons)
                            {
                                var slotWidth = kSlotSize;
                                var slotHeight = (int)((float)heights[i] / widths[i] * kSlotSize);   // take into account the aspect ratio
                                icons[i] = (Texture2D)EditorGUI.ObjectField(
                                    new Rect(rect.x + width - kMaxPreviewSize - kSlotSize - kIconSpacing, rect.y, slotWidth, slotHeight),
                                    icons[i],
                                    typeof(Texture2D),
                                    false);
                            }

                            // Preview
                            var previewRect = new Rect(rect.x + width - kMaxPreviewSize, rect.y, previewWidth, previewHeight);
                            var closestIcon = PlayerSettings.GetPlatformIconForSizeForTargetIcons(platformName, widths[i], heights[i], kinds[i], m_AllLegacyIcons);
                            if (closestIcon != null)
                                GUI.DrawTexture(previewRect, closestIcon);
                            else
                                GUI.Box(previewRect, "");
                        }

                        // Save changes
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(m_Owner.targets, SettingsContent.undoChangedIconString);
                            SetLegacyPlatformIcons(platformName, icons, IconKind.Any, ref m_AllLegacyIcons);
                        }

                        EditorGUI.EndDisabled();
                    }
                }
            }

            if (settingsExtension != null)
                settingsExtension.IconSectionGUI();

            m_Owner.EndSettingsBox();
        }

        internal void ShowPlatformIconsByKind(PlatformIconFieldGroup iconFieldGroup, bool foldByKind, bool foldBySubkind)
        {
            // All icons that are displayed here are serialized into a single map
            // So in the preset we can only exclude/include all icons
            using (var vertical = new EditorGUILayout.VerticalScope())
            using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, m_PlatformIcons))
            {
                int labelHeight = 20;

                foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.FromBuildTargetGroup(iconFieldGroup.targetGroup)))
                {
                    iconFieldGroup.SetPlatformIcons(GetPlatformIcons(iconFieldGroup.targetGroup, kind, ref m_AllIcons), kind);
                }

                foreach (var kindGroup in iconFieldGroup.m_IconsFields)
                {
                    EditorGUI.BeginChangeCheck();

                    var key = kindGroup.Key;

                    if (foldByKind)
                    {
                        GUIContent kindName = new GUIContent(
                            string.Format("{0} icons ({1}/{2})", key.m_Label, kindGroup.Key.m_SetIconSlots, kindGroup.Key.m_IconSlotCount),
                            key.m_KindDescription
                        );

                        Rect rectKindLabel = GUILayoutUtility.GetRect(kSlotSize, labelHeight);
                        rectKindLabel.x += 2;
                        key.m_State = EditorGUI.Foldout(rectKindLabel, key.m_State, kindName, true, EditorStyles.foldout);
                    }
                    else
                        key.m_State = true;

                    if (key.m_State)
                    {
                        kindGroup.Key.m_SetIconSlots = 0;
                        foreach (var subKindGroup in kindGroup.Value)
                        {
                            subKindGroup.Key.m_SetIconSlots =
                                PlayerSettings.GetNonEmptyPlatformIconCount(subKindGroup.Value.Select(x => x.platformIcon)
                                    .ToArray());
                            kindGroup.Key.m_SetIconSlots += subKindGroup.Key.m_SetIconSlots;

                            if (foldBySubkind)
                            {
                                string subKindName = string.Format("{0} icons ({1}/{2})", subKindGroup.Key.m_Label, subKindGroup.Key.m_SetIconSlots, subKindGroup.Value.Length);
                                Rect rectSubKindLabel = GUILayoutUtility.GetRect(kSlotSize, labelHeight);
                                rectSubKindLabel.x += 8;

                                subKindGroup.Key.m_State = EditorGUI.Foldout(rectSubKindLabel, subKindGroup.Key.m_State, subKindName, true, EditorStyles.foldout);
                            }
                            else
                                subKindGroup.Key.m_State = true;

                            if (subKindGroup.Key.m_State || !foldBySubkind)
                            {
                                foreach (var iconField in subKindGroup.Value)
                                {
                                    SetPreviewTextures(iconField.platformIcon);
                                    iconField.DrawAt();
                                }
                            }
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                        SetPlatformIcons(iconFieldGroup.targetGroup, key.m_Kind, iconFieldGroup.m_PlatformIconsByKind[key.m_Kind], ref m_AllIcons);
                }
            }
        }

        /// <summary>
        /// Check if this class belongs to the build profile editor
        /// </summary>
        bool IsInBuildProfileEditor()
        {
            return m_Owner.IsBuildProfile();
        }
    }
}
