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
        class SceneModeData : ScriptableSingleton<SceneModeData>
        {
            public string focusTypeName = null;
            public SceneHierarchyWindow hierarchyWindow = null;
        }

        private static Type s_FocusType = null;
        private static Type focusType
        {
            get
            {
                if(s_FocusType == null && focusTypeName != null)
                    s_FocusType = Type.GetType(focusTypeName);

                return s_FocusType;
            }
            set
            {
                s_FocusType = value;
                focusTypeName = s_FocusType?.AssemblyQualifiedName;
            }
        }

        private static string focusTypeName
        {
            get => SceneModeData.instance.focusTypeName;
            set => SceneModeData.instance.focusTypeName = value;
        }

        private static SceneHierarchyWindow hierarchyWindow
        {
            get => SceneModeData.instance.hierarchyWindow;
            set => SceneModeData.instance.hierarchyWindow = value;
        }


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
                hierarchyWindow = win;
                if (type == null || type == typeof(GameObject))
                {
                    focusType = null;
                    win.ClearSearchFilter();
                }
                else
                {
                    focusType = type;
                    if (win.searchMode == SearchableEditorWindow.SearchMode.Name)
                        win.searchMode = SearchableEditorWindow.SearchMode.All;
                    win.SetSearchFilter("t:" + type.Name, win.searchMode, false);
                    win.hasSearchFilterFocus = true;
                }
            }
            else
                focusType = null;
        }

        public static Type SearchBar(params Type[] types)
        {
            if (s_NoneButtonContent == null)
            {
                s_NoneButtonContent = EditorGUIUtility.IconContent("sv_icon_none");
                s_NoneButtonContent.text = "None";
            }

            if (s_FocusType != null &&
                (hierarchyWindow == null || hierarchyWindow.m_SearchFilter != "t:" + s_FocusType.Name))
            {
                focusType = null;
            }

            GUILayout.Label("Scene Filter:");

            EditorGUILayout.BeginHorizontal();

            {
                GUIContent label = EditorGUIUtility.TempContent(
                    "All",
                    AssetPreview.GetMiniTypeThumbnail(typeof(GameObject)));
                if (TypeButton(label, focusType == null, styles.typeButton))
                    SceneModeUtility.SearchForType(null);
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                Texture2D icon = null;
                if (type == typeof(Renderer))
                    icon = EditorGUIUtility.IconContent<MeshRenderer>().image as Texture2D;
                else if (type == typeof(Terrain))
                    icon = EditorGUIUtility.IconContent<Terrain>().image as Texture2D;
                else
                    icon = AssetPreview.GetMiniTypeThumbnail(type);
                string name = ObjectNames.NicifyVariableName(type.Name) + "s";
                GUIContent label = EditorGUIUtility.TempContent(name, icon);
                if (TypeButton(label, type == focusType, styles.typeButton))
                    SceneModeUtility.SearchForType(type);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            return focusType;
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
            bool allFlagsAreChanged = (changedFlags == int.MaxValue);
            var msgChangedFlags = changedFlags;
            if (msgChangedFlags < 0 && !allFlagsAreChanged)
            {
                //In order to have a list of human readable list of changed flags,
                //we need to filter out bits that does not correspont to any option.
                int allPossibleValues = 0;
                var values = Enum.GetValues(typeof(StaticEditorFlags));
                foreach (var value in values)
                {
                    allPossibleValues |= (int)value;
                }

                msgChangedFlags = msgChangedFlags & allPossibleValues;
            }
            StaticEditorFlags flag = allFlagsAreChanged ?
                (StaticEditorFlags)0 :
                (StaticEditorFlags)Enum.Parse(typeof(StaticEditorFlags), msgChangedFlags.ToString());


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

                // Following change is for backward compatibility after fixing case 1221145
                if (goFlags < 0)
                    goFlags = int.MaxValue;

                // MaxValue will cause issues when changing it to other values so we set it to the max possible value
                // that Static Editor flags can have before doing anything else with it
                if (goFlags == int.MaxValue && flagValue == false)
                    goFlags = (int)Math.Pow(2, Enum.GetNames(typeof(StaticEditorFlags)).Length - 1) - 1;

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
