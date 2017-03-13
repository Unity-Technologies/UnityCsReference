// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class CurvePresetLibrary : PresetLibrary
    {
        [SerializeField]
        List<CurvePreset> m_Presets = new List<CurvePreset>();

        public override int Count()
        {
            return m_Presets.Count;
        }

        public override object GetPreset(int index)
        {
            return m_Presets[index].curve;
        }

        public override void Add(object presetObject, string presetName)
        {
            AnimationCurve curve = presetObject as AnimationCurve;
            if (curve == null)
            {
                Debug.LogError("Wrong type used in CurvePresetLibrary");
                return;
            }

            AnimationCurve copy = new AnimationCurve(curve.keys);
            copy.preWrapMode = curve.preWrapMode;
            copy.postWrapMode = curve.postWrapMode;
            m_Presets.Add(new CurvePreset(copy, presetName));
        }

        public override void Replace(int index, object newPresetObject)
        {
            AnimationCurve curve = newPresetObject as AnimationCurve;
            if (curve == null)
            {
                Debug.LogError("Wrong type used in CurvePresetLibrary");
                return;
            }
            AnimationCurve copy = new AnimationCurve(curve.keys);
            copy.preWrapMode = curve.preWrapMode;
            copy.postWrapMode = curve.postWrapMode;

            m_Presets[index].curve = copy;
        }

        public override void Remove(int index)
        {
            m_Presets.RemoveAt(index);
        }

        public override void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            PresetLibraryHelpers.MoveListItem(m_Presets, index, destIndex, insertAfterDestIndex);
        }

        public override void Draw(Rect rect, int index)
        {
            DrawInternal(rect, m_Presets[index].curve);
        }

        public override void Draw(Rect rect, object presetObject)
        {
            DrawInternal(rect, presetObject as AnimationCurve);
        }

        private void DrawInternal(Rect rect, AnimationCurve animCurve)
        {
            if (animCurve == null)
                return;
            EditorGUIUtility.DrawCurveSwatch(rect, animCurve, null, new Color(0.8f, 0.8f, 0.8f, 1.0f), EditorGUI.kCurveBGColor);
        }

        public override string GetName(int index)
        {
            return m_Presets[index].name;
        }

        public override void SetName(int index, string presetName)
        {
            m_Presets[index].name = presetName;
        }

        [System.Serializable]
        class CurvePreset
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            AnimationCurve m_Curve;

            public CurvePreset(AnimationCurve preset, string presetName)
            {
                curve = preset;
                name = presetName;
            }

            public CurvePreset(AnimationCurve preset, AnimationCurve preset2, string presetName)
            {
                curve = preset;
                name = presetName;
            }

            public AnimationCurve curve
            {
                get { return m_Curve; }
                set { m_Curve = value; }
            }

            public string name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }
        }
    }
}
