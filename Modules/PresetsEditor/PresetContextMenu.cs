// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Presets
{
    internal class PresetContextMenu : PresetSelectorReceiver
    {
        Object[] m_Targets;
        Preset[] m_Presets;
        AssetImporterEditor[] m_ImporterEditors;
        SerializedObject m_ImporterSerialized;

        internal static void CreateAndShow(Object[] targets)
        {
            var instance = CreateInstance<PresetContextMenu>();
            if (targets[0] is AssetImporter)
            {
                // we need to keep our own instance of the selected importer in order to handle the Apply/Reset correctly
                instance.m_ImporterEditors = Resources
                    .FindObjectsOfTypeAll<AssetImporterEditor>()
                    .Where(e => e.targets == targets)
                    .ToArray();
                instance.m_Targets = new[] {Instantiate(targets[0])};
                instance.m_ImporterSerialized = new SerializedObject(targets);
                var prop = instance.m_ImporterEditors[0].m_SerializedObject.GetIterator();
                while (prop.Next(true))
                {
                    instance.m_ImporterSerialized.CopyFromSerializedProperty(prop);
                }
            }
            else
            {
                instance.m_Targets = targets;
                instance.m_Presets = targets.Select(t => new Preset(t)).ToArray();
            }
            PresetSelector.ShowSelector(targets[0], null, true, instance);
        }

        void RevertValues()
        {
            if (m_ImporterEditors != null)
            {
                foreach (var importerEditor in m_ImporterEditors)
                {
                    importerEditor.m_SerializedObject.SetIsDifferentCacheDirty();
                    importerEditor.m_SerializedObject.Update();
                    var seria = m_ImporterSerialized.GetIterator();
                    while (seria.Next(true))
                    {
                        importerEditor.m_SerializedObject.CopyFromSerializedPropertyIfDifferent(seria);
                    }
                }
            }
            else
            {
                Undo.RecordObjects(m_Targets, "Cancel Preset");
                for (int i = 0; i < m_Targets.Length; i++)
                {
                    m_Presets[i].ApplyTo(m_Targets[i]);
                }
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
                if (m_ImporterEditors != null)
                {
                    foreach (var importerEditor in m_ImporterEditors)
                    {
                        importerEditor.m_SerializedObject.SetIsDifferentCacheDirty();
                        importerEditor.m_SerializedObject.Update();
                        var seria = new SerializedObject(m_Targets).GetIterator();
                        while (seria.Next(true))
                        {
                            importerEditor.m_SerializedObject.CopyFromSerializedPropertyIfDifferent(seria);
                        }
                    }
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
