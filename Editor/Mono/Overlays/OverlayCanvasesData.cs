// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Overlays
{
    [Serializable]
    class OverlayCanvasesDataContainer
    {
        public List<SaveData> m_SaveData = new List<SaveData>();

        public List<DynamicPanelContainerData> m_DynamicPanelContainerData = new List<DynamicPanelContainerData>();

        public OverlayCanvasesDataContainer() { }

        public OverlayCanvasesDataContainer(List<SaveData> saveData, List<DynamicPanelContainerData> dynamicPanelContainerData)
        {
            m_SaveData = saveData;
            m_DynamicPanelContainerData = dynamicPanelContainerData;
        }
    }

    [FilePath("Overlays/CanvasesSaveData.asset", FilePathAttribute.Location.PreferencesFolder)]
    class OverlayCanvasesData : ScriptableSingleton<OverlayCanvasesData>, ISerializationCallbackReceiver
    {
        [Serializable]
        struct WindowToCanvasDataPair
        {
            public WindowToCanvasDataPair(string windowType, OverlayCanvasesDataContainer canvasDataContainer)
            {
                m_WindowType = windowType;
                m_CanvasDataContainer = canvasDataContainer;
            }

            public string m_WindowType;
            public OverlayCanvasesDataContainer m_CanvasDataContainer = new OverlayCanvasesDataContainer();
        }

        [SerializeField]
        List<WindowToCanvasDataPair> m_CanvasesData = new();
        Dictionary<string, OverlayCanvasesDataContainer> m_WindowToCanvasData = new();

        [SerializeField]
        OverlayCanvasSaveState m_LastToolbarSaveState;

        [Serializable]
        struct WindowToCanvasPair
        {
            public WindowToCanvasPair(string windowType, OverlayCanvas canvas)
            {
                m_WindowType = windowType;
                m_Canvas = canvas;
            }

            public string m_WindowType;
            public OverlayCanvas m_Canvas;
        }

        // [SerializeField] for the list below is intentionally omitted.
        // Since List is serializable, it will survive domain reload as a ScriptableSingleton field but not serialize to asset.
        // This allows to track last active canvas throughout a session and NOT have this data restored on next session.
        List<WindowToCanvasPair> m_WindowToLastActiveCanvasList = new();

        Dictionary<string, OverlayCanvas> m_WindowToLastActiveCanvasMap = new();

        public void OnBeforeSerialize()
        {
            // Output window to canvas SaveData map to a list
            if (m_CanvasesData == null)
                m_CanvasesData = new();
            else
                m_CanvasesData.Clear();

            foreach (var dataPair in m_WindowToCanvasData)
                m_CanvasesData.Add(new WindowToCanvasDataPair(dataPair.Key, dataPair.Value));
            
            // Output window to last active canvas map to a list
            if (m_WindowToLastActiveCanvasList == null)
                m_WindowToLastActiveCanvasList = new();
            else
                m_WindowToLastActiveCanvasList.Clear();

            foreach (var dataPair in m_WindowToLastActiveCanvasMap)
                m_WindowToLastActiveCanvasList.Add(new WindowToCanvasPair(dataPair.Key, dataPair.Value));
        }

        public void OnAfterDeserialize()
        {
            // Restore window to canvas save datas map
            if (m_WindowToCanvasData == null)
                m_WindowToCanvasData = new();
            else
                m_WindowToCanvasData.Clear();

            if (m_CanvasesData == null)
            {
                m_CanvasesData = new();
                return;
            }

            for (int i = 0; i < m_CanvasesData.Count; ++i)
            {
                var dataPair = m_CanvasesData[i];
                m_WindowToCanvasData.Add(dataPair.m_WindowType, dataPair.m_CanvasDataContainer);
            }

            // Restore window to last active canvas map
            if (m_WindowToLastActiveCanvasMap == null)
                m_WindowToLastActiveCanvasMap = new();
            else
                m_WindowToLastActiveCanvasMap.Clear();

            if (m_WindowToLastActiveCanvasList == null)
            {
                m_WindowToLastActiveCanvasList = new();
                return;
            }

            for (int i = 0; i < m_WindowToLastActiveCanvasList.Count; ++i)
            {
                var dataPair = m_WindowToLastActiveCanvasList[i];
                m_WindowToLastActiveCanvasMap.Add(dataPair.m_WindowType, dataPair.m_Canvas);
            }
        }

        void OnDisable()
        {
            Save();
        }

        public void AddAndSaveCanvasData(EditorWindow containerWindow, OverlayCanvasesDataContainer canvasDataContainer)
        {
            var windowType = containerWindow.GetType().AssemblyQualifiedName;
            if (!m_WindowToCanvasData.TryAdd(windowType, canvasDataContainer))
                m_WindowToCanvasData[windowType] = canvasDataContainer;

            Save();
        }

        public bool GetCanvasData(EditorWindow containerWindow, out OverlayCanvasesDataContainer canvasData)
        {
            if (m_WindowToCanvasData.TryGetValue(containerWindow.GetType().AssemblyQualifiedName, out canvasData) &&
                canvasData != null)
                return true;

            return false;
        }

        void Save()
        {
            Save(true);
        }

        public bool TryGetLastActiveCanvasForWindowType(EditorWindow window, out OverlayCanvas lastActiveCanvas)
        {
            return m_WindowToLastActiveCanvasMap.TryGetValue(window.GetType().AssemblyQualifiedName, out lastActiveCanvas);
        }

        public void SetLastActiveCanvasForWindowType(OverlayCanvas canvas)
        {
            var windowType = canvas.containerWindow.GetType().AssemblyQualifiedName;
            if (!m_WindowToLastActiveCanvasMap.TryAdd(windowType, canvas))
                m_WindowToLastActiveCanvasMap[windowType] = canvas;
        }

        public OverlayCanvasSaveState toolbarSaveState => m_LastToolbarSaveState;
        public void SetToolbarSaveState(OverlayCanvasSaveState save)
        {
            m_LastToolbarSaveState = save;
        }
    }
}
