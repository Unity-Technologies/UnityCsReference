// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

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

    [HelpURL("script-AnimationWindowEvent")]
    class AnimationEventWrapper : ScriptableObject
    {
        public GameObject root;
        public AnimationClip clip;
        // Only used within AnimationClipEditor.
        [NonSerialized] public AnimationClipInfoProperties clipInfo;
        public int eventIndex;

        static public AnimationEventWrapper CreateAndEdit(GameObject root, AnimationClip clip, float time)
        {
            AnimationEvent animationEvent = new AnimationEvent();
            animationEvent.time = time;

            // Or add a new one
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            int eventIndex = InsertAnimationEvent(ref events, clip, animationEvent);

            AnimationEventWrapper animationEventWrapper = CreateInstance<AnimationEventWrapper>();
            animationEventWrapper.hideFlags = HideFlags.HideInHierarchy;
            animationEventWrapper.name = "Animation Event";

            animationEventWrapper.root = root;
            animationEventWrapper.clip = clip;
            animationEventWrapper.clipInfo = null;
            animationEventWrapper.eventIndex = eventIndex;

            return animationEventWrapper;
        }

        static public AnimationEventWrapper Edit(GameObject root, AnimationClip clip, int eventIndex)
        {
            AnimationEventWrapper animationEventWrapper = CreateInstance<AnimationEventWrapper>();
            animationEventWrapper.hideFlags = HideFlags.HideInHierarchy;
            animationEventWrapper.name = "Animation Event";

            animationEventWrapper.root = root;
            animationEventWrapper.clip = clip;
            animationEventWrapper.clipInfo = null;
            animationEventWrapper.eventIndex = eventIndex;

            return animationEventWrapper;
        }

        static public AnimationEventWrapper Edit(AnimationClipInfoProperties clipInfo, int eventIndex)
        {
            AnimationEventWrapper animationEventWrapper = CreateInstance<AnimationEventWrapper>();
            animationEventWrapper.hideFlags = HideFlags.HideInHierarchy;
            animationEventWrapper.name = "Animation Event";

            animationEventWrapper.root = null;
            animationEventWrapper.clip = null;
            animationEventWrapper.clipInfo = clipInfo;
            animationEventWrapper.eventIndex = eventIndex;

            return animationEventWrapper;
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
