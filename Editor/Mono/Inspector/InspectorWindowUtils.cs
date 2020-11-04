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
        public struct LayoutGroupChecker : IDisposable
        {
            // cache the layout group we expect to have at the end of drawing this editor
            GUILayoutGroup m_ExpectedGroup;
            GUILayoutGroup expectedGroup => m_ExpectedGroup ?? (m_ExpectedGroup = GUILayoutUtility.current.topLevel);

            public void Dispose()
            {
                if (GUIUtility.guiIsExiting)
                {
                    return; //Something has already requested an ExitGUI
                }

                // Check and try to cleanup layout groups.
                if (GUILayoutUtility.current.topLevel != expectedGroup)
                {
                    if (!GUILayoutUtility.current.layoutGroups.Contains(expectedGroup))
                    {
                        // We can't recover from this, so we error.
                        Debug.LogError("Expected top level layout group missing! Too many GUILayout.EndScrollView/EndVertical/EndHorizontal?");
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        // We can recover from this, so we warning.
                        Debug.LogWarning("Unexpected top level layout group! Missing GUILayout.EndScrollView/EndVertical/EndHorizontal?");

                        while (GUILayoutUtility.current.topLevel != expectedGroup)
                            GUILayoutUtility.EndLayoutGroup();
                    }
                }
            }
        }

        public static void GetPreviewableTypes(out Dictionary<Type, List<Type>> previewableTypes)
        {
            // We initialize this list once per InspectorWindow, instead of globally.
            // This means that if the user is debugging an IPreviewable structure,
            // the InspectorWindow can be closed and reopened to refresh this list.

            previewableTypes = new Dictionary<Type, List<Type>>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IPreviewable>())
            {
                // we don't want Editor classes with preview here.
                if (type.IsSubclassOf(typeof(Editor)))
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"{type} does not contain a default constructor, it will not be registered as a " +
                        $"preview handler. Use the Initialize function to set up your object instead.");
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

        internal static bool IsExcludedClass(Object target)
        {
            return ModuleMetadata.GetModuleIncludeSettingForObject(target) == ModuleIncludeSetting.ForceExclude;
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

        public static void DrawAddedComponentBackground(Rect position, Object[] targets, float adjust = 0)
        {
            if (Event.current.type == EventType.Repaint && targets.Length == 1)
            {
                Component comp = targets[0] as Component;
                if (comp != null &&
                    EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None &&
                    PrefabUtility.GetCorrespondingConnectedObjectFromSource(comp.gameObject) != null &&
                    PrefabUtility.GetCorrespondingObjectFromSource(comp) == null)
                {
                    // Ensure colored margin here for component body doesn't overlap colored margin from InspectorTitlebar,
                    // and extends down to exactly touch the separator line between/after components.
                    EditorGUI.DrawOverrideBackground(new Rect(position.x, position.y + 3 + adjust, position.width,
                        position.height - 2));
                }
            }
        }
    }
}
