// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Custom editor for the Post-process Build component. The default inspector can't instantiate an
    /// <see cref="IBuildProfilePostprocessBuild"/> for a <c>[SerializeReference]</c> element, so this editor
    /// offers an add dropdown listing the concrete implementers. Callbacks run in list order, so the list is reorderable.
    /// </summary>
    [CustomEditor(typeof(BuildProfilePostprocessBuild))]
    class BuildProfilePostprocessBuildEditor : Editor
    {
        // Leaves room for the foldout arrow of each callback's serialized fields
        const float k_FoldoutIndent = 12;

        SerializedProperty m_Callbacks;
        ReorderableList m_List;

        void OnEnable()
        {
            m_Callbacks = serializedObject.FindProperty("m_Callbacks");
            m_List = new ReorderableList(serializedObject, m_Callbacks, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, TrText.postprocessBuildCallbacksHeader),
                drawElementCallback = DrawElement,
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(m_Callbacks.GetArrayElementAtIndex(index), true),
                onAddDropdownCallback = (rect, _) => ShowAddMenu(rect),
            };
        }

        public override VisualElement CreateInspectorGUI()
        {
            return new IMGUIContainer(DrawInspector);
        }

        void DrawInspector()
        {
            serializedObject.Update();

            if (m_Callbacks.arraySize == 0)
                EditorGUILayout.HelpBox(TrText.postprocessBuildNoCallbacks, MessageType.None);

            m_List.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_Callbacks.GetArrayElementAtIndex(index);
            rect.xMin += k_FoldoutIndent;
            EditorGUI.PropertyField(rect, element, new GUIContent(CallbackName(element)), true);
        }

        static string CallbackName(SerializedProperty element)
        {
            var value = element.managedReferenceValue;
            return value != null ? ObjectNames.NicifyVariableName(value.GetType().Name) : "Callback";
        }

        void ShowAddMenu(Rect rect)
        {
            var entries = new List<(string name, Type type)>();
            foreach (var type in GetEligibleCallbackTypes())
                entries.Add((ObjectNames.NicifyVariableName(type.Name), type));

            // Types with the same name in different namespaces keep a fixed order
            entries.Sort((a, b) =>
            {
                int byName = EditorUtility.NaturalCompare(a.name, b.name);
                return byName != 0 ? byName : string.CompareOrdinal(a.type.FullName, b.type.FullName);
            });

            var menu = new GenericMenu();
            foreach (var (name, type) in entries)
                menu.AddItem(new GUIContent(name), false, () => AddCallback(type));

            if (entries.Count == 0)
                menu.AddDisabledItem(new GUIContent(TrText.postprocessBuildNoTypesFound));

            menu.DropDown(rect);
        }

        void AddCallback(Type type)
        {
            serializedObject.Update();
            int index = m_Callbacks.arraySize;
            m_Callbacks.InsertArrayElementAtIndex(index);
            m_Callbacks.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
            m_List.index = index;
        }

        internal static IEnumerable<Type> GetEligibleCallbackTypes()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IBuildProfilePostprocessBuild>())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                // SerializeReference only stores [Serializable] classes that don't derive from UnityEngine.Object
                if (!type.IsClass || !type.IsSerializable || typeof(UnityEngine.Object).IsAssignableFrom(type))
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                yield return type;
            }
        }
    }
}
