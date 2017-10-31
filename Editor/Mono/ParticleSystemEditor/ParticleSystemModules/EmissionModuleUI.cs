// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    class EmissionModuleUI : ModuleUI
    {
        public SerializedMinMaxCurve m_Time;
        public SerializedMinMaxCurve m_Distance;

        // Keep in sync with EmissionModule.h
        const int k_MaxNumBursts = 8;
        const float k_BurstDragWidth = 15.0f;
        SerializedProperty m_BurstCount;
        SerializedProperty m_Bursts;
        List<SerializedMinMaxCurve> m_BurstCountCurves = new List<SerializedMinMaxCurve>();

        ReorderableList m_BurstList;

        class Texts
        {
            public GUIContent rateOverTime = EditorGUIUtility.TextContent("Rate over Time|The number of particles emitted per second.");
            public GUIContent rateOverDistance = EditorGUIUtility.TextContent("Rate over Distance|The number of particles emitted per distance unit.");
            public GUIContent burst = EditorGUIUtility.TextContent("Bursts|Emission of extra particles at specific times during the duration of the system.");
            public GUIContent burstTime = EditorGUIUtility.TextContent("Time|When the burst will trigger.");
            public GUIContent burstCount = EditorGUIUtility.TextContent("Count|The number of particles to emit.");
            public GUIContent burstCycleCount = EditorGUIUtility.TextContent("Cycles|How many times to emit the burst. Use the dropdown to repeat infinitely.");
            public GUIContent burstCycleCountInfinite = EditorGUIUtility.TextContent("Infinite");
            public GUIContent burstRepeatInterval = EditorGUIUtility.TextContent("Interval|Repeat the burst every N seconds.");
        }
        private static Texts s_Texts;

        public EmissionModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "EmissionModule", displayName)
        {
            m_ToolTip = "Emission of the emitter. This controls the rate at which particles are emitted as well as burst emissions.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_BurstCount != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Time = new SerializedMinMaxCurve(this, s_Texts.rateOverTime, "rateOverTime");
            m_Distance = new SerializedMinMaxCurve(this, s_Texts.rateOverDistance, "rateOverDistance");

            m_BurstCount = GetProperty("m_BurstCount");
            m_Bursts = GetProperty("m_Bursts");

            m_BurstList = new ReorderableList(serializedObject, m_Bursts, false, true, true, true);
            m_BurstList.elementHeight = kReorderableListElementHeight;
            m_BurstList.onAddCallback = OnBurstListAddCallback;
            m_BurstList.onCanAddCallback = OnBurstListCanAddCallback;
            m_BurstList.onRemoveCallback = OnBurstListRemoveCallback;
            m_BurstList.drawHeaderCallback = DrawBurstListHeaderCallback;
            m_BurstList.drawElementCallback = DrawBurstListElementCallback;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIMinMaxCurve(s_Texts.rateOverTime, m_Time);
            GUIMinMaxCurve(s_Texts.rateOverDistance, m_Distance);

            DoBurstGUI(initial);
        }

        private void DoBurstGUI(InitialModuleUI initial)
        {
            while (m_BurstList.count > m_BurstCountCurves.Count)
            {
                SerializedProperty burst = m_Bursts.GetArrayElementAtIndex(m_BurstCountCurves.Count);
                m_BurstCountCurves.Add(new SerializedMinMaxCurve(this, s_Texts.burstCount, burst.propertyPath + ".countCurve", false, true));
            }

            EditorGUILayout.Space();

            Rect rect = GetControlRect(kSingleLineHeight);
            GUI.Label(rect, s_Texts.burst, ParticleSystemStyles.Get().label);

            m_BurstList.displayAdd = (m_Bursts.arraySize < k_MaxNumBursts);
            m_BurstList.DoLayoutList();
        }

        private void OnBurstListAddCallback(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            m_BurstCount.intValue++;

            SerializedProperty burst = m_Bursts.GetArrayElementAtIndex(list.index);
            SerializedProperty burstCountState = burst.FindPropertyRelative("countCurve.minMaxState");
            SerializedProperty burstCount = burst.FindPropertyRelative("countCurve.scalar");
            SerializedProperty burstCycleCount = burst.FindPropertyRelative("cycleCount");
            burstCountState.intValue = (int)ParticleSystemCurveMode.Constant;
            burstCount.floatValue = 30.0f;
            burstCycleCount.intValue = 1;

            SerializedProperty burstCountMinCurve = burst.FindPropertyRelative("countCurve.minCurve");
            SerializedProperty burstCountMaxCurve = burst.FindPropertyRelative("countCurve.maxCurve");
            burstCountMinCurve.animationCurveValue = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
            burstCountMaxCurve.animationCurveValue = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

            m_BurstCountCurves.Add(new SerializedMinMaxCurve(this, s_Texts.burstCount, burst.propertyPath + ".countCurve", false, true));
        }

        private bool OnBurstListCanAddCallback(ReorderableList list)
        {
            return !m_ParticleSystemUI.multiEdit;
        }

        private void OnBurstListRemoveCallback(ReorderableList list)
        {
            // All subsequent curves must be removed from the curve editor, as their indices will change, which means their SerializedProperty paths will also change
            for (int i = list.index; i < m_BurstCountCurves.Count; i++)
                m_BurstCountCurves[i].RemoveCurveFromEditor();
            m_BurstCountCurves.RemoveRange(list.index, m_BurstCountCurves.Count - list.index);
            AnimationCurvePreviewCache.ClearCache();

            // Default remove behavior
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            m_BurstCount.intValue--;
        }

        private void DrawBurstListHeaderCallback(Rect rect)
        {
            rect.width -= k_BurstDragWidth;
            rect.width /= 4;
            rect.x += k_BurstDragWidth;

            EditorGUI.LabelField(rect, s_Texts.burstTime, ParticleSystemStyles.Get().label);
            rect.x += rect.width;

            EditorGUI.LabelField(rect, s_Texts.burstCount, ParticleSystemStyles.Get().label);
            rect.x += rect.width;

            EditorGUI.LabelField(rect, s_Texts.burstCycleCount, ParticleSystemStyles.Get().label);
            rect.x += rect.width;

            EditorGUI.LabelField(rect, s_Texts.burstRepeatInterval, ParticleSystemStyles.Get().label);
            rect.x += rect.width;
        }

        private void DrawBurstListElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty burst = m_Bursts.GetArrayElementAtIndex(index);
            SerializedProperty burstTime = burst.FindPropertyRelative("time");
            SerializedProperty burstCycleCount = burst.FindPropertyRelative("cycleCount");
            SerializedProperty burstRepeatInterval = burst.FindPropertyRelative("repeatInterval");

            rect.width -= (k_BurstDragWidth * 3);
            rect.width /= 4;

            // Time
            FloatDraggable(rect, burstTime, 1.0f, k_BurstDragWidth, "n3");
            rect.x += rect.width;

            // Count
            rect = GUIMinMaxCurveInline(rect, m_BurstCountCurves[index], k_BurstDragWidth);
            rect.x += rect.width;

            // Cycle Count
            rect.width -= k_minMaxToggleWidth;
            if (!burstCycleCount.hasMultipleDifferentValues && burstCycleCount.intValue == 0)
            {
                rect.x += k_BurstDragWidth;
                rect.width -= k_BurstDragWidth;
                EditorGUI.LabelField(rect, s_Texts.burstCycleCountInfinite, ParticleSystemStyles.Get().label);
            }
            else
            {
                IntDraggable(rect, null, burstCycleCount, k_BurstDragWidth);
            }
            rect.width += k_minMaxToggleWidth;
            Rect popupRect = GetPopupRect(rect);
            GUIMMModePopUp(popupRect, burstCycleCount);
            rect.x += rect.width;

            // Repeat Interval
            FloatDraggable(rect, burstRepeatInterval, 1.0f, k_BurstDragWidth, "n3");
            rect.x += rect.width;
        }

        private class ModeCallbackData
        {
            public ModeCallbackData(int i, SerializedProperty p)
            {
                modeProp = p;
                selectedState = i;
            }

            public SerializedProperty modeProp;
            public int selectedState;
        }

        private static void SelectModeCallback(object obj)
        {
            ModeCallbackData data = (ModeCallbackData)obj;
            data.modeProp.intValue = (int)data.selectedState;
        }

        private static void GUIMMModePopUp(Rect rect, SerializedProperty modeProp)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                GUIContent[] texts = { new GUIContent("Infinite"), new GUIContent("Count") };

                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < texts.Length; ++i)
                {
                    menu.AddItem(texts[i], (modeProp.intValue == i), SelectModeCallback, new ModeCallbackData(i, modeProp));
                }

                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            if (m_Distance.scalar.hasMultipleDifferentValues || m_Distance.scalar.floatValue > 0.0f)
                text += "\nDistance-based emission is being used in the Emission module.";
        }
    }
} // namespace UnityEditor
