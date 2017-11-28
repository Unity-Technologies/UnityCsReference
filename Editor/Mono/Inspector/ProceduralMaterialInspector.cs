// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// SUBSTANCE HOOK

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.Build;

#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes

namespace UnityEditor
{
    [CustomEditor(typeof(ProceduralMaterial))]
    [CanEditMultipleObjects]
    internal class ProceduralMaterialInspector : MaterialEditor
    {
        private static ProceduralMaterial m_Material = null;
        private static SubstanceImporter m_Importer  = null;
        private static string[] kMaxTextureSizeStrings = { "32", "64", "128", "256", "512", "1024", "2048" };
        private static int[]    kMaxTextureSizeValues  = { 32, 64, 128, 256, 512, 1024, 2048 };
        private bool m_AllowTextureSizeModification = false;
        private bool m_ShowTexturesSection = false;
        private bool m_ShowHSLInputs       = true;
        private Styles m_Styles;
        private static string[] kMaxLoadBehaviorStrings = { "Do nothing", "Do nothing and cache", "Build on level load", "Build on level load and cache", "Bake and keep Substance", "Bake and discard Substance" };
        private static int[]    kMaxLoadBehaviorValues  = { 0, 5, 1, 4, 2, 3 };
        private static string[] kTextureFormatStrings   = { "Compressed", "Compressed - No Alpha", "RAW", "RAW - No Alpha" };
        private static int[]    kTextureFormatValues    = { 0, 2, 1, 3 };
        private        bool m_MightHaveModified = false;
        private static bool m_UndoWasPerformed  = false;

        private static Dictionary<ProceduralMaterial, float> m_GeneratingSince = new Dictionary<ProceduralMaterial, float>();

        private bool m_ReimportOnDisable = true;

        private class Styles
        {
            public GUIContent hslContent                = new GUIContent("HSL Adjustment", "Hue_Shift, Saturation, Luminosity");
            public GUIContent randomSeedContent         = new GUIContent("Random Seed", "$randomseed : the overall random aspect of the texture.");
            public GUIContent randomizeButtonContent    = new GUIContent("Randomize");
            public GUIContent generateAllOutputsContent = new GUIContent("Generate all outputs", "Force the generation of all Substance outputs.");
            public GUIContent animatedContent           = new GUIContent("Animation update rate", "Set the animation update rate in millisecond");
            public GUIContent defaultPlatform           = EditorGUIUtility.TextContent("Default");
            public GUIContent targetWidth               = new GUIContent("Target Width");
            public GUIContent targetHeight              = new GUIContent("Target Height");
            public GUIContent textureFormat             = EditorGUIUtility.TextContent("Format");
            public GUIContent loadBehavior              = new GUIContent("Load Behavior");
            public GUIContent mipmapContent             = new GUIContent("Generate Mip Maps");
        }

        // A ProceduralMaterialInspector is either created by a multi-material SubstanceImporterInspector,
        // or in standalone mode. There is no way a ProceduralInspector can be moved from "parented by a
        // SubstanceImporterInspector" to standalone mode in its lifetime.
        // As a consequence, this is a one-way trip, no 'Enable' is needed for this.
        public void DisableReimportOnDisable()
        {
            m_ReimportOnDisable = false;
        }

        public void ReimportSubstances()
        {
            string[] asset_names = new string[targets.GetLength(0)];
            int i = 0;
            foreach (ProceduralMaterial material in targets)
            {
                asset_names[i++] = AssetDatabase.GetAssetPath(material);
            }
            for (int j = 0; j < i; ++j)
            {
                SubstanceImporter importer = AssetImporter.GetAtPath(asset_names[j]) as SubstanceImporter;
                if (importer && EditorUtility.IsDirty(importer.GetInstanceID()))
                {
                    AssetDatabase.ImportAsset(asset_names[j], ImportAssetOptions.ForceUncompressedImport);
                }
            }
        }

