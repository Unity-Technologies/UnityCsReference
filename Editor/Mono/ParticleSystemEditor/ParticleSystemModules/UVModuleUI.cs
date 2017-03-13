// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    class UVModuleUI : ModuleUI
    {
        // Keep in sync with enum in UVModule.h
        enum AnimationType { WholeSheet = 0, SingleRow = 1 };

        SerializedMinMaxCurve m_FrameOverTime;
        SerializedMinMaxCurve m_StartFrame;
        SerializedProperty m_TilesX;
        SerializedProperty m_TilesY;
        SerializedProperty m_AnimationType;
        SerializedProperty m_Cycles;
        SerializedProperty m_UVChannelMask;
        SerializedProperty m_RandomRow;
        SerializedProperty m_RowIndex;
        SerializedProperty m_FlipU;
        SerializedProperty m_FlipV;

        class Texts
        {
            public GUIContent frameOverTime = EditorGUIUtility.TextContent("Frame over Time|Controls the uv animation frame of each particle over its lifetime. On the horisontal axis you will find the lifetime. On the vertical axis you will find the sheet index.");
            public GUIContent startFrame = EditorGUIUtility.TextContent("Start Frame|Phase the animation, so it starts on a frame other than 0.");
            public GUIContent animation = EditorGUIUtility.TextContent("Animation|Specifies the animation type: Whole Sheet or Single Row. Whole Sheet will animate over the whole texture sheet from left to right, top to bottom. Single Row will animate a single row in the sheet from left to right.");
            public GUIContent tiles = EditorGUIUtility.TextContent("Tiles|Defines the tiling of the texture.");
            public GUIContent tilesX = EditorGUIUtility.TextContent("X");
            public GUIContent tilesY = EditorGUIUtility.TextContent("Y");
            public GUIContent row = EditorGUIUtility.TextContent("Row|The row in the sheet which will be played.");
            public GUIContent frame = EditorGUIUtility.TextContent("Frame|The frame in the sheet which will be used.");
            public GUIContent cycles = EditorGUIUtility.TextContent("Cycles|Specifies how many times the animation will loop during the lifetime of the particle.");
            public GUIContent uvChannelMask = EditorGUIUtility.TextContent("Enabled UV Channels|Specifies which UV channels will be animated.");
            public GUIContent randomRow = EditorGUIUtility.TextContent("Random Row|If enabled, the animated row will be chosen randomly.");
            public GUIContent flipU = EditorGUIUtility.TextContent("Flip U|Cause some particle texture mapping to be flipped horizontally. (Set between 0 and 1, where a higher value causes more to flip)");
            public GUIContent flipV = EditorGUIUtility.TextContent("Flip V|Cause some particle texture mapping to be flipped vertically. (Set between 0 and 1, where a higher value causes more to flip)");

            public string[] types = new string[] { "Whole Sheet", "Single Row"};
        }
        private static Texts s_Texts;


        public UVModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "UVModule", displayName)
        {
            m_ToolTip = "Particle UV animation. This allows you to specify a texture sheet (a texture with multiple tiles/sub frames) and animate or randomize over it per particle.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_TilesX != null)
                return;

            if (s_Texts == null)
                s_Texts = new Texts();

            m_FrameOverTime = new SerializedMinMaxCurve(this, s_Texts.frameOverTime, "frameOverTime");
            m_StartFrame = new SerializedMinMaxCurve(this, s_Texts.startFrame, "startFrame");
            m_StartFrame.m_AllowCurves = false;
            m_TilesX = GetProperty("tilesX");
            m_TilesY = GetProperty("tilesY");
            m_AnimationType = GetProperty("animationType");
            m_Cycles = GetProperty("cycles");
            m_RandomRow = GetProperty("randomRow");
            m_RowIndex = GetProperty("rowIndex");
            m_UVChannelMask = GetProperty("uvChannelMask");
            m_FlipU = GetProperty("flipU");
            m_FlipV = GetProperty("flipV");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            GUIIntDraggableX2(s_Texts.tiles, s_Texts.tilesX, m_TilesX, s_Texts.tilesY, m_TilesY);

            int type = GUIPopup(s_Texts.animation, m_AnimationType, s_Texts.types);
            if (type == (int)AnimationType.SingleRow)
            {
                GUIToggle(s_Texts.randomRow, m_RandomRow);
                if (!m_RandomRow.boolValue)
                    GUIInt(s_Texts.row, m_RowIndex);
            }

            if (type == (int)AnimationType.SingleRow)
            {
                m_FrameOverTime.m_RemapValue = (float)(m_TilesX.intValue);
                m_StartFrame.m_RemapValue = (float)(m_TilesX.intValue);
            }
            if (type == (int)AnimationType.WholeSheet)
            {
                m_FrameOverTime.m_RemapValue = (float)(m_TilesX.intValue * m_TilesY.intValue);
                m_StartFrame.m_RemapValue = (float)(m_TilesX.intValue * m_TilesY.intValue);
            }
            GUIMinMaxCurve(s_Texts.frameOverTime, m_FrameOverTime);
            GUIMinMaxCurve(s_Texts.startFrame, m_StartFrame);
            GUIFloat(s_Texts.cycles, m_Cycles);

            bool disableFlipping = false;
            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                if (ps.trails.enabled)
                {
                    ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if ((renderer != null) && (renderer.renderMode == ParticleSystemRenderMode.Mesh))
                    {
                        disableFlipping = true;
                        break;
                    }
                }
            }
            using (new EditorGUI.DisabledScope(disableFlipping))
            {
                GUIFloat(s_Texts.flipU, m_FlipU);
                GUIFloat(s_Texts.flipV, m_FlipV);
            }

            m_UVChannelMask.intValue = (int)(UVChannelFlags)GUIEnumMask(s_Texts.uvChannelMask, (UVChannelFlags)m_UVChannelMask.intValue);
        }
    }
} // namespace UnityEditor
