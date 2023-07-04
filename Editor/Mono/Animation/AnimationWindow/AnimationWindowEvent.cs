// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    /// <summary>
    /// Holds the context for AnimationEvent editing.
    /// </summary>
    class AnimationEventEditorState
    {
        static bool s_ShowOverloadedFunctionsDetails = true;
        static bool s_ShowDuplicatedFunctionsDetails = true;

        bool m_ShowOverloadedFunctionsDetails = s_ShowOverloadedFunctionsDetails;
        bool m_ShowDuplicatedFunctionsDetails = s_ShowDuplicatedFunctionsDetails;

        /// <summary>
        /// Used to track whether or not to show extra details about duplicated function names found in among the potential supported functions
        /// </summary>
        public bool ShowOverloadedFunctionsDetails
        {
            get => m_ShowOverloadedFunctionsDetails;
            set
            {
                m_ShowOverloadedFunctionsDetails = s_ShowOverloadedFunctionsDetails = value;
            }
        }

        /// <summary>
        /// Used to track whether or not to show extra details about overloaded function names found in among the potential supported functions
        /// </summary>
        public bool ShowDuplicatedFunctionsDetails
        {
            get => m_ShowDuplicatedFunctionsDetails;
            set
            {
                m_ShowDuplicatedFunctionsDetails = s_ShowDuplicatedFunctionsDetails = value;
            }
        }

        public AnimationEventEditorState()
        {
            m_ShowOverloadedFunctionsDetails = s_ShowOverloadedFunctionsDetails;
            m_ShowDuplicatedFunctionsDetails = s_ShowDuplicatedFunctionsDetails;
        }
    }

    internal class AnimationWindowEvent : ScriptableObject
    {
        public GameObject root;
        public AnimationClip clip;
        public AnimationClipInfoProperties clipInfo;
        public int eventIndex;

        static public AnimationWindowEvent CreateAndEdit(GameObject root, AnimationClip clip, float time)
        {
            AnimationEvent animationEvent = new AnimationEvent();
            animationEvent.time = time;

            // Or add a new one
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            int eventIndex = InsertAnimationEvent(ref events, clip, animationEvent);

            AnimationWindowEvent animationWindowEvent = CreateInstance<AnimationWindowEvent>();
            animationWindowEvent.hideFlags = HideFlags.HideInHierarchy;
            animationWindowEvent.name = "Animation Event";

            animationWindowEvent.root = root;
            animationWindowEvent.clip = clip;
            animationWindowEvent.clipInfo = null;
            animationWindowEvent.eventIndex = eventIndex;

            return animationWindowEvent;
        }

        static public AnimationWindowEvent Edit(GameObject root, AnimationClip clip, int eventIndex)
        {
            AnimationWindowEvent animationWindowEvent = CreateInstance<AnimationWindowEvent>();
            animationWindowEvent.hideFlags = HideFlags.HideInHierarchy;
            animationWindowEvent.name = "Animation Event";

            animationWindowEvent.root = root;
            animationWindowEvent.clip = clip;
            animationWindowEvent.clipInfo = null;
            animationWindowEvent.eventIndex = eventIndex;

            return animationWindowEvent;
        }

        static public AnimationWindowEvent Edit(AnimationClipInfoProperties clipInfo, int eventIndex)
        {
            AnimationWindowEvent animationWindowEvent = CreateInstance<AnimationWindowEvent>();
            animationWindowEvent.hideFlags = HideFlags.HideInHierarchy;
            animationWindowEvent.name = "Animation Event";

            animationWindowEvent.root = null;
            animationWindowEvent.clip = null;
            animationWindowEvent.clipInfo = clipInfo;
            animationWindowEvent.eventIndex = eventIndex;

            return animationWindowEvent;
        }

        static private int InsertAnimationEvent(ref AnimationEvent[] events, AnimationClip clip, AnimationEvent evt)
        {
            Undo.RegisterCompleteObjectUndo(clip, "Add Event");

            // Or add a new one
            int insertIndex = events.Length;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].time > evt.time)
                {
                    insertIndex = i;
                    break;
                }
            }

            ArrayUtility.Insert(ref events, insertIndex, evt);
            AnimationUtility.SetAnimationEvents(clip, events);

            events = AnimationUtility.GetAnimationEvents(clip);
            if (events[insertIndex].time != evt.time || events[insertIndex].functionName != evt.functionName)
                Debug.LogError("Failed insertion");

            return insertIndex;
        }
    }
}
