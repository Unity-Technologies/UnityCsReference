// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    class TrailModuleUI : ModuleUI
    {
        class Texts
        {
            public GUIContent mode = new GUIContent("Mode", "Select how trails are generated on the particles.");
            public GUIContent ratio = new GUIContent("Ratio", "Choose what proportion of particles will receive a trail.");
            public GUIContent lifetime = EditorGUIUtility.TextContent("Lifetime|How long each trail will last, relative to the life of the particle.");
            public GUIContent minVertexDistance = EditorGUIUtility.TextContent("Minimum Vertex Distance|The minimum distance each trail can travel before adding a new vertex.");
            public GUIContent textureMode = EditorGUIUtility.TextContent("Texture Mode|Should the U coordinate be stretched or tiled?");
            public GUIContent worldSpace = EditorGUIUtility.TextContent("World Space|Trail points will be dropped in world space, even if the particle system is simulating in local space.");
            public GUIContent dieWithParticles = EditorGUIUtility.TextContent("Die with Particles|The trails will disappear when their owning particles die.");
            public GUIContent sizeAffectsWidth = EditorGUIUtility.TextContent("Size affects Width|The trails will use the particle size to control their width.");
            public GUIContent sizeAffectsLifetime = EditorGUIUtility.TextContent("Size affects Lifetime|The trails will use the particle size to control their lifetime.");
            public GUIContent inheritParticleColor = EditorGUIUtility.TextContent("Inherit Particle Color|The trails will use the particle color as their base color.");
            public GUIContent colorOverLifetime = EditorGUIUtility.TextContent("Color over Lifetime|The color of the trails during the lifetime of the particle they are attached to.");
            public GUIContent widthOverTrail = EditorGUIUtility.TextContent("Width over Trail|Select a width for the trail from its start to end vertex.");
            public GUIContent colorOverTrail = EditorGUIUtility.TextContent("Color over Trail|Select a color for the trail from its start to end vertex.");
            public GUIContent generateLightingData = EditorGUIUtility.TextContent("Generate Lighting Data|Toggle generation of normal and tangent data, for use in lit shaders.");
            public GUIContent ribbonCount = EditorGUIUtility.TextContent("Ribbon Count|Select how many ribbons to render throughout the Particle System.");
            public GUIContent splitSubEmitterRibbons = EditorGUIUtility.TextContent("Split Sub Emitter Ribbons|When used on a sub emitter, ribbons will connect particles from each parent particle independently.");

            public GUIContent[] trailModeOptions =
            {
                EditorGUIUtility.TextContent("Particles"),
                EditorGUIUtility.TextContent("Ribbon")
            };

            public GUIContent[] textureModeOptions = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Stretch"),
                EditorGUIUtility.TextContent("Tile"),
                EditorGUIUtility.TextContent("DistributePerSegment"),
                EditorGUIUtility.TextContent("RepeatPerSegment")
            };
        }
        private static Texts s_Texts;

        SerializedProperty m_Mode;
        SerializedProperty m_Ratio;
        SerializedMinMaxCurve m_Lifetime;
        SerializedProperty m_MinVertexDistance;
        SerializedProperty m_TextureMode;
        SerializedProperty m_WorldSpace;
        SerializedProperty m_DieWithParticles;
        SerializedProperty m_SizeAffectsWidth;
        SerializedProperty m_SizeAffectsLifetime;
        SerializedProperty m_InheritParticleColor;
        SerializedMinMaxGradient m_ColorOverLifetime;
        SerializedMinMaxCurve m_WidthOverTrail;
        SerializedMinMaxGradient m_ColorOverTrail;
        SerializedProperty m_GenerateLightingData;
        SerializedProperty m_RibbonCount;
        SerializedProperty m_SplitSubEmitterRibbons;

        public TrailModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "TrailModule", displayName)
        {
            m_ToolTip = "Attach trails to the particles.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Ratio != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Mode = GetProperty("mode");
            m_Ratio = GetProperty("ratio");
            m_Lifetime = new SerializedMinMaxCurve(this, s_Texts.lifetime, "lifetime");
            m_MinVertexDistance = GetProperty("minVertexDistance");
            m_TextureMode = GetProperty("textureMode");
            m_WorldSpace = GetProperty("worldSpace");
            m_DieWithParticles = GetProperty("dieWithParticles");
            m_SizeAffectsWidth = GetProperty("sizeAffectsWidth");
            m_SizeAffectsLifetime = GetProperty("sizeAffectsLifetime");
            m_InheritParticleColor = GetProperty("inheritParticleColor");
            m_ColorOverLifetime = new SerializedMinMaxGradient(this, "colorOverLifetime");
            m_WidthOverTrail = new SerializedMinMaxCurve(this, s_Texts.widthOverTrail, "widthOverTrail");
            m_ColorOverTrail = new SerializedMinMaxGradient(this, "colorOverTrail");
            m_GenerateLightingData = GetProperty("generateLightingData");
            m_RibbonCount = GetProperty("ribbonCount");
            m_SplitSubEmitterRibbons = GetProperty("splitSubEmitterRibbons");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            ParticleSystemTrailMode mode = (ParticleSystemTrailMode)GUIPopup(s_Texts.mode, m_Mode, s_Texts.trailModeOptions);
            if (!m_Mode.hasMultipleDifferentValues)
            {
                if (mode == ParticleSystemTrailMode.PerParticle)
                {
                    GUIFloat(s_Texts.ratio, m_Ratio);
                    GUIMinMaxCurve(s_Texts.lifetime, m_Lifetime);
                    GUIFloat(s_Texts.minVertexDistance, m_MinVertexDistance);
                    GUIToggle(s_Texts.worldSpace, m_WorldSpace);
                    GUIToggle(s_Texts.dieWithParticles, m_DieWithParticles);
                }
                else
                {
                    GUIInt(s_Texts.ribbonCount, m_RibbonCount);
                    GUIToggle(s_Texts.splitSubEmitterRibbons, m_SplitSubEmitterRibbons);
                }
            }

            GUIPopup(s_Texts.textureMode, m_TextureMode, s_Texts.textureModeOptions);
            GUIToggle(s_Texts.sizeAffectsWidth, m_SizeAffectsWidth);

            if (!m_Mode.hasMultipleDifferentValues)
            {
                if (mode == ParticleSystemTrailMode.PerParticle)
                {
                    GUIToggle(s_Texts.sizeAffectsLifetime, m_SizeAffectsLifetime);
                }
            }

            GUIToggle(s_Texts.inheritParticleColor, m_InheritParticleColor);
            GUIMinMaxGradient(s_Texts.colorOverLifetime, m_ColorOverLifetime, false);
            GUIMinMaxCurve(s_Texts.widthOverTrail, m_WidthOverTrail);
            GUIMinMaxGradient(s_Texts.colorOverTrail, m_ColorOverTrail, false);
            GUIToggle(s_Texts.generateLightingData, m_GenerateLightingData);

            // Add a warning message when no trail material is assigned, telling users where to find it
            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                if (ps.trails.enabled)
                {
                    ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if ((renderer != null) && (renderer.trailMaterial == null))
                    {
                        EditorGUILayout.HelpBox("Assign a Trail Material in the Renderer Module", MessageType.Warning, true);
                        break;
                    }
                }
            }
        }

        public override void UpdateCullingSupportedString(ref string text)
        {
            Init();

            if (m_Mode.intValue == (int)ParticleSystemTrailMode.PerParticle)
                text += "\nTrails module is enabled.";
        }
    }
} // namespace UnityEditor
