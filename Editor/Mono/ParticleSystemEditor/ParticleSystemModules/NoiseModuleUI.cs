// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;


namespace UnityEditor
{
    class NoiseModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_StrengthX;
        SerializedMinMaxCurve m_StrengthY;
        SerializedMinMaxCurve m_StrengthZ;
        SerializedProperty m_SeparateAxes;
        SerializedProperty m_Frequency;
        SerializedProperty m_Damping;
        SerializedProperty m_Octaves;
        SerializedProperty m_OctaveMultiplier;
        SerializedProperty m_OctaveScale;
        SerializedProperty m_Quality;
        SerializedMinMaxCurve m_ScrollSpeed;
        SerializedMinMaxCurve m_RemapX;
        SerializedMinMaxCurve m_RemapY;
        SerializedMinMaxCurve m_RemapZ;
        SerializedProperty m_RemapEnabled;
        SerializedMinMaxCurve m_PositionAmount;
        SerializedMinMaxCurve m_RotationAmount;
        SerializedMinMaxCurve m_SizeAmount;

        const int k_PreviewSize = 96;
        static Texture2D s_PreviewTexture;
        static bool s_PreviewTextureDirty = true;

        GUIStyle previewTextureStyle;

        class Texts
        {
            public GUIContent separateAxes = EditorGUIUtility.TextContent("Separate Axes|If enabled, you can control the noise separately for each axis.");
            public GUIContent strength = EditorGUIUtility.TextContent("Strength|How strong the overall noise effect is.");
            public GUIContent frequency = EditorGUIUtility.TextContent("Frequency|Low values create soft, smooth noise, and high values create rapidly changing noise.");
            public GUIContent damping = EditorGUIUtility.TextContent("Damping|If enabled, strength is proportional to frequency.");
            public GUIContent octaves = EditorGUIUtility.TextContent("Octaves|Layers of noise that combine to produce final noise (Adding octaves increases the performance cost substantially!)");
            public GUIContent octaveMultiplier = EditorGUIUtility.TextContent("Octave Multiplier|When combining each octave, scale the intensity by this amount.");
            public GUIContent octaveScale = EditorGUIUtility.TextContent("Octave Scale|When combining each octave, zoom in by this amount.");
            public GUIContent quality = EditorGUIUtility.TextContent("Quality|Generate 1D, 2D or 3D noise.");
            public GUIContent scrollSpeed = EditorGUIUtility.TextContent("Scroll Speed|Scroll the noise map over the particle system.");
            public GUIContent remap = EditorGUIUtility.TextContent("Remap|Remap the final noise values into a new range.");
            public GUIContent remapCurve = EditorGUIUtility.TextContent("Remap Curve");
            public GUIContent positionAmount = EditorGUIUtility.TextContent("Position Amount|What proportion of the noise is applied to the particle positions.");
            public GUIContent rotationAmount = EditorGUIUtility.TextContent("Rotation Amount|What proportion of the noise is applied to the particle rotations, in degrees per second.");
            public GUIContent sizeAmount = EditorGUIUtility.TextContent("Size Amount|Multiply the size of the particle by a proportion of the noise.");
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");
            public GUIContent previewTexture = EditorGUIUtility.TextContent("Preview|Preview the noise as a texture.");
            public GUIContent previewTextureMultiEdit = EditorGUIUtility.TextContent("Preview (Disabled)|Preview is disabled in multi-object editing mode.");

