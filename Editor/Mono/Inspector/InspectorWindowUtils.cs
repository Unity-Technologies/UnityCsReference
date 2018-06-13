// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class InspectorWindowUtils
    {
        public static void GetPreviewableTypes(out Dictionary<Type, List<Type>> previewableTypes)
        {
            // We initialize this list once per InspectorWindow, instead of globally.
            // This means that if the user is debugging an IPreviewable structure,
            // the InspectorWindow can be closed and reopened to refresh this list.

            previewableTypes = new Dictionary<Type, List<Type>>();
            foreach (var type in EditorAssemblies.GetAllTypesWithInterface<IPreviewable>())
            {
                // we don't want Editor classes with preview here.
                if (type.IsSubclassOf(typeof(Editor)))
                {
                    continue;
                }

                // Record only the types with a CustomPreviewAttribute.
                var attrs = type.GetCustomAttributes(typeof(CustomPreviewAttribute), false) as CustomPreviewAttribute[];
                foreach (CustomPreviewAttribute previewAttr in attrs)
                {
                    if (previewAttr.m_Type == null)
                    {
                        continue;
                    }

                    List<Type> types;
                    if (!previewableTypes.TryGetValue(previewAttr.m_Type, out types))
                    {
                        types = new List<Type>();
                        previewableTypes.Add(previewAttr.m_Type, types);
                    }

                    types.Add(type);
                }
            }
        }

        public static Editor GetFirstNonImportInspectorEditor(Editor[] editors)
        {
            foreach (Editor e in editors)
            {
                // Check for target rather than the editor type itself,
                // because some importers use default inspector
                if (e.target is AssetImporter)
                {
                    continue;
                }

                return e;
            }

            return null;
        }

        internal static void FlushOptimizedGUIBlock(Editor editor)
        {
            if (editor == null)
            {
                return;
            }

            OptimizedGUIBlock optimizedBlock;
            float height;
            if (editor.GetOptimizedGUIBlock(false, false, out optimizedBlock, out height))
            {
                optimizedBlock.valid = false;
            }
        }

        public static void DisplayDeprecationMessageIfNecessary(Editor editor)
        {
            if (!editor || !editor.target)
            {
                return;
            }

            var obsoleteAttribute = (ObsoleteAttribute)Attribute.GetCustomAttribute(editor.target.GetType(), typeof(ObsoleteAttribute));
            if (obsoleteAttribute == null)
            {
                return;
            }

            string message = String.IsNullOrEmpty(obsoleteAttribute.Message) ? "This component has been marked as obsolete." : obsoleteAttribute.Message;
            EditorGUILayout.HelpBox(message, obsoleteAttribute.IsError ? MessageType.Error : MessageType.Warning);
        }

        public static bool GetRebuildOptimizedGUIBlocks(Object inspectedObject, ref bool isOpenForEdit, ref bool invalidateGUIBlockCache)
        {
            var rebuildOptimizedGUIBlocks = false;

            if (Event.current.type == EventType.Repaint)
            {
                string msg;
                if (inspectedObject != null
                    && isOpenForEdit != Editor.IsAppropriateFileOpenForEdit(inspectedObject, out msg))
                {
                    isOpenForEdit = !isOpenForEdit;
                    rebuildOptimizedGUIBlocks = true;
                }

                if (invalidateGUIBlockCache)
                {
                    rebuildOptimizedGUIBlocks = true;
                    invalidateGUIBlockCache = false;
                }
            }
            else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                rebuildOptimizedGUIBlocks = true;
            }

            return rebuildOptimizedGUIBlocks;
        }
    }
}
