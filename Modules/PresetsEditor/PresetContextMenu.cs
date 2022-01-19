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
            var targets = m_Targets.Where(t => t != null).ToArray();
            if (targets.Length == 0)
                return;

            Undo.RecordObjects(targets, "Cancel Preset");
            for (int i = 0; i < targets.Length; i++)
            {
                m_Presets[i].ApplyTo(targets[i]);
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
                ApplyValues(selection);
            }
            InspectorWindow.RepaintAllInspectors();
            SettingsService.RepaintAllSettingsWindow();
        }

        void ApplyValues(Preset selection)
        {
            var targets = m_Targets.Where(t => t != null).ToArray();
            if (targets.Length == 0)
                return;

            Undo.RecordObjects(targets, "Apply Preset " + selection.name);
            foreach (var target in targets)
            {
                selection.ApplyTo(target);
            }
        }

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            DestroyImmediate(this);
        }
    }
}
