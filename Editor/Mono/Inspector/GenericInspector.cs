// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityEditor
{
    internal class GenericInspector : Editor
    {
        private AudioFilterGUI m_AudioFilterGUI = null;

        internal override bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height)
        {
            bool result = GetOptimizedGUIBlockImplementation(isDirty, isVisible, out block, out height);

            // Don't use optimizedGUI for audio filters
            if (target is MonoBehaviour)
            {
                if (AudioUtil.HasAudioCallback(target as MonoBehaviour) && AudioUtil.GetCustomFilterChannelCount(target as MonoBehaviour) > 0)
                {
                    return false;
                }
            }

            if (IsMissingMonoBehaviourTarget())
                return false;

            return result;
        }

        internal override bool OnOptimizedInspectorGUI(Rect contentRect)
        {
            bool result = OptimizedInspectorGUIImplementation(contentRect);
            return result;
        }

        public bool MissingMonoBehaviourGUI()
        {
            serializedObject.Update();
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty == null)
                return false;

            EditorGUILayout.PropertyField(scriptProperty);

            MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
            bool showScriptWarning = true;
            if (targetScript != null && targetScript.GetScriptTypeWasJustCreatedFromComponentMenu())
                showScriptWarning = false;

            if (showScriptWarning)
            {
                GUIContent c = EditorGUIUtility.TextContent("The associated script can not be loaded.\nPlease fix any compile errors\nand assign a valid script.");
                EditorGUILayout.HelpBox(c.text, MessageType.Warning, true);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.ForceRebuildInspectors();
            }

            return true;
        }

        bool IsMissingMonoBehaviourTarget()
        {
            return target.GetType() == typeof(MonoBehaviour) || target.GetType() == typeof(ScriptableObject);
        }

        public override void OnInspectorGUI()
        {
            if (IsMissingMonoBehaviourTarget() && MissingMonoBehaviourGUI())
                return;

            base.OnInspectorGUI();

            if (target is MonoBehaviour)
            {
                // Does this have a AudioRead callback?
                if (AudioUtil.HasAudioCallback(target as MonoBehaviour) && AudioUtil.GetCustomFilterChannelCount(target as MonoBehaviour) > 0)
                {
                    if (m_AudioFilterGUI == null)
                        m_AudioFilterGUI = new AudioFilterGUI();
                    m_AudioFilterGUI.DrawAudioFilterGUI(target as MonoBehaviour);
                }
            }
        }
    }
} //namespace
