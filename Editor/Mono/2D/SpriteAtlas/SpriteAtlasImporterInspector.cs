// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.U2D;
using UnityEditor.Build;
using UnityEditor.U2D.Common;
using UnityEditor.U2D.Interface;
using UnityEditorInternal;
using UnityEditor.AssetImporters;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteAtlasImporter))]
    internal class SpriteAtlasImporterInspector : AssetImporterEditor
    {
        class SpriteAtlasInspectorPlatformSettingView : TexturePlatformSettingsView
        {
            private bool m_ShowMaxSizeOption;

            public SpriteAtlasInspectorPlatformSettingView(bool showMaxSizeOption)
            {
                m_ShowMaxSizeOption = showMaxSizeOption;
            }

            public override int DrawMaxSize(int defaultValue, bool isMixedValue, bool isDisabled, out bool changed)
            {
                if (m_ShowMaxSizeOption)
                    return base.DrawMaxSize(defaultValue, isMixedValue, isDisabled, out changed);
                else
                    changed = false;
                return defaultValue;
            }
        }

        class Styles
        {
            public readonly GUIStyle preDropDown = "preDropDown";
            public readonly GUIStyle previewButton = "preButton";
            public readonly GUIStyle previewSlider = "preSlider";
            public readonly GUIStyle previewSliderThumb = "preSliderThumb";
            public readonly GUIStyle previewLabel = "preLabel";

            public readonly GUIContent textureSettingLabel = EditorGUIUtility.TrTextContent("Texture");
            public readonly GUIContent variantSettingLabel = EditorGUIUtility.TrTextContent("Variant");
            public readonly GUIContent packingParametersLabel = EditorGUIUtility.TrTextContent("Packing");
            public readonly GUIContent atlasTypeLabel = EditorGUIUtility.TrTextContent("Type");
            public readonly GUIContent defaultPlatformLabel = EditorGUIUtility.TrTextContent("Default");
            public readonly GUIContent masterAtlasLabel = EditorGUIUtility.TrTextContent("Master Atlas", "Assigning another Sprite Atlas asset will make this atlas a variant of it.");
            public readonly GUIContent packerLabel = EditorGUIUtility.TrTextContent("Scriptable Packer", "Scriptable Object that implements custom packing for Sprite-Atlas.");
            public readonly GUIContent bindAsDefaultLabel = EditorGUIUtility.TrTextContent("Include in Build", "Packed textures will be included in the build by default.");
            public readonly GUIContent enableRotationLabel = EditorGUIUtility.TrTextContent("Allow Rotation", "Try rotating the sprite to fit better during packing.");
            public readonly GUIContent enableTightPackingLabel = EditorGUIUtility.TrTextContent("Tight Packing", "Use the mesh outline to fit instead of the whole texture rect during packing.");
            public readonly GUIContent enableAlphaDilationLabel = EditorGUIUtility.TrTextContent("Alpha Dilation", "Enable Alpha Dilation for SpriteAtlas padding pixels.");
            public readonly GUIContent paddingLabel = EditorGUIUtility.TrTextContent("Padding", "The amount of extra padding between packed sprites.");

            public readonly GUIContent generateMipMapLabel = EditorGUIUtility.TrTextContent("Generate Mip Maps");
            public readonly GUIContent packPreviewLabel = EditorGUIUtility.TrTextContent("Pack Preview", "Save and preview packed Sprite Atlas textures.");
            public readonly GUIContent sRGBLabel = EditorGUIUtility.TrTextContent("sRGB", "Texture content is stored in gamma space.");
            public readonly GUIContent readWrite = EditorGUIUtility.TrTextContent("Read/Write", "Enable to be able to access the raw pixel data from code.");
            public readonly GUIContent variantMultiplierLabel = EditorGUIUtility.TrTextContent("Scale", "Down scale ratio.");
            public readonly GUIContent copyMasterButton = EditorGUIUtility.TrTextContent("Copy Master's Settings", "Copy all master's settings into this variant.");

            public readonly GUIContent disabledPackLabel = EditorGUIUtility.TrTextContent("Sprite Atlas packing is disabled. Enable it in Edit > Project Settings > Editor.", null, EditorGUIUtility.GetHelpIcon(MessageType.Info));
            public readonly GUIContent packableListLabel = EditorGUIUtility.TrTextContent("Objects for Packing", "Only accepts Folders, Sprite Sheet (Texture) and Sprite.");

            public readonly GUIContent notPowerOfTwoWarning = EditorGUIUtility.TrTextContent("This scale will produce a Variant Sprite Atlas with a packed Texture that is NPOT (non - power of two). This may cause visual artifacts in certain compression/Texture formats.");
            public readonly GUIContent secondaryTextureNameLabel = EditorGUIUtility.TrTextContent("Secondary Texture Name", "The name of the Secondary Texture to apply the following settings to.");
            public readonly GUIContent platformSettingsDropDownLabel = EditorGUIUtility.TrTextContent("Show Platform Settings For");

            public readonly GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public readonly GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public readonly GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public readonly GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
            public readonly GUIContent trashIcon = EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Delete currently selected settings.");

            public readonly int packableElementHash = "PackableElement".GetHashCode();
            public readonly int packableSelectorHash = "PackableSelector".GetHashCode();

            public readonly string swapObjectRegisterUndo = L10n.Tr("Swap Packable");
            public readonly string secondaryTextureNameTextControlName = "secondary_texture_name_text_field";
            public readonly string defaultTextForSecondaryTextureName = L10n.Tr("(Matches the names of the Secondary Textures in your Sprites.)");
            public readonly string nameUniquenessWarning = L10n.Tr("Secondary Texture names must be unique within a Sprite or Sprite Atlas.");

            public readonly int[] atlasTypeValues = { 0, 1 };
            public readonly GUIContent[] atlasTypeOptions =
            {
                EditorGUIUtility.TrTextContent("Master"),
                EditorGUIUtility.TrTextContent("Variant"),
            };

            public readonly int[] paddingValues = { 2, 4, 8 };
            public readonly GUIContent[] paddingOptions;

            public Styles()
            {
                paddingOptions = new GUIContent[paddingValues.Length];
                for (var i = 0; i < paddingValues.Length; ++i)
                    paddingOptions[i] = EditorGUIUtility.TextContent(paddingValues[i].ToString());
            }
        }

        private static Styles s_Styles;

        private static Styles styles
        {
            get
            {
                s_Styles = s_Styles ?? new Styles();
                return s_Styles;
            }
        }
        private SpriteAtlasAsset spriteAtlasAsset
        {
            get { return m_TargetAsset; }
        }
        private SpriteAtlasImporter spriteAtlasImporter
        {
            get { return target as SpriteAtlasImporter; }
        }
        private enum AtlasType { Undefined = -1, Master = 0, Variant = 1 }

        private SerializedProperty m_FilterMode;
        private SerializedProperty m_AnisoLevel;
        private SerializedProperty m_GenerateMipMaps;
        private SerializedProperty m_Readable;
        private SerializedProperty m_UseSRGB;
        private SerializedProperty m_EnableTightPacking;
        private SerializedProperty m_EnableAlphaDilation;
        private SerializedProperty m_EnableRotation;
        private SerializedProperty m_Padding;
        private SerializedProperty m_BindAsDefault;
        private SerializedProperty m_Packables;

        private SerializedProperty m_MasterAtlas;
        private SerializedProperty m_VariantScale;
        private SerializedProperty m_ScriptablePacker;

        private string m_Hash;
        private int m_PreviewPage = 0;
        private int m_TotalPages = 0;
        private int[] m_OptionValues = null;
        private string[] m_OptionDisplays = null;
        private Texture2D[] m_PreviewTextures = null;
        private Texture2D[] m_PreviewAlphaTextures = null;

        private bool m_PackableListExpanded = true;
        private ReorderableList m_PackableList;
        private SpriteAtlasAsset m_TargetAsset;
        private string m_AssetPath;

        private float m_MipLevel = 0;
        private bool m_ShowAlpha;
        private bool m_Discard = false;

        private List<string> m_PlatformSettingsOptions;
        private int m_SelectedPlatformSettings = 0;

        private int m_ContentHash = 0;
        private List<BuildPlatform> m_ValidPlatforms;
        private Dictionary<string, List<TextureImporterPlatformSettings>> m_TempPlatformSettings;

        private ITexturePlatformSettingsView m_TexturePlatformSettingsView;
        private ITexturePlatformSettingsView m_SecondaryTexturePlatformSettingsView;
        private ITexturePlatformSettingsFormatHelper m_TexturePlatformSettingTextureHelper;
        private ITexturePlatformSettingsController m_TexturePlatformSettingsController;
        private SerializedObject m_SerializedAssetObject = null;

        // The first two options are the main texture and a separator while the last two options are another separator and the new settings menu.
        private bool secondaryTextureSelected { get { return m_SelectedPlatformSettings >= 2 && m_SelectedPlatformSettings <= m_PlatformSettingsOptions.Count - 3; } }

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

        bool IsTargetVariant()
        {
            return spriteAtlasAsset ? spriteAtlasAsset.isVariant : false;
        }

        bool IsTargetMaster()
        {
            return spriteAtlasAsset ? !spriteAtlasAsset.isVariant : true;
        }

        protected override bool needsApplyRevert => false;

        internal override string targetTitle
        {
            get
            {
                return spriteAtlasAsset ? ( Path.GetFileNameWithoutExtension(m_AssetPath) + " (Sprite Atlas)" ) : "SpriteAtlasImporter Settings";
            }
        }

        private string LoadSourceAsset()
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var loadedObjects = InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
            if (loadedObjects.Length > 0)
                m_TargetAsset = loadedObjects[0] as SpriteAtlasAsset;
            return assetPath;
        }

        private SerializedObject serializedAssetObject
        {
            get
            {
                return GetSerializedAssetObject();
            }
        }

        internal static int SpriteAtlasAssetHash(SerializedObject obj)
        {
            int hashCode = 0;
            if (obj == null)
                return 0;
            unchecked
            {
                hashCode = (int)2166136261 ^ (int) obj.FindProperty("m_MasterAtlas").contentHash;
                hashCode = hashCode * 16777619 ^ (int) obj.FindProperty("m_ImporterData").contentHash;
                hashCode = hashCode * 16777619 ^ (int) obj.FindProperty("m_IsVariant").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_ScriptablePacker").contentHash;
            }
            return hashCode;
        }

        internal static int SpriteAtlasImporterHash(SerializedObject obj)
        {
            int hashCode = 0;
            if (obj == null)
                return 0;
            unchecked
            {
                hashCode = (int)2166136261 ^ (int)obj.FindProperty("m_PackingSettings").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_TextureSettings").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_PlatformSettings").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_SecondaryTextureSettings").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_BindAsDefault").contentHash;
                hashCode = hashCode * 16777619 ^ (int)obj.FindProperty("m_VariantMultiplier").contentHash;
            }
            return hashCode;
        }

        internal int GetInspectorHash()
        {
            return SpriteAtlasAssetHash(m_SerializedAssetObject) * 16777619 ^ SpriteAtlasImporterHash(m_SerializedObject);
        }

        private SerializedObject GetSerializedAssetObject()
        {
            if (m_SerializedAssetObject == null)
            {
                try
                {
                    m_SerializedAssetObject = new SerializedObject(spriteAtlasAsset, m_Context);
                    m_SerializedAssetObject.inspectorMode = inspectorMode;
                    m_ContentHash = GetInspectorHash();
                    m_EnabledProperty = m_SerializedAssetObject.FindProperty("m_Enabled");
                }
                catch (System.ArgumentException e)
                {
                    m_SerializedAssetObject = null;
                    m_EnabledProperty = null;
                    throw new SerializedObjectNotCreatableException(e.Message);
                }
            }
            return m_SerializedAssetObject;
        }

       public override void OnEnable()
        {
            base.OnEnable();

            m_FilterMode = serializedObject.FindProperty("m_TextureSettings.filterMode");
            m_AnisoLevel = serializedObject.FindProperty("m_TextureSettings.anisoLevel");
            m_GenerateMipMaps = serializedObject.FindProperty("m_TextureSettings.generateMipMaps");
            m_Readable = serializedObject.FindProperty("m_TextureSettings.readable");
            m_UseSRGB = serializedObject.FindProperty("m_TextureSettings.sRGB");

            m_EnableTightPacking = serializedObject.FindProperty("m_PackingSettings.enableTightPacking");
            m_EnableRotation = serializedObject.FindProperty("m_PackingSettings.enableRotation");
            m_EnableAlphaDilation = serializedObject.FindProperty("m_PackingSettings.enableAlphaDilation");
            m_Padding = serializedObject.FindProperty("m_PackingSettings.padding");
            m_BindAsDefault = serializedObject.FindProperty("m_BindAsDefault");
            m_VariantScale = serializedObject.FindProperty("m_VariantMultiplier");
            PopulatePlatformSettingsOptions();

            SyncPlatformSettings();

            m_TexturePlatformSettingsView = new SpriteAtlasInspectorPlatformSettingView(IsTargetMaster());
            m_TexturePlatformSettingTextureHelper = new TexturePlatformSettingsFormatHelper();
            m_TexturePlatformSettingsController = new TexturePlatformSettingsViewController();

            // Don't show max size option for secondary textures as they must have the same size as the main texture.
            m_SecondaryTexturePlatformSettingsView = new SpriteAtlasInspectorPlatformSettingView(false);

            m_AssetPath = LoadSourceAsset();
            if (spriteAtlasAsset == null)
                return;

            m_MasterAtlas = serializedAssetObject.FindProperty("m_MasterAtlas");
            m_ScriptablePacker = serializedAssetObject.FindProperty("m_ScriptablePacker");
            m_Packables = serializedAssetObject.FindProperty("m_ImporterData.packables");
            m_PackableList = new ReorderableList(serializedAssetObject, m_Packables, true, true, true, true);
            m_PackableList.onAddCallback = AddPackable;
            m_PackableList.drawElementCallback = DrawPackableElement;
            m_PackableList.elementHeight = EditorGUIUtility.singleLineHeight;
            m_PackableList.headerHeight = 0f;
        }

        // Populate the platform settings dropdown list with secondary texture names found through serialized properties of the Sprite Atlas assets.
        private void PopulatePlatformSettingsOptions()
        {
            m_PlatformSettingsOptions = new List<string> { L10n.Tr("Main Texture"), "", "", L10n.Tr("New Secondary Texture settings.") };
            SerializedProperty secondaryPlatformSettings = serializedObject.FindProperty("m_SecondaryTextureSettings");
            if (secondaryPlatformSettings != null && !secondaryPlatformSettings.hasMultipleDifferentValues)
            {
                int numSecondaryTextures = secondaryPlatformSettings.arraySize;
                List<string> secondaryTextureNames = new List<string>(numSecondaryTextures);

                for (int i = 0; i < numSecondaryTextures; ++i)
                    secondaryTextureNames.Add(secondaryPlatformSettings.GetArrayElementAtIndex(i).displayName);

                // Insert after main texture and the separator.
                m_PlatformSettingsOptions.InsertRange(2, secondaryTextureNames);
            }

            m_SelectedPlatformSettings = 0;
        }

        void SyncPlatformSettings()
        {
            m_TempPlatformSettings = new Dictionary<string, List<TextureImporterPlatformSettings>>();

            string secondaryTextureName = null;
            if (secondaryTextureSelected)
                secondaryTextureName = m_PlatformSettingsOptions[m_SelectedPlatformSettings];

            // Default platform
            var defaultSettings = new List<TextureImporterPlatformSettings>();
            m_TempPlatformSettings.Add(TextureImporterInspector.s_DefaultPlatformName, defaultSettings);
            var settings = secondaryTextureSelected
                ? spriteAtlasImporter.GetSecondaryPlatformSettings(TextureImporterInspector.s_DefaultPlatformName, secondaryTextureName)
                : spriteAtlasImporter.GetPlatformSettings(TextureImporterInspector.s_DefaultPlatformName);
            defaultSettings.Add(settings);

            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            foreach (var platform in m_ValidPlatforms)
            {
                var platformSettings = new List<TextureImporterPlatformSettings>();
                m_TempPlatformSettings.Add(platform.name, platformSettings);
                var perPlatformSettings = secondaryTextureSelected ? spriteAtlasImporter.GetSecondaryPlatformSettings(platform.name, secondaryTextureName) : spriteAtlasImporter.GetPlatformSettings(platform.name);
                // setting will be in default state if copy failed
                platformSettings.Add(perPlatformSettings);
            }
        }

        void RenameSecondaryPlatformSettings(string oldName, string newName)
        {
            spriteAtlasImporter.DeleteSecondaryPlatformSettings(oldName);

            var defaultPlatformSettings = m_TempPlatformSettings[TextureImporterInspector.s_DefaultPlatformName];
            spriteAtlasImporter.SetSecondaryPlatformSettings(defaultPlatformSettings[0], newName);

            foreach (var buildPlatform in m_ValidPlatforms)
            {
                var platformSettings = m_TempPlatformSettings[buildPlatform.name];
                spriteAtlasImporter.SetSecondaryPlatformSettings(platformSettings[0], newName);
            }
        }

        void AddPackable(ReorderableList list)
        {
            ObjectSelector.get.Show(null, typeof(Object), null, false);
            ObjectSelector.get.searchFilter = "t:sprite t:texture2d t:folder";
            ObjectSelector.get.objectSelectorID = styles.packableSelectorHash;
        }

        void DrawPackableElement(Rect rect, int index, bool selected, bool focused)
        {
            var property = m_Packables.GetArrayElementAtIndex(index);
            var controlID = EditorGUIUtility.GetControlID(styles.packableElementHash, FocusType.Passive);
            var previousObject = property.objectReferenceValue;

            var changedObject = EditorGUI.DoObjectField(rect, rect, controlID, previousObject, target, typeof(Object), ValidateObjectForPackableFieldAssignment, false);
            if (changedObject != previousObject)
            {
                Undo.RegisterCompleteObjectUndo(spriteAtlasAsset, styles.swapObjectRegisterUndo);
                property.objectReferenceValue = changedObject;
            }

            if (GUIUtility.keyboardControl == controlID && !selected)
                m_PackableList.index = index;
        }

        protected override void Apply()
        {
            if (HasModified())
            {
                if (spriteAtlasAsset)
                {
                    SpriteAtlasAsset.Save(spriteAtlasAsset, m_AssetPath);
                    AssetDatabase.ImportAsset(m_AssetPath);
                }

                m_ContentHash = GetInspectorHash();
            }
            base.Apply();
        }

        protected override bool useAssetDrawPreview { get { return false; } }

        protected void PackPreviewGUI()
        {
            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!HasModified() || !IsValidAtlas() || Application.isPlaying))
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(styles.packPreviewLabel))
                    {
                        GUI.FocusControl(null);
                        SpriteAtlasUtility.EnableV2Import(true);
                        SaveChanges();
                        SpriteAtlasUtility.EnableV2Import(false);
                    }
                }
            }
        }

        private bool IsValidAtlas()
        {
            if (IsTargetVariant())
                return m_MasterAtlas.objectReferenceValue != null;
            else
                return true;
        }

        public override bool HasModified()
        {
            return !m_Discard && (base.HasModified() || m_ContentHash != GetInspectorHash());
        }

        private void ValidateMasterAtlas()
        {
            if (m_MasterAtlas.objectReferenceValue != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(m_MasterAtlas.objectReferenceValue);
                if (assetPath == m_AssetPath)
                {
                    UnityEngine.Debug.LogWarning("Cannot set itself as MasterAtlas. Please assign a valid MasterAtlas.");
                    m_MasterAtlas.objectReferenceValue = null;
                }
            }
            if (m_MasterAtlas.objectReferenceValue != null)
            {
                SpriteAtlas masterAsset = m_MasterAtlas.objectReferenceValue as SpriteAtlas;
                if (masterAsset != null && masterAsset.isVariant)
                {
                    UnityEngine.Debug.LogWarning("Cannot set a VariantAtlas as MasterAtlas. Please assign a valid MasterAtlas.");
                    m_MasterAtlas.objectReferenceValue = null;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Ensure changes done through script are reflected immediately in Inspector by Syncing m_TempPlatformSettings with Actual Settings.
            SyncPlatformSettings();

            serializedObject.Update();
            if (spriteAtlasAsset)
            {
                serializedAssetObject.Update();
                HandleCommonSettingUI();
            }
            EditorGUILayout.PropertyField(m_BindAsDefault, styles.bindAsDefaultLabel);

            GUILayout.Space(EditorGUI.kSpacing);

            bool isTargetMaster = true;
            if (spriteAtlasAsset)
                isTargetMaster = IsTargetMaster();

            if (isTargetMaster)
                HandleMasterSettingUI();
            if (!spriteAtlasAsset || IsTargetVariant())
                HandleVariantSettingUI();

            GUILayout.Space(EditorGUI.kSpacing);

            HandleTextureSettingUI();

            GUILayout.Space(EditorGUI.kSpacing);

            // Only show the packable object list when:
            // - This is a master atlas.
            if (targets.Length == 1 && IsTargetMaster() && spriteAtlasAsset)
                HandlePackableListUI();

            serializedObject.ApplyModifiedProperties();
            if (spriteAtlasAsset)
            {
                serializedAssetObject.ApplyModifiedProperties();
                PackPreviewGUI();
            }

            ApplyRevertGUI();
        }

        private void HandleCommonSettingUI()
        {
            var atlasType = AtlasType.Undefined;
            if (IsTargetMaster())
                atlasType = AtlasType.Master;
            else if (IsTargetVariant())
                atlasType = AtlasType.Variant;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = atlasType == AtlasType.Undefined;
            atlasType = (AtlasType)EditorGUILayout.IntPopup(styles.atlasTypeLabel, (int)atlasType, styles.atlasTypeOptions, styles.atlasTypeValues);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                bool setToVariant = atlasType == AtlasType.Variant;
                spriteAtlasAsset.SetIsVariant(setToVariant);

                // Reinit the platform setting view
                m_TexturePlatformSettingsView = new SpriteAtlasInspectorPlatformSettingView(IsTargetMaster());
            }
            m_ScriptablePacker.objectReferenceValue = EditorGUILayout.ObjectField(styles.packerLabel, m_ScriptablePacker.objectReferenceValue, typeof(UnityEditor.U2D.ScriptablePacker), false);
            if (atlasType == AtlasType.Variant)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_MasterAtlas, styles.masterAtlasLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    ValidateMasterAtlas();
                    // Apply modified properties here to have latest master atlas reflected in native codes.
                    serializedAssetObject.ApplyModifiedPropertiesWithoutUndo();
                    PopulatePlatformSettingsOptions();
                    SyncPlatformSettings();
                }
            }
        }

        private void HandleVariantSettingUI()
        {
            EditorGUILayout.LabelField(styles.variantSettingLabel, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_VariantScale, styles.variantMultiplierLabel);

            // Test if the multiplier scale a power of two size (1024) into another power of 2 size.
            if (!Mathf.IsPowerOfTwo((int)(m_VariantScale.floatValue * 1024)))
                EditorGUILayout.HelpBox(styles.notPowerOfTwoWarning.text, MessageType.Warning, true);
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
            EditorGUILayout.LabelField(styles.packingParametersLabel, EditorStyles.boldLabel);

            HandleBoolToIntPropertyField(m_EnableRotation, styles.enableRotationLabel);
            HandleBoolToIntPropertyField(m_EnableTightPacking, styles.enableTightPackingLabel);
            HandleBoolToIntPropertyField(m_EnableAlphaDilation, styles.enableAlphaDilationLabel);
            EditorGUILayout.IntPopup(m_Padding, styles.paddingOptions, styles.paddingValues, styles.paddingLabel);

            GUILayout.Space(EditorGUI.kSpacing);
        }

        private void HandleTextureSettingUI()
        {
            EditorGUILayout.LabelField(styles.textureSettingLabel, EditorStyles.boldLabel);

            HandleBoolToIntPropertyField(m_Readable, styles.readWrite);
            HandleBoolToIntPropertyField(m_GenerateMipMaps, styles.generateMipMapLabel);
            HandleBoolToIntPropertyField(m_UseSRGB, styles.sRGBLabel);
            EditorGUILayout.PropertyField(m_FilterMode);

            var showAniso = !m_FilterMode.hasMultipleDifferentValues && !m_GenerateMipMaps.hasMultipleDifferentValues
                && (FilterMode)m_FilterMode.intValue != FilterMode.Point && m_GenerateMipMaps.boolValue;
            if (showAniso)
                EditorGUILayout.IntSlider(m_AnisoLevel, 0, 16);

            GUILayout.Space(EditorGUI.kSpacing);

            // "Show Platform Settings For" dropdown
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(s_Styles.platformSettingsDropDownLabel);

                EditorGUI.BeginChangeCheck();
                m_SelectedPlatformSettings = EditorGUILayout.Popup(m_SelectedPlatformSettings, m_PlatformSettingsOptions.ToArray(), GUILayout.MaxWidth(150.0f));
                if (EditorGUI.EndChangeCheck())
                {
                    // New settings option is selected...
                    if (m_SelectedPlatformSettings == m_PlatformSettingsOptions.Count - 1)
                    {
                        m_PlatformSettingsOptions.Insert(m_SelectedPlatformSettings - 1, s_Styles.defaultTextForSecondaryTextureName);
                        m_SelectedPlatformSettings--;
                        EditorGUI.FocusTextInControl(s_Styles.secondaryTextureNameTextControlName);
                    }
                    SyncPlatformSettings();
                }

                if (secondaryTextureSelected)
                {
                    // trash can button
                    if (GUILayout.Button(s_Styles.trashIcon, EditorStyles.iconButton, GUILayout.ExpandWidth(false)))
                    {
                        EditorGUI.EndEditingActiveTextField();

                        spriteAtlasImporter.DeleteSecondaryPlatformSettings(m_PlatformSettingsOptions[m_SelectedPlatformSettings]);

                        m_PlatformSettingsOptions.RemoveAt(m_SelectedPlatformSettings);

                        m_SelectedPlatformSettings--;
                        if (m_SelectedPlatformSettings == 1)
                            m_SelectedPlatformSettings = 0;

                        SyncPlatformSettings();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Texture platform settings UI.
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.indentLevel++;
                GUILayout.Space(EditorGUI.indent);
                EditorGUI.indentLevel--;

                if (m_SelectedPlatformSettings == 0)
                {
                    GUILayout.Space(EditorGUI.kSpacing);
                    HandlePlatformSettingUI(null);
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Space(EditorGUI.kSpacing);
                        string oldSecondaryTextureName = m_PlatformSettingsOptions[m_SelectedPlatformSettings];
                        GUI.SetNextControlName(s_Styles.secondaryTextureNameTextControlName);

                        EditorGUI.BeginChangeCheck();
                        string textFieldText = EditorGUILayout.DelayedTextField(s_Styles.secondaryTextureNameLabel, oldSecondaryTextureName);
                        if (EditorGUI.EndChangeCheck() && oldSecondaryTextureName != textFieldText)
                        {
                            if (!m_PlatformSettingsOptions.Exists(x => x == textFieldText))
                            {
                                m_PlatformSettingsOptions[m_SelectedPlatformSettings] = textFieldText;
                                RenameSecondaryPlatformSettings(oldSecondaryTextureName, textFieldText);
                            }
                            else
                            {
                                Debug.LogWarning(s_Styles.nameUniquenessWarning);
                                EditorGUI.FocusTextInControl(s_Styles.secondaryTextureNameTextControlName);
                            }
                        }

                        string secondaryTextureName = m_PlatformSettingsOptions[m_SelectedPlatformSettings];
                        EditorGUI.BeginChangeCheck();
                        bool value = EditorGUILayout.Toggle(s_Styles.sRGBLabel, spriteAtlasImporter.GetSecondaryColorSpace(secondaryTextureName));
                        if (EditorGUI.EndChangeCheck())
                            spriteAtlasImporter.SetSecondaryColorSpace(secondaryTextureName, value);

                        HandlePlatformSettingUI(textFieldText);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void HandlePlatformSettingUI(string secondaryTextureName)
        {
            int shownTextureFormatPage = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms.ToArray(), styles.defaultPlatformLabel);
            var defaultPlatformSettings = m_TempPlatformSettings[TextureImporterInspector.s_DefaultPlatformName];
            bool isSecondary = secondaryTextureName != null;
            ITexturePlatformSettingsView view = isSecondary ? m_SecondaryTexturePlatformSettingsView : m_TexturePlatformSettingsView;
            if (shownTextureFormatPage == -1)
            {
                if (m_TexturePlatformSettingsController.HandleDefaultSettings(defaultPlatformSettings, m_TexturePlatformSettingsView, m_TexturePlatformSettingTextureHelper))
                {
                    for (var i = 0; i < defaultPlatformSettings.Count; ++i)
                    {
                        if (isSecondary)
                            spriteAtlasImporter.SetSecondaryPlatformSettings(defaultPlatformSettings[i], secondaryTextureName);
                        else
                            spriteAtlasImporter.SetPlatformSettings(defaultPlatformSettings[i]);
                    }
                }
            }
            else
            {
                var buildPlatform = m_ValidPlatforms[shownTextureFormatPage];
                var platformSettings = m_TempPlatformSettings[buildPlatform.name];


                for (var i = 0; i < platformSettings.Count; ++i)
                {
                    var settings = platformSettings[i];
                    if (!settings.overridden)
                    {
                        if (defaultPlatformSettings[0].format == TextureImporterFormat.Automatic)
                        {
                            settings.format = (TextureImporterFormat)spriteAtlasImporter.GetTextureFormat(buildPlatform.defaultTarget);
                        }
                        else
                        {
                            settings.format = defaultPlatformSettings[0].format;
                        }

                        settings.maxTextureSize = defaultPlatformSettings[0].maxTextureSize;
                        settings.crunchedCompression = defaultPlatformSettings[0].crunchedCompression;
                        settings.compressionQuality = defaultPlatformSettings[0].compressionQuality;
                    }
                }

                m_TexturePlatformSettingsView.buildPlatformTitle = buildPlatform.title.text;
                if (m_TexturePlatformSettingsController.HandlePlatformSettings(buildPlatform.defaultTarget, platformSettings, m_TexturePlatformSettingsView, m_TexturePlatformSettingTextureHelper))
                {
                    for (var i = 0; i < platformSettings.Count; ++i)
                    {
                        if (isSecondary)
                            spriteAtlasImporter.SetSecondaryPlatformSettings(platformSettings[i], secondaryTextureName);
                        else
                            spriteAtlasImporter.SetPlatformSettings(platformSettings[i]);
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
                    if (currentEvent.commandName == ObjectSelector.ObjectSelectorClosedCommand && ObjectSelector.get.objectSelectorID == styles.packableSelectorHash)
                        usedEvent = true;
                    break;
                case EventType.ExecuteCommand:
                    if (currentEvent.commandName == ObjectSelector.ObjectSelectorClosedCommand && ObjectSelector.get.objectSelectorID == styles.packableSelectorHash)
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
            m_PackableListExpanded = EditorGUI.Foldout(rect, m_PackableListExpanded, styles.packableListLabel, true);

            if (usedEvent)
                currentEvent.Use();

            if (m_PackableListExpanded)
            {
                EditorGUI.indentLevel++;
                m_PackableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }
        }

        public override void SaveChanges()
        {
            if (!m_Discard)
                base.SaveChanges();
            m_ContentHash = GetInspectorHash();
        }

        public override void DiscardChanges()
        {
            m_Discard = true;
            base.DiscardChanges();
            m_ContentHash = GetInspectorHash();
        }

        void CachePreviewTexture()
        {
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(m_AssetPath);
            if (spriteAtlas != null)
            {
                bool hasPreview = m_PreviewTextures != null && m_PreviewTextures.Length > 0;
                if (hasPreview)
                {
                    foreach (var previewTexture in m_PreviewTextures)
                        hasPreview = previewTexture != null;
                }
                if (!hasPreview || m_Hash != spriteAtlas.GetHash())
                {
                    m_PreviewTextures = spriteAtlas.GetPreviewTextures();
                    m_PreviewAlphaTextures = spriteAtlas.GetPreviewAlphaTextures();
                    m_Hash = spriteAtlas.GetHash();

                    if (m_PreviewTextures != null
                        && m_PreviewTextures.Length > 0
                        && m_TotalPages != m_PreviewTextures.Length)
                    {
                        m_TotalPages = m_PreviewTextures.Length;
                        m_OptionDisplays = new string[m_TotalPages];
                        m_OptionValues = new int[m_TotalPages];
                        for (int i = 0; i < m_TotalPages; ++i)
                        {
                            string texName = m_PreviewTextures[i].name;
                            var pageNum = SpriteAtlasExtensions.GetPageNumberInAtlas(texName);
                            var secondaryName = SpriteAtlasExtensions.GetSecondaryTextureNameInAtlas(texName);
                            m_OptionDisplays[i] = secondaryName == "" ? string.Format("MainTex - Page ({0})", pageNum) : string.Format("{0} - Page ({1})", secondaryName, pageNum);
                            m_OptionValues[i] = i;
                        }
                    }
                }
            }
        }

        public override string GetInfoString()
        {
            if (m_PreviewTextures != null && m_PreviewPage < m_PreviewTextures.Length)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];
                GraphicsFormat format = GraphicsFormatUtility.GetFormat(t);

                return string.Format("{0}x{1} {2}\n{3}", t.width, t.height, GraphicsFormatUtility.GetFormatString(format), EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(t)));
            }
            return "";
        }

        public override bool HasPreviewGUI()
        {
            CachePreviewTexture();
            if (m_PreviewTextures != null && m_PreviewTextures.Length > 0)
            {
                Texture2D t = m_PreviewTextures[0];
                return t != null;
            }
            return false;
        }

        public override void OnPreviewSettings()
        {
            // Do not allow changing of pages when multiple atlases is selected.
            if (targets.Length == 1 && m_OptionDisplays != null && m_OptionValues != null && m_TotalPages > 1)
                m_PreviewPage = EditorGUILayout.IntPopup(m_PreviewPage, m_OptionDisplays, m_OptionValues, styles.preDropDown, GUILayout.MaxWidth(50));
            else
                m_PreviewPage = 0;

            if (m_PreviewTextures != null)
            {
                m_PreviewPage = Mathf.Min(m_PreviewPage, m_PreviewTextures.Length - 1);

                Texture2D t = m_PreviewTextures[m_PreviewPage];
                if (t == null)
                    return;

                if (GraphicsFormatUtility.HasAlphaChannel(t.format) || (m_PreviewAlphaTextures != null && m_PreviewAlphaTextures.Length > 0))
                    m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? styles.alphaIcon : styles.RGBIcon, styles.previewButton);

                int mipCount = Mathf.Max(1, TextureUtil.GetMipmapCount(t));
                if (mipCount > 1)
                {
                    GUILayout.Box(styles.smallZoom, styles.previewLabel);
                    m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, styles.previewSlider, styles.previewSliderThumb, GUILayout.MaxWidth(64)));
                    GUILayout.Box(styles.largeZoom, styles.previewLabel);
                }
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            CachePreviewTexture();

            if (m_ShowAlpha && m_PreviewAlphaTextures != null && m_PreviewPage < m_PreviewAlphaTextures.Length)
            {
                var at = m_PreviewAlphaTextures[m_PreviewPage];
                var bias = m_MipLevel - (float)(System.Math.Log(at.width / r.width) / System.Math.Log(2));

                EditorGUI.DrawTextureTransparent(r, at, ScaleMode.ScaleToFit, 0, bias);
            }
            else if (m_PreviewTextures != null && m_PreviewPage < m_PreviewTextures.Length)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];
                if (t == null)
                    return;

                float bias = m_MipLevel - (float)(System.Math.Log(t.width / r.width) / System.Math.Log(2));

                if (m_ShowAlpha)
                    EditorGUI.DrawTextureAlpha(r, t, ScaleMode.ScaleToFit, 0, bias);
                else
                    EditorGUI.DrawTextureTransparent(r, t, ScaleMode.ScaleToFit, 0, bias);
            }
        }
    }
}
