// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEditorInternal;
using UnityEditor.Audio;

namespace UnityEditor
{
    class AudioMixerExposedParameterView
    {
        private ReorderableListWithRenameAndScrollView m_ReorderableListWithRenameAndScrollView;
        private AudioMixerController m_Controller;
        private SerializedObject m_ControllerSerialized;
        ReorderableListWithRenameAndScrollView.State m_State;

        private float height { get { return m_ReorderableListWithRenameAndScrollView.list.GetHeight(); } }

        public AudioMixerExposedParameterView(ReorderableListWithRenameAndScrollView.State state)
        {
            m_State = state;
        }

        public void OnMixerControllerChanged(AudioMixerController controller)
        {
            m_Controller = controller;

            if (m_Controller)
                m_Controller.ChangedExposedParameter += new ChangedExposedParameterHandler(RecreateListControl);

            RecreateListControl();
        }

        public void RecreateListControl()
        {
            if (m_Controller != null)
            {
                m_ControllerSerialized = new SerializedObject(m_Controller);
                var exposedParams = m_ControllerSerialized.FindProperty("m_ExposedParameters");
                System.Diagnostics.Debug.Assert(exposedParams != null);

                ReorderableList reorderableList = new ReorderableList(m_ControllerSerialized, exposedParams, false, false, false, false);
                reorderableList.onReorderCallback = EndDragChild;
                reorderableList.drawElementCallback += DrawElement;
                reorderableList.elementHeight = 16;
                reorderableList.headerHeight = 0;
                reorderableList.footerHeight = 0;
                reorderableList.showDefaultBackground = false;

                m_ReorderableListWithRenameAndScrollView = new ReorderableListWithRenameAndScrollView(reorderableList, m_State);
                m_ReorderableListWithRenameAndScrollView.onNameChangedAtIndex += NameChanged;
                m_ReorderableListWithRenameAndScrollView.onDeleteItemAtIndex += Delete;
                m_ReorderableListWithRenameAndScrollView.onGetNameAtIndex += GetNameOfElement;
            }
        }

        public void OnGUI(Rect rect)
        {
            if (m_Controller == null)
                return;

            m_ReorderableListWithRenameAndScrollView.OnGUI(rect);
        }

        public void OnContextClick(int itemIndex)
        {
            GenericMenu pm = new GenericMenu();
            pm.AddItem(
                new GUIContent("Unexpose"),
                false,
                delegate(object data) { Delete((int)data); },
                itemIndex);

            pm.AddItem(
                new GUIContent("Rename"),
                false,
                delegate(object data) { m_ReorderableListWithRenameAndScrollView.BeginRename((int)data, 0f); },
                itemIndex);

            pm.ShowAsContext();
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition))
            {
                OnContextClick(index);
                evt.Use();
            }

            if (Event.current.type != EventType.Repaint)
                return;

            // The left side of the text is drawn by the reorderable list, now draw the additional right aligned text
            using (new EditorGUI.DisabledScope(true))
            {
                m_ReorderableListWithRenameAndScrollView.elementStyleRightAligned.Draw(rect, GetInfoString(index), false, false, false, false);
            }
        }

        public Vector2 CalcSize()
        {
            float maxWidth = 0;
            for (int index = 0; index < m_ReorderableListWithRenameAndScrollView.list.count; index++)
            {
                var width = WidthOfRow(index, m_ReorderableListWithRenameAndScrollView.elementStyle, m_ReorderableListWithRenameAndScrollView.elementStyleRightAligned);
                if (width > maxWidth)
                    maxWidth = width;
            }
            return new Vector2(maxWidth, height);
        }

        string GetInfoString(int index)
        {
            ExposedAudioParameter exposedParam = m_Controller.exposedParameters[index];
            return m_Controller.ResolveExposedParameterPath(exposedParam.guid, false);
        }

        private float WidthOfRow(int index, GUIStyle leftStyle, GUIStyle rightStyle)
        {
            const float kMinSpacing = 25f;
            string infoString = GetInfoString(index);
            Vector2 infoSize = rightStyle.CalcSize(GUIContent.Temp(infoString));
            Vector2 size = leftStyle.CalcSize(GUIContent.Temp(GetNameOfElement(index)));
            float width = size.x + infoSize.x + kMinSpacing;
            return width;
        }

        string GetNameOfElement(int index)
        {
            ExposedAudioParameter exposedParam = m_Controller.exposedParameters[index];
            return exposedParam.name;
        }

        public void NameChanged(int index, string newName)
        {
            const int kMaxNameLength = 64;
            if (newName.Length > kMaxNameLength)
            {
                newName = newName.Substring(0, kMaxNameLength);
                Debug.LogWarning("Maximum name length of an exposed parameter is " + kMaxNameLength + " characters. Name truncated to '" + newName + "'");
            }
            ExposedAudioParameter[] parameters = m_Controller.exposedParameters;
            parameters[index].name = newName;
            m_Controller.exposedParameters = parameters;
        }

        void Delete(int index)
        {
            if (m_Controller != null)
            {
                Undo.RecordObject(m_Controller, "Unexpose Mixer Parameter");
                ExposedAudioParameter exposedParam = m_Controller.exposedParameters[index];
                m_Controller.RemoveExposedParameter(exposedParam.guid);
            }
        }

        public void EndDragChild(ReorderableList list)
        {
            m_ControllerSerialized.ApplyModifiedProperties();
        }

        public void OnEvent()
        {
            m_ReorderableListWithRenameAndScrollView.OnEvent();
        }
    }
}
