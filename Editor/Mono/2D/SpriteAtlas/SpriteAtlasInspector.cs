// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.U2D
{
    [CustomEditor(typeof(SpriteAtlas))]
    [CanEditMultipleObjects]
    internal class SpriteAtlasInspector : Editor
    {
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
            public readonly GUIContent masterAtlasLabel = EditorGUIUtility.TextContent("Master Atlas|Assigning another Sprite Atlas asset will make this atlas a variant of it.");
            public readonly GUIContent bindAsDefaultLabel = EditorGUIUtility.TextContent("Include in build|Packed textures will be included in the build by default.");
            public readonly GUIContent enableRotationLabel = EditorGUIUtility.TextContent("Allow Rotation|Try rotate the sprite to fit better during packing.");
            public readonly GUIContent enableTightPackingLabel = EditorGUIUtility.TextContent("Tight Packing|Use the mesh outline to fit instead of the whole texture rect during packing.");
            public readonly GUIContent maxTextureSizeLabel = EditorGUIUtility.TextContent("Max Texture Size|Maximum size of the packed texture.");
            public readonly GUIContent generateMipMapLabel = EditorGUIUtility.TextContent("Generate Mip Maps.");
            public readonly GUIContent readWrite = EditorGUIUtility.TextContent("Read/Write Enabled|Enable to be able to access the raw pixel data from code.");
            public readonly GUIContent variantMultiplierLabel = EditorGUIUtility.TextContent("Scale|Down scale ratio.");
            public readonly GUIContent packButton = EditorGUIUtility.TextContent("Pack Preview|Pack this atlas.");
            public readonly GUIContent disabledPackLabel = EditorGUIUtility.TextContent("Sprite Atlas packing is disabled. Enable it in Edit > Project Settings > Editor.");
            public readonly GUIContent packableListLabel = EditorGUIUtility.TextContent("Objects For Packing|Only accept Folder, Sprite Sheet(Texture) and Sprite.");

            public readonly GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public readonly GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public readonly GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public readonly GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");

            public readonly int packableElementHash = "PackableElement".GetHashCode();
            public readonly int packableSelectorHash = "PackableSelector".GetHashCode();

            public readonly int[] kMaxTextureSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
            public readonly GUIContent[] kMaxTextureSizeStrings;

            public Styles()
            {
                dropzoneStyle.alignment = TextAnchor.MiddleCenter;
                dropzoneStyle.border = new RectOffset(10, 10, 10, 10);

                kMaxTextureSizeStrings = new GUIContent[kMaxTextureSizeValues.Length];
                for (var i = 0; i < kMaxTextureSizeValues.Length; ++i)
                    kMaxTextureSizeStrings[i] = EditorGUIUtility.TextContent(string.Format("{0}", kMaxTextureSizeValues[i]));
            }
        }

        static Styles s_Styles;

        private SerializedProperty m_MaxTextureSize;
        private SerializedProperty m_TextureCompression;
        private SerializedProperty m_ColorSpace;
        private SerializedProperty m_FilterMode;
        private SerializedProperty m_AnisoLevel;
        private SerializedProperty m_GenerateMipMaps;
        private SerializedProperty m_Readable;
        private SerializedProperty m_EnableTightPacking;
        private SerializedProperty m_EnableRotation;
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

        private bool m_PackableListExpanded = true;
        private ReorderableList m_PackableList;

        private float m_MipLevel = 0;
        private bool m_ShowAlpha;

        private SpriteAtlas spriteAtlas { get { return target as SpriteAtlas; } }

        static bool IsPackable(Object o)
        {
            return o != null && (o.GetType() == typeof(Sprite) || o.GetType() == typeof(DefaultAsset) || o.GetType() == typeof(Texture2D));
        }

        static Object ValidateObjectForPackableFieldAssignment(Object[] references, System.Type objType, SerializedProperty property)
        {
            // We only validate and care about the first one as this is a object field assignment.
            if (references.Length > 0 && IsPackable(references[0]))
                return references[0];
            return null;
        }

        bool AllTargetsAreVariant()
        {
            foreach (var t in targets)
            {
                SpriteAtlas sa = t as SpriteAtlas;
                if (!sa.isVariant)
                    return false;
            }
            return true;
        }

        bool AllTargetsAreMaster()
        {
            foreach (var t in targets)
            {
                SpriteAtlas sa = t as SpriteAtlas;
                if (sa.isVariant)
                    return false;
            }
            return true;
        }

        void OnEnable()
        {
            m_MaxTextureSize = serializedObject.FindProperty("m_EditorData.textureSettings.maxTextureSize");
            m_TextureCompression = serializedObject.FindProperty("m_EditorData.textureSettings.textureCompression");
            m_ColorSpace = serializedObject.FindProperty("m_EditorData.textureSettings.colorSpace");
            m_FilterMode = serializedObject.FindProperty("m_EditorData.textureSettings.filterMode");
            m_AnisoLevel = serializedObject.FindProperty("m_EditorData.textureSettings.anisoLevel");
            m_GenerateMipMaps = serializedObject.FindProperty("m_EditorData.textureSettings.generateMipMaps");
            m_Readable = serializedObject.FindProperty("m_EditorData.textureSettings.readable");

            m_EnableTightPacking = serializedObject.FindProperty("m_EditorData.packingParameters.enableTightPacking");
            m_EnableRotation = serializedObject.FindProperty("m_EditorData.packingParameters.enableRotation");

            m_Hash = serializedObject.FindProperty("m_EditorData.hashString").stringValue;
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
        }

        void AddPackable(ReorderableList list)
        {
            ObjectSelector.get.Show(null, typeof(Object), null, false);
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
                    spriteAtlas.Remove(new Object[] {previousObject});
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
                    spriteAtlas.Pack(EditorUserBuildSettings.activeBuildTarget);
            }
            else
            {
                EditorGUILayout.HelpBox(s_Styles.disabledPackLabel.text, MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleCommonSettingUI()
        {
            EditorGUI.BeginChangeCheck();
            var masterAtlas = EditorGUILayout.ObjectField(s_Styles.masterAtlasLabel, m_MasterAtlas.objectReferenceValue, typeof(SpriteAtlas), false) as SpriteAtlas;
            if (EditorGUI.EndChangeCheck())
                spriteAtlas.SetMasterSpriteAtlas(masterAtlas);

            EditorGUILayout.PropertyField(m_BindAsDefault, s_Styles.bindAsDefaultLabel);
        }

        private void HandleVariantSettingUI()
        {
            EditorGUILayout.LabelField(s_Styles.variantSettingLabel, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_VariantMultiplier, s_Styles.variantMultiplierLabel);
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

            GUILayout.Space(EditorGUI.kSpacing);
        }

        private void HandleTextureSettingUI()
        {
            EditorGUILayout.LabelField(s_Styles.textureSettingLabel, EditorStyles.boldLabel);

            if (AllTargetsAreMaster())
                EditorGUILayout.IntPopup(m_MaxTextureSize, s_Styles.kMaxTextureSizeStrings, s_Styles.kMaxTextureSizeValues, s_Styles.maxTextureSizeLabel);

            EditorGUILayout.PropertyField(m_TextureCompression);
            EditorGUILayout.PropertyField(m_ColorSpace);
            HandleBoolToIntPropertyField(m_Readable, s_Styles.readWrite);
            HandleBoolToIntPropertyField(m_GenerateMipMaps, s_Styles.generateMipMapLabel);
            EditorGUILayout.PropertyField(m_FilterMode);

            var showAniso = !m_FilterMode.hasMultipleDifferentValues && !m_GenerateMipMaps.hasMultipleDifferentValues
                && (FilterMode)m_FilterMode.intValue != FilterMode.Point && m_GenerateMipMaps.boolValue;
            if (showAniso)
                EditorGUILayout.IntSlider(m_AnisoLevel, 0, 16);
        }

        private void HandlePackableListUI()
        {
            // EditorGUI.Foldout will use the DragUpdated event after a short time. We need the Drag events perform longer than that.
            var eventBeforeFoldout = new Event(Event.current);

            Rect rect = EditorGUILayout.GetControlRect();
            m_PackableListExpanded = EditorGUI.Foldout(rect, m_PackableListExpanded, s_Styles.packableListLabel, true);

            var controlID = EditorGUIUtility.s_LastControlID;
            switch (eventBeforeFoldout.type)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(eventBeforeFoldout.mousePosition) && GUI.enabled)
                    {
                        // Check each single object, so we can add multiple objects in a single drag.
                        var didAcceptDrag = false;
                        var references = DragAndDrop.objectReferences;
                        foreach (var obj in references)
                        {
                            if (IsPackable(obj))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (eventBeforeFoldout.type == EventType.DragPerform)
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
                            eventBeforeFoldout.Use();
                        }
                    }
                    break;
                case EventType.ValidateCommand:
                    if (eventBeforeFoldout.commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == s_Styles.packableSelectorHash)
                        eventBeforeFoldout.Use();
                    break;
                case EventType.ExecuteCommand:
                    if (eventBeforeFoldout.commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == s_Styles.packableSelectorHash)
                    {
                        var obj = ObjectSelector.GetCurrentObject();
                        if (IsPackable(obj))
                        {
                            m_Packables.AppendFoldoutPPtrValue(obj);
                            m_PackableList.index = m_Packables.arraySize - 1;
                        }

                        eventBeforeFoldout.Use();
                    }
                    break;
            }

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

            if (m_PreviewTextures != null)
            {
                Texture2D t = m_PreviewTextures[m_PreviewPage];

                if (TextureUtil.HasAlphaTextureFormat(t.format))
                    m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? s_Styles.alphaIcon : s_Styles.RGBIcon, s_Styles.previewButton);

                int mipCount = Mathf.Max(1, TextureUtil.GetMipmapCount(t));
                using (new EditorGUI.DisabledGroupScope(mipCount == 1))
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
            if (m_PreviewTextures != null)
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
