// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;

namespace UnityEditor.Presets
{
    internal class PresetContextMenu : PresetSelectorReceiver
    {
        Object[] m_Targets;
        Preset[] m_Presets;

        internal static void CreateAndShow(Object[] targets)
        {
            var instance = CreateInstance<PresetContextMenu>();
            instance.m_Targets = targets;
            instance.m_Presets = targets.Select(t => new Preset(t)).ToArray();
            PresetSelector.ShowSelector(targets[0], null, true, instance);
        }

        void RevertValues()
        {
            Undo.RecordObjects(m_Targets, "Cancel Preset");
            for (int i = 0; i < m_Targets.Length; i++)
            {
                m_Presets[i].ApplyTo(m_Targets[i]);
            }
        }

        public override void OnSelectionChanged(Preset selection)
        {
            if (selection == null)
            {
                RevertValues();
            }
            else
            {
                Undo.RecordObjects(m_Targets, "Apply Preset " + selection.name);
                foreach (var target in m_Targets)
                {
                    selection.ApplyTo(target);
                }
            }
            InspectorWindow.RepaintAllInspectors();
        }

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            DestroyImmediate(this);
        }
    }
}
