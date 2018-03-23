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
                // AssetImporterEditor never applies the AssetImporter's SerializedObject because it uses it for the Apply/Revert mechanic.
                // This means we need to write/read the data directly from the SerializedObject instead of the AssetImporter itself
                // And thus, we can't use Presets directly.
                instance.m_ImporterEditors = Resources
                    .FindObjectsOfTypeAll<AssetImporterEditor>()
                    .Where(e => e.targets == targets)
                    .ToArray();
                // m_Targets needs to keep a dummy version of each real target to avoid overriding the real importers.
                var dummyPresets = targets.Select(t => new Preset(t));
                instance.m_Targets = dummyPresets.Select(p => p.GetReferenceObject()).ToArray();
                instance.m_ImporterSerialized = new SerializedObject(instance.m_Targets);
                // copy values from the first editor serializedObject, because user may have done changes we want to apply back when selecting none.
                var currentEditorValues = instance.m_ImporterEditors[0].m_SerializedObject;
                // Do only apply on the properties that are part of the Preset modifications list
                // That will particularly avoid the AudioImporter preview data that can be a 30k+ array we don't want.
                var presetProperties = dummyPresets.First().PropertyModifications;
                string propertyPath = "";
                foreach (var propertyModification in presetProperties)
                {
                    // We need to filter out .Array.* properties and use the parent one instead,
                    // because changing .Array.size from CopyFromSerializedProperty will corrupt the SerializedObject data.
                    if (!string.IsNullOrEmpty(propertyPath) && propertyModification.propertyPath.StartsWith(propertyPath + ".Array."))
                    {
                        continue;
                    }
                    if (propertyModification.propertyPath.Contains(".Array."))
                    {
                        propertyPath = propertyModification.propertyPath.Substring(0, propertyModification.propertyPath.IndexOf(".Array."));
                    }
                    else
                    {
                        propertyPath = propertyModification.propertyPath;
                    }
                    instance.m_ImporterSerialized.CopyFromSerializedProperty(currentEditorValues.FindProperty(propertyPath));
                }
                instance.m_ImporterSerialized.ApplyModifiedPropertiesWithoutUndo();
                // create a list of Presets that contains the current values of each object
                instance.m_Presets = instance.m_Targets.Select(t => new Preset(t)).ToArray();
            }
            else
            {
                instance.m_Targets = targets;
                instance.m_Presets = targets.Select(t => new Preset(t)).ToArray();
            }
            PresetSelector.ShowSelector(targets[0], null, true, instance);
        }

        void ApplyPresetSerializedObjectToEditorOnes()
        {
            var presetProperties = m_Presets[0].PropertyModifications;
            foreach (var importerEditor in m_ImporterEditors)
            {
                string propertyPath = "";
                // We need to revert the editor serializedobject first
                // to make sure Apply/Revert button stay disabled if the Preset did not make any changes.
                importerEditor.m_SerializedObject.SetIsDifferentCacheDirty();
                importerEditor.m_SerializedObject.Update();
                // Do only apply on the properties that are part of the Preset modifications list
                // That will particularly avoid the AudioImporter preview data that can be a 30k+ array we don't want.
                foreach (var propertyModification in presetProperties)
                {
                    // We need to filter out .Array.* properties and use the parent one instead,
                    // because changing .Array.size from CopyFromSerializedProperty will corrupt the SerializedObject data.
                    if (!string.IsNullOrEmpty(propertyPath) && propertyModification.propertyPath.StartsWith(propertyPath + ".Array."))
                    {
                        continue;
                    }
                    if (propertyModification.propertyPath.Contains(".Array."))
                    {
                        propertyPath = propertyModification.propertyPath.Substring(0, propertyModification.propertyPath.IndexOf(".Array."));
                    }
                    else
                    {
                        propertyPath = propertyModification.propertyPath;
                    }
                    importerEditor.m_SerializedObject.CopyFromSerializedPropertyIfDifferent(m_ImporterSerialized.FindProperty(propertyPath));
                }
            }
        }

        void RevertValues()
        {
            if (m_ImporterEditors != null)
            {
                for (int i = 0; i < m_Targets.Length; i++)
                {
                    m_Presets[i].ApplyTo(m_Targets[i]);
                }
                m_ImporterSerialized.Update();
                ApplyPresetSerializedObjectToEditorOnes();
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
                    m_ImporterSerialized.Update();
                    ApplyPresetSerializedObjectToEditorOnes();
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
