// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.Build;
using UnityEditor.U2D.Common;
using UnityEditor.U2D.Interface;
using UnityEditorInternal;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteAtlas))]
    [CanEditMultipleObjects]
    internal class SpriteAtlasInspector : Editor
    {
        class SpriteAtlasInspectorPlatformSettingView : TexturePlatformSettingsView
        {
            private bool m_ShowMaxSizeOption;

            public SpriteAtlasInspectorPlatformSettingView(bool showMaxSizeOption)
            {
                m_ShowMaxSizeOption = showMaxSizeOption;
            }

            public override int DrawMaxSize(int defaultValue, bool isMixedValue, out bool changed)
            {
                if (m_ShowMaxSizeOption)
                    return base.DrawMaxSize(defaultValue, isMixedValue, out changed);
                else
                    changed = false;
                return defaultValue;
            }
        }

        class Styles
        {
            public readonly GUIStyle dropzoneStyle = new GUIStyle("BoldLabel");
            public readonly GUIStyle preDropDown = "preDropDown";
            public readonly GUIStyle previewButton = "preButton";
            public readonly GUIStyle previewSlider = "preSlider";
            public readonly GUIStyle previewSliderThumb = "preSliderThumb";
            public readonly GUIStyle previewLabel = new GUIStyle("preLabel");

            public readonly GUIContent textureSettingLabel = EditorGUIUtility.TextContent("Texture");
            public readonly GUIContent variantSettingLabel = EditorGUIUtility.TextContent("Variant");
            public readonly GUIContent packingParametersLabel = EditorGUIUtility.TextContent("Packing");
            public readonly GUIContent atlasTypeLabel = EditorGUIUtility.TextContent("Type");
            public readonly GUIContent defaultPlatformLabel = EditorGUIUtility.TextContent("Default");
            public readonly GUIContent masterAtlasLabel = EditorGUIUtility.TextContent("Master Atlas|Assigning another Sprite Atlas asset will make this atlas a variant of it.");
            public readonly GUIContent bindAsDefaultLabel = EditorGUIUtility.TextContent("Include in Build|Packed textures will be included in the build by default.");
            public readonly GUIContent enableRotationLabel = EditorGUIUtility.TextContent("Allow Rotation|Try rotating the sprite to fit better during packing.");
            public readonly GUIContent enableTightPackingLabel = EditorGUIUtility.TextContent("Tight Packing|Use the mesh outline to fit instead of the whole texture rect during packing.");
            public readonly GUIContent paddingLabel = EditorGUIUtility.TextContent("Padding|The amount of extra padding between packed sprites.");

            public readonly GUIContent generateMipMapLabel = EditorGUIUtility.TextContent("Generate Mip Maps");
            public readonly GUIContent sRGBLabel = EditorGUIUtility.TextContent("sRGB|Texture content is stored in gamma space.");
            public readonly GUIContent readWrite = EditorGUIUtility.TextContent("Read/Write Enabled|Enable to be able to access the raw pixel data from code.");
            public readonly GUIContent variantMultiplierLabel = EditorGUIUtility.TextContent("Scale|Down scale ratio.");
            public readonly GUIContent copyMasterButton = EditorGUIUtility.TextContent("Copy Master's Settings|Copy all master's settings into this variant.");
            public readonly GUIContent packButton = EditorGUIUtility.TextContent("Pack Preview|Pack this atlas.");
            public readonly GUIContent disabledPackLabel = EditorGUIUtility.TextContent("Sprite Atlas packing is disabled. Enable it in Edit > Project Settings > Editor.");
            public readonly GUIContent packableListLabel = EditorGUIUtility.TextContent("Objects for Packing|Only accept Folder, Sprite Sheet(Texture) and Sprite.");

            public readonly GUIContent notPowerOfTwoWarning = EditorGUIUtility.TextContent("This scale will produce a Sprite Atlas variant with a packed texture that is NPOT (non - power of two). This may cause visual artifacts in certain compression/texture formats.");

            public readonly GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public readonly GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public readonly GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public readonly GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");

            public readonly int packableElementHash = "PackableElement".GetHashCode();
            public readonly int packableSelectorHash = "PackableSelector".GetHashCode();

            public readonly int[] atlasTypeValues = { 0, 1 };
            public readonly GUIContent[] atlasTypeOptions =
            {
                EditorGUIUtility.TextContent("Master"),
                EditorGUIUtility.TextContent("Variant"),
            };

            public readonly int[] paddingValues = { 2, 4, 8 };
            public readonly GUIContent[] paddingOptions;

            public Styles()
            {
                dropzoneStyle.alignment = TextAnchor.MiddleCenter;
                dropzoneStyle.border = new RectOffset(10, 10, 10, 10);

                paddingOptions = new GUIContent[paddingValues.Length];
                for (var i = 0; i < paddingValues.Length; ++i)
                    paddingOptions[i] = EditorGUIUtility.TextContent(paddingValues[i].ToString());
            }
        }

        private static Styles s_Styles;

        private readonly string kDefaultPlatformName = "default";

        private enum AtlasType { Undefined = -1, Master = 0, Variant = 1 }

        private SerializedProperty m_MaxTextureSize;
        private SerializedProperty m_TextureCompression;
        private SerializedProperty m_UseCrunchedCompression;
        private SerializedProperty m_CompressionQuality;
        private SerializedProperty m_FilterMode;
        private SerializedProperty m_AnisoLevel;
        private SerializedProperty m_GenerateMipMaps;
        private SerializedProperty m_Readable;
        private SerializedProperty m_UseSRGB;
        private SerializedProperty m_EnableTightPacking;
        private SerializedProperty m_EnableRotation;
        private SerializedProperty m_Padding;
        private SerializedProperty m_BindAsDefault;
        private SerializedProperty m_Packables;

        private SerializedProperty m_MasterAtlas;
        private SerializedProperty m_VariantMultiplier;

        private string m_Hash;
        private int m_PreviewPage = 0;
        private int m_TotalPages = 0;
        private int[] m_OptionValues = null;
        private string[] m_OptionDisplays = null;
        private Texture2D[] m_PreviewTextures = null;
        private Texture2D[] m_PreviewAlphaTextures = null;

        private bool m_PackableListExpanded = true;
        private ReorderableList m_PackableList;

        private float m_MipLevel = 0;
        private bool m_ShowAlpha;

        private List<BuildPlatform> m_ValidPlatforms;
        private Dictionary<string, List<TextureImporterPlatformSettings>> m_TempPlatformSettings;

        private ITexturePlatformSettingsView m_TexturePlatformSettingsView;
        private ITexturePlatformSettingsFormatHelper m_TexturePlatformSettingTextureHelper;
        private ITexturePlatformSettingsController m_TexturePlatformSettingsController;

        private SpriteAtlas spriteAtlas { get { return target as SpriteAtlas; } }

        static bool IsPackable(Object o)
        {
            return o != null && (o.GetType() == typeof(Sprite) || o.GetType() == typeof(Texture2D) || (o.GetType() == typeof(DefaultAsset) && ProjectWindowUtil.IsFolder(o.GetInstanceID())));
        }

        static Object ValidateObjectForPackableFieldAssignment(Object[] references, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            // We only validate and care about the first one as this is a object field assignment.
            if (references.Length > 0 && IsPackable(references[0]))
                return references[0];
            return null;
        }

        bool AllTargetsAreVariant()
        {
            foreach (SpriteAtlas sa in targets)
            {
                if (!sa.isVariant)
                    return false;
            }
            return true;
        }

        bool AllTargetsAreMaster()
        {
            foreach (SpriteAtlas sa in targets)
            {
                if (sa.isVariant)
                    return false;
            }
            return true;
        }

        void OnEnable()
        {
            m_MaxTextureSize = serializedObject.FindProperty("m_EditorData.textureSettings.maxTextureSize");
            m_TextureCompression = serializedObject.FindProperty("m_EditorData.textureSettings.textureCompression");
            m_UseCrunchedCompression = serializedObject.FindProperty("m_EditorData.textureSettings.crunchedCompression");
            m_CompressionQuality = serializedObject.FindProperty("m_EditorData.textureSettings.compressionQuality");
            m_FilterMode = serializedObject.FindProperty("m_EditorData.textureSettings.filterMode");
            m_AnisoLevel = serializedObject.FindProperty("m_EditorData.textureSettings.anisoLevel");
            m_GenerateMipMaps = serializedObject.FindProperty("m_EditorData.textureSettings.generateMipMaps");
            m_Readable = serializedObject.FindProperty("m_EditorData.textureSettings.readable");
            m_UseSRGB = serializedObject.FindProperty("m_EditorData.textureSettings.sRGB");

            m_EnableTightPacking = serializedObject.FindProperty("m_EditorData.packingParameters.enableTightPacking");
            m_EnableRotation = serializedObject.FindProperty("m_EditorData.packingParameters.enableRotation");
            m_Padding = serializedObject.FindProperty("m_EditorData.packingParameters.padding");

            m_MasterAtlas = serializedObject.FindProperty("m_MasterAtlas");
            m_BindAsDefault = serializedObject.FindProperty("m_EditorData.bindAsDefault");
            m_VariantMultiplier = serializedObject.FindProperty("m_EditorData.variantMultiplier");

            m_Packables = serializedObject.FindProperty("m_EditorData.packables");
            m_PackableList = new ReorderableList(serializedObject, m_Packables, true, true, true, true);
            m_PackableList.onAddCallback = AddPackable;
            m_PackableList.onRemoveCallback = RemovePackable;
            m_PackableList.drawElementCallback = DrawPackableElement;
            m_PackableList.elementHeight = EditorGUIUtility.singleLineHeight;
            m_PackableList.headerHeight = 0f;

            SyncPlatformSettings();

            // Initialise texture importer's texture format strings
            TextureImporterInspector.InitializeTextureFormatStrings();

            m_TexturePlatformSettingsView = new SpriteAtlasInspectorPlatformSettingView(AllTargetsAreMaster());
            m_TexturePlatformSettingTextureHelper = new TexturePlatformSettingsFormatHelper();
            m_TexturePlatformSettingsController = new TexturePlatformSettingsViewController();
        }

        void SyncPlatformSettings()
        {
            m_TempPlatformSettings = new Dictionary<string, List<TextureImporterPlatformSettings>>();

            // Default platform
            var defaultSettings = new List<TextureImporterPlatformSettings>();
            m_TempPlatformSettings.Add(kDefaultPlatformName, defaultSettings);
            foreach (var t in targets)
            {
                TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                settings.name = kDefaultPlatformName;

                SerializedObject targetSerializedObject = new SerializedObject(t);
                settings.maxTextureSize = targetSerializedObject.FindProperty("m_EditorData.textureSettings.maxTextureSize").intValue;
                settings.textureCompression = (TextureImporterCompression)targetSerializedObject.FindProperty("m_EditorData.textureSettings.textureCompression").enumValueIndex;
                settings.crunchedCompression = targetSerializedObject.FindProperty("m_EditorData.textureSettings.crunchedCompression").boolValue;
                settings.compressionQuality = targetSerializedObject.FindProperty("m_EditorData.textureSettings.compressionQuality").intValue;

                defaultSettings.Add(settings);
            }

            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            foreach (var platform in m_ValidPlatforms)
            {
                var platformSettings = new List<TextureImporterPlatformSettings>();
                m_TempPlatformSettings.Add(platform.name, platformSettings);
                foreach (SpriteAtlas sa in targets)
                {
                    TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                    settings.name = platform.name;
                    sa.CopyPlatformSettingsIfAvailable(platform.name, settings);

                    // setting will be in default state if copy failed
                    platformSettings.Add(settings);
                }
            }
        }

        void AddPackable(ReorderableList list)
        {
            ObjectSelector.get.Show(null, typeof(Object), null, false);
            ObjectSelector.get.searchFilter = "t:sprite t:texture2d t:folder";
            ObjectSelector.get.objectSelectorID = s_Styles.packableSelectorHash;
        }

        void RemovePackable(ReorderableList list)
        {
            var index = list.index;
            if (index != -1)
                spriteAtlas.RemoveAt(index);
        }

        void DrawPackableElement(Rect rect, int index, bool selected, bool focused)
        {
            var property = m_Packables.GetArrayElementAtIndex(index);
            var controlID = EditorGUIUtility.GetControlID(s_Styles.packableElementHash, FocusType.Passive);
            var previousObject = property.objectReferenceValue;

            EditorGUI.BeginChangeCheck();
            var changedObject = EditorGUI.DoObjectField(rect, rect, controlID, previousObject, typeof(Object), null, ValidateObjectForPackableFieldAssignment, false);
            if (EditorGUI.EndChangeCheck())
            {
                // Always call Remove() on the previous object if we swapping the object field item.
                // This ensure the Sprites was pack in this atlas will be refreshed of it unbound.
                if (previousObject != null)
                    spriteAtlas.Remove(new Object[] { previousObject });
                property.objectReferenceValue = changedObject;
            }

            if (GUIUtility.keyboardControl == controlID && !selected)
                m_PackableList.index = index;
        }

        public override void OnInspectorGUI()
        {
            s_Styles = s_Styles ?? new Styles();

            serializedObject.Update();

            HandleCommonSettingUI();

            GUILayout.Space(EditorGUI.kSpacing);

            if (AllTargetsAreVariant())
                HandleVariantSettingUI();
            else if (AllTargetsAreMaster())
                HandleMasterSettingUI();

            GUILayout.Space(EditorGUI.kSpacing);

            HandleTextureSettingUI();

            GUILayout.Space(EditorGUI.kSpacing);

            // Only show the packable object list when:
            // - This is a master atlas.
            // - It is not currently selecting multiple atlases.
            if (targets.Length == 1 && AllTargetsAreMaster())
                HandlePackableListUI();

            bool spriteAtlasPackignEnabled = (EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnlyAtlas
                                              || EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOnAtlas);
            if (spriteAtlasPackignEnabled)
            {
                if (GUILayout.Button(s_Styles.packButton, GUILayout.ExpandWidth(false)))
                {
                    SpriteAtlas[] spriteAtlases = new SpriteAtlas[targets.Length];
                    for (int i = 0; i < spriteAtlases.Length; ++i)
                        spriteAtlases[i] = (SpriteAtlas)targets[i];

                    SpriteAtlasUtility.PackAtlases(spriteAtlases, EditorUserBuildSettings.activeBuildTarget);

                    // Packing an atlas might change platform settings in the process, reinitialize
                    SyncPlatformSettings();

                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(s_Styles.disabledPackLabel.text, MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleCommonSettingUI()
        {
            var atlasType = AtlasType.Undefined;
            if (AllTargetsAreMaster())
                atlasType = AtlasType.Master;
            else if (AllTargetsAreVariant())
                atlasType = AtlasType.Variant;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = atlasType == AtlasType.Undefined;
            atlasType = (AtlasType)EditorGUILayout.IntPopup(s_Styles.atlasTypeLabel, (int)atlasType, s_Styles.atlasTypeOptions, s_Styles.atlasTypeValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                bool setToVariant = atlasType == AtlasType.Variant;
                foreach (SpriteAtlas sa in targets)
                    sa.SetIsVariant(setToVariant);

                // Reinit the platform setting view
                m_TexturePlatformSettingsView = new SpriteAtlasInspectorPlatformSettingView(AllTargetsAreMaster());
            }

            if (atlasType == AtlasType.Variant)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_MasterAtlas, s_Styles.masterAtlasLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    // Apply modified properties here to have latest master atlas reflected in native codes.
                    serializedObject.ApplyModifiedProperties();

                    foreach (SpriteAtlas sa in targets)
                    {
                        sa.CopyMasterAtlasSettings();
                        SyncPlatformSettings();
                    }
                }
            }

            EditorGUILayout.PropertyField(m_BindAsDefault, s_Styles.bindAsDefaultLabel);
        }

        private void HandleVariantSettingUI()
        {
            EditorGUILayout.LabelField(s_Styles.variantSettingLabel, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_VariantMultiplier, s_Styles.variantMultiplierLabel);

            // Test if the multiplier scale a power of two size (1024) into another power of 2 size.
            if (!Mathf.IsPowerOfTwo((int)(m_VariantMultiplier.floatValue * 1024)))
                EditorGUILayout.HelpBox(s_Styles.notPowerOfTwoWarning.text, MessageType.Warning, true);
        }

        private void HandleBoolToIntPropertyField(SerializedProperty prop, GUIContent content)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, content, prop);
            EditorGUI.BeginChangeCheck();
            var boolValue = EditorGUI.Toggle(rect, content, prop.boolValue);
            if (EditorGUI.EndChangeCheck())
                prop.boolValue = boolValue;
            EditorGUI.EndProperty();
        }

        private void HandleMasterSettingUI()
        {
            EditorGUILayout.LabelField(s_Styles.packingParametersLabel, EditorStyles.boldLabel);

            HandleBoolToIntPropertyField(m_EnableRotation, s_Styles.enableRotationLabel);
            HandleBoolToIntPropertyField(m_EnableTightPacking, s_Styles.enableTightPackingLabel);
            EditorGUILayout.IntPopup(m_Padding, s_Styles.paddingOptions, s_Styles.paddingValues, s_Styles.paddingLabel);

            GUILayout.Space(EditorGUI.kSpacing);
        }

        private void HandleTextureSettingUI()
        {
            EditorGUILayout.LabelField(s_Styles.textureSettingLabel, EditorStyles.boldLabel);

            HandleBoolToIntPropertyField(m_Readable, s_Styles.readWrite);
            HandleBoolToIntPropertyField(m_GenerateMipMaps, s_Styles.generateMipMapLabel);
            HandleBoolToIntPropertyField(m_UseSRGB, s_Styles.sRGBLabel);
            EditorGUILayout.PropertyField(m_FilterMode);

            var showAniso = !m_FilterMode.hasMultipleDifferentValues && !m_GenerateMipMaps.hasMultipleDifferentValues
                && (FilterMode)m_FilterMode.intValue != FilterMode.Point && m_GenerateMipMaps.boolValue;
            if (showAniso)
                EditorGUILayout.IntSlider(m_AnisoLevel, 0, 16);

            GUILayout.Space(EditorGUI.kSpacing);

            HandlePlatformSettingUI();
        }

        private void HandlePlatformSettingUI()
        {
            int shownTextureFormatPage = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms.ToArray(), s_Styles.defaultPlatformLabel);
            if (shownTextureFormatPage == -1)
            {
                List<TextureImporterPlatformSettings> platformSettings = m_TempPlatformSettings[kDefaultPlatformName];
                List<TextureImporterPlatformSettings> newSettings = new List<TextureImporterPlatformSettings>(platformSettings.Count);
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                    platformSettings[i].CopyTo(settings);
                    newSettings.Add(settings);
                }

                if (m_TexturePlatformSettingsController.HandleDefaultSettings(newSettings, m_TexturePlatformSettingsView))
                {
                    for (var i = 0; i < newSettings.Count; ++i)
                    {
                        if (platformSettings[i].maxTextureSize != newSettings[i].maxTextureSize)
                            m_MaxTextureSize.intValue = newSettings[i].maxTextureSize;
                        if (platformSettings[i].textureCompression != newSettings[i].textureCompression)
                            m_TextureCompression.enumValueIndex = (int)newSettings[i].textureCompression;
                        if (platformSettings[i].crunchedCompression != newSettings[i].crunchedCompression)
                            m_UseCrunchedCompression.boolValue = newSettings[i].crunchedCompression;
                        if (platformSettings[i].compressionQuality != newSettings[i].compressionQuality)
                            m_CompressionQuality.intValue = newSettings[i].compressionQuality;

                        newSettings[i].CopyTo(platformSettings[i]);
                    }
                }
            }
            else
            {
                BuildPlatform buildPlatform = m_ValidPlatforms[shownTextureFormatPage];
                List<TextureImporterPlatformSettings> platformSettings = m_TempPlatformSettings[buildPlatform.name];

                // Predetermine format if overridden is unchecked
                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    var settings = platformSettings[i];
                    if (!settings.overridden)
                    {
                        SpriteAtlas sa = (SpriteAtlas)targets[i];
                        settings.format = sa.FormatDetermineByAtlasSettings(buildPlatform.defaultTarget);
                    }
                }

                m_TexturePlatformSettingsView.buildPlatformTitle = buildPlatform.title.text;
                if (m_TexturePlatformSettingsController.HandlePlatformSettings(buildPlatform.defaultTarget, platformSettings, m_TexturePlatformSettingsView, m_TexturePlatformSettingTextureHelper))
                {
                    for (var i = 0; i < platformSettings.Count; ++i)
                    {
                        SpriteAtlas sa = (SpriteAtlas)targets[i];
                        sa.SetPlatformSettings(platformSettings[i]);
                    }
                }
            }

            EditorGUILayout.EndPlatformGrouping();
        }

        private void HandlePackableListUI()
        {
            var currentEvent = Event.current;
            var usedEvent = false;

            Rect rect = EditorGUILayout.GetControlRect();

            var controlID = EditorGUIUtility.s_LastControlID;
            switch (currentEvent.type)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(currentEvent.mousePosition) && GUI.enabled)
                    {
                        // Check each single object, so we can add multiple objects in a single drag.
                        var didAcceptDrag = false;
                        var references = DragAndDrop.objectReferences;
                        foreach (var obj in references)
                        {
                            if (IsPackable(obj))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (currentEvent.type == EventType.DragPerform)
                                {
                                    m_Packables.AppendFoldoutPPtrValue(obj);
                                    didAcceptDrag = true;
                                    DragAndDrop.activeControlID = 0;
                                }
                                else
                                    DragAndDrop.activeControlID = controlID;
                            }
                        }
                        if (didAcceptDrag)
                        {
                            GUI.changed = true;
                            DragAndDrop.AcceptDrag();
                            usedEvent = true;
                        }
                    }
                    break;
                case EventType.ValidateCommand:
                    if (currentEvent.commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == s_Styles.packableSelectorHash)
                        usedEvent = true;
                    break;
                case EventType.ExecuteCommand:
                    if (currentEvent.commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == s_Styles.packableSelectorHash)
                    {
                        var obj = ObjectSelector.GetCurrentObject();
                        if (IsPackable(obj))
                        {
                            m_Packables.AppendFoldoutPPtrValue(obj);
                            m_PackableList.index = m_Packables.arraySize - 1;
                        }

                        usedEvent = true;
                    }
                    break;
            }

            // Handle Foldout after we handle the current event because Foldout might process the drag and drop event and used it.
            m_PackableListExpanded = EditorGUI.Foldout(rect, m_PackableListExpanded, s_Styles.packableListLabel, true);

            if (usedEvent)
                currentEvent.Use();

            if (m_PackableListExpanded)
            {
                EditorGUI.indentLevel++;
                m_PackableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }
        }

        void CachePreviewTexture()
        {
            if (m_PreviewTextures == null || m_Hash != spriteAtlas.GetHashString())
            {
                m_PreviewTextures = spriteAtlas.GetPreviewTextures();
                m_PreviewAlphaTextures = spriteAtlas.GetPreviewAlphaTextures();
                m_Hash = spriteAtlas.GetHashString();

                if (m_PreviewTextures != null
                    && m_PreviewTextures.Length > 0
                    && m_TotalPages != m_PreviewTextures.Length)
                {
                    m_TotalPages = m_PreviewTextures.Length;
                    m_OptionDisplays = new string[m_TotalPages];
                    m_OptionValues = new int[m_TotalPages];
                    for (int i = 0; i < m_TotalPages; ++i)
                    {
                        m_OptionDisplays[i] = string.Format("# {0}", i + 1);
                        m_OptionValues[i] = i;
                    }
                }
            }
        }

        public override string GetInfoString()
        {
            if (m_PreviewTextures != null && m_PreviewPage < m_PreviewTextures.Length)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];
                TextureFormat format = TextureUtil.GetTextureFormat(t);

                return string.Format("{0}x{1} {2}\n{3}", t.width, t.height, TextureUtil.GetTextureFormatString(format), EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(t)));
            }
            return "";
        }

        public override bool HasPreviewGUI()
        {
            CachePreviewTexture();
            return (m_PreviewTextures != null && m_PreviewTextures.Length > 0);
        }

        public override void OnPreviewSettings()
        {
            // Do not allow changing of pages when multiple atlases is selected.
            if (targets.Length == 1 && m_OptionDisplays != null && m_OptionValues != null && m_TotalPages > 1)
                m_PreviewPage = EditorGUILayout.IntPopup(m_PreviewPage, m_OptionDisplays, m_OptionValues, s_Styles.preDropDown, GUILayout.MaxWidth(50));
            else
                m_PreviewPage = 0;

            if (m_PreviewTextures != null)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];

                if (TextureUtil.HasAlphaTextureFormat(t.format) || (m_PreviewAlphaTextures != null && m_PreviewAlphaTextures.Length > 0))
                    m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? s_Styles.alphaIcon : s_Styles.RGBIcon, s_Styles.previewButton);

                int mipCount = Mathf.Max(1, TextureUtil.GetMipmapCount(t));
                if (mipCount > 1)
                {
                    GUILayout.Box(s_Styles.smallZoom, s_Styles.previewLabel);
                    m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.MaxWidth(64)));
                    GUILayout.Box(s_Styles.largeZoom, s_Styles.previewLabel);
                }
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            CachePreviewTexture();

            if (m_ShowAlpha && m_PreviewAlphaTextures != null && m_PreviewPage < m_PreviewAlphaTextures.Length)
            {
                var at = m_PreviewAlphaTextures[m_PreviewPage];
                EditorGUI.DrawTextureTransparent(r, at, ScaleMode.ScaleToFit);
            }
            else if (m_PreviewTextures != null && m_PreviewPage < m_PreviewTextures.Length)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];

                float oldBias = t.mipMapBias;
                float bias = m_MipLevel - (float)(System.Math.Log(t.width / r.width) / System.Math.Log(2));
                TextureUtil.SetMipMapBiasNoDirty(t, bias);

                if (m_ShowAlpha)
                    EditorGUI.DrawTextureAlpha(r, t, ScaleMode.ScaleToFit);
                else
                    EditorGUI.DrawTextureTransparent(r, t, ScaleMode.ScaleToFit);

                TextureUtil.SetMipMapBiasNoDirty(t, oldBias);
            }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var spriteAtlas = AssetDatabase.LoadMainAssetAtPath(assetPath) as SpriteAtlas;
            if (spriteAtlas == null)
                return null;

            var previewTextures = spriteAtlas.GetPreviewTextures();
            if (previewTextures == null || previewTextures.Length == 0)
                return null;

            var texture = previewTextures[0];
            PreviewHelpers.AdjustWidthAndHeightForStaticPreview(texture.width, texture.height, ref width, ref height);

            return SpriteUtility.CreateTemporaryDuplicate(texture, width, height);
        }
    }
}
