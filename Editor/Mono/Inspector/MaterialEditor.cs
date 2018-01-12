// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(Material))]
    [CanEditMultipleObjects]
    public partial class MaterialEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIStyle kReflectionProbePickerStyle = "PaneOptions";
            public static readonly GUIContent lightmapEmissiveLabel = EditorGUIUtility.TextContent("Global Illumination|Controls if the emission is baked or realtime.\n\nBaked only has effect in scenes where baked global illumination is enabled.\n\nRealtime uses realtime global illumination if enabled in the scene. Otherwise the emission won't light up other objects.");
            public static GUIContent[] lightmapEmissiveStrings = { EditorGUIUtility.TextContent("Realtime"), EditorGUIUtility.TextContent("Baked"), EditorGUIUtility.TextContent("None") };
            public static int[]  lightmapEmissiveValues = { (int)MaterialGlobalIlluminationFlags.RealtimeEmissive, (int)MaterialGlobalIlluminationFlags.BakedEmissive, (int)MaterialGlobalIlluminationFlags.None };
            public static string propBlockInfo = EditorGUIUtility.TextContent("MaterialPropertyBlock is used to modify these values").text;

            public const int kNewShaderQueueValue = -1;
            public const int kCustomQueueIndex = 4;
            public static readonly GUIContent queueLabel = EditorGUIUtility.TextContent("Render Queue");
            public static readonly GUIContent[] queueNames =
            {
                EditorGUIUtility.TextContent("From Shader"),
                EditorGUIUtility.TextContent("Geometry|Queue 2000"),
                EditorGUIUtility.TextContent("AlphaTest|Queue 2450"),
                EditorGUIUtility.TextContent("Transparent|Queue 3000"),
            };
            public static readonly int[] queueValues =
            {
                kNewShaderQueueValue,
                (int)UnityEngine.Rendering.RenderQueue.Geometry,
                (int)UnityEngine.Rendering.RenderQueue.AlphaTest,
                (int)UnityEngine.Rendering.RenderQueue.Transparent,
            };
            public static GUIContent[] customQueueNames =
            {
                queueNames[0],
                queueNames[1],
                queueNames[2],
                queueNames[3],
                EditorGUIUtility.TextContent(""), // This name will be overriden during runtime
            };
            public static int[] customQueueValues =
            {
                queueValues[0],
                queueValues[1],
                queueValues[2],
                queueValues[3],
                0, // This value will be overriden during runtime
            };

            public static readonly GUIContent enableInstancingLabel = EditorGUIUtility.TextContent("Enable GPU Instancing");
            public static readonly GUIContent doubleSidedGILabel = EditorGUIUtility.TextContent("Double Sided Global Illumination|When enabled, the lightmapper accounts for both sides of the geometry when calculating Global Illumination. Backfaces are not rendered or added to lightmaps, but get treated as valid when seen from other objects. When using the Progressive Lightmapper backfaces bounce light using the same emission and albedo as frontfaces.");
            public static readonly GUIContent emissionLabel = EditorGUIUtility.TextContent("Emission");
        }

        private static readonly List<MaterialEditor> s_MaterialEditors = new List<MaterialEditor>(4);
        private bool m_IsVisible;
        private bool m_CheckSetup;
        internal bool forceVisible { get; set; }
        private static int s_ControlHash = "EditorTextField".GetHashCode();
        const float kSpacingUnderTexture = 6f;
        const float kMiniWarningMessageHeight = 27f;

        private MaterialPropertyBlock m_PropertyBlock;

        private enum PreviewType
        {
            Mesh = 0,
            Plane = 1,
            Skybox = 2
        }

        private static PreviewType GetPreviewType(Material mat)
        {
            if (mat == null)
                return PreviewType.Mesh;
            var tag = mat.GetTag("PreviewType", false, string.Empty).ToLower();
            if (tag == "plane")
                return PreviewType.Plane;
            if (tag == "skybox")
                return PreviewType.Skybox;
            if (mat.shader != null && mat.shader.name.Contains("Skybox"))
                return PreviewType.Skybox;
            return PreviewType.Mesh;
        }

        private static bool DoesPreviewAllowRotation(PreviewType type)
        {
            return type != PreviewType.Plane;
        }

        public bool isVisible { get { return forceVisible || m_IsVisible; } }

        private Shader m_Shader;
        private SerializedProperty m_EnableInstancing;
        private SerializedProperty m_DoubleSidedGI;

        private string                      m_InfoMessage;
        private Vector2                     m_PreviewDir = new Vector2(0, -20);
        private int                         m_SelectedMesh;
        private int                         m_TimeUpdate;
        private int                         m_LightMode = 1;
        private static readonly GUIContent  s_TilingText = new GUIContent("Tiling");
        private static readonly GUIContent  s_OffsetText = new GUIContent("Offset");

        ShaderGUI   m_CustomShaderGUI;
        string      m_CustomEditorClassName;

        bool                                m_InsidePropertiesGUI;
        Renderer[]                          m_RenderersForAnimationMode;

        private Renderer rendererForAnimationMode
        {
            get
            {
                if (m_RenderersForAnimationMode == null)
                    return null;

                if (m_RenderersForAnimationMode.Length == 0)
                    return null;

                return m_RenderersForAnimationMode[0];
            }
        }

        private struct AnimatedCheckData
        {
            public MaterialProperty property;
            public Rect totalPosition;
            public Color color;
            public AnimatedCheckData(MaterialProperty property, Rect totalPosition, Color color)
            {
                this.property = property;
                this.totalPosition = totalPosition;
                this.color = color;
            }
        }

        private static Stack<AnimatedCheckData> s_AnimatedCheckStack = new Stack<AnimatedCheckData>();

        internal delegate void MaterialPropertyCallbackFunction(GenericMenu menu, MaterialProperty property, Renderer[] renderers);
        internal static MaterialPropertyCallbackFunction contextualPropertyMenu;

        internal class ReflectionProbePicker : PopupWindowContent
        {
            ReflectionProbe m_SelectedReflectionProbe;

            public Transform Target
            {
                get { return m_SelectedReflectionProbe != null ? m_SelectedReflectionProbe.transform : null; }
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(170, EditorGUI.kSingleLineHeight * 3);
            }

            public void OnEnable()
            {
                m_SelectedReflectionProbe = EditorUtility.InstanceIDToObject(SessionState.GetInt("PreviewReflectionProbe", 0)) as ReflectionProbe;
            }

            public void OnDisable()
            {
                SessionState.SetInt("PreviewReflectionProbe", m_SelectedReflectionProbe ? m_SelectedReflectionProbe.GetInstanceID() : 0);
            }

            public override void OnGUI(Rect rc)
            {
                EditorGUILayout.LabelField("Select Reflection Probe", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                m_SelectedReflectionProbe = EditorGUILayout.ObjectField("", m_SelectedReflectionProbe, typeof(ReflectionProbe), true) as ReflectionProbe;
            }
        }

        ReflectionProbePicker               m_ReflectionProbePicker = new ReflectionProbePicker();


        public void SetShader(Shader shader)
        {
            SetShader(shader, true);
        }

        public void SetShader(Shader newShader, bool registerUndo)
        {
            bool updateMaterialEditors = false;
            ShaderGUI customEditor = m_CustomShaderGUI;
            CreateCustomShaderEditorIfNeeded(newShader);
            m_Shader = newShader;
            if (customEditor != m_CustomShaderGUI)
            {
                if (customEditor != null)
                {
                    foreach (Material material in targets)
                        customEditor.OnClosed(material);
                }

                updateMaterialEditors = true;
            }

            foreach (Material material in targets)
            {
                Shader oldShader = material.shader;
                Undo.RecordObject(material, "Assign shader");
                if (m_CustomShaderGUI != null)
                {
                    m_CustomShaderGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
                }
                else
                {
                    material.shader = newShader;
                }

                EditorMaterialUtility.ResetDefaultTextures(material, false);
                ApplyMaterialPropertyDrawers(material);
            }

            if (updateMaterialEditors)
            {
                UpdateAllOpenMaterialEditors();
            }
            else
            {
                // ensure e.g., ProceduralMaterialInspector correctly rebuilds textures (case 879446)
                OnShaderChanged();
            }
        }

        private void UpdateAllOpenMaterialEditors()
        {
            // copy current list contents to array in case it changes during iteration
            // e.g., changing shader in ProceduralMaterialInspector will destroy/recreate editors
            foreach (var materialEditor in s_MaterialEditors.ToArray())
                materialEditor.DetectShaderEditorNeedsUpdate();
        }

        // Note: this is called from native code.
        internal void OnSelectedShaderPopup(string command, Shader shader)
        {
            serializedObject.Update();

            if (shader != null)
                SetShader(shader);

            PropertiesChanged();
        }

        private bool HasMultipleMixedShaderValues()
        {
            bool mixed = false;
            Shader sh = (targets[0] as Material).shader;
            for (int i = 1; i < targets.Length; ++i)
            {
                if (sh != (targets[i] as Material).shader)
                {
                    mixed = true;
                    break;
                }
            }
            return mixed;
        }

        private void ShaderPopup(GUIStyle style)
        {
            bool wasEnabled = GUI.enabled;

            Rect position = EditorGUILayout.GetControlRect();
            position = EditorGUI.PrefixLabel(position, 47385, EditorGUIUtility.TempContent("Shader"));
            EditorGUI.showMixedValue = HasMultipleMixedShaderValues();

            GUIContent buttonContent = EditorGUIUtility.TempContent(m_Shader != null ? m_Shader.name : "No Shader Selected");
            if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, style))
            {
                EditorGUI.showMixedValue = false;
                Vector2 pos = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));

                // @TODO: SetupShaderPopupMenu in ShaderMenu.cpp needs to be able to accept null
                // so no choice is selected when the shaders are different for the selected materials.
                InternalEditorUtility.SetupShaderMenu(target as Material);
                EditorUtility.Internal_DisplayPopupMenu(new Rect(pos.x, pos.y, position.width, position.height), "CONTEXT/ShaderPopup", this, 0);
                Event.current.Use();
            }
            EditorGUI.showMixedValue = false;

            GUI.enabled = wasEnabled;
        }

        public virtual void Awake()
        {
            m_IsVisible = InternalEditorUtility.GetIsInspectorExpanded(target);
            if (GetPreviewType(target as Material) == PreviewType.Skybox)
                m_PreviewDir = new Vector2(0, 50);
        }

        private void DetectShaderEditorNeedsUpdate()
        {
            var material = target as Material;
            bool shaderChanged = material && material.shader != m_Shader;
            bool customEditorChanged = material && material.shader && m_CustomEditorClassName != material.shader.customEditor;
            if (shaderChanged || customEditorChanged)
            {
                CreateCustomShaderEditorIfNeeded(material.shader);
                if (shaderChanged)
                {
                    m_Shader = material.shader;
                    OnShaderChanged();
                }
                InspectorWindow.RepaintAllInspectors();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CheckSetup();
            DetectShaderEditorNeedsUpdate();

            if (isVisible && m_Shader != null && !HasMultipleMixedShaderValues())
            {
                // Show Material properties
                if (PropertiesGUI())
                {
                    PropertiesChanged();
                }
            }
        }

        void CheckSetup()
        {
            if (!m_CheckSetup || m_Shader == null)
                return;

            m_CheckSetup = false;
            if (m_CustomShaderGUI == null && !IsMaterialEditor(m_Shader.customEditor))
            {
                Debug.LogWarningFormat("Could not create a custom UI for the shader '{0}'. The shader has the following: 'CustomEditor = {1}'. Does the custom editor specified include its namespace? And does the class either derive from ShaderGUI or MaterialEditor?", m_Shader.name, m_Shader.customEditor);
            }
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector
        internal override void OnAssetStoreInspectorGUI()
        {
            OnInspectorGUI();
        }

        public void PropertiesChanged()
        {
            // @TODO: Show performance warnings for multi-selections too?
            m_InfoMessage = null;
            if (targets.Length == 1)
                m_InfoMessage = Utils.PerformanceChecks.CheckMaterial(target as Material, EditorUserBuildSettings.activeBuildTarget);
        }

        protected virtual void OnShaderChanged()
        {
        }

        protected override void OnHeaderGUI()
        {
            const float spaceForFoldoutArrow = 10f;
            Rect titleRect = DrawHeaderGUI(this, targetTitle, forceVisible ? 0 : spaceForFoldoutArrow);
            int id = GUIUtility.GetControlID(45678, FocusType.Passive);

            if (!forceVisible)
            {
                Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
                renderRect.y = titleRect.yMax - 17f; // align with bottom
                bool newVisible = EditorGUI.DoObjectFoldout(m_IsVisible, titleRect, renderRect, targets, id);

                // Toggle visibility
                if (newVisible != m_IsVisible)
                {
                    m_IsVisible = newVisible;
                    InternalEditorUtility.SetIsInspectorExpanded(target, newVisible);
                }
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(!IsEnabled()))
            {
                EditorGUIUtility.labelWidth = 50;

                // Shader selection dropdown
                ShaderPopup("MiniPulldown");

                // Edit button for custom shaders
                if (m_Shader != null && HasMultipleMixedShaderValues() && (m_Shader.hideFlags & HideFlags.DontSave) == 0)
                {
                    if (GUILayout.Button("Edit...", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        AssetDatabase.OpenAsset(m_Shader);
                }
            }
        }

        // -------- obsolete helper functions to get/set material values

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public float GetFloat(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            float f = ((Material)targets[0]).GetFloat(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                if (((Material)targets[i]).GetFloat(propertyName) != f)
                {
                    hasMixedValue = true;
                    break;
                }
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetFloat(string propertyName, float value)
        {
            foreach (Material material in targets)
                material.SetFloat(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Color GetColor(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Color f = ((Material)targets[0]).GetColor(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetColor(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetColor(string propertyName, Color value)
        {
            foreach (Material material in targets)
                material.SetColor(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Vector4 GetVector(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Vector4 f = ((Material)targets[0]).GetVector(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetVector(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetVector(string propertyName, Vector4 value)
        {
            foreach (Material material in targets)
                material.SetVector(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Texture GetTexture(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Texture f = ((Material)targets[0]).GetTexture(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetTexture(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTexture(string propertyName, Texture value)
        {
            foreach (Material material in targets)
                material.SetTexture(propertyName, value);
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public Vector2 GetTextureScale(string propertyName, out bool hasMixedValueX, out bool hasMixedValueY)
        {
            hasMixedValueX = false;
            hasMixedValueY = false;
            Vector2 f = ((Material)targets[0]).GetTextureScale(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                Vector2 f2 = ((Material)targets[i]).GetTextureScale(propertyName);
                if (f2.x != f.x) { hasMixedValueX = true; }
                if (f2.y != f.y) { hasMixedValueY = true; }
                if (hasMixedValueX && hasMixedValueY)
                    break;
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public Vector2 GetTextureOffset(string propertyName, out bool hasMixedValueX, out bool hasMixedValueY)
        {
            hasMixedValueX = false;
            hasMixedValueY = false;
            Vector2 f = ((Material)targets[0]).GetTextureOffset(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                Vector2 f2 = ((Material)targets[i]).GetTextureOffset(propertyName);
                if (f2.x != f.x) { hasMixedValueX = true; }
                if (f2.y != f.y) { hasMixedValueY = true; }
                if (hasMixedValueX && hasMixedValueY)
                    break;
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTextureScale(string propertyName, Vector2 value, int coord)
        {
            foreach (Material material in targets)
            {
                Vector2 f = material.GetTextureScale(propertyName);
                f[coord] = value[coord];
                material.SetTextureScale(propertyName, f);
            }
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTextureOffset(string propertyName, Vector2 value, int coord)
        {
            foreach (Material material in targets)
            {
                Vector2 f = material.GetTextureOffset(propertyName);
                f[coord] = value[coord];
                material.SetTextureOffset(propertyName, f);
            }
        }

        // -------- helper functions to display common material controls

        // The 'Property' methods that accept GUIContent are internal with different name to avoid
        // breaking backwards compatibility caused by adding overloads:
        // 'RangeProperty(prop, null);' would cause a compile error because of ambigious overloads.

        public float RangeProperty(MaterialProperty prop, string label)
        {
            return RangePropertyInternal(prop, new GUIContent(label));
        }

        internal float RangePropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return RangePropertyInternal(r, prop, label);
        }

        public float RangeProperty(Rect position, MaterialProperty prop, string label)
        {
            return RangePropertyInternal(position, prop, new GUIContent(label));
        }

        internal float RangePropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            float power = (prop.name == "_Shininess") ? 5f : 1f;
            return DoPowerRangeProperty(position, prop, label, power);
        }

        internal static float DoPowerRangeProperty(Rect position, MaterialProperty prop, GUIContent label, float power)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            // For range properties we want to show the slider so we adjust label width to use default width (setting it to 0)
            // See SetDefaultGUIWidths where we set: EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 17;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            float newValue = EditorGUI.PowerSlider(position, label, prop.floatValue, prop.rangeLimits.x, prop.rangeLimits.y, power);
            EditorGUI.showMixedValue = false;

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;

            return prop.floatValue;
        }

        internal static int DoIntRangeProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            // For range properties we want to show the slider so we adjust label width to use default width (setting it to 0)
            // See SetDefaultGUIWidths where we set: EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 17;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            int newValue = EditorGUI.IntSlider(position, label, (int)prop.floatValue, (int)prop.rangeLimits.x, (int)prop.rangeLimits.y);
            EditorGUI.showMixedValue = false;

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (EditorGUI.EndChangeCheck())
                prop.floatValue = (float)newValue;

            return (int)prop.floatValue;
        }

        public float FloatProperty(MaterialProperty prop, string label)
        {
            return FloatPropertyInternal(prop, new GUIContent(label));
        }

        internal float FloatPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return FloatPropertyInternal(r, prop, label);
        }

        public float FloatProperty(Rect position, MaterialProperty prop, string label)
        {
            return FloatPropertyInternal(position, prop, new GUIContent(label));
        }

        internal float FloatPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float newValue = EditorGUI.FloatField(position, label, prop.floatValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;

            return prop.floatValue;
        }

        public Color ColorProperty(MaterialProperty prop, string label)
        {
            return ColorPropertyInternal(prop, new GUIContent(label));
        }

        internal Color ColorPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return ColorPropertyInternal(r, prop, label);
        }

        public Color ColorProperty(Rect position, MaterialProperty prop, string label)
        {
            return ColorPropertyInternal(position, prop, new GUIContent(label));
        }

        internal Color ColorPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            bool isHDR = ((prop.flags & MaterialProperty.PropFlags.HDR) != 0);
            bool showAlpha = true;
            Color newValue = EditorGUI.ColorField(position, label, prop.colorValue, true, showAlpha, isHDR, null);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.colorValue = newValue;

            return prop.colorValue;
        }

        public Vector4 VectorProperty(MaterialProperty prop, string label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return VectorProperty(r, prop, label);
        }

        public Vector4 VectorProperty(Rect position, MaterialProperty prop, string label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            // We want to make room for the field in case it's drawn on the same line as the label
            // Set label width to default width (zero) temporarily
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            Vector4 newValue = EditorGUI.Vector4Field(position, label, prop.vectorValue);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.vectorValue = newValue;

            return prop.vectorValue;
        }

        // Using GUILayout
        public void TextureScaleOffsetProperty(MaterialProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 2 * kLineHeight, EditorStyles.layerMaskField);
            TextureScaleOffsetProperty(rect, property, false);
        }

        // Returns height used
        public float TextureScaleOffsetProperty(Rect position, MaterialProperty property)
        {
            return TextureScaleOffsetProperty(position, property, true);
        }

        // Returns height used
        public float TextureScaleOffsetProperty(Rect position, MaterialProperty property, bool partOfTexturePropertyControl)
        {
            BeginAnimatedCheck(position, property);

            EditorGUI.BeginChangeCheck();
            // Mixed value mask is 4 bits for the uv offset & scale (First bit is for the texture itself)
            int mixedValuemask = property.mixedValueMask >> 1;
            Vector4 scaleAndOffset = TextureScaleOffsetProperty(position, property.textureScaleAndOffset, mixedValuemask, partOfTexturePropertyControl);

            if (EditorGUI.EndChangeCheck())
                property.textureScaleAndOffset = scaleAndOffset;

            EndAnimatedCheck();
            return 2 * kLineHeight;
        }

        private Texture TexturePropertyBody(Rect position, MaterialProperty prop)
        {
            if (prop.type != MaterialProperty.PropType.Texture)
            {
                throw new ArgumentException(string.Format("The MaterialProperty '{0}' should be of type 'Texture' (its type is '{1})'", prop.name, prop.type));
            }

            m_DesiredTexdim = prop.textureDimension;
            System.Type t = MaterialEditor.GetTextureTypeFromDimension(m_DesiredTexdim);

            // Why are we disabling the GUI in Animation Mode here?
            // If it's because object references can't be changed, shouldn't it be done in ObjectField instead?
            bool wasEnabled = GUI.enabled;

            EditorGUI.BeginChangeCheck();
            if ((prop.flags & MaterialProperty.PropFlags.PerRendererData) != 0)
                GUI.enabled = false;

            EditorGUI.showMixedValue = prop.hasMixedValue;
            int controlID = GUIUtility.GetControlID(12354, FocusType.Keyboard, position);
            var newValue = EditorGUI.DoObjectField(position, position, controlID, prop.textureValue, t, null, TextureValidator, false) as Texture;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.textureValue = newValue;

            GUI.enabled = wasEnabled;

            return prop.textureValue;
        }

        public Texture TextureProperty(MaterialProperty prop, string label)
        {
            bool scaleOffset = ((prop.flags & MaterialProperty.PropFlags.NoScaleOffset) == 0);
            return TextureProperty(prop, label, scaleOffset);
        }

        public Texture TextureProperty(MaterialProperty prop, string label, bool scaleOffset)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return TextureProperty(r, prop, label, scaleOffset);
        }

        public bool HelpBoxWithButton(GUIContent messageContent, GUIContent buttonContent)
        {
            const float kButtonWidth = 60f;
            const float kSpacing = 5f;
            const float kButtonHeight = 20f;

            // Reserve size of wrapped text
            Rect contentRect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
            // Reserve size of button
            GUILayoutUtility.GetRect(1, kButtonHeight + kSpacing);

            // Render background box with text at full height
            contentRect.height += kButtonHeight + kSpacing;
            GUI.Label(contentRect, messageContent, EditorStyles.helpBox);

            // Button (align lower right)
            Rect buttonRect = new Rect(contentRect.xMax - kButtonWidth - 4f, contentRect.yMax - kButtonHeight - 4f, kButtonWidth, kButtonHeight);
            return GUI.Button(buttonRect, buttonContent);
        }

        public void TextureCompatibilityWarning(MaterialProperty prop)
        {
            if (InternalEditorUtility.BumpMapTextureNeedsFixing(prop))
            {
                if (HelpBoxWithButton(
                        EditorGUIUtility.TextContent("This texture is not marked as a normal map"),
                        EditorGUIUtility.TextContent("Fix Now")))
                {
                    InternalEditorUtility.FixNormalmapTexture(prop);
                }
            }
        }

        public Texture TexturePropertyMiniThumbnail(Rect position, MaterialProperty prop, string label, string tooltip)
        {
            BeginAnimatedCheck(position, prop);
            Rect thumbRect, labelRect;
            EditorGUI.GetRectsForMiniThumbnailField(position, out thumbRect, out labelRect);
            EditorGUI.HandlePrefixLabel(position, labelRect, new GUIContent(label, tooltip), 0, EditorStyles.label);
            EndAnimatedCheck();

            Texture retValue = TexturePropertyBody(thumbRect, prop);

            Rect warningPosition = position;
            warningPosition.y += position.height;
            warningPosition.height = kMiniWarningMessageHeight;

            TextureCompatibilityWarning(prop);

            return retValue;
        }

        public Rect GetTexturePropertyCustomArea(Rect position)
        {
            EditorGUI.indentLevel++;
            position.height = GetTextureFieldHeight();
            Rect scaleOffsetRect = position;
            scaleOffsetRect.yMin += EditorGUI.kSingleLineHeight;
            scaleOffsetRect.xMax -= EditorGUIUtility.fieldWidth + 2;
            scaleOffsetRect = EditorGUI.IndentedRect(scaleOffsetRect);
            EditorGUI.indentLevel--;
            return scaleOffsetRect;
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label)
        {
            bool scaleOffset = ((prop.flags & MaterialProperty.PropFlags.NoScaleOffset) == 0);
            return TextureProperty(position, prop, label, scaleOffset);
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label, bool scaleOffset)
        {
            return TextureProperty(position, prop, label, string.Empty, scaleOffset);
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label, string tooltip, bool scaleOffset)
        {
            // Label
            EditorGUI.PrefixLabel(position, new GUIContent(label, tooltip));

            // Texture slot
            position.height = GetTextureFieldHeight();
            Rect texPos = position;
            texPos.xMin = texPos.xMax - EditorGUIUtility.fieldWidth;
            Texture value = TexturePropertyBody(texPos, prop);

            // UV scale and offset
            if (scaleOffset)
            {
                TextureScaleOffsetProperty(GetTexturePropertyCustomArea(position), prop);
            }

            // Potential warning help boxes
            {
                GUILayout.Space(-kSpacingUnderTexture);
                TextureCompatibilityWarning(prop);
                GUILayout.Space(kSpacingUnderTexture);
            }
            return value;
        }

        public static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset)
        {
            return TextureScaleOffsetProperty(position, scaleOffset, 0, false);
        }

        public static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset, bool partOfTexturePropertyControl)
        {
            return TextureScaleOffsetProperty(position, scaleOffset, 0, partOfTexturePropertyControl);
        }

        internal static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset, int mixedValueMask, bool partOfTexturePropertyControl)
        {
            Vector2 tiling = new Vector2(scaleOffset.x, scaleOffset.y);
            Vector2 offset = new Vector2(scaleOffset.z, scaleOffset.w);

            float labelWidth = EditorGUIUtility.labelWidth;
            float controlStartX = position.x + labelWidth;
            float labelStartX = position.x + EditorGUI.indent;

            if (partOfTexturePropertyControl)
            {
                labelWidth = 65;
                controlStartX = position.x + labelWidth;
                labelStartX = position.x;
                position.y = position.yMax - 2 * kLineHeight; // align with large texture thumb bottom
            }

            // Tiling
            Rect labelRect = new Rect(labelStartX, position.y, labelWidth, kLineHeight);
            Rect valueRect = new Rect(controlStartX, position.y, position.width - labelWidth, kLineHeight);
            EditorGUI.PrefixLabel(labelRect, s_TilingText);
            tiling = EditorGUI.Vector2Field(valueRect, GUIContent.none, tiling);

            // Offset
            labelRect.y += kLineHeight;
            valueRect.y += kLineHeight;
            EditorGUI.PrefixLabel(labelRect, s_OffsetText);
            offset = EditorGUI.Vector2Field(valueRect, GUIContent.none, offset);

            return new Vector4(tiling.x, tiling.y, offset.x, offset.y);
        }

        public float GetPropertyHeight(MaterialProperty prop)
        {
            return GetPropertyHeight(prop, prop.displayName);
        }

        public float GetPropertyHeight(MaterialProperty prop, string label)
        {
            // has custom drawers?
            float handlerHeight = 0f;
            MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
            if (handler != null)
            {
                handlerHeight = handler.GetPropertyHeight(prop, label ?? prop.displayName, this);
                // if we have a property drawer (and not just decorators), exit now and don't add default height
                if (handler.propertyDrawer != null)
                    return handlerHeight;
            }

            // otherwise, return default height
            return handlerHeight + GetDefaultPropertyHeight(prop);
        }

        private static float GetTextureFieldHeight()
        {
            return EditorGUI.kObjectFieldThumbnailHeight;
        }

        public static float GetDefaultPropertyHeight(MaterialProperty prop)
        {
            if (prop.type == MaterialProperty.PropType.Vector)
                return EditorGUI.kStructHeaderLineHeight + EditorGUI.kSingleLineHeight;

            if (prop.type == MaterialProperty.PropType.Texture)
                return GetTextureFieldHeight() + kSpacingUnderTexture;

            return EditorGUI.kSingleLineHeight;
        }

        private Rect GetPropertyRect(MaterialProperty prop, GUIContent label, bool ignoreDrawer)
        {
            return GetPropertyRect(prop, label.text, ignoreDrawer);
        }

        private Rect GetPropertyRect(MaterialProperty prop, string label, bool ignoreDrawer)
        {
            float handlerHeight = 0f;
            if (!ignoreDrawer)
            {
                MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
                if (handler != null)
                {
                    handlerHeight = handler.GetPropertyHeight(prop, label ?? prop.displayName, this);
                    // if we have a property drawer (and not just decorators), exit now and don't add default height
                    if (handler.propertyDrawer != null)
                        return EditorGUILayout.GetControlRect(true, handlerHeight, EditorStyles.layerMaskField);
                }
            }

            return EditorGUILayout.GetControlRect(true, handlerHeight + GetDefaultPropertyHeight(prop), EditorStyles.layerMaskField);
        }

        public void BeginAnimatedCheck(Rect totalPosition, MaterialProperty prop)
        {
            if (rendererForAnimationMode == null)
                return;

            s_AnimatedCheckStack.Push(new AnimatedCheckData(prop, totalPosition, GUI.backgroundColor));

            Color overrideColor;
            if (MaterialAnimationUtility.OverridePropertyColor(prop, rendererForAnimationMode, out overrideColor))
                GUI.backgroundColor = overrideColor;
        }

        public void BeginAnimatedCheck(MaterialProperty prop)
        {
            BeginAnimatedCheck(Rect.zero, prop);
        }

        public void EndAnimatedCheck()
        {
            if (rendererForAnimationMode == null)
                return;

            AnimatedCheckData data = s_AnimatedCheckStack.Pop();
            if (Event.current.type == EventType.ContextClick && data.totalPosition.Contains(Event.current.mousePosition))
            {
                DoPropertyContextMenu(data.property);
            }

            GUI.backgroundColor = data.color;
        }

        private void DoPropertyContextMenu(MaterialProperty prop)
        {
            if (contextualPropertyMenu != null)
            {
                GenericMenu pm = new GenericMenu();
                contextualPropertyMenu(pm, prop, m_RenderersForAnimationMode);

                if (pm.GetItemCount() > 0)
                    pm.ShowAsContext();
            }
        }

        public void ShaderProperty(MaterialProperty prop, string label)
        {
            ShaderProperty(prop, new GUIContent(label));
        }

        public void ShaderProperty(MaterialProperty prop, GUIContent label)
        {
            ShaderProperty(prop, label, 0);
        }

        public void ShaderProperty(MaterialProperty prop, string label, int labelIndent)
        {
            ShaderProperty(prop, new GUIContent(label), labelIndent);
        }

        public void ShaderProperty(MaterialProperty prop, GUIContent label, int labelIndent)
        {
            Rect r = GetPropertyRect(prop, label, false);
            ShaderProperty(r, prop, label, labelIndent);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, string label)
        {
            ShaderProperty(position, prop, new GUIContent(label));
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            ShaderProperty(position, prop, label, 0);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, string label, int labelIndent)
        {
            ShaderProperty(position, prop, new GUIContent(label), labelIndent);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, GUIContent label, int labelIndent)
        {
            BeginAnimatedCheck(position, prop);
            EditorGUI.indentLevel += labelIndent;

            ShaderPropertyInternal(position, prop, label);

            EditorGUI.indentLevel -= labelIndent;
            EndAnimatedCheck();
        }

        public void LightmapEmissionProperty()
        {
            LightmapEmissionProperty(0);
        }

        public void LightmapEmissionProperty(int labelIndent)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField);
            LightmapEmissionProperty(r, labelIndent);
        }

        static MaterialGlobalIlluminationFlags GetGlobalIlluminationFlags(MaterialGlobalIlluminationFlags flags)
        {
            MaterialGlobalIlluminationFlags newFlags = MaterialGlobalIlluminationFlags.None;
            if ((flags & MaterialGlobalIlluminationFlags.RealtimeEmissive) != 0)
                newFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            else if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0)
                newFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            return newFlags;
        }

        public void LightmapEmissionProperty(Rect position, int labelIndent)
        {
            EditorGUI.indentLevel += labelIndent;

            Object[] materials = targets;
            Material firstMaterial = (Material)target;

            // Calculate isMixed
            MaterialGlobalIlluminationFlags giFlags = GetGlobalIlluminationFlags(firstMaterial.globalIlluminationFlags);
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                Material material = (Material)materials[i];

                if (GetGlobalIlluminationFlags(material.globalIlluminationFlags) != giFlags)
                    isMixed = true;
            }

            EditorGUI.BeginChangeCheck();

            // Show popup
            EditorGUI.showMixedValue = isMixed;
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUI.IntPopup(position, Styles.lightmapEmissiveLabel, (int)giFlags, Styles.lightmapEmissiveStrings, Styles.lightmapEmissiveValues);
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material material in materials)
                {
                    MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;

                    flags &= ~(MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.BakedEmissive);
                    flags |= giFlags;

                    material.globalIlluminationFlags = flags;
                }
            }

            EditorGUI.indentLevel -= labelIndent;
        }

        public bool EmissionEnabledProperty()
        {
            Material[] materials = Array.ConvertAll(targets, (Object o) => { return (Material)o; });

            // Query global lighting state
            LightModeUtil lmu = LightModeUtil.Get();
            MaterialGlobalIlluminationFlags defaultEnabled = lmu.IsRealtimeGIEnabled() ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : (lmu.AreBakedLightmapsEnabled() ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None);

            // Calculate isMixed
            bool enabled = materials[0].globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                if ((materials[i].globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) != enabled)
                {
                    isMixed = true;
                    break;
                }
            }

            // initial checkbox for enabling/disabling emission
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixed;
            enabled = EditorGUILayout.Toggle(Styles.emissionLabel, enabled);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material mat in materials)
                {
                    mat.globalIlluminationFlags = enabled ? defaultEnabled : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                return enabled;
            }
            return !isMixed && enabled;
        }

        public static void FixupEmissiveFlag(Material mat)
        {
            if (mat == null)
                throw new ArgumentNullException("mat");

            mat.globalIlluminationFlags = FixupEmissiveFlag(mat.GetColor("_EmissionColor"), mat.globalIlluminationFlags);
        }

        public static MaterialGlobalIlluminationFlags FixupEmissiveFlag(Color col, MaterialGlobalIlluminationFlags flags)
        {
            if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0 && col.maxColorComponent == 0.0f) // flag black baked
                flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            else if (flags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) // clear baked flag on everything else, unless it's explicity disabled
                flags &= MaterialGlobalIlluminationFlags.AnyEmissive;
            return flags;
        }

        public void LightmapEmissionFlagsProperty(int indent, bool enabled)
        {
            Material[] materials = Array.ConvertAll(targets, (Object o) => { return (Material)o; });

            // Calculate isMixed
            MaterialGlobalIlluminationFlags any_em = MaterialGlobalIlluminationFlags.AnyEmissive;
            MaterialGlobalIlluminationFlags giFlags = materials[0].globalIlluminationFlags & any_em;
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                isMixed = isMixed || (materials[i].globalIlluminationFlags & any_em) != giFlags;
            }

            EditorGUI.BeginChangeCheck();

            // Show popup
            EditorGUI.showMixedValue = isMixed;
            EditorGUI.indentLevel += indent;
            int[] shortvals = { Styles.lightmapEmissiveValues[0], Styles.lightmapEmissiveValues[1] };
            GUIContent[] shortstrings = { Styles.lightmapEmissiveStrings[0], Styles.lightmapEmissiveStrings[1] };
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUILayout.IntPopup(Styles.lightmapEmissiveLabel, (int)giFlags, shortstrings, shortvals);
            EditorGUI.indentLevel -= indent;
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            bool applyFlags = EditorGUI.EndChangeCheck();
            foreach (Material mat in materials)
            {
                mat.globalIlluminationFlags = applyFlags ? giFlags : mat.globalIlluminationFlags;
                FixupEmissiveFlag(mat);
            }
        }

        void ShaderPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            // Use custom property drawers if needed
            MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
            if (handler != null)
            {
                handler.OnGUI(ref position, prop, label.text != null ? label : new GUIContent(prop.displayName), this);
                // If we had a property drawer (and not just decorators), exit now and don't draw default UI.
                if (handler.propertyDrawer != null)
                    return;
            }

            DefaultShaderPropertyInternal(position, prop, label);
        }

        public void DefaultShaderProperty(MaterialProperty prop, string label)
        {
            DefaultShaderPropertyInternal(prop, new GUIContent(label));
        }

        internal void DefaultShaderPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            DefaultShaderPropertyInternal(r, prop, label);
        }

        public void DefaultShaderProperty(Rect position, MaterialProperty prop, string label)
        {
            DefaultShaderPropertyInternal(position, prop, new GUIContent(label));
        }

        internal void DefaultShaderPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            switch (prop.type)
            {
                case MaterialProperty.PropType.Range: // float ranges
                    RangePropertyInternal(position, prop, label);
                    break;
                case MaterialProperty.PropType.Float: // floats
                    FloatPropertyInternal(position, prop, label);
                    break;
                case MaterialProperty.PropType.Color: // colors
                    ColorPropertyInternal(position, prop, label);
                    break;
                case MaterialProperty.PropType.Texture: // textures
                    TextureProperty(position, prop, label.text);
                    break;
                case MaterialProperty.PropType.Vector: // vectors
                    VectorProperty(position, prop, label.text);
                    break;
                default:
                    GUI.Label(position, "Unknown property type: " + prop.name + ": " + (int)prop.type);
                    break;
            }
        }

        // -------- obsolete versions of common controls


        [System.Obsolete("Use RangeProperty with MaterialProperty instead.")]
        public float RangeProperty(string propertyName, string label, float v2, float v3)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return RangeProperty(prop, label);
        }

        [System.Obsolete("Use FloatProperty with MaterialProperty instead.")]
        public float FloatProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return FloatProperty(prop, label);
        }

        [System.Obsolete("Use ColorProperty with MaterialProperty instead.")]
        public Color ColorProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return ColorProperty(prop, label);
        }

        [System.Obsolete("Use VectorProperty with MaterialProperty instead.")]
        public Vector4 VectorProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return VectorProperty(prop, label);
        }

        [System.Obsolete("Use TextureProperty with MaterialProperty instead.")]
        public Texture TextureProperty(string propertyName, string label, ShaderUtil.ShaderPropertyTexDim texDim)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return TextureProperty(prop, label);
        }

        [System.Obsolete("Use TextureProperty with MaterialProperty instead.")]
        public Texture TextureProperty(string propertyName, string label, ShaderUtil.ShaderPropertyTexDim texDim, bool scaleOffset)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return TextureProperty(prop, label, scaleOffset);
        }

        [System.Obsolete("Use ShaderProperty that takes MaterialProperty parameter instead.")]
        public void ShaderProperty(Shader shader, int propertyIndex)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyIndex);
            ShaderProperty(prop, prop.displayName);
        }

        // -------- other functionality

        public static MaterialProperty[] GetMaterialProperties(Object[] mats)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperties(mats);
        }

        public static MaterialProperty GetMaterialProperty(Object[] mats, string name)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperty(mats, name);
        }

        public static MaterialProperty GetMaterialProperty(Object[] mats, int propertyIndex)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperty_Index(mats, propertyIndex);
        }

        class ForwardApplyMaterialModification
        {
            readonly Renderer[] renderers;
            bool        isMaterialEditable;

            public ForwardApplyMaterialModification(Renderer[] r, bool inIsMaterialEditable)
            {
                renderers = r;
                isMaterialEditable = inIsMaterialEditable;
            }

            public bool DidModifyAnimationModeMaterialProperty(MaterialProperty property, int changedMask, object previousValue)
            {
                bool didModify = false;
                foreach (Renderer renderer in renderers)
                {
                    didModify = didModify | MaterialAnimationUtility.ApplyMaterialModificationToAnimationRecording(property, changedMask, renderer, previousValue);
                }

                if (didModify)
                    return true;

                // If the material is not editable,
                // then we explicitly make sure that things that these properties arecan not be recorded are not going to be applied to the material
                return !isMaterialEditable;
            }
        }

        static Renderer[] GetAssociatedRenderersFromInspector()
        {
            List<Renderer> renderers = new List<Renderer>();
            if (InspectorWindow.s_CurrentInspectorWindow)
            {
                Editor[] editors = InspectorWindow.s_CurrentInspectorWindow.tracker.activeEditors;
                foreach (var editor in editors)
                {
                    foreach (Object target in editor.targets)
                    {
                        var renderer = target as Renderer;
                        if (renderer)
                            renderers.Add(renderer);
                    }
                }
            }

            return renderers.ToArray();
        }

        public static Renderer PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, bool isMaterialEditable)
        {
            Renderer[] renderers = PrepareMaterialPropertiesForAnimationMode(properties, GetAssociatedRenderersFromInspector(), isMaterialEditable);
            return (renderers != null && renderers.Length > 0) ? renderers[0] : null;
        }

        internal static Renderer PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, Renderer renderer, bool isMaterialEditable)
        {
            Renderer[] renderers = PrepareMaterialPropertiesForAnimationMode(properties, new Renderer[] {renderer}, isMaterialEditable);
            return (renderers != null && renderers.Length > 0) ? renderers[0] : null;
        }

        internal static Renderer[] PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, Renderer[] renderers, bool isMaterialEditable)
        {
            bool isInAnimationMode = AnimationMode.InAnimationMode();

            if (renderers != null && renderers.Length > 0)
            {
                var callback = new ForwardApplyMaterialModification(renderers, isMaterialEditable);
                var block = new MaterialPropertyBlock();

                renderers[0].GetPropertyBlock(block);
                foreach (MaterialProperty prop in properties)
                {
                    prop.ReadFromMaterialPropertyBlock(block);
                    if (isInAnimationMode)
                        prop.applyPropertyCallback = callback.DidModifyAnimationModeMaterialProperty;
                }
            }

            if (isInAnimationMode)
                return renderers;
            else
                return null;
        }

        public void SetDefaultGUIWidths()
        {
            EditorGUIUtility.fieldWidth = EditorGUI.kObjectFieldThumbnailHeight;
            EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 17;
        }

        private bool IsMaterialEditor(string customEditorName)
        {
            string unityEditorFullName = "UnityEditor." + customEditorName; // for convenience: adding UnityEditor namespace is not needed in the shader

            foreach (var assembly in EditorAssemblies.loadedAssemblies)
            {
                Type[] types = AssemblyHelper.GetTypesFromAssembly(assembly);
                foreach (var type in types)
                {
                    if (type.FullName.Equals(customEditorName, StringComparison.Ordinal) ||
                        type.FullName.Equals(unityEditorFullName, StringComparison.Ordinal))
                    {
                        if (typeof(MaterialEditor).IsAssignableFrom(type))
                            return true;
                    }
                }
            }
            return false;
        }

        void CreateCustomShaderEditorIfNeeded(Shader shader)
        {
            if (shader == null || string.IsNullOrEmpty(shader.customEditor))
            {
                m_CustomEditorClassName = "";
                m_CustomShaderGUI = null;
                return;
            }
            if (m_CustomEditorClassName == shader.customEditor)
                return;

            m_CustomEditorClassName = shader.customEditor;
            m_CustomShaderGUI = ShaderGUIUtility.CreateShaderGUI(m_CustomEditorClassName);
            // We need to delay checking setup because we need all loaded editor assemblies which is not ready
            // during package import. During package import we create an Editor to generate a asset preview. (case 707328)
            m_CheckSetup = true;
        }

        public bool PropertiesGUI()
        {
            if (m_InsidePropertiesGUI)
            {
                Debug.LogWarning("PropertiesGUI() is being called recursively. If you want to render the default gui for shader properties then call PropertiesDefaultGUI() instead");
                return false;
            }

            EditorGUI.BeginChangeCheck();

            MaterialProperty[] props = GetMaterialProperties(targets);

            // In animation mode we are actually animating the Renderer instead of the material.
            // Thus all properties are editable even if the material is not editable.
            m_RenderersForAnimationMode = PrepareMaterialPropertiesForAnimationMode(props, GetAssociatedRenderersFromInspector(), GUI.enabled);
            bool wasEnabled = GUI.enabled;
            if (m_RenderersForAnimationMode != null)
                GUI.enabled = true;

            m_InsidePropertiesGUI = true;

            // Since ExitGUI is called when showing the Object Picker we wrap
            // properties gui in try/catch to catch the ExitGUIException thrown by ExitGUI()
            // to ensure our m_InsidePropertiesGUI flag is reset
            try
            {
                if (m_CustomShaderGUI != null)
                    m_CustomShaderGUI.OnGUI(this, props);
                else
                    PropertiesDefaultGUI(props);

                Renderer[] renderers = GetAssociatedRenderersFromInspector();
                if (renderers != null && renderers.Length > 0)
                {
                    if (Event.current.type == EventType.Layout)
                    {
                        renderers[0].GetPropertyBlock(m_PropertyBlock);
                    }

                    if (m_PropertyBlock != null && !m_PropertyBlock.isEmpty)
                        EditorGUILayout.HelpBox(Styles.propBlockInfo, MessageType.Info);
                }
            }
            catch (Exception)
            {
                GUI.enabled = wasEnabled;
                m_InsidePropertiesGUI = false;
                m_RenderersForAnimationMode = null;
                throw;
            }

            GUI.enabled = wasEnabled;
            m_InsidePropertiesGUI = false;
            m_RenderersForAnimationMode = null;

            return EditorGUI.EndChangeCheck();
        }

        public void PropertiesDefaultGUI(MaterialProperty[] props)
        {
            SetDefaultGUIWidths();

            if (m_InfoMessage != null)
                EditorGUILayout.HelpBox(m_InfoMessage, MessageType.Info);
            else
                // Hack to make sure that control IDs stay the same when the help box is there or is not there.
                // Otherwise, open color picker windows will not keep synched to the same properties when the help box
                // shows up or disappears (case 566958)
                GUIUtility.GetControlID(s_ControlHash, FocusType.Passive, new Rect(0, 0, 0, 0));

            for (var i = 0; i < props.Length; i++)
            {
                if ((props[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                    continue;

                float h = GetPropertyHeight(props[i], props[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                ShaderProperty(r, props[i], props[i].displayName);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            RenderQueueField();
            EnableInstancingField();
            DoubleSidedGIField();
        }

        public static void ApplyMaterialPropertyDrawers(Material material)
        {
            var objs = new Object[] { material };
            ApplyMaterialPropertyDrawers(objs);
        }

        public static void ApplyMaterialPropertyDrawers(Object[] targets)
        {
            if (targets == null || targets.Length == 0)
                return;
            var target = targets[0] as Material;
            if (target == null)
                return;

            var shader = target.shader;
            var props = GetMaterialProperties(targets);
            for (var i = 0; i < props.Length; i++)
            {
                MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(shader, props[i].name);
                if (handler != null && handler.propertyDrawer != null)
                    handler.propertyDrawer.Apply(props[i]);
            }
        }

        public void RegisterPropertyChangeUndo(string label)
        {
            Undo.RecordObjects(targets, "Modify " + label + " of " + targetTitle);
        }

        private UnityEngine.Rendering.TextureDimension m_DesiredTexdim;

        private Object TextureValidator(Object[] references, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            foreach (Object i in references)
            {
                var t = i as Texture;
                if (t)
                {
                    if (t.dimension == m_DesiredTexdim || m_DesiredTexdim == UnityEngine.Rendering.TextureDimension.Any)
                        return t;
                }
            }
            return null;
        }

        private PreviewRenderUtility m_PreviewUtility;
        private static readonly Mesh[] s_Meshes = {null, null, null, null, null };
        private static Mesh s_PlaneMesh;
        private static readonly GUIContent[] s_MeshIcons = { null, null, null, null, null };
        private static readonly GUIContent[] s_LightIcons = { null, null };
        private static readonly GUIContent[] s_TimeIcons = { null, null };

        private void Init()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.camera, true);
            }

            if (s_Meshes[0] == null)
            {
                var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = meshFilter.sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = meshFilter.sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = meshFilter.sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = meshFilter.sharedMesh;
                            break;
                        default:
                            Debug.Log("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }

                s_MeshIcons[0] = EditorGUIUtility.IconContent("PreMatSphere");
                s_MeshIcons[1] = EditorGUIUtility.IconContent("PreMatCube");
                s_MeshIcons[2] = EditorGUIUtility.IconContent("PreMatCylinder");
                s_MeshIcons[3] = EditorGUIUtility.IconContent("PreMatTorus");
                s_MeshIcons[4] = EditorGUIUtility.IconContent("PreMatQuad");

                s_LightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                s_LightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");

                s_TimeIcons[0] = EditorGUIUtility.IconContent("PlayButton");
                s_TimeIcons[1] = EditorGUIUtility.IconContent("PauseButton");

                Mesh quadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
                s_Meshes[4] = quadMesh;
                s_PlaneMesh = quadMesh;
            }
        }

        public override void OnPreviewSettings()
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialPreviewSettingsGUI(this);
            else
                DefaultPreviewSettingsGUI();
        }

        private bool PreviewSettingsMenuButton(out Rect buttonRect)
        {
            buttonRect = GUILayoutUtility.GetRect(14, 24, 14, 20);

            const float iconWidth = 16f;
            const float iconHeight = 6f;
            Rect iconRect = new Rect(buttonRect.x + (buttonRect.width - iconWidth) / 2, buttonRect.y + (buttonRect.height - iconHeight) / 2, iconWidth, iconHeight);

            if (Event.current.type == EventType.Repaint)
                Styles.kReflectionProbePickerStyle.Draw(iconRect, false, false, false, false);

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
                return true;

            return false;
        }

        public void DefaultPreviewSettingsGUI()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            Init();

            var mat = target as Material;
            var viewType = GetPreviewType(mat);
            if (targets.Length > 1 || viewType == PreviewType.Mesh)
            {
                m_TimeUpdate = PreviewGUI.CycleButton(m_TimeUpdate, s_TimeIcons);
                m_SelectedMesh = PreviewGUI.CycleButton(m_SelectedMesh, s_MeshIcons);
                m_LightMode = PreviewGUI.CycleButton(m_LightMode, s_LightIcons);

                Rect settingsButton;
                if (PreviewSettingsMenuButton(out settingsButton))
                    PopupWindow.Show(settingsButton, m_ReflectionProbePicker);
            }
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            Init();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview();

            return m_PreviewUtility.EndStaticPreview();
        }

        private void DoRenderPreview()
        {
            if (m_PreviewUtility.renderTexture.width <= 0 || m_PreviewUtility.renderTexture.height <= 0)
                return;

            var mat = target as Material;
            var viewType = GetPreviewType(mat);

            m_PreviewUtility.camera.transform.position = -Vector3.forward * 5;
            m_PreviewUtility.camera.transform.rotation = Quaternion.identity;
            if (m_LightMode == 0)
            {
                m_PreviewUtility.lights[0].intensity = 1.0f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0);
                m_PreviewUtility.lights[1].intensity = 0;
            }
            else
            {
                m_PreviewUtility.lights[0].intensity = 1.0f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
                m_PreviewUtility.lights[1].intensity = 1.0f;
            }

            m_PreviewUtility.ambientColor = new Color(.2f, .2f, .2f, 0);

            Quaternion rot = Quaternion.identity;
            if (DoesPreviewAllowRotation(viewType))
                rot = Quaternion.Euler(m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, m_PreviewDir.x, 0);
            Mesh mesh = s_Meshes[m_SelectedMesh];

            switch (viewType)
            {
                case PreviewType.Plane:
                    mesh = s_PlaneMesh;
                    break;
                case PreviewType.Mesh:
                    // We need to rotate camera, so we can see different reflections from different angles
                    // If we would only rotate object, the reflections would stay the same
                    m_PreviewUtility.camera.transform.position = Quaternion.Inverse(rot) * m_PreviewUtility.camera.transform.position;
                    m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
                    rot = Quaternion.identity;
                    break;
                case PreviewType.Skybox:
                    mesh = null;
                    m_PreviewUtility.camera.transform.rotation = Quaternion.Inverse(rot);
                    m_PreviewUtility.camera.fieldOfView = 120.0f;
                    break;
            }

            if (mesh != null)
            {
                m_PreviewUtility.DrawMesh(mesh, Vector3.zero, rot, mat, 0, null, m_ReflectionProbePicker.Target, false);
            }

            m_PreviewUtility.Render(true);
            if (viewType == PreviewType.Skybox)
            {
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                InternalEditorUtility.DrawSkyboxMaterial(mat, m_PreviewUtility.camera);
                GL.sRGBWrite = false;
            }
        }

        public sealed override bool HasPreviewGUI()
        {
            return true;
        }

        public override bool RequiresConstantRepaint()
        {
            return m_TimeUpdate == 1;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialInteractivePreviewGUI(this, r, background);
            else
                base.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialPreviewGUI(this, r, background);
            else
                DefaultPreviewGUI(r, background);
        }

        public void DefaultPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Material preview \nnot available");
                return;
            }

            Init();

            var mat = target as Material;
            var viewType = GetPreviewType(mat);

            if (DoesPreviewAllowRotation(viewType))
                m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r,  background);

            DoRenderPreview();

            m_PreviewUtility.EndAndDrawPreview(r);
        }

        public virtual void OnEnable()
        {
            if (!target)
                return;
            m_Shader = serializedObject.FindProperty("m_Shader").objectReferenceValue as Shader;
            m_CustomEditorClassName = "";
            CreateCustomShaderEditorIfNeeded(m_Shader);

            m_EnableInstancing = serializedObject.FindProperty("m_EnableInstancingVariants");
            m_DoubleSidedGI =  serializedObject.FindProperty("m_DoubleSidedGI");

            s_MaterialEditors.Add(this);
            Undo.undoRedoPerformed += UndoRedoPerformed;
            PropertiesChanged();

            m_PropertyBlock = new MaterialPropertyBlock();

            m_ReflectionProbePicker.OnEnable();
        }

        public virtual void UndoRedoPerformed()
        {
            // Undo could've restored old shader which might lead to change in custom editor class
            // therefore we need to rebuild inspector
            UpdateAllOpenMaterialEditors();

            PropertiesChanged();
        }

        public virtual void OnDisable()
        {
            m_ReflectionProbePicker.OnDisable();
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            s_MaterialEditors.Remove(this);
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        // Handle dragging of material onto renderers
        internal void OnSceneDrag(SceneView sceneView)
        {
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
                return;

            var materialIndex = -1;
            var go = HandleUtility.PickGameObject(evt.mousePosition, out materialIndex);

            if (EditorMaterialUtility.IsBackgroundMaterial((target as Material)))
            {
                HandleSkybox(go, evt);
            }
            else if (go && go.GetComponent<Renderer>())
                HandleRenderer(go.GetComponent<Renderer>(), materialIndex, evt);
        }

        internal void HandleSkybox(GameObject go, Event evt)
        {
            bool draggingOverBackground = !go;
            var applyAndConsumeEvent = false;

            if (!draggingOverBackground || evt.type == EventType.DragExited)
            {
                // cancel material assignment, if not hovering over background anymore
                evt.Use();
            }
            else
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        applyAndConsumeEvent = true;
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        applyAndConsumeEvent = true;
                        break;
                }

            if (applyAndConsumeEvent)
            {
                Undo.RecordObject(FindObjectOfType<RenderSettings>(), "Assign Skybox Material");

                RenderSettings.skybox = target as Material;

                evt.Use();
            }
        }

        internal void HandleRenderer(Renderer r, int materialIndex, Event evt)
        {
            if (r.GetType().GetCustomAttributes(typeof(RejectDragAndDropMaterial), true).Length > 0)
                return;

            var applyAndConsumeEvent = false;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    applyAndConsumeEvent = true;
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    applyAndConsumeEvent = true;
                    break;
            }

            if (applyAndConsumeEvent)
            {
                Undo.RecordObject(r, "Assign Material");
                var materials = r.sharedMaterials;

                bool altIsDown = evt.alt;
                bool isValidMaterialIndex = (materialIndex >= 0 && materialIndex < r.sharedMaterials.Length);
                if (!altIsDown && isValidMaterialIndex)
                {
                    materials[materialIndex] = target as Material;
                }
                else
                {
                    for (int q = 0; q < materials.Length; ++q)
                        materials[q] = target as Material;
                }

                r.sharedMaterials = materials;
                evt.Use();
            }
        }
    }
} // namespace UnityEditor