            public GUIContent[] qualityDropdown = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Low (1D)"),
                EditorGUIUtility.TextContent("Medium (2D)"),
                EditorGUIUtility.TextContent("High (3D)")
            };
        }
        private static Texts s_Texts;

        public NoiseModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "NoiseModule", displayName)
        {
            m_ToolTip = "Add noise/turbulence to particle movement.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_StrengthX != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_StrengthX = new SerializedMinMaxCurve(this, s_Texts.x, "strength", kUseSignedRange);
            m_StrengthY = new SerializedMinMaxCurve(this, s_Texts.y, "strengthY", kUseSignedRange);
            m_StrengthZ = new SerializedMinMaxCurve(this, s_Texts.z, "strengthZ", kUseSignedRange);
            m_SeparateAxes = GetProperty("separateAxes");
            m_Damping = GetProperty("damping");
            m_Frequency = GetProperty("frequency");
            m_Octaves = GetProperty("octaves");
            m_OctaveMultiplier = GetProperty("octaveMultiplier");
            m_OctaveScale = GetProperty("octaveScale");
            m_Quality = GetProperty("quality");
            m_ScrollSpeed = new SerializedMinMaxCurve(this, s_Texts.scrollSpeed, "scrollSpeed", kUseSignedRange);
            m_ScrollSpeed.m_AllowRandom = false;
            m_RemapX = new SerializedMinMaxCurve(this, s_Texts.x, "remap", kUseSignedRange);
            m_RemapY = new SerializedMinMaxCurve(this, s_Texts.y, "remapY", kUseSignedRange);
            m_RemapZ = new SerializedMinMaxCurve(this, s_Texts.z, "remapZ", kUseSignedRange);
            m_RemapX.m_AllowRandom = false;
            m_RemapY.m_AllowRandom = false;
            m_RemapZ.m_AllowRandom = false;
            m_RemapX.m_AllowConstant = false;
            m_RemapY.m_AllowConstant = false;
            m_RemapZ.m_AllowConstant = false;
            m_RemapEnabled = GetProperty("remapEnabled");
            m_PositionAmount = new SerializedMinMaxCurve(this, s_Texts.positionAmount, "positionAmount", kUseSignedRange);
            m_RotationAmount = new SerializedMinMaxCurve(this, s_Texts.rotationAmount, "rotationAmount", kUseSignedRange);
            m_SizeAmount = new SerializedMinMaxCurve(this, s_Texts.sizeAmount, "sizeAmount", kUseSignedRange);

            if (s_PreviewTexture == null)
            {
                s_PreviewTexture = new Texture2D(k_PreviewSize, k_PreviewSize, TextureFormat.RGBA32, false, true);
                s_PreviewTexture.name = "ParticleNoisePreview";
                s_PreviewTexture.filterMode = FilterMode.Bilinear;
                s_PreviewTexture.hideFlags = HideFlags.HideAndDontSave;
                s_Texts.previewTexture.image = s_PreviewTexture;
                s_Texts.previewTextureMultiEdit.image = s_PreviewTexture;
            }
            s_PreviewTextureDirty = true;

            previewTextureStyle = new GUIStyle(ParticleSystemStyles.Get().label);
            previewTextureStyle.alignment = TextAnchor.LowerCenter;
            previewTextureStyle.imagePosition = ImagePosition.ImageAbove;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            // values take 1 frame to appear in the module, so delay our generation of the preview texture using this bool
            if (s_PreviewTextureDirty)
            {
                if (m_ParticleSystemUI.multiEdit)
                {
                    Color32[] pixels = new Color32[s_PreviewTexture.width * s_PreviewTexture.height];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(120, 120, 120, 255);
                    s_PreviewTexture.SetPixels32(pixels);
                    s_PreviewTexture.Apply(false);
                }
                else
                {
                    m_ParticleSystemUI.m_ParticleSystems[0].GenerateNoisePreviewTexture(s_PreviewTexture);
                }
                s_PreviewTextureDirty = false;
            }

            if (!isWindowView)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
            }

            EditorGUI.BeginChangeCheck();
            bool separateAxes = GUIToggle(s_Texts.separateAxes, m_SeparateAxes);
            bool separateAxesChanged = EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();

            // Remove old curves from curve editor
            if (separateAxesChanged && !separateAxes)
            {
                m_StrengthY.RemoveCurveFromEditor();
                m_StrengthZ.RemoveCurveFromEditor();
                m_RemapY.RemoveCurveFromEditor();
                m_RemapZ.RemoveCurveFromEditor();
            }

            // Keep states in sync
            if (!m_StrengthX.stateHasMultipleDifferentValues)
            {
                m_StrengthZ.SetMinMaxState(m_StrengthX.state, separateAxes);
                m_StrengthY.SetMinMaxState(m_StrengthX.state, separateAxes);
            }
            if (!m_RemapX.stateHasMultipleDifferentValues)
            {
                m_RemapZ.SetMinMaxState(m_RemapX.state, separateAxes);
                m_RemapY.SetMinMaxState(m_RemapX.state, separateAxes);
            }

            if (separateAxes)
            {
                m_StrengthX.m_DisplayName = s_Texts.x;
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_StrengthX, s_Texts.y, m_StrengthY, s_Texts.z, m_StrengthZ, null);
            }
            else
            {
                m_StrengthX.m_DisplayName = s_Texts.strength;
                GUIMinMaxCurve(s_Texts.strength, m_StrengthX);
            }

            GUIFloat(s_Texts.frequency, m_Frequency);
            GUIMinMaxCurve(s_Texts.scrollSpeed, m_ScrollSpeed);
            GUIToggle(s_Texts.damping, m_Damping);

            int numOctaves = GUIInt(s_Texts.octaves, m_Octaves);
            using (new EditorGUI.DisabledScope(numOctaves == 1))
            {
                GUIFloat(s_Texts.octaveMultiplier, m_OctaveMultiplier);
                GUIFloat(s_Texts.octaveScale, m_OctaveScale);
            }

            GUIPopup(s_Texts.quality, m_Quality, s_Texts.qualityDropdown);

            EditorGUI.BeginChangeCheck();
            bool remapEnabled = GUIToggle(s_Texts.remap, m_RemapEnabled);
            bool remapEnabledChanged = EditorGUI.EndChangeCheck();

            // Remove old curves from curve editor
            if (remapEnabledChanged && !remapEnabled)
            {
                m_RemapX.RemoveCurveFromEditor();
                m_RemapY.RemoveCurveFromEditor();
                m_RemapZ.RemoveCurveFromEditor();
            }

            using (new EditorGUI.DisabledScope(remapEnabled == false))
            {
                if (separateAxes)
                {
                    m_RemapX.m_DisplayName = s_Texts.x;
                    GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_RemapX, s_Texts.y, m_RemapY, s_Texts.z, m_RemapZ, null);
                }
                else
                {
                    m_RemapX.m_DisplayName = s_Texts.remap;
                    GUIMinMaxCurve(s_Texts.remapCurve, m_RemapX);
                }
            }

            GUIMinMaxCurve(s_Texts.positionAmount, m_PositionAmount);
            GUIMinMaxCurve(s_Texts.rotationAmount, m_RotationAmount);
            GUIMinMaxCurve(s_Texts.sizeAmount, m_SizeAmount);

            if (!isWindowView)
                GUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() || m_ScrollSpeed.scalar.floatValue != 0.0f || remapEnabled || separateAxesChanged)
            {
                s_PreviewTextureDirty = true;
                m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.Repaint();
            }

            if (m_ParticleSystemUI.multiEdit)
                GUILayout.Label(s_Texts.previewTextureMultiEdit, previewTextureStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            else
                GUILayout.Label(s_Texts.previewTexture, previewTextureStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

            if (!isWindowView)
                GUILayout.EndHorizontal();
        }

        public override void UpdateCullingSupportedString(ref string text)
        {
            text += "\nNoise module is enabled.";
        }
    }
} // namespace UnityEditor
