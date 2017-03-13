// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class GradientPresetLibrary : PresetLibrary
    {
        [SerializeField]
        List<GradientPreset> m_Presets = new List<GradientPreset>();

        public override int Count()
        {
            return m_Presets.Count;
        }

        public override object GetPreset(int index)
        {
            return m_Presets[index].gradient;
        }

        public override void Add(object presetObject, string presetName)
        {
            Gradient gradient = presetObject as Gradient;
            if (gradient == null)
            {
                Debug.LogError("Wrong type used in GradientPresetLibrary");
                return;
            }

            Gradient copy = new Gradient();
            copy.alphaKeys = gradient.alphaKeys;
            copy.colorKeys = gradient.colorKeys;
            copy.mode = gradient.mode;
            m_Presets.Add(new GradientPreset(copy, presetName));
        }

        public override void Replace(int index, object newPresetObject)
        {
            Gradient gradient = newPresetObject as Gradient;
            if (gradient == null)
            {
                Debug.LogError("Wrong type used in GradientPresetLibrary");
                return;
            }

            Gradient copy = new Gradient();
            copy.alphaKeys = gradient.alphaKeys;
            copy.colorKeys = gradient.colorKeys;
            copy.mode = gradient.mode;
            m_Presets[index].gradient = copy;
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
            DrawInternal(rect, m_Presets[index].gradient);
        }

        public override void Draw(Rect rect, object presetObject)
        {
            DrawInternal(rect, presetObject as Gradient);
        }

        private void DrawInternal(Rect rect, Gradient gradient)
        {
            if (gradient == null)
                return;
            GradientEditor.DrawGradientWithBackground(rect, gradient);
        }

        public override string GetName(int index)
        {
            return m_Presets[index].name;
        }

        public override void SetName(int index, string presetName)
        {
            m_Presets[index].name = presetName;
        }

        public void DebugCreateTonsOfPresets()
        {
            int count = 150; // well not a ton, but..
            string autoName = "Preset_";
            for (int i = 0; i < count; ++i)
            {
                List<GradientColorKey> cols = new List<GradientColorKey>();
                int numColors = (int)Random.Range(3, 8);
                for (int j = 0; j < numColors; ++j)
                    cols.Add(new GradientColorKey(new Color(Random.value, Random.value, Random.value), Random.value));

                List<GradientAlphaKey> alps = new List<GradientAlphaKey>();
                int numAlphas = (int)Random.Range(3, 8);
                for (int j = 0; j < numAlphas; ++j)
                    alps.Add(new GradientAlphaKey(Random.value, Random.value));

                Gradient g = new Gradient();
                g.colorKeys = cols.ToArray();
                g.alphaKeys = alps.ToArray();
                Add(g, autoName + (i + 1));
            }
        }

        [System.Serializable]
        class GradientPreset
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            Gradient m_Gradient;

            public GradientPreset(Gradient preset, string presetName)
            {
                gradient = preset;
                name = presetName;
            }

            public Gradient gradient
            {
                get { return m_Gradient; }
                set { m_Gradient = value; }
            }

            public string name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }
        }
    }
}
