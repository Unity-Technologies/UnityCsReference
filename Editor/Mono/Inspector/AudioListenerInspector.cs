// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioListener))]
    [CanEditMultipleObjects]
    class AudioListenerInspector : Editor
    {
        private AudioListenerExtensionEditor m_SpatializerEditor = null;
        private bool m_AddSpatializerExtension = false;
        private bool m_AddSpatializerExtensionMixedValues = false;

        private GUIContent addSpatializerExtensionLabel = new GUIContent("Override Spatializer Settings", "Override the Google spatializer's default settings.");

        void OnEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            UpdateSpatializerExtensionMixedValues();
            if (m_AddSpatializerExtension)
                CreateExtensionEditors();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool allowExtensionEditing = (m_AddSpatializerExtension && !m_AddSpatializerExtensionMixedValues) || !serializedObject.isEditingMultipleObjects;
            if (AudioExtensionManager.IsListenerSpatializerExtensionRegistered() && allowExtensionEditing)
            {
                EditorGUI.showMixedValue = m_AddSpatializerExtensionMixedValues;
                bool addSpatializerExtensionNew = EditorGUILayout.Toggle(addSpatializerExtensionLabel, m_AddSpatializerExtension);
                EditorGUI.showMixedValue = false;

                bool showExtensionProperties = false;
                if (m_AddSpatializerExtension != addSpatializerExtensionNew)
                {
                    m_AddSpatializerExtension = addSpatializerExtensionNew;
                    if (m_AddSpatializerExtension)
                    {
                        CreateExtensionEditors();

                        if (m_SpatializerEditor != null)
                            showExtensionProperties = m_SpatializerEditor.FindAudioExtensionProperties(serializedObject);
                    }
                    else
                    {
                        ClearExtensionProperties();
                        DestroyExtensionEditors();
                        showExtensionProperties = false;
                    }
                }
                else if (m_SpatializerEditor != null)
                {
                    showExtensionProperties = m_SpatializerEditor.FindAudioExtensionProperties(serializedObject);
                    if (!showExtensionProperties)
                    {
                        m_AddSpatializerExtension = false;
                        ClearExtensionProperties();
                        DestroyExtensionEditors();
                    }
                }

                if ((m_SpatializerEditor != null) && showExtensionProperties)
                {
                    EditorGUI.indentLevel++;
                    m_SpatializerEditor.OnAudioListenerGUI();
                    EditorGUI.indentLevel--;

                    // Update AudioSourceExtension properties, if we are currently playing in Editor.
                    for (int i = 0; i < targets.Length; i++)
                    {
                        AudioListener listener = targets[i] as AudioListener;
                        if (listener != null)
                        {
                            AudioListenerExtension extension = AudioExtensionManager.GetSpatializerExtension(listener);
                            if (extension != null)
                            {
                                string extensionName = AudioExtensionManager.GetListenerSpatializerExtensionType().Name;
                                for (int j = 0; j < m_SpatializerEditor.GetNumExtensionProperties(); j++)
                                {
                                    PropertyName propertyName = m_SpatializerEditor.GetExtensionPropertyName(j);
                                    float value = 0.0f;
                                    if (listener.ReadExtensionProperty(extensionName, propertyName, ref value))
                                    {
                                        extension.WriteExtensionProperty(propertyName, value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnDisable()
        {
            DestroyExtensionEditors();

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void UpdateSpatializerExtensionMixedValues()
        {
            m_AddSpatializerExtension = false;

            int numTargetsWithSpatializerExtensions = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                AudioListener listener = targets[i] as AudioListener;
                if (listener != null)
                {
                    System.Type spatializerExtensionType = AudioExtensionManager.GetListenerSpatializerExtensionType();
                    if ((spatializerExtensionType != null) && (listener.GetNumExtensionPropertiesForThisExtension(spatializerExtensionType.Name) > 0))
                    {
                        m_AddSpatializerExtension = true;
                        numTargetsWithSpatializerExtensions++;
                    }
                }
            }

            m_AddSpatializerExtensionMixedValues = ((numTargetsWithSpatializerExtensions == 0) || (numTargetsWithSpatializerExtensions == targets.Length)) ? false : true;
            if (m_AddSpatializerExtensionMixedValues)
                m_AddSpatializerExtension = false;
        }

        // Created editors for all the enabled extensions of this AudioSource.
        private void CreateExtensionEditors()
        {
            if (m_SpatializerEditor != null)
                DestroyExtensionEditors();

            System.Type spatializerEditorType = AudioExtensionManager.GetListenerSpatializerExtensionEditorType();
            m_SpatializerEditor = ScriptableObject.CreateInstance(spatializerEditorType) as AudioListenerExtensionEditor;

            if (m_SpatializerEditor != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    AudioListener listener = targets[i] as AudioListener;
                    if (listener != null)
                    {
                        Undo.RecordObject(listener, "Add AudioListener extension properties");
                        PropertyName extensionName = AudioExtensionManager.GetListenerSpatializerExtensionName();
                        for (int j = 0; j < m_SpatializerEditor.GetNumExtensionProperties(); j++)
                        {
                            PropertyName propertyName = m_SpatializerEditor.GetExtensionPropertyName(j);
                            float value = 0.0f;

                            // If the AudioListener is missing an extension property, then create it now.
                            if (!listener.ReadExtensionProperty(extensionName, propertyName, ref value))
                            {
                                value = m_SpatializerEditor.GetExtensionPropertyDefaultValue(j);
                                listener.WriteExtensionProperty(AudioExtensionManager.GetSpatializerName(), extensionName, propertyName, value);
                            }
                        }
                    }
                }
            }

            m_AddSpatializerExtensionMixedValues = false;
        }

        private void DestroyExtensionEditors()
        {
            DestroyImmediate(m_SpatializerEditor);
            m_SpatializerEditor = null;
        }

        private void ClearExtensionProperties()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                AudioListener listener = targets[i] as AudioListener;
                if (listener != null)
                {
                    Undo.RecordObject(listener, "Remove AudioListener extension properties");
                    listener.ClearExtensionProperties(AudioExtensionManager.GetListenerSpatializerExtensionName());
                }
            }

            m_AddSpatializerExtensionMixedValues = false;
        }

        private void UndoRedoPerformed()
        {
            DestroyExtensionEditors();

            UpdateSpatializerExtensionMixedValues();
            if (!m_AddSpatializerExtension && !m_AddSpatializerExtensionMixedValues)
                ClearExtensionProperties();

            if (m_AddSpatializerExtension)
                CreateExtensionEditors();

            Repaint();
        }
    }
}
