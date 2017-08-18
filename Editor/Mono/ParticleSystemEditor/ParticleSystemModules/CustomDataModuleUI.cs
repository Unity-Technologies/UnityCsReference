// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class CustomDataModuleUI : ModuleUI
    {
        enum Mode { Disabled = 0, Vector = 1, Color = 2 };
        const int k_NumCustomDataStreams = 2;
        const int k_NumChannelsPerStream = 4;

        SerializedProperty[] m_Modes = new SerializedProperty[k_NumCustomDataStreams];
        SerializedProperty[] m_VectorComponentCount = new SerializedProperty[k_NumCustomDataStreams];
        SerializedMinMaxCurve[,] m_Vectors = new SerializedMinMaxCurve[k_NumCustomDataStreams, k_NumChannelsPerStream];
        SerializedMinMaxGradient[] m_Colors = new SerializedMinMaxGradient[k_NumCustomDataStreams];
        SerializedProperty[,] m_VectorLabels = new SerializedProperty[k_NumCustomDataStreams, k_NumChannelsPerStream];
        SerializedProperty[] m_ColorLabels = new SerializedProperty[k_NumCustomDataStreams];

        class Texts
        {
            public GUIContent mode = EditorGUIUtility.TextContent("Mode|Select the type of data to populate this stream with.");
            public GUIContent vectorComponentCount = EditorGUIUtility.TextContent("Number of Components|How many of the components (XYZW) to fill.");

            public GUIContent[] modes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Disabled"),
                EditorGUIUtility.TextContent("Vector"),
                EditorGUIUtility.TextContent("Color")
            };
        }
        static Texts s_Texts;

        public CustomDataModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "CustomDataModule", displayName)
        {
            m_ToolTip = "Configure custom data to be read in scripts or shaders. Use GetCustomParticleData from script, or send to shaders using the Custom Vertex Streams.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Modes[0] != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            for (int i = 0; i < k_NumCustomDataStreams; i++)
            {
                m_Modes[i] = GetProperty("mode" + i);
                m_VectorComponentCount[i] = GetProperty("vectorComponentCount" + i);
                m_Colors[i] = new SerializedMinMaxGradient(this, "color" + i);
                m_ColorLabels[i] = GetProperty("colorLabel" + i);
                for (int j = 0; j < k_NumChannelsPerStream; j++)
                {
                    m_Vectors[i, j] = new SerializedMinMaxCurve(this, GUIContent.none, "vector" + i + "_" + j, kUseSignedRange);
                    m_VectorLabels[i, j] = GetProperty("vectorLabel" + i + "_" + j);
                }
            }
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            for (int i = 0; i < k_NumCustomDataStreams; i++)
            {
                GUILayout.BeginVertical("Custom" + (i + 1), GUI.skin.window);

                Mode mode = (Mode)GUIPopup(s_Texts.mode, m_Modes[i], s_Texts.modes);
                if (mode == Mode.Vector)
                {
                    int vectorComponentCount = Mathf.Min(GUIInt(s_Texts.vectorComponentCount, m_VectorComponentCount[i]), k_NumChannelsPerStream);
                    for (int j = 0; j < vectorComponentCount; j++)
                    {
                        GUIMinMaxCurve(m_VectorLabels[i, j], m_Vectors[i, j]);
                    }
                }
                else if (mode == Mode.Color)
                {
                    GUIMinMaxGradient(m_ColorLabels[i], m_Colors[i], true);
                }

                GUILayout.EndVertical();
            }
        }
    }
} // namespace UnityEditor
