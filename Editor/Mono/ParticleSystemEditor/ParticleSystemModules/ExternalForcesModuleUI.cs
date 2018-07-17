// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    class ExternalForcesModuleUI : ModuleUI
    {
        SerializedProperty m_Multiplier;
        SerializedProperty m_InfluenceFilter;
        SerializedProperty m_InfluenceMask;
        SerializedProperty m_InfluenceList;

        ReorderableList m_InfluenceListView;

        class Texts
        {
            public GUIContent multiplier = EditorGUIUtility.TrTextContent("Multiplier", "Used to scale the force applied to this particle system.");
            public GUIContent influenceFilter = EditorGUIUtility.TrTextContent("Influence Filter", "Use either a LayerMask or a List, to decide which Force Fields affect this Particle System.");
            public GUIContent influenceMask = EditorGUIUtility.TrTextContent("Influence Mask", "Select a global mask of which GameObjects can affect this Particle System.");

            public GUIContent[] influenceFilters = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("LayerMask"),
                EditorGUIUtility.TrTextContent("List")
            };
        }
        static Texts s_Texts;

        public ExternalForcesModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ExternalForcesModule", displayName)
        {
            m_ToolTip = "Controls the wind zones that each particle is affected by.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Multiplier != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Multiplier = GetProperty("multiplier");
            m_InfluenceFilter = GetProperty("influenceFilter");
            m_InfluenceMask = GetProperty("influenceMask");
            m_InfluenceList = GetProperty("influenceList");

            m_InfluenceListView = new ReorderableList(serializedObject, m_InfluenceList, true, true, true, true);
            m_InfluenceListView.elementHeight = kReorderableListElementHeight;
            m_InfluenceListView.headerHeight = 0;
            m_InfluenceListView.drawElementCallback = DrawInfluenceListElementCallback;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIFloat(s_Texts.multiplier, m_Multiplier);

            ParticleSystemGameObjectFilter filter = (ParticleSystemGameObjectFilter)GUIPopup(s_Texts.influenceFilter, m_InfluenceFilter, s_Texts.influenceFilters);
            if (m_InfluenceFilter.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Influence List editing is only available when all selected systems have the same filter type", MessageType.Info, true);
            }
            else
            {
                if (filter == ParticleSystemGameObjectFilter.LayerMask)
                    GUILayerMask(s_Texts.influenceMask, m_InfluenceMask);
                else
                    m_InfluenceListView.DoLayoutList();
            }
        }

        override public void OnSceneViewGUI()
        {
            using (new Handles.DrawingScope(Handles.matrix))
            {
                ParticleSystemForceField[] forceFields = ParticleSystemForceField.FindAll();
                foreach (ParticleSystemForceField ff in forceFields)
                {
                    bool valid = false;
                    foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                    {
                        if (ps.externalForces.IsAffectedBy(ff))
                        {
                            valid = true;
                            break;
                        }
                    }
                    if (!valid)
                        continue;

                    ParticleSystemForceFieldInspector.DrawHandle(ff);
                }
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nExternal Forces module is enabled.";
        }

        private void DrawInfluenceListElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty forceField = m_InfluenceList.GetArrayElementAtIndex(index);
            EditorGUI.ObjectField(rect, forceField, null, GUIContent.none, ParticleSystemStyles.Get().objectField);
        }
    } // namespace UnityEditor
}
