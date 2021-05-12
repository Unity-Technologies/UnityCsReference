// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    class ExternalForcesModuleUI : ModuleUI
    {
        SerializedMinMaxCurve m_Multiplier;
        SerializedProperty m_InfluenceFilter;
        SerializedProperty m_InfluenceMask;
        SerializedProperty m_InfluenceList;

        ReorderableList m_InfluenceListView;

        class Texts
        {
            public GUIContent multiplier = EditorGUIUtility.TrTextContent("Multiplier", "Used to scale the force applied to this Particle System. If you use a curve to set this value, the Particle System applies the curve over the lifetime of each particle.");
            public GUIContent influenceFilter = EditorGUIUtility.TrTextContent("Influence Filter", "Use either a LayerMask or a List, to decide which Force Fields affect this Particle System.");
            public GUIContent influenceMask = EditorGUIUtility.TrTextContent("Influence Mask", "Select a global mask of which GameObjects can affect this Particle System.");
            public GUIContent createForceField = EditorGUIUtility.TrTextContent("", "Create a GameObject containing a Particle System Force Field and assign it to the list.");

            public GUIContent[] influenceFilters = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Layer Mask"),
                EditorGUIUtility.TrTextContent("List"),
                EditorGUIUtility.TrTextContent("Layer Mask and List")
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

            m_Multiplier = new SerializedMinMaxCurve(this, s_Texts.multiplier, "multiplierCurve");
            m_InfluenceFilter = GetProperty("influenceFilter");
            m_InfluenceMask = GetProperty("influenceMask");
            m_InfluenceList = GetProperty("influenceList");

            m_InfluenceListView = new ReorderableList(serializedObject, m_InfluenceList, true, true, true, true);
            m_InfluenceListView.elementHeight = kReorderableListElementHeight;
            m_InfluenceListView.headerHeight = 0;
            m_InfluenceListView.drawElementCallback = DrawInfluenceListElementCallback;
            m_InfluenceListView.onAddCallback = OnAddForceFieldElementCallback;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIMinMaxCurve(s_Texts.multiplier, m_Multiplier);

            ParticleSystemGameObjectFilter filter = (ParticleSystemGameObjectFilter)GUIPopup(s_Texts.influenceFilter, m_InfluenceFilter, s_Texts.influenceFilters);
            if (m_InfluenceFilter.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Influence List editing is only available when all selected systems have the same filter type", MessageType.Info, true);
            }
            else
            {
                if (filter != ParticleSystemGameObjectFilter.List)
                    GUILayerMask(s_Texts.influenceMask, m_InfluenceMask);
                if (filter != ParticleSystemGameObjectFilter.LayerMask)
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

        void OnAddForceFieldElementCallback(ReorderableList list)
        {
            int index = m_InfluenceList.arraySize;
            m_InfluenceList.InsertArrayElementAtIndex(index);
            m_InfluenceList.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }

        private void DrawInfluenceListElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty forceField = m_InfluenceList.GetArrayElementAtIndex(index);

            rect.height = kSingleLineHeight;
            Rect objectRect = new Rect(rect.x, rect.y, rect.width - EditorGUI.kSpacing - ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
            GUIObject(objectRect, GUIContent.none, forceField, null);

            if (forceField.objectReferenceValue == null)
            {
                Rect buttonRect = new Rect(objectRect.xMax + EditorGUI.kSpacing, rect.y + 4, ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
                if (GUI.Button(buttonRect, s_Texts.createForceField, ParticleSystemStyles.Get().plus))
                {
                    GameObject go = CreateDefaultForceField("ForceField " + (index + 1), m_ParticleSystemUI.m_ParticleSystems[0]);
                    go.transform.localPosition = new Vector3(0, 0, 10 + index); // ensure each force field is not at the same position
                    forceField.objectReferenceValue = go.GetComponent<ParticleSystemForceField>();
                }
            }
        }

        private static GameObject CreateDefaultForceField(string name, ParticleSystem parentOfGameObject)
        {
            GameObject go = new GameObject(name);
            if (go)
            {
                if (parentOfGameObject)
                    go.transform.parent = parentOfGameObject.transform;
                go.AddComponent<ParticleSystemForceField>();
                return go;
            }
            return null;
        }
    } // namespace UnityEditor
}
