// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace UnityEditor
{
    [CustomEditor(typeof(PlayableDirector))]
    [CanEditMultipleObjects]
    internal class DirectorEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent PlayableText = EditorGUIUtility.TextContent("Playable");
            public static readonly GUIContent InitialTimeContent = EditorGUIUtility.TextContent("Initial Time|The time at which the Playable will begin playing");
            public static readonly GUIContent TimeContent = EditorGUIUtility.TextContent("Current Time|The current Playable time");
            public static readonly GUIContent InitialStateContent = EditorGUIUtility.TextContent("Play On Awake|Whether the Playable should be playing after it loads");
            public static readonly GUIContent UpdateMethod = EditorGUIUtility.TextContent("Update Method|Controls how the Playable updates every frame");
            public static readonly GUIContent WrapModeContent = EditorGUIUtility.TextContent("Wrap Mode|Controls the behaviour of evaluating the Playable outside its duration");
            public static readonly GUIContent NoBindingsContent = EditorGUIUtility.TextContent("This channel will not playback because it is not currently assigned");
            public static readonly GUIContent BindingsTitleContent = EditorGUIUtility.TextContent("Bindings");
        }

        private SerializedProperty m_PlayableAsset;
        private SerializedProperty m_InitialState;
        private SerializedProperty m_WrapMode;
        private SerializedProperty m_InitialTime;
        private SerializedProperty m_UpdateMethod;

        private GUIContent m_AnimatorContent;
        private GUIContent m_ScriptContent;
        private Texture    m_DefaultScriptContentTexture;

        public void OnEnable()
        {
            m_PlayableAsset = serializedObject.FindProperty("m_PlayableAsset");
            m_InitialState = serializedObject.FindProperty("m_InitialState");
            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_UpdateMethod = serializedObject.FindProperty("m_DirectorUpdateMode");
            m_InitialTime = serializedObject.FindProperty("m_InitialTime");

            m_AnimatorContent = new GUIContent(AssetPreview.GetMiniTypeThumbnail(typeof(Animator)));
            m_ScriptContent = new GUIContent(EditorGUIUtility.LoadIcon("ScriptableObject Icon"));
            m_DefaultScriptContentTexture = m_ScriptContent.image;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PlayableAssetField(m_PlayableAsset, Styles.PlayableText);
            EditorGUILayout.PropertyField(m_UpdateMethod, Styles.UpdateMethod);

            var rect = EditorGUILayout.GetControlRect(true);
            var label = EditorGUI.BeginProperty(rect, Styles.InitialStateContent, m_InitialState);
            bool playOnAwake = m_InitialState.enumValueIndex != (int)PlayState.Paused;
            EditorGUI.BeginChangeCheck();
            playOnAwake = EditorGUI.Toggle(rect, label, playOnAwake);
            if (EditorGUI.EndChangeCheck())
            {
                m_InitialState.enumValueIndex = (int)(playOnAwake ? PlayState.Playing : PlayState.Paused);
            }
            EditorGUI.EndProperty();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_WrapMode, Styles.WrapModeContent);
            if (EditorGUI.EndChangeCheck())
            {
                // case 876701 - we need to explicitly set the property so any playing graphs get
                //  updated with the new wrap mode
                DirectorWrapMode mode = (DirectorWrapMode)m_WrapMode.enumValueIndex;
                foreach (var t in targets.OfType<PlayableDirector>())
                {
                    t.extrapolationMode = mode;
                }
            }


            PropertyFieldAsFloat(m_InitialTime, Styles.InitialTimeContent);

            if (Application.isPlaying)
            {
                // time field isn't functional in Editor, unless the sequencer window is open,
                // at which point it isn't required. In playmode it can provide valuable feedback,
                // and allow the user to scrub and debug without the sequencer window
                CurrentTimeField();
            }

            if (targets.Length == 1)
            {
                DoDirectorPerChannelInspector(target as PlayableDirector, m_PlayableAsset.objectReferenceValue as PlayableAsset);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void BindingInspector(PlayableBinding binding, PlayableDirector director)
        {
            if (binding.sourceObject == null)
            {
                return;
            }

            if (binding.streamType == DataStreamType.Audio)
            {
            }
            else if (binding.streamType == DataStreamType.Animation)
            {
                GameObject gameObject = director.GetGenericBinding(binding.sourceObject) as GameObject;
                m_AnimatorContent.text = binding.streamName;
                m_AnimatorContent.tooltip = (gameObject == null) ? Styles.NoBindingsContent.text : string.Empty;
                EditorGUI.BeginChangeCheck();
                Animator animator = EditorGUILayout.ObjectField(m_AnimatorContent, gameObject, typeof(Animator), true) as Animator;
                if (EditorGUI.EndChangeCheck())
                {
                    SetBinding(director, binding.sourceObject, (animator == null) ? null : animator.gameObject);
                }
            }
            else if (binding.streamType == DataStreamType.None)
            {
                Type objectType = typeof(UnityEngine.Object);
                if (binding.sourceBindingType != null && objectType.IsAssignableFrom(binding.sourceBindingType))
                {
                    var obj = director.GetGenericBinding(binding.sourceObject) as UnityEngine.Object;
                    m_ScriptContent.text = binding.streamName;
                    m_ScriptContent.tooltip = (obj == null) ? Styles.NoBindingsContent.text : string.Empty;
                    m_ScriptContent.image = AssetPreview.GetMiniTypeThumbnail(binding.sourceBindingType) ?? m_DefaultScriptContentTexture;
                    EditorGUI.BeginChangeCheck();
                    obj = EditorGUILayout.ObjectField(m_ScriptContent, obj, binding.sourceBindingType, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetBinding(director, binding.sourceObject, obj);
                    }
                }
            }
        }

        private void SetBinding(PlayableDirector director, UnityEngine.Object bindTo, UnityEngine.Object objectToBind)
        {
            if (director == null || bindTo == null)
                return;

            director.SetGenericBinding(bindTo, objectToBind);
            if (!Application.isPlaying)
            {
                director.Stop();
            }
        }

        private void DoDirectorPerChannelInspector(PlayableDirector thisPlayer, PlayableAsset playableAsset)
        {
            if (thisPlayer == null || playableAsset == null)
                return;

            var bindings = playableAsset.outputs;
            if (!bindings.Any())
                return;

            EditorGUILayout.LabelField(Styles.BindingsTitleContent);
            EditorGUI.indentLevel++;
            foreach (var binding in bindings)
            {
                BindingInspector(binding, thisPlayer);
            }
            EditorGUI.indentLevel--;
        }

        // To show the current time field in play mode
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        private static void PlayableAssetField(SerializedProperty property, GUIContent title)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            title = EditorGUI.BeginProperty(rect, title, property);

            // We don't use the serialized property version because the property is UnityEngine.Object,
            // and the selection filter in the dialog uses Object instead of PlayableAsset in that version
            EditorGUI.BeginChangeCheck();
            var prop = EditorGUI.ObjectField(rect, title, property.objectReferenceValue, typeof(PlayableAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = prop;
            }

            EditorGUI.EndProperty();
        }

        private static void PropertyFieldAsFloat(SerializedProperty property, GUIContent title)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            title = EditorGUI.BeginProperty(rect, title, property);
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUI.FloatField(rect, title, (float)property.doubleValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.doubleValue = newValue;
            }
            EditorGUI.EndProperty();
        }

        // Does not use Properties because time is not a serialized property
        private void CurrentTimeField()
        {
            if (targets.Length == 1)
            {
                var thisPlayer = target as PlayableDirector;
                EditorGUI.BeginChangeCheck();
                float t = EditorGUILayout.FloatField(Styles.TimeContent, (float)thisPlayer.time);
                if (EditorGUI.EndChangeCheck())
                {
                    thisPlayer.time = t;
                }
            }
            else
            {
                EditorGUILayout.TextField(Styles.TimeContent, EditorGUI.mixedValueContent.text);
            }
        }
    }
}
