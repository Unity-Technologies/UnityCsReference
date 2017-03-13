// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class AnimationClipOverrideComparer : IComparer<KeyValuePair<AnimationClip, AnimationClip>>
    {
        public int Compare(KeyValuePair<AnimationClip, AnimationClip> x, KeyValuePair<AnimationClip, AnimationClip> y)
        {
            return string.Compare(x.Key.name, y.Key.name, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [CustomEditor(typeof(AnimatorOverrideController))]
    [CanEditMultipleObjects]
    internal class AnimatorOverrideControllerInspector : Editor
    {
        SerializedProperty m_Controller;

        private List<KeyValuePair<AnimationClip, AnimationClip>> m_Clips;

        ReorderableList m_ClipList;
        string m_Search;

        void OnEnable()
        {
            AnimatorOverrideController animatorOverrideController = target as AnimatorOverrideController;

            m_Controller = serializedObject.FindProperty("m_Controller");
            m_Search = "";

            if (m_Clips == null)
                m_Clips = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            if (m_ClipList == null)
            {
                animatorOverrideController.GetOverrides(m_Clips);

                m_Clips.Sort(new AnimationClipOverrideComparer());

                m_ClipList = new ReorderableList(m_Clips, typeof(KeyValuePair<AnimationClip, AnimationClip>), false, true, false, false);
                m_ClipList.drawElementCallback = DrawClipElement;
                m_ClipList.drawHeaderCallback = DrawClipHeader;
                m_ClipList.onSelectCallback = SelectClip;
                m_ClipList.elementHeight = 16;
            }
            animatorOverrideController.OnOverrideControllerDirty += Repaint;
        }

        void OnDisable()
        {
            AnimatorOverrideController animatorOverrideController = target as AnimatorOverrideController;
            animatorOverrideController.OnOverrideControllerDirty -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            bool isEditingMultipleObjects = targets.Length > 1;
            bool changeCheck = false;

            serializedObject.UpdateIfRequiredOrScript();

            AnimatorOverrideController animatorOverrideController = target as AnimatorOverrideController;
            RuntimeAnimatorController  runtimeAnimatorController  = m_Controller.hasMultipleDifferentValues ? null : animatorOverrideController.runtimeAnimatorController;

            EditorGUI.BeginChangeCheck();
            runtimeAnimatorController = EditorGUILayout.ObjectField("Controller", runtimeAnimatorController, typeof(Animations.AnimatorController), false) as RuntimeAnimatorController;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    AnimatorOverrideController controller = targets[i] as AnimatorOverrideController;
                    controller.runtimeAnimatorController = runtimeAnimatorController;
                }

                changeCheck = true;
            }

            {
                GUI.SetNextControlName("OverridesSearch");

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && GUI.GetNameOfFocusedControl() == "OverridesSearch")
                    m_Search = "";

                EditorGUI.BeginChangeCheck();
                string newSearch = EditorGUILayout.ToolbarSearchField(m_Search);
                if (EditorGUI.EndChangeCheck())
                    m_Search = newSearch;
            }


            using (new EditorGUI.DisabledScope(m_Controller == null || (isEditingMultipleObjects && m_Controller.hasMultipleDifferentValues) || runtimeAnimatorController == null))
            {
                EditorGUI.BeginChangeCheck();
                animatorOverrideController.GetOverrides(m_Clips);

                if (m_Search.Length > 0)
                    FilterOverrides();
                else // If there is not filter simply sort all the list.
                    m_Clips.Sort(new AnimationClipOverrideComparer());

                m_ClipList.list = m_Clips;
                m_ClipList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        AnimatorOverrideController controller = targets[i] as AnimatorOverrideController;
                        controller.ApplyOverrides(m_Clips);
                    }
                    changeCheck = true;
                }
            }

            if (changeCheck)
                animatorOverrideController.PerformOverrideClipListCleanup();
        }

        private void FilterOverrides()
        {
            if (m_Search.Length == 0)
                return;

            // Support multiple search words separated by spaces.
            string[] searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            List<KeyValuePair<AnimationClip, AnimationClip>> matchesStart = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            List<KeyValuePair<AnimationClip, AnimationClip>> matchesWithin = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (KeyValuePair<AnimationClip, AnimationClip> kvp in m_Clips)
            {
                string name = kvp.Key.name;
                name = name.ToLower().Replace(" ", "");

                bool didMatchAll = true;
                bool didMatchStart = false;

                // See if we match ALL the search words.
                for (int w = 0; w < searchWords.Length; w++)
                {
                    string search = searchWords[w];
                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }
                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(kvp);
                    else
                        matchesWithin.Add(kvp);
                }
            }

            m_Clips.Clear();

            matchesStart.Sort(new AnimationClipOverrideComparer());
            matchesWithin.Sort(new AnimationClipOverrideComparer());

            // Add search results
            m_Clips.AddRange(matchesStart);
            m_Clips.AddRange(matchesWithin);
        }

        private void DrawClipElement(Rect rect, int index, bool selected, bool focused)
        {
            AnimationClip originalClip = m_Clips[index].Key;
            AnimationClip overrideClip = m_Clips[index].Value;

            rect.xMax = rect.xMax / 2.0f;
            GUI.Label(rect, originalClip.name, EditorStyles.label);
            rect.xMin = rect.xMax;
            rect.xMax *= 2.0f;

            EditorGUI.BeginChangeCheck();
            overrideClip = EditorGUI.ObjectField(rect, "", overrideClip, typeof(AnimationClip), false) as AnimationClip;
            if (EditorGUI.EndChangeCheck())
                m_Clips[index] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClip);
        }

        private void DrawClipHeader(Rect rect)
        {
            rect.xMax = rect.xMax / 2.0f;
            GUI.Label(rect, "Original", EditorStyles.label);
            rect.xMin = rect.xMax;
            rect.xMax *= 2.0f;
            GUI.Label(rect, "Override", EditorStyles.label);
        }

        private void SelectClip(ReorderableList list)
        {
            if (0 <= list.index && list.index < m_Clips.Count)
                EditorGUIUtility.PingObject(m_Clips[list.index].Key);
        }
    }
}
