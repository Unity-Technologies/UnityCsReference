// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor.PlatformSupport
{
    class ReorderableIconLayerList
    {
        UnityEditorInternal.ReorderableList m_List;

        public delegate void ChangedCallbackDelegate(ReorderableIconLayerList list);

        // Used to notify about element order and content changes. The textures list
        // must not be changed during the execution of the callback.
        public ChangedCallbackDelegate onChangedCallback = null;

        public List<Texture2D> textures
        {
            get { return (List<Texture2D>)m_List.list; }
            set { m_List.list = value; }
        }

        public List<Texture2D> previewTextures { get; set; }

        public string headerString = "";

        const int kSlotSize = 86;
        const int kIconSpacing = 6;

        public int m_ImageWidth = 20;
        public int m_ImageHeight = 20;
        public int minItems = 1;
        public int maxItems = 5;


        public void SetElementLabels(params string[] labels)
        {
            m_useCustomLayerLabel = true;
            m_layerLabels = labels;
        }

        private bool m_useCustomLayerLabel;
        private string[] m_layerLabels;

        string GetElementLabel(int index)
        {
            if (m_useCustomLayerLabel)
                return m_layerLabels[index];

            string namestr = LocalizationDatabase.GetLocalizedString("Layer {0}");
            string label = String.Format(namestr, index);

            return label;
        }

        public ReorderableIconLayerList(bool draggable = true, bool showControls = true)
        {
            m_List = new UnityEditorInternal.ReorderableList(new List<Texture2D>(), typeof(Texture2D), draggable, true, showControls, showControls);
            m_List.onAddCallback = OnAdd;
            m_List.onRemoveCallback = OnRemove;
            m_List.onReorderCallback = OnChange;
            m_List.drawElementCallback = OnElementDraw;
            m_List.drawHeaderCallback = OnHeaderDraw;
            m_List.onCanAddCallback = OnCanAdd;
            m_List.onCanRemoveCallback = OnCanRemove;

            UpdateElementHeight();
        }

        public void SetImageSize(int width, int height)
        {
            m_ImageWidth = width;
            m_ImageHeight = height;
            UpdateElementHeight();
        }

        void UpdateElementHeight()
        {
            m_List.elementHeight = kSlotSize * ((float)m_ImageHeight / m_ImageWidth);
        }

        bool OnCanAdd(UnityEditorInternal.ReorderableList list)
        {
            return list.count < maxItems;
        }

        bool OnCanRemove(UnityEditorInternal.ReorderableList list)
        {
            if (list.count <= minItems)
                return false;
            return true;
        }

        void OnAdd(UnityEditorInternal.ReorderableList list)
        {
            textures.Add(null);
            m_List.index = textures.Count - 1;
            OnChange(list);
        }

        void OnRemove(UnityEditorInternal.ReorderableList list)
        {
            textures.RemoveAt(list.index);
            list.index = 0;
            OnChange(list);
        }

        void OnChange(UnityEditorInternal.ReorderableList list)
        {
            if (onChangedCallback != null)
                onChangedCallback(this);
        }

        void OnElementDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            string label = GetElementLabel(index);

            float width = Mathf.Min(rect.width, EditorGUIUtility.labelWidth + 4 + kSlotSize + kIconSpacing);
            GUI.Label(new Rect(rect.x, rect.y, width - kSlotSize - kIconSpacing, 20), label);

            // Texture slot
            int slotWidth = kSlotSize;
            int slotHeight = (int)((float)m_ImageHeight / m_ImageWidth * kSlotSize);  // take into account the aspect ratio
            var textureRect = new Rect(rect.x + rect.width  - slotWidth - slotWidth - kIconSpacing, rect.y, slotWidth, slotHeight);

            EditorGUI.BeginChangeCheck();
            textures[index] = (Texture2D)EditorGUI.ObjectField(textureRect, textures[index], typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
                OnChange(m_List);

            // Preview
            Rect previewRect = new Rect(rect.x + rect.width - slotWidth, rect.y, slotWidth, slotHeight);

            GUI.Box(previewRect, "");

            Texture2D closestIcon = previewTextures[index];

            if (closestIcon != null)
                GUI.DrawTexture(PlatformIconField.GetContentRect(previewRect, 1, 1), previewTextures[index]);
        }

        void OnHeaderDraw(Rect rect)
        {
            GUI.Label(rect, LocalizationDatabase.GetLocalizedString(headerString), EditorStyles.label);
        }

        public void DoLayoutList()
        {
            m_List.DoLayoutList();
        }
    }
} // namespace UnityEditor.AppleTV