        public override void Awake()
        {
            base.Awake();
            m_ShowTexturesSection = EditorPrefs.GetBool("ProceduralShowTextures", false);
            m_ReimportOnDisable = true;
            // after undo, object is deselected because of reimport
            // ensure all outputs are up-to-date when reselecting after an undo or redo
            if (m_UndoWasPerformed)
            {
                m_UndoWasPerformed = false;
                OnShaderChanged();
            }
            m_UndoWasPerformed = false;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void ReimportSubstancesIfNeeded()
        {
            if (m_MightHaveModified && !m_UndoWasPerformed)
            {
                // Reimport if required
                if (!EditorApplication.isPlaying && !InternalEditorUtility.ignoreInspectorChanges)
                    ReimportSubstances();
            }
        }

        public override void OnDisable()
        {
            // Warn the user if there are some unapplied import settings (output size / format / load behaviour)
            // This mimics what the other asset importers do, except the ProceduralMaterialInspector does not derive
            // from AssetImporter, so it's done manually here (case 707737)
            ProceduralMaterial material = target as ProceduralMaterial;
            if (material && m_PlatformSettings != null && HasModified())
            {
                string dialogText = "Unapplied import settings for \'" + AssetDatabase.GetAssetPath(target) + "\'";
                if (EditorUtility.DisplayDialog("Unapplied import settings", dialogText, "Apply", "Revert"))
                {
                    Apply();
                    ReimportSubstances();
                }
                ResetValues();
            }

            // If this inspector is a sub-inspector of a SubstanceImporterInspector, then we do not reimport the substances right now,
            // but when the parent SubstanceImporterInspector is deleted instead.
            if (m_ReimportOnDisable)
                ReimportSubstancesIfNeeded();
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            base.OnDisable();
        }

        public override void UndoRedoPerformed()
        {
            m_UndoWasPerformed = true;
            if (m_Material != null)
            {
                m_Material.RebuildTextures();
            }
            base.UndoRedoPerformed();
        }

        protected override void OnShaderChanged()
        {
            // trigger reimport so that generated textures are up-to-date
            foreach (ProceduralMaterial mat in targets)
            {
                string matPath = AssetDatabase.GetAssetPath(mat);
                SubstanceImporter importer = AssetImporter.GetAtPath(matPath) as SubstanceImporter;
                // some targets may be null when first selecting after reimport
                if (importer != null && mat != null)
                    importer.OnShaderModified(mat);
            }
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector or
        // for materials extracted from asset bundles (i.e. where no importer is available)
        internal void DisplayRestrictedInspector()
        {
            m_MightHaveModified = false;

            if (m_Styles == null)
                m_Styles = new Styles();

            // Procedural Material
            ProceduralMaterial material = target as ProceduralMaterial;
            // Set current material to test for changes later on
            if (m_Material != material)
            {
                m_Material = material;
            }

            ProceduralProperties();
            GUILayout.Space(15);
            GeneratedTextures();
        }

        internal override void OnAssetStoreInspectorGUI()
        {
            DisplayRestrictedInspector();
        }

        // Don't disable the gui based on hideFlags of the targets because this editor applies changes
        // back to the importer, so it's an exception. We still want to respect IsOpenForEdit() though.
        internal override bool IsEnabled() { return IsOpenForEdit(); }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            // Procedural Material
            ProceduralMaterial material = target as ProceduralMaterial;

            // Retrieve the Substance importer
            string path = AssetDatabase.GetAssetPath(target);
            m_Importer = AssetImporter.GetAtPath(path) as SubstanceImporter;

            // In case the user somehow created a ProceduralMaterial manually outside of the importer
            if (m_Importer == null)
                return;

            string materialName = material.name;
            materialName = EditorGUI.DelayedTextField(titleRect, materialName, EditorStyles.textField);
            if (materialName != material.name)
            {
                if (m_Importer.RenameMaterial(material, materialName))
                {
                    AssetDatabase.ImportAsset(m_Importer.assetPath, ImportAssetOptions.ForceUncompressedImport);
                    GUIUtility.ExitGUI();
                }
                else
                {
                    materialName = material.name;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(AnimationMode.InAnimationMode()))
            {
                m_MightHaveModified = true;

                if (m_Styles == null)
                    m_Styles = new Styles();

                // Procedural Material
                ProceduralMaterial material = target as ProceduralMaterial;

                // Retrieve the Substance importer
                string path = AssetDatabase.GetAssetPath(target);
                m_Importer = AssetImporter.GetAtPath(path) as SubstanceImporter;

                // In case the user somehow created a ProceduralMaterial manually outside of the importer
                if (m_Importer == null)
                {
                    DisplayRestrictedInspector();
                    return;
                }

                // Set current material to test for changes later on
                if (m_Material != material)
                {
                    m_Material = material;
                }

                // Show Material header that also works as foldout
                if (!isVisible || material.shader == null)
                {
                    return;
                }

                // Show standard GUI without substance textures
                if (PropertiesGUI())
                {
                    OnShaderChanged();
                    PropertiesChanged();
                }

                // Show input header
                GUILayout.Space(5);
                ProceduralProperties();
                GUILayout.Space(15);
                GeneratedTextures();
            }
        }

        void ProceduralProperties()
        {
            GUILayout.Label("Procedural Properties", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Ensure that materials are updated
            foreach (ProceduralMaterial mat in targets)
            {
                if (mat.isProcessing)
                {
                    Repaint();
                    SceneView.RepaintAll();
                    GameView.RepaintAll();
                    break;
                }
            }

            if (targets.Length > 1)
            {
                GUILayout.Label("Procedural properties do not support multi-editing.", EditorStyles.wordWrappedLabel);
                return;
            }

            // Reset label and field widths
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;

            // Show inputs

            if (m_Importer != null)
            {
                // Display warning if substance is not supported
                if (!ProceduralMaterial.isSupported)
                {
                    GUILayout.Label("Procedural Materials are not supported on " + EditorUserBuildSettings.activeBuildTarget + ". Textures will be baked.",
                        EditorStyles.helpBox, GUILayout.ExpandWidth(true));
                }

                // Do not track GenerateAllOutputs/Mipmaps/AnimationUpdateRate as undo-able stuff
                // Changing these actually triggers a reimport form inside the inspector, and this cannot be re-done by the Undo system
                // when undo-ing a change to these properties
                bool changed = GUI.changed;
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    { // Generate all outputs flag
                        EditorGUI.BeginChangeCheck();
                        bool value = EditorGUILayout.Toggle(m_Styles.generateAllOutputsContent, m_Importer.GetGenerateAllOutputs(m_Material));
                        if (EditorGUI.EndChangeCheck())
                            m_Importer.SetGenerateAllOutputs(m_Material, value);
                    }
                    { // Mipmap generation flag
                        EditorGUI.BeginChangeCheck();
                        bool value = EditorGUILayout.Toggle(m_Styles.mipmapContent, m_Importer.GetGenerateMipMaps(m_Material));
                        if (EditorGUI.EndChangeCheck())
                            m_Importer.SetGenerateMipMaps(m_Material, value);
                    }
                }
                if (m_Material.HasProceduralProperty("$time"))
                { // Animation update rate
                    EditorGUI.BeginChangeCheck();
                    int value = EditorGUILayout.IntField(m_Styles.animatedContent, m_Importer.GetAnimationUpdateRate(m_Material));
                    if (EditorGUI.EndChangeCheck())
                        m_Importer.SetAnimationUpdateRate(m_Material, value);
                }
                GUI.changed = changed;
            }
            InputOptions(m_Material);
        }

        void GeneratedTextures()
        {
            if (targets.Length > 1)
                return;

            ProceduralPropertyDescription[] inputs = m_Material.GetProceduralPropertyDescriptions();
            foreach (ProceduralPropertyDescription input in inputs)
            {
                if (input.name == "$outputsize")
                {
                    m_AllowTextureSizeModification = true;
                    break;
                }
            }

            // Generated Textures foldout
            string header = "Generated Textures";

            // Ensure that textures are updated
            if (ShowIsGenerating(target as ProceduralMaterial))
                header += " (Generating...)";

            EditorGUI.BeginChangeCheck();
            m_ShowTexturesSection = EditorGUILayout.Foldout(m_ShowTexturesSection, header, true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool("ProceduralShowTextures", m_ShowTexturesSection);

            // Generated Textures section
            if (m_ShowTexturesSection)
            {
                // Show textures
                ShowProceduralTexturesGUI(m_Material);
                ShowGeneratedTexturesGUI(m_Material);

                if (m_Importer != null)
                {
                    // Show texture offset
                    if (HasProceduralTextureProperties(m_Material))
                        OffsetScaleGUI(m_Material);

                    GUILayout.Space(5f);

                    // Do not allow texture modification if in play mode
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                    {
                        ShowTextureSizeGUI();
                    }
                }
            }
        }

        public static bool ShowIsGenerating(ProceduralMaterial mat)
        {
            if (!m_GeneratingSince.ContainsKey(mat))
                m_GeneratingSince[mat] = 0;
            if (mat.isProcessing)
                return (Time.realtimeSinceStartup > m_GeneratingSince[mat] + 0.4f);
            else
                m_GeneratingSince[mat] = Time.realtimeSinceStartup;
            return false;
        }

        public override string GetInfoString()
        {
            ProceduralMaterial material = target as ProceduralMaterial;
            Texture[] textures = material.GetGeneratedTextures();
            if (textures.Length == 0)
                return string.Empty;
            return textures[0].width + "x" + textures[0].height;
        }

        public bool HasProceduralTextureProperties(Material material)
        {
            Shader shader = material.shader;
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string name = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = material.GetTexture(name);

                if (SubstanceImporter.IsProceduralTextureSlot(material, tex, name))
                    return true;
            }
            return false;
        }

        protected void RecordForUndo(ProceduralMaterial material, SubstanceImporter importer, string message)
        {
            if (importer)
                Undo.RecordObjects(new Object[] { material, importer }, message);
            else
                Undo.RecordObject(material, message);
        }

        protected void OffsetScaleGUI(ProceduralMaterial material)
        {
            if (m_Importer == null || targets.Length > 1)
                return;

            Vector2 scale = m_Importer.GetMaterialScale(material);
            Vector2 offset = m_Importer.GetMaterialOffset(material);
            Vector4 scaleAndOffset = new Vector4(scale.x, scale.y, offset.x, offset.y);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            Rect rect = GUILayoutUtility.GetRect(100, 10000, 2 * EditorGUI.kSingleLineHeight, 2 * EditorGUI.kSingleLineHeight);
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            scaleAndOffset = TextureScaleOffsetProperty(rect, scaleAndOffset);
            if (EditorGUI.EndChangeCheck())
            {
                RecordForUndo(material, m_Importer, "Modify " + material.name + "'s Tiling/Offset");
                m_Importer.SetMaterialScale(material, new Vector2(scaleAndOffset.x, scaleAndOffset.y));
                m_Importer.SetMaterialOffset(material, new Vector2(scaleAndOffset.z, scaleAndOffset.w));
            }
        }

        protected void InputOptions(ProceduralMaterial material)
        {
            EditorGUI.BeginChangeCheck();
            InputsGUI();

            if (EditorGUI.EndChangeCheck())
            {
                material.RebuildTextures();
            }
        }

        [MenuItem("CONTEXT/ProceduralMaterial/Reset", false, -100)]
        public static void ResetSubstance(MenuCommand command)
        {
            // Retrieve the Substance importer
            string path = AssetDatabase.GetAssetPath(command.context);
            m_Importer = AssetImporter.GetAtPath(path) as SubstanceImporter;
            // Reset substance
            m_Importer.ResetMaterial(command.context as ProceduralMaterial);
        }

        private static void ExportBitmaps(ProceduralMaterial material, bool alphaRemap)
        {
            // Select the output folder
            string exportPath = EditorUtility.SaveFolderPanel("Set bitmap export path...", "", "");
            if (exportPath == "")
                return;

            // Retrieve the Substance importer
            string assetPath = AssetDatabase.GetAssetPath(material);
            SubstanceImporter importer = AssetImporter.GetAtPath(assetPath) as SubstanceImporter;
            // This can be null for substances that are previewed from the asset store for instance
            if (importer)
            {
                // Export the bitmaps
                importer.ExportBitmaps(material, exportPath, alphaRemap);
            }
        }

        [MenuItem("CONTEXT/ProceduralMaterial/Export Bitmaps (remapped alpha channels)", false)]
        public static void ExportBitmapsAlphaRemap(MenuCommand command)
        {
            ExportBitmaps(command.context as ProceduralMaterial, true);
        }

        [MenuItem("CONTEXT/ProceduralMaterial/Export Bitmaps (original alpha channels)", false)]
        public static void ExportBitmapsNoAlphaRemap(MenuCommand command)
        {
            ExportBitmaps(command.context as ProceduralMaterial, false);
        }

        [MenuItem("CONTEXT/ProceduralMaterial/Export Preset", false)]
        public static void ExportPreset(MenuCommand command)
        {
            // Select the output folder
            string exportPath = EditorUtility.SaveFolderPanel("Set preset export path...", "", "");
            if (exportPath == "")
                return;

            // Retrieve the Substance importer
            ProceduralMaterial material = command.context as ProceduralMaterial;
            string assetPath = AssetDatabase.GetAssetPath(material);
            SubstanceImporter importer = AssetImporter.GetAtPath(assetPath) as SubstanceImporter;
            // This can be null for substances that are previewed from the asset store for instance
            if (importer)
            {
                // Export the preset string
                importer.ExportPreset(material, exportPath);
            }
        }

        protected void ShowProceduralTexturesGUI(ProceduralMaterial material)
        {
            if (targets.Length > 1)
                return;

            EditorGUILayout.Space();

            // Show textures
            Shader shader = material.shader;
            if (shader == null)
                return;
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(4);
            GUILayout.FlexibleSpace();

            float spacing = 10;
            bool first = true;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                // Only show texture properties
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string name = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = material.GetTexture(name);

                // Only show substance textures
                if (!SubstanceImporter.IsProceduralTextureSlot(material, tex, name))
                    continue;

                string label = ShaderUtil.GetPropertyDescription(shader, i);

                UnityEngine.Rendering.TextureDimension desiredTexdim = ShaderUtil.GetTexDim(shader, i);
                System.Type t = MaterialEditor.GetTextureTypeFromDimension(desiredTexdim);

                // TODO: Move into styles class
                GUIStyle styleLabel = "ObjectPickerResultsGridLabel";

                if (first)
                    first = false;
                else
                    GUILayout.Space(spacing);

                GUILayout.BeginVertical(GUILayout.Height(72 + styleLabel.fixedHeight + styleLabel.fixedHeight + 8));

                Rect rect = EditorGUILayoutUtilityInternal.GetRect(72, 72);

                // Create object field with no "texture drop-box"
                DoObjectPingField(rect, rect, EditorGUIUtility.GetControlID(12354, FocusType.Keyboard, rect), tex, t);
                ShowAlphaSourceGUI(material, tex as ProceduralTexture, ref rect);

                rect.height = styleLabel.fixedHeight;
                GUI.Label(rect, label, styleLabel);

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        Vector2 m_ScrollPos = new Vector2();
        protected void ShowGeneratedTexturesGUI(ProceduralMaterial material)
        {
            if (targets.Length > 1)
                return;

            if (m_Importer != null && !m_Importer.GetGenerateAllOutputs(m_Material))
                return;

            GUIStyle styleLabel = "ObjectPickerResultsGridLabel";
            EditorGUILayout.Space();
            GUILayout.FlexibleSpace();
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(64 + styleLabel.fixedHeight + styleLabel.fixedHeight + 16));
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            float spacing = 10;
            Texture[] textures = material.GetGeneratedTextures();
            foreach (Texture texture in textures)
            {
                ProceduralTexture procTex = texture as ProceduralTexture;
                if (procTex != null)
                {
                    // This hard space is there so that textures do not touch even when the inspector is really narrow,
                    // even if we are already in a FlexibleSpace-enclosed block.
                    GUILayout.Space(spacing);

                    GUILayout.BeginVertical(GUILayout.Height(64 + styleLabel.fixedHeight + 8));

                    Rect rect = EditorGUILayoutUtilityInternal.GetRect(64, 64);

                    // Create object field with no "texture drop-box"
                    DoObjectPingField(rect, rect, EditorGUIUtility.GetControlID(12354, FocusType.Keyboard, rect), procTex, typeof(Texture));
                    ShowAlphaSourceGUI(material, procTex, ref rect);

                    GUILayout.EndVertical();

                    // This hard space is there so that textures do not touch even when the inspector is really narrow,
                    // even if we are already in a FlexibleSpace-enclosed block.
                    GUILayout.Space(spacing);

                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        void ShowAlphaSourceGUI(ProceduralMaterial material, ProceduralTexture tex, ref Rect rect)
        {
            GUIStyle styleLabel = "ObjectPickerResultsGridLabel";
            float spacing = 10;
            rect.y = rect.yMax + 2;
            if (m_Importer != null)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    if (tex.GetProceduralOutputType() != ProceduralOutputType.Normal && tex.hasAlpha)
                    {
                        rect.height = styleLabel.fixedHeight;
                        // create alpha modifier popup
                        string[] m_TextureSrc =
                        {
                            "Source (A)",
                            "Diffuse (A)",
                            "Normal (A)",
                            "Height (A)",
                            "Emissive (A)",
                            "Specular (A)",
                            "Opacity (A)",
                            "Smoothness (A)",
                            "Amb. Occlusion (A)",
                            "Detail Mask (A)",
                            "Metallic (A)",
                            "Roughness (A)"
                        };
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUI.BeginChangeCheck();
                        int alphaSource = EditorGUI.Popup(rect, (int)m_Importer.GetTextureAlphaSource(material, tex.name), m_TextureSrc);
                        if (EditorGUI.EndChangeCheck())
                        {
                            RecordForUndo(material, m_Importer, "Modify " + material.name + "'s Alpha Modifier");
                            m_Importer.SetTextureAlphaSource(material, tex.name, (ProceduralOutputType)alphaSource);
                        }
                        rect.y = rect.yMax + 2;
                    }
                }
            }
            rect.width += spacing;
        }

        // Similar to ObjectField, but does not allow changing the object.
        // It will still ping or select the object when clicked / double-clicked.
        internal static void DoObjectPingField(Rect position, Rect dropRect, int id, Object obj, System.Type objType)
        {
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            bool usePreview = EditorGUIUtility.HasObjectThumbnail(objType) && position.height > EditorGUI.kSingleLineHeight;

            switch (eventType)
            {
                case EventType.MouseDown:
                    // Ignore right clicks
                    if (Event.current.button != 0)
                        break;
                    if (position.Contains(Event.current.mousePosition))
                    {
                        Object actualTargetObject = obj;
                        Component com = actualTargetObject as Component;
                        if (com)
                            actualTargetObject = com.gameObject;

                        // One click shows where the referenced object is
                        if (Event.current.clickCount == 1)
                        {
                            GUIUtility.keyboardControl = id;
                            if (actualTargetObject)
                                EditorGUIUtility.PingObject(actualTargetObject);
                            evt.Use();
                        }
                        // Double click changes selection to referenced object
                        else if (Event.current.clickCount == 2)
                        {
                            if (actualTargetObject)
                            {
                                AssetDatabase.OpenAsset(actualTargetObject);
                                GUIUtility.ExitGUI();
                            }
                            evt.Use();
                        }
                    }
                    break;

                case EventType.Repaint:
                    GUIContent temp = EditorGUIUtility.ObjectContent(obj, objType);
                    if (usePreview)
                    {
                        GUIStyle style = EditorStyles.objectFieldThumb;
                        style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);

                        if (obj != null)
                        {
                            EditorGUI.DrawPreviewTexture(style.padding.Remove(position), temp.image);
                        }
                        else
                        {
                            GUIStyle s2 = style.name + "Overlay";
                            s2.Draw(position, temp, id);
                        }
                    }
                    else
                    {
                        GUIStyle style = EditorStyles.objectField;
                        style.Draw(position, temp, id, DragAndDrop.activeControlID == id);
                    }
                    break;
            }
        }

        internal void ResetValues()
        {
            BuildTargetList();
            if (HasModified())
                Debug.LogError("Impossible");
        }

        internal void Apply()
        {
            foreach (ProceduralPlatformSetting ps in m_PlatformSettings)
            {
                ps.Apply();
            }
        }

        internal bool HasModified()
        {
            foreach (ProceduralPlatformSetting ps in m_PlatformSettings)
            {
                if (ps.HasChanged())
                {
                    return true;
                }
            }

            return false;
        }

        [System.Serializable]
        protected class ProceduralPlatformSetting
        {
            Object[] targets;
            public string name;
            public bool m_Overridden;
            public int maxTextureWidth;
            public int maxTextureHeight;
            public int m_TextureFormat;
            public int m_LoadBehavior;
            public BuildTarget target;
            public Texture2D icon;
            public bool isDefault { get { return name == ""; } }

            public int textureFormat
            {
                get { return m_TextureFormat; }
                set
                {
                    m_TextureFormat = value;
                }
            }

            public ProceduralPlatformSetting(Object[] objects, string _name, BuildTarget _target, Texture2D _icon)
            {
                targets = objects;
                m_Overridden = false;
                target = _target;
                name = _name;
                icon = _icon;
                m_Overridden = false;
                if (name != "")
                {
                    foreach (ProceduralMaterial mat in targets)
                    {
                        string matPath = AssetDatabase.GetAssetPath(mat);
                        SubstanceImporter importer = AssetImporter.GetAtPath(matPath) as SubstanceImporter;
                        if (importer != null && importer.GetPlatformTextureSettings(mat.name, name, out maxTextureWidth, out maxTextureHeight, out m_TextureFormat, out m_LoadBehavior))
                        {
                            m_Overridden = true;
                            break;
                        }
                    }
                }
                if (!m_Overridden && targets.Length > 0)
                {
                    string matPath = AssetDatabase.GetAssetPath(targets[0]);
                    SubstanceImporter importer = AssetImporter.GetAtPath(matPath) as SubstanceImporter;
                    if (importer != null)
                        importer.GetPlatformTextureSettings((targets[0] as ProceduralMaterial).name, "", out maxTextureWidth, out maxTextureHeight, out m_TextureFormat, out m_LoadBehavior);
                }
            }

            public bool overridden
            {
                get
                {
                    return m_Overridden;
                }
            }

            public void SetOverride(ProceduralPlatformSetting master)
            {
                m_Overridden = true;
            }

            public void ClearOverride(ProceduralPlatformSetting master)
            {
                m_TextureFormat = master.textureFormat;
                maxTextureWidth = master.maxTextureWidth;
                maxTextureHeight = master.maxTextureHeight;
                m_LoadBehavior = master.m_LoadBehavior;
                m_Overridden = false;
            }

            public bool HasChanged()
            {
                ProceduralPlatformSetting orig = new ProceduralPlatformSetting(targets, name, target, null);
                return orig.m_Overridden != m_Overridden || orig.maxTextureWidth != maxTextureWidth
                    || orig.maxTextureHeight != maxTextureHeight || orig.textureFormat != textureFormat
                    || orig.m_LoadBehavior != m_LoadBehavior;
            }

            public void Apply()
            {
                foreach (ProceduralMaterial mat in targets)
                {
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    SubstanceImporter importer = AssetImporter.GetAtPath(matPath) as SubstanceImporter;

                    if (name != "")
                    {
                        if (m_Overridden)
                        {
                            importer.SetPlatformTextureSettings(mat, name, maxTextureWidth, maxTextureHeight, m_TextureFormat, m_LoadBehavior);
                        }
                        else
                        {
                            importer.ClearPlatformTextureSettings(mat.name, name);
                        }
                    }
                    else
                    {
                        importer.SetPlatformTextureSettings(mat, name, maxTextureWidth, maxTextureHeight, m_TextureFormat, m_LoadBehavior);
                    }
                }
            }
        }

        protected List<ProceduralPlatformSetting> m_PlatformSettings;

        public void BuildTargetList()
        {
            List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();

            m_PlatformSettings = new List<ProceduralPlatformSetting>();
            m_PlatformSettings.Add(new ProceduralPlatformSetting(targets, "", BuildTarget.StandaloneWindows, null));

            foreach (BuildPlatform bp in validPlatforms)
            {
                m_PlatformSettings.Add(new ProceduralPlatformSetting(targets, bp.name, bp.defaultTarget, bp.smallIcon));
            }
        }

        public void ShowTextureSizeGUI()
        {
            if (m_PlatformSettings == null)
                BuildTargetList();

            TextureSizeGUI();
        }

        protected void TextureSizeGUI()
        {
            BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
            int shownTextureFormatPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, m_Styles.defaultPlatform);
            ProceduralPlatformSetting realPS = m_PlatformSettings[shownTextureFormatPage + 1];
            ProceduralPlatformSetting ps = realPS;

            bool newOverride = true;
            if (realPS.name != "")
            {
                EditorGUI.BeginChangeCheck();
                newOverride = GUILayout.Toggle(realPS.overridden, "Override for " + realPS.name);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newOverride)
                    {
                        realPS.SetOverride(m_PlatformSettings[0]);
                    }
                    else
                    {
                        realPS.ClearOverride(m_PlatformSettings[0]);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!newOverride))
            {
                // Inform the user the texture size cannot be changed (case 693959)
                if (!m_AllowTextureSizeModification)
                    GUILayout.Label("This ProceduralMaterial was published with a fixed size.", EditorStyles.wordWrappedLabel);

                // Do not allow texture size modification if the SBSAR doesn't have an "$outputsize" input (case 693959)
                using (new EditorGUI.DisabledScope(!m_AllowTextureSizeModification))
                {
                    EditorGUI.BeginChangeCheck();
                    ps.maxTextureWidth = EditorGUILayout.IntPopup(m_Styles.targetWidth.text, ps.maxTextureWidth, kMaxTextureSizeStrings, kMaxTextureSizeValues);
                    ps.maxTextureHeight = EditorGUILayout.IntPopup(m_Styles.targetHeight.text, ps.maxTextureHeight, kMaxTextureSizeStrings, kMaxTextureSizeValues);
                    if (EditorGUI.EndChangeCheck() && ps.isDefault)
                    {
                        foreach (ProceduralPlatformSetting psToUpdate in m_PlatformSettings)
                        {
                            if (psToUpdate.isDefault || psToUpdate.overridden) continue;
                            psToUpdate.maxTextureWidth = ps.maxTextureWidth;
                            psToUpdate.maxTextureHeight = ps.maxTextureHeight;
                        }
                    }
                }

                EditorGUI.BeginChangeCheck();
                int tf = (int)ps.textureFormat;
                if (tf < 0 || tf >= kTextureFormatStrings.Length)
                {
                    Debug.LogError("Invalid TextureFormat");
                }
                tf = EditorGUILayout.IntPopup(m_Styles.textureFormat.text, tf, kTextureFormatStrings, kTextureFormatValues);
                if (EditorGUI.EndChangeCheck())
                {
                    ps.textureFormat = tf;
                    if (ps.isDefault)
                    {
                        foreach (ProceduralPlatformSetting psToUpdate in m_PlatformSettings)
                        {
                            if (psToUpdate.isDefault || psToUpdate.overridden) continue;
                            psToUpdate.textureFormat = ps.textureFormat;
                        }
                    }
                }
                EditorGUI.BeginChangeCheck();
                ps.m_LoadBehavior = EditorGUILayout.IntPopup(m_Styles.loadBehavior.text, ps.m_LoadBehavior, kMaxLoadBehaviorStrings, kMaxLoadBehaviorValues);
                if (EditorGUI.EndChangeCheck() && ps.isDefault)
                {
                    foreach (ProceduralPlatformSetting psToUpdate in m_PlatformSettings)
                    {
                        if (psToUpdate.isDefault || psToUpdate.overridden) continue;
                        psToUpdate.m_LoadBehavior = ps.m_LoadBehavior;
                    }
                }

                GUILayout.Space(5);
                using (new EditorGUI.DisabledScope(!HasModified()))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Revert"))
                    {
                        ResetValues();
                    }

                    if (GUILayout.Button("Apply"))
                    {
                        Apply();
                        ReimportSubstances();
                        ResetValues();
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
                EditorGUILayout.EndPlatformGrouping();
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
            if (ShowIsGenerating(target as ProceduralMaterial) && r.width > 50)
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Generating...");
        }

        public void InputsGUI()
        {
            /* Proper input listing is:
               1) Groupless non-image-type parameters
               2) For each parameter group
                  a) Non-image-type parameters
                  b) Image-type parameters
               3) Groupless image-type parameters
            */

            List<string> groupNames = new List<string>();
            Dictionary<string, List<ProceduralPropertyDescription>> nonImageInputsSortedByGroup = new Dictionary<string, List<ProceduralPropertyDescription>>();
            Dictionary<string, List<ProceduralPropertyDescription>> imageInputsSortedByGroup = new Dictionary<string, List<ProceduralPropertyDescription>>();

            ProceduralPropertyDescription[] inputs = m_Material.GetProceduralPropertyDescriptions();

            // Try to catch groupless H/S/L tweaks in order to group them later on
            ProceduralPropertyDescription inputH = null;
            ProceduralPropertyDescription inputS = null;
            ProceduralPropertyDescription inputL = null;

            foreach (ProceduralPropertyDescription input in inputs)
            {
                if (input.name == "$randomseed")
                {
                    InputSeedGUI(input);
                    continue;
                }

                // Skip all '$'-prefixed special parameters
                if (input.name.Length > 0 && input.name[0] == '$')
                    continue;

                // Skip all non-visible inputs
                if (!m_Material.IsProceduralPropertyVisible(input.name))
                    continue;

                string group = input.group;

                // Keep track of all non-empty parameter group names
                if (group != string.Empty && !groupNames.Contains(group))
                    groupNames.Add(group);

                // Keep track of the HSL tweaks if we need to group them later on
                if (input.name == "Hue_Shift" && input.type == ProceduralPropertyType.Float && group == string.Empty)
                    inputH = input;
                if (input.name == "Saturation" && input.type == ProceduralPropertyType.Float && group == string.Empty)
                    inputS = input;
                if (input.name == "Luminosity" && input.type == ProceduralPropertyType.Float && group == string.Empty)
                    inputL = input;

                // Sort the parameters into image and non-image, in dictionaries keyed by the parameter group name
                if (input.type == ProceduralPropertyType.Texture)
                {
                    if (!imageInputsSortedByGroup.ContainsKey(group))
                        imageInputsSortedByGroup.Add(group, new List<ProceduralPropertyDescription>());
                    imageInputsSortedByGroup[group].Add(input);
                }
                else
                {
                    if (!nonImageInputsSortedByGroup.ContainsKey(group))
                        nonImageInputsSortedByGroup.Add(group, new List<ProceduralPropertyDescription>());
                    nonImageInputsSortedByGroup[group].Add(input);
                }
            }

            bool showCustomHSLGroup = false;
            if (inputH != null && inputS != null && inputL != null)
                showCustomHSLGroup = true;

            // Start with non-image groupless parameters
            List<ProceduralPropertyDescription> grouplessNonImageParameters;
            if (nonImageInputsSortedByGroup.TryGetValue(string.Empty, out grouplessNonImageParameters))
            {
                foreach (ProceduralPropertyDescription input in grouplessNonImageParameters)
                {
                    // Don't draw the H/S/L right here if we already know we'll draw a special H/S/L group at the bottom of the inspector
                    if (showCustomHSLGroup && (input == inputH || input == inputS || input == inputL))
                        continue;

                    InputGUI(input);
                }
            }

            // For each group, draw non-image tweaks first, then image tweaks
            foreach (string groupName in groupNames)
            {
                ProceduralMaterial material = target as ProceduralMaterial;
                string materialName = material.name;
                string groupCompleteName = materialName + groupName;

                GUILayout.Space(5);
                bool showGroup = EditorPrefs.GetBool(groupCompleteName, true);
                EditorGUI.BeginChangeCheck();
                showGroup = EditorGUILayout.Foldout(showGroup, groupName, true);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(groupCompleteName, showGroup);
                }

                if (showGroup)
                {
                    EditorGUI.indentLevel++;

                    List<ProceduralPropertyDescription> nonImageParameters;
                    if (nonImageInputsSortedByGroup.TryGetValue(groupName, out nonImageParameters))
                        foreach (ProceduralPropertyDescription input in nonImageParameters)
                            InputGUI(input);

                    List<ProceduralPropertyDescription> imageParameters;
                    if (imageInputsSortedByGroup.TryGetValue(groupName, out imageParameters))
                    {
                        GUILayout.Space(2);
                        foreach (ProceduralPropertyDescription input in imageParameters)
                            InputGUI(input);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            // Draw H/S/L special group if we need to
            if (showCustomHSLGroup)
                InputHSLGUI(inputH, inputS, inputL);

            // Then end the parameter list with groupless image tweaks
            List<ProceduralPropertyDescription> grouplessImageParameters;
            if (imageInputsSortedByGroup.TryGetValue(string.Empty, out grouplessImageParameters))
            {
                GUILayout.Space(5);
                foreach (ProceduralPropertyDescription input in grouplessImageParameters)
                    InputGUI(input);
            }
        }

        private void InputGUI(ProceduralPropertyDescription input)
        {
            ProceduralPropertyType type = input.type;
            GUIContent content = new GUIContent(input.label, input.name);
            switch (type)
            {
                case ProceduralPropertyType.Boolean:
                {
                    EditorGUI.BeginChangeCheck();
                    bool val = EditorGUILayout.Toggle(content, m_Material.GetProceduralBoolean(input.name));
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralBoolean(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.Float:
                {
                    float val;
                    EditorGUI.BeginChangeCheck();
                    if (input.hasRange)
                    {
                        float min = input.minimum;
                        float max = input.maximum;
                        val = EditorGUILayout.Slider(content, m_Material.GetProceduralFloat(input.name), min, max);
                    }
                    else
                    {
                        val = EditorGUILayout.FloatField(content, m_Material.GetProceduralFloat(input.name));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralFloat(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.Vector2:
                case ProceduralPropertyType.Vector3:
                case ProceduralPropertyType.Vector4:
                {
                    int inputCount = (type == ProceduralPropertyType.Vector2 ? 2 : (type == ProceduralPropertyType.Vector3 ? 3 : 4));
                    Vector4 val = m_Material.GetProceduralVector(input.name);

                    EditorGUI.BeginChangeCheck();
                    if (input.hasRange)
                    {
                        float min = input.minimum;
                        float max = input.maximum;
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(EditorGUI.indentLevel * 15);
                        GUILayout.Label(content);
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < inputCount; i++)
                            val[i] = EditorGUILayout.Slider(new GUIContent(input.componentLabels[i]), val[i], min, max);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        switch (inputCount)
                        {
                            case 2: val = EditorGUILayout.Vector2Field(input.name, val); break;
                            case 3: val = EditorGUILayout.Vector3Field(input.name, val); break;
                            case 4: val = EditorGUILayout.Vector4Field(input.name, val); break;
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralVector(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.Color3:
                case ProceduralPropertyType.Color4:
                {
                    EditorGUI.BeginChangeCheck();
                    Color val = EditorGUILayout.ColorField(content, m_Material.GetProceduralColor(input.name));
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralColor(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.Enum:
                {
                    GUIContent[] enumOptions = new GUIContent[input.enumOptions.Length];
                    for (int i = 0; i < enumOptions.Length; ++i)
                    {
                        enumOptions[i] = new GUIContent(input.enumOptions[i]);
                    }
                    EditorGUI.BeginChangeCheck();
                    int val = EditorGUILayout.Popup(content, m_Material.GetProceduralEnum(input.name), enumOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralEnum(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.String:
                {
                    EditorGUI.BeginChangeCheck();
                    string val = EditorGUILayout.TextField(content, m_Material.GetProceduralString(input.name));

                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralString(input.name, val);
                    }
                    break;
                }
                case ProceduralPropertyType.Texture:
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 15);
                    GUILayout.Label(content);
                    GUILayout.FlexibleSpace();
                    Rect R = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                    EditorGUI.BeginChangeCheck();
                    Texture2D val = EditorGUI.DoObjectField(R, R, EditorGUIUtility.GetControlID(12354, FocusType.Keyboard, R), m_Material.GetProceduralTexture(input.name) as Object, typeof(Texture2D), null, null, false) as Texture2D;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordForUndo(m_Material, m_Importer, "Modified property " + input.name + " for material " + m_Material.name);
                        m_Material.SetProceduralTexture(input.name, val);
                    }
                    break;
                }
            }
        }

        private void InputHSLGUI(ProceduralPropertyDescription hInput, ProceduralPropertyDescription sInput, ProceduralPropertyDescription lInput)
        {
            GUILayout.Space(5);
            m_ShowHSLInputs = EditorPrefs.GetBool("ProceduralShowHSL", true);
            EditorGUI.BeginChangeCheck();
            m_ShowHSLInputs = EditorGUILayout.Foldout(m_ShowHSLInputs, m_Styles.hslContent, true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("ProceduralShowHSL", m_ShowHSLInputs);
            }

            if (m_ShowHSLInputs)
            {
                EditorGUI.indentLevel++;
                InputGUI(hInput);
                InputGUI(sInput);
                InputGUI(lInput);
                EditorGUI.indentLevel--;
            }
        }

        private void InputSeedGUI(ProceduralPropertyDescription input)
        {
            Rect r = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            float val = (float)RandomIntField(r, m_Styles.randomSeedContent, (int)m_Material.GetProceduralFloat(input.name), 0, 9999);
            if (EditorGUI.EndChangeCheck())
            {
                RecordForUndo(m_Material, m_Importer, "Modified random seed for material " + m_Material.name);
                m_Material.SetProceduralFloat(input.name, val);
            }
        }

        internal int RandomIntField(Rect position, GUIContent label, int val, int min, int max)
        {
            position = EditorGUI.PrefixLabel(position, 0, label);
            return RandomIntField(position, val, min, max);
        }

        internal int RandomIntField(Rect position, int val, int min, int max)
        {
            position.width = position.width - EditorGUIUtility.fieldWidth - EditorGUI.kSpacing;

            if (GUI.Button(position, m_Styles.randomizeButtonContent, EditorStyles.miniButton))
            {
                val = Random.Range(min, max + 1);
            }

            position.x += position.width + EditorGUI.kSpacing;
            position.width = EditorGUIUtility.fieldWidth;
            val = Mathf.Clamp(EditorGUI.IntField(position, val), min, max);

            return val;
        }
    }
}

#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes
