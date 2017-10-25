// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class PresetContextMenu
    {
        static class Style
        {
            public static GUIContent presetIcon = EditorGUIUtility.IconContent("Preset.Context");
            public static GUIContent saveAsPreset = new GUIContent("Save Preset");
        }

        static IEnumerable<Preset> FindAllPresetsForObject(Object target)
        {
            return AssetDatabase.FindAssets("t:Preset")
                .Select(a => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(a)))
                .Where(preset => preset.CanBeAppliedTo(target));
        }

        private class PropertyModComparer : IEqualityComparer<PropertyModification>
        {
            public bool Equals(PropertyModification x, PropertyModification y)
            {
                return x.propertyPath == y.propertyPath && x.value == y.value;
            }

            public int GetHashCode(PropertyModification obj)
            {
                return obj.propertyPath.GetHashCode() << 2 & obj.value.GetHashCode();
            }
        }

        [EditorHeaderItem(typeof(Object), -999)]
        static bool DisplayPresetsMenu(Rect rectangle, Object[] targets)
        {
            var target = targets[0];
            if (Preset.IsExcludedFromPresets(target))
                return false;

            if ((target.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            // don't display on empty components
            if (target is Component)
            {
                var singleObject = new SerializedObject(target);
                if (!singleObject.GetIterator().NextVisible(true))
                    return false;
            }

            if (EditorGUI.DropdownButton(rectangle, Style.presetIcon , FocusType.Passive,
                    EditorStyles.iconButton))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(Style.saveAsPreset, false, targets.Length == 1 ? CreatePreset : (GenericMenu.MenuFunction2)null, target);

                foreach (var preset in FindAllPresetsForObject(target))
                {
                    var tpl = new Tuple<Preset, Object[]>(preset, targets);
                    menu.AddItem(new GUIContent("Apply Preset/" + preset.name), false, ApplyPreset, tpl);
                }
                if (menu.GetItemCount() == 1)
                {
                    menu.AddDisabledItem(new GUIContent("Apply Preset/No matching Preset found"));
                }

                menu.DropDown(rectangle);
            }
            return true;
        }

        static bool ApplyImportSettingsBeforeSavingPreset(ref Preset preset, Object target)
        {
            // make sure modifications to importer get applied before creating preset.
            foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
            {
                ActiveEditorTracker activeEditor = i.tracker;
                foreach (Editor e in activeEditor.activeEditors)
                {
                    var editor = e as AssetImporterEditor;
                    if (editor != null && editor.target == target && editor.HasModified())
                    {
                        if (EditorUtility.DisplayDialog("Unapplied import settings", "Apply settings before creating a new preset", "Apply", "Cancel"))
                        {
                            editor.ApplyAndImport();
                            // after reimporting, the target object has changed, so update the preset with the newly imported values.
                            preset.UpdateProperties(editor.target);
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        static string CreatePresetDialog(ref Preset preset, Object target)
        {
            if (target is AssetImporter && ApplyImportSettingsBeforeSavingPreset(ref preset, target))
                return null;

            return EditorUtility.SaveFilePanelInProject("New Preset", preset.GetTargetTypeName(), "preset", "", ProjectWindowUtil.GetActiveFolderPath());
        }

        static void CreatePreset(object target)
        {
            Object targetObject = target as Object;
            var preset = new Preset(targetObject);
            var path = CreatePresetDialog(ref preset, targetObject);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(preset, path);
            }
        }

        static void ApplyPreset(object presetObjectsTuple)
        {
            var tpl = presetObjectsTuple as Tuple<Preset, Object[]>;

            if (tpl.Item2[0] is AssetImporter)
            {
                foreach (var o in tpl.Item2)
                {
                    tpl.Item1.ApplyTo(o);
                    ((AssetImporter)o).SaveAndReimport();
                }
            }
            else
            {
                Undo.RecordObjects(tpl.Item2, "Apply Preset");
                foreach (var o in tpl.Item2)
                    tpl.Item1.ApplyTo(o);
            }
        }
    }
}
