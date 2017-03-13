// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public static class SceneModeUtility
    {
        private static Type s_FocusType = null;
        private static SceneHierarchyWindow s_HierarchyWindow = null;
        private static GUIContent s_NoneButtonContent = null;

        private class Styles
        {
            public GUIStyle typeButton = "SearchModeFilter";
        }
        private static Styles s_Styles;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        public static T[] GetSelectedObjectsOfType<T>(out GameObject[] gameObjects, params Type[] types) where T : Object
        {
            if (types.Length == 0)
                types = new Type[] { typeof(T) };

            List<GameObject> gameObjectList = new List<GameObject>();
            List<T> components = new List<T>();

            Transform[] selectedTransforms = Selection.GetTransforms(SelectionMode.ExcludePrefab | SelectionMode.Editable);
            foreach (Transform t in selectedTransforms)
            {
                foreach (Type type in types)
                {
                    Object comp = t.gameObject.GetComponent(type);
                    if (comp != null)
                    {
                        gameObjectList.Add(t.gameObject);
                        components.Add((T)comp);
                        break;
                    }
                }
            }
            gameObjects = gameObjectList.ToArray();
            return components.ToArray();
        }

        public static void SearchForType(Type type)
        {
            Object[] wins = Resources.FindObjectsOfTypeAll(typeof(SceneHierarchyWindow));
            SceneHierarchyWindow win = wins.Length > 0 ? (wins[0] as SceneHierarchyWindow) : null;

            if (win)
            {
                s_HierarchyWindow = win;
                if (type == null || type == typeof(GameObject))
                {
                    s_FocusType = null;
                    win.ClearSearchFilter();
                }
                else
                {
                    s_FocusType = type;
                    if (win.searchMode == SearchableEditorWindow.SearchMode.Name)
                        win.searchMode = SearchableEditorWindow.SearchMode.All;
                    win.SetSearchFilter("t:" + type.Name, win.searchMode, false);
                    win.hasSearchFilterFocus = true;
                }
            }
            else
                s_FocusType = null;
        }

        public static Type SearchBar(params Type[] types)
        {
            if (s_NoneButtonContent == null)
            {
                s_NoneButtonContent = EditorGUIUtility.IconContent("sv_icon_none");
                s_NoneButtonContent.text = "None";
            }

            if (s_FocusType != null && (s_HierarchyWindow == null || s_HierarchyWindow.m_SearchFilter != "t:" + s_FocusType.Name))
                s_FocusType = null;

            GUILayout.Label("Scene Filter:");

            EditorGUILayout.BeginHorizontal();

            {
                GUIContent label = EditorGUIUtility.TempContent(
                        "All",
                        AssetPreview.GetMiniTypeThumbnail(typeof(GameObject)));
                if (TypeButton(label, s_FocusType == null, styles.typeButton))
                    SceneModeUtility.SearchForType(null);
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                Texture2D icon = null;
                if (type == typeof(Renderer))
                    icon = EditorGUIUtility.IconContent("MeshRenderer Icon").image as Texture2D;
                else if (type == typeof(Terrain))
                    icon = EditorGUIUtility.IconContent("Terrain Icon").image as Texture2D;
                else
                    icon = AssetPreview.GetMiniTypeThumbnail(type);
                string name = ObjectNames.NicifyVariableName(type.Name) + "s";
                GUIContent label = EditorGUIUtility.TempContent(name, icon);
                if (TypeButton(label, type == s_FocusType, styles.typeButton))
                    SceneModeUtility.SearchForType(type);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            return s_FocusType;
        }

        private static bool TypeButton(GUIContent label, bool selected, GUIStyle style)
        {
            EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
            bool newSelected = GUILayout.Toggle(selected, label, style);
            EditorGUIUtility.SetIconSize(Vector2.zero);
            return newSelected && (newSelected != selected);
        }

        public static bool StaticFlagField(string label, SerializedProperty property, int flag)
        {
            bool oldToggle = (property.intValue & flag) != 0;
            bool oldDifferent = (property.hasMultipleDifferentValuesBitwise & flag) != 0;
            EditorGUI.showMixedValue = oldDifferent;
            EditorGUI.BeginChangeCheck();
            var toggle = EditorGUILayout.Toggle(label, oldToggle);
            if (EditorGUI.EndChangeCheck())
            {
                if (!SetStaticFlags(property.serializedObject.targetObjects, flag, toggle))
                    return oldToggle && !oldDifferent;
                return toggle;
            }
            EditorGUI.showMixedValue = false;
            return toggle && !oldDifferent;
        }

        public static bool SetStaticFlags(Object[] targetObjects, int changedFlags, bool flagValue)
        {
            bool allFlagsAreChanged = (changedFlags == ~0);
            StaticEditorFlags flag = allFlagsAreChanged ?
                (StaticEditorFlags)0 :
                (StaticEditorFlags)Enum.Parse(typeof(StaticEditorFlags), changedFlags.ToString());

            // Should we include child objects?
            GameObjectUtility.ShouldIncludeChildren includeChildren = GameObjectUtility.DisplayUpdateChildrenDialogIfNeeded(targetObjects.OfType<GameObject>(), "Change Static Flags",
                    allFlagsAreChanged ?
                    "Do you want to " + (flagValue ? "enable" : "disable") + " the static flags for all the child objects as well?" :
                    "Do you want to " + (flagValue ? "enable" : "disable") + " the " + ObjectNames.NicifyVariableName(flag.ToString()) + " flag for all the child objects as well?");

            if (includeChildren == GameObjectUtility.ShouldIncludeChildren.Cancel)
            {
                EditorGUIUtility.ExitGUI();
                return false;
            }
            var objects = GetObjects(targetObjects, includeChildren == GameObjectUtility.ShouldIncludeChildren.IncludeChildren);
            Undo.RecordObjects(objects, "Change Static Flags");

            // Calculate new flags value separately for each object so other flags are not affected.
            foreach (GameObject go in objects)
            {
                int goFlags = (int)GameObjectUtility.GetStaticEditorFlags(go);
                goFlags = flagValue ?
                    goFlags | changedFlags :
                    goFlags & ~changedFlags;
                GameObjectUtility.SetStaticEditorFlags(go, (StaticEditorFlags)goFlags);
            }

            return true;
        }

        static void GetObjectsRecurse(Transform root, List<GameObject> arr)
        {
            arr.Add(root.gameObject);
            foreach (Transform t in root)
                GetObjectsRecurse(t, arr);
        }

        public static GameObject[] GetObjects(Object[] gameObjects, bool includeChildren)
        {
            List<GameObject> allObjects = new List<GameObject>();
            if (!includeChildren)
            {
                foreach (GameObject go in gameObjects)
                    allObjects.Add(go);
            }
            else
            {
                foreach (GameObject go in gameObjects)
                    GetObjectsRecurse(go.transform, allObjects);
            }
            return allObjects.ToArray();
        }
    }
}
