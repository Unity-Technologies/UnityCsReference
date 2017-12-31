// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    [System.Serializable]
    class AnimationWindowClipPopup
    {
        [SerializeField] public AnimationWindowState state;
        [SerializeField] private int selectedIndex;

        public void OnGUI()
        {
            if (state.selection.canChangeAnimationClip)
            {
                string[] menuContent = GetClipMenuContent();
                EditorGUI.BeginChangeCheck();
                // TODO: Make this more robust
                selectedIndex = EditorGUILayout.Popup(ClipToIndex(state.activeAnimationClip), menuContent, EditorStyles.toolbarPopup);
                if (EditorGUI.EndChangeCheck())
                {
                    if (menuContent[selectedIndex] == AnimationWindowStyles.createNewClip.text)
                    {
                        AnimationClip newClip = AnimationWindowUtility.CreateNewClip(state.selection.rootGameObject.name);
                        if (newClip)
                        {
                            AnimationWindowUtility.AddClipToAnimationPlayerComponent(state.activeAnimationPlayer, newClip);
                            state.activeAnimationClip = newClip;
                        }

                        //  Layout has changed, bail out now.
                        EditorGUIUtility.ExitGUI();
                    }
                    else
                    {
                        state.activeAnimationClip = IndexToClip(selectedIndex);
                    }
                }
            }
            else if (state.activeAnimationClip != null)
            {
                Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, AnimationWindowStyles.toolbarLabel);
                EditorGUI.LabelField(r, CurveUtility.GetClipName(state.activeAnimationClip), AnimationWindowStyles.toolbarLabel);
            }
        }

        private string[] GetClipMenuContent()
        {
            List<string> content = new List<string>();
            content.AddRange(GetClipNames());

            if (state.selection.rootGameObject != null)
            {
                content.Add("");
                content.Add(AnimationWindowStyles.createNewClip.text);
            }

            return content.ToArray();
        }

        private AnimationClip[] GetOrderedClipList()
        {
            AnimationClip[] clips = new AnimationClip[0];
            if (state.activeRootGameObject != null)
                clips = AnimationUtility.GetAnimationClips(state.activeRootGameObject);

            Array.Sort(clips, (AnimationClip clip1, AnimationClip clip2) => CurveUtility.GetClipName(clip1).CompareTo(CurveUtility.GetClipName(clip2)));

            return clips;
        }

        private string[] GetClipNames()
        {
            AnimationClip[] clips = GetOrderedClipList();
            string[] clipNames = new string[clips.Length];

            for (int i = 0; i < clips.Length; i++)
                clipNames[i] = CurveUtility.GetClipName(clips[i]);

            return clipNames;
        }

        // TODO: Make this more robust
        private AnimationClip IndexToClip(int index)
        {
            if (state.activeRootGameObject != null)
            {
                AnimationClip[] clips = GetOrderedClipList();
                if (index >= 0 && index < clips.Length)
                    return clips[index];
            }

            return null;
        }

        // TODO: Make this more robust
        private int ClipToIndex(AnimationClip clip)
        {
            if (state.activeRootGameObject != null)
            {
                int index = 0;
                AnimationClip[] clips = GetOrderedClipList();
                foreach (AnimationClip other in clips)
                {
                    if (clip == other)
                        return index;
                    index++;
                }
            }
            return 0;
        }
    }
}
