// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    [System.Serializable]
    class DoubleCurve
    {
        [SerializeField]
        AnimationCurve m_MinCurve;

        [SerializeField]
        AnimationCurve m_MaxCurve;

        [SerializeField]
        bool m_SignedRange;


        public DoubleCurve(AnimationCurve minCurve, AnimationCurve maxCurve, bool signedRange)
        {
            // Ensure not to hold references to other curves
            AnimationCurve copy;
            if (minCurve != null)
            {
                copy = new AnimationCurve(minCurve.keys);
                m_MinCurve = copy;
            }

            if (maxCurve != null)
            {
                copy = new AnimationCurve(maxCurve.keys);
                m_MaxCurve = copy;
            }
            else
            {
                Debug.LogError("Ensure that maxCurve is not null when creating a double curve. The minCurve can be null for single curves");
            }

            m_SignedRange = signedRange;
        }

        public AnimationCurve minCurve
        {
            get { return m_MinCurve; }
            set { m_MinCurve = value; }
        }

        public AnimationCurve maxCurve
        {
            get { return m_MaxCurve; }
            set { m_MaxCurve = value; }
        }

        public bool signedRange
        {
            get { return m_SignedRange; }
            set { m_SignedRange = value; }
        }

        public bool IsSingleCurve()
        {
            return minCurve == null || minCurve.length == 0;
        }
    }

    class DoubleCurvePresetLibrary : PresetLibrary
    {
        [SerializeField]
        List<DoubleCurvePreset> m_Presets = new List<DoubleCurvePreset>();

        readonly Rect kUnsignedRange = new Rect(0, 0, 1, 1); // Vertical range 0...1
        readonly Rect kSignedRange = new Rect(0, -1, 1, 2); // Vertical range -1...1

        public override int Count()
        {
            return m_Presets.Count;
        }

        public override object GetPreset(int index)
        {
            return m_Presets[index].doubleCurve;
        }

        public override void Add(object presetObject, string presetName)
        {
            DoubleCurve doubleCurve = presetObject as DoubleCurve;
            if (doubleCurve == null)
            {
                Debug.LogError("Wrong type used in DoubleCurvePresetLibrary: Should be a DoubleCurve");
                return;
            }
            m_Presets.Add(new DoubleCurvePreset(doubleCurve, presetName));
        }

        public override void Replace(int index, object newPresetObject)
        {
            DoubleCurve doubleCurve = newPresetObject as DoubleCurve;
            if (doubleCurve == null)
            {
                Debug.LogError("Wrong type used in DoubleCurvePresetLibrary");
                return;
            }
            m_Presets[index].doubleCurve = doubleCurve;
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
            DrawInternal(rect, m_Presets[index].doubleCurve);
        }

        public override void Draw(Rect rect, object presetObject)
        {
            DrawInternal(rect, presetObject as DoubleCurve);
        }

        private void DrawInternal(Rect rect, DoubleCurve doubleCurve)
        {
            if (doubleCurve == null)
            {
                Debug.Log("DoubleCurve is null");
                return;
            }
            EditorGUIUtility.DrawRegionSwatch(rect, doubleCurve.maxCurve, doubleCurve.minCurve, new Color(0.8f, 0.8f, 0.8f, 1.0f), EditorGUI.kCurveBGColor, doubleCurve.signedRange ? kSignedRange : kUnsignedRange);
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
        class DoubleCurvePreset
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            DoubleCurve m_DoubleCurve;

            public DoubleCurvePreset(DoubleCurve doubleCurvePreset, string presetName)
            {
                doubleCurve = doubleCurvePreset;
                name = presetName;
            }

            public DoubleCurve doubleCurve
            {
                get { return m_DoubleCurve; }
                set { m_DoubleCurve = value; }
            }

            public string name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }
        }
    }
}
